using PullRequestReviewAgent.Analysis.Interfaces;
using PullRequestReviewAgent.Analysis.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System.Text.Json;

namespace PullRequestReviewAgent.Analysis.Analyzers;

/// <summary>
/// AI-powered code analyzer using LLM for intelligent code review
/// </summary>
public class AICodeAnalyzer : ICodeAnalyzer
{
    public string Name => "AI Code Analyzer";

    public IEnumerable<IssueCategory> SupportedCategories => 
        Enum.GetValues<IssueCategory>();

    private readonly OpenAIClient _openAIClient;
    private readonly ILogger<AICodeAnalyzer> _logger;
    private readonly AIAnalysisOptions _options;

    public AICodeAnalyzer(IOptions<AIAnalysisOptions> options, ILogger<AICodeAnalyzer> logger)
    {
        _options = options.Value;
        _logger = logger;
        
        if (string.IsNullOrEmpty(_options.OpenAIApiKey))
        {
            throw new InvalidOperationException("OpenAI API key is required for AI analysis");
        }

        _openAIClient = new OpenAIClient(_options.OpenAIApiKey);
    }

    public async Task<List<CodeIssue>> AnalyzeFileAsync(string fileName, string content)
    {
        var files = new Dictionary<string, string> { { fileName, content } };
        return await AnalyzeFilesAsync(files);
    }

    public async Task<List<CodeIssue>> AnalyzeFilesAsync(Dictionary<string, string> files)
    {
        var issues = new List<CodeIssue>();
        
        foreach (var file in files)
        {
            if (string.IsNullOrEmpty(file.Value) || !ShouldAnalyzeFile(file.Key))
            {
                continue;
            }

            try
            {
                var fileIssues = await AnalyzeSingleFileWithAI(file.Key, file.Value);
                issues.AddRange(fileIssues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to analyze file {FileName} with AI", file.Key);
                
                // Add an error issue if AI analysis fails
                issues.Add(new CodeIssue
                {
                    FileName = file.Key,
                    LineNumber = 1,
                    ColumnNumber = 1,
                    Title = "AI Analysis Failed",
                    Description = $"AI analysis failed for this file: {ex.Message}",
                    Severity = IssueSeverity.Warning,
                    Category = IssueCategory.Quality,
                    RuleId = "AI_ANALYSIS_ERROR",
                    CodeSnippet = "",
                    Suggestion = "Check the file format and try again. Manual review may be needed."
                });
            }
        }

        return issues;
    }

    private async Task<List<CodeIssue>> AnalyzeSingleFileWithAI(string fileName, string content)
    {
        _logger.LogInformation("Analyzing file {FileName} with AI", fileName);

        var systemPrompt = CreateSystemPrompt();
        var userPrompt = CreateUserPrompt(fileName, content);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        };

        var chatCompletion = await _openAIClient.GetChatClient(_options.Model).CompleteChatAsync(messages, new ChatCompletionOptions
        {
            Temperature = 0.1f, // Low temperature for consistent, focused analysis
            MaxOutputTokenCount = 4000,
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
        });
        _logger.LogInformation("AI response for {FileName}: {Response}", fileName, chatCompletion.Value.Content[0].Text);
        var response = chatCompletion.Value.Content[0].Text;
        
        try
        {
            var analysisResult = JsonSerializer.Deserialize<AIAnalysisResponse>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return ConvertToCodeIssues(analysisResult, fileName);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse AI response for {FileName}", fileName);
            return new List<CodeIssue>();
        }
    }

