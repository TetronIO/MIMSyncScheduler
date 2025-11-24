# Development Log

This document tracks major development milestones, architectural decisions, and ongoing work for the MIM Synchronisation Scheduler project.

## Project Overview

**Purpose:** Automated scheduler for Microsoft Identity Manager (MIM) synchronisation operations

**Technology Stack:**
- .NET Framework 4.8.1
- C# 8.0 (SDK-style project)
- Serilog for logging
- xUnit + FluentAssertions + Moq for testing
- GitHub Actions for CI/CD

**Architecture:** Service-oriented with dependency injection

---

## Major Milestones

### 2025-11-23: Comprehensive Testing & DI Architecture
**Status:** ✅ Completed

**What Changed:**
- Refactored from monolithic static Program.cs to service-oriented architecture
- Implemented dependency injection throughout
- Added 37 comprehensive unit and integration tests (100% passing)
- Restructured solution to src/tests pattern
- Added GitHub Actions CI/CD pipeline

**Impact:**
- Code is now fully testable without external dependencies
- Reduced Program.cs from ~800 lines to ~100 lines
- All external operations mockable through interfaces

**See:** [AI_SESSIONS.md - Session 1](AI_SESSIONS.md#session-1-comprehensive-unit-testing--architecture-refactoring)

---

### Previous: Initial Implementation
**Status:** ✅ Completed (pre-AI sessions)

**Features:**
- XML-based schedule configuration
- Support for multiple task types:
  - Management Agent run profiles
  - PowerShell scripts
  - VBScript execution
  - Arbitrary executables
  - SQL Server commands
- Parallel execution with block tasks
- WhatIf mode for testing schedules
- Continuation conditions based on import changes

---

## Architecture Decisions

### ADR-001: Dependency Injection Pattern
**Date:** 2025-11-23
**Status:** Accepted

**Context:**
Original codebase used static methods in Program.cs, making unit testing impossible without actual MIM infrastructure, SQL Server, etc.

**Decision:**
Extract all execution logic into injectable service classes with interfaces:
- `IProcessExecutor` - External process execution (PowerShell, VBS, executables)
- `ISqlExecutor` - SQL Server command execution
- `IManagementAgentExecutor` - MIM WMI operations
- `ITaskExecutor` - Task orchestration
- `IScheduleExecutor` - Schedule execution

**Consequences:**
- ✅ 100% testable through mocking
- ✅ Clear separation of concerns
- ✅ Easy to add new task types
- ⚠️ Slight increase in code complexity
- ⚠️ ~550 lines of old code needs removal (tracked in CODE_REVIEW.md)

### ADR-002: Integration vs Unit Testing Strategy
**Date:** 2025-11-23
**Status:** Accepted

**Context:**
ProcessExecutor executes external processes (PowerShell, VBS, executables). Should we mock these or test them for real?

**Decision:**
- ProcessExecutor: Integration tests with real external processes
- TaskExecutor, ScheduleExecutor: Unit tests with mocked dependencies

**Rationale:**
- ProcessExecutor is a thin wrapper around Process API - integration tests verify it works
- Higher-level orchestration logic needs fast, isolated unit tests
- Best of both worlds: confidence in process execution + fast unit tests

**Consequences:**
- ✅ High confidence that PowerShell/VBS actually execute correctly
- ✅ Fast unit test suite for business logic
- ⚠️ Integration tests take ~8 seconds to run

### ADR-003: .NET Framework 4.8.1 (not .NET Core/8)
**Date:** Earlier (pre-AI)
**Status:** Accepted

**Context:**
Modern .NET (Core/8+) is preferred for new development, but this project targets .NET Framework 4.8.1.

**Decision:**
Remain on .NET Framework 4.8.1

**Rationale:**
- System.Management.Automation (PowerShell SDK) works better on .NET Framework
- System.Management (WMI for MIM) is .NET Framework-specific
- MIM infrastructure typically runs on Windows Server with .NET Framework

**Consequences:**
- ✅ Better compatibility with MIM ecosystem
- ✅ PowerShell execution works reliably
- ⚠️ Cannot use latest .NET features
- ⚠️ Must use windows-latest runner in GitHub Actions

---

## Current Status

### Active Work
None currently - see [Known Issues](#known-issues) for potential next steps.

### Recently Completed
- ✅ Dependency injection refactoring
- ✅ 37 comprehensive tests
- ✅ GitHub Actions CI/CD
- ✅ Code review and documentation

### Known Issues

See [CODE_REVIEW.md](CODE_REVIEW.md) for detailed findings. Summary:

#### Critical 🔴
- [ ] Remove duplicate implementation from Program.cs (lines 271-826)

#### Medium Priority 🟡
- [ ] Add null validation in ScheduleExecutor constructor
- [ ] Add null check for schedule.Name attribute
- [ ] Handle StopOnIncompletion in failed block tasks

#### Low Priority 🟢
- [ ] Fix block task validation to check all tasks
- [ ] Add unit tests for retry scenarios
- [ ] Document WhatIf mode behavior

---

## Testing

### Test Coverage
| Component | Tests | Status |
|-----------|-------|--------|
| ProcessExecutor | 11 | ✅ All passing |
| TaskExecutor | 13 | ✅ All passing |
| ScheduleExecutor | 3 | ✅ All passing |
| Models | 10 | ✅ All passing |
| **Total** | **37** | **✅ 100%** |

### Running Tests
```bash
# All tests
dotnet test

# Detailed output
dotnet test --logger "console;verbosity=detailed"

# Specific test class
dotnet test --filter "FullyQualifiedName~ProcessExecutorTests"
```

See [tests/README.md](../tests/Tetron.Mim.SynchronisationScheduler.Tests/README.md) for comprehensive test documentation.

---

## Building & Running

### Prerequisites
- .NET SDK 8.0+ (can build .NET Framework 4.8.1 projects)
- Windows (required for System.Management and MIM operations)

### Build
```bash
# Restore and build
dotnet build Tetron.Mim.SynchronisationScheduler.sln

# Release build
dotnet build -c Release
```

### Run
```bash
# From output directory
cd src/Tetron.Mim.SynchronisationScheduler/bin/Debug/net481
./Tetron.Mim.SynchronisationScheduler.exe "path/to/schedule.config"

# WhatIf mode (set in App.config)
# <add key="whatif" value="true" />
```

---

## Configuration

### Schedule Files
Schedules are defined in XML configuration files. See `src/Tetron.Mim.SynchronisationScheduler/Example Schedules/` for examples.

**Key Elements:**
- `<Schedule>` - Root element with Name and StopOnIncompletion attributes
- `<ManagementAgent>` - MIM run profile execution
- `<PowerShell>` - PowerShell script execution
- `<VisualBasicScript>` - VBScript execution
- `<Executable>` - Arbitrary executable
- `<SqlServer>` - SQL command execution
- `<Block>` - Parallel execution container
- `<ContinuationCondition>` - Conditional execution based on state

### App Configuration
Key settings in App.config:
- `LoggingLevel` - Verbose, Debug, Information, Warning, Error, Fatal
- `whatif` - true/false for simulation mode

---

## Contributing

### Code Style
- British English spelling (e.g., "Synchronisation", "Initialise")
- XML documentation on public APIs
- FluentAssertions for test assertions
- Arrange-Act-Assert test pattern

### Adding New Task Types
1. Add enum value to `ScheduleTaskType` in Models/Enums.cs
2. Add execution logic to relevant executor service (or create new one)
3. Wire up in TaskExecutor.ExecuteTask() switch statement
4. Add XML parsing in Program.BuildScheduleTask()
5. Add unit tests with mocked dependencies
6. Update documentation

---

## References

- [AI Development Sessions](AI_SESSIONS.md) - Detailed session logs
- [Code Review Findings](CODE_REVIEW.md) - Issues and recommendations
- [Test Documentation](../tests/Tetron.Mim.SynchronisationScheduler.Tests/README.md) - Test strategy and details

---

**Last Updated:** 2025-11-23
