using Microsoft.EntityFrameworkCore;
using PDV.Domain.Interfaces;
using PDV.Infrastructure.Data;

namespace PDV.Infrastructure.Services;

/// <summary>
/// Produz <see cref="UnitOfWork"/>s sob demanda. Cada uma recebe um
/// <see cref="AppDbContext"/> recém-criado pela <see cref="IDbContextFactory{TContext}"/>
/// (que NÃO é rastreado pelo container de DI), e o descarta no seu próprio Dispose.
/// </summary>
public sealed class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public UnitOfWorkFactory(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public IUnitOfWork Create() => new UnitOfWork(_contextFactory.CreateDbContext());
}
