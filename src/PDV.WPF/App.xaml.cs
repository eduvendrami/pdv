using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PDV.Application;
using PDV.Infrastructure;
using PDV.Infrastructure.Data;
using PDV.Infrastructure.Services;
using PDV.WPF.Services;
using PDV.WPF.ViewModels;

namespace PDV.WPF;

public partial class App : System.Windows.Application
{
    private IHost? _host;
    private System.Threading.Timer? _backupTimer;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // ── Licença QuestPDF (Community — gratuita) ───────────────────────
        QuestPDF.Settings.License = LicenseType.Community;

        // ── Capturadores globais de exceções ──────────────────────────────
        DispatcherUnhandledException += (_, ex) =>
        {
            ShowFatalError(ex.Exception);
            ex.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, ex) =>
        {
            if (ex.ExceptionObject is Exception exception)
                ShowFatalError(exception);
        };
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, ex) =>
        {
            ex.SetObserved();
            // Silencioso para tasks em background — não derruba o app
        };

        // ── Banco de dados em %LocalAppData%\PDV\pdv.db ───────────────────
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PDV");
        Directory.CreateDirectory(appData);
        var dbPath = Path.Combine(appData, "pdv.db");

        // ── Container de DI ───────────────────────────────────────────────
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddInfrastructure($"Data Source={dbPath}");
                services.AddApplication();

                // BackupService como singleton para reutilizar no ViewModel
                services.AddSingleton(_ => new BackupService(dbPath));

                // Serviço de diálogos modais (desacopla ViewModels das Views)
                services.AddSingleton<IDialogService, DialogService>();

                // ViewModels
                services.AddTransient<LoginViewModel>();
                services.AddTransient<SetupViewModel>();
                services.AddTransient<MainViewModel>();
                services.AddTransient<SaleViewModel>();
                services.AddTransient<ProductListViewModel>();
                services.AddTransient<ProductEditViewModel>();
                services.AddTransient<CustomerListViewModel>();
                services.AddTransient<CustomerEditViewModel>();
                services.AddTransient<SupplierListViewModel>();
                services.AddTransient<SupplierEditViewModel>();
                services.AddTransient<StockViewModel>();
                services.AddTransient<CashViewModel>();
                services.AddTransient<ReportsViewModel>();
                services.AddTransient<UserListViewModel>();
                services.AddTransient<UserEditViewModel>();
                services.AddTransient<NFeImportViewModel>();
                services.AddTransient<BackupViewModel>();
            })
            .Build();

        await _host.StartAsync();

        // ── Migração + seed ───────────────────────────────────────────────
        var dbFactory = _host.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            await DbSeeder.SeedAsync(db);

            // ── Roteamento da tela inicial ────────────────────────────────
            bool hasUsers = await db.Users.AnyAsync();
            if (!hasUsers)
            {
                // Primeira instalação — exibe setup para criar o admin
                new Views.SetupWindow().Show();
            }
            else
            {
                // Instalação existente — vai direto para o login
                new Views.LoginWindow().Show();
            }
        }

        // ── Backup automático a cada 6 horas ──────────────────────────────
        var backupService = _host.Services.GetRequiredService<BackupService>();
        _backupTimer = new System.Threading.Timer(
            _ => backupService.CreateBackup(),
            null,
            TimeSpan.FromHours(6),
            TimeSpan.FromHours(6));

        // ── Atalho na área de trabalho ────────────────────────────────────────
        CreateDesktopShortcutIfNeeded();

        // ── Verificação silenciosa de atualização (background) ─────────────
        // Aguarda 8 s para não atrasar a exibição da tela de login/setup.
        _ = CheckForUpdateInBackgroundAsync();
    }

    private static void CreateDesktopShortcutIfNeeded()
    {
        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var lnkPath = Path.Combine(desktop, "PDV.lnk");
            if (File.Exists(lnkPath)) return;

            var currentExe = Process.GetCurrentProcess().MainModule!.FileName;
            var shellType  = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return;

            dynamic shell    = Activator.CreateInstance(shellType)!;
            dynamic shortcut = shell.CreateShortcut(lnkPath);
            shortcut.TargetPath       = currentExe;
            shortcut.WorkingDirectory = Path.GetDirectoryName(currentExe);
            shortcut.Description      = "PDV Material de Construção";
            shortcut.Save();
        }
        catch
        {
            // Falha silenciosa — não bloqueia a inicialização
        }
    }

    private static async Task CheckForUpdateInBackgroundAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(8));
        try
        {
            var svc     = new UpdateService();
            var release = await svc.CheckForUpdateAsync();
            if (release == null) return;

            // Volta para o thread de UI para exibir a janela
            await Current.Dispatcher.InvokeAsync(() =>
            {
                var win = new Views.UpdateProgressWindow(release)
                {
                    Owner = GetMainWindow()
                };
                win.Show();
            });
        }
        catch
        {
            // Falha silenciosa — não interrompe o uso do sistema
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        _backupTimer?.Dispose();
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        base.OnExit(e);
    }

    public static T GetService<T>() where T : notnull =>
        ((App)Current)._host!.Services.GetRequiredService<T>();

    /// <summary>
    /// Retorna a instância de MainWindow que está aberta no momento.
    /// Evita usar Application.Current.MainWindow, que pode apontar para
    /// uma janela já fechada (ex.: LoginWindow) e causar InvalidOperationException.
    /// </summary>
    public static System.Windows.Window? GetMainWindow() =>
        Current.Windows.OfType<Views.MainWindow>().FirstOrDefault();

    private static void ShowFatalError(Exception ex)
    {
        var msg = $"Ocorreu um erro inesperado:\n\n{ex.Message}";
        if (ex.InnerException != null)
            msg += $"\n\nDetalhe: {ex.InnerException.Message}";

        try
        {
            System.Windows.MessageBox.Show(
                msg, "Erro",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        catch { /* último recurso */ }
    }
}
