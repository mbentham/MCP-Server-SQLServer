using System.ComponentModel;
using ModelContextProtocol.Server;
using SqlAugur.Services;

namespace SqlAugur.Tools;

[McpServerToolType]
public sealed class QuickieCacheTool
{
    private readonly IDarlingDataService _darlingDataService;
    private readonly IRateLimitingService _rateLimiter;

    public QuickieCacheTool(IDarlingDataService darlingDataService, IRateLimitingService rateLimiter)
    {
        _darlingDataService = darlingDataService;
        _rateLimiter = rateLimiter;
    }

    [McpServerTool(
        Name = "sp_quickie_cache",
        Title = "Plan Cache Analysis (sp_QuickieCache)",
        ReadOnly = true,
        Idempotent = true)]
    [Description("Run sp_QuickieCache to analyze the plan cache for high-impact queries using a Pareto/impact-score approach over the dm_exec_*_stats DMVs. The plan-cache companion to sp_QuickieStore (Query Store). Requires the DarlingData toolkit.")]
    public async Task<string> QuickieCache(
        [Description("Name of the SQL Server to query (use list_servers to see available names)")]
        string serverName,
        [Description("Database to analyze (NULL = all user databases)")]
        string? databaseName = null,
        [Description("Sort results by: cpu (default), duration, reads, writes, memory, spills, executions")]
        string? sortOrder = null,
        [Description("Number of top queries per metric to return (1-100, default 10)")]
        int? top = null,
        [Description("Only include plans created on or after this date (yyyy-MM-dd or yyyy-MM-dd HH:mm:ss)")]
        DateTime? startDate = null,
        [Description("Only include plans created on or before this date (yyyy-MM-dd or yyyy-MM-dd HH:mm:ss)")]
        DateTime? endDate = null,
        [Description("Minimum execution count to include (default 2)")]
        int? minimumExecutionCount = null,
        [Description("Exclude system databases (default true)")]
        bool? ignoreSystemDatabases = null,
        [Description("Minimum impact score (0.00-9.99) required to surface a query (default 0.50)")]
        double? impactThreshold = null,
        [Description("Also surface single-use (one-hit-wonder) plans")]
        bool? findSingleUsePlans = null,
        [Description("Also surface duplicate plans (same query cached as multiple plans)")]
        bool? findDuplicatePlans = null,
        [Description("Include XML query plan columns in output (excluded by default to reduce response size)")]
        bool? includeQueryPlans = null,
        [Description("Return all columns and full-length values with no truncation")]
        bool? verbose = null,
        CancellationToken cancellationToken = default)
    {
        if (top.HasValue)
            top = Math.Clamp(top.Value, 1, 100);

        return await ToolHelper.ExecuteAsync(_rateLimiter, () =>
            _darlingDataService.ExecuteQuickieCacheAsync(
                serverName, databaseName, sortOrder, top, startDate, endDate,
                minimumExecutionCount, ignoreSystemDatabases, impactThreshold,
                findSingleUsePlans, findDuplicatePlans,
                includeQueryPlans, verbose, cancellationToken), cancellationToken);
    }
}
