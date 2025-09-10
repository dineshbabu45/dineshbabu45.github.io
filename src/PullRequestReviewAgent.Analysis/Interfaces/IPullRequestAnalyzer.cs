using PullRequestReviewAgent.Analysis.Models;

namespace PullRequestReviewAgent.Analysis.Interfaces;

/// <summary>
/// Interface for analyzing pull requests
/// </summary>
public interface IPullRequestAnalyzer
{
    /// <summary>
    /// Analyzes a pull request and returns issues found
    /// </summary>
    /// <param name="context">Pull request context</param>
    /// <returns>Analysis result containing issues and summary</returns>
    Task<AnalysisResult> AnalyzePullRequestAsync(PullRequestContext context);

    /// <summary>
    /// Registers a code analyzer to use during analysis
    /// </summary>
    /// <param name="analyzer">Analyzer to register</param>
    void RegisterAnalyzer(ICodeAnalyzer analyzer);

    /// <summary>
    /// Gets all registered analyzers
    /// </summary>
    IEnumerable<ICodeAnalyzer> RegisteredAnalyzers { get; }
}