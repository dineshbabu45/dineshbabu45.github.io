using PullRequestReviewAgent.Analysis.Analyzers;
using PullRequestReviewAgent.Analysis.Models;
using Xunit;

namespace PullRequestReviewAgent.Tests;

public class QualityAnalyzerTests
{
    private readonly QualityAnalyzer _analyzer;

    public QualityAnalyzerTests()
    {
        _analyzer = new QualityAnalyzer();
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithMagicNumber_ShouldDetectIssue()
    {
        // Arrange
        var fileName = "calculator.cs";
        var content = @"
public class Calculator
{
    public double CalculateArea(double radius)
    {
        return 3.14159 * radius * radius;
    }
}";

        // Act
        var issues = await _analyzer.AnalyzeFileAsync(fileName, content);

        // Assert
        Assert.Contains(issues, i => i.RuleId == "MAGIC_NUMBER");
        var magicNumberIssue = issues.First(i => i.RuleId == "MAGIC_NUMBER");
        Assert.Equal("Magic Number", magicNumberIssue.Title);
        Assert.Equal(IssueCategory.Quality, magicNumberIssue.Category);
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithEmptyCatchBlock_ShouldDetectIssue()
    {
        // Arrange
        var fileName = "errorhandling.cs";
        var content = @"
public class FileProcessor
{
    public void ProcessFile(string path)
    {
        try
        {
            File.ReadAllText(path);
        }
        catch (Exception ex)
        {
        }
    }
}";

        // Act
        var issues = await _analyzer.AnalyzeFileAsync(fileName, content);

        // Assert
        Assert.Contains(issues, i => i.RuleId == "EMPTY_CATCH");
        var emptyCatchIssue = issues.First(i => i.RuleId == "EMPTY_CATCH");
        Assert.Equal("Empty Catch Block", emptyCatchIssue.Title);
        Assert.Equal(IssueSeverity.Error, emptyCatchIssue.Severity);
        Assert.Equal(IssueCategory.Reliability, emptyCatchIssue.Category);
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithTodoComment_ShouldDetectIssue()
    {
        // Arrange
        var fileName = "incomplete.cs";
        var content = @"
public class IncompleteFeature
{
    public void DoSomething()
    {
        // TODO: Implement this method
        throw new NotImplementedException();
    }
}";

        // Act
        var issues = await _analyzer.AnalyzeFileAsync(fileName, content);

        // Assert
        Assert.Contains(issues, i => i.RuleId == "TODO_COMMENT");
        var todoIssue = issues.First(i => i.RuleId == "TODO_COMMENT");
        Assert.Equal("TODO Comment", todoIssue.Title);
        Assert.Equal(IssueSeverity.Info, todoIssue.Severity);
    }
}