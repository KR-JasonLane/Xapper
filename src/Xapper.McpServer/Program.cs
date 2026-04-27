using Xapper.Injector;
using Xapper.McpServer;
using Xapper.McpServer.Tools;

// Xapper MCP Server - stdio transport
// Provides AI agents with tools to inspect and control WPF applications

var sessionManager = new SessionManager();
var injectorDllPath = Path.Combine(AppContext.BaseDirectory, "Xapper.Inspector.dll");
var injector = new WpfProcessInjector(injectorDllPath);

var processTools = new ProcessTools(sessionManager, injector);
var snapshotTools = new SnapshotTools(sessionManager);
var actionTools = new ActionTools(sessionManager);

// TODO: Integrate with MCP SDK (ModelContextProtocol NuGet package)
// For now, output available tools as a placeholder
Console.Error.WriteLine("Xapper MCP Server started (stdio mode)");
Console.Error.WriteLine("Available tools: xapper_list_processes, xapper_attach, xapper_detach, xapper_snapshot, xapper_click, xapper_type");
Console.Error.WriteLine("Waiting for MCP SDK integration...");

// Keep process alive
await Task.Delay(Timeout.Infinite);
