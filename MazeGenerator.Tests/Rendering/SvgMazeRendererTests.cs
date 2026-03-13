using MazeGenerator.Models;
using MazeGenerator.Rendering;
using MazeGenerator.Services;
using Xunit;

namespace MazeGenerator.Tests.Rendering
{
    public class SvgMazeRendererTests
    {
        [Fact]
        public void Render_CreatesValidSvgFile()
        {
            var renderer = new SvgMazeRenderer(new MazeWallCalculator());
            var grid = TestGridFactory.CreateGeneratedGrid(2);
            var path = Path.Combine(Path.GetTempPath(), $"maze_test_{Guid.NewGuid():N}.svg");

            try
            {
                renderer.Render(grid, null, path);

                Assert.True(File.Exists(path));
                var content = File.ReadAllText(path);
                Assert.Contains("<svg", content);
                Assert.Contains("</svg>", content);
                Assert.Contains("stroke=\"black\"", content);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public void Render_WithSolution_ContainsRedStroke()
        {
            var renderer = new SvgMazeRenderer(new MazeWallCalculator());
            var grid = TestGridFactory.CreateGeneratedGrid(3);
            var pathFinder = new PathFinder();
            var selector = new EntranceExitSelector(pathFinder);
            var generator = new DfsBacktrackerGenerator(42);
            var (entrance, exit) = selector.FindOptimalAndCreateOpenings(grid, generator, 0);
            var solutionPath = pathFinder.FindPath(entrance, exit);
            var solution = new MazeSolution(entrance, exit, solutionPath, 50.0);

            var filePath = Path.Combine(Path.GetTempPath(), $"maze_test_{Guid.NewGuid():N}.svg");

            try
            {
                renderer.Render(grid, solution, filePath);

                var content = File.ReadAllText(filePath);
                Assert.Contains("stroke=\"red\"", content);
            }
            finally
            {
                if (File.Exists(filePath)) File.Delete(filePath);
            }
        }

        [Fact]
        public void FileExtension_IsSvg()
        {
            var renderer = new SvgMazeRenderer(new MazeWallCalculator());
            Assert.Equal(".svg", renderer.FileExtension);
        }
    }
}
