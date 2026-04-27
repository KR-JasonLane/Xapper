using System.Diagnostics;

namespace Xapper.Injector;

/// <summary>
/// Wraps the injection mechanism to load Xapper.Inspector into a target WPF process.
/// Currently uses a simplified approach pending Snoop submodule integration.
/// </summary>
public sealed class WpfProcessInjector
{
    private readonly string _inspectorDllPath;

    public WpfProcessInjector(string inspectorDllPath)
    {
        _inspectorDllPath = inspectorDllPath;
    }

    public void Inject(int processId)
    {
        var process = Process.GetProcessById(processId);

        if (process.MainWindowHandle == IntPtr.Zero)
            throw new InvalidOperationException(
                $"Process {processId} ({process.ProcessName}) has no visible main window");

        // TODO: Integrate Snoop InjectorLauncher via submodule
        // For now, this is a placeholder that will be replaced with:
        //   var processWrapper = ProcessWrapper.From(processId, process.MainWindowHandle);
        //   var injectorData = new InjectorData
        //   {
        //       FullAssemblyPath = _inspectorDllPath,
        //       ClassName = "Xapper.Inspector.EntryPoint",
        //       MethodName = "Initialize"
        //   };
        //   Injector.InjectIntoProcess(processWrapper, injectorData);

        throw new NotImplementedException(
            "Snoop submodule integration pending. " +
            "Add snoopwpf as submodule and reference Snoop.InjectorLauncher.");
    }

    public static bool IsWpfProcess(Process process)
    {
        try
        {
            foreach (ProcessModule module in process.Modules)
            {
                if (module.ModuleName.Equals("PresentationFramework.dll", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch (Exception)
        {
            // Access denied or process exited
        }
        return false;
    }

    public static List<WpfProcessInfo> GetWpfProcesses()
    {
        var result = new List<WpfProcessInfo>();
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (process.MainWindowHandle != IntPtr.Zero && IsWpfProcess(process))
                {
                    result.Add(new WpfProcessInfo
                    {
                        ProcessId = process.Id,
                        ProcessName = process.ProcessName,
                        MainWindowTitle = process.MainWindowTitle
                    });
                }
            }
            catch
            {
                // Skip inaccessible processes
            }
        }
        return result;
    }
}

public sealed class WpfProcessInfo
{
    public int ProcessId { get; set; }
    public required string ProcessName { get; set; }
    public string? MainWindowTitle { get; set; }
}
