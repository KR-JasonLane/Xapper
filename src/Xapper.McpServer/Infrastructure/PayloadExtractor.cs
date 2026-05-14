namespace Xapper.McpServer.Infrastructure;

/// <summary>
/// 임베디드 리소스로 내장된 인젝션 페이로드 DLL을 임시 디렉토리에 추출합니다.
/// TFM별 Inspector DLL과 GenericInjector DLL의 파일 경로를 제공.
/// </summary>
public sealed class PayloadExtractor
{
    #region Constants

    private const string ResourcePrefix = "Xapper.Payload.";

    private static readonly string[] SupportedTfms =
    [
        "net6.0-windows",
        "net7.0-windows",
        "net8.0-windows",
        "net9.0-windows"
    ];

    private static readonly string[] InspectorFiles =
    [
        "Xapper.Inspector.dll",
        "Xapper.Protocol.dll",
        "Xapper.Inspector.deps.json"
    ];

    private static readonly string[] GenericInjectorFiles =
    [
        "Snoop.GenericInjector.x64.dll",
        "Snoop.GenericInjector.x86.dll",
        "Snoop.GenericInjector.ARM64.dll"
    ];

    #endregion

    #region Fields

    private readonly string _extractionDir;

    #endregion

    #region Constructor

    /// <summary>
    /// <see cref="PayloadExtractor"/>의 새 인스턴스를 생성합니다.
    /// 어셈블리 버전 기반의 추출 디렉토리 경로를 결정.
    /// </summary>
    public PayloadExtractor()
    {
        var assembly = typeof(PayloadExtractor).Assembly;
        var version = assembly.GetName().Version?.ToString() ?? "dev";
        _extractionDir = Path.Combine(Path.GetTempPath(), "Xapper", version);
    }

    #endregion

    #region Properties

    /// <summary>TFM별 Inspector DLL이 추출된 기본 디렉토리 경로.</summary>
    public string InspectorBaseDir => _extractionDir;

    /// <summary>GenericInjector DLL이 추출된 디렉토리 경로.</summary>
    public string GenericInjectorDir => _extractionDir;

    #endregion

    #region Public Methods

    /// <summary>
    /// 지정된 TFM에 해당하는 Inspector DLL의 전체 경로를 반환합니다.
    /// </summary>
    /// <param name="tfm">대상 프레임워크 모니커 (예: "net9.0-windows").</param>
    /// <returns>추출된 Xapper.Inspector.dll의 전체 경로.</returns>
    public string GetInspectorDllPath(string tfm)
    {
        return Path.Combine(_extractionDir, tfm, "Xapper.Inspector.dll");
    }

    /// <summary>
    /// 모든 페이로드 리소스를 디스크에 추출합니다.
    /// GenericInjector는 루트 디렉토리에, Inspector는 TFM별 하위 디렉토리에 추출.
    /// 이미 존재하는 파일은 건너뜁니다.
    /// </summary>
    /// <exception cref="InvalidOperationException">임베디드 리소스를 찾을 수 없는 경우.</exception>
    public void ExtractAll()
    {
        Directory.CreateDirectory(_extractionDir);

        var assembly = typeof(PayloadExtractor).Assembly;

        // GenericInjector DLL → 루트 디렉토리
        foreach (var name in GenericInjectorFiles)
            ExtractResource(assembly, name, _extractionDir, name);

        // Inspector DLL → TFM별 하위 디렉토리
        foreach (var tfm in SupportedTfms)
        {
            var tfmDir = Path.Combine(_extractionDir, tfm);
            Directory.CreateDirectory(tfmDir);

            foreach (var name in InspectorFiles)
                ExtractResource(assembly, $"{tfm}.{name}", tfmDir, name);
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 단일 임베디드 리소스를 디스크에 추출합니다.
    /// </summary>
    private static void ExtractResource(System.Reflection.Assembly assembly, string resourceName, string targetDir, string fileName)
    {
        var targetPath = Path.Combine(targetDir, fileName);
        if (File.Exists(targetPath))
            return;

        using var stream = assembly.GetManifestResourceStream(ResourcePrefix + resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{ResourcePrefix + resourceName}' not found in assembly.");

        using var fs = File.Create(targetPath);
        stream.CopyTo(fs);
    }

    #endregion
}
