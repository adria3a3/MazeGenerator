using MazeGenerator.Models;
using Xunit;

namespace MazeGenerator.Tests.Services
{
    public class GridBuilderTests
    {
        private static MazeGrid CreateGrid(int rings = 3) =>
            TestGridFactory.CreateInitializedGrid(rings);

        [Fact]
        public void BuildGrid_CreatesCells_ForAllRings()
        {
            var grid = CreateGrid(3);

            Assert.Equal(3, grid.Cells.Count);
            foreach (var ring in grid.Cells)
            {
                Assert.NotEmpty(ring);
            }
            Assert.True(grid.TotalCells > 0);
        }

        [Fact]
        public void BuildGrid_EstablishesClockwiseNeighbors_WithWrapAround()
        {
            var grid = CreateGrid(2);
            var ring = grid.Cells[0];

            // Last cell's clockwise neighbor should be the first cell (wrap-around)
            var lastCell = ring[ring.Count - 1];
            Assert.Equal(ring[0], lastCell.ClockwiseNeighbor);

            // First cell's counter-clockwise neighbor should be the last cell
            Assert.Equal(ring[ring.Count - 1], ring[0].CounterClockwiseNeighbor);
        }

        [Fact]
        public void BuildGrid_EstablishesInwardNeighbors()
        {
            var grid = CreateGrid(3);

            // Cells in ring 1 should have inward neighbors in ring 0
            foreach (var cell in grid.Cells[1])
            {
                Assert.NotEmpty(cell.InwardNeighbors);
                Assert.All(cell.InwardNeighbors, n => Assert.Equal(0, n.RingIndex));
            }
        }

        [Fact]
        public void BuildGrid_EstablishesOutwardNeighbors()
        {
            var grid = CreateGrid(3);

            // Cells in ring 0 should have outward neighbors in ring 1
            foreach (var cell in grid.Cells[0])
            {
                Assert.NotEmpty(cell.OutwardNeighbors);
                Assert.All(cell.OutwardNeighbors, n => Assert.Equal(1, n.RingIndex));
            }
        }

        [Fact]
        public void BuildGrid_ZeroRings_ThrowsInvalidOperationException()
        {
            // rings=0 causes CalculateCellCounts to return [], ValidateCellCounts([]) → false → throw.
            var grid = new MazeGrid(new MazeConfiguration { Rings = 0 });
            Assert.Throws<InvalidOperationException>(() => grid.Initialize());
        }

        [Fact]
        public void BuildGrid_CellsHaveCorrectAnglesAndRadii()
        {
            var grid = CreateGrid(2);
            var firstCell = grid.Cells[0][0];

            // First cell starts at angle 0
            Assert.Equal(0.0, firstCell.AngleStart);
            Assert.True(firstCell.AngleEnd > 0);

            // Inner radius of ring 0 should equal the configured inner radius
            Assert.Equal(grid.Configuration.InnerRadius, firstCell.RadiusInner);
            Assert.True(firstCell.RadiusOuter > firstCell.RadiusInner);
        }
    }
}


