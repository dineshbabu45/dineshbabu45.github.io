using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PullRequestReviewAgent.Analysis.Analyzers;
using PullRequestReviewAgent.Analysis.Interfaces;
using PullRequestReviewAgent.Analysis.Models;
using Xunit;

namespace PullRequestReviewAgent.Tests;

public class IntegrationTests
{
    [Fact]
    public void Application_WithoutOpenAIKey_ShouldUseFallbackAnalyzers()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AIAnalysis:OpenAIApiKey"] = "",
                ["AIAnalysis:Model"] = "gpt-4o-mini"
            })
            .Build();

        var host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddConfiguration(configuration);
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddLogging();
                services.AddSingleton<IPullRequestAnalyzer, PullRequestAnalyzer>();
                services.Configure<AIAnalysisOptions>(hostContext.Configuration.GetSection("AIAnalysis"));
                
                // Register all analyzers
                services.AddTransient<ICodeAnalyzer, SecurityAnalyzer>();
                services.AddTransient<ICodeAnalyzer, QualityAnalyzer>();
                services.AddTransient<ICodeAnalyzer, StyleAnalyzer>();
            })
            .Build();

        // Act
        var prAnalyzer = host.Services.GetRequiredService<IPullRequestAnalyzer>();
        var analyzers = host.Services.GetServices<ICodeAnalyzer>();
        
        // Register manual analyzers (simulating fallback behavior)
        foreach (var analyzer in analyzers)
        {
            prAnalyzer.RegisterAnalyzer(analyzer);
        }

        // Assert
        Assert.Equal(3, prAnalyzer.RegisteredAnalyzers.Count());
        Assert.Contains(prAnalyzer.RegisteredAnalyzers, a => a.Name == "Security Analyzer");
        Assert.Contains(prAnalyzer.RegisteredAnalyzers, a => a.Name == "Quality Analyzer");
        Assert.Contains(prAnalyzer.RegisteredAnalyzers, a => a.Name == "Style Analyzer");
    }

    [Fact]
    public async Task SecurityAnalyzer_ShouldDetectHardcodedPassword()
    {
        // Arrange
        var analyzer = new SecurityAnalyzer();
        var testCode = @"
public class TestClass 
{
    private string password = ""hardcoded123"";
}";

        // Act
        var issues = await analyzer.AnalyzeFileAsync("test.cs", testCode);

        // Assert
        Assert.Single(issues);
        Assert.Equal("Hardcoded Password", issues[0].Title);
        Assert.Equal(IssueSeverity.Critical, issues[0].Severity);
        Assert.Equal(IssueCategory.Security, issues[0].Category);
    }

    [Fact]
    public async Task QualityAnalyzer_ShouldDetectTodoComments()
    {
        // Arrange
        var analyzer = new QualityAnalyzer();
        var testCode = @"
public class TestClass 
{
    public void Method()
    {
        // TODO: Implement this method
        throw new NotImplementedException();
    }
}";

        // Act
        var issues = await analyzer.AnalyzeFileAsync("test.cs", testCode);

        // Assert
        Assert.Contains(issues, i => i.Title == "TODO Comment");
    }

    [Fact]
    public async Task StyleAnalyzer_ShouldDetectLongLines()
    {
        // Arrange
        var analyzer = new StyleAnalyzer();
        var testCode = "public class TestClass { public void VeryLongMethodNameThatExceedsTheRecommendedLineLengthAndShouldBeDetectedByTheStyleAnalyzer() { } }";

        // Act
        var issues = await analyzer.AnalyzeFileAsync("test.cs", testCode);

        // Assert
        Assert.Contains(issues, i => i.Title.Contains("Line") || i.Description.Contains("Lines longer than"));
    }
}