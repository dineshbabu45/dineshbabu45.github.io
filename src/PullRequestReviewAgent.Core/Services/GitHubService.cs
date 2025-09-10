using Octokit;
using PullRequestReviewAgent.Analysis.Models;
using Microsoft.Extensions.Logging;

namespace PullRequestReviewAgent.Core.Services;

/// <summary>
/// Service for interacting with GitHub API
/// </summary>
public class GitHubService
{
    private readonly GitHubClient _client;
    private readonly ILogger<GitHubService> _logger;

    public GitHubService(string token, ILogger<GitHubService> logger)
    {
        _client = new GitHubClient(new ProductHeaderValue("PullRequestReviewAgent"))
        {
            Credentials = new Credentials(token)
        };
        _logger = logger;
    }

    /// <summary>
    /// Gets pull request context including changed files
    /// </summary>
    public async Task<PullRequestContext> GetPullRequestContextAsync(string owner, string repo, int pullRequestNumber)
    {
        try
        {
            _logger.LogInformation("Fetching PR context for {Owner}/{Repo}#{Number}", owner, repo, pullRequestNumber);

            var pr = await _client.PullRequest.Get(owner, repo, pullRequestNumber);
            var files = await _client.PullRequest.Files(owner, repo, pullRequestNumber);

            var context = new PullRequestContext
            {
                RepositoryOwner = owner,
                RepositoryName = repo,
                PullRequestNumber = pullRequestNumber,
                BaseBranch = pr.Base.Ref,
                HeadBranch = pr.Head.Ref,
                Title = pr.Title,
                Description = pr.Body ?? string.Empty
            };

            foreach (var file in files)
            {
                var changedFile = new ChangedFile
                {
                    FileName = file.FileName,
                    Status = file.Status.ToString().ToLower(),
                    AddedLines = file.Additions,
                    DeletedLines = file.Deletions
                };

                // Get file content for analysis
                if (file.Status != "deleted")
                {
                    try
                    {
                        var fileContent = await _client.Repository.Content.GetAllContentsByRef(
                            owner, repo, file.FileName, pr.Head.Sha);
                        
                        if (fileContent.Any())
                        {
                            changedFile.NewContent = fileContent.First().Content;
                        }
                    }
                    catch (NotFoundException)
                    {
                        _logger.LogWarning("Could not fetch content for file: {FileName}", file.FileName);
                    }
                }

                // Parse line changes from patch
                if (!string.IsNullOrEmpty(file.Patch))
                {
                    changedFile.Changes = ParsePatch(file.Patch);
                }

                context.ChangedFiles.Add(changedFile);
            }

            _logger.LogInformation("Successfully fetched PR context with {FileCount} files", context.ChangedFiles.Count);
            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching PR context for {Owner}/{Repo}#{Number}", owner, repo, pullRequestNumber);
            throw;
        }
    }

