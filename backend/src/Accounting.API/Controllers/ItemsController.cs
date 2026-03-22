using Accounting.Application.Auth;
using Accounting.Application.Items.DTOs;
using Accounting.Core.Entities;
using Accounting.Core.Exceptions;
using Accounting.Core.Interfaces;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Accounting.Infrastructure.Data;

namespace Accounting.API.Controllers;

/// <summary>Item catalog management endpoints.</summary>
public class ItemsController : BaseController
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateItemRequest> _createValidator;
    private readonly IValidator<UpdateItemRequest> _updateValidator;
    private readonly AccountingDbContext _context;

    public ItemsController(
        IUnitOfWork uow,
        IMapper mapper,
        IValidator<CreateItemRequest> createValidator,
        IValidator<UpdateItemRequest> updateValidator,
        AccountingDbContext context)
    {
        _uow = uow;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _context = context;
    }

    /// <summary>Get paginated list of items.</summary>
    [HttpGet]
    [Authorize(Policy = PermissionNames.ItemsRead)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken ct = default)
    {
        var query = _context.Items
            .AsNoTracking()
            .Include(i => i.Category)
            .Include(i => i.Unit)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(i =>
                i.Name.Contains(search) ||
                i.SKU.Contains(search) ||
                (i.Barcode != null && i.Barcode.Contains(search)));

        if (isActive.HasValue)
            query = query.Where(i => i.IsActive == isActive.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(i => i.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Ok(new
        {
            Items = _mapper.Map<List<ItemDto>>(items),
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)total / pageSize)
        });
    }

    /// <summary>Get a single item by ID.</summary>
    [HttpGet("{id:guid}", Name = "GetItemById")]
    [Authorize(Policy = PermissionNames.ItemsRead)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var item = await _context.Items
            .AsNoTracking()
            .Include(i => i.Category)
            .Include(i => i.Unit)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (item is null) return NotFound(new { Message = $"Item '{id}' not found." });
        return Ok(_mapper.Map<ItemDto>(item));
    }

    /// <summary>Create a new item.</summary>
    [HttpPost]
    [Authorize(Policy = PermissionNames.ItemsWrite)]
    public async Task<IActionResult> Create([FromBody] CreateItemRequest request, CancellationToken ct)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        // Check SKU uniqueness
        if (await _uow.Items.ExistsAsync(i => i.SKU == request.SKU, ct))
            throw new DuplicateEntityException(nameof(Item), "SKU", request.SKU);

        var item = new Item
        {
            Name = request.Name,
            SKU = request.SKU,
            Barcode = request.Barcode,
            Description = request.Description,
            CategoryId = request.CategoryId,
            UnitId = request.UnitId,
            CostPrice = request.CostPrice,
            SalePrice = request.SalePrice,
            ReorderLevel = request.ReorderLevel,
            TrackBatch = request.TrackBatch || request.TrackExpiry,
            TrackExpiry = request.TrackExpiry,
            MinExpiryDaysBeforeSale = request.MinExpiryDaysBeforeSale
        };

        await _uow.Items.AddAsync(item, ct);
        await _uow.SaveChangesAsync(ct);

        return CreatedAtRoute("GetItemById", new { id = item.Id }, new { item.Id });
    }

    /// <summary>Update an existing item.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionNames.ItemsWrite)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateItemRequest request, CancellationToken ct)
    {
        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return UnprocessableEntity(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var item = await _uow.Items.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Item), id);

        item.Name = request.Name;
        item.Barcode = request.Barcode;
        item.Description = request.Description;
        item.CategoryId = request.CategoryId;
        item.UnitId = request.UnitId;
        item.CostPrice = request.CostPrice;
        item.SalePrice = request.SalePrice;
        item.ReorderLevel = request.ReorderLevel;
        item.TrackBatch = request.TrackBatch || request.TrackExpiry;
        item.TrackExpiry = request.TrackExpiry;
        item.MinExpiryDaysBeforeSale = request.MinExpiryDaysBeforeSale;
        item.IsActive = request.IsActive;

        _uow.Items.Update(item);
        await _uow.SaveChangesAsync(ct);

        return NoContent();
    }

    /// <summary>Toggle item active/inactive status.</summary>
    [HttpPatch("{id:guid}/toggle-active")]
    [Authorize(Policy = PermissionNames.ItemsWrite)]
    public async Task<IActionResult> ToggleActive(Guid id, CancellationToken ct)
    {
        var item = await _uow.Items.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Item), id);

        item.IsActive = !item.IsActive;
        _uow.Items.Update(item);
        await _uow.SaveChangesAsync(ct);

        return Ok(new { item.Id, item.IsActive });
    }
}


