using AutoMapper;
using PDV.Application.DTOs;
using PDV.Domain.Entities;
using PDV.Domain.Interfaces;

namespace PDV.Application.Services;

public class SupplierService : ISupplierService
{
    private readonly IUnitOfWorkFactory _uowFactory;
    private readonly IMapper _mapper;

    public SupplierService(IUnitOfWorkFactory uowFactory, IMapper mapper)
    {
        _uowFactory = uowFactory;
        _mapper = mapper;
    }

    public async Task<IEnumerable<SupplierDto>> GetAllAsync()
    {
        using var uow = _uowFactory.Create();
        var suppliers = await uow.Suppliers.FindAsync(s => s.IsActive);
        return _mapper.Map<IEnumerable<SupplierDto>>(suppliers);
    }

    public async Task<SupplierDto?> GetByIdAsync(int id)
    {
        using var uow = _uowFactory.Create();
        var s = await uow.Suppliers.GetByIdAsync(id);
        return s == null ? null : _mapper.Map<SupplierDto>(s);
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierDto dto)
    {
        using var uow = _uowFactory.Create();
        var supplier = _mapper.Map<Supplier>(dto);
        await uow.Suppliers.AddAsync(supplier);
        await uow.SaveChangesAsync();
        return _mapper.Map<SupplierDto>(supplier);
    }

    public async Task<SupplierDto?> UpdateAsync(int id, CreateSupplierDto dto)
    {
        using var uow = _uowFactory.Create();
        var supplier = await uow.Suppliers.GetByIdAsync(id);
        if (supplier == null) return null;
        _mapper.Map(dto, supplier);
        await uow.Suppliers.UpdateAsync(supplier);
        await uow.SaveChangesAsync();
        return _mapper.Map<SupplierDto>(supplier);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var uow = _uowFactory.Create();
        var supplier = await uow.Suppliers.GetByIdAsync(id);
        if (supplier == null) return false;
        supplier.IsActive = false;
        await uow.Suppliers.UpdateAsync(supplier);
        await uow.SaveChangesAsync();
        return true;
    }
}
