namespace AIHelpDesk.Application.Dashboard.DTOs;

public sealed class DashboardMetrics
{
    public int TotalTickets { get; set; }
    public int Created { get; set; }
    public int AnalysisInProgress { get; set; }
    public int AnalysisFailed { get; set; }
    public int AwaitingAssignment { get; set; }
    public int Assigned { get; set; }
    public int InProgress { get; set; }
    public int Resolved { get; set; }
    public int Closed { get; set; }
    public int Unassigned { get; set; }
    public IReadOnlyList<CategoryCount> ByCategory { get; set; } = [];
    public IReadOnlyList<PriorityCount> ByPriority { get; set; } = [];
}

public sealed class CategoryCount
{
    public string CategoryName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class PriorityCount
{
    public string PriorityName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class ReportQuery
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Status { get; set; }
    public int? CategoryId { get; set; }
    public int? PriorityId { get; set; }
    public string? AgentId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
