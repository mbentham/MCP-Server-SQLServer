using System.Globalization;
using System.Text;
using Microsoft.Data.SqlClient;

namespace SqlServerMcp.Services;

internal static class SchemaQueryHelper
{
    internal sealed record TableInfo(string Schema, string Name);

    internal static string FormatDataType(string dataType, int maxLength, byte precision, byte scale)
    {
        return dataType.ToLowerInvariant() switch
        {
            "nvarchar" or "varchar" or "nchar" or "char" or "varbinary" or "binary"
                => maxLength == -1
                    ? $"{dataType}(MAX)"
                    : $"{dataType}({maxLength})",
            "decimal" or "numeric"
                => $"{dataType}({precision},{scale})",
            _ => dataType
        };
    }

    internal static async Task<List<TableInfo>> QueryTablesAsync(SqlConnection connection,
        string? schemaFilter, int maxTables, int commandTimeoutSeconds, CancellationToken cancellationToken)
    {
        var sql = new StringBuilder("""
            SELECT TABLE_SCHEMA, TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE'
              AND TABLE_SCHEMA NOT IN ('sys', 'INFORMATION_SCHEMA')
            """);

        if (schemaFilter is not null)
            sql.Append(" AND TABLE_SCHEMA = @schemaFilter");

        sql.Append(" ORDER BY TABLE_SCHEMA, TABLE_NAME");

        await using var cmd = new SqlCommand(sql.ToString(), connection)
        {
            CommandTimeout = commandTimeoutSeconds
        };

        if (schemaFilter is not null)
            cmd.Parameters.AddWithValue("@schemaFilter", schemaFilter);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var tables = new List<TableInfo>();
        while (await reader.ReadAsync(cancellationToken))
        {
            if (tables.Count >= maxTables)
                break;

            tables.Add(new TableInfo(reader.GetString(0), reader.GetString(1)));
        }

        return tables;
    }

    internal static async Task CreateTableFilterAsync(SqlConnection connection,
        List<TableInfo> tables, string tempTableName, int commandTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        // Create temp table
        await using var createCmd = new SqlCommand(
            $"CREATE TABLE {tempTableName} (SchemaName sysname NOT NULL, TableName sysname NOT NULL);",
            connection)
        {
            CommandTimeout = commandTimeoutSeconds
        };
        await createCmd.ExecuteNonQueryAsync(cancellationToken);

        if (tables.Count == 0)
            return;

        // Use multiple batched INSERT statements to avoid SQL Server's 2100 parameter limit
        // Batch size of 500 means max 1000 parameters per batch (well under the 2100 limit)
        const int batchSize = 500;
        for (var batchStart = 0; batchStart < tables.Count; batchStart += batchSize)
        {
            var batchEnd = Math.Min(batchStart + batchSize, tables.Count);
            var batchCount = batchEnd - batchStart;

            var sb = new StringBuilder();
            sb.Append($"INSERT INTO {tempTableName} (SchemaName, TableName) VALUES ");

            for (var i = 0; i < batchCount; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(CultureInfo.InvariantCulture, $"(@s{i}, @t{i})");
            }

            await using var insertCmd = new SqlCommand(sb.ToString(), connection)
            {
                CommandTimeout = commandTimeoutSeconds
            };

            for (var i = 0; i < batchCount; i++)
            {
                var table = tables[batchStart + i];
                insertCmd.Parameters.AddWithValue($"@s{i}", table.Schema);
                insertCmd.Parameters.AddWithValue($"@t{i}", table.Name);
            }

            await insertCmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
