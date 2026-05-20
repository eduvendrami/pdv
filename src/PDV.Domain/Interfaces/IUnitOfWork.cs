namespace PDV.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IProductRepository Products { get; }
    ISaleRepository Sales { get; }
    ICustomerRepository Customers { get; }
    IUserRepository Users { get; }
    ICashSessionRepository CashSessions { get; }
    IRepository<PDV.Domain.Entities.Category> Categories { get; }
    IRepository<PDV.Domain.Entities.Supplier> Suppliers { get; }
    IRepository<PDV.Domain.Entities.StockMovement> StockMovements { get; }
    Task<int> SaveChangesAsync();
}
