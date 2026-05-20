using AutoMapper;
using PDV.Application.DTOs;
using PDV.Domain.Entities;
using PDV.Domain.Interfaces;

namespace PDV.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public CustomerService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<IEnumerable<CustomerDto>> GetAllAsync()
    {
        var customers = await _uow.Customers.FindAsync(c => c.IsActive);
        return _mapper.Map<IEnumerable<CustomerDto>>(customers);
    }

    public async Task<CustomerDto?> GetByIdAsync(int id)
    {
        var c = await _uow.Customers.GetByIdAsync(id);
        return c == null ? null : _mapper.Map<CustomerDto>(c);
    }

    public async Task<IEnumerable<CustomerDto>> SearchAsync(string term)
    {
        var customers = await _uow.Customers.SearchAsync(term);
        return _mapper.Map<IEnumerable<CustomerDto>>(customers);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerDto dto)
    {
        var customer = _mapper.Map<Customer>(dto);
        await _uow.Customers.AddAsync(customer);
        await _uow.SaveChangesAsync();
        return _mapper.Map<CustomerDto>(customer);
    }

    public async Task<CustomerDto?> UpdateAsync(int id, CreateCustomerDto dto)
    {
        var customer = await _uow.Customers.GetByIdAsync(id);
        if (customer == null) return null;
        _mapper.Map(dto, customer);
        await _uow.Customers.UpdateAsync(customer);
        await _uow.SaveChangesAsync();
        return _mapper.Map<CustomerDto>(customer);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var customer = await _uow.Customers.GetByIdAsync(id);
        if (customer == null) return false;
        customer.IsActive = false;
        await _uow.Customers.UpdateAsync(customer);
        await _uow.SaveChangesAsync();
        return true;
    }
}
