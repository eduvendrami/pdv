namespace PDV.Application.DTOs;

public class NFeInfoDto
{
    public string   NfeNumber    { get; set; } = string.Empty;
    public string   SerieNumber  { get; set; } = string.Empty;
    public DateTime EmissionDate { get; set; }
    public decimal  TotalValue   { get; set; }

    public NFeSupplierDto        Supplier { get; set; } = new();
    public List<NFeItemDto>      Items    { get; set; } = new();
}

public class NFeSupplierDto
{
    public string  Cnpj      { get; set; } = string.Empty;
    public string  Name      { get; set; } = string.Empty;
    public string  TradeName { get; set; } = string.Empty;
    public string? Phone     { get; set; }
    public string  Address   { get; set; } = string.Empty;
    public string  City      { get; set; } = string.Empty;
    public string  State     { get; set; } = string.Empty;
}

public class NFeItemDto
{
    public string  InternalCode  { get; set; } = string.Empty;
    public string? Barcode       { get; set; }   // null se "SEM GTIN"
    public string  Name          { get; set; } = string.Empty;
    public string  UnitLabel     { get; set; } = "PC";
    public decimal Quantity      { get; set; }
    public decimal UnitCostPrice { get; set; }
}
