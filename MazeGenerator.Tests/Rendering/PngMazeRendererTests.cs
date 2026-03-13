using MazeGenerator.Models;
using MazeGenerator.Rendering;
using MazeGenerator.Services;
using Xunit;

namespace MazeGenerator.Tests.Rendering
{
    public class PngMazeRendererTests
    {
        [Fact]
        public void Render_CreatesValidPngFile()
        {
            var renderer = new PngMazeRenderer(new MazeWallCalculator());
            var grid = TestGridFactory.CreateGeneratedGrid(2);
            var path = Path.Combine(Path.GetTempPath(), $"maze_test_{Guid.NewGuid():N}.png");

            try
            {
                renderer.Render(grid, null, path);

                Assert.True(File.Exists(path));
                var bytes = File.ReadAllBytes(path);
                Assert.True(bytes.Length > 100);
                // PNG magic bytes
                Assert.Equal(0x89, bytes[0]);
                Assert.Equal(0x50, bytes[1]); // P
                Assert.Equal(0x4E, bytes[2]); // N
                Assert.Equal(0x47, bytes[3]); // G
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public void Render_WithSolution_CreatesValidPngFile()
        {
            var renderer = new PngMazeRenderer(new MazeWallCalculator());
            var grid = TestGridFactory.CreateGeneratedGrid(3);
            var pathFinder = new PathFinder();
            var selector = new EntranceExitSelector(pathFinder);
            var generator = new DfsBacktrackerGenerator(42);
            var (entrance, exit) = selector.FindOptimalAndCreateOpenings(grid, generator, 0);
            var solutionPath = pathFinder.FindPath(entrance, exit);
            var solution = new MazeSolution(entrance, exit, solutionPath, 50.0);

            var path = Path.Combine(Path.GetTempPath(), $"maze_test_{Guid.NewGuid():N}.png");

            try
            {
                renderer.Render(grid, solution, path);

                Assert.True(File.Exists(path));
                var bytes = File.ReadAllBytes(path);
                Assert.True(bytes.Length > 100);
                Assert.Equal(0x89, bytes[0]);
                Assert.Equal(0x50, bytes[1]);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public void FileExtension_IsPng()
        {
            var renderer = new PngMazeRenderer(new MazeWallCalculator());
            Assert.Equal(".png", renderer.FileExtension);
        }
    }
}
