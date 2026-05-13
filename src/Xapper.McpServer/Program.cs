// Xapper MCP 서버 진입점.
// stdio 전송을 통해 AI 에이전트와 통신하며, WPF 프로세스 인젝션 및 UI 자동화 도구를 제공.
// 환경 변수 XAPPER_INSPECTOR_DLL, XAPPER_GENERIC_INJECTOR_DIR로 경로 오버라이드 가능.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Xapper.Injector;
using Xapper.McpServer;
using Xapper.McpServer.Infrastructure;
using Xapper.McpServer.Tools;

var builder = Host.CreateApplicationBuilder(args);

// MCP SDK의 StdioServerTransport가 stdout을 점유하므로 콘솔 로깅 비활성화
builder.Logging.ClearProviders();

// 경로 해석: 환경 변수 우선, 없으면 임베디드 리소스 추출
var envInspector = Environment.GetEnvironmentVariable("XAPPER_INSPECTOR_DLL");
var envInjectorDir = Environment.GetEnvironmentVariable("XAPPER_GENERIC_INJECTOR_DIR");

string inspectorDllPath;
string genericInjectorDir;

if (envInspector is not null && envInjectorDir is not null)
{
    inspectorDllPath = envInspector;
    genericInjectorDir = envInjectorDir;
}
else
{
    var extractor = new PayloadExtractor();
    extractor.ExtractAll();
    inspectorDllPath = envInspector ?? extractor.InspectorDllPath;
    genericInjectorDir = envInjectorDir ?? extractor.GenericInjectorDir;
}

builder.Services.AddSingleton<SessionManager>();
builder.Services.AddSingleton(new WpfProcessInjector(inspectorDllPath, genericInjectorDir));

builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new()
    {
        Name = "Xapper",
        Version = "0.1.0"
    };
})
.WithStdioServerTransport()
.WithTools<ProcessTools>()
.WithTools<SnapshotTools>()
.WithTools<ActionTools>()
.WithTools<InteractionTools>()
.WithTools<DiagnosticTools>()
.WithTools<CaptureTools>()
.WithTools<FindTools>();

var app = builder.Build();
await app.RunAsync();
