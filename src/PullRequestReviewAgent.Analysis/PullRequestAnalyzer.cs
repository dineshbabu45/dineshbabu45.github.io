using PullRequestReviewAgent.Analysis.Interfaces;
using PullRequestReviewAgent.Analysis.Models;
using System.Diagnostics;

namespace PullRequestReviewAgent.Analysis.Analyzers;

/// <summary>
/// Main analyzer that coordinates multiple code analyzers for pull request review
/// </summary>
public class PullRequestAnalyzer : IPullRequestAnalyzer
{
    private readonly List<ICodeAnalyzer> _analyzers = new();

    public IEnumerable<ICodeAnalyzer> RegisteredAnalyzers => _analyzers.AsReadOnly();

    public void RegisterAnalyzer(ICodeAnalyzer analyzer)
    {
        if (analyzer == null)
            throw new ArgumentNullException(nameof(analyzer));

        _analyzers.Add(analyzer);
    }

    public async Task<AnalysisResult> AnalyzePullRequestAsync(PullRequestContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var allIssues = new List<CodeIssue>();

        // Analyze each changed file
        foreach (var changedFile in context.ChangedFiles.Where(f => f.Status != "deleted"))
        {
            if (string.IsNullOrEmpty(changedFile.NewContent))
                continue;

            // Run all analyzers on the file
            foreach (var analyzer in _analyzers)
            {
                try
                {
                    var issues = await analyzer.AnalyzeFileAsync(changedFile.FileName, changedFile.NewContent);
                    allIssues.AddRange(issues);
                }
                catch (Exception ex)
                {
                    // Log analyzer errors but continue with other analyzers
                    allIssues.Add(new CodeIssue
                    {
                        FileName = changedFile.FileName,
                        LineNumber = 1,
                        ColumnNumber = 1,
                        Title = "Analyzer Error",
                        Description = $"Error in {analyzer.Name}: {ex.Message}",
                        Severity = IssueSeverity.Warning,
                        Category = IssueCategory.Quality,
                        RuleId = "ANALYZER_ERROR",
                        CodeSnippet = "",
                        Suggestion = "Check analyzer configuration and file format."
                    });
                }
            }
        }

        stopwatch.Stop();

        // Create summary
        var summary = CreateAnalysisSummary(allIssues, context.ChangedFiles, stopwatch.ElapsedMilliseconds);

        return new AnalysisResult
        {
            Context = context,
            Issues = allIssues,
            Summary = summary,
            AnalyzedAt = DateTime.UtcNow
        };
    }

    private AnalysisSummary CreateAnalysisSummary(List<CodeIssue> issues, List<ChangedFile> changedFiles, double elapsedMs)
    {
        var summary = new AnalysisSummary
        {
            TotalIssues = issues.Count,
            FilesAnalyzed = changedFiles.Count(f => f.Status != "deleted"),
            LinesAnalyzed = changedFiles.Where(f => f.Status != "deleted").Sum(f => f.AddedLines),
            AnalysisDurationMs = elapsedMs
        };

        // Count by severity
        summary.CriticalIssues = issues.Count(i => i.Severity == IssueSeverity.Critical);
        summary.ErrorIssues = issues.Count(i => i.Severity == IssueSeverity.Error);
        summary.WarningIssues = issues.Count(i => i.Severity == IssueSeverity.Warning);
        summary.InfoIssues = issues.Count(i => i.Severity == IssueSeverity.Info);

        // Count by category
        summary.SecurityIssues = issues.Count(i => i.Category == IssueCategory.Security);
        summary.QualityIssues = issues.Count(i => i.Category == IssueCategory.Quality);
        summary.StyleIssues = issues.Count(i => i.Category == IssueCategory.Style);
        summary.PerformanceIssues = issues.Count(i => i.Category == IssueCategory.Performance);
        summary.MaintainabilityIssues = issues.Count(i => i.Category == IssueCategory.Maintainability);
        summary.ReliabilityIssues = issues.Count(i => i.Category == IssueCategory.Reliability);

        return summary;
    }
}