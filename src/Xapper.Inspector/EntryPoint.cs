using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Xapper.Inspector;

public static class EntryPoint
{
    private static IpcServer? _server;
    private static readonly string LogPath = Path.Combine(Path.GetTempPath(), "xapper_inspector.log");

    private static void Log(string msg)
    {
        try { File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\n"); } catch { }
    }

    public static int Initialize(string args)
    {
        try
        {
            Log($"Initialize called. Args={args}");

            // Register assembly resolver BEFORE referencing any types from dependencies.
            // ExecuteInDefaultAppDomain in .NET Core doesn't resolve deps automatically.
            var thisDir = Path.GetDirectoryName(typeof(EntryPoint).Assembly.Location)!;
            Log($"Assembly dir: {thisDir}");

            AssemblyLoadContext.Default.Resolving += (ctx, name) =>
            {
                var path = Path.Combine(thisDir, name.Name + ".dll");
                Log($"Resolving: {name.Name} -> {path} (exists={File.Exists(path)})");
                if (File.Exists(path))
                    return ctx.LoadFromAssemblyPath(path);
                return null;
            };

            StartServer();
            return 0;
        }
        catch (Exception ex)
        {
            Log($"Initialize FAILED: {ex}");
            return 1;
        }
    }

    // Separate method to ensure Protocol types are not JIT-resolved
    // until after the assembly resolver is registered.
    private static void StartServer()
    {
        var pipeName = Protocol.IpcPipeNames.ForProcess(Environment.ProcessId);
        Log($"Starting server on pipe: {pipeName}");
        _server = new IpcServer(pipeName);
        _ = Task.Run(async () =>
        {
            try
            {
                Log("Server task started");
                await _server.StartListening();
            }
            catch (Exception ex)
            {
                Log($"Server CRASHED: {ex}");
            }
        });
        Log("Server task fired");
    }
}
