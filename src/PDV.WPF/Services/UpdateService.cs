using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
// System.Windows not imported here to avoid ambiguity with PDV.Application namespace

namespace PDV.WPF.Services;

public class UpdateService
{
    // ╔════════════════════════════════════════════════════════════╗
    // ║  DADOS DO REPOSITÓRIO NO GITHUB                           ║
    // ╚════════════════════════════════════════════════════════════╝
    private const string GitHubOwner = "eduvendrami";   // username do GitHub (não o e-mail)
    private const string GitHubRepo  = "pdv";

    // Padrão do arquivo na release: PDV_v1.0.1.exe, PDV_v2.0.0.exe, etc.
    private const string AssetPrefix = "PDV_v";
    private const string AssetSuffix = ".exe";

    private static readonly string ApiUrl =
        $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";

    public static Version CurrentVersion =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);

    /// <summary>
    /// Consulta o GitHub e retorna informações sobre a nova versão,
    /// ou null se já estiver na versão mais recente (ou sem conexão).
    /// </summary>
    public async Task<ReleaseInfo?> CheckForUpdateAsync()
    {
        using var client = CreateClient();

        string json;
        try
        {
            json = await client.GetStringAsync(ApiUrl);
        }
        catch
        {
            return null; // sem internet ou repo não encontrado — silencioso
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var tagName = root.GetProperty("tag_name").GetString()?.TrimStart('v');
        if (!Version.TryParse(tagName, out var latestVersion))
            return null;

        if (latestVersion <= CurrentVersion)
            return null;

        // Procura asset pelo padrão PDV_v{versão}.exe
        string? downloadUrl = null;
        if (root.TryGetProperty("assets", out var assets))
        {
            foreach (var asset in assets.EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString() ?? string.Empty;
                if (name.StartsWith(AssetPrefix, StringComparison.OrdinalIgnoreCase)
                    && name.EndsWith(AssetSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    downloadUrl = asset.GetProperty("browser_download_url").GetString();
                    break;
                }
            }
        }

        if (downloadUrl == null)
            return null;

        var notes = root.TryGetProperty("body", out var body) ? body.GetString() : null;
        return new ReleaseInfo(latestVersion, downloadUrl, notes?.Trim() ?? string.Empty);
    }

    public async Task DownloadAsync(
        string             downloadUrl,
        string             destPath,
        IProgress<double>? progress = null,
        CancellationToken  ct       = default)
    {
        using var client = CreateClient();

        using var response = await client.GetAsync(
            downloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;

        await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        await using var fileStream    = new FileStream(
            destPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

        var buffer     = new byte[81920];
        long totalRead = 0;
        int  bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
            totalRead += bytesRead;
            if (totalBytes > 0)
                progress?.Report((double)totalRead / totalBytes * 100.0);
        }
    }

    public static void ApplyAndRestart(string newExePath)
    {
        var currentExe = Process.GetCurrentProcess().MainModule!.FileName;
        var batPath    = Path.Combine(Path.GetTempPath(), "pdv_update.bat");

        var bat = new StringBuilder();
        bat.AppendLine("@echo off");
        bat.AppendLine("timeout /t 2 /nobreak > nul");
        bat.AppendLine($"copy /y \"{newExePath}\" \"{currentExe}\"");
        bat.AppendLine("if errorlevel 1 (");
        bat.AppendLine("    echo Falha ao aplicar a atualizacao.");
        bat.AppendLine("    pause");
        bat.AppendLine("    exit /b 1");
        bat.AppendLine(")");
        bat.AppendLine($"start \"\" \"{currentExe}\"");
        bat.AppendLine("del \"%~0\"");

        File.WriteAllText(batPath, bat.ToString(), Encoding.Default);

        Process.Start(new ProcessStartInfo
        {
            FileName        = batPath,
            UseShellExecute = true,
            WindowStyle     = ProcessWindowStyle.Hidden
        });

        System.Windows.Application.Current.Dispatcher.Invoke(
            () => System.Windows.Application.Current.Shutdown());
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", $"PDV-Updater/{CurrentVersion}");
        client.Timeout = TimeSpan.FromSeconds(30);
        return client;
    }
}

public record ReleaseInfo(Version Version, string DownloadUrl, string Notes);
