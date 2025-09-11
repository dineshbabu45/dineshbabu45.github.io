using PullRequestReviewAgent.Analysis.Interfaces;
using PullRequestReviewAgent.Analysis.Models;
using System.Text.RegularExpressions;

namespace PullRequestReviewAgent.Analysis.Analyzers;

/// <summary>
/// Analyzes code for security vulnerabilities
/// </summary>
public class SecurityAnalyzer : ICodeAnalyzer
{
    public string Name => "Security Analyzer";

    public IEnumerable<IssueCategory> SupportedCategories => 
        new[] { IssueCategory.Security };

    private readonly Dictionary<string, (string Pattern, string Title, string Description, IssueSeverity Severity)> _securityPatterns = new()
    {
        ["SQL_INJECTION"] = (@"(SELECT|INSERT|UPDATE|DELETE).*\+", 
            "Potential SQL Injection", 
            "String concatenation in SQL queries can lead to SQL injection vulnerabilities. Use parameterized queries instead.",
            IssueSeverity.Critical),
        
        ["HARDCODED_PASSWORD"] = (@"(password|pwd|pass)\s*=\s*[""'][^""']+[""']", 
            "Hardcoded Password", 
            "Hardcoded passwords in source code pose security risks. Use configuration or secure vaults instead.",
            IssueSeverity.Critical),
        
        ["WEAK_CRYPTO"] = (@"(MD5|SHA1|DES)\s*\(", 
            "Weak Cryptographic Algorithm", 
            "Weak cryptographic algorithms are vulnerable to attacks. Use stronger alternatives like SHA-256 or AES.",
            IssueSeverity.Error),
        
        ["XSS_VULNERABILITY"] = (@"(innerHTML|outerHTML)\s*[+=]\s*[^;]+", 
            "Potential XSS Vulnerability", 
            "Direct assignment to innerHTML can lead to XSS attacks. Sanitize user input or use safer alternatives.",
            IssueSeverity.Error),
        
        ["INSECURE_HTTP"] = (@"http://[^/\s]+", 
            "Insecure HTTP Protocol", 
            "HTTP connections are not encrypted and can be intercepted. Use HTTPS instead.",
            IssueSeverity.Warning),
        
        ["EVAL_USAGE"] = (@"eval\s*\(", 
            "Dangerous eval() Usage", 
            "The eval() function can execute arbitrary code and is dangerous. Avoid using eval() with user input.",
            IssueSeverity.Error)
    };

    public async Task<List<CodeIssue>> AnalyzeFileAsync(string fileName, string content)
    {
        var issues = new List<CodeIssue>();
        var lines = content.Split('\n');

        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            var lineNumber = lineIndex + 1;

            foreach (var pattern in _securityPatterns)
            {
                var regex = new Regex(pattern.Value.Pattern, RegexOptions.IgnoreCase);
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
                        Category = IssueCategory.Security,
                        RuleId = pattern.Key,
                        CodeSnippet = line.Trim(),
                        Suggestion = GetSecuritySuggestion(pattern.Key)
                    });
                }
            }
        }

        await Task.CompletedTask; // For async interface compliance
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

    private string GetSecuritySuggestion(string ruleId)
    {
        return ruleId switch
        {
            "SQL_INJECTION" => "Use parameterized queries or ORM frameworks to prevent SQL injection.",
            "HARDCODED_PASSWORD" => "Store passwords in environment variables or secure configuration.",
            "WEAK_CRYPTO" => "Use SHA-256, SHA-3, or AES for cryptographic operations.",
            "XSS_VULNERABILITY" => "Use textContent instead of innerHTML or sanitize input properly.",
            "INSECURE_HTTP" => "Replace http:// with https:// for secure communication.",
            "EVAL_USAGE" => "Use JSON.parse() for data parsing or alternative safe methods.",
            _ => "Review this code for security implications."
        };
    }
}