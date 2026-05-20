using PDV.Domain.Entities;

namespace PDV.Domain.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetByBarcodeAsync(string barcode);
    Task<IEnumerable<Product>> GetLowStockAsync();
    Task<IEnumerable<Product>> SearchAsync(string term);
    Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId);
}
