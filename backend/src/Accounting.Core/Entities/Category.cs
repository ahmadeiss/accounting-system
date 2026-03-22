namespace Accounting.Core.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Self-referencing: supports parent/child hierarchy (e.g., Food > Dairy > Cheese)
    public Guid? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }
    public ICollection<Category> SubCategories { get; set; } = new List<Category>();

    // Navigation
    public ICollection<Item> Items { get; set; } = new List<Item>();
}

