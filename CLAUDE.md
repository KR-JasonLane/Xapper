# Xapper

AI-agent-driven testing platform for WPF applications via MCP server and process injection.

## Project Structure

```
src/
  Xapper.Protocol/      - Shared IPC message contracts (net9.0, no WPF dependency)
  Xapper.Injector/      - Snoop Injector wrapper (calls InjectorLauncher as subprocess)
  Xapper.Inspector/     - Injected library (runs inside target WPF app)
  Xapper.McpServer/     - MCP server (stdio transport, ModelContextProtocol 1.2.0)
tests/
  Xapper.TestApp/       - Sample WPF login form for testing
  Xapper.Tests/         - Unit tests (xUnit)
external/
  snoopwpf/             - Git submodule (Snoop WPF)
```

## Build & Run

```bash
dotnet build
dotnet test
dotnet run --project src/Xapper.McpServer
```

## MCP Tools (14 total)

| Tool | Description |
|------|-------------|
| xapper_list_processes | List running WPF processes |
| xapper_attach | Inject inspector into target WPF process |
| xapper_detach | Disconnect from target |
| xapper_snapshot | Get UI tree with element refs |
| xapper_find | Search elements by name/id/type/text |
| xapper_click | Click element by ref |
| xapper_type | Type text into element |
| xapper_select | Select item in ComboBox/ListBox |
| xapper_toggle | Toggle CheckBox/ToggleButton |
| xapper_expand | Expand/collapse TreeViewItem/Expander |
| xapper_scroll | Scroll within ScrollViewer |
| xapper_get_property | Read any property value |
| xapper_get_bindings | Inspect data bindings and errors |
| xapper_screenshot | Capture window/element as base64 PNG |
| xapper_assert | Assert property value (PASS/FAIL) |

## Conventions

- Target: net9.0-windows (Directory.Build.props)
- Language: C# 12
- IPC: Named Pipes with length-prefixed JSON (4-byte LE length + UTF-8 payload)
- All UI thread access must go through Dispatcher.InvokeAsync
- Action execution: AutomationPeer first, RaiseEvent fallback
- Element refs: reassigned on each snapshot, use WeakReference<DependencyObject>

## Maintenance Notes

- .gitignore: Add new ignore patterns as needed when new tooling or artifacts appear during development.
- Snoop submodule: requires C++ build tools for GenericInjector native DLL. Build Snoop separately before E2E testing.
