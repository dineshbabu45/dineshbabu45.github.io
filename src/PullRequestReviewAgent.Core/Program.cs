using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PullRequestReviewAgent.Analysis.Analyzers;
using PullRequestReviewAgent.Analysis.Interfaces;
using PullRequestReviewAgent.Analysis.Models;
using PullRequestReviewAgent.Core.Services;

namespace PullRequestReviewAgent.Core;

class Program
{
    static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        
        try
        {
            await RunAnalysis(host.Services, args);
        }
        catch (Exception ex)
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Application failed");
            Environment.Exit(1);
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true);
                config.AddEnvironmentVariables();
                config.AddCommandLine(args);
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddLogging();
                services.AddSingleton<IPullRequestAnalyzer, PullRequestAnalyzer>();
                services.AddTransient<ICodeAnalyzer, SecurityAnalyzer>();
                services.AddTransient<ICodeAnalyzer, QualityAnalyzer>();
                services.AddTransient<ICodeAnalyzer, StyleAnalyzer>();
            });

    static async Task RunAnalysis(IServiceProvider services, string[] args)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        // Parse command line arguments
        if (args.Length < 3)
        {
            logger.LogError("Usage: PullRequestReviewAgent <owner> <repo> <pr-number> [--post-comments]");
            logger.LogInformation("Example: PullRequestReviewAgent microsoft dotnet 12345 --post-comments");
            logger.LogInformation("Environment variables:");
            logger.LogInformation("  GITHUB_TOKEN: GitHub personal access token");
            return;
        }

        var owner = args[0];
        var repo = args[1];
        if (!int.TryParse(args[2], out var prNumber))
        {
            logger.LogError("Pull request number must be a valid integer");
            return;
        }

        var postComments = args.Contains("--post-comments");
        var githubToken = configuration["GITHUB_TOKEN"] ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");

        if (string.IsNullOrEmpty(githubToken))
        {
            logger.LogError("GitHub token is required. Set GITHUB_TOKEN environment variable or configuration.");
            return;
        }

        logger.LogInformation("Starting PR analysis for {Owner}/{Repo}#{Number}", owner, repo, prNumber);

        // Set up services
        var githubService = new GitHubService(githubToken, services.GetRequiredService<ILogger<GitHubService>>());
        var prAnalyzer = services.GetRequiredService<IPullRequestAnalyzer>();

        // Register all analyzers
        var analyzers = services.GetServices<ICodeAnalyzer>();
        foreach (var analyzer in analyzers)
        {
            prAnalyzer.RegisterAnalyzer(analyzer);
            logger.LogInformation("Registered analyzer: {AnalyzerName}", analyzer.Name);
        }

        try
        {
            // Get PR context
            var context = await githubService.GetPullRequestContextAsync(owner, repo, prNumber);
            logger.LogInformation("Retrieved PR context: {Title}", context.Title);

            // Analyze PR
            var result = await prAnalyzer.AnalyzePullRequestAsync(context);
            logger.LogInformation("Analysis completed. Found {IssueCount} issues", result.Issues.Count);

            // Output results
            OutputResults(result, logger);

            // Post comments if requested
            if (postComments)
            {
                logger.LogInformation("Posting review comments...");
                await githubService.PostReviewCommentAsync(owner, repo, prNumber, result);
                
                if (result.Issues.Any(i => i.Severity >= IssueSeverity.Warning))
                {
                    await githubService.PostLineCommentsAsync(owner, repo, prNumber, result);
                }
                
                logger.LogInformation("Review comments posted successfully");
            }
            else
            {
                logger.LogInformation("Use --post-comments flag to post review comments to GitHub");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to analyze pull request");
            throw;
        }
    }

    static void OutputResults(AnalysisResult result, ILogger logger)
    {
        var summary = result.Summary;
        
        logger.LogInformation("=== ANALYSIS RESULTS ===");
        logger.LogInformation("Total Issues: {Total}", summary.TotalIssues);
        logger.LogInformation("Critical: {Critical}, Errors: {Errors}, Warnings: {Warnings}, Info: {Info}",
            summary.CriticalIssues, summary.ErrorIssues, summary.WarningIssues, summary.InfoIssues);
        logger.LogInformation("Files Analyzed: {Files}, Lines: {Lines}, Duration: {Duration}ms",
            summary.FilesAnalyzed, summary.LinesAnalyzed, summary.AnalysisDurationMs);

        if (result.Issues.Any())
        {
            logger.LogInformation("\n=== ISSUES FOUND ===");
            
            var groupedIssues = result.Issues
                .GroupBy(i => i.Severity)
                .OrderByDescending(g => g.Key);

            foreach (var severityGroup in groupedIssues)
            {
                logger.LogInformation("\n{Severity} Issues ({Count}):", severityGroup.Key, severityGroup.Count());
                
                foreach (var issue in severityGroup.Take(10)) // Limit output
                {
                    logger.LogInformation("  {File}:{Line} - {Title}", 
                        issue.FileName, issue.LineNumber, issue.Title);
                    logger.LogInformation("    {Description}", issue.Description);
                    if (!string.IsNullOrEmpty(issue.Suggestion))
                    {
                        logger.LogInformation("    💡 {Suggestion}", issue.Suggestion);
                    }
                }
                
                if (severityGroup.Count() > 10)
                {
                    logger.LogInformation("  ... and {More} more {Severity} issues", 
                        severityGroup.Count() - 10, severityGroup.Key);
                }
            }
        }
        else
        {
            logger.LogInformation("✅ No issues found! Great work!");
        }
    }
}
