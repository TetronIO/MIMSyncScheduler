using FluentAssertions;
using Moq;
using Tetron.Mim.SynchronisationScheduler.Interfaces;
using Tetron.Mim.SynchronisationScheduler.Models;
using Tetron.Mim.SynchronisationScheduler.Services;
using Xunit;

namespace Tetron.Mim.SynchronisationScheduler.Tests.Services
{
    public class TaskExecutorTests
    {
        private readonly Mock<IProcessExecutor> _mockProcessExecutor;
        private readonly Mock<ISqlExecutor> _mockSqlExecutor;
        private readonly Mock<IManagementAgentExecutor> _mockManagementAgentExecutor;
        private readonly TaskExecutor _taskExecutor;

        public TaskExecutorTests()
        {
            _mockProcessExecutor = new Mock<IProcessExecutor>();
            _mockSqlExecutor = new Mock<ISqlExecutor>();
            _mockManagementAgentExecutor = new Mock<IManagementAgentExecutor>();

            _taskExecutor = new TaskExecutor(
                _mockProcessExecutor.Object,
                _mockSqlExecutor.Object,
                _mockManagementAgentExecutor.Object,
                whatIfMode: false);
        }

        [Fact]
        public void ExecuteTask_PowerShellTask_CallsProcessExecutor()
        {
            // Arrange
            var task = new ScheduleTask
            {
                Name = "Test PowerShell",
                Type = ScheduleTaskType.PowerShell,
                Command = "test.ps1"
            };
            _mockProcessExecutor.Setup(x => x.ExecutePowerShellScript("test.ps1")).Returns(true);
            var importsChanged = false;

            // Act
            var result = _taskExecutor.ExecuteTask(task, stopOnIncompletion: false, ref importsChanged);

            // Assert
            result.Should().BeTrue();
            _mockProcessExecutor.Verify(x => x.ExecutePowerShellScript("test.ps1"), Times.Once);
        }

        [Fact]
        public void ExecuteTask_VBScriptTask_CallsProcessExecutor()
        {
            // Arrange
            var task = new ScheduleTask
            {
                Name = "Test VBS",
                Type = ScheduleTaskType.VisualBasicScript,
                Command = "test.vbs"
            };
            _mockProcessExecutor.Setup(x => x.ExecuteVisualBasicScript("test.vbs")).Returns(true);
            var importsChanged = false;

            // Act
            var result = _taskExecutor.ExecuteTask(task, stopOnIncompletion: false, ref importsChanged);

            // Assert
            result.Should().BeTrue();
            _mockProcessExecutor.Verify(x => x.ExecuteVisualBasicScript("test.vbs"), Times.Once);
        }

        [Fact]
        public void ExecuteTask_ExecutableTask_CallsProcessExecutor()
        {
            // Arrange
            var task = new ScheduleTask
            {
                Name = "Test Exe",
                Type = ScheduleTaskType.Executable,
                Command = "test.exe",
                Arguments = "arg1 arg2",
                ShowExecutableWindow = true
            };
            _mockProcessExecutor.Setup(x => x.ExecuteExecutable("test.exe", "arg1 arg2", true)).Returns(true);
            var importsChanged = false;

            // Act
            var result = _taskExecutor.ExecuteTask(task, stopOnIncompletion: false, ref importsChanged);

            // Assert
            result.Should().BeTrue();
            _mockProcessExecutor.Verify(x => x.ExecuteExecutable("test.exe", "arg1 arg2", true), Times.Once);
        }

        [Fact]
        public void ExecuteTask_SqlServerTask_CallsSqlExecutor()
        {
            // Arrange
            var task = new ScheduleTask
            {
                Name = "Test SQL",
                Type = ScheduleTaskType.SqlServer,
                Command = "SELECT 1",
                Server = "localhost"
            };
            _mockSqlExecutor.Setup(x => x.ExecuteCommand("SELECT 1", "localhost")).Returns(true);
            var importsChanged = false;

            // Act
            var result = _taskExecutor.ExecuteTask(task, stopOnIncompletion: false, ref importsChanged);

            // Assert
            result.Should().BeTrue();
            _mockSqlExecutor.Verify(x => x.ExecuteCommand("SELECT 1", "localhost"), Times.Once);
        }

