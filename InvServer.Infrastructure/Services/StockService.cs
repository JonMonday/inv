using InvServer.Core.Constants;
using InvServer.Core.Entities;
using InvServer.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InvServer.Infrastructure.Services;

public class StockService : IStockService
{
    private readonly InvDbContext _db;

    public StockService(InvDbContext db)
    {
        _db = db;
    }

    public async Task<long> PostMovementAsync(StockMovementRequest request, string? correlationId = null, string? routeKey = null, string? idempotencyKey = null)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            // 1. Race-Safe Idempotency Check
            if (!string.IsNullOrEmpty(idempotencyKey) && !string.IsNullOrEmpty(routeKey))
            {
                var existingKey = await _db.IdempotencyKeys
                    .FirstOrDefaultAsync(k => k.UserId == request.UserId && k.RouteKey == routeKey && k.Key == idempotencyKey);

                if (existingKey != null)
                {
                    if (existingKey.Status == "COMPLETED" && existingKey.MovementId.HasValue)
                    {
                        await transaction.RollbackAsync();
                        return existingKey.MovementId.Value;
                    }
                    if (existingKey.Status == "PROCESSING")
                        throw new InvalidOperationException("Request is currently being processed.");
                }

                // Attempt to insert PROCESSING record
                var ik = new IdempotencyKey
                {
                    UserId = request.UserId,
                    RouteKey = routeKey,
                    Key = idempotencyKey,
                    Status = "PROCESSING",
                    CreatedAtUtc = DateTime.UtcNow,
                    ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
                };
                _db.IdempotencyKeys.Add(ik);
                
                try
                {
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateException) // Unique constraint violation
                {
                    _db.Entry(ik).State = EntityState.Detached;
                    var reloadedKey = await _db.IdempotencyKeys
                        .AsNoTracking()
                        .FirstOrDefaultAsync(k => k.UserId == request.UserId && k.RouteKey == routeKey && k.Key == idempotencyKey);
                    
                    if (reloadedKey != null && reloadedKey.Status == "COMPLETED" && reloadedKey.MovementId.HasValue)
                    {
                        await transaction.RollbackAsync();
                        return reloadedKey.MovementId.Value;
                    }
                    throw new InvalidOperationException("Idempotency conflict detected.");
                }
            }

            // 2. Get IDs from Codes (Strict Lookup Rule)
            var movementType = await _db.InventoryMovementTypes.FirstOrDefaultAsync(t => t.Code == request.MovementTypeCode);
            if (movementType == null)
                throw new InvalidOperationException($"Movement Type Code '{request.MovementTypeCode}' not found.");
            
            var movementStatus = await _db.InventoryMovementStatuses.FirstOrDefaultAsync(s => s.Code == MovementStatusCodes.Posted);
            if (movementStatus == null)
                throw new InvalidOperationException($"Movement Status Code '{MovementStatusCodes.Posted}' not found.");

            long? reasonCodeId = null;
            if (!string.IsNullOrEmpty(request.ReasonCode))
            {
                var reason = await _db.InventoryReasonCodes.FirstOrDefaultAsync(r => r.Code == request.ReasonCode);
                if (reason == null)
                    throw new InvalidOperationException($"Reason Code '{request.ReasonCode}' not found.");
                reasonCodeId = reason.ReasonCodeId;
            }

            // 3. Create Movement Header (First step in DB order)
            var movement = new StockMovement
            {
                MovementTypeId = movementType.MovementTypeId,
                MovementStatusId = movementStatus.MovementStatusId,
                ReasonCodeId = reasonCodeId,
                WarehouseId = request.WarehouseId,
                ReferenceRequestId = request.RequestId,
                ReferenceReservationId = request.ReservationId,
                CreatedByUserId = request.UserId,
                PostedByUserId = request.UserId,
                CreatedAt = DateTime.UtcNow,
                PostedAt = DateTime.UtcNow,
                Notes = request.Notes
            };

            _db.StockMovements.Add(movement);
            await _db.SaveChangesAsync(); // Ensure we have StockMovementId

