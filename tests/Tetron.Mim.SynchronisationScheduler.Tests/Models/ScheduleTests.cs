using FluentAssertions;
using Tetron.Mim.SynchronisationScheduler.Models;
using Xunit;

namespace Tetron.Mim.SynchronisationScheduler.Tests.Models
{
    public class ScheduleTests
    {
        [Fact]
        public void Constructor_ShouldInitialiseTasksList()
        {
            // Act
            var schedule = new Schedule();

            // Assert
            schedule.Tasks.Should().NotBeNull();
            schedule.Tasks.Should().BeEmpty();
        }

        [Fact]
        public void Name_ShouldSetAndGetCorrectly()
        {
            // Arrange
            var schedule = new Schedule();
            var expectedName = "Test Schedule";

            // Act
            schedule.Name = expectedName;

            // Assert
            schedule.Name.Should().Be(expectedName);
        }

        [Fact]
        public void StopOnIncompletion_ShouldSetAndGetCorrectly()
        {
            // Arrange
            var schedule = new Schedule();

            // Act
            schedule.StopOnIncompletion = true;

            // Assert
            schedule.StopOnIncompletion.Should().BeTrue();
        }

        [Fact]
        public void Tasks_ShouldAllowAddingTasks()
        {
            // Arrange
            var schedule = new Schedule();
            var task = new ScheduleTask { Name = "Test Task" };

            // Act
            schedule.Tasks.Add(task);

            // Assert
            schedule.Tasks.Should().HaveCount(1);
            schedule.Tasks[0].Name.Should().Be("Test Task");
        }
    }
}
