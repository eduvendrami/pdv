using PDV.Application.DTOs;

namespace PDV.Application.Services;

public interface ICustomerService
{
    Task<IEnumerable<CustomerDto>> GetAllAsync();
    Task<CustomerDto?> GetByIdAsync(int id);
    Task<IEnumerable<CustomerDto>> SearchAsync(string term);
    Task<CustomerDto> CreateAsync(CreateCustomerDto dto);
    Task<CustomerDto?> UpdateAsync(int id, CreateCustomerDto dto);
    Task<bool> DeleteAsync(int id);
}
