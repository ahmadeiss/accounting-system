namespace Accounting.Core.Entities;

public class Unit : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Item> Items { get; set; } = new List<Item>();
}

