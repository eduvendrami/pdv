using PDV.Domain.Entities;

namespace PDV.Domain.Interfaces;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByCpfCnpjAsync(string cpfCnpj);
    Task<IEnumerable<Customer>> SearchAsync(string term);
}