    /// <summary>
    /// Posts a review comment on the pull request
    /// </summary>
    public async Task PostReviewCommentAsync(string owner, string repo, int pullRequestNumber, AnalysisResult analysisResult)
    {
        try
        {
            var reviewBody = FormatAnalysisResult(analysisResult);
            
            var review = new PullRequestReviewCreate
            {
                Body = reviewBody,
                Event = PullRequestReviewEvent.Comment
            };

            await _client.PullRequest.Review.Create(owner, repo, pullRequestNumber, review);
            _logger.LogInformation("Posted review comment for PR {Owner}/{Repo}#{Number}", owner, repo, pullRequestNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting review comment for {Owner}/{Repo}#{Number}", owner, repo, pullRequestNumber);
            throw;
        }
    }

    /// <summary>
    /// Posts individual comments on specific lines with issues
    /// </summary>
    public async Task PostLineCommentsAsync(string owner, string repo, int pullRequestNumber, AnalysisResult analysisResult)
    {
        try
        {
            foreach (var issue in analysisResult.Issues.Where(i => i.Severity >= IssueSeverity.Warning))
            {
                var comment = new PullRequestReviewCommentCreate(
                    FormatIssueComment(issue),
                    issue.FileName,
                    "HEAD",
                    issue.LineNumber);

                try
                {
                    await _client.PullRequest.ReviewComment.Create(owner, repo, pullRequestNumber, comment);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not post comment for issue {RuleId} on line {Line} in {File}", 
                        issue.RuleId, issue.LineNumber, issue.FileName);
                }
            }

            _logger.LogInformation("Posted line comments for PR {Owner}/{Repo}#{Number}", owner, repo, pullRequestNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting line comments for {Owner}/{Repo}#{Number}", owner, repo, pullRequestNumber);
            throw;
        }
    }

    private List<LineChange> ParsePatch(string patch)
    {
        var changes = new List<LineChange>();
        var lines = patch.Split('\n');
        var currentLineNumber = 0;

        foreach (var line in lines)
        {
            if (line.StartsWith("@@"))
            {
                // Parse line numbers from hunk header
                var match = System.Text.RegularExpressions.Regex.Match(line, @"\+(\d+)");
                if (match.Success)
                {
                    currentLineNumber = int.Parse(match.Groups[1].Value);
                }
            }
            else if (line.StartsWith("+") && !line.StartsWith("+++"))
            {
                changes.Add(new LineChange
                {
                    LineNumber = currentLineNumber,
                    ChangeType = "added",
                    Content = line.Substring(1)
                });
                currentLineNumber++;
            }
            else if (line.StartsWith("-") && !line.StartsWith("---"))
            {
                changes.Add(new LineChange
                {
                    LineNumber = currentLineNumber,
                    ChangeType = "removed",
                    Content = line.Substring(1)
                });
            }
            else if (line.StartsWith(" "))
            {
                currentLineNumber++;
            }
        }

        return changes;
    }

    private string FormatAnalysisResult(AnalysisResult result)
    {
        var summary = result.Summary;
        var markdown = $@"## 🤖 AI Code Review Results

### Summary
- **Total Issues**: {summary.TotalIssues}
- **Files Analyzed**: {summary.FilesAnalyzed}
- **Lines Analyzed**: {summary.LinesAnalyzed}
- **Analysis Duration**: {summary.AnalysisDurationMs:F0}ms

### Issues by Severity
- 🔴 **Critical**: {summary.CriticalIssues}
- 🟠 **Error**: {summary.ErrorIssues}
- 🟡 **Warning**: {summary.WarningIssues}
- 🔵 **Info**: {summary.InfoIssues}

### Issues by Category
- 🔒 **Security**: {summary.SecurityIssues}
- ⚡ **Quality**: {summary.QualityIssues}
- 🎨 **Style**: {summary.StyleIssues}
- 🏃 **Performance**: {summary.PerformanceIssues}
- 🔧 **Maintainability**: {summary.MaintainabilityIssues}
- 🛡️ **Reliability**: {summary.ReliabilityIssues}

";

        if (result.Issues.Any(i => i.Severity >= IssueSeverity.Warning))
        {
            markdown += "\n### Notable Issues\n";
            
            var criticalAndErrors = result.Issues
                .Where(i => i.Severity >= IssueSeverity.Error)
                .Take(5)
                .ToList();

            foreach (var issue in criticalAndErrors)
            {
                var emoji = issue.Severity == IssueSeverity.Critical ? "🔴" : "🟠";
                markdown += $"- {emoji} **{issue.Title}** in `{issue.FileName}:{issue.LineNumber}`\n";
                markdown += $"  - {issue.Description}\n";
                if (!string.IsNullOrEmpty(issue.Suggestion))
                {
                    markdown += $"  - 💡 *{issue.Suggestion}*\n";
                }
                markdown += "\n";
            }
        }
        else
        {
            markdown += "\n✅ **Great work! No critical issues found.**\n";
        }

        markdown += $"\n*Analysis performed by AI Pull Request Review Agent v{result.AnalysisVersion}*";

        return markdown;
    }

    private string FormatIssueComment(CodeIssue issue)
    {
        var emoji = issue.Severity switch
        {
            IssueSeverity.Critical => "🔴",
            IssueSeverity.Error => "🟠",
            IssueSeverity.Warning => "🟡",
            _ => "🔵"
        };

        var categoryEmoji = issue.Category switch
        {
            IssueCategory.Security => "🔒",
            IssueCategory.Quality => "⚡",
            IssueCategory.Style => "🎨",
            IssueCategory.Performance => "🏃",
            IssueCategory.Maintainability => "🔧",
            IssueCategory.Reliability => "🛡️",
            _ => "📝"
        };

        var comment = $"{emoji} {categoryEmoji} **{issue.Title}**\n\n";
        comment += $"{issue.Description}\n\n";
        
        if (!string.IsNullOrEmpty(issue.Suggestion))
        {
            comment += $"💡 **Suggestion**: {issue.Suggestion}\n\n";
        }

        comment += $"*Rule: `{issue.RuleId}` | Severity: {issue.Severity} | Category: {issue.Category}*";

        return comment;
    }
}