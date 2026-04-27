namespace Xapper.Protocol;

public static class IpcPipeNames
{
    public static string ForProcess(int processId) => $"xapper_{processId}";
}
