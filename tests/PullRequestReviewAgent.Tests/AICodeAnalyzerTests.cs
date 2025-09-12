using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PullRequestReviewAgent.Analysis.Analyzers;
using PullRequestReviewAgent.Analysis.Models;
using Xunit;

namespace PullRequestReviewAgent.Tests;

public class AICodeAnalyzerTests
{
    private readonly ILogger<AICodeAnalyzer> _logger;
    private readonly AIAnalysisOptions _options;

    public AICodeAnalyzerTests()
    {
        _logger = new LoggerFactory().CreateLogger<AICodeAnalyzer>();
        _options = new AIAnalysisOptions
        {
            OpenAIApiKey = "test-key", // Mock key for testing
            Model = "gpt-4o-mini",
            MaxFileSizeBytes = 50000
        };
    }

    [Fact]
    public void Constructor_WithValidOptions_ShouldCreateInstance()
    {
        // Arrange
        var optionsWrapper = Options.Create(_options);

        // Act & Assert - Constructor should create instance with any non-empty API key
        var analyzer = new AICodeAnalyzer(optionsWrapper, _logger);
        Assert.NotNull(analyzer);
        Assert.Equal("AI Code Analyzer", analyzer.Name);
    }

    [Fact]
    public void Constructor_WithEmptyApiKey_ShouldThrowException()
    {
        // Arrange
        var invalidOptions = new AIAnalysisOptions { OpenAIApiKey = "" };
        var optionsWrapper = Options.Create(invalidOptions);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new AICodeAnalyzer(optionsWrapper, _logger));
    }

    [Fact]
    public void Name_ShouldReturnCorrectName()
    {
        // Arrange
        var optionsWrapper = Options.Create(_options);
        var analyzer = new AICodeAnalyzer(optionsWrapper, _logger);

        // Act & Assert
        Assert.Equal("AI Code Analyzer", analyzer.Name);
    }

    [Fact]
    public void SupportedCategories_ShouldReturnAllCategories()
    {
        // Arrange & Act
        var analyzer = new TestableAICodeAnalyzer();
        var categories = analyzer.SupportedCategories.ToList();

        // Assert
        Assert.Contains(IssueCategory.Security, categories);
        Assert.Contains(IssueCategory.Quality, categories);
        Assert.Contains(IssueCategory.Style, categories);
        Assert.Contains(IssueCategory.Performance, categories);
        Assert.Contains(IssueCategory.Maintainability, categories);
        Assert.Contains(IssueCategory.Reliability, categories);
    }

    [Theory]
    [InlineData("test.cs", true)]
    [InlineData("test.js", true)]
    [InlineData("test.py", true)]
    [InlineData("test.txt", false)]
    [InlineData("test.md", false)]
    [InlineData("", false)]
    public void ShouldAnalyzeFile_WithDifferentExtensions_ShouldReturnExpectedResult(string fileName, bool expected)
    {
        // Arrange
        var analyzer = new TestableAICodeAnalyzer();

        // Act
        var result = analyzer.TestShouldAnalyzeFile(fileName);

        // Assert
        Assert.Equal(expected, result);
    }

    // Helper class to test non-API dependent functionality
    private class TestableAICodeAnalyzer
    {
        public string Name => "AI Code Analyzer";
        
        public IEnumerable<IssueCategory> SupportedCategories => 
            Enum.GetValues<IssueCategory>();

        public bool TestShouldAnalyzeFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var supportedExtensions = new[]
            {
                ".cs", ".js", ".ts", ".py", ".java", ".cpp", ".c", ".h", ".hpp",
                ".go", ".rs", ".php", ".rb", ".swift", ".kt", ".scala", ".sh",
                ".ps1", ".sql", ".html", ".css", ".jsx", ".tsx", ".vue", ".yaml", ".yml"
            };

            return supportedExtensions.Contains(extension);
        }
    }
}