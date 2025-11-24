# MIMSyncScheduler

[![Tests](https://img.shields.io/badge/tests-37%20passing-success)](tests/Tetron.Mim.SynchronisationScheduler.Tests/)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8.1-blue)](https://dotnet.microsoft.com/download/dotnet-framework)
[![Build](https://github.com/TetronIO/MIMSyncScheduler/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/TetronIO/MIMSyncScheduler/actions)

An intelligent, battle-tested scheduler for Microsoft Identity Manager (MIM) 2016 synchronisation operations with support for parallel execution, conditional logic, and comprehensive task orchestration.

Originally written in 2013 and recently modernised with dependency injection architecture and comprehensive test coverage (37 tests, 100% passing). Battle-tested over the years against some of the largest MIM solutions.

---

## Table of Contents

- [Features](#features)
- [Quick Start](#quick-start)
- [Prerequisites](#prerequisites)
- [Installation & Building](#installation--building)
- [Usage](#usage)
- [Configuration](#configuration)
- [Task Types](#task-types)
- [Examples](#examples)
- [Testing](#testing)
- [Troubleshooting](#troubleshooting)
- [Architecture](#architecture)
- [Documentation](#documentation)
- [Contributing](#contributing)
- [License](#license)

---

## Features

### Core Capabilities
- **MIM Synchronisation run profiles** - Execute any MIM Management Agent run profile
- **Executables** - Run arbitrary programs with arguments
- **SQL Scripts** - Execute SQL Server commands
- **PowerShell scripts** - Run PowerShell with full output capture
- **VBScripts** - Execute Visual Basic scripts

### Intelligent Optimisation
- **Conditional execution** - Only run synchronisation if there are pending imports
- **Smart exports** - Only run export operations if there are pending exports
- **Parallel execution** - Run independent tasks simultaneously (imports/exports)
- **Continuation conditions** - Stop processing if no changes detected upstream

### Operational Features
- **What-If mode** - Test schedules without executing actual operations
- **Comprehensive logging** - Serilog with rolling file logs and configurable verbosity
- **Retry logic** - Automatic retry on SQL deadlock responses from MIM
- **Windows Scheduled Task integration** - Run on schedules via Task Scheduler
- **Command-line operation** - Run manually with full console output

---

## Quick Start

**1. Build the project:**
```powershell
dotnet build Tetron.Mim.SynchronisationScheduler.sln -c Release
```

**2. Create a simple schedule file** (`my-schedule.config`):
```xml
<?xml version="1.0" encoding="utf-8" ?>
<Schedule Name="Simple Test" StopOnIncompletion="true">
  <ManagementAgent Name="AD MA" RunProfile="FISO" Enabled="true">
    <ManagementAgent Name="AD MA" RunProfile="FS" Enabled="true" />
  </ManagementAgent>
</Schedule>
```

**3. Run in What-If mode** (edit `App.config` first - see [Configuration](#configuration)):
```powershell
cd src\Tetron.Mim.SynchronisationScheduler\bin\Release\net481
.\Tetron.Mim.SynchronisationScheduler.exe "C:\path\to\my-schedule.config"
```

**4. Check logs** in `logs\scheduler-YYYY-MM-DD.log`

See [Example Schedules](src/Tetron.Mim.SynchronisationScheduler/Example%20Schedules/) for comprehensive examples.

---

## Prerequisites

### Required
- **Windows Server** (tested on Windows Server 2016+)
- **.NET Framework 4.8.1** runtime
- **MIM 2016** (or MIM 2019 - compatible)
- **Administrator privileges** (for WMI access to MIM)

### For Development
- **.NET SDK 8.0+** (can build .NET Framework 4.8.1 projects)
- **Visual Studio 2022** or **VS Code** (optional)
- **Git** (for cloning repository)

### Permissions Required
The service account running the scheduler needs:
- **Local Administrator** on the MIM Synchronisation Server (for WMI access)
- **SQL Server access** (Windows Authentication) if using SqlServer tasks
- **File system access** to PowerShell/VBScript paths
- **Execute permissions** for any referenced executables

---

## Installation & Building

### Clone Repository
```powershell
git clone https://github.com/TetronIO/MIMSyncScheduler.git
cd MIMSyncScheduler
```

### Build from Source
```powershell
# Restore dependencies
dotnet restore

# Build (Debug)
dotnet build

# Build (Release)
dotnet build -c Release
```

### Output Location
```
src\Tetron.Mim.SynchronisationScheduler\bin\Release\net481\
├── Tetron.Mim.SynchronisationScheduler.exe
├── App.config
└── Example Schedules\
```

### Run Tests
```powershell
# All 37 tests
dotnet test

# Detailed output
dotnet test --logger "console;verbosity=detailed"

# Specific test class
dotnet test --filter "FullyQualifiedName~TaskExecutorTests"
```

See [Testing](#testing) for more details.

---

## Usage

### Command Line
```powershell
.\Tetron.Mim.SynchronisationScheduler.exe "<path-to-schedule-file>"
```

**Example:**
```powershell
.\Tetron.Mim.SynchronisationScheduler.exe "C:\MIM\Schedules\delta-sync.config"
```

### Windows Scheduled Task
1. Create a new Scheduled Task in Task Scheduler
2. **Action:** Start a program
3. **Program:** `C:\Path\To\Tetron.Mim.SynchronisationScheduler.exe`
4. **Arguments:** `"C:\MIM\Schedules\delta-sync.config"`
5. **Run as:** Service account with required permissions
6. **Schedule:** As needed (e.g., every 15 minutes for delta sync)

**Tip:** Create multiple scheduled tasks for different schedule files (e.g., delta sync every 15 minutes, full sync weekly).

### Logs
Logs are written to:
```
logs\scheduler-YYYY-MM-DD.log
```

Rolling logs create a new file each day and retain history based on Serilog defaults.

---

## Configuration

### App.config

Program behaviour is customised via `App.config` settings:

| Key | Values | Default | Description |
| --- | ------ | ------- | ----------- |
| `LoggingLevel` | `Verbose`, `Debug`, `Information`, `Warning`, `Error`, `Fatal` | `Information` | Controls log verbosity. Use `Debug` for development, `Information` or `Warning` for production. |
| `whatif` | `true`, `false` | `false` | **What-If mode** - When `true`, executes the schedule logic and creates logs but **does not perform actual operations**. Essential for testing schedules safely. |

**Example App.config:**
```xml
<appSettings>
  <add key="LoggingLevel" value="Information" />
  <add key="whatif" value="false" />
</appSettings>
```

### Schedule Files

Schedule files are XML documents defining tasks and execution order. Multiple schedule files enable different sync strategies (delta vs full, different systems, etc.).

**Key Concepts:**
- **Sequential execution** - Nest tasks as children (parent → child → grandchild)
- **Parallel execution** - Use `<Block>` elements with sibling tasks
- **Conditional execution** - Use `<ContinuationCondition>` to skip work when no changes detected

**Rules:**
1. `<Block>` elements run child tasks **in parallel**
2. `<Block>` elements can **only have other `<Block>` siblings**
3. All other task types must **nest** (no siblings) for sequential execution
4. All tasks require `Name` and `Enabled` attributes

**Example - Sequential:**
```xml
<ManagementAgent Name="AD MA" RunProfile="FISO" Enabled="true">
  <ManagementAgent Name="AD MA" RunProfile="FS" Enabled="true">
    <ManagementAgent Name="AD MA" RunProfile="E" Enabled="true" />
  </ManagementAgent>
</ManagementAgent>
```
Execution order: Import → Sync → Export (one after another)

**Example - Parallel:**
```xml
<Block Name="Parallel Imports" Enabled="true">
  <ManagementAgent Name="AD MA" RunProfile="FISO" Enabled="true" />
  <ManagementAgent Name="HRMS MA" RunProfile="FISO" Enabled="true" />
  <ManagementAgent Name="SQL MA" RunProfile="FISO" Enabled="true" />
</Block>
```
All three imports run simultaneously.

⚠️ **Warning:** Never use parallel execution for synchronisation run profiles (FS, DS) - MIM will experience SQL deadlocks.

---

## Task Types

All task types require `Name` and `Enabled` attributes.

### ManagementAgent

Executes a MIM Management Agent run profile.

**Attributes:**
- `RunProfile` (required) - Run profile name as defined in MIM (e.g., "FISO", "FS", "E")
- `OnlyIfPendingExportsExist` (optional) - If `true`, only runs if pending exports exist

**Example:**
```xml
<ManagementAgent Name="AD MA" RunProfile="E" Enabled="true" OnlyIfPendingExportsExist="true" />
```

### PowerShell

Executes a PowerShell script file.

**Attributes:**
- `Path` (required) - Fully-qualified path to `.ps1` file

**Example:**
```xml
<PowerShell Name="Generate deltas" Path="C:\Scripts\generate-deltas.ps1" Enabled="true" />
```

⚠️ **Important:** Do not use `Write-Host` in scripts (not compatible with the scheduler). Use `Write-Output` instead.

### VisualBasicScript

Executes a VBScript file.

**Attributes:**
- `Path` (required) - Fully-qualified path to `.vbs` file

**Example:**
```xml
<VisualBasicScript Name="Legacy script" Path="C:\Scripts\process-data.vbs" Enabled="true" />
```

### Executable

Runs an arbitrary executable program.

**Attributes:**
- `Command` (required) - Path to executable
- `Arguments` (optional) - Command-line arguments
- `ShowWindow` (optional, default: `false`) - Show console window (use `true` for development/debugging)

**Example:**
```xml
<Executable Name="Custom Tool"
            Command="C:\Tools\MyTool.exe"
            Arguments="--mode=sync --verbose"
            ShowWindow="false"
            Enabled="true" />
```

### SqlServer

Executes a SQL Server command (stored procedure or SQL statement).

**Attributes:**
- `Server` (required) - SQL Server connection string (host/instance)
- `Command` (required) - SQL command (stored proc name or SQL statement)

**Authentication:** Uses Windows Authentication (integrated security). The service account must have appropriate SQL Server permissions.

**Example:**
```xml
<SqlServer Name="Prepare deltas"
           Server="SQLSVR01\MIM"
           Command="dbo.spPrepareDeltas"
           Enabled="true" />
```

### Block

Container for parallel task execution. All child tasks run simultaneously.

**Attributes:**
- None (just `Name` and `Enabled`)

**Rules:**
- Can only have other `<Block>` siblings
- Children can be any task type
- Do not use for synchronisation run profiles (FS, DS)

**Example:**
```xml
<Block Name="All Imports" Enabled="true">
  <ManagementAgent Name="AD MA" RunProfile="FISO" Enabled="true" />
  <ManagementAgent Name="HRMS MA" RunProfile="FISO" Enabled="true" />
  <PowerShell Name="Generate DB deltas" Path="C:\Scripts\gen.ps1" Enabled="true" />
</Block>
```

### ContinuationCondition

Conditional execution based on runtime state. Child tasks only run if condition is met.

**Attributes:**
- `Type` (required) - Condition type

**Supported Types:**
- `ManagementAgentsHadImports` - Only continue if preceding import run profiles detected changes

**Use Case:** Optimisation - skip all downstream work if no changes detected in source systems.

**Example:**
```xml
<ContinuationCondition Type="ManagementAgentsHadImports">
  <ManagementAgent Name="AD MA" RunProfile="FS" Enabled="true">
    <!-- All synchronisation and export work here -->
  </ManagementAgent>
</ContinuationCondition>
```

---

## Examples

See [Example Schedules](src/Tetron.Mim.SynchronisationScheduler/Example%20Schedules/) directory:

- **[full-schedule.config](src/Tetron.Mim.SynchronisationScheduler/Example%20Schedules/full-schedule.config)** - Complex production schedule with parallel imports, continuation conditions, and nested sequential tasks
- **[ps-test-schedule.config](src/Tetron.Mim.SynchronisationScheduler/Example%20Schedules/ps-test-schedule.config)** - Simple PowerShell execution example
- **[vbs-test-schedule.config](src/Tetron.Mim.SynchronisationScheduler/Example%20Schedules/vbs-test-schedule.config)** - VBScript execution example

### Common Patterns

**Pattern 1: Delta Sync Schedule**
```xml
<Schedule Name="Delta Sync" StopOnIncompletion="true">
  <!-- Parallel imports from all sources -->
  <Block Name="Source Imports" Enabled="true">
    <ManagementAgent Name="AD MA" RunProfile="DISO" Enabled="true" />
    <ManagementAgent Name="HRMS MA" RunProfile="DISO" Enabled="true" />
  </Block>

  <!-- Only continue if changes detected -->
  <ContinuationCondition Type="ManagementAgentsHadImports">
    <!-- Sequential synchronisation -->
    <ManagementAgent Name="AD MA" RunProfile="DS" Enabled="true">
      <ManagementAgent Name="HRMS MA" RunProfile="DS" Enabled="true">

        <!-- Parallel exports -->
        <Block Name="Exports" Enabled="true">
          <ManagementAgent Name="AD MA" RunProfile="E" Enabled="true" />
          <ManagementAgent Name="HRMS MA" RunProfile="E" Enabled="true" />
        </Block>

      </ManagementAgent>
    </ManagementAgent>
  </ContinuationCondition>
</Schedule>
```

**Pattern 2: Pre-processing with Scripts**
```xml
<PowerShell Name="Generate deltas" Path="C:\Scripts\gen-deltas.ps1" Enabled="true">
  <ManagementAgent Name="Custom MA" RunProfile="FISO" Enabled="true">
    <ManagementAgent Name="Custom MA" RunProfile="FS" Enabled="true" />
  </ManagementAgent>
</PowerShell>
```

---

## Testing

### Test Suite
The project includes **37 comprehensive tests** with 100% pass rate:

| Component | Tests | Coverage |
|-----------|-------|----------|
| ProcessExecutor | 11 | PowerShell, VBScript, Executable execution |
| TaskExecutor | 13 | Sequential, parallel, retry, conditions |
| ScheduleExecutor | 3 | Schedule execution, WhatIf mode |
| Models | 10 | Validation, serialisation |

### Running Tests

**All tests:**
```powershell
dotnet test
```

**With detailed output:**
```powershell
dotnet test --logger "console;verbosity=detailed"
```

**Specific test class:**
```powershell
dotnet test --filter "FullyQualifiedName~ProcessExecutorTests"
```

**Integration tests only:**
```powershell
dotnet test --filter "Category=Integration"
```

### CI/CD
GitHub Actions runs all tests automatically on every push:
- Build verification
- All 37 tests
- Test result publishing

See [.github/workflows/build-and-test.yml](.github/workflows/build-and-test.yml)

### Test Documentation
Comprehensive test documentation available at:
- [tests/README.md](tests/Tetron.Mim.SynchronisationScheduler.Tests/README.md) - Detailed test strategy and coverage

---

## Troubleshooting

### Common Issues

#### "Access denied" when executing Management Agent operations
**Cause:** WMI permissions required for MIM operations
**Solution:** Run as Local Administrator on MIM Sync server, or add service account to appropriate WMI security settings

#### PowerShell scripts fail with "execution policy" errors
**Cause:** PowerShell execution policy restrictions
**Solution:**
```powershell
Set-ExecutionPolicy RemoteSigned -Scope CurrentUser
```
Or sign your scripts with a trusted certificate

#### "SQL deadlock" errors during synchronisation
**Cause:** Multiple synchronisation run profiles (FS/DS) running in parallel
**Solution:** Never use `<Block>` for synchronisation run profiles. The scheduler will automatically retry once, but repeated deadlocks indicate a configuration issue.

#### Schedule file not found
**Cause:** Invalid path or relative path used
**Solution:** Always use fully-qualified paths:
```powershell
# Good
.\Scheduler.exe "C:\MIM\Schedules\delta-sync.config"

# Bad
.\Scheduler.exe "delta-sync.config"
```

#### Tasks appear to hang
**Cause:** Long-running operations or waiting for user input
**Solution:**
- Check logs for current operation
- Ensure `ShowWindow="false"` on Executable tasks (windows waiting for user input)
- Verify external scripts/executables don't prompt for input

#### Logs not being created
**Cause:** Insufficient permissions or invalid log path
**Solution:** Ensure service account has write access to `logs\` directory (created automatically in executable directory)

### Debug Mode

Enable verbose logging for troubleshooting:

**App.config:**
```xml
<add key="LoggingLevel" value="Debug" />
```

This logs:
- All task executions with timing
- WMI operations
- Process execution details
- Detailed error information

### What-If Mode for Testing

Test schedules safely without executing operations:

**App.config:**
```xml
<add key="whatif" value="true" />
```

All logs will be prefixed with `WHATIF:` and no actual operations will be performed.

---

## Architecture

### Overview
Modern service-oriented architecture with dependency injection (refactored 2025-11-23).

**Core Principles:**
- **Separation of concerns** - Orchestration logic separate from execution
- **Testability** - All external dependencies abstracted behind interfaces
- **Interface-based design** - Easy to mock and test

### Service Layer

| Interface | Implementation | Responsibility |
|-----------|----------------|----------------|
| `IScheduleExecutor` | `ScheduleExecutor` | Schedule orchestration |
| `ITaskExecutor` | `TaskExecutor` | Task execution and flow control |
| `IManagementAgentExecutor` | `ManagementAgentExecutor` | MIM WMI operations |
| `IProcessExecutor` | `ProcessExecutor` | External process execution |
| `ISqlExecutor` | `SqlExecutor` | SQL Server operations |

### Key Components

**Program.cs** - Entry point, configuration, schedule loading (258 lines)
**Services/** - Execution services with dependency injection
**Models/** - Domain models (Schedule, ScheduleTask, etc.)
**Interfaces/** - Service contracts for testability

### Parallel Execution
Block tasks use `Task.Run` with `Task.WaitAll` for true parallel execution while aggregating state (import changes) across threads.

### Detailed Documentation
- [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) - Architecture Decision Records (ADRs), development log
- [docs/CODE_REVIEW.md](docs/CODE_REVIEW.md) - Code review findings (A+ grade, all issues resolved)
- [docs/AI_SESSIONS.md](docs/AI_SESSIONS.md) - AI-assisted development session logs

---

## Documentation

### Project Documentation
- **[docs/DEVELOPMENT.md](docs/DEVELOPMENT.md)** - Development log, ADRs, build instructions, contributing guide
- **[docs/CODE_REVIEW.md](docs/CODE_REVIEW.md)** - Code review findings and resolutions
- **[docs/AI_SESSIONS.md](docs/AI_SESSIONS.md)** - AI-assisted development sessions

### Test Documentation
- **[tests/README.md](tests/Tetron.Mim.SynchronisationScheduler.Tests/README.md)** - Comprehensive test strategy and coverage

### Code Documentation
- XML documentation on all public APIs
- Inline comments for complex logic
- Example schedules with detailed comments

---

## Contributing

Contributions are welcome! This is a mature, battle-tested codebase with comprehensive test coverage.

### Code Style
- **British English spelling** throughout (e.g., "Synchronisation", "Initialise")
- **XML documentation** on all public APIs
- **FluentAssertions** for test assertions
- **Arrange-Act-Assert** pattern in tests

### Development Workflow
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Make changes with tests
4. Run all tests (`dotnet test`)
5. Ensure all 37 tests pass
6. Commit with descriptive messages
7. Push and create a Pull Request

### Adding New Task Types
1. Add enum value to `ScheduleTaskType` in `Models/Enums.cs`
2. Add execution logic to relevant service (or create new service interface)
3. Wire up in `TaskExecutor.ExecuteTask()` switch statement
4. Add XML parsing in `Program.BuildScheduleTask()`
5. Add unit tests with mocked dependencies
6. Add example to Example Schedules
7. Update documentation

### Testing Requirements
- All new features must include unit tests
- Maintain 100% test pass rate
- Use mocked dependencies for unit tests
- Follow existing test patterns

See [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) for detailed contribution guidelines.

---

## License

**Open Source - Use as you like**

This project is provided as-is for use with MIM synchronisation scenarios. While no formal license is specified, it's intended for free use in MIM deployments.

Pull requests are welcome and will be reviewed for quality and compatibility.

---

## History

**Originally developed:** 2013
**Major refactoring:** 2025-11-23 (dependency injection, comprehensive testing)
**Battle-tested:** Used in production across multiple large-scale MIM deployments

This scheduler has been proven in demanding enterprise environments with complex synchronisation requirements, multiple connected systems, and tight SLA requirements.

### Recent Improvements (2025-11-23)
- ✅ Dependency injection architecture
- ✅ 37 comprehensive tests (100% passing)
- ✅ GitHub Actions CI/CD
- ✅ Modern SDK-style project format
- ✅ Code review (A+ grade)
- ✅ Comprehensive documentation

See [docs/AI_SESSIONS.md](docs/AI_SESSIONS.md) for detailed development history.

---

**Questions?** Open an issue or submit a pull request.

**Found a bug?** Please report with:
- Schedule file (sanitised)
- Log output
- Expected vs actual behaviour
- MIM version and environment details
