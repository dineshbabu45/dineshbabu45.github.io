namespace PullRequestReviewAgent.Analysis.Models;

/// <summary>
/// Represents a code issue found during analysis
/// </summary>
public class CodeIssue
{
    public string FileName { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public int ColumnNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IssueSeverity Severity { get; set; }
    public IssueCategory Category { get; set; }
    public string RuleId { get; set; } = string.Empty;
    public string? Suggestion { get; set; }
    public string CodeSnippet { get; set; } = string.Empty;
}

/// <summary>
/// Severity levels for code issues
/// </summary>
public enum IssueSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2,
    Critical = 3
}

/// <summary>
/// Categories of code issues
/// </summary>
public enum IssueCategory
{
    Quality = 0,
    Security = 1,
    Style = 2,
    Performance = 3,
    Maintainability = 4,
    Reliability = 5
}