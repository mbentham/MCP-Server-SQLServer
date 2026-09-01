using System.Text;
using System.Text.Json;

namespace SqlAugur.Services;

/// <summary>
/// Validates tool-call argument names against the tool's input schema before dispatch.
/// The MCP SDK's own binding rejects wrong or missing argument names with a bare
/// "An error occurred invoking 'tool'." that gives the calling model no hint about what
/// was wrong, so it repeats the same mistake for the rest of the session — observed in
/// practice as "all connecting tools are down" when a model drifted from 'serverName' /
/// 'databaseName' to 'server' / 'database'. This validator produces a corrective message
/// naming the expected arguments so the caller can self-correct on the next call.
/// </summary>
public static class ArgumentNameValidator
{
    /// <summary>
    /// Returns null when the provided argument names satisfy the schema, otherwise a
    /// message describing the unknown/missing names and listing the expected ones.
    /// </summary>
    public static string? Validate(JsonElement inputSchema, IEnumerable<string>? providedNames)
    {
        if (inputSchema.ValueKind != JsonValueKind.Object ||
            !inputSchema.TryGetProperty("properties", out var properties) ||
            properties.ValueKind != JsonValueKind.Object)
            return null;

        var expected = properties.EnumerateObject().Select(p => p.Name).ToList();

        var required = inputSchema.TryGetProperty("required", out var requiredElement) &&
                       requiredElement.ValueKind == JsonValueKind.Array
            ? requiredElement.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString()!)
                .ToList()
            : [];

        var provided = providedNames?.ToList() ?? [];
        var unknown = provided.Except(expected, StringComparer.Ordinal).ToList();
        var missing = required.Except(provided, StringComparer.Ordinal).ToList();

        if (unknown.Count == 0 && missing.Count == 0)
            return null;

        var sb = new StringBuilder();
        foreach (var name in unknown)
        {
            // Covers the observed drift (server -> serverName) and case slips (ServerName).
            var suggestion = expected.FirstOrDefault(e =>
                e.StartsWith(name, StringComparison.OrdinalIgnoreCase));
            sb.Append(suggestion is not null
                ? $"Unknown argument '{name}' - did you mean '{suggestion}'? "
                : $"Unknown argument '{name}'. ");
        }

        if (missing.Count > 0)
            sb.Append($"Missing required argument{(missing.Count > 1 ? "s" : "")}: " +
                      $"{string.Join(", ", missing.Select(m => $"'{m}'"))}. ");

        sb.Append($"Expected arguments: {string.Join(", ", expected)}. Retry with exactly these names.");
        return sb.ToString();
    }
}
