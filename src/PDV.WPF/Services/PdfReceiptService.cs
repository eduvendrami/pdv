using System.IO;
using PDV.Application.DTOs;
using PDV.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PDV.WPF.Services;

/// <summary>
/// Gera o comprovante da venda em PDF usando QuestPDF, com o layout timbrado
/// da empresa (ver <see cref="CompanyInfo"/>).
/// Salva em %LocalAppData%\PDV\cupons\Venda_{numero}.pdf e retorna o caminho.
/// </summary>
public static class PdfReceiptService
{
    private static readonly string HeaderBg = "#FBE0CE";
    private static readonly string LineColor = "#444444";

    public static string Generate(SaleDto sale)
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PDV", "cupons");
        Directory.CreateDirectory(folder);

        var safeName = string.Concat(sale.SaleNumber.Split(Path.GetInvalidFileNameChars()));
        var filePath = Path.Combine(folder, $"Venda_{safeName}.pdf");

        byte[]? logoBytes = CompanyInfo.HasLogo ? TryReadLogo() : null;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Content().Column(col =>
                {
                    // ══════════ CABEÇALHO TIMBRADO ══════════
                    col.Item().Border(1).BorderColor(LineColor).Row(row =>
                    {
                        // Logo
                        row.ConstantItem(120).BorderRight(1).BorderColor(LineColor)
                           .Padding(8).AlignMiddle().AlignCenter()
                           .Element(e =>
                           {
                               if (logoBytes is not null)
                                   e.Image(logoBytes).FitArea();
                               else
                                   e.Text("LOGO").FontSize(12).Bold().FontColor(Colors.Grey.Medium);
                           });

                        // Dados da empresa
                        row.RelativeItem().Padding(10).Column(c =>
                        {
                            c.Item().Text(CompanyInfo.Name).FontSize(13).Bold().FontColor("#1565C0");
                            c.Item().PaddingTop(3).Text($"CNPJ: {CompanyInfo.Cnpj}").FontSize(9);
                            c.Item().Text(CompanyInfo.Address).FontSize(9);
                            c.Item().Text(CompanyInfo.Phones).FontSize(9);
                            c.Item().Text(CompanyInfo.Email).FontSize(9);
                        });

                        // Box com dados da venda
                        row.ConstantItem(210).BorderLeft(1).BorderColor(LineColor).Column(c =>
                        {
                            InfoRow(c, "VENDA Nº", sale.SaleNumber);
                            InfoRow(c, "DATA", sale.SaleDate.ToString("dd/MM/yyyy  HH:mm"));
                            InfoRow(c, "CLIENTE", string.IsNullOrWhiteSpace(sale.CustomerName)
                                                      ? "Consumidor Final" : sale.CustomerName!);
                            InfoRow(c, "OPERADOR", sale.UserName ?? "-", last: true);
                        });
                    });

                    // ══════════ TABELA DE ITENS ══════════
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(70);   // Quantidade
                            cols.RelativeColumn();      // Descrição
                            cols.ConstantColumn(80);    // Valor
                            cols.ConstantColumn(90);    // Valor Total
                        });

                        table.Header(header =>
                        {
                            HeaderCell(header, "QUANTIDADE", center: true);
                            HeaderCell(header, "DESCRIÇÃO");
                            HeaderCell(header, "VALOR", right: true);
                            HeaderCell(header, "VALOR TOTAL", right: true);
                        });

                        foreach (var item in sale.Items)
                        {
                            BodyCell(table).AlignCenter().Text(item.Quantity.ToString("N2"));
                            BodyCell(table).Text(item.ProductName);
                            BodyCell(table).AlignRight().Text(item.UnitPrice.ToString("C2"));
                            BodyCell(table).AlignRight().Text(item.TotalPrice.ToString("C2")).SemiBold();
                        }
                    });

                    // ══════════ TOTAIS ══════════
                    col.Item().PaddingTop(8).AlignRight().Column(c =>
                    {
                        c.Item().Text(t =>
                        {
                            t.Span("Subtotal:  ").FontSize(11);
                            t.Span(sale.TotalAmount.ToString("C2")).FontSize(11);
                        });

                        if (sale.DiscountAmount > 0)
                        {
                            c.Item().Text(t =>
                            {
                                t.Span("Desconto:  ").FontSize(11).FontColor(Colors.Red.Darken1);
                                t.Span($"- {sale.DiscountAmount:C2}").FontSize(11).FontColor(Colors.Red.Darken1);
                            });
                        }

                        c.Item().PaddingTop(4).Background(HeaderBg).Padding(6).Text(t =>
                        {
                            t.Span("TOTAL:  ").FontSize(15).Bold();
                            t.Span(sale.FinalAmount.ToString("C2")).FontSize(15).Bold().FontColor("#1565C0");
                        });
                    });

                    // ══════════ PAGAMENTOS ══════════
                    col.Item().PaddingTop(14).Text("PAGAMENTOS").FontSize(10).Bold().FontColor(Colors.Grey.Darken2);
                    foreach (var pay in sale.Payments)
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text(FormatPaymentMethod(pay.Method)).FontSize(11);
                            row.AutoItem().AlignRight().Text(pay.Amount.ToString("C2")).SemiBold().FontSize(11);
                        });
                    }

                    col.Item().PaddingTop(16).AlignCenter().Text("Obrigado pela preferência!")
                       .FontSize(12).Italic().FontColor(Colors.Grey.Darken1);
                });
            });
        }).GeneratePdf(filePath);

        return filePath;
    }

    // ── Helpers de tabela/box ────────────────────────────────────────────────
    private static void InfoRow(ColumnDescriptor c, string label, string value, bool last = false)
    {
        c.Item().Row(r =>
        {
            r.ConstantItem(85).Background(HeaderBg)
             .BorderRight(1).BorderBottom(last ? 0 : 1).BorderColor(LineColor)
             .Padding(5).Text(label).Bold().FontSize(9);
            r.RelativeItem()
             .BorderBottom(last ? 0 : 1).BorderColor(LineColor)
             .Padding(5).Text(value).FontSize(9);
        });
    }

    private static void HeaderCell(TableCellDescriptor header, string text,
                                   bool center = false, bool right = false)
    {
        var cell = header.Cell().Background(HeaderBg).Border(0.5f).BorderColor(LineColor).Padding(5);
        var t = center ? cell.AlignCenter() : right ? cell.AlignRight() : cell;
        t.Text(text).Bold().FontSize(10);
    }

    private static IContainer BodyCell(TableDescriptor table) =>
        table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4);

    private static byte[]? TryReadLogo()
    {
        try { return File.ReadAllBytes(CompanyInfo.LogoFilePath); }
        catch { return null; }
    }

    private static string FormatPaymentMethod(PaymentMethod m) => m switch
    {
        PaymentMethod.CartaoDebito  => "Cartão Débito",
        PaymentMethod.CartaoCredito => "Cartão Crédito",
        PaymentMethod.Crediario     => "Crediário",
        PaymentMethod.Pix           => "PIX",
        _                           => m.ToString()
    };
}
