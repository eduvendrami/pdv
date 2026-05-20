using PDV.Domain.Enums;

namespace PDV.Domain.Entities;

public class Payment : BaseEntity
{
    public int SaleId { get; set; }
    public Sale? Sale { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
}
