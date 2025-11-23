using FluentAssertions;
using System.Threading;
using Xunit;

namespace Tetron.Mim.SynchronisationScheduler.Tests
{
    public class TimerTests
    {
        [Fact]
        public void Timer_ShouldCreateSuccessfully()
        {
            // Act
            var timer = new Timer();

            // Assert
            timer.Should().NotBeNull();
        }

        [Fact]
        public void Stop_ShouldCompleteWithoutError()
        {
            // Arrange
            var timer = new Timer();
            Thread.Sleep(10); // Small delay to ensure measurable time

            // Act
            timer.Stop();

            // Assert - Stop should complete without throwing
        }
    }
}
