using Microsoft.EntityFrameworkCore;
using PDV.Domain.Entities;
using PDV.Domain.Enums;
using PDV.Domain.Interfaces;
using PDV.Infrastructure.Data;

namespace PDV.Infrastructure.Repositories;

public class SaleRepository : Repository<Sale>, ISaleRepository
{
    public SaleRepository(AppDbContext context) : base(context) { }

    public async Task<Sale?> GetWithItemsAsync(int id) =>
        await _dbSet
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .Include(s => s.Payments)
            .Include(s => s.Customer)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task<IEnumerable<Sale>> GetByDateRangeAsync(DateTime start, DateTime end) =>
        await _dbSet
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .Include(s => s.Payments)
            .Include(s => s.Customer)
            .Where(s => s.SaleDate >= start && s.SaleDate <= end)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();

    public async Task<IEnumerable<Sale>> GetByCustomerAsync(int customerId) =>
        await _dbSet.Where(s => s.CustomerId == customerId)
            .OrderByDescending(s => s.SaleDate).ToListAsync();

    public async Task<string> GenerateSaleNumberAsync()
    {
        var today = DateTime.Today;
        var prefix = today.ToString("yyyyMMdd");
        var count = await _dbSet.CountAsync(s => s.SaleDate.Date == today);
        return $"{prefix}-{(count + 1):D4}";
    }

    public async Task<decimal> GetTotalByDateAsync(DateTime date)
    {
        var next = date.AddDays(1);
        return await _dbSet
            .Where(s => s.SaleDate >= date && s.SaleDate < next && s.Status == SaleStatus.Finalizada)
            .SumAsync(s => (decimal?)s.FinalAmount) ?? 0;
    }
}
