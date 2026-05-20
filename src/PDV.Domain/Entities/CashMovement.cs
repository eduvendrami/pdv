using PDV.Domain.Enums;

namespace PDV.Domain.Entities;

public class CashMovement : BaseEntity
{
    public int CashSessionId { get; set; }
    public CashSession? CashSession { get; set; }
    public CashMovementType Type { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public int? SaleId { get; set; }
}
