# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2025-11-24

### Overview
Major architectural modernization release. This version represents a complete refactoring of the battle-tested MIMSyncScheduler with modern development practices, comprehensive test coverage, and extensive documentation. While maintaining full backwards compatibility with existing schedule files and configurations, the internal architecture has been completely rebuilt.

### Added
- **Dependency Injection Architecture** - Complete refactoring to use service-oriented architecture with dependency injection for better testability and maintainability
- **Comprehensive Test Suite** - 37 unit tests with 100% pass rate covering all major components (ProcessExecutor, TaskExecutor, ScheduleExecutor, Models)
- **GitHub Actions CI/CD** - Automated build and test pipeline running on every push
- **Configurable Log File Mode** - New `LogFileMode` setting with two options:
  - `Daily` - One log file per day (format: `YYYYMMDD-scheduler.log`)
  - `PerExecution` - One log file per execution (format: `YYYYMMDDHHmmss-scheduler.log`)
- **Comprehensive Documentation**:
  - Extensively rewritten README.md with modern documentation standards
  - Development documentation (docs/DEVELOPMENT.md) with Architecture Decision Records
  - Code review documentation (docs/CODE_REVIEW.md)
  - AI session logs (docs/AI_SESSIONS.md)
  - Test strategy documentation (tests/README.md)
- **Modern Project Structure** - Migrated to SDK-style project format with proper separation of concerns
- **Service Layer Interfaces** - Clean abstraction of all external dependencies for better testing:
  - IScheduleExecutor
  - ITaskExecutor
  - IManagementAgentExecutor
  - IProcessExecutor
  - ISqlExecutor

### Changed
- **Code Quality** - Achieved A+ grade in comprehensive code review with all issues resolved
- **Test Infrastructure** - All tests use mocked dependencies for reliable, fast execution
- **Project Organization** - Restructured to follow .NET best practices with clear separation between source and tests

### Removed
- Unused `_whatIfMode` fields from TaskExecutor and ScheduleExecutor (consolidated into constructor parameters)

### Fixed
- GitHub Actions badge URL in README.md
- UtilitiesTests to work correctly in CI environment

### Core Features (Existing)
This release includes all battle-tested features from the original 2013 implementation:

#### Task Types
- **ManagementAgent** - Execute MIM Management Agent run profiles with conditional execution support
- **PowerShell** - Run PowerShell scripts with full output capture
- **VisualBasicScript** - Execute VBScript files
- **Executable** - Run arbitrary programs with arguments
- **SqlServer** - Execute SQL Server commands with Windows Authentication
- **Block** - Parallel execution of multiple tasks
- **ContinuationCondition** - Conditional execution based on runtime state

#### Execution Features
- **Parallel Execution** - Run independent tasks simultaneously for optimal performance
- **Conditional Execution** - Only run tasks when changes are detected
- **Smart Exports** - Only export when pending exports exist
- **Retry Logic** - Automatic retry on SQL deadlock responses from MIM
- **What-If Mode** - Test schedules without executing actual operations
- **Comprehensive Logging** - Serilog with rolling file logs and configurable verbosity levels

#### Platform Support
- .NET Framework 4.8.1
- Windows Server 2016+
- MIM 2016 / MIM 2019 compatibility
- WMI integration for MIM operations

### Technical Details
- **Test Coverage**: 37 tests across 4 major components
- **Code Quality**: A+ grade with all review issues resolved
- **Architecture**: Service-oriented with dependency injection
- **Documentation**: Comprehensive with examples and troubleshooting guides

### Migration Notes
Users upgrading from v1.0.x releases should:
1. **Schedule files**: No changes required - fully backwards compatible
2. **App.config**: Add new optional `LogFileMode` setting (defaults to `Daily` if not specified)
   ```xml
   <add key="LogFileMode" value="Daily" />
   ```
3. **Log file naming**: When using `Daily` mode, log files now use `YYYYMMDD-scheduler.log` format (date-first) instead of `scheduler-YYYYMMDD.log`
4. **No breaking changes** to existing functionality or API surface

### Known Limitations
- Requires local administrator privileges for WMI access to MIM
- Parallel execution should not be used for synchronisation run profiles (FS/DS) due to SQL deadlock potential
- PowerShell scripts cannot use `Write-Host` (use `Write-Output` instead)

---

## Previous Releases

### [1.0.9104.28823] - 2024-12-04
- Built for x64 platforms to enable PowerShell modules to be imported in scripts

### [1.0.9091.28389] - 2024-11-21
- Major changelog updates

### [1.0.7761.19659] - 2024-04-01
- Spaces in VBS task paths now supported
- Logging level set to Debug

### [1.0.7759.21022] - 2024-03-30
- Executable tasks log output at Debug/Error levels
- Removed executable timeout feature

### [1.0.7755.30985] - 2024-03-26
- Added .vbs script scheduling support
- Fixed PowerShell script run issue

### [1.0.7720.19396] - 2024-02-19
- Minor updates and file compression for easier downloading

### [1.0.7717.19958] - 2024-02-16
- Initial public release
- Built on eight years of proven production use

---

**Full Changelog**: https://github.com/TetronIO/MIMSyncScheduler/compare/v1.0.9104.28823...v2.0.0

[2.0.0]: https://github.com/TetronIO/MIMSyncScheduler/releases/tag/v2.0.0
[1.0.9104.28823]: https://github.com/TetronIO/MIMSyncScheduler/releases/tag/v1.0.9104.28823
[1.0.9091.28389]: https://github.com/TetronIO/MIMSyncScheduler/releases/tag/v1.0.9091.28389
[1.0.7761.19659]: https://github.com/TetronIO/MIMSyncScheduler/releases/tag/v1.0.7761.19659
[1.0.7759.21022]: https://github.com/TetronIO/MIMSyncScheduler/releases/tag/v1.0.7759.21022
[1.0.7755.30985]: https://github.com/TetronIO/MIMSyncScheduler/releases/tag/v1.0.7755.30985
[1.0.7720.19396]: https://github.com/TetronIO/MIMSyncScheduler/releases/tag/v1.0.7720.19396
[1.0.7717.19958]: https://github.com/TetronIO/MIMSyncScheduler/releases/tag/v1.0.7717.19958
