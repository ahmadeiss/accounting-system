using Accounting.Core.Entities;
using Accounting.Core.Enums;

namespace Accounting.Core.Tests;

/// <summary>
/// Unit tests for ItemBatch domain logic.
/// These tests verify FEFO-critical business rules with zero infrastructure dependencies.
/// </summary>
public class ItemBatchTests
{
    private static ItemBatch CreateBatch(
        DateOnly? expiryDate = null,
        decimal availableQty = 100,
        BatchStatus status = BatchStatus.Active)
    {
        return new ItemBatch
        {
            ItemId = Guid.NewGuid(),
            WarehouseId = Guid.NewGuid(),
            BatchNumber = "BATCH-001",
            ExpiryDate = expiryDate,
            ReceivedQuantity = availableQty,
            AvailableQuantity = availableQty,
            CostPerUnit = 10,
            Status = status
        };
    }

    [Fact]
    public void IsExpiredOn_WhenExpiryDateIsBeforeCheckDate_ReturnsTrue()
    {
        var batch = CreateBatch(expiryDate: new DateOnly(2024, 1, 1));
        Assert.True(batch.IsExpiredOn(new DateOnly(2024, 1, 2)));
    }

    [Fact]
    public void IsExpiredOn_WhenExpiryDateEqualsCheckDate_ReturnsFalse()
    {
        var batch = CreateBatch(expiryDate: new DateOnly(2024, 6, 15));
        Assert.False(batch.IsExpiredOn(new DateOnly(2024, 6, 15)));
    }

    [Fact]
    public void IsExpiredOn_WhenNoExpiryDate_ReturnsFalse()
    {
        var batch = CreateBatch(expiryDate: null);
        Assert.False(batch.IsExpiredOn(new DateOnly(2099, 1, 1)));
    }

    [Fact]
    public void IsNearExpiryOn_WhenExpiryWithinWindow_ReturnsTrue()
    {
        var batch = CreateBatch(expiryDate: new DateOnly(2024, 6, 10));
        Assert.True(batch.IsNearExpiryOn(new DateOnly(2024, 6, 5), withinDays: 7));
    }

    [Fact]
    public void IsNearExpiryOn_WhenExpiryOutsideWindow_ReturnsFalse()
    {
        var batch = CreateBatch(expiryDate: new DateOnly(2024, 6, 20));
        Assert.False(batch.IsNearExpiryOn(new DateOnly(2024, 6, 5), withinDays: 7));
    }

    [Fact]
    public void IsNearExpiryOn_WhenAlreadyExpired_ReturnsFalse()
    {
        var batch = CreateBatch(expiryDate: new DateOnly(2024, 6, 1));
        Assert.False(batch.IsNearExpiryOn(new DateOnly(2024, 6, 5), withinDays: 7));
    }

    [Fact]
    public void IsSellable_WhenActiveWithStockAndNotExpired_ReturnsTrue()
    {
        var batch = CreateBatch(expiryDate: new DateOnly(2025, 12, 31), availableQty: 50);
        Assert.True(batch.IsSellable(new DateOnly(2025, 1, 1), minDaysBeforeExpiry: 7));
    }

    [Fact]
    public void IsSellable_WhenExpired_ReturnsFalse()
    {
        var batch = CreateBatch(expiryDate: new DateOnly(2024, 1, 1), availableQty: 50);
        Assert.False(batch.IsSellable(new DateOnly(2024, 6, 1), minDaysBeforeExpiry: 0));
    }

    [Fact]
    public void IsSellable_WhenWithinMinExpiryDays_ReturnsFalse()
    {
        var batch = CreateBatch(expiryDate: new DateOnly(2024, 6, 10), availableQty: 50);
        Assert.False(batch.IsSellable(new DateOnly(2024, 6, 5), minDaysBeforeExpiry: 7));
    }

    [Fact]
    public void IsSellable_WhenZeroQuantity_ReturnsFalse()
    {
        var batch = CreateBatch(expiryDate: new DateOnly(2025, 12, 31), availableQty: 0);
        Assert.False(batch.IsSellable(new DateOnly(2025, 1, 1), minDaysBeforeExpiry: 0));
    }

    [Fact]
    public void IsSellable_WhenStatusIsExpired_ReturnsFalse()
    {
        var batch = CreateBatch(expiryDate: new DateOnly(2025, 12, 31), availableQty: 50, status: BatchStatus.Expired);
        Assert.False(batch.IsSellable(new DateOnly(2025, 1, 1), minDaysBeforeExpiry: 0));
    }

    [Fact]
    public void IsSellable_WhenNoExpiryDate_ReturnsTrue()
    {
        var batch = CreateBatch(expiryDate: null, availableQty: 10);
        Assert.True(batch.IsSellable(new DateOnly(2099, 1, 1), minDaysBeforeExpiry: 30));
    }
}