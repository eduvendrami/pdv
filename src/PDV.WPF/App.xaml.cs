using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
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
            })
            .Build();

        await _host.StartAsync();

        // ── Migração + seed ───────────────────────────────────────────────
        using (var scope = _host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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
        var backupService = new BackupService(dbPath);
        _backupTimer = new System.Threading.Timer(
            _ => backupService.CreateBackup(),
            null,
            TimeSpan.FromHours(6),
            TimeSpan.FromHours(6));

        // ── Verificação silenciosa de atualização (background) ─────────────
        // Aguarda 8 s para não atrasar a exibição da tela de login/setup.
        _ = CheckForUpdateInBackgroundAsync();
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
                    Owner = Current.MainWindow
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
}
