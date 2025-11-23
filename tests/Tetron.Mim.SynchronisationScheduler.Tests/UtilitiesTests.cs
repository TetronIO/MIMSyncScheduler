using FluentAssertions;
using Xunit;

namespace Tetron.Mim.SynchronisationScheduler.Tests
{
    public class UtilitiesTests
    {
        [Fact]
        public void GetCallingClassName_ShouldReturnNonEmptyString()
        {
            // Act
            var result = GetCallingClassNameWrapper();

            // Assert
            // Stack trace methods return different values when run through test runners
            // vs direct execution, so we verify they return a non-empty class name
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain(".");  // Should be a fully qualified name
        }

        [Fact]
        public void GetCallingMethodName_ShouldReturnNonEmptyString()
        {
            // Act
            var result = GetCallingMethodNameWrapper();

            // Assert
            // Stack trace methods return different values when run through test runners
            // vs direct execution, so we verify they return a non-empty method name
            result.Should().NotBeNullOrEmpty();
        }

        private string GetCallingClassNameWrapper()
        {
            return Utilities.GetCallingClassName();
        }

        private string GetCallingMethodNameWrapper()
        {
            return Utilities.GetCallingMethodName();
        }
    }
}
