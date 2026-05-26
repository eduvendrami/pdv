using AutoMapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PDV.Application.Mappings;
using PDV.Domain.Interfaces;
using PDV.Infrastructure.Data;
using PDV.Infrastructure.Services;

namespace PDV.Tests;

/// <summary>
/// Banco SQLite in-memory com conexão mantida aberta para sobreviver entre contextos.
/// Cada teste cria a sua própria instância (descartável), garantindo isolamento total.
/// O <see cref="UowFactory"/> reproduz o comportamento de produção: cada Create() devolve
/// uma UnitOfWork com um AppDbContext novo apontando para o mesmo banco.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public IUnitOfWorkFactory UowFactory { get; }
    public IMapper Mapper { get; }

    public TestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using (var ctx = new AppDbContext(_options))
            ctx.Database.EnsureCreated();

        UowFactory = new TestUnitOfWorkFactory(_options);
        Mapper = new MapperConfiguration(c => c.AddProfile<AutoMapperProfile>()).CreateMapper();
    }

    /// <summary>Contexto avulso para arrange/assert direto no banco.</summary>
    public AppDbContext NewContext() => new(_options);

    public void Dispose() => _connection.Dispose();

    private sealed class TestUnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly DbContextOptions<AppDbContext> _opts;
        public TestUnitOfWorkFactory(DbContextOptions<AppDbContext> opts) => _opts = opts;
        public IUnitOfWork Create() => new UnitOfWork(new AppDbContext(_opts));
    }
}
