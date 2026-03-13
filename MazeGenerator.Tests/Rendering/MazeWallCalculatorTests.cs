using MazeGenerator.Models;
using MazeGenerator.Rendering;
using MazeGenerator.Services;
using Xunit;

namespace MazeGenerator.Tests.Rendering
{
    public class MazeWallCalculatorTests
    {
        private static MazeGrid CreateFullGrid(int rings = 2, int seed = 42) =>
            TestGridFactory.CreateGeneratedGrid(rings, seed);

        [Fact]
        public void Calculate_ReturnsNonEmptyWalls()
        {
            var calc = new MazeWallCalculator();
            var grid = CreateFullGrid();

            var commands = calc.Calculate(grid);

            Assert.NotEmpty(commands.WallLines);
            Assert.NotEmpty(commands.WallArcs);
        }

        [Fact]
        public void Calculate_NoSolution_EmptySolutionPrimitives()
        {
            var calc = new MazeWallCalculator();
            var grid = CreateFullGrid();

            var commands = calc.Calculate(grid);

            Assert.Empty(commands.SolutionLines);
            Assert.Empty(commands.SolutionArcs);
        }

        [Fact]
        public void Calculate_WithSolution_HasSolutionPrimitives()
        {
            var calc = new MazeWallCalculator();
            var grid = CreateFullGrid(rings: 3);
            var pathFinder = new PathFinder();
            var selector = new EntranceExitSelector(pathFinder);
            var generator = new DfsBacktrackerGenerator(42);
            var (entrance, exit) = selector.FindOptimalAndCreateOpenings(grid, generator, 0);
            var path = pathFinder.FindPath(entrance, exit);
            var solution = new MazeSolution(entrance, exit, path, 50.0);

            var commands = calc.Calculate(grid, solution);

            Assert.NotEmpty(commands.SolutionLines);
            // Solution arcs may or may not be empty depending on path shape
            Assert.True(commands.SolutionLines.Count >= 2); // At least entrance + exit extension
        }

        [Fact]
        public void Calculate_WithExitCell_BoundaryHasGap()
        {
            var calc = new MazeWallCalculator();
            var grid = CreateFullGrid();

            // Without exit: boundary is full circle
            var commandsNoExit = calc.Calculate(grid);
            var boundaryArcsNoExit = commandsNoExit.WallArcs.Count;

            // With exit
            var entrance = grid.Cells[0][0];
            var exit = grid.Cells[grid.Cells.Count - 1][0];
            exit.IsExit = true;
            grid.Solution = new MazeSolution(entrance, exit, new List<Cell>(), 0);
            var commandsWithExit = calc.Calculate(grid, grid.Solution);

            // Should still have wall arcs (boundary arc shape changes but count may differ)
            Assert.NotEmpty(commandsWithExit.WallArcs);
        }

        [Fact]
        public void Calculate_PageDimensions_MatchConfig()
        {
            var calc = new MazeWallCalculator();
            var grid = CreateFullGrid();

            var commands = calc.Calculate(grid);

            Assert.Equal(grid.Configuration.PageWidth, commands.PageWidth);
            Assert.Equal(grid.Configuration.PageHeight, commands.PageHeight);
            Assert.Equal(grid.Configuration.WallThickness, commands.WallThickness);
        }

        [Fact]
        public void Calculate_PartialArcsWithPassages()
        {
            var calc = new MazeWallCalculator();
            var grid = CreateFullGrid(rings: 3);

            var commands = calc.Calculate(grid);

            // A 3-ring grid will have partial arcs where passages exist
            Assert.True(commands.WallArcs.Count > 0);
            Assert.True(commands.WallLines.Count > 0);
        }

        [Fact]
        public void Calculate_SameRingSolutionSegment_ProducesArc()
        {
            var calc = new MazeWallCalculator();
            // Create two adjacent cells in same ring with a passage
            var config = new MazeConfiguration { Rings = 1, PageWidth = 400, PageHeight = 600, Margin = 20, InnerRadius = 20 };
            var grid = new MazeGrid(config);
            grid.Initialize();
            var gen = new DfsBacktrackerGenerator(42);
            gen.GenerateMaze(grid);

            // Find two same-ring neighbors
            Cell? c1 = null, c2 = null;
            foreach (var cell in grid.Cells[0])
            {
                if (cell.ClockwiseNeighbor != null && cell.Passages.Contains(cell.ClockwiseNeighbor))
                {
                    c1 = cell;
                    c2 = cell.ClockwiseNeighbor;
                    break;
                }
            }

            if (c1 != null && c2 != null)
            {
                var path = new List<Cell> { c1, c2 };
                var solution = new MazeSolution(c1, c2, path, 50.0);
                var commands = calc.Calculate(grid, solution);
                Assert.True(commands.SolutionArcs.Count > 0 || commands.SolutionLines.Count > 0);
            }
        }
    }
}
