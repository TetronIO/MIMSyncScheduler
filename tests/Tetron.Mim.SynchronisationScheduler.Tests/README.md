# MIM Synchronisation Scheduler - Test Suite

## Overview

This test project provides unit test coverage for the MIM Synchronisation Scheduler application.

## Test Structure

### Current Test Coverage

#### Model Tests
- **ScheduleTests.cs** - Tests for the Schedule model
  - Constructor initialisation
  - Property getters/setters
  - Task collection management

- **ScheduleTaskTests.cs** - Tests for the ScheduleTask model
  - Constructor initialisation
  - Property getters/setters
  - Child task management
  - ToString() formatting for different task types

#### Utility Tests
- **UtilitiesTests.cs** - Tests for utility methods
  - GetCallingClassName() - Stack trace introspection
  - GetCallingMethodName() - Method name retrieval

- **TimerTests.cs** - Tests for the Timer class
  - Timer creation
  - Timer stop operation

### Test Helpers

The `TestHelpers/` directory contains resources for integration testing:

- **test-success.ps1** - PowerShell script that succeeds (exit code 0)
- **test-failure.ps1** - PowerShell script that fails (exit code 1)
- **test-success.vbs** - VBScript that succeeds (exit code 0)
- **test-failure.vbs** - VBScript that fails (exit code 1)
- **test-schedule.config** - Sample schedule configuration for integration tests

## Testing Strategy

### What We Test

✅ **Model Classes** - Full coverage of Schedule and ScheduleTask models
✅ **Utility Methods** - Stack trace helpers and timer functionality
✅ **Basic Functionality** - Object initialisation and property management

### What We Cannot Currently Test

❌ **SQL Server Operations** - Requires database server connection
❌ **MIM Management Agent Operations** - Requires MIM installation
❌ **Full Schedule Execution** - Static Program class design limits testability

### Future Testing Improvements

To enable comprehensive testing of all task types, the following refactoring would be required:

1. **Dependency Injection** - Extract static methods from Program.cs into injectable services
2. **Interface Abstraction** - Use ISqlExecutor and IManagementAgentExecutor interfaces (already created)
3. **Service Classes** - Create:
   - `ScheduleExecutor` class for schedule execution logic
   - `TaskExecutor` class for individual task execution
   - `SqlExecutor` class implementing ISqlExecutor
   - `ManagementAgentExecutor` class implementing IManagementAgentExecutor

4. **Mock-based Testing** - Use Moq to mock external dependencies

## Running Tests

```powershell
# Run all tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test class
dotnet test --filter "FullyQualifiedName~ScheduleTests"
```

## Test Frameworks

- **xUnit 2.9.2** - Test framework
- **FluentAssertions 6.12.2** - Fluent assertion library (British English compatible)
- **Moq 4.20.72** - Mocking framework (for future use)
- **Microsoft.NET.Test.Sdk** - Test platform integration

## Contributing Tests

When adding new features:

1. Add corresponding unit tests for any new models or utility methods
2. Use British English spellings (e.g., "Initialise" not "Initialize")
3. Follow the Arrange-Act-Assert pattern
4. Use FluentAssertions for readable assertions
5. Keep tests focused and independent

## Notes

- All tests use British English spellings to match the codebase conventions
- Test helper scripts are automatically copied to the output directory during build
- The test project targets .NET Framework 4.8.1 to match the main application
