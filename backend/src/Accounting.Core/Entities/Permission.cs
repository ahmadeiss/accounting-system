namespace Accounting.Core.Entities;

/// <summary>
/// Represents a granular system permission.
/// Example: Module=Items, Action=Create → Name=items.create
/// </summary>
public class Permission : BaseEntity
{
    /// <summary>Unique machine-readable key. Example: "items.create", "sales.view"</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Module the permission belongs to. Example: "Items", "Sales", "Purchasing"</summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>Action within the module. Example: "create", "view", "edit", "delete", "confirm"</summary>
    public string Action { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    // Navigation
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

