using Microsoft.EntityFrameworkCore;
using PDV.Domain.Entities;
using PDV.Domain.Interfaces;
using PDV.Infrastructure.Data;

namespace PDV.Infrastructure.Repositories;

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(AppDbContext context) : base(context) { }

    public async Task<Customer?> GetByCpfCnpjAsync(string cpfCnpj) =>
        await _dbSet.FirstOrDefaultAsync(c => c.CpfCnpj == cpfCnpj && c.IsActive);

    public async Task<IEnumerable<Customer>> SearchAsync(string term) =>
        await _dbSet.Where(c => c.IsActive && (
            c.Name.Contains(term) ||
            (c.CpfCnpj != null && c.CpfCnpj.Contains(term)) ||
            (c.Phone != null && c.Phone.Contains(term))))
            .ToListAsync();
}
