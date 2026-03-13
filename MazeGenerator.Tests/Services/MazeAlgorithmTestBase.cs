using MazeGenerator.Models;
using MazeGenerator.Services;
using Xunit;

namespace MazeGenerator.Tests.Services
{
    public abstract class MazeAlgorithmTestBase
    {
        protected abstract IMazeGenerator CreateGenerator(int? seed = null);

        private MazeGrid CreateInitializedGrid(int rings = 3) =>
            TestGridFactory.CreateInitializedGrid(rings);

        [Fact]
        public void GenerateMaze_AllCellsVisited()
        {
            var grid = CreateInitializedGrid();
            var generator = CreateGenerator(seed: 42);
            generator.GenerateMaze(grid);
            Assert.All(grid.GetAllCells(), cell => Assert.True(cell.Visited));
        }

        [Fact]
        public void GenerateMaze_CreatesSpanningTree_NMinus1Passages()
        {
            var grid = CreateInitializedGrid();
            var generator = CreateGenerator(seed: 42);
            generator.GenerateMaze(grid);

            var totalConnections = grid.GetAllCells().Sum(c => c.Passages.Count);
            var passages = totalConnections / 2;
            Assert.Equal(grid.TotalCells - 1, passages);
        }

        [Fact]
        public void GenerateMaze_NoCycles()
        {
            var grid = CreateInitializedGrid();
            var generator = CreateGenerator(seed: 42);
            generator.GenerateMaze(grid);
            Assert.True(MazeValidation.IsPerfectMaze(grid));
        }

        [Fact]
        public void GenerateMaze_DeterministicWithSameSeed()
        {
            var grid1 = CreateInitializedGrid();
            var grid2 = CreateInitializedGrid();
            var gen1 = CreateGenerator(seed: 123);
            var gen2 = CreateGenerator(seed: 123);

            gen1.GenerateMaze(grid1);
            gen2.GenerateMaze(grid2);

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

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(10)]
        public void GenerateMaze_VariousSizes_ProducePerfectMaze(int rings)
        {
            var grid = CreateInitializedGrid(rings);
            var generator = CreateGenerator(seed: 42);
            generator.GenerateMaze(grid);

            Assert.True(MazeValidation.IsPerfectMaze(grid));
        }

        [Fact]
        public void GenerateMaze_ResetsState()
        {
            var grid = CreateInitializedGrid();
            var generator = CreateGenerator(seed: 42);

            generator.GenerateMaze(grid);
            generator.GenerateMaze(grid);

            Assert.True(MazeValidation.IsPerfectMaze(grid));
        }
    }
}
