using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
                
                // Configure AI Analysis options
                services.Configure<AIAnalysisOptions>(hostContext.Configuration.GetSection("AIAnalysis"));
                
                // Register all analyzers - the application will choose which ones to use
                services.AddTransient<ICodeAnalyzer, AICodeAnalyzer>();
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
            logger.LogError("Usage: PullRequestReviewAgent <owner> <repo> <pr-number> [--post-comments] [--use-ai]");
            logger.LogInformation("Example: PullRequestReviewAgent microsoft dotnet 12345 --post-comments --use-ai");
            logger.LogInformation("Environment variables:");
            logger.LogInformation("  GITHUB_TOKEN: GitHub personal access token");
            logger.LogInformation("  OPENAI_API_KEY: OpenAI API key for AI analysis");
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
        var useAI = args.Contains("--use-ai") || configuration.GetValue<bool>("AIAnalysis:Enabled", true);
        
        var githubToken = configuration["GITHUB_TOKEN"] ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        var openAIApiKey = configuration["AIAnalysis:OpenAIApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrEmpty(githubToken))
        {
            logger.LogError("GitHub token is required. Set GITHUB_TOKEN environment variable or configuration.");
            return;
        }

        if (useAI && string.IsNullOrEmpty(openAIApiKey))
        {
            logger.LogWarning("OpenAI API key not found. Falling back to manual analyzers.");
            logger.LogInformation("Set OPENAI_API_KEY environment variable or AIAnalysis:OpenAIApiKey in config for AI analysis.");
            useAI = false;
        }

        logger.LogInformation("Starting PR analysis for {Owner}/{Repo}#{Number}", owner, repo, prNumber);

        // Set up services
        var githubService = new GitHubService(githubToken, services.GetRequiredService<ILogger<GitHubService>>());
        var prAnalyzer = services.GetRequiredService<IPullRequestAnalyzer>();

        // Register analyzers based on configuration
        var allAnalyzers = services.GetServices<ICodeAnalyzer>();
        
        if (useAI)
        {
            // Use AI analyzer only
            logger.LogError("AI analyzer found, using AI analyzers");
            var aiAnalyzer = allAnalyzers.FirstOrDefault(a => a.Name == "AI Code Analyzer");
            if (aiAnalyzer != null)
            {
                // Update OpenAI API key in the configuration
                if (!string.IsNullOrEmpty(openAIApiKey))
                {
                    logger.LogInformation("Using AI-powered analysis with {AnalyzerName}", aiAnalyzer.Name);
                    var aiOptions = services.GetRequiredService<IOptions<AIAnalysisOptions>>();
                    aiOptions.Value.OpenAIApiKey = openAIApiKey;
                }
                logger.LogInformation("Calling Analyzer");
                prAnalyzer.RegisterAnalyzer(aiAnalyzer);
                logger.LogInformation("Using AI-powered analysis with {AnalyzerName}", aiAnalyzer.Name);
            }
            else
            {
                logger.LogError("AI analyzer not found, falling back to manual analyzers");
                useAI = false;
            }
        }
        
        if (!useAI)
        {
            // Use manual rule-based analyzers
            logger.LogError("AI analyzer not found, using manual analyzers");
            var manualAnalyzers = allAnalyzers.Where(a => a.Name != "AI Code Analyzer");
            foreach (var analyzer in manualAnalyzers)
            {
                prAnalyzer.RegisterAnalyzer(analyzer);
                logger.LogInformation("Registered manual analyzer: {AnalyzerName}", analyzer.Name);
            }
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
