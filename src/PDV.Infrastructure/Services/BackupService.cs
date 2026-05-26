namespace PDV.Infrastructure.Services;

/// <summary>Resultado de uma operação de backup (local + cópia externa opcional).</summary>
public record BackupResult(string LocalPath, string? ExternalPath, string? ExternalError);

public class BackupService
{
    private const int KeepCount = 10;

    private readonly string _dbPath;
    private readonly string _backupDir;
    private readonly string _configPath;
    private string? _externalBackupDir;

    public string  BackupDir         => _backupDir;
    public string? ExternalBackupDir => _externalBackupDir;

    public BackupService(string dbPath)
    {
        _dbPath = dbPath;
        var baseDir = Path.GetDirectoryName(dbPath) ?? ".";

        _backupDir = Path.Combine(baseDir, "backups");
        Directory.CreateDirectory(_backupDir);

        _configPath = Path.Combine(baseDir, "external_backup.txt");
        _externalBackupDir = ResolveExternalDir();
    }

    /// <summary>Cria o backup local e, se configurada, copia para a pasta externa (nuvem).</summary>
    public BackupResult CreateBackup()
    {
        var fileName  = $"pdv_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
        var localDest = Path.Combine(_backupDir, fileName);

        if (!File.Exists(_dbPath))
            return new BackupResult(localDest, null, null);

        File.Copy(_dbPath, localDest, overwrite: true);
        CleanOld(_backupDir);

        // Cópia para a pasta externa sincronizada com a nuvem (OneDrive, Google Drive, etc.)
        if (_externalBackupDir is null)
            return new BackupResult(localDest, null, null);

        try
        {
            Directory.CreateDirectory(_externalBackupDir);
            var externalDest = Path.Combine(_externalBackupDir, fileName);
            File.Copy(localDest, externalDest, overwrite: true);
            CleanOld(_externalBackupDir);
            return new BackupResult(localDest, externalDest, null);
        }
        catch (Exception ex)
        {
            // Falha na cópia externa não invalida o backup local.
            return new BackupResult(localDest, null, ex.Message);
        }
    }

    /// <summary>Define (e persiste) a pasta externa de backup. Passe null para desativar.</summary>
    public void SetExternalBackupDir(string? dir)
    {
        _externalBackupDir = string.IsNullOrWhiteSpace(dir) ? null : dir.Trim();
        try
        {
            if (_externalBackupDir is null)
            {
                if (File.Exists(_configPath)) File.Delete(_configPath);
            }
            else
            {
                File.WriteAllText(_configPath, _externalBackupDir);
            }
        }
        catch { /* persistência best-effort */ }
    }

    // ── Resolução da pasta externa: env var > configuração salva > autodetecção ──
    private string? ResolveExternalDir()
    {
        var env = Environment.GetEnvironmentVariable("PDV_BACKUP_EXTERNAL_DIR");
        if (!string.IsNullOrWhiteSpace(env)) return env.Trim();

        try
        {
            if (File.Exists(_configPath))
            {
                var saved = File.ReadAllText(_configPath).Trim();
                if (!string.IsNullOrWhiteSpace(saved)) return saved;
            }
        }
        catch { /* ignora */ }

        return AutoDetectCloudDir();
    }

    /// <summary>Tenta localizar uma pasta de nuvem comum e devolve {pasta}\PDV\backups.</summary>
    private static string? AutoDetectCloudDir()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("OneDrive"),
            Environment.GetEnvironmentVariable("OneDriveConsumer"),
            Environment.GetEnvironmentVariable("OneDriveCommercial"),
            Path.Combine(profile, "OneDrive"),
            Path.Combine(profile, "Google Drive"),
            Path.Combine(profile, "Dropbox"),
        };

        foreach (var c in candidates)
            if (!string.IsNullOrWhiteSpace(c) && Directory.Exists(c))
                return Path.Combine(c!, "PDV", "backups");

        return null;
    }

    private static void CleanOld(string dir)
    {
        var files = Directory.GetFiles(dir, "pdv_backup_*.db")
            .OrderByDescending(f => f)
            .Skip(KeepCount);
        foreach (var f in files)
        {
            try { File.Delete(f); } catch { /* ignora arquivos travados pela nuvem */ }
        }
    }
}