        [Fact]
        public void ExecuteTask_ManagementAgentTask_CallsManagementAgentExecutor()
        {
            // Arrange
            var task = new ScheduleTask
            {
                Name = "Test MA",
                Type = ScheduleTaskType.ManagementAgent,
                Command = "Full Import"
            };
            _mockManagementAgentExecutor.Setup(x => x.ExecuteRunProfile("Test MA", "Full Import", out It.Ref<bool>.IsAny)).Returns(true);
            var importsChanged = false;

            // Act
            var result = _taskExecutor.ExecuteTask(task, stopOnIncompletion: false, ref importsChanged);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ExecuteTask_ManagementAgentWithPendingExportsCheck_OnlyRunsIfExportsPending()
        {
            // Arrange
            var task = new ScheduleTask
            {
                Name = "Test MA",
                Type = ScheduleTaskType.ManagementAgent,
                Command = "Export",
                OnlyRunIfPendingExportsExist = true
            };
            _mockManagementAgentExecutor.Setup(x => x.HasPendingExports("Test MA")).Returns(false);
            var importsChanged = false;

            // Act
            var result = _taskExecutor.ExecuteTask(task, stopOnIncompletion: false, ref importsChanged);

            // Assert
            result.Should().BeFalse();
            _mockManagementAgentExecutor.Verify(x => x.HasPendingExports("Test MA"), Times.Once);
            _mockManagementAgentExecutor.Verify(x => x.ExecuteRunProfile(It.IsAny<string>(), It.IsAny<string>(), out It.Ref<bool>.IsAny), Times.Never);
        }

        [Fact]
        public void ExecuteTask_WithChildTasks_ExecutesChildrenWhenParentSucceeds()
        {
            // Arrange
            var childTask = new ScheduleTask
            {
                Name = "Child",
                Type = ScheduleTaskType.PowerShell,
                Command = "child.ps1"
            };
            var parentTask = new ScheduleTask
            {
                Name = "Parent",
                Type = ScheduleTaskType.PowerShell,
                Command = "parent.ps1",
                ChildTasks = { childTask }
            };
            _mockProcessExecutor.Setup(x => x.ExecutePowerShellScript("parent.ps1")).Returns(true);
            _mockProcessExecutor.Setup(x => x.ExecutePowerShellScript("child.ps1")).Returns(true);
            var importsChanged = false;

            // Act
            var result = _taskExecutor.ExecuteTask(parentTask, stopOnIncompletion: false, ref importsChanged);

            // Assert
            result.Should().BeTrue();
            _mockProcessExecutor.Verify(x => x.ExecutePowerShellScript("parent.ps1"), Times.Once);
            _mockProcessExecutor.Verify(x => x.ExecutePowerShellScript("child.ps1"), Times.Once);
        }

        [Fact]
        public void ExecuteTask_WithChildTasks_DoesNotExecuteChildrenWhenParentFails()
        {
            // Arrange
            var childTask = new ScheduleTask
            {
                Name = "Child",
                Type = ScheduleTaskType.PowerShell,
                Command = "child.ps1"
            };
            var parentTask = new ScheduleTask
            {
                Name = "Parent",
                Type = ScheduleTaskType.PowerShell,
                Command = "parent.ps1",
                ChildTasks = { childTask }
            };
            _mockProcessExecutor.Setup(x => x.ExecutePowerShellScript("parent.ps1")).Returns(false);
            var importsChanged = false;

            // Act
            var result = _taskExecutor.ExecuteTask(parentTask, stopOnIncompletion: false, ref importsChanged);

            // Assert
            result.Should().BeFalse();
            _mockProcessExecutor.Verify(x => x.ExecutePowerShellScript("parent.ps1"), Times.Once);
            _mockProcessExecutor.Verify(x => x.ExecutePowerShellScript("child.ps1"), Times.Never);
        }

        [Fact]
        public void ExecuteTask_ContinuationCondition_StopsWhenNoImportsDetected()
        {
            // Arrange
            var task = new ScheduleTask
            {
                Name = "Continuation",
                Type = ScheduleTaskType.ContinuationCondition,
                ConditionType = ContinuationConditionType.ManagementAgentsHadImports
            };
            var importsChanged = false;

            // Act
            var result = _taskExecutor.ExecuteTask(task, stopOnIncompletion: false, ref importsChanged);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ExecuteTask_ContinuationCondition_ContinuesWhenImportsDetected()
        {
            // Arrange
            var task = new ScheduleTask
            {
                Name = "Continuation",
                Type = ScheduleTaskType.ContinuationCondition,
                ConditionType = ContinuationConditionType.ManagementAgentsHadImports
            };
            var importsChanged = true;

            // Act
            var result = _taskExecutor.ExecuteTask(task, stopOnIncompletion: false, ref importsChanged);

            // Assert
            result.Should().BeTrue();
        }
    }
}
