using FluentAssertions;
using Moq;
using Tetron.Mim.SynchronisationScheduler.Interfaces;
using Tetron.Mim.SynchronisationScheduler.Models;
using Tetron.Mim.SynchronisationScheduler.Services;
using Xunit;

namespace Tetron.Mim.SynchronisationScheduler.Tests.Services
{
    public class ScheduleExecutorTests
    {
        private readonly Mock<ITaskExecutor> _mockTaskExecutor;
        private readonly ScheduleExecutor _scheduleExecutor;

        public ScheduleExecutorTests()
        {
            _mockTaskExecutor = new Mock<ITaskExecutor>();
            _scheduleExecutor = new ScheduleExecutor(_mockTaskExecutor.Object, whatIfMode: false);
        }

        [Fact]
        public void ExecuteSchedule_CallsTaskExecutorWithScheduleTasks()
        {
            // Arrange
            var schedule = new Schedule
            {
                Name = "Test Schedule",
                StopOnIncompletion = true
            };
            schedule.Tasks.Add(new ScheduleTask
            {
                Name = "Task 1",
                Type = ScheduleTaskType.PowerShell,
                Command = "test.ps1"
            });

            _mockTaskExecutor
                .Setup(x => x.ExecuteTasks(schedule.Tasks, true, ref It.Ref<bool>.IsAny))
                .Returns(true);

            // Act
            _scheduleExecutor.ExecuteSchedule(schedule);

            // Assert
            _mockTaskExecutor.Verify(
                x => x.ExecuteTasks(schedule.Tasks, true, ref It.Ref<bool>.IsAny),
                Times.Once);
        }

        [Fact]
        public void ExecuteSchedule_PassesStopOnIncompletionCorrectly()
        {
            // Arrange
            var schedule = new Schedule
            {
                Name = "Test Schedule",
                StopOnIncompletion = false
            };

            _mockTaskExecutor
                .Setup(x => x.ExecuteTasks(schedule.Tasks, false, ref It.Ref<bool>.IsAny))
                .Returns(true);

            // Act
            _scheduleExecutor.ExecuteSchedule(schedule);

            // Assert
            _mockTaskExecutor.Verify(
                x => x.ExecuteTasks(schedule.Tasks, false, ref It.Ref<bool>.IsAny),
                Times.Once);
        }

        [Fact]
        public void ExecuteSchedule_WithEmptySchedule_CompletesSuccessfully()
        {
            // Arrange
            var schedule = new Schedule
            {
                Name = "Empty Schedule",
                StopOnIncompletion = true
            };

            _mockTaskExecutor
                .Setup(x => x.ExecuteTasks(schedule.Tasks, true, ref It.Ref<bool>.IsAny))
                .Returns(true);

            // Act & Assert
            _scheduleExecutor.ExecuteSchedule(schedule);

            _mockTaskExecutor.Verify(
                x => x.ExecuteTasks(schedule.Tasks, true, ref It.Ref<bool>.IsAny),
                Times.Once);
        }
    }
}
