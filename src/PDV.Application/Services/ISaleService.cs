using PDV.Application.DTOs;

namespace PDV.Application.Services;

public interface ISaleService
{
    Task<SaleDto> CreateSaleAsync(CreateSaleDto dto, int userId);
    Task<SaleDto?> GetByIdAsync(int id);
    Task<IEnumerable<SaleDto>> GetByDateRangeAsync(DateTime start, DateTime end);
    Task<bool> CancelSaleAsync(int id, int userId);
}
