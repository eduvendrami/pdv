using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PDV.Domain.Interfaces;
using PDV.Infrastructure.Data;
using PDV.Infrastructure.Services;

namespace PDV.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        // Transient DbContext: each service gets its own instance, preventing the
        // singleton DbContext anti-pattern when resolving from the WPF root container.
        services.AddDbContext<AppDbContext>(
            options => options.UseSqlite(connectionString),
            ServiceLifetime.Transient,
            ServiceLifetime.Singleton); // Options builder is safe as singleton

        services.AddTransient<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
