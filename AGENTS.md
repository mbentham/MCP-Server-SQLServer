# Repository Guidelines

## Project Structure & Module Organization
- `SqlAugur/`: main MCP server (`Program.cs`) and production code.
- `SqlAugur/Tools/`: MCP tool endpoints (thin wrappers with `[McpServerTool]`).
- `SqlAugur/Services/`: business logic and SQL access (validation, schema, DBA integrations).
- `SqlAugur/Configuration/`: options, validators, and toolset registry.
- `SqlAugur.Tests/`: unit tests (xUnit v3, no external services).
- `SqlAugur.IntegrationTests/`: container-backed integration tests (SQL Server via Testcontainers/Podman).
- Root docs: `README.md`, `CONTRIBUTING.md`, `SECURITY.md`, `CHANGELOG.md`.

## Build, Test, and Development Commands
- `dotnet build`: build the full solution from repo root (`SqlAugur.slnx`).
- `dotnet run --project SqlAugur`: run the MCP server on stdio.
- `dotnet test SqlAugur.Tests`: run unit tests only.
- `DOCKER_HOST=unix:///run/user/1000/podman/podman.sock TESTCONTAINERS_RYUK_DISABLED=true dotnet test SqlAugur.IntegrationTests`: run integration tests.
- `dotnet publish SqlAugur -c Release -o SqlAugur/publish`: produce release output.
- `dotnet pack SqlAugur -c Release`: build the NuGet global tool package.

## Coding Style & Naming Conventions
- Language: C# on `net10.0` with nullable and implicit usings enabled.
- Indentation: 4 spaces; keep formatting consistent with existing files.
- Naming: `PascalCase` for types/methods/properties; `camelCase` for locals/parameters; interfaces prefixed with `I` (for example `ISqlServerService`).
- Design rule: keep tools thin; put behavior in services.
- Security-sensitive logic (especially query validation) should remain AST-based, not regex-based.

## Testing Guidelines
- Framework: xUnit v3 with `Microsoft.NET.Test.Sdk` and `coverlet.collector`.
- Test file naming: `*Tests.cs` (for example `QueryValidatorTests.cs`).
- Add or update tests for all behavior changes; include bypass/regression cases for validator changes.
- Helpful filters:
  - `dotnet test --filter "DisplayName~TestNameHere"`
  - `dotnet test --filter "FullyQualifiedName~QueryValidatorTests"`

## Commit & Pull Request Guidelines
- Follow existing commit style: concise, imperative summaries (for example `Add maxRows parameter...`, `Fix Dockerfile publish...`).
- Keep commits focused on one concern.
- Before committing, run:
  - `dotnet list package --vulnerable`
  - `dotnet list package --deprecated`
  - `dotnet list package --outdated`
- PRs should include a clear description, test evidence (`dotnet test` output), and links to related issues. For behavioral changes, call out user-visible impact and security implications.
