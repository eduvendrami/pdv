using PDV.Domain.Enums;

namespace PDV.Application.DTOs;

public class CashSessionDto
{
    public int Id { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal ExpectedBalance { get; set; }
    public decimal Difference { get; set; }
    public string? UserName { get; set; }
    public string? Notes { get; set; }
    public bool IsOpen => ClosedAt == null;
    public List<CashMovementDto> Movements { get; set; } = new();
}

public class CashMovementDto
{
    public int Id { get; set; }
    public CashMovementType Type { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OpenCashSessionDto
{
    public decimal OpeningBalance { get; set; }
}

public class CloseCashSessionDto
{
    public decimal ClosingBalance { get; set; }
    public string? Notes { get; set; }
}

public class CashSupplyDto
{
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public CashMovementType Type { get; set; }
}
