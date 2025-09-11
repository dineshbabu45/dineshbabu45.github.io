# Pull Request Review Agent

An AI-powered tool for automatically reviewing code changes in pull requests. This agent uses artificial intelligence (LLM) to analyze code for quality, security, and style issues, providing intelligent feedback to help maintain code quality and security standards.

## Features

### 🤖 AI-Powered Analysis
- **Intelligent Code Review**: Uses advanced language models (GPT-4) to analyze code for various issues
- **Automatic Issue Detection**: No need to manually add analyzers for each type of issue
- **Comprehensive Coverage**: Identifies security, quality, style, performance, maintainability, and reliability issues
- **Contextual Suggestions**: Provides intelligent suggestions and fixes for identified issues
- **Multi-Language Support**: Works with various programming languages automatically

### 🔒 Security Analysis
- **SQL Injection Detection**: Identifies potential SQL injection vulnerabilities
- **Hardcoded Credentials**: Detects hardcoded passwords and sensitive data
- **Weak Cryptography**: Flags use of weak cryptographic algorithms
- **XSS Vulnerabilities**: Identifies potential cross-site scripting issues
- **Insecure Protocols**: Detects use of insecure HTTP connections
- **Input Validation**: Checks for proper input validation and sanitization

### ⚡ Quality Analysis
- **Code Complexity**: Identifies overly complex methods and conditions
- **Magic Numbers**: Flags hardcoded numeric values
- **Empty Catch Blocks**: Detects poor error handling practices
- **Code Duplication**: Identifies duplicate code patterns
- **TODO Comments**: Tracks incomplete work
- **Logic Errors**: Detects potential bugs and logical issues

### 🎨 Style Analysis
- **Formatting Issues**: Detects inconsistent indentation and spacing
- **Line Length**: Identifies overly long lines
- **Naming Conventions**: Checks adherence to naming standards
- **Code Structure**: Ensures consistent brace placement and formatting
- **Documentation**: Checks for missing or inadequate documentation

### 🚀 Performance Analysis
- **Inefficient Algorithms**: Identifies performance bottlenecks
- **Memory Usage**: Detects potential memory leaks or excessive usage
- **Database Queries**: Flags inefficient or unnecessary database operations
- **Async/Await**: Checks for proper asynchronous programming patterns

## Quick Start

### Prerequisites
- .NET 8.0 SDK
- GitHub personal access token (for GitHub integration)
- OpenAI API key (for AI-powered analysis) - Get one from [OpenAI Platform](https://platform.openai.com/api-keys)

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

**AI-Powered Analysis (Recommended):**
```bash
# Set up your API keys
export GITHUB_TOKEN="your-github-token"
export OPENAI_API_KEY="your-openai-api-key"

# Run AI-powered analysis (output to console)
dotnet run --project src/PullRequestReviewAgent.Core owner repo pr-number --use-ai

# Post AI analysis comments to GitHub
dotnet run --project src/PullRequestReviewAgent.Core owner repo pr-number --post-comments --use-ai
```

**Manual Rule-Based Analysis (Fallback):**
```bash
# Basic analysis with manual analyzers (output to console)
dotnet run --project src/PullRequestReviewAgent.Core owner repo pr-number

# Post comments to GitHub with manual analyzers
export GITHUB_TOKEN="your-github-token"
dotnet run --project src/PullRequestReviewAgent.Core owner repo pr-number --post-comments
```

#### GitHub Actions

Add the provided workflow file (`.github/workflows/ai-pr-review.yml`) to your repository to automatically review all pull requests with AI.

**Setup Instructions:**
1. Add the workflow file to your repository
2. Add your OpenAI API key as a repository secret named `OPENAI_API_KEY`
3. The workflow will automatically trigger on pull request events

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
  },
  "AIAnalysis": {
    "OpenAIApiKey": "your-openai-api-key",
    "Model": "gpt-4o-mini",
    "CodingGuidelinesUrl": "https://your-company.com/coding-guidelines",
    "MaxFileSizeBytes": 50000,
    "EnableBatchAnalysis": true
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

- **AI Analysis Engine**: Core AI-powered analysis using OpenAI's language models
- **Manual Analyzers**: Fallback rule-based analyzers for specific issue types
- **GitHub Integration**: API client for fetching PR data and posting comments
- **Extensible Architecture**: Modular analyzer system supporting both AI and manual analyzers
- **Reporting System**: Formatted output and GitHub comment generation

### Analysis Modes

#### AI-Powered Mode (Recommended)
- Uses OpenAI's GPT models for intelligent code analysis
- Automatically detects all types of issues without manual rules
- Provides contextual suggestions and explanations
- Supports external coding guidelines integration
- Works with any programming language

#### Manual Mode (Fallback)
- Rule-based analyzers with predefined patterns
- Faster execution but limited coverage
- Works without external API dependencies
- Supports offline analysis

### Supported Languages

**AI-Powered Analysis:**
- All major programming languages supported by GPT models
- Automatic language detection and appropriate analysis
- No manual configuration required per language

**Manual Analysis (Fallback):**
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

#### AI-Powered Approach (Recommended)
No need to add individual analyzers! The AI analyzer automatically handles all types of issues. To customize:

1. Add your coding guidelines to the configuration:
```json
{
  "AIAnalysis": {
    "CodingGuidelinesUrl": "https://your-company.com/guidelines"
  }
}
```

2. The AI will automatically incorporate your guidelines into the analysis.

#### Manual Analyzer Approach (Advanced)
For specific use cases, you can still add manual analyzers:

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

- [x] AI-powered code analysis using LLM
- [x] Automatic issue detection without manual rules
- [x] Integration with external coding guidelines
- [x] Multi-language support through AI
- [ ] Machine learning-based issue ranking
- [ ] Integration with more version control systems
- [ ] Advanced security vulnerability detection with AI
- [ ] Custom AI prompt configuration
- [ ] Performance analysis and optimization suggestions
- [ ] Code metrics and technical debt tracking with AI insights

## Support

For issues and feature requests, please use the [GitHub Issues](https://github.com/dineshbabu45/dineshbabu45.github.io/issues) page.

---

**Note**: This is a demonstration project showcasing AI-powered code review capabilities. For production use, consider additional security measures and thorough testing in your specific environment.