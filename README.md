# **Xapper**

## **목차**

<b>

- [개요]
- [기술 및 도구]
- [라이브러리]
- [프로젝트 구조]
- [기능 구현]
  - [프로세스 탐색 및 인젝션]
  - [UI 트리 스냅샷]
  - [요소 검색]
  - [UI 자동 조작]
  - [진단 및 검증]
- [사용 방법]
  - [빌드]
  - [MCP 서버 연결 (Claude Desktop)]
  - [MCP 서버 연결 (Claude Code)]
  - [사용 예시]

</b>

## **Xapper 개요**

> **프로젝트 목적 :** AI 에이전트가 실행 중인 WPF 애플리케이션의 UI를 직접 조작하고 검증할 수 있는 MCP 기반 테스트 자동화 플랫폼
>
> **기획 및 제작 :** 이전석
>
> **주요 기능 :** 프로세스 인젝션을 통한 WPF UI 트리 접근, 자연어 기반 UI 테스트 자동화, 실시간 스냅샷/클릭/입력/검증
>
> **개발 환경 :** Windows 11, Visual Studio 2022 Community, .NET 9.0
>
> **문의 :** malbox5034@naver.com

<br/>

## **기술 및 도구**
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![.NET](https://img.shields.io/badge/.NET_9-512BD4?style=for-the-badge&logo=.net&logoColor=white)
![WPF](https://img.shields.io/badge/WPF-0078D4?style=for-the-badge&logo=windows&logoColor=white)
![MCP](https://img.shields.io/badge/MCP-FF6B35?style=for-the-badge&logoColor=white)
![VS](https://img.shields.io/badge/Visual_Studio-5C2D91?style=for-the-badge&logo=visual%20studio&logoColor=white)
![GitHub](https://img.shields.io/badge/GitHub-100000?style=for-the-badge&logo=github&logoColor=white)

<br/>

## **라이브러리**

|라이브러리|버전|비고|
|:---|---:|:---:|
|ModelContextProtocol|1.2.0|MCP 서버 SDK (stdio transport)|
|Microsoft.Extensions.Hosting|10.0.7|호스팅/DI 프레임워크|
|Snoop.InjectorLauncher|6.1.0|WPF 프로세스 인젝션 (pre-built)|
|xUnit|2.9.3|단위 테스트|

<br/>

## **프로젝트 구조**

```
src/
├── Xapper.Protocol        IPC 메시지 계약 (net9.0, WPF 무의존)
├── Xapper.Injector        Snoop 인젝터 래퍼 (서브프로세스 호출)
├── Xapper.Inspector       인젝션 라이브러리 (대상 WPF 앱 내부에서 실행)
└── Xapper.McpServer       MCP 서버 (stdio transport, 15개 도구)
tests/
├── Xapper.TestApp         샘플 WPF 로그인 폼
└── Xapper.Tests           단위 테스트 (xUnit)
external/
└── snoop-bin/             Snoop InjectorLauncher 바이너리
```

<br/>

## **기능 구현**

### **1. 프로세스 탐색 및 인젝션**
- `xapper_list_processes` : 실행 중인 WPF 프로세스 목록 조회
- `xapper_attach` : Snoop 인젝터를 통해 대상 프로세스에 Inspector DLL 주입
- `xapper_detach` : Named Pipe 연결 해제 및 정리
- Named Pipe 기반 IPC (4바이트 LE 길이 접두사 + UTF-8 JSON)

### **2. UI 트리 스냅샷**
- `xapper_snapshot` : Visual Tree를 계층적 텍스트로 반환
- 각 요소에 ref 번호 부여 (이후 조작에 사용)
- maxDepth 파라미터로 탐색 깊이 제어

### **3. 요소 검색**
- `xapper_find` : Name, AutomationId, Type, Text 기반 요소 검색
- 트리 필터링으로 대규모 UI에서도 빠른 탐색

### **4. UI 자동 조작**
- `xapper_click` : 버튼/요소 클릭 (AutomationPeer -> RaiseEvent 폴백)
- `xapper_type` : TextBox에 텍스트 입력
- `xapper_select` : ComboBox/ListBox 항목 선택
- `xapper_toggle` : CheckBox/ToggleButton 토글
- `xapper_expand` : TreeViewItem/Expander 펼치기/접기
- `xapper_scroll` : ScrollViewer 스크롤

### **5. 진단 및 검증**
- `xapper_get_property` : 임의 DependencyProperty 값 읽기
- `xapper_get_bindings` : 데이터 바인딩 상태 및 오류 조회
- `xapper_screenshot` : 윈도우/요소 캡처 (base64 PNG)
- `xapper_assert` : 속성 값 단언 (PASS/FAIL 반환)

<br/>

## **사용 방법**

### **빌드**

```bash
git clone https://github.com/jameslee/Xapper.git
cd Xapper
dotnet build
```

> Windows 전용입니다. WPF 인젝션은 Windows에서만 동작합니다.

### **MCP 서버 연결 (Claude Desktop)**

`%AppData%\Claude\claude_desktop_config.json` 파일에 추가:

```json
{
  "mcpServers": {
    "xapper": {
      "command": "D:\\dev\\Xapper\\src\\Xapper.McpServer\\bin\\Debug\\net9.0-windows\\Xapper.McpServer.exe"
    }
  }
}
```

### **MCP 서버 연결 (Claude Code)**

프로젝트 루트에 `.mcp.json` 파일 생성:

```json
{
  "mcpServers": {
    "xapper": {
      "command": "D:\\dev\\Xapper\\src\\Xapper.McpServer\\bin\\Debug\\net9.0-windows\\Xapper.McpServer.exe"
    }
  }
}
```

### **사용 예시**

MCP 연결 후, AI 에이전트에게 자연어로 요청합니다:

```
"내 WPF 앱에서 로그인 테스트를 해줘.
 UsernameInput에 'admin'을 입력하고 LoginButton을 클릭한 뒤
 StatusText가 'Welcome'을 포함하는지 확인해."
```

AI 에이전트가 자동으로 수행하는 흐름:

```
1. xapper_list_processes       → WPF 프로세스 목록 확인
2. xapper_attach {pid}         → 대상 프로세스에 인젝션
3. xapper_snapshot             → UI 트리 구조 파악
4. xapper_find "UsernameInput" → 입력 필드 ref 획득
5. xapper_type {ref, "admin"}  → 텍스트 입력
6. xapper_click {loginRef}     → 로그인 버튼 클릭
7. xapper_assert {statusRef, "Text", "contains", "Welcome"}
                               → 결과 검증 (PASS/FAIL)
```

### **MCP 도구 전체 목록**

| 도구 | 설명 | 주요 파라미터 |
|------|------|---------------|
| `xapper_list_processes` | WPF 프로세스 목록 | - |
| `xapper_attach` | 프로세스 인젝션 | `pid` |
| `xapper_detach` | 연결 해제 | - |
| `xapper_snapshot` | UI 트리 스냅샷 | `maxDepth` |
| `xapper_find` | 요소 검색 | `name`, `automationId`, `type`, `text` |
| `xapper_click` | 클릭 | `ref` |
| `xapper_type` | 텍스트 입력 | `ref`, `text` |
| `xapper_select` | 항목 선택 | `ref`, `item` |
| `xapper_toggle` | 토글 | `ref` |
| `xapper_expand` | 펼치기/접기 | `ref`, `expand` |
| `xapper_scroll` | 스크롤 | `ref`, `direction`, `amount` |
| `xapper_get_property` | 속성 읽기 | `ref`, `propertyName` |
| `xapper_get_bindings` | 바인딩 조회 | `ref` |
| `xapper_screenshot` | 스크린샷 | `ref` (선택) |
| `xapper_assert` | 값 단언 | `ref`, `propertyName`, `operator`, `expected` |

<br/>

## **동작 원리**

```
┌─────────────────┐     stdio (JSON-RPC)     ┌──────────────────┐
│  AI Agent       │ ◄──────────────────────► │  Xapper.McpServer │
│  (Claude, etc.) │                           └────────┬─────────┘
└─────────────────┘                                    │
                                                       │ Snoop Injector
                                                       ▼
                                              ┌──────────────────┐
                                              │  Target WPF App  │
                                              │  ┌─────────────┐ │
                                              │  │  Inspector  │ │ ← Named Pipe IPC
                                              │  │  (injected) │ │
                                              │  └─────────────┘ │
                                              └──────────────────┘
```

1. AI 에이전트가 MCP 프로토콜로 도구 호출
2. McpServer가 Snoop InjectorLauncher를 통해 Inspector DLL을 대상 프로세스에 주입
3. Inspector가 Named Pipe 서버를 열고 McpServer와 IPC 연결
4. McpServer가 Inspector에 명령 전달 → Inspector가 Dispatcher를 통해 UI 스레드에서 실행
5. 결과를 MCP 응답으로 반환

<br/>
<br/>
