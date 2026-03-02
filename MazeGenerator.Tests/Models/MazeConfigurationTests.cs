using MazeGenerator.Models;
using Xunit;

namespace MazeGenerator.Tests.Models
{
    public class MazeConfigurationTests
    {
        [Fact]
        public void Validate_ValidConfig_ReturnsNoErrors()
        {
            var config = new MazeConfiguration
            {
                Rings = 10,
                MinCoverage = 30,
                WallThickness = 2.0,
                OutputBaseName = "test_maze"
            };

            var errors = config.Validate();

            Assert.Empty(errors);
        }

        [Fact]
        public void Validate_RingsTooLow_ReturnsError()
        {
            var config = new MazeConfiguration { Rings = 0 };

            var errors = config.Validate();

            Assert.Contains(errors, e => e.Contains("Rings"));
        }

        [Fact]
        public void Validate_RingsTooHigh_ReturnsError()
        {
            var config = new MazeConfiguration { Rings = 101 };

            var errors = config.Validate();

            Assert.Contains(errors, e => e.Contains("Rings"));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public void Validate_MinCoverageOutOfRange_ReturnsError(int minCoverage)
        {
            var config = new MazeConfiguration { Rings = 10, MinCoverage = minCoverage };

            var errors = config.Validate();

            Assert.Contains(errors, e => e.Contains("MinCoverage"));
        }

        [Theory]
        [InlineData(0.1)]
        [InlineData(11.0)]
        public void Validate_WallThicknessOutOfRange_ReturnsError(double wallThickness)
        {
            var config = new MazeConfiguration { Rings = 10, WallThickness = wallThickness };

            var errors = config.Validate();

            Assert.Contains(errors, e => e.Contains("Wall thickness"));
        }

        [Theory]
        [InlineData(0.5)]
        [InlineData(10.0)]
        public void Validate_WallThicknessAtBoundary_ReturnsNoError(double wallThickness)
        {
            var config = new MazeConfiguration { Rings = 10, WallThickness = wallThickness };

            var errors = config.Validate();

            Assert.DoesNotContain(errors, e => e.Contains("Wall thickness"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Validate_EmptyOutputBaseName_ReturnsError(string outputBaseName)
        {
            var config = new MazeConfiguration { Rings = 10, OutputBaseName = outputBaseName };

            var errors = config.Validate();

            Assert.Contains(errors, e => e.Contains("Output base name"));
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(-100.0)]
        public void Validate_NonPositivePageWidth_ReturnsError(double pageWidth)
        {
            var config = new MazeConfiguration { Rings = 10, PageWidth = pageWidth };

            Assert.Contains(config.Validate(), e => e.Contains("PageWidth"));
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(-100.0)]
        public void Validate_NonPositivePageHeight_ReturnsError(double pageHeight)
        {
            var config = new MazeConfiguration { Rings = 10, PageHeight = pageHeight };

            Assert.Contains(config.Validate(), e => e.Contains("PageHeight"));
        }

        [Fact]
        public void Validate_NegativeMargin_ReturnsError()
        {
            var config = new MazeConfiguration { Rings = 10, Margin = -1.0 };

            Assert.Contains(config.Validate(), e => e.Contains("Margin"));
        }

        [Fact]
        public void Validate_MarginTooLargeForPage_ReturnsError()
        {
            // Margin ≥ half of either page dimension makes UsableRadius ≤ 0
            var config = new MazeConfiguration { Rings = 10, PageWidth = 100, PageHeight = 100, Margin = 51 };

            Assert.Contains(config.Validate(), e => e.Contains("Margin"));
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(-5.0)]
        public void Validate_NonPositiveInnerRadius_ReturnsError(double innerRadius)
        {
            var config = new MazeConfiguration { Rings = 10, InnerRadius = innerRadius };

            Assert.Contains(config.Validate(), e => e.Contains("InnerRadius"));
        }
    }
}

