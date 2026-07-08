using System.Diagnostics;
using System.Net.Sockets;
using Microsoft.Data.SqlClient;

namespace HomeServicesPortal.Infrastructure;

/// <summary>
/// Ensures the SSH SQL tunnel is running and healthy before EF connects in local Development.
/// </summary>
public static class DevSqlTunnelBootstrap
{
    public static void EnsureStarted(IConfiguration configuration, IWebHostEnvironment environment, ILogger logger)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        if (!configuration.GetValue("DevSqlTunnel:Enabled", true))
        {
            return;
        }

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var localPort = configuration.GetValue("DevSqlTunnel:LocalPort", 11433);
        if (!UsesLocalTunnel(connectionString, localPort))
        {
            return;
        }

        if (CanConnectToSql(connectionString))
        {
            logger.LogDebug("SQL dev tunnel is healthy on port {Port}.", localPort);
            return;
        }

        var scriptPath = ResolveEnsureScriptPath(environment.ContentRootPath);
        if (scriptPath is null)
        {
            logger.LogWarning(
                "ensure-sql-tunnel.ps1 not found. Start the tunnel manually: .\\scripts\\dev-sql-tunnel.ps1");
            return;
        }

        var portWasOpen = IsPortListening(localPort);
        if (portWasOpen)
        {
            logger.LogWarning(
                "Port {Port} is open but SQL is unreachable (stale tunnel). Restarting SSH tunnel...",
                localPort);
        }
        else
        {
            logger.LogInformation("Starting SQL dev tunnel (localhost:{Port})...", localPort);
        }

        RunEnsureScript(scriptPath, forceRestart: portWasOpen);

        var deadline = DateTime.UtcNow.AddSeconds(25);
        while (DateTime.UtcNow < deadline)
        {
            if (CanConnectToSql(connectionString))
            {
                logger.LogInformation("SQL dev tunnel is ready on port {Port}.", localPort);
                return;
            }

            Thread.Sleep(500);
        }

        logger.LogWarning(
            "Could not verify SQL connectivity through the dev tunnel. Run: .\\scripts\\dev-sql-tunnel.ps1");
    }

    private static void RunEnsureScript(string scriptPath, bool forceRestart)
    {
        var args = forceRestart
            ? $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -ForceRestart"
            : $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"";

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(scriptPath)!
        });

        process?.WaitForExit(TimeSpan.FromSeconds(30));
    }

    private static bool CanConnectToSql(string connectionString)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                ConnectTimeout = 5
            };

            using var connection = new SqlConnection(builder.ConnectionString);
            connection.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool UsesLocalTunnel(string connectionString, int localPort)
    {
        var portToken = $",{localPort}";
        return connectionString.Contains($"127.0.0.1{portToken}", StringComparison.OrdinalIgnoreCase)
               || connectionString.Contains($"localhost{portToken}", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPortListening(int port)
    {
        try
        {
            using var client = new TcpClient();
            var task = client.ConnectAsync("127.0.0.1", port);
            return task.Wait(TimeSpan.FromSeconds(2)) && client.Connected;
        }
        catch
        {
            return false;
        }
    }

    private static string? ResolveEnsureScriptPath(string contentRootPath)
    {
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(contentRootPath, "..", "scripts", "ensure-sql-tunnel.ps1")),
            Path.GetFullPath(Path.Combine(contentRootPath, "scripts", "ensure-sql-tunnel.ps1"))
        };

        return candidates.FirstOrDefault(File.Exists);
    }
}
