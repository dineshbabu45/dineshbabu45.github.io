#!/bin/bash

# Demonstration script for AI-powered PR review agent
# This script shows how to use the tool with both AI and manual analyzers

echo "=== Pull Request Review Agent Demonstration ==="
echo

# Check if .NET is installed
if ! command -v dotnet &> /dev/null; then
    echo "Error: .NET 8.0 SDK is required but not found."
    echo "Please install .NET 8.0 SDK from: https://dotnet.microsoft.com/download"
    exit 1
fi

# Build the solution
echo "Building the solution..."
dotnet build --configuration Release
if [ $? -ne 0 ]; then
    echo "Build failed. Please check the errors above."
    exit 1
fi

echo "Build successful!"
echo

# Run tests
echo "Running tests..."
dotnet test --configuration Release --verbosity quiet
if [ $? -ne 0 ]; then
    echo "Tests failed. Please check the errors above."
    exit 1
fi

echo "All tests passed!"
echo

# Display usage information
echo "=== Usage Examples ==="
echo

echo "1. Manual Analysis (No API key required):"
echo "   export GITHUB_TOKEN='your-github-token'"
echo "   dotnet run --project src/PullRequestReviewAgent.Core owner repo pr-number"
echo

echo "2. AI-Powered Analysis (Requires OpenAI API key):"
echo "   export GITHUB_TOKEN='your-github-token'"
echo "   export OPENAI_API_KEY='your-openai-api-key'"
echo "   dotnet run --project src/PullRequestReviewAgent.Core owner repo pr-number --use-ai"
echo

echo "3. Post Comments to GitHub:"
echo "   dotnet run --project src/PullRequestReviewAgent.Core owner repo pr-number --post-comments --use-ai"
echo

echo "=== Configuration ==="
echo "The application can be configured via:"
echo "- Environment variables (GITHUB_TOKEN, OPENAI_API_KEY)"
echo "- Configuration file (src/PullRequestReviewAgent.Core/appsettings.json)"
echo "- Command line arguments"
echo

echo "=== Available Analyzers ==="
echo "- AI Code Analyzer: Uses OpenAI GPT models for intelligent analysis"
echo "- Security Analyzer: Rule-based security vulnerability detection"
echo "- Quality Analyzer: Code quality and best practices"
echo "- Style Analyzer: Code formatting and style guidelines"
echo

echo "=== GitHub Actions Integration ==="
echo "To use with GitHub Actions:"
echo "1. Copy .github/workflows/ai-pr-review.yml to your repository"
echo "2. Add OPENAI_API_KEY as a repository secret"
echo "3. The workflow will automatically analyze all pull requests"
echo

echo "Demonstration complete! Ready to analyze pull requests."