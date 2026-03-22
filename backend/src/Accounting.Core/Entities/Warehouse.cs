namespace Accounting.Core.Entities;

public class Warehouse : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    /// <summary>Each branch has exactly one default warehouse used for sales and quick receiving.</summary>
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<ItemBatch> ItemBatches { get; set; } = new List<ItemBatch>();
    public ICollection<StockBalance> StockBalances { get; set; } = new List<StockBalance>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}

