using MazeGenerator.Models;
using Xunit;

namespace MazeGenerator.Tests.Models
{
    public class CellTests
    {
        [Fact]
        public void DefaultPropertyValues_AreCorrect()
        {
            var cell = new Cell();

            Assert.Equal(0, cell.RingIndex);
            Assert.Equal(0, cell.CellIndex);
            Assert.Equal(0.0, cell.AngleStart);
            Assert.Equal(0.0, cell.AngleEnd);
            Assert.Equal(0.0, cell.RadiusInner);
            Assert.Equal(0.0, cell.RadiusOuter);
            Assert.False(cell.Visited);
            Assert.False(cell.IsExit);
            Assert.Null(cell.ClockwiseNeighbor);
            Assert.Null(cell.CounterClockwiseNeighbor);
        }

        [Fact]
        public void InwardNeighbors_InitializedEmpty()
        {
            var cell = new Cell();
            Assert.Empty(cell.InwardNeighbors);
        }

        [Fact]
        public void OutwardNeighbors_InitializedEmpty()
        {
            var cell = new Cell();
            Assert.Empty(cell.OutwardNeighbors);
        }

        [Fact]
        public void GetPassableNeighbors_ReturnsPassagesList()
        {
            var cell = new Cell();
            var neighbor = new Cell();
            cell.CreatePassage(neighbor);

            var passable = cell.GetPassableNeighbors();

            Assert.Single(passable);
            Assert.Contains(neighbor, passable);
        }

        [Fact]
        public void CreatePassage_AddsNeighborToPassages()
        {
            var cell = new Cell();
            var neighbor = new Cell();

            cell.CreatePassage(neighbor);

            Assert.Single(cell.Passages);
            Assert.Contains(neighbor, cell.Passages);
        }

        [Fact]
        public void CreatePassage_DoesNotAddDuplicate()
        {
            var cell = new Cell();
            var neighbor = new Cell();

            cell.CreatePassage(neighbor);
            cell.CreatePassage(neighbor);

            Assert.Single(cell.Passages);
        }

        [Fact]
        public void ClearPassages_RemovesAllPassages()
        {
            var cell = new Cell();
            var n1 = new Cell();
            var n2 = new Cell();
            cell.CreatePassage(n1);
            cell.CreatePassage(n2);

            cell.ClearPassages();

            Assert.Empty(cell.Passages);
            Assert.Empty(cell.GetPassableNeighbors());
        }

        [Fact]
        public void Passages_IsReadOnly()
        {
            var cell = new Cell();
            cell.CreatePassage(new Cell());

            var passages = cell.Passages;

            Assert.IsAssignableFrom<IReadOnlyList<Cell>>(passages);
            Assert.Single(passages);
        }

        [Fact]
        public void InitOnlyProperties_CanBeSetDuringInitialization()
        {
            var cell = new Cell
            {
                RingIndex = 2,
                CellIndex = 5,
                AngleStart = 1.0,
                AngleEnd = 2.0,
                RadiusInner = 10.0,
                RadiusOuter = 20.0
            };

            Assert.Equal(2, cell.RingIndex);
            Assert.Equal(5, cell.CellIndex);
            Assert.Equal(1.0, cell.AngleStart);
            Assert.Equal(2.0, cell.AngleEnd);
            Assert.Equal(10.0, cell.RadiusInner);
            Assert.Equal(20.0, cell.RadiusOuter);
        }

        [Fact]
        public void ToString_ReturnsFormattedString()
        {
            var cell = new Cell { RingIndex = 3, CellIndex = 7 };

            Assert.Equal("Cell[R3,C7]", cell.ToString());
        }
    }
}

