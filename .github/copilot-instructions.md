# HoneyDrunk.Standards Repository Guidelines

## Project Overview

This repository contains language-agnostic standards for HoneyDrunk Studios (the "Hive"), including:
- .editorconfig and .globalconfig files
- Lint configurations
- Naming conventions and coding rules
- Contribution guidelines and etiquette

This is a .NET 9.0 standards library project that provides shared configuration and standards to be used across multiple repositories in the organization.

## Technology Stack

- **Framework**: .NET 9.0
- **Language**: C#
- **Project Type**: Class Library
- **Features Enabled**: 
  - Implicit Usings
  - Nullable Reference Types

## Coding Standards

### C# Conventions

- Follow Microsoft's C# coding conventions and best practices
- Use nullable reference types appropriately (`enable` is set in the project)
- Leverage implicit usings where appropriate
- Use PascalCase for public members, methods, and classes
- Use camelCase for private fields and local variables
- Prefix private fields with an underscore (e.g., `_fieldName`) when it improves clarity

### Code Organization

- Keep the project structure clean and organized
- Place configuration files (.editorconfig, .globalconfig, etc.) at the appropriate level in the repository
- Standards files should be placed in logical directories that reflect their purpose

### Documentation

- Add XML documentation comments for public APIs
- Include clear README.md files for any new standards or configurations added
- Document the purpose and usage of configuration files
- Update documentation when making changes to existing standards

## Build and Testing

### Building the Project

- Build the solution using `dotnet build` from the HoneyDrunk.Standards directory
- The project targets .NET 9.0
- Ensure builds succeed before committing changes

### Testing

- If adding new standards or configurations, test them in a sample project first
- Verify that configuration changes work as expected across different scenarios

## File Management

### Ignore Patterns

- Follow the existing .gitignore patterns
- Do not commit build artifacts (bin/, obj/ directories)
- Do not commit user-specific IDE files (.suo, .user, etc.)
- Do not commit environment files (*.env)

### File Naming

- Use PascalCase for C# files matching class names
- Use kebab-case for configuration files (e.g., .editorconfig)
- Use descriptive names that clearly indicate the file's purpose

## Contribution Guidelines

### Making Changes

- Keep changes focused and minimal
- Test configuration changes before committing
- Follow the existing patterns in the repository
- Update documentation when adding or modifying standards

### Code Reviews

- All changes should be reviewed before merging
- Ensure changes align with the repository's purpose of providing standards
- Verify that new standards are clear, well-documented, and useful

### Commit Messages

- Write clear, concise commit messages
- Use present tense ("Add feature" not "Added feature")
- Start with a verb (Add, Update, Fix, Remove, etc.)
- Keep the first line under 50 characters when possible

## Special Considerations

### Standards Philosophy

- Standards should be opinionated but reasonable
- Aim for consistency across the Hive ecosystem
- Balance strictness with flexibility
- Consider the impact on different types of projects

### Compatibility

- Ensure standards work with modern versions of tools and IDEs
- Test compatibility with Visual Studio, VS Code, and JetBrains Rider
- Consider cross-platform development scenarios

## Questions or Issues

When working on issues:
- Clearly understand the requirement before making changes
- Ask for clarification if the issue description is ambiguous
- Consider the broader impact of standards changes on the organization
- Test thoroughly before considering a task complete
