namespace Xapper.Inspector;

public static class EntryPoint
{
    private static IpcServer? _server;

    public static string Initialize(string args)
    {
        var pipeName = Protocol.IpcPipeNames.ForProcess(Environment.ProcessId);
        _server = new IpcServer(pipeName);
        _ = Task.Run(() => _server.StartListening());
        return "OK";
    }
}
