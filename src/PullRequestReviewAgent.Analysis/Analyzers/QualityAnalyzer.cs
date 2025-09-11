using PullRequestReviewAgent.Analysis.Interfaces;
using PullRequestReviewAgent.Analysis.Models;
using System.Text.RegularExpressions;

namespace PullRequestReviewAgent.Analysis.Analyzers;

/// <summary>
/// Analyzes code for quality issues
/// </summary>
public class QualityAnalyzer : ICodeAnalyzer
{
    public string Name => "Quality Analyzer";

    public IEnumerable<IssueCategory> SupportedCategories => 
        new[] { IssueCategory.Quality, IssueCategory.Maintainability, IssueCategory.Reliability };

    private readonly Dictionary<string, (string Pattern, string Title, string Description, IssueSeverity Severity, IssueCategory Category)> _qualityPatterns = new()
    {
        ["LONG_METHOD"] = (@"(public|private|protected|internal)\s+\w+\s+\w+\s*\([^)]*\)\s*\{", 
            "Potentially Long Method", 
            "Methods with too many lines are harder to understand and maintain. Consider breaking into smaller methods.",
            IssueSeverity.Warning,
            IssueCategory.Maintainability),
        
        ["MAGIC_NUMBER"] = (@"\b\d{2,}\b", 
            "Magic Number", 
            "Magic numbers make code harder to understand. Consider using named constants instead.",
            IssueSeverity.Warning,
            IssueCategory.Quality),
        
        ["EMPTY_CATCH"] = (@"catch\s*\([^)]*\)\s*\{[\s\r\n]*\}", 
            "Empty Catch Block", 
            "Empty catch blocks hide errors and make debugging difficult. Add proper error handling.",
            IssueSeverity.Error,
            IssueCategory.Reliability),
        
        ["TODO_COMMENT"] = (@"//\s*(TODO|FIXME|HACK|BUG)", 
            "TODO Comment", 
            "TODO comments indicate incomplete work. Consider addressing before merging.",
            IssueSeverity.Info,
            IssueCategory.Quality),
        
        ["CONSOLE_LOG"] = (@"(console\.(log|debug|info|warn|error)|Console\.(WriteLine|Write))", 
            "Debug/Console Output", 
            "Console output statements should be removed from production code or replaced with proper logging.",
            IssueSeverity.Warning,
            IssueCategory.Quality),
        
        ["DUPLICATE_CODE"] = (@"(if\s*\([^)]+\)\s*\{[^}]+\})\s*(if\s*\([^)]+\)\s*\{[^}]+\})", 
            "Potential Code Duplication", 
            "Duplicate code increases maintenance burden. Consider extracting common logic into methods.",
            IssueSeverity.Warning,
            IssueCategory.Maintainability),
        
        ["COMPLEX_CONDITION"] = (@"if\s*\([^)]*&&[^)]*\|\|[^)]*\)", 
            "Complex Condition", 
            "Complex boolean conditions are hard to read. Consider breaking into multiple conditions or using variables.",
            IssueSeverity.Warning,
            IssueCategory.Maintainability),
        
        ["LARGE_CLASS"] = (@"class\s+\w+", 
            "Potentially Large Class", 
            "Large classes violate single responsibility principle. Consider breaking into smaller classes.",
            IssueSeverity.Info,
            IssueCategory.Maintainability)
    };

