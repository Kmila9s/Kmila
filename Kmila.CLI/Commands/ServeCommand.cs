using Kmila.CLI.Cli;
using Kmila.CLI.Serve;

namespace Kmila.CLI.Commands;

/// <summary>
/// Implements <c>kmila serve</c> — run Kmila's local <c>/api/v1</c> headless
/// simulation API from the console (projects + files + simulate), so other tools
/// can drive builds/sims over HTTP without the GUI. Runs until Ctrl+C.
/// </summary>
public static class ServeCommand
{
    /// <summary>Starts the API host and blocks until interrupted.</summary>
    public static async Task<int> RunAsync(string[] args)
    {
        var p = new ArgParser(args, "bind-lan", "help", "h");
        if (p.Has("help", "h"))
        {
            Console.WriteLine(HelpText.Serve());
            return 0;
        }

        int port = p.GetInt(8080, "port");
        if (port <= 0 || port > 65535) { Console.Error.WriteLine("kmila serve: --port must be 1..65535."); return 64; }
        bool bindLan = p.Has("bind-lan");
        var token = p.Get("token");

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        var host = new ApiHost(token);
        try
        {
            await host.RunAsync(port, bindLan,
                url =>
                {
                    Console.Error.WriteLine($"[serve] Kmila API listening on {url}");
                    Console.Error.WriteLine($"[serve] health: GET {url}/health   (Ctrl+C to stop)");
                    if (!string.IsNullOrEmpty(token)) Console.Error.WriteLine("[serve] bearer token required");
                },
                cts.Token);
        }
        catch (System.Net.HttpListenerException ex)
        {
            Console.Error.WriteLine($"kmila serve: cannot bind port {port}: {ex.Message}");
            if (bindLan) Console.Error.WriteLine("kmila serve: a LAN bind may need elevated privileges on some systems.");
            return 1;
        }
        catch (OperationCanceledException) { /* Ctrl+C */ }

        Console.Error.WriteLine("[serve] stopped.");
        return 0;
    }
}
