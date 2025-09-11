using PullRequestReviewAgent.Analysis.Analyzers;
using PullRequestReviewAgent.Analysis.Models;
using Xunit;

namespace PullRequestReviewAgent.Tests;

public class StyleAnalyzerTests
{
    private readonly StyleAnalyzer _analyzer;

    public StyleAnalyzerTests()
    {
        _analyzer = new StyleAnalyzer();
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithTrailingWhitespace_ShouldDetectIssue()
    {
        // Arrange
        var fileName = "messy.cs";
        var content = "public class Example   \n{\n    public void Method()   \n    {\n    }\n}";

        // Act
        var issues = await _analyzer.AnalyzeFileAsync(fileName, content);

        // Assert
        var trailingWhitespaceIssues = issues.Where(i => i.RuleId == "TRAILING_WHITESPACE").ToList();
        Assert.True(trailingWhitespaceIssues.Count >= 1);
        Assert.All(trailingWhitespaceIssues, issue =>
        {
            Assert.Equal("Trailing Whitespace", issue.Title);
            Assert.Equal(IssueCategory.Style, issue.Category);
        });
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithLongLine_ShouldDetectIssue()
    {
        // Arrange
        var fileName = "longline.cs";
        var longLine = "public class VeryLongClassNameThatExceedsTheLineLimit" + new string('X', 80) + " { }";
        var content = longLine;

        // Act
        var issues = await _analyzer.AnalyzeFileAsync(fileName, content);

        // Assert
        Assert.Contains(issues, i => i.RuleId == "LONG_LINE");
        var longLineIssue = issues.First(i => i.RuleId == "LONG_LINE");
        Assert.Equal("Long Line", longLineIssue.Title);
        Assert.Equal(IssueSeverity.Info, longLineIssue.Severity);
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithMissingSpaceAfterKeyword_ShouldDetectIssue()
    {
        // Arrange
        var fileName = "badspacing.cs";
        var content = @"
public class BadSpacing
{
    public void TestMethod()
    {
        if(true)
        {
            Console.WriteLine(""Bad spacing"");
        }
    }
}";

        // Act
        var issues = await _analyzer.AnalyzeFileAsync(fileName, content);

        // Assert
        Assert.Contains(issues, i => i.RuleId == "MISSING_SPACE_AFTER_KEYWORD");
        var spacingIssue = issues.First(i => i.RuleId == "MISSING_SPACE_AFTER_KEYWORD");
        Assert.Equal("Missing Space After Keyword", spacingIssue.Title);
        Assert.Equal(IssueCategory.Style, spacingIssue.Category);
    }

    [Fact]
    public async Task AnalyzeFileAsync_WithWellFormattedCode_ShouldHaveMinimalIssues()
    {
        // Arrange
        var fileName = "clean.cs";
        var content = @"public class CleanCode
{
    public void WellFormattedMethod()
    {
        if (true)
        {
            Console.WriteLine(""Clean code"");
        }
    }
}
";

        // Act
        var issues = await _analyzer.AnalyzeFileAsync(fileName, content);

        // Assert
        // Should have no major style issues
        Assert.DoesNotContain(issues, i => i.Severity >= IssueSeverity.Warning);
    }
}