    public async Task<List<CodeIssue>> AnalyzeFileAsync(string fileName, string content)
    {
        var issues = new List<CodeIssue>();
        var lines = content.Split('\n');

        // Check for long methods
        issues.AddRange(await AnalyzeLongMethods(fileName, content));
        
        // Check for other patterns
        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            var lineNumber = lineIndex + 1;

            foreach (var pattern in _qualityPatterns.Where(p => p.Key != "LONG_METHOD"))
            {
                var regexOptions = pattern.Key == "EMPTY_CATCH" ? RegexOptions.IgnoreCase | RegexOptions.Singleline : RegexOptions.IgnoreCase;
                var regex = new Regex(pattern.Value.Pattern, regexOptions);
                
                if (pattern.Key == "EMPTY_CATCH")
                {
                    // For empty catch, search the entire content instead of line by line
                    var matches = regex.Matches(content);
                    foreach (Match match in matches)
                    {
                        var matchLineNumber = content.Substring(0, match.Index).Split('\n').Length;
                        issues.Add(new CodeIssue
                        {
                            FileName = fileName,
                            LineNumber = matchLineNumber,
                            ColumnNumber = match.Index + 1,
                            Title = pattern.Value.Title,
                            Description = pattern.Value.Description,
                            Severity = pattern.Value.Severity,
                            Category = pattern.Value.Category,
                            RuleId = pattern.Key,
                            CodeSnippet = match.Value,
                            Suggestion = GetQualitySuggestion(pattern.Key)
                        });
                    }
                }
                else
                {
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
                            Category = pattern.Value.Category,
                            RuleId = pattern.Key,
                            CodeSnippet = line.Trim(),
                            Suggestion = GetQualitySuggestion(pattern.Key)
                        });
                    }
                }
            }
        }

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

    private async Task<List<CodeIssue>> AnalyzeLongMethods(string fileName, string content)
    {
        var issues = new List<CodeIssue>();
        var lines = content.Split('\n');
        
        var methodRegex = new Regex(@"(public|private|protected|internal)\s+\w+\s+(\w+)\s*\([^)]*\)\s*\{", RegexOptions.Multiline);
        var matches = methodRegex.Matches(content);

        foreach (Match match in matches)
        {
            var methodName = match.Groups[2].Value;
            var startLine = content.Substring(0, match.Index).Split('\n').Length;
            var methodLines = CountMethodLines(content, match.Index);

            if (methodLines > 50) // Threshold for long method
            {
                issues.Add(new CodeIssue
                {
                    FileName = fileName,
                    LineNumber = startLine,
                    ColumnNumber = match.Index,
                    Title = "Long Method",
                    Description = $"Method '{methodName}' has {methodLines} lines. Consider breaking it into smaller methods.",
                    Severity = IssueSeverity.Warning,
                    Category = IssueCategory.Maintainability,
                    RuleId = "LONG_METHOD",
                    CodeSnippet = match.Value,
                    Suggestion = "Break this method into smaller, more focused methods."
                });
            }
        }

        await Task.CompletedTask;
        return issues;
    }

    private int CountMethodLines(string content, int methodStartIndex)
    {
        var braceCount = 0;
        var lineCount = 1;
        var foundOpenBrace = false;

        for (int i = methodStartIndex; i < content.Length; i++)
        {
            var ch = content[i];
            
            if (ch == '{')
            {
                braceCount++;
                foundOpenBrace = true;
            }
            else if (ch == '}')
            {
                braceCount--;
                if (foundOpenBrace && braceCount == 0)
                {
                    break;
                }
            }
            else if (ch == '\n')
            {
                lineCount++;
            }
        }

        return lineCount;
    }

    private string GetQualitySuggestion(string ruleId)
    {
        return ruleId switch
        {
            "MAGIC_NUMBER" => "Define a named constant or configuration value instead of using the literal number.",
            "EMPTY_CATCH" => "Add proper error handling, logging, or at least a comment explaining why the exception is ignored.",
            "TODO_COMMENT" => "Complete the TODO item or create a tracking issue for future work.",
            "CONSOLE_LOG" => "Replace with proper logging framework or remove if not needed.",
            "DUPLICATE_CODE" => "Extract common code into a shared method or utility function.",
            "COMPLEX_CONDITION" => "Break complex conditions into smaller, named boolean variables or methods.",
            "LARGE_CLASS" => "Consider applying Single Responsibility Principle and splitting the class.",
            _ => "Review this code for quality improvements."
        };
    }
}