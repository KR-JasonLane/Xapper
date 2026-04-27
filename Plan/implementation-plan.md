# Xapper 구현 계획

## Context
AI Agent가 Windows WPF 앱을 자동으로 테스트할 수 있는 플랫폼을 처음부터 구축한다.  
Snoop의 DLL Injection 기술을 재사용하되, Playwright 스타일의 ref 기반 요소 식별 + auto-wait + MCP 서버를 직접 설계한다.

**결정 사항:**
- 프로젝트명: **Xapper** (XAML + Apper)
- 대상: .NET 8+ WPF 앱 전용 (Phase 1)
- Transport: stdio
- Injector: Snoop InjectorLauncher를 git submodule로 재사용
- 언어: C# (.NET 8)

---

## 아키텍처

```
AI Agent (Claude)
    │ stdio (JSON-RPC, MCP)
    ▼
Xapper.McpServer ─── MCP 도구 정의, 세션 관리
    │ Named Pipes (xapper_{pid})
    ▼
Xapper.Inspector ─── 대상 프로세스 내부에서 실행
    │ VisualTreeHelper, AutomationPeer, RaiseEvent
    ▼
Target WPF App
```

---

## 솔루션 구조

```
D:\dev\Xapper\
├── Xapper.sln
├── Directory.Build.props
├── .gitmodules
├── external/
│   └── snoopwpf/                    (git submodule)
├── src/
│   ├── Xapper.Protocol/            공유 IPC 메시지 계약
│   ├── Xapper.Injector/            Snoop Injector 래퍼
│   ├── Xapper.Inspector/           주입되는 라이브러리
│   └── Xapper.McpServer/           MCP 서버 (stdio)
└── tests/
    ├── Xapper.TestApp/             테스트용 WPF 앱
    └── Xapper.Tests/               단위/통합 테스트
```

---

## Phase 1: MVP (Attach + Snapshot + Click/Type)

### 1. Xapper.Protocol (공유 계약)

| 파일 | 역할 |
|------|------|
| `IpcMessage.cs` | 메시지 봉투 (Id, Type, Method, Payload) |
| `ElementRef.cs` | ref 모델 |
| `ElementSnapshot.cs` | 트리 노드 (ref, type, name, automationId, text, isEnabled, isVisible, children) |
| `Messages/Requests/*` | PingRequest, SnapshotRequest, ClickRequest, TypeTextRequest |
| `Messages/Responses/*` | SnapshotResponse, ActionResponse, ErrorResponse |
| `IpcPipeNames.cs` | 파이프 이름 규칙: `xapper_{pid}` |

**IPC 프로토콜 설계:**
- Named Pipes, length-prefixed JSON
- 포맷: `[4바이트 little-endian 길이][UTF-8 JSON 페이로드]`
- 요청-응답 직렬화 (한 번에 하나씩, SemaphoreSlim(1,1))

```csharp
public sealed class IpcMessage
{
    public string Id { get; set; }          // Correlation ID (GUID)
    public string Type { get; set; }        // "request" | "response" | "error"
    public string Method { get; set; }      // "snapshot", "click", "type", etc.
    public JsonElement? Payload { get; set; }
}
```

---

### 2. Xapper.Injector (Snoop 래퍼)

| 파일 | 역할 |
|------|------|
| `WpfProcessInjector.cs` | Snoop `Injector.InjectIntoProcess` 호출 래퍼 |

```csharp
public class WpfProcessInjector
{
    public void Inject(int processId, string inspectorDllPath)
    {
        var process = Process.GetProcessById(processId);
        IntPtr hwnd = process.MainWindowHandle;

        var processWrapper = ProcessWrapper.From(processId, hwnd);
        var injectorData = new InjectorData
        {
            FullAssemblyPath = inspectorDllPath,
            ClassName = "Xapper.Inspector.EntryPoint",
            MethodName = "Initialize"
        };

        Injector.InjectIntoProcess(processWrapper, injectorData);
    }
}
```

- Snoop의 x86/x64 GenericInjector DLL을 빌드 출력에 포함
- ProcessWrapper가 대상 아키텍처 자동 감지

---

### 3. Xapper.Inspector (주입 라이브러리)

| 파일 | 역할 |
|------|------|
| `EntryPoint.cs` | CLR 호스팅이 호출하는 진입점 |
| `IpcServer.cs` | NamedPipeServerStream, 메시지 수신/디스패치 |
| `VisualTree/TreeWalker.cs` | VisualTreeHelper 재귀 순회 |
| `VisualTree/RefRegistry.cs` | 스냅샷마다 ref 할당, WeakReference 저장 |
| `Actions/ClickAction.cs` | 하이브리드 클릭 |
| `Actions/TypeAction.cs` | 하이브리드 텍스트 입력 |
| `AutoWait/ElementWaiter.cs` | 요소 준비 상태 대기 |

