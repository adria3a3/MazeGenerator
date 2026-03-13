using MazeGenerator.Models;
using MazeGenerator.Services;
using Xunit;

namespace MazeGenerator.Tests.Services
{
    public class PathFinderTests
    {
        #region FindPath

        [Fact]
        public void FindPath_PathExists_ReturnsCorrectPath()
        {
            var pathFinder = new PathFinder();
            var entrance = new Cell();
            var cell2 = new Cell();
            var exit = new Cell();

            entrance.CreatePassage(cell2); cell2.CreatePassage(entrance);
            cell2.CreatePassage(exit); exit.CreatePassage(cell2);

            var path = pathFinder.FindPath(entrance, exit);

            Assert.Equal(3, path.Count);
            Assert.Equal(entrance, path[0]);
            Assert.Equal(cell2, path[1]);
            Assert.Equal(exit, path[2]);
        }

        [Fact]
        public void FindPath_NoPathExists_ReturnsEmptyList()
        {
            var pathFinder = new PathFinder();
            var entrance = new Cell();
            var exit = new Cell();

            var path = pathFinder.FindPath(entrance, exit);

            Assert.Empty(path);
        }

        [Fact]
        public void FindPath_EntranceIsExit_ReturnsSingleCell()
        {
            var pathFinder = new PathFinder();
            var cell = new Cell();

            var path = pathFinder.FindPath(cell, cell);

            Assert.Single(path);
            Assert.Equal(cell, path[0]);
        }

        [Fact]
        public void FindPath_BranchingGraph_ReturnsShortest()
        {
            var pathFinder = new PathFinder();
            var a = new Cell();
            var b = new Cell();
            var c = new Cell();
            var d = new Cell();

            a.CreatePassage(b); b.CreatePassage(a);
            a.CreatePassage(c); c.CreatePassage(a);
            b.CreatePassage(d); d.CreatePassage(b);
            c.CreatePassage(d); d.CreatePassage(c);

            var path = pathFinder.FindPath(a, d);

            Assert.Equal(3, path.Count);
            Assert.Equal(a, path[0]);
            Assert.Equal(d, path[2]);
        }

        #endregion

        #region CalculateCoverage

        [Fact]
        public void CalculateCoverage_ReturnsCorrectPercentage()
        {
            var pathFinder = new PathFinder();
            Assert.Equal(10.0, pathFinder.CalculateCoverage(10, 100));
        }

        [Fact]
        public void CalculateCoverage_ZeroTotalCells_ReturnsZero()
        {
            var pathFinder = new PathFinder();
            Assert.Equal(0.0, pathFinder.CalculateCoverage(5, 0));
        }

        [Fact]
        public void CalculateCoverage_FullCoverage_Returns100()
        {
            var pathFinder = new PathFinder();
            Assert.Equal(100.0, pathFinder.CalculateCoverage(50, 50));
        }

        #endregion

        #region FindDiameter

        [Fact]
        public void FindDiameter_LinearChain_ReturnsEndpoints()
        {
            var pathFinder = new PathFinder();
            var config = new MazeConfiguration { Rings = 1 };
            var grid = new MazeGrid(config);

            var cells = Enumerable.Range(0, 5).Select(i => new Cell { RingIndex = 0, CellIndex = i }).ToList();
            for (var i = 0; i < cells.Count - 1; i++)
            {
                cells[i].CreatePassage(cells[i + 1]);
                cells[i + 1].CreatePassage(cells[i]);
            }
            grid.Cells.Add(cells);

            var (ep1, ep2, path) = pathFinder.FindDiameter(grid);

            Assert.Equal(5, path.Count);
            Assert.True(
                (ep1 == cells[0] && ep2 == cells[4]) || (ep1 == cells[4] && ep2 == cells[0]));
        }

        [Fact]
        public void FindDiameter_SingleCell_ReturnsSameEndpoints()
        {
            var pathFinder = new PathFinder();
            var config = new MazeConfiguration { Rings = 1 };
            var grid = new MazeGrid(config);
            var cell = new Cell { RingIndex = 0, CellIndex = 0 };
            grid.Cells.Add(new List<Cell> { cell });

            var (ep1, ep2, path) = pathFinder.FindDiameter(grid);

            Assert.Equal(ep1, ep2);
            Assert.Single(path);
        }

        [Fact]
        public void FindDiameter_EmptyGrid_ThrowsArgumentException()
        {
            var pathFinder = new PathFinder();
            var grid = new MazeGrid(new MazeConfiguration { Rings = 1 });
            Assert.Throws<ArgumentException>(() => pathFinder.FindDiameter(grid));
        }

