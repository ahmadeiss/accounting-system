using Accounting.Application.Items.DTOs;
using Accounting.Application.Purchasing.DTOs;
using Accounting.Application.Sales.DTOs;
using Accounting.Application.Stock.DTOs;
using Accounting.Core.Entities;
using Accounting.Core.Enums;
using AutoMapper;

namespace Accounting.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ─── Item ─────────────────────────────────────────────────────────────
        CreateMap<Item, ItemDto>()
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category.Name))
            .ForMember(d => d.UnitName, o => o.MapFrom(s => s.Unit.Name))
            .ForMember(d => d.UnitAbbreviation, o => o.MapFrom(s => s.Unit.Abbreviation));

        CreateMap<ItemBatch, ItemBatchDto>()
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.IsExpired, o => o.MapFrom(s =>
                s.ExpiryDate.HasValue && s.ExpiryDate.Value < DateOnly.FromDateTime(DateTime.UtcNow)))
            .ForMember(d => d.DaysUntilExpiry, o => o.MapFrom(s =>
                s.ExpiryDate.HasValue
                    ? (int?)(s.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow.Date).TotalDays
                    : null));

        // ─── Stock ────────────────────────────────────────────────────────────
        CreateMap<StockMovement, StockMovementDto>()
            .ForMember(d => d.ItemName, o => o.MapFrom(s => s.Item.Name))
            .ForMember(d => d.ItemSKU, o => o.MapFrom(s => s.Item.SKU))
            .ForMember(d => d.BatchNumber, o => o.MapFrom(s => s.ItemBatch != null ? s.ItemBatch.BatchNumber : null))
            .ForMember(d => d.WarehouseName, o => o.MapFrom(s => s.Warehouse.Name))
            .ForMember(d => d.MovementType, o => o.MapFrom(s => s.MovementType.ToString()))
            .ForMember(d => d.CreatedByName, o => o.MapFrom(s => s.CreatedBy.FullName));

        CreateMap<StockBalance, StockBalanceDto>()
            .ForMember(d => d.ItemName, o => o.MapFrom(s => s.Item.Name))
            .ForMember(d => d.SKU, o => o.MapFrom(s => s.Item.SKU))
            .ForMember(d => d.WarehouseName, o => o.MapFrom(s => s.Warehouse.Name))
            .ForMember(d => d.ReorderLevel, o => o.MapFrom(s => s.Item.ReorderLevel))
            .ForMember(d => d.IsBelowReorder, o => o.MapFrom(s => s.QuantityOnHand <= s.Item.ReorderLevel));

        // ─── Purchase Invoice ─────────────────────────────────────────────────
        CreateMap<PurchaseInvoice, PurchaseInvoiceDto>()
            .ForMember(d => d.SupplierName, o => o.MapFrom(s => s.Supplier.Name))
            .ForMember(d => d.BranchName, o => o.MapFrom(s => s.Branch.Name))
            .ForMember(d => d.WarehouseName, o => o.MapFrom(s => s.Warehouse.Name))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.CreatedByName, o => o.MapFrom(s => s.CreatedBy.FullName));

        CreateMap<PurchaseInvoiceLine, PurchaseInvoiceLineDto>()
            .ForMember(d => d.ItemName, o => o.MapFrom(s => s.Item.Name))
            .ForMember(d => d.ItemSKU, o => o.MapFrom(s => s.Item.SKU));

        // ─── Sales Invoice ────────────────────────────────────────────────────
        CreateMap<SalesInvoice, SalesInvoiceDto>()
            .ForMember(d => d.BranchName, o => o.MapFrom(s => s.Branch.Name))
            .ForMember(d => d.WarehouseName, o => o.MapFrom(s => s.Warehouse.Name))
            .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.Customer != null ? s.Customer.Name : null))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.PaymentMethod, o => o.MapFrom(s => s.PaymentMethod.ToString()))
            .ForMember(d => d.CreatedByName, o => o.MapFrom(s => s.CreatedBy.FullName));

        // SalesInvoiceLine: batch traceability is now in Allocations; remove old flat fields
        CreateMap<SalesInvoiceLine, SalesInvoiceLineDto>()
            .ForMember(d => d.ItemName, o => o.MapFrom(s => s.Item.Name))
            .ForMember(d => d.ItemSKU, o => o.MapFrom(s => s.Item.SKU))
            .ForMember(d => d.Allocations, o => o.MapFrom(s => s.Allocations));

        // Allocation → DTO: snapshot batch number from the loaded ItemBatch navigation
        CreateMap<SalesInvoiceLineAllocation, SalesInvoiceLineAllocationDto>()
            .ForMember(d => d.BatchNumber,
                o => o.MapFrom(s => s.ItemBatch != null ? s.ItemBatch.BatchNumber : null));
    }
}

