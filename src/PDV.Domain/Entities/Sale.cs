using PDV.Domain.Enums;

namespace PDV.Domain.Entities;

public class Sale : BaseEntity
{
    public string SaleNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; } = DateTime.Now;
    public SaleStatus Status { get; set; } = SaleStatus.Aberta;
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string? Notes { get; set; }
    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
