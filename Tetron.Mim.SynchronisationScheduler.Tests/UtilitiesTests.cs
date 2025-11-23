using FluentAssertions;
using Xunit;

namespace Tetron.Mim.SynchronisationScheduler.Tests
{
    public class UtilitiesTests
    {
        [Fact]
        public void GetCallingClassName_ShouldReturnClassName()
        {
            // Act
            var result = GetCallingClassNameWrapper();

            // Assert
            result.Should().Be("Tetron.Mim.SynchronisationScheduler.Tests.UtilitiesTests");
        }

        [Fact]
        public void GetCallingMethodName_ShouldReturnMethodName()
        {
            // Act
            var result = GetCallingMethodNameWrapper();

            // Assert
            result.Should().Be("GetCallingMethodName_ShouldReturnMethodName");
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
