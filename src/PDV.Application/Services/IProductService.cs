using PDV.Application.DTOs;

namespace PDV.Application.Services;

public interface IProductService
{
    Task<IEnumerable<ProductDto>> GetAllAsync();
    Task<ProductDto?> GetByIdAsync(int id);
    Task<ProductDto?> GetByBarcodeAsync(string barcode);
    Task<IEnumerable<ProductDto>> SearchAsync(string term);
    Task<IEnumerable<ProductDto>> GetLowStockAsync();
    Task<IEnumerable<CategoryDto>> GetCategoriesAsync();
    Task<ProductDto> CreateAsync(CreateProductDto dto);
    Task<ProductDto?> UpdateAsync(int id, CreateProductDto dto);
    Task<bool> DeleteAsync(int id);
    /// <summary>Ativa ou desativa um produto sem excluí-lo fisicamente. Admin only.</summary>
    Task<bool> SetActiveAsync(int id, bool active);
    /// <summary>Retorna todos os produtos, incluindo inativos. Admin only.</summary>
    Task<IEnumerable<ProductDto>> GetAllIncludingInactiveAsync();
    Task<CategoryDto> CreateCategoryAsync(string name, string? description);
}
