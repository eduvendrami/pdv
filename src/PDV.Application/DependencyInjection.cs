using Microsoft.Extensions.DependencyInjection;
using PDV.Application.Mappings;
using PDV.Application.Services;

namespace PDV.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);

        // Transient: WPF resolve services from the root container (no HTTP request scope),
        // so Scoped would silently become a singleton. Transient gives a fresh instance
        // per injection point and avoids DbContext change-tracker accumulation.
        services.AddTransient<IProductService, ProductService>();
        services.AddTransient<ISaleService, SaleService>();
        services.AddTransient<ICustomerService, CustomerService>();
        services.AddTransient<ISupplierService, SupplierService>();
        services.AddTransient<IStockService, StockService>();
        services.AddTransient<ICashService, CashService>();
        services.AddTransient<IReportService, ReportService>();
        services.AddTransient<IAuthService, AuthService>();

        return services;
    }
}
