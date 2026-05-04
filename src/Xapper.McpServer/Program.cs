using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using Xapper.Injector;
using Xapper.McpServer;
using Xapper.McpServer.Tools;

var builder = Host.CreateApplicationBuilder(args);

// Resolve paths: check environment variables first, then fall back to well-known locations.
var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var inspectorDllPath = Environment.GetEnvironmentVariable("XAPPER_INSPECTOR_DLL")
    ?? Path.Combine(projectRoot, "src", "Xapper.Inspector", "bin", "Debug", "net9.0-windows", "Xapper.Inspector.dll");
var injectorLauncherPath = Environment.GetEnvironmentVariable("XAPPER_INJECTOR_LAUNCHER")
    ?? Path.Combine(projectRoot, "external", "snoop-bin", "Snoop.InjectorLauncher.x64.exe");

builder.Services.AddSingleton<SessionManager>();
builder.Services.AddSingleton(new WpfProcessInjector(inspectorDllPath, injectorLauncherPath));

builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new()
    {
        Name = "Xapper",
        Version = "0.1.0"
    };
})
.WithStdioServerTransport()
.WithTools<ProcessTools>()
.WithTools<SnapshotTools>()
.WithTools<ActionTools>()
.WithTools<InteractionTools>()
.WithTools<DiagnosticTools>()
.WithTools<CaptureTools>()
.WithTools<FindTools>();

var app = builder.Build();
await app.RunAsync();
