namespace Accounting.Core.Entities;

public class Branch : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
    public ICollection<User> Users { get; set; } = new List<User>();
}