        [Fact]
        public void FindDiameter_EmptyInnerRing_ThrowsArgumentException()
        {
            var pathFinder = new PathFinder();
            var grid = new MazeGrid(new MazeConfiguration { Rings = 1 });
            grid.Cells.Add(new List<Cell>());
            Assert.Throws<ArgumentException>(() => pathFinder.FindDiameter(grid));
        }

        #endregion

        #region FindSolution

        [Fact]
        public void FindSolution_NullEntrance_ReturnsEmpty()
        {
            var pathFinder = new PathFinder();
            var grid = new MazeGrid(new MazeConfiguration { Rings = 1 });

            var result = pathFinder.FindSolution(grid);

            Assert.Empty(result);
        }

        [Fact]
        public void FindSolution_DirectPath_ReturnsCorrectPath()
        {
            var pathFinder = new PathFinder();
            var grid = new MazeGrid(new MazeConfiguration { Rings = 1 });
            var entrance = new Cell { RingIndex = 0, CellIndex = 0 };
            var middle = new Cell { RingIndex = 0, CellIndex = 1 };
            var exit = new Cell { RingIndex = 0, CellIndex = 2, IsExit = true };

            var solution = new MazeSolution(entrance, exit, new List<Cell>(), 0);
            grid.Solution = solution;
            grid.Cells.Add(new List<Cell> { entrance, middle, exit });

            entrance.CreatePassage(middle); middle.CreatePassage(entrance);
            middle.CreatePassage(exit); exit.CreatePassage(middle);

            var result = pathFinder.FindSolution(grid);

            Assert.Equal(new List<Cell> { entrance, middle, exit }, result);
        }

        [Fact]
        public void FindSolution_NullExit_ReturnsEmpty()
        {
            var pathFinder = new PathFinder();
            var grid = new MazeGrid(new MazeConfiguration { Rings = 1 });
            var entrance = new Cell { RingIndex = 0, CellIndex = 0 };
            var solution = new MazeSolution(entrance, new Cell(), new List<Cell>(), 0);
            grid.Solution = solution;
            grid.Cells.Add(new List<Cell> { entrance, new Cell() }); // No IsExit cell

            var result = pathFinder.FindSolution(grid);

            Assert.Empty(result);
        }

        [Fact]
        public void FindSolution_DisconnectedExit_ReturnsEmpty()
        {
            var pathFinder = new PathFinder();
            var grid = new MazeGrid(new MazeConfiguration { Rings = 1 });
            var entrance = new Cell { RingIndex = 0, CellIndex = 0 };
            var exit = new Cell { RingIndex = 0, CellIndex = 1, IsExit = true };

            var solution = new MazeSolution(entrance, exit, new List<Cell>(), 0);
            grid.Solution = solution;
            grid.Cells.Add(new List<Cell> { entrance, exit });

            var result = pathFinder.FindSolution(grid);

            Assert.Empty(result);
        }

        [Fact]
        public void FindSolution_MultiHopPath_ReturnsCorrectOrder()
        {
            var pathFinder = new PathFinder();
            var grid = new MazeGrid(new MazeConfiguration { Rings = 1 });
            var c1 = new Cell { RingIndex = 0, CellIndex = 0 };
            var c2 = new Cell { RingIndex = 0, CellIndex = 1 };
            var c3 = new Cell { RingIndex = 0, CellIndex = 2 };
            var c4 = new Cell { RingIndex = 0, CellIndex = 3, IsExit = true };

            var solution = new MazeSolution(c1, c4, new List<Cell>(), 0);
            grid.Solution = solution;
            grid.Cells.Add(new List<Cell> { c1, c2, c3, c4 });

            c1.CreatePassage(c2); c2.CreatePassage(c1);
            c2.CreatePassage(c3); c3.CreatePassage(c2);
            c3.CreatePassage(c4); c4.CreatePassage(c3);

            var result = pathFinder.FindSolution(grid);

            Assert.Equal(4, result.Count);
            Assert.Equal(c1, result[0]);
            Assert.Equal(c2, result[1]);
            Assert.Equal(c3, result[2]);
            Assert.Equal(c4, result[3]);
        }

        #endregion

        #region IPathFinder interface

        [Fact]
        public void PathFinder_ImplementsIPathFinder()
        {
            var pathFinder = new PathFinder();
            Assert.IsAssignableFrom<IPathFinder>(pathFinder);
        }

        #endregion
    }
}
