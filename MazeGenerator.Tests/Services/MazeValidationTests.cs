using MazeGenerator.Models;
using MazeGenerator.Services;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MazeGenerator.Tests.Services
{
    public class MazeValidationTests
    {
        private static MazeGrid CreateGeneratedGrid(int rings = 3, int seed = 42) =>
            TestGridFactory.CreateGeneratedGrid(rings, seed);

        [Fact]
        public void IsPerfectMaze_EmptyGrid_ReturnsFalse()
        {
            var config = new MazeConfiguration { Rings = 1 };
            var grid = new MazeGrid(config);
            // No cells added

            Assert.False(MazeValidation.IsPerfectMaze(grid));
        }

        [Fact]
        public void IsPerfectMaze_PerfectMaze_ReturnsTrue()
        {
            var grid = CreateGeneratedGrid();

            Assert.True(MazeValidation.IsPerfectMaze(grid));
        }

        [Fact]
        public void IsPerfectMaze_NotAllVisited_ReturnsFalse()
        {
            var grid = CreateGeneratedGrid();

            // Unmark one cell as visited
            grid.Cells[0][0].Visited = false;

            Assert.False(MazeValidation.IsPerfectMaze(grid));
        }

        [Fact]
        public void IsPerfectMaze_TooManyPassages_ReturnsFalse()
        {
            var grid = CreateGeneratedGrid();

            // Find two cells that do NOT already have a passage between them
            var allCells = grid.GetAllCells().ToList();
            Cell? cell1 = null, cell2 = null;
            for (var i = 0; i < allCells.Count && cell1 == null; i++)
            {
                for (var j = i + 1; j < allCells.Count; j++)
                {
                    if (!allCells[i].Passages.Contains(allCells[j]))
                    {
                        cell1 = allCells[i];
                        cell2 = allCells[j];
                        break;
                    }
                }
            }

            // Add an extra passage to break the spanning tree property
            cell1!.CreatePassage(cell2!);
            cell2!.CreatePassage(cell1!);

            Assert.False(MazeValidation.IsPerfectMaze(grid));
        }

        [Fact]
        public void IsPerfectMaze_WithCycle_ReturnsFalse()
        {
            var config = new MazeConfiguration { Rings = 1 };
            var grid = new MazeGrid(config);

            // Create a triangle cycle manually: A-B-C-A
            var a = new Cell { RingIndex = 0, CellIndex = 0, Visited = true };
            var b = new Cell { RingIndex = 0, CellIndex = 1, Visited = true };
            var c = new Cell { RingIndex = 0, CellIndex = 2, Visited = true };

            a.CreatePassage(b); b.CreatePassage(a);
            b.CreatePassage(c); c.CreatePassage(b);
            c.CreatePassage(a); a.CreatePassage(c);

            grid.Cells.Add(new List<Cell> { a, b, c });

            Assert.False(MazeValidation.IsPerfectMaze(grid));
        }

        [Fact]
        public void HasCycles_EmptyGrid_ReturnsFalse()
        {
            var config = new MazeConfiguration { Rings = 1 };
            var grid = new MazeGrid(config);

            Assert.False(MazeValidation.HasCycles(grid));
        }

        [Fact]
        public void HasCycles_TreeGraph_ReturnsFalse()
        {
            var grid = CreateGeneratedGrid();

            // A properly generated maze is a spanning tree — no cycles
            Assert.False(MazeValidation.HasCycles(grid));
        }

        [Fact]
        public void HasCycles_WithCycle_ReturnsTrue()
        {
            var config = new MazeConfiguration { Rings = 1 };
            var grid = new MazeGrid(config);

            var a = new Cell { RingIndex = 0, CellIndex = 0 };
            var b = new Cell { RingIndex = 0, CellIndex = 1 };
            var c = new Cell { RingIndex = 0, CellIndex = 2 };

            a.CreatePassage(b); b.CreatePassage(a);
            b.CreatePassage(c); c.CreatePassage(b);
            c.CreatePassage(a); a.CreatePassage(c);

            grid.Cells.Add(new List<Cell> { a, b, c });

            Assert.True(MazeValidation.HasCycles(grid));
        }

        [Fact]
        public void HasCycles_SingleIsolatedNode_ReturnsFalse()
        {
            var config = new MazeConfiguration { Rings = 1 };
            var grid = new MazeGrid(config);
            var a = new Cell { RingIndex = 0, CellIndex = 0 };
            grid.Cells.Add(new List<Cell> { a });

            Assert.False(MazeValidation.HasCycles(grid));
        }

        [Fact]
        public void HasCycles_DisconnectedComponents_NoCycles_ReturnsFalse()
        {
            var config = new MazeConfiguration { Rings = 1 };
            var grid = new MazeGrid(config);

            // Two disconnected pairs (trees), no cycles
            var a = new Cell { RingIndex = 0, CellIndex = 0 };
            var b = new Cell { RingIndex = 0, CellIndex = 1 };
            var c = new Cell { RingIndex = 0, CellIndex = 2 };
            var d = new Cell { RingIndex = 0, CellIndex = 3 };

            a.CreatePassage(b); b.CreatePassage(a);
            c.CreatePassage(d); d.CreatePassage(c);

            grid.Cells.Add(new List<Cell> { a, b, c, d });

            Assert.False(MazeValidation.HasCycles(grid));
        }
    }
}


