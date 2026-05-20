using Microsoft.EntityFrameworkCore;
using PDV.Domain.Entities;
using PDV.Domain.Interfaces;
using PDV.Infrastructure.Data;

namespace PDV.Infrastructure.Repositories;

public class CashSessionRepository : Repository<CashSession>, ICashSessionRepository
{
    public CashSessionRepository(AppDbContext context) : base(context) { }

    public async Task<CashSession?> GetOpenSessionAsync() =>
        await _dbSet.Include(s => s.User).Include(s => s.Movements)
            .FirstOrDefaultAsync(s => s.ClosedAt == null);

    public async Task<CashSession?> GetWithMovementsAsync(int id) =>
        await _dbSet.Include(s => s.Movements).FirstOrDefaultAsync(s => s.Id == id);
}
