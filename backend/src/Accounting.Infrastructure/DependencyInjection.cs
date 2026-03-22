using Accounting.Application.Auth.Interfaces;
using Accounting.Application.Dashboard.Interfaces;
using Accounting.Application.Import.Services;
using Accounting.Application.Purchasing.Services;
using Accounting.Application.Sales.Services;
using Accounting.Core.Interfaces;
using Accounting.Infrastructure.Data;
using Accounting.Infrastructure.Data.Seeders;
using Accounting.Infrastructure.Repositories;
using Accounting.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Accounting.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── PostgreSQL via EF Core ────────────────────────────────────────────
        services.AddDbContext<AccountingDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(AccountingDbContext).Assembly.FullName)
            )
        );

        // ── Unit of Work ──────────────────────────────────────────────────────
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ── Core domain services ──────────────────────────────────────────────
        services.AddScoped<IStockService, StockService>();
        services.AddScoped<IAuditService, AuditService>();

        // ── Purchasing ────────────────────────────────────────────────────────
        services.AddScoped<IPurchaseInvoiceService, PurchaseInvoiceService>();

        // ── Sales ─────────────────────────────────────────────────────────────
        services.AddScoped<IBatchSelectionService, BatchSelectionService>();
        services.AddScoped<ISalesInvoiceService, SalesInvoiceService>();

        // ── Import ────────────────────────────────────────────────────────────
        services.AddScoped<ItemImportProcessor>();
        services.AddScoped<OpeningStockImportProcessor>();
        services.AddScoped<IImportService, ImportService>();

        // ── Alerts ────────────────────────────────────────────────────────────
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<AlertScanner>();

        // ── Auth ──────────────────────────────────────────────────────────────
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();

        // ── Dashboard ─────────────────────────────────────────────────────────
        services.AddScoped<IDashboardService, DashboardService>();

        // ── Seeder ────────────────────────────────────────────────────────────
        services.AddScoped<DbSeeder>();

        return services;
    }
}