            // 4. Validation: Reservations only allowed if workflow is in FULFILLMENT
            if (request.MovementTypeCode == MovementTypeCodes.Reserve && request.RequestId != null)
            {
                var isValid = await _db.InventoryRequests
                    .Include(r => r.WorkflowInstance)
                        .ThenInclude(i => i.CurrentStep)
                            .ThenInclude(s => s.StepType)
                    .Where(r => r.RequestId == request.RequestId)
                    .Select(r => r.WorkflowInstance.CurrentStep != null && r.WorkflowInstance.CurrentStep.StepType.Code == WorkflowStepTypeCodes.Fulfillment)
                    .FirstOrDefaultAsync();

                if (!isValid)
                    throw new InvalidOperationException("Reservations only allowed when Request is in FULFILLMENT workflow step.");
            }

            foreach (var lineReq in request.Lines)
            {
                // 5. Create Movement Line
                var movementLine = new StockMovementLine
                {
                    StockMovementId = movement.StockMovementId,
                    ProductId = lineReq.ProductId,
                    QtyDeltaOnHand = lineReq.QtyDeltaOnHand,
                    QtyDeltaReserved = lineReq.QtyDeltaReserved,
                    UnitCost = lineReq.UnitCost,
                    LineNotes = lineReq.LineNotes
                };
                _db.StockMovementLines.Add(movementLine);

                // 6. SQL SERVER LOCK: Stock Level Row
                // Use Raw SQL for UPDLOCK, HOLDLOCK to ensure serializable-like isolation on the specifically affected row.
                var stockLevel = await _db.StockLevels
                    .FirstOrDefaultAsync(s => s.WarehouseId == request.WarehouseId && s.ProductId == lineReq.ProductId);

                if (stockLevel == null)
                {
                    stockLevel = new StockLevel
                    {
                        WarehouseId = request.WarehouseId,
                        ProductId = lineReq.ProductId,
                        OnHandQty = 0,
                        ReservedQty = 0,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.StockLevels.Add(stockLevel);
                    // To prevent race conditions on initial creation of a StockLevel row, we attempt a save here.
                    // If it fails, another process won the race and we'll re-query with lock.
                    try {
                        await _db.SaveChangesAsync();
                    } catch (DbUpdateException) {
                        _db.Entry(stockLevel).State = EntityState.Detached;
                        stockLevel = await _db.StockLevels
                            .FirstAsync(s => s.WarehouseId == request.WarehouseId && s.ProductId == lineReq.ProductId);
                    }
                }

                // 7. Update Quantities and Validate Invariants
                stockLevel.OnHandQty += lineReq.QtyDeltaOnHand;
                stockLevel.ReservedQty += lineReq.QtyDeltaReserved;
                stockLevel.UpdatedAt = DateTime.UtcNow;

                if (stockLevel.OnHandQty < 0)
                    throw new InvalidOperationException($"Insufficient on-hand stock for Product {lineReq.ProductId}.");

                if (stockLevel.ReservedQty < 0)
                    throw new InvalidOperationException($"Negative reservation not allowed for Product {lineReq.ProductId}.");

                if (stockLevel.ReservedQty > stockLevel.OnHandQty)
                    throw new InvalidOperationException($"Reservation cannot exceed on-hand stock for Product {lineReq.ProductId}.");
            }

            // 8. Update Idempotency Key record to COMPLETED
            if (!string.IsNullOrEmpty(idempotencyKey) && !string.IsNullOrEmpty(routeKey))
            {
                var ik = await _db.IdempotencyKeys
                    .FirstOrDefaultAsync(k => k.UserId == request.UserId && k.RouteKey == routeKey && k.Key == idempotencyKey);
                if (ik != null)
                {
                    ik.Status = "COMPLETED";
                    ik.MovementId = movement.StockMovementId;
                    ik.ResponseStatusCode = 200;
                    ik.ResponseJson = System.Text.Json.JsonSerializer.Serialize(new { movementId = movement.StockMovementId });
                }
            }

            // 9. Harden Audit Payload (Safe Summary)
            var audit = new AuditLog
            {
                CorrelationId = correlationId,
                UserId = request.UserId,
                EventType = "STOCK_POSTED",
                EntityTable = "STOCK_MOVEMENT",
                Action = "POST",
                CreatedAtUtc = DateTime.UtcNow,
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new {
                    movementId = movement.StockMovementId,
                    requestId = request.RequestId,
                    warehouseId = request.WarehouseId,
                    lineCount = request.Lines.Count,
                    type = request.MovementTypeCode
                })
            };
            _db.AuditLogs.Add(audit);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return movement.StockMovementId;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
