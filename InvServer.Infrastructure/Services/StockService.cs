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
            
            // SPECIAL HANDLING FOR TRANSFER
            if (request.MovementTypeCode == MovementTypeCodes.Transfer)
            {
                if (!request.ToWarehouseId.HasValue)
                    throw new InvalidOperationException("ToWarehouseId is required for Transfer movements.");

                if (request.ToWarehouseId == request.WarehouseId)
                    throw new InvalidOperationException("Cannot transfer to the same warehouse.");

                // Create Outbound Movement (Source)
                var outRequest = new StockMovementRequest
                {
                    MovementTypeCode = MovementTypeCodes.TransferOut,
                    WarehouseId = request.WarehouseId,
                    ReasonCode = request.ReasonCode,
                    UserId = request.UserId,
                    Notes = $"Transfer Out to Warehouse {request.ToWarehouseId}. {request.Notes}",
                    Lines = request.Lines.Select(l => new StockMovementLineRequest
                    {
                        ProductId = l.ProductId,
                        QtyDeltaOnHand = -Math.Abs(l.QtyDeltaOnHand), // Ensure negative
                        QtyDeltaReserved = 0, // Transfers affect OnHand typically
                        UnitCost = l.UnitCost,
                        LineNotes = l.LineNotes
                    }).ToList()
                };

                // Create Inbound Movement (Dest)
                var inRequest = new StockMovementRequest
                {
                    MovementTypeCode = MovementTypeCodes.TransferIn,
                    WarehouseId = request.ToWarehouseId.Value,
                    ReasonCode = request.ReasonCode,
                    UserId = request.UserId,
                    Notes = $"Transfer In from Warehouse {request.WarehouseId}. {request.Notes}",
                    Lines = request.Lines.Select(l => new StockMovementLineRequest
                    {
                        ProductId = l.ProductId,
                        QtyDeltaOnHand = Math.Abs(l.QtyDeltaOnHand), // Ensure positive
                        QtyDeltaReserved = 0,
                        UnitCost = l.UnitCost,
                        LineNotes = l.LineNotes
                    }).ToList()
                };

                // Recursively call PostMovementAsync (but passing null for idempotency to avoid loop issues, or we handle manual DB calls here to stay in same transaction?)
                // Since this method is transactional, calling itself safely requires passing the EXISTING transaction?
                // EF Core uses ambient transactions or we can just duplicate logic?
                // Recursive call implies new transaction `BeginTransactionAsync`. Nested transactions are not supported by all providers or EF nicely.
                // BETTER APPROACH: Refactor the core movement logic to a private method that takes the DbContext/Transaction context, or just implement specific logic here.
                
                // Let's implement the dual movement creation manually here to respect the SINGLE transaction we already started.
                
                // --- OUTBOUND ---
                var outType = await _db.InventoryMovementTypes.FirstAsync(t => t.Code == MovementTypeCodes.TransferOut);
                var postedStatus = await _db.InventoryMovementStatuses.FirstAsync(s => s.Code == MovementStatusCodes.Posted);
                long? reasonId = null;
                if (!string.IsNullOrEmpty(request.ReasonCode))
                {
                    var r = await _db.InventoryReasonCodes.FirstOrDefaultAsync(rc => rc.Code == request.ReasonCode);
                    if (r != null) reasonId = r.ReasonCodeId;
                }

                var moveOut = new StockMovement
                {
                    MovementTypeId = outType.MovementTypeId,
                    MovementStatusId = postedStatus.MovementStatusId,
                    ReasonCodeId = reasonId,
                    WarehouseId = request.WarehouseId,
                    CreatedByUserId = request.UserId,
                    PostedByUserId = request.UserId,
                    CreatedAt = DateTime.UtcNow,
                    PostedAt = DateTime.UtcNow,
                    Notes = outRequest.Notes
                };
                _db.StockMovements.Add(moveOut);
                await _db.SaveChangesAsync();

                foreach (var line in outRequest.Lines)
                {
                    _db.StockMovementLines.Add(new StockMovementLine { StockMovementId = moveOut.StockMovementId, ProductId = line.ProductId, QtyDeltaOnHand = line.QtyDeltaOnHand, UnitCost = line.UnitCost, LineNotes = line.LineNotes });
                    await _db.Database.ExecuteSqlRawAsync("CALL sp_UpdateStockQuantity({0}, {1}, {2}, {3})", line.ProductId, outRequest.WarehouseId, line.QtyDeltaOnHand, 0);
                    // Validate Source Stock
                    var sl = await _db.StockLevels.AsNoTracking().FirstAsync(s => s.WarehouseId == outRequest.WarehouseId && s.ProductId == line.ProductId);
                    if (sl.OnHandQty < 0) throw new InvalidOperationException($"Insufficient stock for transfer of Product {line.ProductId} at Source.");
                }

                // --- INBOUND ---
                var inType = await _db.InventoryMovementTypes.FirstAsync(t => t.Code == MovementTypeCodes.TransferIn);
                var moveIn = new StockMovement
                {
                    MovementTypeId = inType.MovementTypeId,
                    MovementStatusId = postedStatus.MovementStatusId,
                    ReasonCodeId = reasonId,
                    WarehouseId = inRequest.WarehouseId,
                    CreatedByUserId = request.UserId,
                    PostedByUserId = request.UserId,
                    CreatedAt = DateTime.UtcNow,
                    PostedAt = DateTime.UtcNow,
                    Notes = inRequest.Notes,
                    ReversedMovementId = moveOut.StockMovementId // Link them logicially? Or adding a new LinkTable? StockMovement has ReversedMovementId but that implies reversal.
                    // For now, we just log them.
                };
                _db.StockMovements.Add(moveIn);
                await _db.SaveChangesAsync();

                foreach (var line in inRequest.Lines)
                {
                    _db.StockMovementLines.Add(new StockMovementLine { StockMovementId = moveIn.StockMovementId, ProductId = line.ProductId, QtyDeltaOnHand = line.QtyDeltaOnHand, UnitCost = line.UnitCost, LineNotes = line.LineNotes });
                    await _db.Database.ExecuteSqlRawAsync("CALL sp_UpdateStockQuantity({0}, {1}, {2}, {3})", line.ProductId, inRequest.WarehouseId, line.QtyDeltaOnHand, 0);
                }

                await _db.SaveChangesAsync(); // <-- Fix: Persist lines for TRANSFER_IN

                await transaction.CommitAsync();
                return moveOut.StockMovementId; // Return Source Movement ID
            }

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
                    .Select(r => r.WorkflowInstance != null && r.WorkflowInstance.CurrentStep != null && r.WorkflowInstance.CurrentStep.StepType.Code == WorkflowStepTypeCodes.Fulfillment)
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

                // 6. Use Stored Procedure for Stock Update
                await _db.Database.ExecuteSqlRawAsync(
                    "CALL sp_UpdateStockQuantity({0}, {1}, {2}, {3})",
                    lineReq.ProductId,
                    request.WarehouseId,
                    lineReq.QtyDeltaOnHand,
                    lineReq.QtyDeltaReserved
                );

                // 7. Validate Invariants (Post-Update Check)
                // We fetch the updated record within the transaction to ensure validity.
                var stockLevel = await _db.StockLevels
                    .AsNoTracking()
                    .FirstAsync(s => s.WarehouseId == request.WarehouseId && s.ProductId == lineReq.ProductId);

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
