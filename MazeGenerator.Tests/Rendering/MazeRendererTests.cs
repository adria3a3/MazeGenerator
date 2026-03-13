using MazeGenerator.Models;
using MazeGenerator.Rendering;
using MazeGenerator.Services;
using Xunit;

namespace MazeGenerator.Tests.Rendering
{
    public class MazeRendererTests
    {
        private static MazeGrid CreateFullMazeGrid(int rings = 2, int seed = 42) =>
            TestGridFactory.CreateGeneratedGrid(rings, seed);

        private static string TempPdfPath() =>
            Path.Combine(Path.GetTempPath(), $"maze_test_{Guid.NewGuid():N}.pdf");

        private static PdfMazeRenderer CreateRenderer() =>
            new PdfMazeRenderer(new MazeWallCalculator());

        [Fact]
        public void Render_CreatesOutputFile()
        {
            var renderer = CreateRenderer();
            var grid = CreateFullMazeGrid();
            var outputPath = TempPdfPath();

            try
            {
                renderer.Render(grid, null, outputPath);

                Assert.True(File.Exists(outputPath));
                Assert.True(new FileInfo(outputPath).Length > 0);
            }
            finally
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
            }
        }

        [Fact]
        public void Render_WithSolution_CreatesOutputFile()
        {
            var renderer = CreateRenderer();
            var grid = CreateFullMazeGrid(rings: 3);

            var pathFinder = new PathFinder();
            var generator = new DfsBacktrackerGenerator(42);
            var selector = new EntranceExitSelector(pathFinder);
            var (entrance, exit) = selector.FindOptimalAndCreateOpenings(grid, generator, 0);
            var solutionPath = pathFinder.FindPath(entrance, exit);
            var solution = new MazeSolution(entrance, exit, solutionPath, 50.0);

            Assert.NotEmpty(solution.Path);

            var outputPath = TempPdfPath();

            try
            {
                renderer.Render(grid, solution, outputPath);

                Assert.True(File.Exists(outputPath));
                Assert.True(new FileInfo(outputPath).Length > 0);
            }
            finally
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
            }
        }

        [Fact]
        public void Render_WithExitSolution_DrawsBoundaryOpenings()
        {
            var renderer = CreateRenderer();
            var grid = CreateFullMazeGrid();

            var entrance = grid.Cells[0][0];
            var exit = grid.Cells[grid.Cells.Count - 1][0];
            exit.IsExit = true;
            var solution = new MazeSolution(entrance, exit, new List<Cell>(), 0);

            var outputPath = TempPdfPath();

            try
            {
                renderer.Render(grid, solution, outputPath);

                Assert.True(File.Exists(outputPath));
                Assert.True(new FileInfo(outputPath).Length > 0);
            }
            finally
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
            }
        }

        [Fact]
        public void Render_NoSolution_DrawsCompleteBoundaries()
        {
            var renderer = CreateRenderer();
            var grid = CreateFullMazeGrid();

            var outputPath = TempPdfPath();

            try
            {
                renderer.Render(grid, null, outputPath);

                Assert.True(File.Exists(outputPath));
                Assert.True(new FileInfo(outputPath).Length > 0);
            }
            finally
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
            }
        }

        [Fact]
        public void Render_DegeneratePassageOverlap_DoesNotThrow()
        {
            var config = new MazeConfiguration
            {
                Rings = 2,
                PageWidth = 400,
                PageHeight = 600,
                Margin = 20,
                InnerRadius = 20,
                WallThickness = 2.0
            };
            var grid = new MazeGrid(config);

            var innerCell = new Cell
            {
                RingIndex = 0, CellIndex = 0,
                AngleStart = 0.15, AngleEnd = 0.25,
                RadiusInner = 20, RadiusOuter = 50
            };

            var outerCell = new Cell
            {
                RingIndex = 1, CellIndex = 0,
                AngleStart = 6.2, AngleEnd = 6.28,
                RadiusInner = 50, RadiusOuter = 80
            };

            innerCell.CreatePassage(outerCell);
            outerCell.CreatePassage(innerCell);
            innerCell.OutwardNeighbors.Add(outerCell);
            outerCell.InwardNeighbors.Add(innerCell);

            grid.Cells.Add(new List<Cell> { innerCell });
            grid.Cells.Add(new List<Cell> { outerCell });

            var renderer = CreateRenderer();
            var outputPath = Path.Combine(Path.GetTempPath(), $"maze_degenerate_{Guid.NewGuid():N}.pdf");
            try
            {
                renderer.Render(grid, null, outputPath);
                Assert.True(File.Exists(outputPath));
            }
            finally
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
            }
        }

        [Fact]
        public void Render_NullSolution_SkipsSolutionDrawing()
        {
            var renderer = CreateRenderer();
            var grid = CreateFullMazeGrid();

            var outputPath = TempPdfPath();

            try
            {
                renderer.Render(grid, null, outputPath);

                Assert.True(File.Exists(outputPath));
                Assert.True(new FileInfo(outputPath).Length > 0);
            }
            finally
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
            }
        }

        [Fact]
        public void FileExtension_IsPdf()
        {
            var renderer = CreateRenderer();
            Assert.Equal(".pdf", renderer.FileExtension);
        }
    }
}
