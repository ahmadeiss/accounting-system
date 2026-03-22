namespace Accounting.Application.Auth;

/// <summary>
/// Central registry of all permission codes used throughout the system.
/// Format: {module}.{action} — always lowercase with dot separator.
///
/// When adding a new module:
///   1. Add constants here.
///   2. Add to PermissionNames.All.
///   3. The seeder will auto-insert on first startup.
///   4. Assign to roles via POST /api/v1/roles/{id}/permissions.
/// </summary>
public static class PermissionNames
{
    // ── Items ─────────────────────────────────────────────────────────────────
    public const string ItemsRead  = "items.read";
    public const string ItemsWrite = "items.write";

    // ── Purchases ─────────────────────────────────────────────────────────────
    public const string PurchasesRead    = "purchases.read";
    public const string PurchasesConfirm = "purchases.confirm";

    // ── Sales ─────────────────────────────────────────────────────────────────
    public const string SalesRead    = "sales.read";
    public const string SalesConfirm = "sales.confirm";

    // ── Imports ───────────────────────────────────────────────────────────────
    public const string ImportsRun = "imports.run";

    // ── Alerts ────────────────────────────────────────────────────────────────
    public const string AlertsRead   = "alerts.read";
    public const string AlertsManage = "alerts.manage";

    // ── Users ─────────────────────────────────────────────────────────────────
    public const string UsersManage = "users.manage";

    // ── Roles ─────────────────────────────────────────────────────────────────
    public const string RolesManage = "roles.manage";

    // ── Dashboard ─────────────────────────────────────────────────────────────
    public const string DashboardRead = "dashboard.read";

    /// <summary>All known permissions. Used by the seeder to insert rows on first startup.</summary>
    public static readonly IReadOnlyList<string> All = new[]
    {
        ItemsRead, ItemsWrite,
        PurchasesRead, PurchasesConfirm,
        SalesRead, SalesConfirm,
        ImportsRun,
        AlertsRead, AlertsManage,
        UsersManage, RolesManage,
        DashboardRead,
    };
}

