using System.ComponentModel;
using ModelContextProtocol.Server;
using Xapper.Injector;

namespace Xapper.McpServer.Tools;

/// <summary>
/// WPF 프로세스 목록 조회, 연결(attach), 해제(detach) MCP 도구를 제공하는 클래스.
/// </summary>
[McpServerToolType]
public sealed class ProcessTools
{
    private readonly SessionManager _sessionManager;
    private readonly WpfProcessInjector _injector;

    public ProcessTools(SessionManager sessionManager, WpfProcessInjector injector)
    {
        _sessionManager = sessionManager;
        _injector = injector;
    }

    [McpServerTool(Name = "xapper_list_processes"), Description("List running WPF processes available for attachment")]
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

    [McpServerTool(Name = "xapper_attach"), Description("Attach to a WPF process by injecting the Xapper inspector")]
    public async Task<string> Attach(
        [Description("Process ID of the target WPF application")] int pid,
        [Description("Timeout in ms to wait for connection (default 10000)")] int timeout = 10000,
        CancellationToken ct = default)
    {
        _injector.Inject(pid);

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        // Give the inspector time to start its pipe server
        await Task.Delay(1000, linkedCts.Token);

        var client = await _sessionManager.AttachAsync(pid, linkedCts.Token);
        var pong = await client.PingAsync(linkedCts.Token);

        return $"Attached to process {pid}. Connection verified.";
    }

    [McpServerTool(Name = "xapper_detach"), Description("Detach from the currently attached WPF process")]
    public async Task<string> Detach(
        [Description("Process ID (optional, defaults to active session)")] int? pid = null,
        CancellationToken ct = default)
    {
        await _sessionManager.DetachAsync(pid, ct);
        return pid.HasValue
            ? $"Detached from process {pid}."
            : "Detached from active session.";
    }
}
