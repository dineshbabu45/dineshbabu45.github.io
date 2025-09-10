# Pull Request Review Agent

An AI-powered tool for automatically reviewing code changes in pull requests. This agent analyzes code for quality, security, and style issues, providing intelligent feedback to help maintain code quality and security standards.

## Features

### 🔒 Security Analysis
- **SQL Injection Detection**: Identifies potential SQL injection vulnerabilities
- **Hardcoded Credentials**: Detects hardcoded passwords and sensitive data
- **Weak Cryptography**: Flags use of weak cryptographic algorithms
- **XSS Vulnerabilities**: Identifies potential cross-site scripting issues
- **Insecure Protocols**: Detects use of insecure HTTP connections

### ⚡ Quality Analysis
- **Code Complexity**: Identifies overly complex methods and conditions
- **Magic Numbers**: Flags hardcoded numeric values
- **Empty Catch Blocks**: Detects poor error handling practices
- **Code Duplication**: Identifies duplicate code patterns
- **TODO Comments**: Tracks incomplete work

### 🎨 Style Analysis
- **Formatting Issues**: Detects inconsistent indentation and spacing
- **Line Length**: Identifies overly long lines
- **Naming Conventions**: Checks adherence to naming standards
- **Code Structure**: Ensures consistent brace placement and formatting

## Quick Start

### Prerequisites
- .NET 8.0 SDK
- GitHub personal access token (for GitHub integration)

### Installation

1. Clone the repository:
```bash
git clone https://github.com/dineshbabu45/dineshbabu45.github.io.git
cd dineshbabu45.github.io
```

2. Build the solution:
```bash
dotnet build
```

3. Run tests:
```bash
dotnet test
```

### Usage

#### Command Line
```bash
# Basic analysis (output to console)
dotnet run --project src/PullRequestReviewAgent.Core owner repo pr-number

# Post comments to GitHub
export GITHUB_TOKEN="your-github-token"
dotnet run --project src/PullRequestReviewAgent.Core owner repo pr-number --post-comments
```

#### GitHub Actions

Add the provided workflow file (`.github/workflows/ai-pr-review.yml`) to your repository to automatically review all pull requests.

### Configuration

Create an `appsettings.json` file in the Core project directory:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "Analysis": {
    "MaxFileSizeBytes": 1048576,
    "SupportedFileExtensions": [".cs", ".js", ".ts", ".py", ".java"],
    "ExcludedDirectories": ["node_modules", "bin", "obj", ".git"]
  }
}
```

## Examples

### Sample Analysis Output

```
=== ANALYSIS RESULTS ===
Total Issues: 12
Critical: 2, Errors: 3, Warnings: 5, Info: 2
Files Analyzed: 3, Lines: 245, Duration: 156ms

Critical Issues (2):
  VulnerableCode.cs:13 - Potential SQL Injection
    String concatenation in SQL queries can lead to SQL injection vulnerabilities.
    💡 Use parameterized queries or ORM frameworks to prevent SQL injection.

  VulnerableCode.cs:7 - Hardcoded Password
    Hardcoded passwords in source code pose security risks.
    💡 Store passwords in environment variables or secure configuration.
```

### GitHub Integration

When integrated with GitHub Actions, the agent will:
1. Automatically analyze all changed files in pull requests
2. Post a comprehensive review summary
3. Add inline comments on specific issues
4. Provide actionable suggestions for improvements

## Architecture

### Components

- **Analysis Engine**: Core analysis algorithms for different issue types
- **GitHub Integration**: API client for fetching PR data and posting comments
- **Extensible Analyzers**: Modular analyzer system for different code quality aspects
- **Reporting System**: Formatted output and GitHub comment generation

### Supported Languages

Currently optimized for:
- C#
- JavaScript/TypeScript
- Python
- Java
- C/C++

Easily extensible to support additional languages.

## Development

### Project Structure

```
├── src/
│   ├── PullRequestReviewAgent.Core/     # Main application
│   └── PullRequestReviewAgent.Analysis/ # Analysis engine
├── tests/
│   └── PullRequestReviewAgent.Tests/    # Unit tests
├── samples/                             # Example code files
├── .github/workflows/                   # GitHub Actions
└── docs/                               # Documentation
```

### Adding New Analyzers

1. Implement the `ICodeAnalyzer` interface:

```csharp
public class CustomAnalyzer : ICodeAnalyzer
{
    public string Name => "Custom Analyzer";
    public IEnumerable<IssueCategory> SupportedCategories => new[] { IssueCategory.Quality };

    public async Task<List<CodeIssue>> AnalyzeFileAsync(string fileName, string content)
    {
        // Implement your analysis logic
        return issues;
    }
}
```

2. Register the analyzer in `Program.cs`:

```csharp
services.AddTransient<ICodeAnalyzer, CustomAnalyzer>();
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test category
dotnet test --filter SecurityAnalyzerTests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality
4. Ensure all tests pass
5. Submit a pull request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Roadmap

- [ ] Machine learning-based issue detection
- [ ] Integration with more version control systems
- [ ] Advanced security vulnerability detection
- [ ] Custom rule configuration
- [ ] Performance analysis capabilities
- [ ] Code metrics and technical debt tracking

## Support

For issues and feature requests, please use the [GitHub Issues](https://github.com/dineshbabu45/dineshbabu45.github.io/issues) page.

---

**Note**: This is a demonstration project showcasing AI-powered code review capabilities. For production use, consider additional security measures and thorough testing in your specific environment.