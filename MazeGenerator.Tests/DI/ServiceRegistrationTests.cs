using MazeGenerator.DI;
using MazeGenerator.Models;
using MazeGenerator.Rendering;
using MazeGenerator.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MazeGenerator.Tests.DI
{
    public class ServiceRegistrationTests
    {
        [Fact]
        public void BuildServiceProvider_ResolvesAllServices()
        {
            var config = new MazeConfiguration { Rings = 5, Seed = 42 };
            var sp = ServiceRegistration.BuildServiceProvider(config);

            Assert.NotNull(sp.GetRequiredService<MazeConfiguration>());
            Assert.NotNull(sp.GetRequiredService<IMazeGenerator>());
            Assert.NotNull(sp.GetRequiredService<IPathFinder>());
            Assert.NotNull(sp.GetRequiredService<IEntranceExitSelector>());
            Assert.NotNull(sp.GetRequiredService<MazeWallCalculator>());
            Assert.NotNull(sp.GetRequiredService<IMazeRenderer>());
        }

        [Theory]
        [InlineData(MazeAlgorithm.DfsBacktracker, typeof(DfsBacktrackerGenerator))]
        [InlineData(MazeAlgorithm.Prims, typeof(PrimsGenerator))]
        [InlineData(MazeAlgorithm.Kruskals, typeof(KruskalsGenerator))]
        [InlineData(MazeAlgorithm.Wilsons, typeof(WilsonsGenerator))]
        public void BuildServiceProvider_ResolvesCorrectAlgorithm(MazeAlgorithm algorithm, Type expectedType)
        {
            var config = new MazeConfiguration { Rings = 5, Algorithm = algorithm };
            var sp = ServiceRegistration.BuildServiceProvider(config);

            var generator = sp.GetRequiredService<IMazeGenerator>();
            Assert.IsType(expectedType, generator);
        }

        [Theory]
        [InlineData(OutputFormat.Pdf, typeof(PdfMazeRenderer))]
        [InlineData(OutputFormat.Svg, typeof(SvgMazeRenderer))]
        [InlineData(OutputFormat.Png, typeof(PngMazeRenderer))]
        public void BuildServiceProvider_ResolvesCorrectRenderer(OutputFormat format, Type expectedType)
        {
            var config = new MazeConfiguration { Rings = 5, OutputFormat = format };
            var sp = ServiceRegistration.BuildServiceProvider(config);

            var renderer = sp.GetRequiredService<IMazeRenderer>();
            Assert.IsType(expectedType, renderer);
        }

        [Fact]
        public void BuildServiceProvider_PathFinderIsCorrectType()
        {
            var config = new MazeConfiguration { Rings = 5 };
            var sp = ServiceRegistration.BuildServiceProvider(config);

            Assert.IsType<PathFinder>(sp.GetRequiredService<IPathFinder>());
        }

        [Fact]
        public void BuildServiceProvider_EntranceExitSelectorIsCorrectType()
        {
            var config = new MazeConfiguration { Rings = 5 };
            var sp = ServiceRegistration.BuildServiceProvider(config);

            Assert.IsType<EntranceExitSelector>(sp.GetRequiredService<IEntranceExitSelector>());
        }

        [Fact]
        public void BuildServiceProvider_WithoutSeed_ResolvesGenerator()
        {
            var config = new MazeConfiguration { Rings = 5 };
            var sp = ServiceRegistration.BuildServiceProvider(config);

            Assert.NotNull(sp.GetRequiredService<IMazeGenerator>());
        }
    }
}
