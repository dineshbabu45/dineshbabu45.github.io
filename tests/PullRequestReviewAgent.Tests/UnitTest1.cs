using PullRequestReviewAgent.Analysis.Analyzers;
using PullRequestReviewAgent.Analysis.Models;
using Xunit;

namespace PullRequestReviewAgent.Tests;

public class SecurityAnalyzerTests
{
    private readonly SecurityAnalyzer _analyzer;

    public SecurityAnalyzerTests()
    {
        _analyzer = new SecurityAnalyzer();
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithSqlInjectionVulnerability_ShouldDetectIssue()
    {
        // Arrange
        var fileName = "test.cs";
        var content = @"
public class UserRepository
{
    public User GetUser(string userId)
    {
        var sql = ""SELECT * FROM Users WHERE Id = "" + userId;
        return Database.Query(sql);
    }
}";

        // Act
        var issues = await _analyzer.AnalyzeFileAsync(fileName, content);

        // Assert
        Assert.Single(issues);
        var issue = issues.First();
        Assert.Equal("Potential SQL Injection", issue.Title);
        Assert.Equal(IssueSeverity.Critical, issue.Severity);
        Assert.Equal(IssueCategory.Security, issue.Category);
        Assert.Equal("SQL_INJECTION", issue.RuleId);
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithHardcodedPassword_ShouldDetectIssue()
    {
        // Arrange
        var fileName = "config.cs";
        var content = @"
public class Configuration
{
    private readonly string password = ""secret123"";
}";

        // Act
        var issues = await _analyzer.AnalyzeFileAsync(fileName, content);

        // Assert
        Assert.Single(issues);
        var issue = issues.First();
        Assert.Equal("Hardcoded Password", issue.Title);
        Assert.Equal(IssueSeverity.Critical, issue.Severity);
        Assert.Equal(IssueCategory.Security, issue.Category);
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithSecureCode_ShouldNotDetectIssues()
    {
        // Arrange
        var fileName = "secure.cs";
        var content = @"
public class UserRepository
{
    public User GetUser(string userId)
    {
        var sql = ""SELECT * FROM Users WHERE Id = @userId"";
        return Database.Query(sql, new { userId });
    }
}";

        // Act
        var issues = await _analyzer.AnalyzeFileAsync(fileName, content);

        // Assert
        Assert.Empty(issues);
    }
}