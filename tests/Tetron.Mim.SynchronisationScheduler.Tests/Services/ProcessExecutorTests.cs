using FluentAssertions;
using System.IO;
using Tetron.Mim.SynchronisationScheduler.Services;
using Xunit;

namespace Tetron.Mim.SynchronisationScheduler.Tests.Services
{
    public class ProcessExecutorTests
    {
        private readonly string _testHelpersPath;

        public ProcessExecutorTests()
        {
            _testHelpersPath = Path.Combine(Directory.GetCurrentDirectory(), "TestHelpers");
        }

        [Fact]
        public void ExecutePowerShellScript_WithSuccessScript_ReturnsTrue()
        {
            // Arrange
            var executor = new ProcessExecutor(whatIfMode: false);
            var scriptPath = Path.Combine(_testHelpersPath, "test-success.ps1");

            // Act
            var result = executor.ExecutePowerShellScript(scriptPath);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ExecutePowerShellScript_WithFailureScript_ReturnsFalse()
        {
            // Arrange
            var executor = new ProcessExecutor(whatIfMode: false);
            var scriptPath = Path.Combine(_testHelpersPath, "test-failure.ps1");

            // Act
            var result = executor.ExecutePowerShellScript(scriptPath);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ExecutePowerShellScript_InWhatIfMode_ReturnsTrue()
        {
            // Arrange
            var executor = new ProcessExecutor(whatIfMode: true);
            var scriptPath = "nonexistent-script.ps1";

            // Act
            var result = executor.ExecutePowerShellScript(scriptPath);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ExecuteVisualBasicScript_WithSuccessScript_ReturnsTrue()
        {
            // Arrange
            var executor = new ProcessExecutor(whatIfMode: false);
            var scriptPath = Path.Combine(_testHelpersPath, "test-success.vbs");

            // Act
            var result = executor.ExecuteVisualBasicScript(scriptPath);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ExecuteVisualBasicScript_WithFailureScript_ReturnsFalse()
        {
            // Arrange
            var executor = new ProcessExecutor(whatIfMode: false);
            var scriptPath = Path.Combine(_testHelpersPath, "test-failure.vbs");

            // Act
            var result = executor.ExecuteVisualBasicScript(scriptPath);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ExecuteVisualBasicScript_InWhatIfMode_ReturnsTrue()
        {
            // Arrange
            var executor = new ProcessExecutor(whatIfMode: true);
            var scriptPath = "nonexistent-script.vbs";

            // Act
            var result = executor.ExecuteVisualBasicScript(scriptPath);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ExecuteExecutable_WithSuccessExitCode_ReturnsTrue()
        {
            // Arrange
            var executor = new ProcessExecutor(whatIfMode: false);

            // Act
            var result = executor.ExecuteExecutable("cmd.exe", "/c exit 0");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ExecuteExecutable_WithFailureExitCode_ReturnsFalse()
        {
            // Arrange
            var executor = new ProcessExecutor(whatIfMode: false);

            // Act
            var result = executor.ExecuteExecutable("cmd.exe", "/c exit 1");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ExecuteExecutable_InWhatIfMode_ReturnsTrue()
        {
            // Arrange
            var executor = new ProcessExecutor(whatIfMode: true);

            // Act
            var result = executor.ExecuteExecutable("nonexistent.exe");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ExecuteExecutable_WithNullArguments_HandlesGracefully()
        {
            // Arrange
            var executor = new ProcessExecutor(whatIfMode: false);

            // Act
            var result = executor.ExecuteExecutable("cmd.exe", null);

            // Assert
            result.Should().BeTrue();
        }
    }
}