**EntryPoint (CLR 호스팅 호출):**
```csharp
public static class EntryPoint
{
    public static string Initialize(string args)
    {
        var pipeName = $"xapper_{Environment.ProcessId}";
        var server = new IpcServer(pipeName);
        _ = Task.Run(() => server.StartListening());
        return "OK";
    }
}
```

---

### 4. Xapper.McpServer (MCP 서버)

| 파일 | 역할 |
|------|------|
| `Program.cs` | Host builder, stdio transport 설정 |
| `Tools/ProcessTools.cs` | xapper_list_processes, xapper_attach, xapper_detach |
| `Tools/SnapshotTools.cs` | xapper_snapshot |
| `Tools/ActionTools.cs` | xapper_click, xapper_type |
| `Ipc/InspectorClient.cs` | NamedPipeClientStream, 요청/응답 |
| `SessionManager.cs` | 연결된 프로세스 추적 |

**MCP 도구 정의:**
```csharp
[McpServerTool, Description("List running WPF processes available for attachment")]
public static string XapperListProcesses() { }

[McpServerTool, Description("Attach to a WPF process by injecting the Xapper inspector")]
public static async Task<string> XapperAttach(int pid, int timeout = 10000) { }

[McpServerTool, Description("Get UI visual tree snapshot with element refs")]
public static async Task<string> XapperSnapshot(int? rootRef = null, int maxDepth = 5) { }

[McpServerTool, Description("Click a UI element by ref")]
public static async Task<string> XapperClick(int @ref, int timeout = 5000) { }

[McpServerTool, Description("Type text into an element by ref")]
public static async Task<string> XapperType(int @ref, string text, bool clear = true) { }
```

---

### 5. Xapper.TestApp
- 간단한 WPF 앱: TextBox(username, password), Button(Login), CheckBox(Remember me)
- Phase 1 검증용

---

## Ref 시스템

### 동작 흐름:
1. `xapper_snapshot()` 호출 → Inspector가 Visual Tree 순회
2. 각 요소에 순차 정수 ref 할당 → `Dictionary<int, WeakReference<DependencyObject>>`
3. generation 카운터 증가 (이전 ref 무효화)
4. 컴팩트 텍스트 출력 반환

### 출력 예시:
```
[ref=1] Window "MainWindow" 800x600
  [ref=2] Grid
    [ref=3] StackPanel
      [ref=4] TextBox name="txtUsername" text="" enabled
      [ref=5] TextBox name="txtPassword" text="" enabled
      [ref=6] Button name="btnLogin" content="Log In" enabled
      [ref=7] CheckBox name="chkRemember" content="Remember me" checked=false
```

### Ref 생명주기:
- 스냅샷마다 초기화 및 재할당
- 액션 시 WeakReference alive → 실행, dead → "re-snapshot required" 에러
- generation 불일치 시 경고

---

## 액션 실행 (하이브리드)

```
액션 요청 도착
    │
    ▼
ElementWaiter.WaitForReady(ref)    ← IsVisible + IsEnabled + IsLoaded 대기
    │
    ▼
Dispatcher.Invoke:
    │
    ├─ AutomationPeer 존재? → Pattern 지원? → Pattern.Invoke()  [1순위]
    │
    └─ 없으면 → RaiseEvent (RoutedEvent 직접 발생)            [2순위]
```

### AutomationPeer 패턴 매핑:

| Pattern | 대상 컨트롤 | 동작 |
|---------|------------|------|
| IInvokeProvider | Button, MenuItem | 클릭 |
| IValueProvider | TextBox, ComboBox | 값 설정 |
| IToggleProvider | CheckBox, ToggleButton | 토글 |
| ISelectionItemProvider | ListBoxItem, TabItem | 선택 |
| IExpandCollapseProvider | TreeViewItem, Expander | 펼치기/접기 |
| IScrollProvider | ScrollViewer | 스크롤 |

---

## Auto-Wait 메커니즘

