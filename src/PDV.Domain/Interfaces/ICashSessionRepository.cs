using PDV.Domain.Entities;

namespace PDV.Domain.Interfaces;

public interface ICashSessionRepository : IRepository<CashSession>
{
    Task<CashSession?> GetOpenSessionAsync();
    Task<CashSession?> GetWithMovementsAsync(int id);
}
