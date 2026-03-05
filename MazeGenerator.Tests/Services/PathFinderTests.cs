using MazeGenerator.Models;
using MazeGenerator.Services;
using Moq;
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
            // Diamond graph: A -> B -> D, A -> C -> D
            // Both paths length 3, BFS should find one of them
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

        #region SelectEntrance / SelectExit

        [Fact]
        public void SelectEntrance_ReturnsFromInnerRing()
        {
            var pathFinder = new PathFinder();
            var config = new MazeConfiguration { Rings = 2 };
            var grid = new MazeGrid(config);
            var innerCell = new Cell { RingIndex = 0 };
            var outerCell = new Cell { RingIndex = 1 };
            grid.Cells.Add(new List<Cell> { innerCell });
            grid.Cells.Add(new List<Cell> { outerCell });

            var entrance = pathFinder.SelectEntrance(grid, new Random(0));

            Assert.Equal(0, entrance.RingIndex);
        }

        [Fact]
        public void SelectExit_ReturnsFromOuterRing_AndSetsIsExit()
        {
            var pathFinder = new PathFinder();
            var config = new MazeConfiguration { Rings = 2 };
            var grid = new MazeGrid(config);
            grid.Cells.Add(new List<Cell> { new Cell { RingIndex = 0 } });
            grid.Cells.Add(new List<Cell> { new Cell { RingIndex = 1 } });

            var exit = pathFinder.SelectExit(grid, new Random(0));

            Assert.Equal(1, exit.RingIndex);
            Assert.True(exit.IsExit);
        }

        [Fact]
        public void SelectEntrance_DeterministicWithSeed()
        {
            var pathFinder = new PathFinder();
            var config = new MazeConfiguration { Rings = 1 };
            var grid = new MazeGrid(config);
            grid.Cells.Add(new List<Cell>
            {
                new Cell { CellIndex = 0 },
                new Cell { CellIndex = 1 },
                new Cell { CellIndex = 2 },
                new Cell { CellIndex = 3 }
            });

            var entrance1 = pathFinder.SelectEntrance(grid, new Random(42));
            var entrance2 = pathFinder.SelectEntrance(grid, new Random(42));

            Assert.Equal(entrance1.CellIndex, entrance2.CellIndex);
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

        #endregion

        #region FindOptimalAndCreateOpenings

        [Fact]
        public void FindOptimal_SucceedsOnFirstTry()
        {
            var pathFinder = new PathFinder();
            var mockGenerator = new Mock<IMazeGenerator>();
            var config = new MazeConfiguration { Rings = 2, MinCoverage = 10 };
            var grid = new MazeGrid(config)
            {
                Cells = new List<List<Cell>>
                {
                    new List<Cell> { new Cell { RingIndex = 0, CellIndex = 0 } },
                    new List<Cell> { new Cell { RingIndex = 1, CellIndex = 0 } }
                }
            };

            var entrance = grid.Cells[0][0];
            var exit = grid.Cells[1][0];
            entrance.CreatePassage(exit); exit.CreatePassage(entrance);
            entrance.Visited = true; exit.Visited = true;

            var (foundEntrance, foundExit) = pathFinder.FindOptimalAndCreateOpenings(
                grid, mockGenerator.Object, new Random(0), config.MinCoverage);

            Assert.Equal(entrance, foundEntrance);
            Assert.Equal(exit, foundExit);
            Assert.True(foundExit.IsExit);
            mockGenerator.Verify(g => g.GenerateMaze(It.IsAny<MazeGrid>()), Times.Never);
        }

        [Fact]
        public void FindOptimal_RegeneratesWhenCoverageTooLow()
        {
            var pathFinder = new PathFinder();
            var mockGenerator = new Mock<IMazeGenerator>();
            var config = new MazeConfiguration { Rings = 2, MinCoverage = 80 };
            var grid = new MazeGrid(config);

            var entrance = new Cell { RingIndex = 0, CellIndex = 0 };
            var exit1 = new Cell { RingIndex = 1, CellIndex = 0 };
            var exit2 = new Cell { RingIndex = 1, CellIndex = 1 };

            grid.Cells.Add(new List<Cell> { entrance });
            grid.Cells.Add(new List<Cell> { exit1, exit2 });

            entrance.CreatePassage(exit1); exit1.CreatePassage(entrance);

            mockGenerator.Setup(g => g.GenerateMaze(It.IsAny<MazeGrid>()))
                .Callback<MazeGrid>(_ =>
                {
                    entrance.Passages.Clear();
                    exit1.Passages.Clear();
                    exit2.Passages.Clear();
                    entrance.CreatePassage(exit1); exit1.CreatePassage(entrance);
                    exit1.CreatePassage(exit2); exit2.CreatePassage(exit1);
                });

            var (foundEntrance, foundExit) = pathFinder.FindOptimalAndCreateOpenings(
                grid, mockGenerator.Object, new Random(0), config.MinCoverage, 2);

            Assert.Equal(0, foundEntrance.RingIndex);
            Assert.Equal(1, foundExit.RingIndex);
            Assert.True(foundExit.IsExit);
            mockGenerator.Verify(g => g.GenerateMaze(It.IsAny<MazeGrid>()), Times.Once);
        }

        [Fact]
        public void FindOptimal_Throws_WhenMaxRetriesExhausted()
        {
            var pathFinder = new PathFinder();
            var mockGenerator = new Mock<IMazeGenerator>();
            var config = new MazeConfiguration { Rings = 2, MinCoverage = 99 };
            var grid = new MazeGrid(config);

            var entrance = new Cell { RingIndex = 0, CellIndex = 0 };
            var exit = new Cell { RingIndex = 1, CellIndex = 0 };

            grid.Cells.Add(new List<Cell> { entrance });
            grid.Cells.Add(new List<Cell> { exit });

            entrance.CreatePassage(exit); exit.CreatePassage(entrance);

            // Coverage = 2/2 = 100% which is >= 99%, so this will actually succeed.
            // Use a grid with more cells to force low coverage.
            var extra = new Cell { RingIndex = 1, CellIndex = 1 };
            grid.Cells[1].Add(extra);

            // Now 3 cells, path of 2 = 66.7% < 99%
            Assert.Throws<InvalidOperationException>(() =>
                pathFinder.FindOptimalAndCreateOpenings(
                    grid, mockGenerator.Object, new Random(0), 99, maxRetries: 2));
        }

        [Fact]
        public void FindOptimal_MarksExitIsExit()
        {
            var pathFinder = new PathFinder();
            var mockGenerator = new Mock<IMazeGenerator>();
            var config = new MazeConfiguration { Rings = 2, MinCoverage = 0 };
            var grid = new MazeGrid(config);

            var entrance = new Cell { RingIndex = 0, CellIndex = 0 };
            var exit = new Cell { RingIndex = 1, CellIndex = 0 };
            grid.Cells.Add(new List<Cell> { entrance });
            grid.Cells.Add(new List<Cell> { exit });
            entrance.CreatePassage(exit); exit.CreatePassage(entrance);

            var (_, foundExit) = pathFinder.FindOptimalAndCreateOpenings(
                grid, mockGenerator.Object, new Random(0), 0);

            Assert.True(foundExit.IsExit);
        }

        [Fact]
        public void FindOptimal_CallsGenerateMaze_CorrectNumberOfTimes()
        {
            var pathFinder = new PathFinder();
            var mockGenerator = new Mock<IMazeGenerator>();
            var config = new MazeConfiguration { Rings = 2, MinCoverage = 99 };
            var grid = new MazeGrid(config);

            var entrance = new Cell { RingIndex = 0, CellIndex = 0 };
            var exit = new Cell { RingIndex = 1, CellIndex = 0 };
            var extra = new Cell { RingIndex = 1, CellIndex = 1 };

            grid.Cells.Add(new List<Cell> { entrance });
            grid.Cells.Add(new List<Cell> { exit, extra });
            entrance.CreatePassage(exit); exit.CreatePassage(entrance);

            try
            {
                pathFinder.FindOptimalAndCreateOpenings(
                    grid, mockGenerator.Object, new Random(0), 99, maxRetries: 3);
            }
            catch (InvalidOperationException) { }

            // Should call GenerateMaze for retries (maxRetries-1 times since last iteration doesn't call it)
            mockGenerator.Verify(g => g.GenerateMaze(It.IsAny<MazeGrid>()), Times.Exactly(2));
        }

        #endregion

        #region SelectEntrance / SelectExit guard throws

        [Fact]
        public void SelectEntrance_EmptyGrid_ThrowsArgumentException()
        {
            var pathFinder = new PathFinder();
            var grid = new MazeGrid(new MazeConfiguration { Rings = 1 }); // Cells list is empty
            Assert.Throws<ArgumentException>(() => pathFinder.SelectEntrance(grid, new Random(0)));
        }

        [Fact]
        public void SelectEntrance_EmptyInnerRing_ThrowsArgumentException()
        {
            var pathFinder = new PathFinder();
            var grid = new MazeGrid(new MazeConfiguration { Rings = 1 });
            grid.Cells.Add(new List<Cell>()); // ring 0 exists but has no cells
            Assert.Throws<ArgumentException>(() => pathFinder.SelectEntrance(grid, new Random(0)));
        }

        [Fact]
        public void SelectExit_EmptyGrid_ThrowsArgumentException()
        {
            var pathFinder = new PathFinder();
            var grid = new MazeGrid(new MazeConfiguration { Rings = 1 }); // Cells list is empty
            Assert.Throws<ArgumentException>(() => pathFinder.SelectExit(grid, new Random(0)));
        }

        [Fact]
        public void SelectExit_EmptyOuterRing_ThrowsArgumentException()
        {
            var pathFinder = new PathFinder();
            var grid = new MazeGrid(new MazeConfiguration { Rings = 1 });
            grid.Cells.Add(new List<Cell>()); // outer ring exists but has no cells
            Assert.Throws<ArgumentException>(() => pathFinder.SelectExit(grid, new Random(0)));
        }

        #endregion

        #region FindDiameter guard throws

        [Fact]
        public void FindDiameter_EmptyGrid_ThrowsArgumentException()
        {
            var pathFinder = new PathFinder();
            var grid = new MazeGrid(new MazeConfiguration { Rings = 1 }); // Cells list is empty
            Assert.Throws<ArgumentException>(() => pathFinder.FindDiameter(grid));
        }

        [Fact]
        public void FindDiameter_EmptyInnerRing_ThrowsArgumentException()
        {
            var pathFinder = new PathFinder();
            var grid = new MazeGrid(new MazeConfiguration { Rings = 1 });
            grid.Cells.Add(new List<Cell>()); // ring 0 exists but has no cells
            Assert.Throws<ArgumentException>(() => pathFinder.FindDiameter(grid));
        }

        #endregion

        #region FindOptimalAndCreateOpenings — unreachable outer ring

        [Fact]
        public void FindOptimalAndCreateOpenings_OuterRingUnreachable_ThrowsInvalidOperationException()
        {
            // Entrance connects only to inner-ring cells; outer ring is isolated.
            // FindFarthestCellInRing finds no reachable target-ring cells and throws.
            var pathFinder = new PathFinder();
            var mockGenerator = new Mock<IMazeGenerator>();
            var grid = new MazeGrid(new MazeConfiguration { Rings = 2 });

            var entrance = new Cell { RingIndex = 0, CellIndex = 0 };
            var inner2   = new Cell { RingIndex = 0, CellIndex = 1 };
            var outerCell = new Cell { RingIndex = 1, CellIndex = 0 };

            entrance.CreatePassage(inner2);
            inner2.CreatePassage(entrance);

            grid.Cells.Add(new List<Cell> { entrance, inner2 });
            grid.Cells.Add(new List<Cell> { outerCell });

            Assert.Throws<InvalidOperationException>(() =>
                pathFinder.FindOptimalAndCreateOpenings(
                    grid, mockGenerator.Object, new Random(0), minCoverage: 0, maxRetries: 1));
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

            grid.Entrance = entrance;
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
            grid.Entrance = entrance;
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

            grid.Entrance = entrance;
            grid.Cells.Add(new List<Cell> { entrance, exit });
            // No passages between them

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

            grid.Entrance = c1;
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
    }
}



