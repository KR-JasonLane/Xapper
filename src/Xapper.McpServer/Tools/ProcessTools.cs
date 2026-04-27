using System.ComponentModel;
using System.Text.Json;
using Xapper.Injector;
using Xapper.Protocol;

namespace Xapper.McpServer.Tools;

public sealed class ProcessTools
{
    private readonly SessionManager _sessionManager;
    private readonly WpfProcessInjector _injector;

    public ProcessTools(SessionManager sessionManager, WpfProcessInjector injector)
    {
        _sessionManager = sessionManager;
        _injector = injector;
    }

    public string ListProcesses()
    {
        var processes = WpfProcessInjector.GetWpfProcesses();

        if (processes.Count == 0)
            return "No WPF processes found.";

        var lines = new List<string> { $"Found {processes.Count} WPF process(es):", "" };
        foreach (var p in processes)
        {
            lines.Add($"  PID={p.ProcessId}  {p.ProcessName}  \"{p.MainWindowTitle}\"");
        }
        return string.Join("\n", lines);
    }

    public async Task<string> AttachAsync(int pid, int timeout = 10000, CancellationToken ct = default)
    {
        // Step 1: Inject inspector into target process
        _injector.Inject(pid);

        // Step 2: Wait for pipe to become available, then connect
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        // Give the inspector a moment to start its pipe server
        await Task.Delay(500, linkedCts.Token);

        var client = await _sessionManager.AttachAsync(pid, linkedCts.Token);

        // Step 3: Verify connection
        var pong = await client.PingAsync(linkedCts.Token);
        return $"Attached to process {pid}. Connection verified.";
    }

    public async Task<string> DetachAsync(int? pid = null, CancellationToken ct = default)
    {
        await _sessionManager.DetachAsync(pid, ct);
        return pid.HasValue
            ? $"Detached from process {pid}."
            : "Detached from active session.";
    }
}