```csharp
public class ElementWaiter
{
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);
    private readonly TimeSpan _pollInterval = TimeSpan.FromMilliseconds(50);

    public async Task<DependencyObject> WaitForReady(int ref, RefRegistry registry)
    {
        var deadline = DateTime.UtcNow + _timeout;
        while (DateTime.UtcNow < deadline)
        {
            var element = registry.Resolve(ref);
            if (element == null) throw new ElementNotFoundException(ref);

            bool ready = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (element is FrameworkElement fe)
                    return fe.IsVisible && fe.IsEnabled && fe.IsLoaded;
                return true;
            });

            if (ready) return element;
            await Task.Delay(_pollInterval);
        }
        throw new TimeoutException($"Element ref={ref} not ready within {_timeout}");
    }
}
```

---

## Phase 2: 전체 도구 + 진단

### 추가 MCP 도구:
| 도구 | 기능 |
|------|------|
| `xapper_select` | ComboBox/ListBox 아이템 선택 |
| `xapper_toggle` | CheckBox/ToggleButton 토글 |
| `xapper_expand` | TreeViewItem/Expander 펼치기/접기 |
| `xapper_scroll` | ScrollViewer 스크롤 |
| `xapper_get_property` | 특정 속성 값 조회 |
| `xapper_get_bindings` | 바인딩 상태/에러 조회 |
| `xapper_screenshot` | RenderTargetBitmap → base64 PNG |
| `xapper_assert` | 속성 값/가시성/활성 상태 검증 |

### 추가 파일:
- `Actions/SelectAction.cs`, `ToggleAction.cs`, `ExpandAction.cs`, `ScrollAction.cs`
- `Diagnostics/BindingInspector.cs` — BindingExpression, HasError, ValidationErrors
- `Diagnostics/PropertyReader.cs` — DependencyProperty 값 조회
- `Capture/ScreenshotCapture.cs` — RenderTargetBitmap + PngBitmapEncoder

---

## Phase 3: 안정성 + 확장

- **트리 필터링:** max_depth, type 필터, 검색 (name/automationId 기반)
- **페이지네이션:** 대형 트리를 청크로 분할 반환
- **다중 윈도우:** xapper_list_windows, xapper_focus_window
- **크래시 감지:** 파이프 R/W 타임아웃 10초, IOException → 세션 정리
- **재연결:** 대상 앱 재시작 시 자동 재 attach 시도

---

## 핵심 기술 과제 & 해결

| 과제 | 해결 |
|------|------|
| Inspector 의존성 로딩 | AssemblyLoadContext 커스텀 리졸버 또는 PublishSingleFile |
| UI 스레드 접근 | 모든 트리/액션 코드를 Dispatcher.InvokeAsync로 래핑 |
| 대형 트리 토큰 초과 | max_depth 기본 5, subtree 스냅샷, 컴팩트 텍스트 |
| 프로세스 크래시 | 파이프 R/W 타임아웃 10초, IOException 캐치 → 세션 정리 |
| x86/x64 매칭 | Snoop ProcessWrapper 자동 감지 → 올바른 네이티브 DLL 선택 |
| 다중 프로세스 연결 | SessionManager가 {pid → InspectorClient} 딕셔너리 관리 |

---

## 검증 방법

### Phase 1 E2E 테스트:
1. `Xapper.TestApp` 빌드 및 실행
2. `Xapper.McpServer` 콘솔 실행
3. stdin으로 MCP 요청 전송:
   - `xapper_list_processes` → TestApp PID 확인
   - `xapper_attach(pid)` → "attached" 응답
   - `xapper_snapshot()` → 트리 + ref 출력
   - `xapper_type(ref, "hello")` → TestApp에 텍스트 표시
   - `xapper_click(ref)` → 버튼 동작 확인
   - `xapper_detach()` → 정상 분리

### Claude Desktop 통합:
```json
{
  "mcpServers": {
    "xapper": {
      "command": "dotnet",
      "args": ["run", "--project", "D:\\dev\\Xapper\\src\\Xapper.McpServer"]
    }
  }
}
```

### 단위 테스트:
- IPC 메시지 직렬화/역직렬화 라운드트립
- RefRegistry: ref 할당, 해석, generation 무효화
- TreeWalker: 모킹된 트리에서 스냅샷 구조 검증

---

## 구현 순서 (Phase 1)

1. 솔루션 구조 생성, Directory.Build.props, git submodule 추가
2. Xapper.Protocol — 메시지 계약, 직렬화 헬퍼
3. Xapper.Inspector — EntryPoint, IpcServer, TreeWalker, RefRegistry
4. Xapper.TestApp — 간단한 WPF 폼
5. Xapper.Injector — Snoop InjectorLauncher 통합, 수동 Injection 테스트
6. Xapper.McpServer — Program.cs, SessionManager, InspectorClient
7. MCP 도구: list_processes, attach, detach, snapshot
8. MCP 도구: click, type
9. E2E 테스트
