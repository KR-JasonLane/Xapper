using System.Diagnostics;

namespace Xapper.Injector;

/// <summary>
/// Injects Xapper.Inspector into a target WPF process using Snoop's InjectorLauncher.
/// Calls the InjectorLauncher as a subprocess with appropriate command-line arguments.
/// </summary>
public sealed class WpfProcessInjector
{
    private readonly string _inspectorDllPath;
    private readonly string _injectorLauncherPath;

    public WpfProcessInjector(string inspectorDllPath, string injectorLauncherPath)
    {
        _inspectorDllPath = inspectorDllPath;
        _injectorLauncherPath = injectorLauncherPath;
    }

    public void Inject(int processId)
    {
        var process = Process.GetProcessById(processId);

        if (process.MainWindowHandle == IntPtr.Zero)
            throw new InvalidOperationException(
                $"Process {processId} ({process.ProcessName}) has no visible main window");

        if (!File.Exists(_injectorLauncherPath))
            throw new FileNotFoundException(
                $"Snoop InjectorLauncher not found at: {_injectorLauncherPath}. " +
                "Build Snoop first or provide the correct path.",
                _injectorLauncherPath);

        if (!File.Exists(_inspectorDllPath))
            throw new FileNotFoundException(
                $"Xapper.Inspector.dll not found at: {_inspectorDllPath}",
                _inspectorDllPath);

        var hwnd = process.MainWindowHandle.ToInt64();
        var args = $"-t {processId} -h {hwnd} -a \"{_inspectorDllPath}\" -c \"Xapper.Inspector.EntryPoint\" -m \"Initialize\"";

        var startInfo = new ProcessStartInfo
        {
            FileName = _injectorLauncherPath,
            Arguments = args,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var injectorProcess = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start InjectorLauncher process");

        injectorProcess.WaitForExit(TimeSpan.FromSeconds(30));

        if (!injectorProcess.HasExited)
        {
            injectorProcess.Kill();
            throw new TimeoutException("InjectorLauncher timed out after 30 seconds");
        }

        if (injectorProcess.ExitCode != 0)
        {
            var stderr = injectorProcess.StandardError.ReadToEnd();
            var stdout = injectorProcess.StandardOutput.ReadToEnd();
            throw new InvalidOperationException(
                $"InjectorLauncher failed with exit code {injectorProcess.ExitCode}.\n" +
                $"stdout: {stdout}\nstderr: {stderr}");
        }
    }

    public static bool IsWpfProcess(Process process)
    {
        try
        {
            foreach (ProcessModule module in process.Modules)
            {
                if (module.ModuleName.Equals("PresentationFramework.dll", StringComparison.OrdinalIgnoreCase)
                    || module.ModuleName.Equals("wpfgfx_cor3.dll", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch
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
