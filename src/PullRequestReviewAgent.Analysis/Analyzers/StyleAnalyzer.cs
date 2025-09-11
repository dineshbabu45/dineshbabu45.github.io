using PullRequestReviewAgent.Analysis.Interfaces;
using PullRequestReviewAgent.Analysis.Models;
using System.Text.RegularExpressions;

namespace PullRequestReviewAgent.Analysis.Analyzers;

/// <summary>
/// Analyzes code for style and formatting issues
/// </summary>
public class StyleAnalyzer : ICodeAnalyzer
{
    public string Name => "Style Analyzer";

    public IEnumerable<IssueCategory> SupportedCategories => 
        new[] { IssueCategory.Style };

    private readonly Dictionary<string, (string Pattern, string Title, string Description, IssueSeverity Severity)> _stylePatterns = new()
    {
        ["MIXED_INDENTATION"] = (@"^(\t+\s+|\s+\t+)", 
            "Mixed Indentation", 
            "Mixed tabs and spaces can cause formatting issues. Use consistent indentation.",
            IssueSeverity.Warning),
        
        ["TRAILING_WHITESPACE"] = (@"\s+$", 
            "Trailing Whitespace", 
            "Trailing whitespace should be removed for cleaner code.",
            IssueSeverity.Info),
        
        ["LONG_LINE"] = (@"^.{121,}$", 
            "Long Line", 
            "Lines longer than 120 characters are harder to read. Consider breaking the line.",
            IssueSeverity.Info),
        
        ["MISSING_SPACE_AFTER_KEYWORD"] = (@"\b(if|for|while|switch|catch)\(", 
            "Missing Space After Keyword", 
            "Add a space after control flow keywords for better readability.",
            IssueSeverity.Info),
        
        ["INCONSISTENT_BRACES"] = (@"(if|for|while|else)\s*[^{]*\n\s*[^{]", 
            "Inconsistent Brace Style", 
            "Use consistent brace style throughout the codebase.",
            IssueSeverity.Info),
        
        ["MULTIPLE_EMPTY_LINES"] = (@"\n\s*\n\s*\n", 
            "Multiple Empty Lines", 
            "Multiple consecutive empty lines should be reduced to a single empty line.",
            IssueSeverity.Info),
        
        ["MISSING_FINAL_NEWLINE"] = (@"[^\n]$", 
            "Missing Final Newline", 
            "Files should end with a newline character.",
            IssueSeverity.Info),
        
        ["CAMELCASE_VIOLATION"] = (@"\b[a-z]+[A-Z][a-z]*[A-Z]", 
            "Naming Convention", 
            "Use consistent naming conventions (camelCase for variables, PascalCase for methods).",
            IssueSeverity.Warning)
    };

    public async Task<List<CodeIssue>> AnalyzeFileAsync(string fileName, string content)
    {
        var issues = new List<CodeIssue>();
        var lines = content.Split('\n');

        // Check file-level issues
        CheckFileLevel(fileName, content, issues);

        // Check line-level issues
        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            var lineNumber = lineIndex + 1;

            CheckLineLevel(fileName, line, lineNumber, issues);
        }

        await Task.CompletedTask;
        return issues;
    }

    public async Task<List<CodeIssue>> AnalyzeFilesAsync(Dictionary<string, string> files)
    {
        var allIssues = new List<CodeIssue>();

        foreach (var file in files)
        {
            var fileIssues = await AnalyzeFileAsync(file.Key, file.Value);
            allIssues.AddRange(fileIssues);
        }

        return allIssues;
    }

    private void CheckFileLevel(string fileName, string content, List<CodeIssue> issues)
    {
        // Check for multiple empty lines
        var multipleEmptyLinesRegex = new Regex(_stylePatterns["MULTIPLE_EMPTY_LINES"].Pattern);
        var matches = multipleEmptyLinesRegex.Matches(content);
        
        foreach (Match match in matches)
        {
            var lineNumber = content.Substring(0, match.Index).Split('\n').Length;
            issues.Add(new CodeIssue
            {
                FileName = fileName,
                LineNumber = lineNumber,
                ColumnNumber = 1,
                Title = _stylePatterns["MULTIPLE_EMPTY_LINES"].Title,
                Description = _stylePatterns["MULTIPLE_EMPTY_LINES"].Description,
                Severity = _stylePatterns["MULTIPLE_EMPTY_LINES"].Severity,
                Category = IssueCategory.Style,
                RuleId = "MULTIPLE_EMPTY_LINES",
                CodeSnippet = "Multiple empty lines",
                Suggestion = "Replace multiple empty lines with a single empty line."
            });
        }

        // Check for missing final newline
        if (!content.EndsWith("\n") && content.Length > 0)
        {
            issues.Add(new CodeIssue
            {
                FileName = fileName,
                LineNumber = content.Split('\n').Length,
                ColumnNumber = content.Split('\n').Last().Length + 1,
                Title = _stylePatterns["MISSING_FINAL_NEWLINE"].Title,
                Description = _stylePatterns["MISSING_FINAL_NEWLINE"].Description,
                Severity = _stylePatterns["MISSING_FINAL_NEWLINE"].Severity,
                Category = IssueCategory.Style,
                RuleId = "MISSING_FINAL_NEWLINE",
                CodeSnippet = content.Split('\n').Last(),
                Suggestion = "Add a newline at the end of the file."
            });
        }
    }

    private void CheckLineLevel(string fileName, string line, int lineNumber, List<CodeIssue> issues)
    {
        foreach (var pattern in _stylePatterns.Where(p => p.Key != "MULTIPLE_EMPTY_LINES" && p.Key != "MISSING_FINAL_NEWLINE"))
        {
            var regex = new Regex(pattern.Value.Pattern);
            var matches = regex.Matches(line);

            foreach (Match match in matches)
            {
                issues.Add(new CodeIssue
                {
                    FileName = fileName,
                    LineNumber = lineNumber,
                    ColumnNumber = match.Index + 1,
                    Title = pattern.Value.Title,
                    Description = pattern.Value.Description,
                    Severity = pattern.Value.Severity,
                    Category = IssueCategory.Style,
                    RuleId = pattern.Key,
                    CodeSnippet = line.Trim(),
                    Suggestion = GetStyleSuggestion(pattern.Key)
                });
            }
        }
    }

    private string GetStyleSuggestion(string ruleId)
    {
        return ruleId switch
        {
            "MIXED_INDENTATION" => "Use either spaces or tabs consistently for indentation.",
            "TRAILING_WHITESPACE" => "Remove trailing whitespace from the end of the line.",
            "LONG_LINE" => "Break the line into multiple lines or refactor to reduce length.",
            "MISSING_SPACE_AFTER_KEYWORD" => "Add a space after the keyword: 'if (' instead of 'if('.",
            "INCONSISTENT_BRACES" => "Use consistent brace placement throughout the file.",
            "CAMELCASE_VIOLATION" => "Follow naming conventions: camelCase for variables, PascalCase for methods.",
            _ => "Review this code for style consistency."
        };
    }
}