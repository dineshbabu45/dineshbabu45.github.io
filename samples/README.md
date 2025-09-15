# Sample Code Files for PR Agent Testing

This directory contains sample code files with intentional issues to demonstrate and test the AI Pull Request Review Agent's detection capabilities.

## Files Overview

### Secure Examples
- **SecureCode.cs** - Demonstrates proper coding practices and security measures

### Vulnerable Examples (For Testing PR Agent)
- **VulnerableCode.cs** - Original sample with SQL injection, hardcoded passwords, and quality issues
- **ProblematicPaymentService.cs** - C# payment service with multiple security and quality issues
- **problematic-user-controller.js** - JavaScript/Node.js user controller with various vulnerabilities
- **problematic_data_processor.py** - Python data processor with security and style issues
- **ProblematicJavaService.java** - Java service class with multiple code quality problems

## Issues Demonstrated

### Security Issues
- **SQL Injection** - Direct string concatenation in SQL queries
- **Hardcoded Credentials** - API keys, passwords, and encryption keys in source code
- **Weak Cryptography** - Use of deprecated algorithms like MD5, DES
- **Insecure HTTP** - Using HTTP instead of HTTPS for sensitive communications
- **XSS Vulnerabilities** - Unescaped user input in web output
- **Insecure Random Generation** - Predictable random number generation

### Quality Issues
- **Magic Numbers** - Hardcoded numeric values without explanation
- **Empty Catch Blocks** - Poor error handling practices
- **Complex Conditions** - Overly complex conditional logic
- **Long Methods** - Methods that try to do too much
- **TODO Comments** - Unfinished work markers
- **Code Duplication** - Repeated code patterns
- **Too Many Parameters** - Methods with excessive parameter counts

### Style Issues
- **Poor Formatting** - Inconsistent spacing, indentation, and brace placement
- **Long Lines** - Lines exceeding recommended length limits
- **Naming Conventions** - Variables and methods not following language conventions
- **Missing Documentation** - Lack of proper comments and documentation

## Testing the PR Agent

These files are designed to trigger the AI Pull Request Review Agent when included in pull requests. The agent should detect and report on the various issues present in each file.

To test:
1. Create a pull request that includes changes to these files
2. The GitHub Actions workflow will automatically run the AI review
3. Check the PR comments for detected issues and suggestions

## Languages Supported

The samples demonstrate issues across multiple programming languages:
- C# (.cs files)
- JavaScript/Node.js (.js files)  
- Python (.py files)
- Java (.java files)

This helps test the PR agent's multi-language analysis capabilities.

## Note

⚠️ **Important**: These files contain intentionally vulnerable and problematic code for demonstration purposes only. Do NOT use any of these patterns in production code.