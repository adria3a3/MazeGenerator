using MazeGenerator.Models;
using Xunit;

namespace MazeGenerator.Tests.Models
{
    public class MazeGridTests
    {
        private static MazeConfiguration DefaultConfig(
            double pageWidth = 400, double pageHeight = 600, double margin = 20, double innerRadius = 10, int rings = 5)
        {
            return new MazeConfiguration
            {
                PageWidth = pageWidth,
                PageHeight = pageHeight,
                Margin = margin,
                InnerRadius = innerRadius,
                Rings = rings
            };
        }

        [Fact]
        public void Constructor_CalculatesCenterX()
        {
            var grid = new MazeGrid(DefaultConfig(pageWidth: 400));
            Assert.Equal(200.0, grid.CenterX);
        }

        [Fact]
        public void Constructor_CalculatesCenterY()
        {
            var grid = new MazeGrid(DefaultConfig(pageHeight: 600));
            Assert.Equal(300.0, grid.CenterY);
        }

        [Fact]
        public void Constructor_CalculatesUsableRadius_UsesMinOfXY()
        {
            // Landscape: width > height, so min is based on height
            var config = DefaultConfig(pageWidth: 800, pageHeight: 400, margin: 20);
            var grid = new MazeGrid(config);

            var expectedMaxX = (800.0 / 2.0) - 20.0; // 380
            var expectedMaxY = (400.0 / 2.0) - 20.0; // 180
            Assert.Equal(Math.Min(expectedMaxX, expectedMaxY), grid.UsableRadius);
            Assert.Equal(180.0, grid.UsableRadius);
        }

        [Fact]
        public void Constructor_CalculatesRingWidth()
        {
            var config = DefaultConfig(pageWidth: 400, pageHeight: 600, margin: 20, innerRadius: 10, rings: 5);
            var grid = new MazeGrid(config);

            // UsableRadius = min(400/2 - 20, 600/2 - 20) = min(180, 280) = 180
            var expected = (180.0 - 10.0) / 5.0; // 34.0
            Assert.Equal(expected, grid.RingWidth);
        }

        [Fact]
        public void TotalCells_SumsAllRings()
        {
            var grid = new MazeGrid(DefaultConfig());
            grid.Cells.Add(new List<Cell> { new Cell(), new Cell(), new Cell() });
            grid.Cells.Add(new List<Cell> { new Cell(), new Cell(), new Cell(), new Cell(), new Cell() });

            Assert.Equal(8, grid.TotalCells);
        }

        [Fact]
        public void TotalCells_EmptyGrid_ReturnsZero()
        {
            var grid = new MazeGrid(DefaultConfig());
            Assert.Equal(0, grid.TotalCells);
        }

        [Fact]
        public void GetExitCell_ReturnsFirstCellWithIsExit()
        {
            var grid = new MazeGrid(DefaultConfig());
            var normalCell = new Cell { IsExit = false };
            var exitCell = new Cell { IsExit = true };
            grid.Cells.Add(new List<Cell> { normalCell, exitCell });

            var result = grid.GetExitCell();

            Assert.Equal(exitCell, result);
        }

        [Fact]
        public void GetExitCell_NoExitCell_ReturnsNull()
        {
            var grid = new MazeGrid(DefaultConfig());
            grid.Cells.Add(new List<Cell> { new Cell(), new Cell() });

            var result = grid.GetExitCell();

            Assert.Null(result);
        }
    }
}



