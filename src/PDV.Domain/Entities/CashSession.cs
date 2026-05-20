namespace PDV.Domain.Entities;

public class CashSession : BaseEntity
{
    public DateTime OpenedAt { get; set; } = DateTime.Now;
    public DateTime? ClosedAt { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal ExpectedBalance { get; set; }
    public decimal Difference { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string? Notes { get; set; }
    public ICollection<CashMovement> Movements { get; set; } = new List<CashMovement>();
}
