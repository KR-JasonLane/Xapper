using System.Diagnostics;

namespace Xapper.Injector;

/// <summary>
/// Snoop의 InjectorLauncher를 사용하여 대상 WPF 프로세스에 Xapper.Inspector를 주입하는 클래스.
/// InjectorLauncher를 서브프로세스로 실행하여 DLL 인젝션 수행.
/// </summary>
public sealed class WpfProcessInjector
{
    #region Fields

    private readonly string _inspectorDllPath;
    private readonly string _injectorLauncherPath;

    #endregion

    #region Constructor

    /// <summary>
    /// <see cref="WpfProcessInjector"/>의 새 인스턴스를 생성합니다.
    /// </summary>
    /// <param name="inspectorDllPath">주입할 Xapper.Inspector.dll의 경로.</param>
    /// <param name="injectorLauncherPath">Snoop InjectorLauncher 실행 파일 경로.</param>
    public WpfProcessInjector(string inspectorDllPath, string injectorLauncherPath)
    {
        _inspectorDllPath = inspectorDllPath;
        _injectorLauncherPath = injectorLauncherPath;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 대상 프로세스에 Xapper.Inspector.dll을 주입합니다.
    /// InjectorLauncher를 서브프로세스로 실행하여 EntryPoint.Initialize를 호출.
    /// </summary>
    /// <param name="processId">주입 대상 WPF 프로세스의 PID.</param>
    /// <exception cref="InvalidOperationException">프로세스에 메인 윈도우가 없거나 주입에 실패한 경우.</exception>
    /// <exception cref="FileNotFoundException">InjectorLauncher 또는 Inspector DLL을 찾을 수 없는 경우.</exception>
    /// <exception cref="TimeoutException">InjectorLauncher가 30초 내에 완료되지 않은 경우.</exception>
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

    /// <summary>
    /// 지정된 프로세스가 WPF 애플리케이션인지 확인합니다.
    /// PresentationFramework.dll 또는 wpfgfx_cor3.dll 모듈 로드 여부로 판단.
    /// </summary>
    /// <param name="process">확인할 프로세스.</param>
    /// <returns>WPF 프로세스이면 true.</returns>
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
            // 접근 거부 또는 프로세스 종료
        }
        return false;
    }

    /// <summary>
    /// 현재 실행 중인 모든 WPF 프로세스 목록을 반환합니다.
    /// 메인 윈도우가 있는 프로세스만 포함.
    /// </summary>
    /// <returns>WPF 프로세스 정보 목록.</returns>
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
                // 접근 불가 프로세스 건너뜀
            }
        }
        return result;
    }

    #endregion
}

/// <summary>
/// WPF 프로세스의 기본 정보를 담는 데이터 클래스.
/// </summary>
public sealed class WpfProcessInfo
{
    /// <summary>프로세스 ID.</summary>
    public int ProcessId { get; set; }

    /// <summary>프로세스 이름.</summary>
    public required string ProcessName { get; set; }

    /// <summary>메인 윈도우 제목.</summary>
    public string? MainWindowTitle { get; set; }
}
