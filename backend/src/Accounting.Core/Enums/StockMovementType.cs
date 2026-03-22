namespace Accounting.Core.Enums;

public enum StockMovementType
{
    /// <summary>Initial stock loaded via opening stock import or entry.</summary>
    Opening = 1,

    /// <summary>Stock received from a purchase invoice.</summary>
    Purchase = 2,

    /// <summary>Stock consumed by a confirmed sales invoice.</summary>
    Sale = 3,

    /// <summary>Explicit stock adjustment (gain or loss) with a reason.</summary>
    Adjustment = 4,

    /// <summary>Stock transferred out of this warehouse to another.</summary>
    TransferOut = 5,

    /// <summary>Stock received into this warehouse from another.</summary>
    TransferIn = 6,

    /// <summary>Stock returned from a customer (reverses a sale).</summary>
    SalesReturn = 7,

    /// <summary>Stock returned to a supplier (reverses a purchase).</summary>
    PurchaseReturn = 8
}

