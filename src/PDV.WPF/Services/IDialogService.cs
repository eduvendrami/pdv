using System.Collections.Generic;
using PDV.Application.DTOs;

namespace PDV.WPF.Services;

/// <summary>
/// Abstrai a abertura de diálogos modais para que os ViewModels não dependam
/// diretamente das Views (WPF). Permite testar fluxos como a finalização de
/// venda sem precisar de um thread de UI.
/// </summary>
public interface IDialogService
{
    /// <summary>Pede a quantidade de um produto. Retorna null se o operador cancelar.</summary>
    decimal? AskQuantity(ProductDto product, decimal defaultQty = 1);

    /// <summary>Abre o diálogo de pagamento. Retorna os pagamentos ou null se cancelado.</summary>
    IReadOnlyList<PaymentDto>? RequestPayment(decimal total);

    /// <summary>Exibe o cupom/recibo da venda finalizada.</summary>
    void ShowReceipt(SaleDto sale);
}
