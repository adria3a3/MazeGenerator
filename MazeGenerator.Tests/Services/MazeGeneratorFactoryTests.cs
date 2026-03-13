using MazeGenerator.Models;
using MazeGenerator.Services;
using Xunit;

namespace MazeGenerator.Tests.Services
{
    public class MazeGeneratorFactoryTests
    {
        [Theory]
        [InlineData(MazeAlgorithm.DfsBacktracker, typeof(DfsBacktrackerGenerator))]
        [InlineData(MazeAlgorithm.Prims, typeof(PrimsGenerator))]
        [InlineData(MazeAlgorithm.Kruskals, typeof(KruskalsGenerator))]
        [InlineData(MazeAlgorithm.Wilsons, typeof(WilsonsGenerator))]
        public void Create_ReturnsCorrectType(MazeAlgorithm algorithm, Type expectedType)
        {
            var generator = MazeGeneratorFactory.Create(algorithm);
            Assert.IsType(expectedType, generator);
        }

        [Fact]
        public void Create_InvalidAlgorithm_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                MazeGeneratorFactory.Create((MazeAlgorithm)999));
        }

        [Fact]
        public void Create_PassesSeed()
        {
            var gen1 = MazeGeneratorFactory.Create(MazeAlgorithm.DfsBacktracker, 42);
            var gen2 = MazeGeneratorFactory.Create(MazeAlgorithm.DfsBacktracker, 42);

            var grid1 = TestGridFactory.CreateInitializedGrid();
            var grid2 = TestGridFactory.CreateInitializedGrid();
            gen1.GenerateMaze(grid1);
            gen2.GenerateMaze(grid2);

            var cells1 = grid1.GetAllCells().ToList();
            var cells2 = grid2.GetAllCells().ToList();
            for (var i = 0; i < cells1.Count; i++)
            {
                var p1 = cells1[i].Passages.Select(p => $"R{p.RingIndex}C{p.CellIndex}").OrderBy(s => s);
                var p2 = cells2[i].Passages.Select(p => $"R{p.RingIndex}C{p.CellIndex}").OrderBy(s => s);
                Assert.Equal(p1, p2);
            }
        }
    }
}
