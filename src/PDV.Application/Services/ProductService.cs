using AutoMapper;
using PDV.Application.DTOs;
using PDV.Domain.Entities;
using PDV.Domain.Interfaces;

namespace PDV.Application.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public ProductService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync()
    {
        var products = await _uow.Products.FindAsync(p => p.IsActive);
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var product = await _uow.Products.GetByIdAsync(id);
        return product == null ? null : _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto?> GetByBarcodeAsync(string barcode)
    {
        var product = await _uow.Products.GetByBarcodeAsync(barcode);
        return product == null ? null : _mapper.Map<ProductDto>(product);
    }

    public async Task<IEnumerable<ProductDto>> SearchAsync(string term)
    {
        var products = await _uow.Products.SearchAsync(term);
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<IEnumerable<ProductDto>> GetLowStockAsync()
    {
        var products = await _uow.Products.GetLowStockAsync();
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync()
    {
        var cats = await _uow.Categories.FindAsync(c => c.IsActive);
        return _mapper.Map<IEnumerable<CategoryDto>>(cats);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto)
    {
        var product = _mapper.Map<Product>(dto);
        await _uow.Products.AddAsync(product);
        await _uow.SaveChangesAsync();
        var created = await _uow.Products.GetByIdAsync(product.Id);
        return _mapper.Map<ProductDto>(created!);
    }

    public async Task<ProductDto?> UpdateAsync(int id, CreateProductDto dto)
    {
        var product = await _uow.Products.GetByIdAsync(id);
        if (product == null) return null;
        _mapper.Map(dto, product);
        await _uow.Products.UpdateAsync(product);
        await _uow.SaveChangesAsync();
        var updated = await _uow.Products.GetByIdAsync(id);
        return _mapper.Map<ProductDto>(updated);
    }

    public async Task<bool> DeleteAsync(int id) => await SetActiveAsync(id, false);

    public async Task<bool> SetActiveAsync(int id, bool active)
    {
        var product = await _uow.Products.GetByIdAsync(id);
        if (product == null) return false;
        product.IsActive = active;
        await _uow.Products.UpdateAsync(product);
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<ProductDto>> GetAllIncludingInactiveAsync()
    {
        var products = await _uow.Products.FindAsync(_ => true);
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<CategoryDto> CreateCategoryAsync(string name, string? description)
    {
        var cat = new Category { Name = name, Description = description };
        await _uow.Categories.AddAsync(cat);
        await _uow.SaveChangesAsync();
        return _mapper.Map<CategoryDto>(cat);
    }
}
