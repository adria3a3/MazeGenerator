using MazeGenerator.Models;
using Xunit;

namespace MazeGenerator.Tests.Services
{
    public class MazeGeneratorServiceTests
    {
        private static MazeGrid CreateInitializedGrid(int rings = 3) =>
            TestGridFactory.CreateInitializedGrid(rings);

        [Fact]
        public void GenerateMaze_AllCellsVisited()
        {
            var grid = CreateInitializedGrid();
            var generator = new MazeGenerator.Services.DfsBacktrackerGenerator(seed: 42);

            generator.GenerateMaze(grid);

            Assert.All(grid.GetAllCells(), cell => Assert.True(cell.Visited));
        }

        [Fact]
        public void GenerateMaze_CreatesSpanningTree_NMinus1Passages()
        {
            var grid = CreateInitializedGrid();
            var generator = new MazeGenerator.Services.DfsBacktrackerGenerator(seed: 42);

            generator.GenerateMaze(grid);

            var totalConnections = grid.GetAllCells().Sum(c => c.Passages.Count);
            var passages = totalConnections / 2; // Each passage counted twice
            Assert.Equal(grid.TotalCells - 1, passages);
        }

        [Fact]
        public void GenerateMaze_ResetsStateBeforeGeneration()
        {
            var grid = CreateInitializedGrid();
            var generator = new MazeGenerator.Services.DfsBacktrackerGenerator(seed: 42);

            // Set up some state that should be cleared
            generator.GenerateMaze(grid);
            var entrance = grid.Cells[0][0];
            var exit = grid.Cells[grid.Cells.Count - 1][0];
            grid.Solution = new MazeSolution(entrance, exit, new List<Cell> { entrance }, 10);

            // Regenerate — state should be reset
            generator.GenerateMaze(grid);

            Assert.Null(grid.Solution);
        }

        [Fact]
        public void GenerateMaze_DeterministicWithSameSeed()
        {
            var grid1 = CreateInitializedGrid();
            var grid2 = CreateInitializedGrid();
            var gen1 = new MazeGenerator.Services.DfsBacktrackerGenerator(seed: 123);
            var gen2 = new MazeGenerator.Services.DfsBacktrackerGenerator(seed: 123);

            gen1.GenerateMaze(grid1);
            gen2.GenerateMaze(grid2);

            // Same seed should produce identical passage structures
            var cells1 = grid1.GetAllCells().ToList();
            var cells2 = grid2.GetAllCells().ToList();
            Assert.Equal(cells1.Count, cells2.Count);

            for (var i = 0; i < cells1.Count; i++)
            {
                var passages1 = cells1[i].Passages.Select(p => $"R{p.RingIndex}C{p.CellIndex}").OrderBy(s => s).ToList();
                var passages2 = cells2[i].Passages.Select(p => $"R{p.RingIndex}C{p.CellIndex}").OrderBy(s => s).ToList();
                Assert.Equal(passages1, passages2);
            }
        }

        [Fact]
        public void GenerateMaze_DifferentSeeds_ProduceDifferentMazes()
        {
            var grid1 = CreateInitializedGrid(5);
            var grid2 = CreateInitializedGrid(5);
            var gen1 = new MazeGenerator.Services.DfsBacktrackerGenerator(seed: 1);
            var gen2 = new MazeGenerator.Services.DfsBacktrackerGenerator(seed: 9999);

            gen1.GenerateMaze(grid1);
            gen2.GenerateMaze(grid2);

            // At least one cell should have different passages
            var cells1 = grid1.GetAllCells().ToList();
            var cells2 = grid2.GetAllCells().ToList();
            var anyDifference = false;
            for (var i = 0; i < cells1.Count; i++)
            {
                var p1 = cells1[i].Passages.Select(p => $"R{p.RingIndex}C{p.CellIndex}").OrderBy(s => s).ToList();
                var p2 = cells2[i].Passages.Select(p => $"R{p.RingIndex}C{p.CellIndex}").OrderBy(s => s).ToList();
                if (!p1.SequenceEqual(p2))
                {
                    anyDifference = true;
                    break;
                }
            }
            Assert.True(anyDifference, "Different seeds should produce different mazes");
        }

        [Fact]
        public void GenerateMaze_BacktracksWhenNoUnvisitedNeighbors()
        {
            // A small grid forces frequent backtracking
            var grid = CreateInitializedGrid(1);
            var generator = new MazeGenerator.Services.DfsBacktrackerGenerator(seed: 42);

            generator.GenerateMaze(grid);

            // If backtracking works, all cells should still be visited
            Assert.All(grid.GetAllCells(), cell => Assert.True(cell.Visited));
            // And the maze should be a valid spanning tree
            var passages = grid.GetAllCells().Sum(c => c.Passages.Count) / 2;
            Assert.Equal(grid.TotalCells - 1, passages);
        }
    }
}


