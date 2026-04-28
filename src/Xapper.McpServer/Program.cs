using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using Xapper.Injector;
using Xapper.McpServer;
using Xapper.McpServer.Tools;

var builder = Host.CreateApplicationBuilder(args);

var inspectorDllPath = Path.Combine(AppContext.BaseDirectory, "Xapper.Inspector.dll");
var injectorLauncherPath = Path.Combine(AppContext.BaseDirectory, "external", "Snoop.InjectorLauncher.x64.exe");

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
.WithTools<ActionTools>();

var app = builder.Build();
await app.RunAsync();