    private string CreateSystemPrompt()
    {
        var guidelines = !string.IsNullOrEmpty(_options.CodingGuidelinesUrl) 
            ? $"Additional coding guidelines: {_options.CodingGuidelinesUrl}\n" 
            : "";

        return $@"You are an expert code reviewer analyzing code for potential issues. 
{guidelines}
Analyze the provided code for:

**Security Issues:**
- SQL injection vulnerabilities
- Cross-site scripting (XSS) risks
- Hardcoded credentials or secrets
- Weak cryptographic algorithms
- Insecure protocol usage
- Input validation issues
- Authentication/authorization flaws

**Quality Issues:**
- Code complexity and readability
- Potential bugs and logic errors
- Error handling problems
- Resource management issues
- Dead code or unused variables
- Magic numbers or hardcoded values
- Poor naming conventions

**Performance Issues:**
- Inefficient algorithms or data structures
- Memory leaks or excessive memory usage
- Unnecessary database queries
- Blocking operations in async contexts
- Expensive operations in loops

**Style Issues:**
- Inconsistent formatting
- Line length violations
- Missing documentation
- Inconsistent naming conventions

**Maintainability Issues:**
- Code duplication
- High coupling, low cohesion
- Violation of SOLID principles
- Large methods or classes
- Complex conditional logic

**Reliability Issues:**
- Race conditions
- Null reference possibilities
- Exception handling gaps
- Resource disposal issues

Respond with a JSON object containing an array of issues found:
{{
  ""issues"": [
    {{
      ""lineNumber"": 1,
      ""columnNumber"": 1,
      ""title"": ""Issue Title"",
      ""description"": ""Detailed description of the issue"",
      ""severity"": ""Critical|Error|Warning|Info"",
      ""category"": ""Security|Quality|Performance|Style|Maintainability|Reliability"",
      ""ruleId"": ""RULE_ID"",
      ""suggestion"": ""Suggested fix or improvement"",
      ""codeSnippet"": ""Relevant code snippet""
    }}
  ]
}}

Only report actual issues. Do not report issues for standard library usage, normal patterns, or correctly implemented code.";
    }

    private string CreateUserPrompt(string fileName, string content)
    {
        return $@"Please analyze the following code file for potential issues:

**File:** {fileName}

**Code:**
```
{content}
```

Provide analysis in the specified JSON format.";
    }

    private List<CodeIssue> ConvertToCodeIssues(AIAnalysisResponse? response, string fileName)
    {
        if (response?.Issues == null)
        {
            _logger.LogError("No Issues Found");
            return new List<CodeIssue>();
        }

        return response.Issues.Select(issue => new CodeIssue
        {
            FileName = fileName,
            LineNumber = issue.LineNumber,
            ColumnNumber = issue.ColumnNumber,
            Title = issue.Title ?? "AI Analysis Issue",
            Description = issue.Description ?? "",
            Severity = ParseSeverity(issue.Severity),
            Category = ParseCategory(issue.Category),
            RuleId = issue.RuleId ?? "AI_RULE",
            Suggestion = issue.Suggestion,
            CodeSnippet = issue.CodeSnippet ?? ""
        }).ToList();
    }

    private IssueSeverity ParseSeverity(string? severity)
    {
        return severity?.ToLowerInvariant() switch
        {
            "critical" => IssueSeverity.Critical,
            "error" => IssueSeverity.Error,
            "warning" => IssueSeverity.Warning,
            "info" => IssueSeverity.Info,
            _ => IssueSeverity.Warning
        };
    }

    private IssueCategory ParseCategory(string? category)
    {
        return category?.ToLowerInvariant() switch
        {
            "security" => IssueCategory.Security,
            "quality" => IssueCategory.Quality,
            "performance" => IssueCategory.Performance,
            "style" => IssueCategory.Style,
            "maintainability" => IssueCategory.Maintainability,
            "reliability" => IssueCategory.Reliability,
            _ => IssueCategory.Quality
        };
    }

    private bool ShouldAnalyzeFile(string fileName)
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

/// <summary>
/// Configuration options for AI analysis
/// </summary>
public class AIAnalysisOptions
{
    public string OpenAIApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4.1-nano";
    public string? CodingGuidelinesUrl { get; set; }
    public int MaxFileSizeBytes { get; set; } = 50000; // 50KB limit for AI analysis
    public bool EnableBatchAnalysis { get; set; } = true;
}

/// <summary>
/// Response format from AI analysis
/// </summary>
internal class AIAnalysisResponse
{
    public List<AIIssue>? Issues { get; set; }
}

/// <summary>
/// Individual issue from AI analysis
/// </summary>
internal class AIIssue
{
    public int LineNumber { get; set; }
    public int ColumnNumber { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Severity { get; set; }
    public string? Category { get; set; }
    public string? RuleId { get; set; }
    public string? Suggestion { get; set; }
    public string? CodeSnippet { get; set; }
}