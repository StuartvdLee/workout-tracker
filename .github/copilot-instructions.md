# GitHub Copilot Instructions

## Project Overview
This is the Workout Tracker project - an application for tracking and managing workout routines and fitness progress.

## Code Style & Standards

### General Guidelines
- Write clean, readable code with meaningful variable and function names
- Use descriptive comments for complex logic
- Follow the existing project structure and patterns
- Keep functions focused and single-responsibility

### Language-Specific Standards
- Use consistent indentation (2 spaces for YAML, 4 spaces for Python if applicable)
- Add type hints where applicable
- Write tests for new features and bug fixes

## Naming Conventions
- Use camelCase for variables and functions
- Use PascalCase for classes and components
- Use UPPER_SNAKE_CASE for constants
- Prefix private methods with underscore (_)

## Git Workflow
- Create feature branches from `main`
- Write clear, descriptive commit messages
- Reference related issues in commit messages and PRs
- Keep commits atomic and logical

## Documentation
- Update README.md for user-facing changes
- Add inline comments for non-obvious code
- Document new functions and their parameters
- Include examples for complex functionality

## Testing
- Write unit tests for new functions
- Aim for meaningful test coverage
- Test edge cases and error conditions
- Keep tests isolated and independent

## Performance & Security
- Avoid unnecessary computations in loops
- Use appropriate data structures for the use case
- Sanitize user inputs
- Don't commit sensitive information (API keys, passwords)

## Code Review Guidelines

### General Code Review Standards
- Review for readability, maintainability, and adherence to project standards
- Check that code follows naming conventions and style guidelines
- Verify that complex logic includes appropriate comments
- Ensure no commented-out code or debug statements remain
- Validate that error handling is appropriate for the change
- Check for potential performance impacts or N+1 query issues
- Confirm tests are included and cover the new functionality

### Security Review
- For code changes involving authentication, authorization, data handling, or external APIs, request a security review using the **SE: Security** agent
- The security agent reviews against OWASP Top 10, Zero Trust principles, and LLM security standards
- Priority areas for security review:
  - Authentication and access control logic
  - API endpoints handling sensitive data
  - External service integrations
  - User input validation and sanitization
  - Cryptographic operations

## Pull Request Standards
- Keep PRs focused on a single feature or fix
- Provide clear descriptions of changes
- Link related issues
- Ensure all tests pass before requesting review
