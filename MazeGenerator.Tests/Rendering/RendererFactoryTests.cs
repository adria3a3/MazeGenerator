using MazeGenerator.Models;
using MazeGenerator.Rendering;
using Xunit;

namespace MazeGenerator.Tests.Rendering
{
    public class RendererFactoryTests
    {
        [Theory]
        [InlineData(OutputFormat.Pdf, typeof(PdfMazeRenderer))]
        [InlineData(OutputFormat.Svg, typeof(SvgMazeRenderer))]
        [InlineData(OutputFormat.Png, typeof(PngMazeRenderer))]
        public void Create_ReturnsCorrectType(OutputFormat format, Type expectedType)
        {
            var renderer = RendererFactory.Create(format);
            Assert.IsType(expectedType, renderer);
        }

        [Fact]
        public void Create_InvalidFormat_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                RendererFactory.Create((OutputFormat)999));
        }

        [Fact]
        public void Create_WithCalculator_UsesProvidedCalculator()
        {
            var calc = new MazeWallCalculator();
            var renderer = RendererFactory.Create(OutputFormat.Svg, calc);
            Assert.IsType<SvgMazeRenderer>(renderer);
        }
    }
}
