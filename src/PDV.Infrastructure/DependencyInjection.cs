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
        // DbContextFactory: o contexto é criado/descartado por unidade de trabalho,
        // não injetado como Transient enraizado no container raiz do WPF (o que vazaria).
        // A factory é singleton; os contextos que ela cria NÃO são rastreados pelo DI.
        services.AddDbContextFactory<AppDbContext>(
            options => options.UseSqlite(connectionString));

        // Singleton: sem estado próprio, apenas embrulha a IDbContextFactory.
        services.AddSingleton<IUnitOfWorkFactory, UnitOfWorkFactory>();

        return services;
    }
}
