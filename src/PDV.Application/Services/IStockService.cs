using PDV.Application.DTOs;

namespace PDV.Application.Services;

public interface IStockService
{
    Task<IEnumerable<StockMovementDto>> GetMovementsAsync(int? productId = null);
    Task AdjustStockAsync(AdjustStockDto dto, int userId);
    Task<IEnumerable<ProductDto>> GetLowStockProductsAsync();
}
