using System.Globalization;
using System.Xml.Linq;
using PDV.Application.DTOs;
using PDV.Domain.Enums;

namespace PDV.WPF.Services;

/// <summary>
/// Lê e interpreta um arquivo XML de NF-e (modelo 55, versão 4.00).
/// </summary>
public static class NFeParseService
{
    private static readonly XNamespace Ns = "http://www.portalfiscal.inf.br/nfe";

    public static NFeInfoDto Parse(string filePath)
    {
        var doc    = XDocument.Load(filePath);
        var infNFe = doc.Descendants(Ns + "infNFe").First();

        var ide   = infNFe.Element(Ns + "ide")!;
        var emit  = infNFe.Element(Ns + "emit")!;
        var ender = emit.Element(Ns + "enderEmit")!;
        var total = infNFe.Element(Ns + "total")!.Element(Ns + "ICMSTot")!;

        var supplier = new NFeSupplierDto
        {
            Cnpj      = FormatCnpj(emit.Element(Ns + "CNPJ")?.Value),
            Name      = emit.Element(Ns + "xNome")?.Value ?? string.Empty,
            TradeName = emit.Element(Ns + "xFant")?.Value ?? string.Empty,
            Phone     = FormatPhone(ender.Element(Ns + "fone")?.Value),
            Address   = BuildAddress(ender),
            City      = ender.Element(Ns + "xMun")?.Value ?? string.Empty,
            State     = ender.Element(Ns + "UF")?.Value   ?? string.Empty,
        };

        var items = infNFe.Elements(Ns + "det").Select(det =>
        {
            var prod = det.Element(Ns + "prod")!;
            var ean  = prod.Element(Ns + "cEAN")?.Value;

            return new NFeItemDto
            {
                InternalCode  = prod.Element(Ns + "cProd")?.Value ?? string.Empty,
                Barcode       = IsValidEan(ean) ? ean : null,
                Name          = prod.Element(Ns + "xProd")?.Value ?? string.Empty,
                UnitLabel     = prod.Element(Ns + "uCom")?.Value  ?? "PC",
                Quantity      = ParseDecimal(prod.Element(Ns + "qCom")?.Value),
                UnitCostPrice = ParseDecimal(prod.Element(Ns + "vUnCom")?.Value),
            };
        }).ToList();

        return new NFeInfoDto
        {
            NfeNumber    = ide.Element(Ns + "nNF")?.Value   ?? string.Empty,
            SerieNumber  = ide.Element(Ns + "serie")?.Value ?? string.Empty,
            EmissionDate = ParseDate(ide.Element(Ns + "dhEmi")?.Value),
            TotalValue   = ParseDecimal(total.Element(Ns + "vNF")?.Value),
            Supplier     = supplier,
            Items        = items,
        };
    }

    // ── Mapeamento de unidade NF-e → enum interno ─────────────────────────
    public static UnitOfMeasure MapUnit(string nfeUnit) =>
        nfeUnit.Trim().ToUpperInvariant() switch
        {
            "PC" or "UN" or "UNID" or "PÇ" or "PCA" => UnitOfMeasure.Unidade,
            "CX" or "CXA"                            => UnitOfMeasure.Caixa,
            "PCT" or "PCO" or "PT"                   => UnitOfMeasure.Pacote,
            "SC" or "SAC"                            => UnitOfMeasure.Saco,
            "KG" or "KGF"                            => UnitOfMeasure.Kg,
            "G"  or "GR"  or "GRM"                   => UnitOfMeasure.Grama,
            "L"  or "LT"  or "LIT"                   => UnitOfMeasure.Litro,
            "ML"                                     => UnitOfMeasure.Ml,
            "M"  or "MT"  or "MET"                   => UnitOfMeasure.Metro,
            "M2" or "M²"                             => UnitOfMeasure.MetroQuadrado,
            "M3" or "M³"                             => UnitOfMeasure.MetroCubico,
            "RL" or "ROL"                            => UnitOfMeasure.Rolo,
            "BD" or "BAL" or "BLD"                   => UnitOfMeasure.Balde,
            "TB" or "TBL"                            => UnitOfMeasure.Tambor,
            "DZ" or "DUZ"                            => UnitOfMeasure.Duzia,
            "PA" or "PAR"                            => UnitOfMeasure.Unidade,
            "CJ" or "CON" or "KIT"                   => UnitOfMeasure.Conjunto,
            _                                        => UnitOfMeasure.Unidade,
        };

    // ── Helpers ───────────────────────────────────────────────────────────
    private static string BuildAddress(XElement ender)
    {
        var logradouro = ender.Element(Ns + "xLgr")?.Value   ?? string.Empty;
        var numero     = ender.Element(Ns + "nro")?.Value    ?? string.Empty;
        var bairro     = ender.Element(Ns + "xBairro")?.Value ?? string.Empty;
        return $"{logradouro}, {numero} - {bairro}".Trim(' ', ',', '-');
    }

    private static bool IsValidEan(string? ean) =>
        !string.IsNullOrWhiteSpace(ean) &&
        !ean.Equals("SEM GTIN", StringComparison.OrdinalIgnoreCase);

    private static decimal ParseDecimal(string? value) =>
        decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;

    private static DateTime ParseDate(string? value) =>
        DateTime.TryParse(value, out var dt) ? dt : DateTime.Now;

    private static string FormatCnpj(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Length != 14) return raw ?? string.Empty;
        return $"{raw[..2]}.{raw[2..5]}.{raw[5..8]}/{raw[8..12]}-{raw[12..]}";
    }

    private static string? FormatPhone(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        raw = new string(raw.Where(char.IsDigit).ToArray());
        return raw.Length == 11 ? $"({raw[..2]}) {raw[2..7]}-{raw[7..]}"
             : raw.Length == 10 ? $"({raw[..2]}) {raw[2..6]}-{raw[6..]}"
             : raw;
    }
}
