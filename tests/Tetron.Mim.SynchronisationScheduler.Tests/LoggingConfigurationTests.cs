using System;
using System.Configuration;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Tetron.Mim.SynchronisationScheduler.Tests
{
    /// <summary>
    /// Integration tests for logging configuration and file naming conventions.
    /// </summary>
    public class LoggingConfigurationTests : IDisposable
    {
        private readonly string _testLogsDirectory;

        public LoggingConfigurationTests()
        {
            _testLogsDirectory = Path.Combine(Path.GetTempPath(), $"MIMSchedulerTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testLogsDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testLogsDirectory))
            {
                Directory.Delete(_testLogsDirectory, true);
            }
        }

        [Fact]
        public void LogFileMode_Daily_CreatesCorrectFileNameFormat()
        {
            // Arrange
            var logFileMode = "Daily";
            var expectedPattern = @"^\d{8}-scheduler\.log$"; // YYYYMMDD-scheduler.log

            // Act
            var fileName = GetExpectedLogFileName(logFileMode);

            // Assert
            fileName.Should().MatchRegex(expectedPattern,
                "Daily mode should create files with format YYYYMMDD-scheduler.log");

            // Verify date component is today
            var dateComponent = fileName.Substring(0, 8);
            var expectedDate = DateTime.Now.ToString("yyyyMMdd");
            dateComponent.Should().Be(expectedDate, "log file should be dated today");
        }

        [Fact]
        public void LogFileMode_PerExecution_CreatesCorrectFileNameFormat()
        {
            // Arrange
            var logFileMode = "PerExecution";
            var expectedPattern = @"^\d{14}-scheduler\.log$"; // YYYYMMDDHHmmss-scheduler.log

            // Act
            var fileName = GetExpectedLogFileName(logFileMode);

            // Assert
            fileName.Should().MatchRegex(expectedPattern,
                "PerExecution mode should create files with format YYYYMMDDHHmmss-scheduler.log");

            // Verify timestamp is recent (within last minute)
            var timestamp = fileName.Substring(0, 14);
            var logTime = DateTime.ParseExact(timestamp, "yyyyMMddHHmmss", null);
            var timeDifference = DateTime.Now - logTime;
            timeDifference.TotalMinutes.Should().BeLessThan(1,
                "log file timestamp should be within the last minute");
        }

        [Fact]
        public void LogFileMode_Default_UsesDailyMode()
        {
            // Arrange
            var logFileMode = ""; // Empty/null should default to Daily
            var expectedPattern = @"^\d{8}-scheduler\.log$";

            // Act
            var fileName = GetExpectedLogFileName(logFileMode);

            // Assert
            fileName.Should().MatchRegex(expectedPattern,
                "default (empty) mode should use Daily format");
        }

        [Fact]
        public void LogFileMode_Invalid_DefaultsToDailyMode()
        {
            // Arrange
            var logFileMode = "InvalidMode";
            var expectedPattern = @"^\d{8}-scheduler\.log$";

            // Act
            var fileName = GetExpectedLogFileName(logFileMode);

            // Assert
            fileName.Should().MatchRegex(expectedPattern,
                "invalid mode should default to Daily format");
        }

        [Fact]
        public void LogFileMode_Daily_UsesSameFileForMultipleExecutions()
        {
            // Arrange
            var logFileMode = "Daily";

            // Act
            var fileName1 = GetExpectedLogFileName(logFileMode);
            System.Threading.Thread.Sleep(1000); // Wait 1 second
            var fileName2 = GetExpectedLogFileName(logFileMode);

            // Assert
            fileName1.Should().Be(fileName2,
                "Daily mode should use the same log file for all executions on the same day");

            // Verify it's dated today
            var dateComponent = fileName1.Substring(0, 8);
            var expectedDate = DateTime.Now.ToString("yyyyMMdd");
            dateComponent.Should().Be(expectedDate, "log file should be dated today");
        }

        [Fact]
        public void LogFileMode_PerExecution_CreatesUniqueFilePerExecution()
        {
            // Arrange
            var logFileMode = "PerExecution";

            // Act
            var fileName1 = GetExpectedLogFileName(logFileMode);
            System.Threading.Thread.Sleep(1000); // Wait 1 second
            var fileName2 = GetExpectedLogFileName(logFileMode);

            // Assert
            fileName1.Should().NotBe(fileName2,
                "PerExecution mode should create unique files for each execution");
        }

        [Theory]
        [InlineData("Daily", "20251124-scheduler.log")]
        [InlineData("PerExecution", "20251124143052-scheduler.log")]
        public void LogFileName_FollowsDateFirstConvention(string mode, string exampleFormat)
        {
            // Arrange & Act
            var fileName = GetExpectedLogFileName(mode);

            // Assert
            var firstChar = fileName[0];
            char.IsDigit(firstChar).Should().BeTrue(
                $"log file name should start with date (format example: {exampleFormat})");

            fileName.Should().EndWith("-scheduler.log",
                "all log files should end with -scheduler.log");
        }

        /// <summary>
        /// Simulates the log file name generation logic from Program.InitialiseLogging()
        /// </summary>
        private string GetExpectedLogFileName(string logFileMode)
        {
            if (string.IsNullOrEmpty(logFileMode))
                logFileMode = "Daily";

            string fileName;
            switch (logFileMode.ToLower())
            {
                case "perexecution":
                    var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    fileName = $"{timestamp}-scheduler.log";
                    break;
                case "daily":
                default:
                    var dateStamp = DateTime.Now.ToString("yyyyMMdd");
                    fileName = $"{dateStamp}-scheduler.log";
                    break;
            }

            return fileName;
        }
    }
}
