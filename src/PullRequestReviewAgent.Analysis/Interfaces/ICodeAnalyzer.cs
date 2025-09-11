using PullRequestReviewAgent.Analysis.Models;

namespace PullRequestReviewAgent.Analysis.Interfaces;

/// <summary>
/// Interface for analyzing code for various issues
/// </summary>
public interface ICodeAnalyzer
{
    /// <summary>
    /// Analyzes a single file for issues
    /// </summary>
    /// <param name="fileName">Name of the file being analyzed</param>
    /// <param name="content">Content of the file</param>
    /// <returns>List of issues found</returns>
    Task<List<CodeIssue>> AnalyzeFileAsync(string fileName, string content);

    /// <summary>
    /// Analyzes multiple files for issues
    /// </summary>
    /// <param name="files">Files to analyze</param>
    /// <returns>List of issues found across all files</returns>
    Task<List<CodeIssue>> AnalyzeFilesAsync(Dictionary<string, string> files);

    /// <summary>
    /// Gets the categories of issues this analyzer can detect
    /// </summary>
    IEnumerable<IssueCategory> SupportedCategories { get; }

    /// <summary>
    /// Gets the name of this analyzer
    /// </summary>
    string Name { get; }
}