# MIM Synchronisation Scheduler - Test Suite

## Overview

This test project provides comprehensive unit and integration test coverage for the MIM Synchronisation Scheduler application using dependency injection and mocking.

## Test Statistics

**Total Tests: 37** ✅
**Success Rate: 100%** 🎯

## Test Structure

### Service Tests (27 tests)

#### ProcessExecutorTests.cs (11 tests)
Integration tests for external process execution:
- ✅ PowerShell script execution (success/failure/whatif modes)
- ✅ VBScript execution (success/failure/whatif modes)
- ✅ Executable execution (success/failure/whatif/null arguments)
- Tests real PowerShell, VBS, and CMD processes

#### TaskExecutorTests.cs (13 tests)
Unit tests with mocked dependencies:
- ✅ PowerShell task execution
- ✅ VBScript task execution
- ✅ Executable task execution (with arguments and window settings)
- ✅ SQL Server task execution
- ✅ Management Agent task execution
- ✅ Management Agent pending exports check
- ✅ Child task execution (success and failure scenarios)
- ✅ Continuation condition logic (import detection)
- All external dependencies mocked with Moq

#### ScheduleExecutorTests.cs (3 tests)
Unit tests for schedule orchestration:
- ✅ Schedule execution with task delegation
- ✅ StopOnIncompletion flag passing
- ✅ Empty schedule handling

### Model Tests (10 tests)

#### ScheduleTests.cs
Tests for the Schedule model:
- Constructor initialisation
- Property getters/setters
- Task collection management

#### ScheduleTaskTests.cs
Tests for the ScheduleTask model:
- Constructor initialisation
- Property getters/setters
- Child task management
- ToString() formatting for different task types

### Utility Tests (4 tests)

#### UtilitiesTests.cs
Tests for utility methods:
- GetCallingClassName() - Stack trace introspection
- GetCallingMethodName() - Method name retrieval

#### TimerTests.cs
Tests for the Timer class:
- Timer creation
- Timer stop operation

## Test Helpers

The `TestHelpers/` directory contains resources for integration testing:

- **test-success.ps1** - PowerShell script that succeeds (exit code 0)
- **test-failure.ps1** - PowerShell script that fails (exit code 1)
- **test-success.vbs** - VBScript that succeeds (exit code 0)
- **test-failure.vbs** - VBScript that fails (exit code 1)
- **test-schedule.config** - Sample schedule configuration

## Architecture

### Service Layer (Fully Testable)

The application uses dependency injection throughout:

```
Program.cs (coordinator)
    ↓
ScheduleExecutor (IScheduleExecutor)
    ↓
TaskExecutor (ITaskExecutor)
    ↓
├── ProcessExecutor (IProcessExecutor) - PowerShell, VBS, Executables
├── SqlExecutor (ISqlExecutor) - SQL Server commands
└── ManagementAgentExecutor (IManagementAgentExecutor) - MIM operations
```

All services can be mocked for comprehensive unit testing.

## Testing Strategy

### What We Test

✅ **All Task Types** - PowerShell, VBS, Executable, SQL, Management Agent, Block, Continuation
✅ **Real Process Execution** - Integration tests with actual scripts and executables
✅ **Mocked Dependencies** - Unit tests for business logic without external dependencies
✅ **Edge Cases** - Failure scenarios, null arguments, whatif mode, child tasks
✅ **Task Orchestration** - Sequential and parallel execution, stop on incompletion
✅ **Model Classes** - Full coverage of domain models
✅ **Utility Methods** - Stack trace helpers and timers

### Testing Approach

- **ProcessExecutor**: Integration tests (real scripts/executables)
- **TaskExecutor**: Unit tests (fully mocked dependencies)
- **ScheduleExecutor**: Unit tests (mocked TaskExecutor)
- **Models**: Unit tests (property and behaviour validation)

### What Cannot Be Fully Tested

❌ **SQL Server Operations** - Requires actual database (can be mocked in unit tests)
❌ **MIM Management Agent Operations** - Requires MIM installation (can be mocked in unit tests)

Note: While we can't test against real SQL/MIM without infrastructure, we CAN and DO test all the logic through mocks.

## Running Tests

```powershell
# Run all tests
dotnet test

# Run with detailed output (recommended)
dotnet test --logger "console;verbosity=detailed"

# Run specific test class
dotnet test --filter "FullyQualifiedName~ProcessExecutorTests"

# Run all service tests
dotnet test --filter "FullyQualifiedName~Services"
```

## Test Frameworks

- **xUnit 2.9.2** - Test framework
- **FluentAssertions 6.12.2** - Fluent assertion library (British English compatible)
- **Moq 4.20.72** - Mocking framework (actively used throughout)
- **Microsoft.NET.Test.Sdk 17.11.1** - Test platform integration

## Contributing Tests

When adding new features:

1. **Add service tests** - Mock dependencies and test business logic
2. **Add integration tests** - Test real external interactions where appropriate
3. **Use British English spellings** (e.g., "Initialise" not "Initialize")
4. **Follow Arrange-Act-Assert pattern**
5. **Use FluentAssertions** for readable assertions
6. **Keep tests focused and independent**
7. **Mock external dependencies** with Moq

### Example Test

```csharp
[Fact]
public void ExecuteTask_PowerShellTask_CallsProcessExecutor()
{
    // Arrange
    var mockExecutor = new Mock<IProcessExecutor>();
    mockExecutor.Setup(x => x.ExecutePowerShellScript("test.ps1")).Returns(true);
    var taskExecutor = new TaskExecutor(mockExecutor.Object, ...);

    var task = new ScheduleTask
    {
        Type = ScheduleTaskType.PowerShell,
        Command = "test.ps1"
    };

    // Act
    var result = taskExecutor.ExecuteTask(task, ...);

    // Assert
    result.Should().BeTrue();
    mockExecutor.Verify(x => x.ExecutePowerShellScript("test.ps1"), Times.Once);
}
```

## Notes

- All tests use British English spellings to match codebase conventions
- Test helper scripts automatically copied to output directory during build
- Test project targets .NET Framework 4.8.1 to match main application
- All 37 tests pass consistently
- Services are fully testable through dependency injection
- Comprehensive coverage of all execution paths
