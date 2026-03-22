namespace Accounting.Core.Enums;

public enum AlertType
{
    LowStock = 1,
    NearExpiry = 2,
    ExpiredStock = 3,
    BatchRecalled = 4
}

public enum AlertSeverity
{
    Info = 1,
    Warning = 2,
    Critical = 3
}

public enum AlertStatus
{
    /// <summary>Alert is active and requires attention.</summary>
    Active = 1,

    /// <summary>A user has seen the alert but it is not yet resolved.</summary>
    Acknowledged = 2,

    /// <summary>The underlying condition is resolved (stock replenished, batch sold/disposed, etc.).</summary>
    Resolved = 3
}

