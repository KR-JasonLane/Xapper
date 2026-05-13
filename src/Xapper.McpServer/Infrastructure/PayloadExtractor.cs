using System.Reflection;

namespace Xapper.McpServer.Infrastructure;

/// <summary>
/// 임베디드 리소스로 내장된 인젝션 페이로드 DLL을 임시 디렉토리에 추출합니다.
/// Inspector DLL과 GenericInjector DLL의 파일 경로를 제공.
/// </summary>
public sealed class PayloadExtractor
{
    #region Constants

    private const string ResourcePrefix = "Xapper.Payload.";

    private static readonly string[] PayloadResources =
    [
        "Xapper.Inspector.dll",
        "Xapper.Protocol.dll",
        "Xapper.Inspector.deps.json",
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

    /// <summary>추출된 Xapper.Inspector.dll의 전체 경로.</summary>
    public string InspectorDllPath => Path.Combine(_extractionDir, "Xapper.Inspector.dll");

    /// <summary>GenericInjector DLL이 추출된 디렉토리 경로.</summary>
    public string GenericInjectorDir => _extractionDir;

    #endregion

    #region Public Methods

    /// <summary>
    /// 모든 페이로드 리소스를 디스크에 추출합니다.
    /// 이미 존재하는 파일은 건너뜁니다.
    /// </summary>
    /// <exception cref="InvalidOperationException">임베디드 리소스를 찾을 수 없는 경우.</exception>
    public void ExtractAll()
    {
        Directory.CreateDirectory(_extractionDir);

        var assembly = typeof(PayloadExtractor).Assembly;

        foreach (var name in PayloadResources)
        {
            var targetPath = Path.Combine(_extractionDir, name);
            if (File.Exists(targetPath))
                continue;

            using var stream = assembly.GetManifestResourceStream(ResourcePrefix + name)
                ?? throw new InvalidOperationException(
                    $"Embedded resource '{ResourcePrefix + name}' not found in assembly.");

            using var fs = File.Create(targetPath);
            stream.CopyTo(fs);
        }
    }

    #endregion
}
