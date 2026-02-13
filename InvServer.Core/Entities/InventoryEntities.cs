using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvServer.Core.Entities;

[Table("UNIT_OF_MEASURE")]
public class UnitOfMeasure
{
    [Key]
    public long UnitOfMeasureId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

[Table("CATEGORY")]
public class Category
{
    [Key]
    public long CategoryId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public long? ParentCategoryId { get; set; }
    [ForeignKey(nameof(ParentCategoryId))]
    public Category? ParentCategory { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Category> SubCategories { get; set; } = new List<Category>();
}

[Table("PRODUCT")]
public class Product
{
    [Key]
    public long ProductId { get; set; }

    [Required]
    [MaxLength(50)]
    public string SKU { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public long? CategoryId { get; set; }
    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }

    public long UnitOfMeasureId { get; set; }
    [ForeignKey(nameof(UnitOfMeasureId))]
    public UnitOfMeasure UnitOfMeasure { get; set; } = null!;

    public decimal ReorderLevel { get; set; } = 0;

    public bool IsActive { get; set; } = true;
}

[Table("STOCK_LEVEL")]
public class StockLevel
{
    [Key]
    public long StockLevelId { get; set; }

    public long WarehouseId { get; set; }
    [ForeignKey(nameof(WarehouseId))]
    public Warehouse Warehouse { get; set; } = null!;

    public long ProductId { get; set; }
    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;

    public decimal OnHandQty { get; set; } = 0;
    public decimal ReservedQty { get; set; } = 0;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
