using System.IO;
using System.Windows;
using PDV.WPF.Services;

namespace PDV.WPF.Views;

public partial class UpdateProgressWindow : Window
{
    private readonly ReleaseInfo        _release;
    private          CancellationTokenSource? _cts;

    public UpdateProgressWindow(ReleaseInfo release)
    {
        InitializeComponent();
        _release = release;

        // Preenche as informações da release
        TxtVersion.Text = $"Versão atual: {UpdateService.CurrentVersion.ToString(3)}  →  " +
                          $"Nova versão: {release.Version.ToString(3)}";

        TxtNotes.Text = string.IsNullOrWhiteSpace(release.Notes)
            ? "(sem notas de versão)"
            : release.Notes;
    }

    private async void BtnUpdate_Click(object sender, RoutedEventArgs e)
    {
        BtnUpdate.IsEnabled = false;
        BtnLater.IsEnabled  = false;
        PnlProgress.Visibility = Visibility.Visible;

        _cts = new CancellationTokenSource();
        var destPath = Path.Combine(Path.GetTempPath(), "PDV_update.exe");

        try
        {
            var progress = new Progress<double>(pct =>
            {
                Dispatcher.Invoke(() =>
                {
                    PrgDownload.Value = pct;
                    TxtStatus.Text    = $"Baixando... {pct:N0}%";
                });
            });

            var svc = new UpdateService();
            await svc.DownloadAsync(_release.DownloadUrl, destPath, progress, _cts.Token);

            TxtStatus.Text = "Aplicando atualização...";
            await Task.Delay(500); // pequena pausa visual

            // Substitui o exe atual e reinicia
            UpdateService.ApplyAndRestart(destPath);
        }
        catch (OperationCanceledException)
        {
            TxtStatus.Text = "Cancelado.";
            BtnLater.IsEnabled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Falha ao baixar a atualização:\n{ex.Message}\n\nTente novamente mais tarde.",
                "Erro na atualização",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            BtnUpdate.IsEnabled = true;
            BtnLater.IsEnabled  = true;
            PnlProgress.Visibility = Visibility.Collapsed;
        }
    }

    private void BtnLater_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        Close();
    }

    protected override void OnClosed(System.EventArgs e)
    {
        _cts?.Cancel();
        base.OnClosed(e);
    }
}
