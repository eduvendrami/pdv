using PDV.Domain.Entities;
using PDV.Domain.Interfaces;
using PDV.Infrastructure.Data;
using PDV.Infrastructure.Repositories;

namespace PDV.Infrastructure.Services;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IProductRepository? _products;
    private ISaleRepository? _sales;
    private ICustomerRepository? _customers;
    private IUserRepository? _users;
    private ICashSessionRepository? _cashSessions;
    private IRepository<Category>? _categories;
    private IRepository<Supplier>? _suppliers;
    private IRepository<StockMovement>? _stockMovements;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IProductRepository Products => _products ??= new ProductRepository(_context);
    public ISaleRepository Sales => _sales ??= new SaleRepository(_context);
    public ICustomerRepository Customers => _customers ??= new CustomerRepository(_context);
    public IUserRepository Users => _users ??= new UserRepository(_context);
    public ICashSessionRepository CashSessions => _cashSessions ??= new CashSessionRepository(_context);
    public IRepository<Category> Categories => _categories ??= new Repository<Category>(_context);
    public IRepository<Supplier> Suppliers => _suppliers ??= new Repository<Supplier>(_context);
    public IRepository<StockMovement> StockMovements => _stockMovements ??= new Repository<StockMovement>(_context);

    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

    public void Dispose() => _context.Dispose();
}
