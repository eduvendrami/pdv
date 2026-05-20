namespace PDV.Infrastructure.Services;

public class BackupService
{
    private readonly string _dbPath;
    private readonly string _backupDir;

    public BackupService(string dbPath)
    {
        _dbPath = dbPath;
        _backupDir = Path.Combine(Path.GetDirectoryName(dbPath) ?? ".", "backups");
        Directory.CreateDirectory(_backupDir);
    }

    public void CreateBackup()
    {
        if (!File.Exists(_dbPath)) return;
        var fileName = $"pdv_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
        var dest = Path.Combine(_backupDir, fileName);
        File.Copy(_dbPath, dest, overwrite: true);
        CleanOldBackups();
    }

    private void CleanOldBackups()
    {
        var files = Directory.GetFiles(_backupDir, "*.db")
            .OrderByDescending(f => f)
            .Skip(10);
        foreach (var f in files)
            File.Delete(f);
    }
}
