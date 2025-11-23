using FluentAssertions;
using Tetron.Mim.SynchronisationScheduler.Models;
using Xunit;

namespace Tetron.Mim.SynchronisationScheduler.Tests.Models
{
    public class ScheduleTaskTests
    {
        [Fact]
        public void Constructor_ShouldInitialiseChildTasksList()
        {
            // Act
            var task = new ScheduleTask();

            // Assert
            task.ChildTasks.Should().NotBeNull();
            task.ChildTasks.Should().BeEmpty();
        }

        [Fact]
        public void Name_ShouldSetAndGetCorrectly()
        {
            // Arrange
            var task = new ScheduleTask();
            var expectedName = "Test Task";

            // Act
            task.Name = expectedName;

            // Assert
            task.Name.Should().Be(expectedName);
        }

        [Fact]
        public void ToString_ForManagementAgentType_ShouldIncludeCommand()
        {
            // Arrange
            var task = new ScheduleTask
            {
                Name = "Test MA",
                Type = ScheduleTaskType.ManagementAgent,
                Command = "Full Import"
            };

            // Act
            var result = task.ToString();

            // Assert
            result.Should().Be("Test MA - Full Import");
        }

        [Fact]
        public void ToString_ForNonManagementAgentType_ShouldReturnNameOnly()
        {
            // Arrange
            var task = new ScheduleTask
            {
                Name = "Test PowerShell",
                Type = ScheduleTaskType.PowerShell,
                Command = "script.ps1"
            };

            // Act
            var result = task.ToString();

            // Assert
            result.Should().Be("Test PowerShell");
        }

        [Fact]
        public void OnlyRunIfPendingExportsExist_ShouldSetAndGetCorrectly()
        {
            // Arrange
            var task = new ScheduleTask();

            // Act
            task.OnlyRunIfPendingExportsExist = true;

            // Assert
            task.OnlyRunIfPendingExportsExist.Should().BeTrue();
        }

        [Fact]
        public void ChildTasks_ShouldAllowAddingTasks()
        {
            // Arrange
            var parentTask = new ScheduleTask { Name = "Parent" };
            var childTask = new ScheduleTask { Name = "Child" };

            // Act
            parentTask.ChildTasks.Add(childTask);

            // Assert
            parentTask.ChildTasks.Should().HaveCount(1);
            parentTask.ChildTasks[0].Name.Should().Be("Child");
        }
    }
}
