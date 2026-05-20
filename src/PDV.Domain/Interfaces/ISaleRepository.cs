using PDV.Domain.Entities;

namespace PDV.Domain.Interfaces;

public interface ISaleRepository : IRepository<Sale>
{
    Task<Sale?> GetWithItemsAsync(int id);
    Task<IEnumerable<Sale>> GetByDateRangeAsync(DateTime start, DateTime end);
    Task<IEnumerable<Sale>> GetByCustomerAsync(int customerId);
    Task<string> GenerateSaleNumberAsync();
    Task<decimal> GetTotalByDateAsync(DateTime date);
}
