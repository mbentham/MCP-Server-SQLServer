using SqlServerMcp.Services;
using static SqlServerMcp.Services.DiagramService;
using static SqlServerMcp.Services.SchemaQueryHelper;

namespace SqlServerMcp.Tests;

public class MermaidDiagramHelperTests
{
    // ───────────────────────────────────────────────
    // SanitizeMermaidText
    // ───────────────────────────────────────────────

    [Fact]
    public void SanitizeMermaidText_RemovesNewlines()
    {
        Assert.Equal("hello world", DiagramService.SanitizeMermaidText("hello\r\n world"));
    }

    [Fact]
    public void SanitizeMermaidText_RemovesQuotesAndBraces()
    {
        Assert.Equal("abc", DiagramService.SanitizeMermaidText("\"a{b}c\""));
    }

    [Fact]
    public void SanitizeMermaidText_RemovesPercentAndSemicolon()
    {
        Assert.Equal("abc", DiagramService.SanitizeMermaidText("a%b;c"));
    }

    [Fact]
    public void SanitizeMermaidText_PlainText_Unchanged()
    {
        Assert.Equal("NormalText_123", DiagramService.SanitizeMermaidText("NormalText_123"));
    }

    // ───────────────────────────────────────────────
    // SanitizeMermaidEntity
    // ───────────────────────────────────────────────

    [Fact]
    public void SanitizeMermaidEntity_ReplacesDots()
    {
        Assert.Equal("dbo_Users", DiagramService.SanitizeMermaidEntity("dbo.Users"));
    }

    [Fact]
    public void SanitizeMermaidEntity_ReplacesSpaces()
    {
        Assert.Equal("my_table", DiagramService.SanitizeMermaidEntity("my table"));
    }

    [Fact]
    public void SanitizeMermaidEntity_PlainText_Unchanged()
    {
        Assert.Equal("simple_alias", DiagramService.SanitizeMermaidEntity("simple_alias"));
    }

    // ───────────────────────────────────────────────
    // GenerateEmptyMermaidDiagram
    // ───────────────────────────────────────────────

    [Fact]
    public void GenerateEmptyMermaidDiagram_ContainsErDiagram()
    {
        var result = DiagramService.GenerateEmptyMermaidDiagram("srv", "db", null);
        Assert.Contains("erDiagram", result);
    }

    [Fact]
    public void GenerateEmptyMermaidDiagram_ContainsTitle()
    {
        var result = DiagramService.GenerateEmptyMermaidDiagram("srv", "db", null);
        Assert.Contains("title:", result);
        Assert.Contains("db", result);
        Assert.Contains("srv", result);
    }

    [Fact]
    public void GenerateEmptyMermaidDiagram_ContainsNoTablesComment()
    {
        var result = DiagramService.GenerateEmptyMermaidDiagram("srv", "db", null);
        Assert.Contains("No tables found", result);
    }

    // ───────────────────────────────────────────────
    // BuildMermaid — entities
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildMermaid_SingleTable_RendersEntity()
    {
        var tables = new List<TableInfo> { new("dbo", "Users") };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Users", "Id", "int", 0, 0, 0, false, true, true),
            new("dbo", "Users", "Name", "nvarchar", 100, 0, 0, false, false, false)
        };
        var fks = new List<ForeignKeyInfo>();

        var result = DiagramService.BuildMermaid("srv", "db", null, 10, tables, columns, fks);

