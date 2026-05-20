using Microsoft.EntityFrameworkCore;
using PDV.Domain.Entities;
using PDV.Domain.Interfaces;
using PDV.Infrastructure.Data;

namespace PDV.Infrastructure.Repositories;

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext context) : base(context) { }

    public async Task<Product?> GetByBarcodeAsync(string barcode) =>
        await _dbSet.Include(p => p.Category).Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Barcode == barcode && p.IsActive);

    public async Task<IEnumerable<Product>> GetLowStockAsync() =>
        await _dbSet.Include(p => p.Category)
            .Where(p => p.IsActive && p.StockQuantity <= p.MinStockQuantity)
            .ToListAsync();

    public async Task<IEnumerable<Product>> SearchAsync(string term) =>
        await _dbSet.Include(p => p.Category).Include(p => p.Supplier)
            .Where(p => p.IsActive && (
                p.Name.Contains(term) ||
                (p.Barcode != null && p.Barcode.Contains(term)) ||
                (p.InternalCode != null && p.InternalCode.Contains(term))))
            .ToListAsync();

    public async Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId) =>
        await _dbSet.Where(p => p.IsActive && p.CategoryId == categoryId).ToListAsync();

    public override async Task<IEnumerable<Product>> FindAsync(System.Linq.Expressions.Expression<Func<Product, bool>> predicate) =>
        await _dbSet.Include(p => p.Category).Include(p => p.Supplier).Where(predicate).ToListAsync();
}
