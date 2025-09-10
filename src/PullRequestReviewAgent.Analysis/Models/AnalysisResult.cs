namespace PullRequestReviewAgent.Analysis.Models;

/// <summary>
/// Represents the result of analyzing a pull request
/// </summary>
public class AnalysisResult
{
    public PullRequestContext Context { get; set; } = new();
    public List<CodeIssue> Issues { get; set; } = new List<CodeIssue>();
    public AnalysisSummary Summary { get; set; } = new();
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    public string AnalysisVersion { get; set; } = "1.0.0";
}

/// <summary>
/// Summary statistics of the analysis
/// </summary>
public class AnalysisSummary
{
    public int TotalIssues { get; set; }
    public int CriticalIssues { get; set; }
    public int ErrorIssues { get; set; }
    public int WarningIssues { get; set; }
    public int InfoIssues { get; set; }
    public int SecurityIssues { get; set; }
    public int QualityIssues { get; set; }
    public int StyleIssues { get; set; }
    public int PerformanceIssues { get; set; }
    public int MaintainabilityIssues { get; set; }
    public int ReliabilityIssues { get; set; }
    public int FilesAnalyzed { get; set; }
    public int LinesAnalyzed { get; set; }
    public double AnalysisDurationMs { get; set; }
}