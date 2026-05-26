using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Infrastructure.Services;

namespace PDV.WPF.ViewModels;

public partial class BackupViewModel : BaseViewModel
{
    private readonly BackupService _backupService;
    private readonly string        _backupDir;

    [ObservableProperty] private ObservableCollection<BackupFileInfo> _backups = new();
    [ObservableProperty] private BackupFileInfo? _selectedBackup;
    [ObservableProperty] private string _externalBackupDir;

    public string BackupDir => _backupDir;

    public BackupViewModel(BackupService backupService)
    {
        _backupService     = backupService;
        _backupDir         = backupService.BackupDir;
        _externalBackupDir = backupService.ExternalBackupDir ?? "(não configurado)";
        LoadBackups();
    }

    public void LoadBackups()
    {
        if (!Directory.Exists(_backupDir)) return;
        var files = Directory.GetFiles(_backupDir, "*.db")
            .Select(f => new BackupFileInfo(f))
            .OrderByDescending(b => b.CreatedAt);
        Backups = new ObservableCollection<BackupFileInfo>(files);
    }

    // ── Criar backup agora ────────────────────────────────────────────────
    [RelayCommand]
    public async Task CreateBackupNow()
    {
        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var result = await Task.Run(() => _backupService.CreateBackup());
            LoadBackups();

            if (result.ExternalError != null)
                StatusMessage = $"✔ Backup local criado, mas falhou ao copiar para a nuvem: {result.ExternalError}";
            else if (result.ExternalPath != null)
                StatusMessage = $"✔ Backup criado e copiado para a nuvem em {DateTime.Now:HH:mm:ss}.";
            else
                StatusMessage = $"✔ Backup criado em {DateTime.Now:HH:mm:ss}. (pasta externa não configurada)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro ao criar backup: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    // ── Configurar pasta externa (nuvem) ──────────────────────────────────
    [RelayCommand]
    public void ChooseExternalFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Escolha a pasta sincronizada com a nuvem (OneDrive, Google Drive, Dropbox...)"
        };
        if (dialog.ShowDialog() != true) return;

        var chosen = Path.Combine(dialog.FolderName, "PDV", "backups");
        _backupService.SetExternalBackupDir(chosen);
        ExternalBackupDir = chosen;
        StatusMessage = $"✔ Pasta externa definida:\n{chosen}\nOs próximos backups serão copiados para lá.";
    }

    // ── Desativar pasta externa ───────────────────────────────────────────
    [RelayCommand]
    public void DisableExternalFolder()
    {
        _backupService.SetExternalBackupDir(null);
        ExternalBackupDir = "(não configurado)";
        StatusMessage = "Backup externo desativado.";
    }

    // ── Abrir pasta de backups ────────────────────────────────────────────
    [RelayCommand]
    public void OpenBackupFolder()
    {
        Directory.CreateDirectory(_backupDir);
        Process.Start(new ProcessStartInfo
        {
            FileName        = _backupDir,
            UseShellExecute = true
        });
    }

    // ── Exportar backup para local escolhido pelo usuário ─────────────────
    [RelayCommand]
    public void ExportBackup(BackupFileInfo? backup)
    {
        if (backup == null) return;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName   = backup.FileName,
            DefaultExt = ".db",
            Filter     = "Arquivo de banco de dados (*.db)|*.db|Todos os arquivos (*.*)|*.*",
            Title      = "Exportar backup"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            File.Copy(backup.FullPath, dialog.FileName, overwrite: true);
            StatusMessage = $"✔ Backup exportado para:\n{dialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro ao exportar: {ex.Message}";
        }
    }

    // ── Restaurar backup (requer reinício do app) ─────────────────────────
    [RelayCommand]
    public void RestoreBackup(BackupFileInfo? backup)
    {
        if (backup == null) return;

        var confirm = System.Windows.MessageBox.Show(
            $"Restaurar o backup de {backup.CreatedAt:dd/MM/yyyy HH:mm}?\n\n" +
            "⚠ ATENÇÃO: todos os dados inseridos após essa data serão perdidos.\n" +
            "O sistema será reiniciado automaticamente após a restauração.",
            "Confirmar restauração",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (confirm != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            var appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PDV");
            var dbPath = Path.Combine(appData, "pdv.db");

            var currentExe = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;
            var batPath    = Path.Combine(Path.GetTempPath(), "pdv_restore.bat");

            File.WriteAllText(batPath,
                $"@echo off\r\n" +
                $"timeout /t 2 /nobreak >nul\r\n" +
                $"copy /Y \"{backup.FullPath}\" \"{dbPath}\"\r\n" +
                $"start \"\" \"{currentExe}\"\r\n" +
                $"del \"%~f0\"\r\n");

            System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName        = batPath,
                UseShellExecute = true,
                WindowStyle     = System.Diagnostics.ProcessWindowStyle.Hidden
            });

            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro ao restaurar: {ex.Message}";
        }
    }

    // ── Excluir arquivo de backup ─────────────────────────────────────────
    [RelayCommand]
    public void DeleteBackupFile(BackupFileInfo? backup)
    {
        if (backup == null) return;
        var confirm = System.Windows.MessageBox.Show(
            $"Excluir o arquivo de backup de {backup.CreatedAt:dd/MM/yyyy HH:mm}?",
            "Confirmar", System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);
        if (confirm != System.Windows.MessageBoxResult.Yes) return;
        try
        {
            File.Delete(backup.FullPath);
            LoadBackups();
        }
        catch (Exception ex) { StatusMessage = $"Erro: {ex.Message}"; }
    }
}

// ── Model auxiliar ─────────────────────────────────────────────────────────
public class BackupFileInfo
{
    public string   FullPath  { get; }
    public string   FileName  { get; }
    public DateTime CreatedAt { get; }
    public string   SizeMb    { get; }

    public BackupFileInfo(string fullPath)
    {
        FullPath  = fullPath;
        FileName  = Path.GetFileName(fullPath);
        var info  = new FileInfo(fullPath);
        SizeMb    = $"{info.Length / 1024.0 / 1024.0:N2} MB";

        if (DateTime.TryParseExact(
                Path.GetFileNameWithoutExtension(fullPath).Replace("pdv_backup_", ""),
                "yyyyMMdd_HHmmss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var dt))
            CreatedAt = dt;
        else
            CreatedAt = info.CreationTime;
    }
}
