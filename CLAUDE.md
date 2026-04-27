# Xapper

AI-agent-driven testing platform for WPF applications via MCP server and process injection.

## Project Structure

```
src/
  Xapper.Protocol/      - Shared IPC message contracts
  Xapper.Injector/      - Snoop Injector wrapper
  Xapper.Inspector/     - Injected library (runs inside target WPF app)
  Xapper.McpServer/     - MCP server (stdio transport)
tests/
  Xapper.TestApp/       - Sample WPF app for testing
  Xapper.Tests/         - Unit/integration tests
external/
  snoopwpf/             - Git submodule (Snoop WPF)
```

## Build & Run

```bash
dotnet build
dotnet run --project src/Xapper.McpServer
```

## Conventions

- Target: .NET 8.0-windows
- Language: C# 12
- IPC: Named Pipes with length-prefixed JSON
- All UI thread access must go through Dispatcher.InvokeAsync

## Maintenance Notes

- .gitignore: Add new ignore patterns as needed when new tooling or artifacts appear during development.
