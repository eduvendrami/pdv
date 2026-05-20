using AutoMapper;
using PDV.Application.DTOs;
using PDV.Domain.Entities;
using PDV.Domain.Interfaces;

namespace PDV.Application.Services;

public class SupplierService : ISupplierService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public SupplierService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<IEnumerable<SupplierDto>> GetAllAsync()
    {
        var suppliers = await _uow.Suppliers.FindAsync(s => s.IsActive);
        return _mapper.Map<IEnumerable<SupplierDto>>(suppliers);
    }

    public async Task<SupplierDto?> GetByIdAsync(int id)
    {
        var s = await _uow.Suppliers.GetByIdAsync(id);
        return s == null ? null : _mapper.Map<SupplierDto>(s);
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierDto dto)
    {
        var supplier = _mapper.Map<Supplier>(dto);
        await _uow.Suppliers.AddAsync(supplier);
        await _uow.SaveChangesAsync();
        return _mapper.Map<SupplierDto>(supplier);
    }

    public async Task<SupplierDto?> UpdateAsync(int id, CreateSupplierDto dto)
    {
        var supplier = await _uow.Suppliers.GetByIdAsync(id);
        if (supplier == null) return null;
        _mapper.Map(dto, supplier);
        await _uow.Suppliers.UpdateAsync(supplier);
        await _uow.SaveChangesAsync();
        return _mapper.Map<SupplierDto>(supplier);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var supplier = await _uow.Suppliers.GetByIdAsync(id);
        if (supplier == null) return false;
        supplier.IsActive = false;
        await _uow.Suppliers.UpdateAsync(supplier);
        await _uow.SaveChangesAsync();
        return true;
    }
}
