using AutoMapper;
using PDV.Application.DTOs;
using PDV.Domain.Entities;
using PDV.Domain.Interfaces;

namespace PDV.Application.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWorkFactory _uowFactory;
    private readonly IMapper _mapper;

    public ProductService(IUnitOfWorkFactory uowFactory, IMapper mapper)
    {
        _uowFactory = uowFactory;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync()
    {
        using var uow = _uowFactory.Create();
        var products = await uow.Products.FindAsync(p => p.IsActive);
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        using var uow = _uowFactory.Create();
        var product = await uow.Products.GetByIdAsync(id);
        return product == null ? null : _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto?> GetByBarcodeAsync(string barcode)
    {
        using var uow = _uowFactory.Create();
        var product = await uow.Products.GetByBarcodeAsync(barcode);
        return product == null ? null : _mapper.Map<ProductDto>(product);
    }

    public async Task<IEnumerable<ProductDto>> SearchAsync(string term)
    {
        using var uow = _uowFactory.Create();
        var products = await uow.Products.SearchAsync(term);
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<IEnumerable<ProductDto>> GetLowStockAsync()
    {
        using var uow = _uowFactory.Create();
        var products = await uow.Products.GetLowStockAsync();
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync()
    {
        using var uow = _uowFactory.Create();
        var cats = await uow.Categories.FindAsync(c => c.IsActive);
        return _mapper.Map<IEnumerable<CategoryDto>>(cats);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto)
    {
        using var uow = _uowFactory.Create();
        var product = _mapper.Map<Product>(dto);
        await uow.Products.AddAsync(product);
        await uow.SaveChangesAsync();
        var created = await uow.Products.GetByIdAsync(product.Id);
        return _mapper.Map<ProductDto>(created!);
    }

    public async Task<ProductDto?> UpdateAsync(int id, CreateProductDto dto)
    {
        using var uow = _uowFactory.Create();
        var product = await uow.Products.GetByIdAsync(id);
        if (product == null) return null;
        _mapper.Map(dto, product);
        await uow.Products.UpdateAsync(product);
        await uow.SaveChangesAsync();
        var updated = await uow.Products.GetByIdAsync(id);
        return _mapper.Map<ProductDto>(updated);
    }

    public async Task<bool> DeleteAsync(int id) => await SetActiveAsync(id, false);

    public async Task<bool> SetActiveAsync(int id, bool active)
    {
        using var uow = _uowFactory.Create();
        var product = await uow.Products.GetByIdAsync(id);
        if (product == null) return false;
        product.IsActive = active;
        await uow.Products.UpdateAsync(product);
        await uow.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<ProductDto>> GetAllIncludingInactiveAsync()
    {
        using var uow = _uowFactory.Create();
        var products = await uow.Products.FindAsync(_ => true);
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<CategoryDto> CreateCategoryAsync(string name, string? description)
    {
        using var uow = _uowFactory.Create();
        var cat = new Category { Name = name, Description = description };
        await uow.Categories.AddAsync(cat);
        await uow.SaveChangesAsync();
        return _mapper.Map<CategoryDto>(cat);
    }

    public async Task<string> GetNextInternalCodeAsync()
    {
        using var uow = _uowFactory.Create();
        // Inclui inativos para não reutilizar códigos de produtos desativados
        var all = await uow.Products.FindAsync(_ => true);

        int maxNum = 0;
        int padLen = 5; // mínimo de 5 dígitos (00001, 00002 …)

        foreach (var p in all)
        {
            if (string.IsNullOrWhiteSpace(p.InternalCode)) continue;
            var raw = p.InternalCode.Trim();
            if (!int.TryParse(raw, out int num)) continue;

            if (num > maxNum)     maxNum = num;
            if (raw.Length > padLen) padLen = raw.Length;
        }

        return (maxNum + 1).ToString().PadLeft(padLen, '0');
    }
}
