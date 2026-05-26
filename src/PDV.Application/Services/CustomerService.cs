using AutoMapper;
using PDV.Application.DTOs;
using PDV.Domain.Entities;
using PDV.Domain.Interfaces;

namespace PDV.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly IUnitOfWorkFactory _uowFactory;
    private readonly IMapper _mapper;

    public CustomerService(IUnitOfWorkFactory uowFactory, IMapper mapper)
    {
        _uowFactory = uowFactory;
        _mapper = mapper;
    }

    public async Task<IEnumerable<CustomerDto>> GetAllAsync()
    {
        using var uow = _uowFactory.Create();
        var customers = await uow.Customers.FindAsync(c => c.IsActive);
        return _mapper.Map<IEnumerable<CustomerDto>>(customers);
    }

    public async Task<CustomerDto?> GetByIdAsync(int id)
    {
        using var uow = _uowFactory.Create();
        var c = await uow.Customers.GetByIdAsync(id);
        return c == null ? null : _mapper.Map<CustomerDto>(c);
    }

    public async Task<IEnumerable<CustomerDto>> SearchAsync(string term)
    {
        using var uow = _uowFactory.Create();
        var customers = await uow.Customers.SearchAsync(term);
        return _mapper.Map<IEnumerable<CustomerDto>>(customers);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerDto dto)
    {
        using var uow = _uowFactory.Create();
        var customer = _mapper.Map<Customer>(dto);
        await uow.Customers.AddAsync(customer);
        await uow.SaveChangesAsync();
        return _mapper.Map<CustomerDto>(customer);
    }

    public async Task<CustomerDto?> UpdateAsync(int id, CreateCustomerDto dto)
    {
        using var uow = _uowFactory.Create();
        var customer = await uow.Customers.GetByIdAsync(id);
        if (customer == null) return null;
        _mapper.Map(dto, customer);
        await uow.Customers.UpdateAsync(customer);
        await uow.SaveChangesAsync();
        return _mapper.Map<CustomerDto>(customer);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var uow = _uowFactory.Create();
        var customer = await uow.Customers.GetByIdAsync(id);
        if (customer == null) return false;
        customer.IsActive = false;
        await uow.Customers.UpdateAsync(customer);
        await uow.SaveChangesAsync();
        return true;
    }
}
