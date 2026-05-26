using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;
using PDV.Application.DTOs;
using PDV.Domain.Enums;
using PDV.WPF.Services;

namespace PDV.WPF.Views;

public partial class SaleReceiptWindow : Window
{
    private readonly SaleDto _sale;

    public SaleReceiptWindow(SaleDto sale)
    {
        InitializeComponent();
        _sale = sale;
        PopulateReceipt();
    }

    private void PopulateReceipt()
    {
        // ── Cabeçalho timbrado (dados da empresa) ──
        CompanyNameText.Text = CompanyInfo.Name;
        CnpjText.Text        = $"CNPJ: {CompanyInfo.Cnpj}";
        AddressText.Text     = CompanyInfo.Address;
        PhonesText.Text      = CompanyInfo.Phones;
        EmailText.Text       = CompanyInfo.Email;
        LoadLogo();

        // ── Dados da venda ──
        TxtDate.Text       = _sale.SaleDate.ToString("dd/MM/yyyy  HH:mm");
        TxtSaleNumber.Text = _sale.SaleNumber;
        TxtUser.Text       = _sale.UserName ?? "-";
        TxtCustomer.Text   = string.IsNullOrWhiteSpace(_sale.CustomerName)
                                 ? "Consumidor Final"
                                 : _sale.CustomerName;

        ItemsGrid.ItemsSource    = _sale.Items;
        PaymentsList.ItemsSource = _sale.Payments.Select(p => new
        {
            Method = FormatPaymentMethod(p.Method),
            p.Amount
        }).ToList();

        TxtSubtotal.Text = _sale.TotalAmount.ToString("C2");
        TxtTotal.Text    = _sale.FinalAmount.ToString("C2");

        if (_sale.DiscountAmount > 0)
            TxtDiscount.Text = $"- {_sale.DiscountAmount:C2}";
        else
            RowDiscount.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Carrega o logo de %LocalAppData%\PDV\logo.png se existir.
    /// Enquanto não houver arquivo, mostra um placeholder "LOGO".
    /// </summary>
    private void LoadLogo()
    {
        if (!CompanyInfo.HasLogo) return;

        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;   // não trava o arquivo
            bmp.UriSource   = new Uri(CompanyInfo.LogoFilePath);
            bmp.EndInit();

            LogoImage.Source       = bmp;
            LogoImage.Visibility   = Visibility.Visible;
            LogoPlaceholder.Visibility = Visibility.Collapsed;
        }
        catch
        {
            // Logo inválido/corrompido: mantém o placeholder.
        }
    }

    private static string FormatPaymentMethod(PaymentMethod m) => m switch
    {
        PaymentMethod.CartaoDebito  => "Cartão Débito",
        PaymentMethod.CartaoCredito => "Cartão Crédito",
        PaymentMethod.Crediario     => "Crediário",
        PaymentMethod.Pix           => "PIX",
        _                           => m.ToString()
    };

    private void BtnPrint_Click(object sender, RoutedEventArgs e)
    {
        BtnPrint.IsEnabled = false;
        try
        {
            // Gera o PDF com QuestPDF e obtém o caminho do arquivo
            var pdfPath = PdfReceiptService.Generate(_sale);

            // Abre o PDF no visualizador padrão do Windows (Acrobat, Edge, etc.)
            Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Erro ao gerar PDF:\n{ex.Message}",
                "Erro",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            BtnPrint.IsEnabled = true;
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
}
