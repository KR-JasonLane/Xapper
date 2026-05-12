using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Xapper.Injector;

/// <summary>
/// Win32 P/Invoke를 사용하여 대상 프로세스에 DLL을 주입하는 네이티브 인젝터.
/// Snoop의 GenericInjector DLL을 LoadLibraryW로 로드한 뒤 ExecuteInDefaultAppDomain을 호출하여
/// .NET 어셈블리의 정적 메서드를 타겟 프로세스에서 실행.
/// </summary>
public sealed class NativeInjector
{
    #region Constants

    private const uint PROCESS_ALL_ACCESS = 0x001FFFFF;
    private const uint MEM_COMMIT = 0x1000;
    private const uint MEM_RESERVE = 0x2000;
    private const uint MEM_RELEASE = 0x8000;
    private const uint PAGE_READWRITE = 0x04;
    private const uint INFINITE = 0xFFFFFFFF;

    #endregion

    #region P/Invoke

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out uint lpNumberOfBytesWritten);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out uint lpThreadId);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandleW(string lpModuleName);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetExitCodeThread(IntPtr hThread, out uint lpExitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint dwFreeType);

    #endregion

    #region Fields

    private readonly string _genericInjectorDir;

    #endregion

    #region Constructor

    /// <summary>
    /// <see cref="NativeInjector"/>의 새 인스턴스를 생성합니다.
    /// </summary>
    /// <param name="genericInjectorDir">Snoop GenericInjector DLL이 위치한 디렉토리 경로.</param>
    public NativeInjector(string genericInjectorDir)
    {
        if (string.IsNullOrWhiteSpace(genericInjectorDir))
            throw new ArgumentException("GenericInjector directory path is required.", nameof(genericInjectorDir));

        _genericInjectorDir = genericInjectorDir;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 대상 프로세스에 .NET 어셈블리를 주입하고 지정된 정적 메서드를 실행합니다.
    /// GenericInjector DLL을 로드한 뒤 ExecuteInDefaultAppDomain을 호출하여 타겟 프로세스에서 메서드 실행.
    /// </summary>
    /// <param name="processId">주입 대상 프로세스의 PID.</param>
    /// <param name="assemblyPath">주입할 .NET 어셈블리의 전체 경로.</param>
    /// <param name="typeName">호출할 정적 메서드가 포함된 타입의 전체 이름.</param>
    /// <param name="methodName">호출할 정적 메서드 이름.</param>
    /// <exception cref="InvalidOperationException">인젝션 과정에서 오류가 발생한 경우.</exception>
    /// <exception cref="FileNotFoundException">GenericInjector DLL을 찾을 수 없는 경우.</exception>
    public void Inject(int processId, string assemblyPath, string typeName, string methodName)
    {
        var injectorDllPath = ResolveInjectorDll(processId);

        if (!File.Exists(injectorDllPath))
            throw new FileNotFoundException(
                $"GenericInjector DLL not found: {injectorDllPath}", injectorDllPath);

        var hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, processId);
        if (hProcess == IntPtr.Zero)
            throw new InvalidOperationException(
                $"Failed to open process {processId}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");

        try
        {
            var injectorHandle = LoadDllIntoProcess(hProcess, injectorDllPath);
            CallExecuteInDefaultAppDomain(hProcess, processId, injectorDllPath, injectorHandle, assemblyPath, typeName, methodName);
        }
        finally
        {
            CloseHandle(hProcess);
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 타겟 프로세스의 아키텍처에 맞는 GenericInjector DLL 경로를 결정합니다.
    /// </summary>
    private string ResolveInjectorDll(int processId)
    {
        var process = Process.GetProcessById(processId);
        var arch = GetProcessArchitecture(process);

        var dllName = arch switch
        {
            ProcessArchitecture.X64 => "Snoop.GenericInjector.x64.dll",
            ProcessArchitecture.X86 => "Snoop.GenericInjector.x86.dll",
            ProcessArchitecture.Arm64 => "Snoop.GenericInjector.ARM64.dll",
            _ => throw new InvalidOperationException($"Unsupported process architecture: {arch}")
        };

        return Path.Combine(_genericInjectorDir, dllName);
    }

    /// <summary>
    /// 타겟 프로세스의 CPU 아키텍처를 판별합니다.
    /// </summary>
    private static ProcessArchitecture GetProcessArchitecture(Process process)
    {
        if (RuntimeInformation.OSArchitecture == Architecture.Arm64)
        {
            return IsWow64(process) ? ProcessArchitecture.X86 : ProcessArchitecture.Arm64;
        }

        return IsWow64(process) ? ProcessArchitecture.X86 : ProcessArchitecture.X64;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWow64Process(IntPtr hProcess, [MarshalAs(UnmanagedType.Bool)] out bool wow64Process);

    /// <summary>
    /// 프로세스가 WOW64에서 실행 중인지 확인합니다 (32비트 프로세스 on 64비트 OS).
    /// </summary>
    private static bool IsWow64(Process process)
    {
        try
        {
            if (IsWow64Process(process.Handle, out var isWow64))
                return isWow64;
        }
        catch
        {
            // 접근 거부 시 현재 프로세스와 동일한 아키텍처로 가정
        }
        return false;
    }

    /// <summary>
    /// CreateRemoteThread + LoadLibraryW를 사용하여 타겟 프로세스에 DLL을 로드합니다.
    /// 로드 후 EnumProcessModulesEx로 정확한 64비트 HMODULE을 얻습니다.
    /// </summary>
    /// <returns>로드된 DLL의 모듈 핸들 (HMODULE).</returns>
    private static IntPtr LoadDllIntoProcess(IntPtr hProcess, string dllPath)
    {
        var dllPathBytes = Encoding.Unicode.GetBytes(dllPath + '\0');
        var allocSize = (uint)dllPathBytes.Length;

        var remoteMem = VirtualAllocEx(hProcess, IntPtr.Zero, allocSize, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
        if (remoteMem == IntPtr.Zero)
            throw new InvalidOperationException(
                $"VirtualAllocEx failed: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");

        try
        {
            if (!WriteProcessMemory(hProcess, remoteMem, dllPathBytes, allocSize, out _))
                throw new InvalidOperationException(
                    $"WriteProcessMemory failed: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");

            var kernel32 = GetModuleHandleW("kernel32.dll");
            if (kernel32 == IntPtr.Zero)
                throw new InvalidOperationException("Failed to get kernel32.dll handle");

            var loadLibraryAddr = GetProcAddress(kernel32, "LoadLibraryW");
            if (loadLibraryAddr == IntPtr.Zero)
                throw new InvalidOperationException("Failed to get LoadLibraryW address");

            var hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibraryAddr, remoteMem, 0, out _);
            if (hThread == IntPtr.Zero)
                throw new InvalidOperationException(
                    $"CreateRemoteThread (LoadLibraryW) failed: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");

            try
            {
                WaitForSingleObject(hThread, INFINITE);

                if (!GetExitCodeThread(hThread, out var exitCode))
                    throw new InvalidOperationException("Failed to get LoadLibraryW thread exit code");

                if (exitCode == 0)
                    throw new InvalidOperationException(
                        $"LoadLibraryW failed in target process. DLL path: {dllPath}");
            }
            finally
            {
                CloseHandle(hThread);
            }
        }
        finally
        {
            VirtualFreeEx(hProcess, remoteMem, 0, MEM_RELEASE);
        }

        // GetExitCodeThread는 32비트 DWORD만 반환하므로 64비트 HMODULE이 잘릴 수 있음.
        // EnumProcessModulesEx로 리모트 프로세스의 모듈을 열거하여 정확한 핸들을 얻음.
        return FindModuleInProcess(hProcess, dllPath);
    }

    private const uint LIST_MODULES_ALL = 0x03;

    [DllImport("psapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumProcessModulesEx(IntPtr hProcess, IntPtr[] lphModule, int cb, out int lpcbNeeded, uint dwFilterFlag);

    [DllImport("psapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int GetModuleFileNameExW(IntPtr hProcess, IntPtr hModule, StringBuilder lpFilename, int nSize);

    /// <summary>
    /// 리모트 프로세스에서 지정된 DLL의 모듈 핸들을 검색합니다.
    /// </summary>
    private static IntPtr FindModuleInProcess(IntPtr hProcess, string dllPath)
    {
        var dllFileName = Path.GetFileName(dllPath);
        var modules = new IntPtr[1024];

        if (!EnumProcessModulesEx(hProcess, modules, modules.Length * IntPtr.Size, out var cbNeeded, LIST_MODULES_ALL))
            throw new InvalidOperationException(
                $"EnumProcessModulesEx failed: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");

        var moduleCount = cbNeeded / IntPtr.Size;
        var sb = new StringBuilder(1024);

        for (int i = 0; i < moduleCount; i++)
        {
            sb.Clear();
            if (GetModuleFileNameExW(hProcess, modules[i], sb, sb.Capacity) > 0)
            {
                var moduleName = Path.GetFileName(sb.ToString());
                if (moduleName.Equals(dllFileName, StringComparison.OrdinalIgnoreCase))
                    return modules[i];
            }
        }

        throw new InvalidOperationException(
            $"Module '{dllFileName}' not found in target process after LoadLibraryW succeeded");
    }

    /// <summary>
    /// 대상 프로세스의 로드된 coreclr.dll 경로에서 .NET 런타임 메이저 버전을 감지합니다.
    /// 예: Microsoft.NETCore.App\9.0.16 → "net9.0-windows"
    /// </summary>
    private static string DetectFrameworkVersion(int processId)
    {
        var process = Process.GetProcessById(processId);

        foreach (ProcessModule module in process.Modules)
        {
            if (module.FileName is null)
                continue;

            // coreclr.dll 경로에서 버전 추출: ...\Microsoft.NETCore.App\{major}.{minor}.{patch}\coreclr.dll
            if (!module.ModuleName.Equals("coreclr.dll", StringComparison.OrdinalIgnoreCase))
                continue;

            var dir = Path.GetDirectoryName(module.FileName);
            if (dir is null)
                continue;

            var versionDir = Path.GetFileName(dir);
            if (versionDir is not null && Version.TryParse(versionDir, out _))
                // GenericInjector는 .NET Core 호스팅에 "net6.0-windows" 식별자를 사용.
                // hostfxr 기반 호스팅은 .NET 6~9+ 모두 동일하므로 고정값 사용.
                return "net6.0-windows";
        }

        // coreclr.dll을 찾지 못한 경우 .NET Framework로 가정
        return "net4.0-windows";
    }

    /// <summary>
    /// 이미 로드된 GenericInjector DLL의 ExecuteInDefaultAppDomain 함수를 호출합니다.
    /// 파라미터 문자열을 타겟 프로세스 메모리에 써넣고 CreateRemoteThread로 실행.
    /// </summary>
    private static void CallExecuteInDefaultAppDomain(
        IntPtr hProcess, int processId, string injectorDllPath,
        IntPtr injectorHandle, string assemblyPath, string typeName, string methodName)
    {
        // GenericInjector가 기대하는 파라미터 포맷:
        // "{framework}<|>{assemblyPath}<|>{typeName}<|>{methodName}<|><|>{logFile}"
        var framework = DetectFrameworkVersion(processId);
        var logFile = Path.Combine(Path.GetTempPath(), $"xapper_inject_{processId}.log");
        var paramString = $"{framework}<|>{assemblyPath}<|>{typeName}<|>{methodName}<|><|>{logFile}";
        var paramBytes = Encoding.Unicode.GetBytes(paramString + '\0');
        var allocSize = (uint)paramBytes.Length;

        var remoteMem = VirtualAllocEx(hProcess, IntPtr.Zero, allocSize, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
        if (remoteMem == IntPtr.Zero)
            throw new InvalidOperationException(
                $"VirtualAllocEx for params failed: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");

        try
        {
            if (!WriteProcessMemory(hProcess, remoteMem, paramBytes, allocSize, out _))
                throw new InvalidOperationException(
                    $"WriteProcessMemory for params failed: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");

            // GenericInjector.dll 내부의 ExecuteInDefaultAppDomain export 주소를 구해야 함.
            // 로컬 프로세스에서 GetProcAddress를 쓸 수 없으므로 (리모트 프로세스에 로드됨),
            // 로컬에도 같은 DLL을 로드하여 오프셋을 계산한 뒤 리모트 주소를 산출.
            var funcAddr = GetRemoteExportAddress(hProcess, injectorDllPath, injectorHandle, "ExecuteInDefaultAppDomain");

            var hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, funcAddr, remoteMem, 0, out _);
            if (hThread == IntPtr.Zero)
                throw new InvalidOperationException(
                    $"CreateRemoteThread (ExecuteInDefaultAppDomain) failed: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");

            try
            {
                WaitForSingleObject(hThread, INFINITE);

                if (!GetExitCodeThread(hThread, out var exitCode))
                    throw new InvalidOperationException("Failed to get ExecuteInDefaultAppDomain thread exit code");

                if (exitCode != 0)
                    throw new InvalidOperationException(
                        $"ExecuteInDefaultAppDomain returned error code {exitCode}. Check log: {logFile}");
            }
            finally
            {
                CloseHandle(hThread);
            }
        }
        finally
        {
            VirtualFreeEx(hProcess, remoteMem, 0, MEM_RELEASE);
        }
    }

    /// <summary>
    /// 로컬에서 DLL을 로드하여 export 함수의 오프셋을 계산하고,
    /// 리모트 프로세스에서의 실제 주소를 산출합니다.
    /// </summary>
    private static IntPtr GetRemoteExportAddress(IntPtr hProcess, string dllPath, IntPtr remoteModuleHandle, string exportName)
    {
        // 로컬 프로세스에도 같은 DLL을 로드
        var localModule = LoadLibraryExW(dllPath, IntPtr.Zero, DONT_RESOLVE_DLL_REFERENCES);
        if (localModule == IntPtr.Zero)
            throw new InvalidOperationException(
                $"Failed to load {dllPath} locally: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");

        try
        {
            var localFuncAddr = GetProcAddress(localModule, exportName);
            if (localFuncAddr == IntPtr.Zero)
                throw new InvalidOperationException(
                    $"Export '{exportName}' not found in {Path.GetFileName(dllPath)}");

            // 오프셋 = 로컬 함수 주소 - 로컬 모듈 베이스
            var offset = (long)localFuncAddr - (long)localModule;

            // 리모트 함수 주소 = 리모트 모듈 핸들 + 오프셋
            return new IntPtr((long)remoteModuleHandle + offset);
        }
        finally
        {
            FreeLibrary(localModule);
        }
    }

    private const uint DONT_RESOLVE_DLL_REFERENCES = 0x00000001;

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibraryExW(string lpLibFileName, IntPtr hFile, uint dwFlags);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeLibrary(IntPtr hModule);

    #endregion
}

/// <summary>
/// 프로세스 CPU 아키텍처를 나타내는 열거형.
/// </summary>
public enum ProcessArchitecture
{
    /// <summary>x86 (32비트).</summary>
    X86,

    /// <summary>x64 (64비트).</summary>
    X64,

    /// <summary>ARM64.</summary>
    Arm64
}