        Assert.Contains("erDiagram", result);
        Assert.Contains("Users {", result);
        Assert.Contains("int Id PK", result);
        Assert.Contains("nvarchar_100 Name", result);
    }

    [Fact]
    public void BuildMermaid_NonDboSchema_IncludesSchemaPrefix()
    {
        var tables = new List<TableInfo> { new("sales", "Orders") };
        var columns = new List<ColumnInfo>
        {
            new("sales", "Orders", "Id", "int", 0, 0, 0, false, true, false)
        };
        var fks = new List<ForeignKeyInfo>();

        var result = DiagramService.BuildMermaid("srv", "db", null, 10, tables, columns, fks);

        Assert.Contains("sales__Orders {", result);
    }

    [Fact]
    public void BuildMermaid_DboSchema_OmitsSchemaPrefix()
    {
        var tables = new List<TableInfo> { new("dbo", "Orders") };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Orders", "Id", "int", 0, 0, 0, false, true, false)
        };
        var fks = new List<ForeignKeyInfo>();

        var result = DiagramService.BuildMermaid("srv", "db", null, 10, tables, columns, fks);

        Assert.Contains("    Orders {", result);
        Assert.DoesNotContain("dbo__Orders", result);
    }

    // ───────────────────────────────────────────────
    // BuildMermaid — PK/FK markers
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildMermaid_FKColumn_MarkedFK()
    {
        var tables = new List<TableInfo>
        {
            new("dbo", "Parent"),
            new("dbo", "Child")
        };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Parent", "Id", "int", 0, 0, 0, false, true, false),
            new("dbo", "Child", "Id", "int", 0, 0, 0, false, true, false),
            new("dbo", "Child", "ParentId", "int", 0, 0, 0, false, false, false)
        };
        var fks = new List<ForeignKeyInfo>
        {
            new("FK_Child_Parent", "dbo", "Child", "ParentId", "dbo", "Parent", "Id", false, false)
        };

        var result = DiagramService.BuildMermaid("srv", "db", null, 10, tables, columns, fks);

        Assert.Contains("int ParentId FK", result);
    }

    [Fact]
    public void BuildMermaid_PKAndFK_MarkedBoth()
    {
        var tables = new List<TableInfo>
        {
            new("dbo", "Parent"),
            new("dbo", "Child")
        };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Parent", "Id", "int", 0, 0, 0, false, true, false),
            new("dbo", "Child", "ParentId", "int", 0, 0, 0, false, true, false)
        };
        var fks = new List<ForeignKeyInfo>
        {
            new("FK_Child_Parent", "dbo", "Child", "ParentId", "dbo", "Parent", "Id", false, true)
        };

        var result = DiagramService.BuildMermaid("srv", "db", null, 10, tables, columns, fks);

        Assert.Contains("int ParentId PK,FK", result);
    }

    // ───────────────────────────────────────────────
    // BuildMermaid — cardinality
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildMermaid_FKCardinality_OneToOneMandatory()
    {
        var tables = new List<TableInfo>
        {
            new("dbo", "Parent"),
            new("dbo", "Child")
        };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Parent", "Id", "int", 0, 0, 0, false, true, false),
            new("dbo", "Child", "ParentId", "int", 0, 0, 0, false, false, false)
        };
        var fks = new List<ForeignKeyInfo>
        {
            new("FK_Child_Parent", "dbo", "Child", "ParentId", "dbo", "Parent", "Id",
                IsNullable: false, IsUnique: true)
        };

        var result = DiagramService.BuildMermaid("srv", "db", null, 10, tables, columns, fks);
        Assert.Contains("Parent ||--|| Child", result);
    }

    [Fact]
    public void BuildMermaid_FKCardinality_OneToOneOptional()
    {
        var tables = new List<TableInfo>
        {
            new("dbo", "Parent"),
            new("dbo", "Child")
        };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Parent", "Id", "int", 0, 0, 0, false, true, false),
            new("dbo", "Child", "ParentId", "int", 0, 0, 0, true, false, false)
        };
        var fks = new List<ForeignKeyInfo>
        {
            new("FK_Child_Parent", "dbo", "Child", "ParentId", "dbo", "Parent", "Id",
                IsNullable: true, IsUnique: true)
        };

        var result = DiagramService.BuildMermaid("srv", "db", null, 10, tables, columns, fks);
        Assert.Contains("Parent ||--o| Child", result);
    }

    [Fact]
    public void BuildMermaid_FKCardinality_OneToManyMandatory()
    {
        var tables = new List<TableInfo>
        {
            new("dbo", "Parent"),
            new("dbo", "Child")
        };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Parent", "Id", "int", 0, 0, 0, false, true, false),
            new("dbo", "Child", "ParentId", "int", 0, 0, 0, false, false, false)
        };
        var fks = new List<ForeignKeyInfo>
        {
            new("FK_Child_Parent", "dbo", "Child", "ParentId", "dbo", "Parent", "Id",
                IsNullable: false, IsUnique: false)
        };

        var result = DiagramService.BuildMermaid("srv", "db", null, 10, tables, columns, fks);
        Assert.Contains("Parent ||--|{ Child", result);
    }

    [Fact]
    public void BuildMermaid_FKCardinality_OneToManyOptional()
    {
        var tables = new List<TableInfo>
        {
            new("dbo", "Parent"),
            new("dbo", "Child")
        };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Parent", "Id", "int", 0, 0, 0, false, true, false),
            new("dbo", "Child", "ParentId", "int", 0, 0, 0, true, false, false)
        };
        var fks = new List<ForeignKeyInfo>
        {
            new("FK_Child_Parent", "dbo", "Child", "ParentId", "dbo", "Parent", "Id",
                IsNullable: true, IsUnique: false)
        };

        var result = DiagramService.BuildMermaid("srv", "db", null, 10, tables, columns, fks);
        Assert.Contains("Parent ||--o{ Child", result);
    }

    [Fact]
    public void BuildMermaid_CompositeFKDeduplication()
    {
        var tables = new List<TableInfo>
        {
            new("dbo", "Parent"),
            new("dbo", "Child")
        };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Parent", "Id1", "int", 0, 0, 0, false, true, false),
            new("dbo", "Parent", "Id2", "int", 0, 0, 0, false, true, false),
            new("dbo", "Child", "ParentId1", "int", 0, 0, 0, false, false, false),
            new("dbo", "Child", "ParentId2", "int", 0, 0, 0, false, false, false)
        };
        var fks = new List<ForeignKeyInfo>
        {
            new("FK_Child_Parent", "dbo", "Child", "ParentId1", "dbo", "Parent", "Id1", false, false),
            new("FK_Child_Parent", "dbo", "Child", "ParentId2", "dbo", "Parent", "Id2", false, false)
        };

        var result = DiagramService.BuildMermaid("srv", "db", null, 10, tables, columns, fks);

        var relationshipCount = System.Text.RegularExpressions.Regex.Matches(result, @"Parent \|\|--\|\{ Child").Count;
        Assert.Equal(1, relationshipCount);
    }

    // ───────────────────────────────────────────────
    // BuildMermaid — truncation warning
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildMermaid_TruncationWarning()
    {
        var tables = new List<TableInfo>
        {
            new("dbo", "Table1"),
            new("dbo", "Table2")
        };
        var columns = new List<ColumnInfo>();
        var fks = new List<ForeignKeyInfo>();

        var result = DiagramService.BuildMermaid("srv", "db", null, 2, tables, columns, fks);

        Assert.Contains("WARNING", result);
    }

    // ───────────────────────────────────────────────
    // BuildMermaid — compact mode
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildMermaid_Compact_ShowsOnlyPkAndFkColumns()
    {
        var tables = new List<TableInfo>
        {
            new("dbo", "Parent"),
            new("dbo", "Child")
        };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Parent", "Id", "int", 0, 0, 0, false, true, true),
            new("dbo", "Parent", "Name", "nvarchar", 100, 0, 0, false, false, false),
            new("dbo", "Child", "Id", "int", 0, 0, 0, false, true, true),
            new("dbo", "Child", "ParentId", "int", 0, 0, 0, false, false, false),
            new("dbo", "Child", "Description", "nvarchar", 500, 0, 0, true, false, false)
        };
        var fks = new List<ForeignKeyInfo>
        {
            new("FK_Child_Parent", "dbo", "Child", "ParentId", "dbo", "Parent", "Id", false, false)
        };

        var result = DiagramService.BuildMermaid("srv", "db", null, 10, tables, columns, fks, compact: true);

        // Compact mode uses _ placeholder type (Mermaid requires a type token)
        Assert.Contains("_ Id PK", result);
        Assert.Contains("_ ParentId FK", result);
        // No real data types in compact mode
        Assert.DoesNotContain("int ", result);
        Assert.DoesNotContain("nvarchar", result);
        // Non-key columns omitted
        Assert.DoesNotContain("Description", result);
    }

    [Fact]
    public void BuildMermaid_Compact_PreservesRelationships()
    {
        var tables = new List<TableInfo>
        {
            new("dbo", "Parent"),
            new("dbo", "Child")
        };
        var columns = new List<ColumnInfo>
        {
            new("dbo", "Parent", "Id", "int", 0, 0, 0, false, true, false),
            new("dbo", "Child", "ParentId", "int", 0, 0, 0, false, false, false)
        };
        var fks = new List<ForeignKeyInfo>
        {
            new("FK_Child_Parent", "dbo", "Child", "ParentId", "dbo", "Parent", "Id", false, false)
        };

        var result = DiagramService.BuildMermaid("srv", "db", null, 10, tables, columns, fks, compact: true);

        Assert.Contains("Parent ||--|{ Child : \"FK_Child_Parent\"", result);
    }

    // ───────────────────────────────────────────────
    // BuildMermaid — table with no columns
    // ───────────────────────────────────────────────

    [Fact]
    public void BuildMermaid_TableWithNoColumns_RendersEmptyEntity()
    {
        var tables = new List<TableInfo> { new("dbo", "EmptyTable") };
        var columns = new List<ColumnInfo>();
        var fks = new List<ForeignKeyInfo>();

        var result = DiagramService.BuildMermaid("srv", "db", null, 10, tables, columns, fks);

        Assert.Contains("EmptyTable {", result);
        Assert.Contains("}", result);
    }
}
