using ModelContextProtocol;
using SqlAugur.Services;
using SqlAugur.Tools;

namespace SqlAugur.Tests;

public class GetMermaidDiagramToolTests
{
    private readonly StubDiagramService _stub = new();
    private readonly GetMermaidDiagramTool _tool;

    public GetMermaidDiagramToolTests()
    {
        _tool = new GetMermaidDiagramTool(_stub, new NoOpRateLimiter());
    }

    [Fact]
    public async Task GetDiagram_ValidMmdExtension_SavesFile()
    {
        _stub.MermaidResult = "erDiagram\n    Users {\n    }\n";
        var dir = Path.Combine(Path.GetTempPath(), $"mmd_{Guid.NewGuid():N}");
        try
        {
            var path = Path.Combine(dir, "test.mmd");
            var result = await _tool.GetDiagram("srv", "db", path, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Contains("Mermaid diagram saved to", result);
            Assert.True(File.Exists(path));
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task GetDiagram_WrongExtension_ThrowsMcpException()
    {
        _stub.MermaidResult = "erDiagram";
        var path = Path.Combine(Path.GetTempPath(), "test.puml");

        var ex = await Assert.ThrowsAsync<McpException>(
            () => _tool.GetDiagram("srv", "db", path, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains(".mmd", ex.Message);
    }

    [Fact]
    public async Task GetDiagram_ArgumentException_WrappedAsMcpException()
    {
        _stub.ExceptionToThrow = new ArgumentException("Server 'bad' not found.");

        var ex = await Assert.ThrowsAsync<McpException>(
            () => _tool.GetDiagram("bad", "db", "/tmp/test.mmd", cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("Server 'bad' not found", ex.Message);
    }

    [Fact]
    public async Task GetDiagram_ClampsMaxTables()
    {
        _stub.MermaidResult = "erDiagram\n";
        var dir = Path.Combine(Path.GetTempPath(), $"mmd_{Guid.NewGuid():N}");
        try
        {
            var path = Path.Combine(dir, "test.mmd");
            // maxTables > 200 should be clamped to 200
            await _tool.GetDiagram("srv", "db", path, maxTables: 500, cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal(200, _stub.CapturedMaxTables);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    private sealed class StubDiagramService : IDiagramService
    {
        public string PlantUmlResult { get; set; } = "@startuml\n@enduml";
        public string MermaidResult { get; set; } = "erDiagram";
        public Exception? ExceptionToThrow { get; set; }
        public int CapturedMaxTables { get; private set; }

        public Task<string> GenerateDiagramAsync(string serverName, string databaseName,
            IReadOnlyList<string>? includeSchemas, IReadOnlyList<string>? excludeSchemas,
            IReadOnlyList<string>? includeTables, IReadOnlyList<string>? excludeTables,
            int maxTables, CancellationToken cancellationToken, bool compact = false)
        {
            if (ExceptionToThrow is not null) throw ExceptionToThrow;
            CapturedMaxTables = maxTables;
            return Task.FromResult(PlantUmlResult);
        }

        public Task<string> GenerateMermaidDiagramAsync(string serverName, string databaseName,
            IReadOnlyList<string>? includeSchemas, IReadOnlyList<string>? excludeSchemas,
            IReadOnlyList<string>? includeTables, IReadOnlyList<string>? excludeTables,
            int maxTables, CancellationToken cancellationToken, bool compact = false)
        {
            if (ExceptionToThrow is not null) throw ExceptionToThrow;
            CapturedMaxTables = maxTables;
            return Task.FromResult(MermaidResult);
        }
    }
}
