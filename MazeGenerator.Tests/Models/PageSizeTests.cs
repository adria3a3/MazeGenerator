using MazeGenerator.Models;
using Xunit;

namespace MazeGenerator.Tests.Models
{
    public class PageSizeTests
    {
        [Theory]
        [InlineData(PageSizeName.A4, 595.28, 841.89)]
        [InlineData(PageSizeName.A3, 841.89, 1190.55)]
        [InlineData(PageSizeName.A2, 1190.55, 1683.78)]
        [InlineData(PageSizeName.Letter, 612.0, 792.0)]
        [InlineData(PageSizeName.Legal, 612.0, 1008.0)]
        [InlineData(PageSizeName.Tabloid, 792.0, 1224.0)]
        public void GetDimensions_ReturnsCorrectValues(PageSizeName name, double expectedWidth, double expectedHeight)
        {
            var (width, height) = PageSize.GetDimensions(name);
            Assert.Equal(expectedWidth, width);
            Assert.Equal(expectedHeight, height);
        }

        [Fact]
        public void GetDimensions_InvalidName_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                PageSize.GetDimensions((PageSizeName)999));
        }

        [Fact]
        public void MazeConfiguration_DefaultPageSizeName_IsA2()
        {
            var config = new MazeConfiguration();
            Assert.Equal(PageSizeName.A2, config.PageSizeName);
        }

        [Fact]
        public void MazeConfiguration_DerivedDimensions_MatchPageSize()
        {
            var (w, h) = PageSize.GetDimensions(PageSizeName.A4);
            var config = new MazeConfiguration
            {
                Rings = 5,
                PageSizeName = PageSizeName.A4,
                PageWidth = w,
                PageHeight = h
            };

            Assert.Equal(w, config.PageWidth);
            Assert.Equal(h, config.PageHeight);
            Assert.Empty(config.Validate());
        }
    }
}
