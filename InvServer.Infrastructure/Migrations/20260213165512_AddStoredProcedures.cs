using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvServer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStoredProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. sp_GetProductStockLevel
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION sp_GetProductStockLevel(p_product_id BIGINT)
                RETURNS TABLE (
                    WarehouseId BIGINT,
                    OnHandQty DECIMAL,
                    ReservedQty DECIMAL
                ) AS $$
                BEGIN
                    RETURN QUERY
                    SELECT 
                        sl.""WarehouseId"",
                        COALESCE(SUM(sl.""OnHandQty""), 0) as OnHandQty,
                        COALESCE(SUM(sl.""ReservedQty""), 0) as ReservedQty
                    FROM ""STOCK_LEVEL"" sl
                    WHERE sl.""ProductId"" = p_product_id
                    GROUP BY sl.""WarehouseId"";
                END;
                $$ LANGUAGE plpgsql;
            ");

            // 2. sp_UpdateStockQuantity
            migrationBuilder.Sql(@"
                CREATE OR REPLACE Procedure sp_UpdateStockQuantity(
                    p_product_id BIGINT,
                    p_warehouse_id BIGINT,
                    p_delta_on_hand DECIMAL,
                    p_delta_reserved DECIMAL
                )
                LANGUAGE plpgsql
                AS $$
                DECLARE
                    v_exists BOOLEAN;
                BEGIN
                    -- Check if record exists
                    SELECT EXISTS(
                        SELECT 1 FROM ""STOCK_LEVEL"" 
                        WHERE ""ProductId"" = p_product_id AND ""WarehouseId"" = p_warehouse_id
                    ) INTO v_exists;

                    IF v_exists THEN
                        UPDATE ""STOCK_LEVEL""
                        SET 
                            ""OnHandQty"" = ""OnHandQty"" + p_delta_on_hand,
                            ""ReservedQty"" = ""ReservedQty"" + p_delta_reserved,
                            ""UpdatedAt"" = NOW() at time zone 'utc'
                        WHERE ""ProductId"" = p_product_id AND ""WarehouseId"" = p_warehouse_id;
                    ELSE
                        INSERT INTO ""STOCK_LEVEL"" (
                            ""ProductId"", 
                            ""WarehouseId"", 
                            ""OnHandQty"", 
                            ""ReservedQty"", 
                            ""UpdatedAt""
                        ) VALUES (
                            p_product_id, 
                            p_warehouse_id, 
                            p_delta_on_hand, 
                            p_delta_reserved, 
                            NOW() at time zone 'utc'
                        );
                    END IF;
                END;
                $$;
            ");

            // 3. sp_GetLowStockProducts
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION sp_GetLowStockProducts(p_warehouse_id BIGINT DEFAULT NULL)
                RETURNS TABLE (
                    ProductId BIGINT,
                    SKU TEXT,
                    ProductName TEXT,
                    TotalOnHand DECIMAL,
                    ReorderLevel DECIMAL
                ) AS $$
                BEGIN
                    RETURN QUERY
                    SELECT 
                        p.""ProductId"",
                        p.""SKU""::TEXT,
                        p.""Name""::TEXT as ProductName,
                        COALESCE(SUM(sl.""OnHandQty""), 0) as TotalOnHand,
                        p.""ReorderLevel""
                    FROM ""PRODUCT"" p
                    LEFT JOIN ""STOCK_LEVEL"" sl ON p.""ProductId"" = sl.""ProductId""
                    WHERE (p_warehouse_id IS NULL OR sl.""WarehouseId"" = p_warehouse_id)
                    GROUP BY p.""ProductId"", p.""SKU"", p.""Name"", p.""ReorderLevel""
                    HAVING COALESCE(SUM(sl.""OnHandQty""), 0) <= p.""ReorderLevel"";
                END;
                $$ LANGUAGE plpgsql;
            ");

            // 4. sp_GetWarehouseValuation
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION sp_GetWarehouseValuation(p_warehouse_id BIGINT)
                RETURNS DECIMAL
                AS $$
                DECLARE
                    v_total_value DECIMAL;
                BEGIN
                    SELECT COALESCE(SUM(sl.""OnHandQty"" * sml.""UnitCost""), 0)
                    INTO v_total_value
                    -- NOTE: This is an approximation. Real valuation usually requires FIFO/LIFO tracking 
                    -- or a specific cost at the product level. 
                    -- Since we don't store UnitCost on Product, we can't easily calculate this without 
                    -- more complex logic or assuming a standard cost.
                    -- FOR NOW, RETURNING 0 until we define where cost comes from for valuation.
                    -- Ideally, Product should have a StandardCost or helper table.
                    FROM ""STOCK_LEVEL"" sl
                    WHERE sl.""WarehouseId"" = p_warehouse_id;
                    
                    RETURN 0; 
                END;
                $$ LANGUAGE plpgsql;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS sp_GetProductStockLevel(BIGINT);");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_UpdateStockQuantity(BIGINT, BIGINT, DECIMAL, DECIMAL);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS sp_GetLowStockProducts(BIGINT);");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS sp_GetWarehouseValuation(BIGINT);");
        }
    }
}
