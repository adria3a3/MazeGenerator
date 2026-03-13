using MazeGenerator.Models;
using MazeGenerator.Services;
using Moq;
using Xunit;

namespace MazeGenerator.Tests.Services
{
    public class EntranceExitSelectorTests
    {
        #region SelectEntrance / SelectExit

        [Fact]
        public void SelectEntrance_ReturnsFromInnerRing()
        {
            var selector = new EntranceExitSelector(new PathFinder());
            var config = new MazeConfiguration { Rings = 2 };
            var grid = new MazeGrid(config);
            var innerCell = new Cell { RingIndex = 0 };
            var outerCell = new Cell { RingIndex = 1 };
            grid.Cells.Add(new List<Cell> { innerCell });
            grid.Cells.Add(new List<Cell> { outerCell });

            var entrance = selector.SelectEntrance(grid, new Random(0));

            Assert.Equal(0, entrance.RingIndex);
        }

        [Fact]
        public void SelectEntrance_DeterministicWithSeed()
        {
            var selector = new EntranceExitSelector(new PathFinder());
            var config = new MazeConfiguration { Rings = 1 };
            var grid = new MazeGrid(config);
            grid.Cells.Add(new List<Cell>
            {
                new Cell { CellIndex = 0 },
                new Cell { CellIndex = 1 },
                new Cell { CellIndex = 2 },
                new Cell { CellIndex = 3 }
            });

            var entrance1 = selector.SelectEntrance(grid, new Random(42));
            var entrance2 = selector.SelectEntrance(grid, new Random(42));

            Assert.Equal(entrance1.CellIndex, entrance2.CellIndex);
        }

        #endregion

        #region SelectEntrance / SelectExit guard throws

        [Fact]
        public void SelectEntrance_EmptyGrid_ThrowsArgumentException()
        {
            var selector = new EntranceExitSelector(new PathFinder());
            var grid = new MazeGrid(new MazeConfiguration { Rings = 1 });
            Assert.Throws<ArgumentException>(() => selector.SelectEntrance(grid, new Random(0)));
        }

        [Fact]
        public void SelectEntrance_EmptyInnerRing_ThrowsArgumentException()
        {
            var selector = new EntranceExitSelector(new PathFinder());
            var grid = new MazeGrid(new MazeConfiguration { Rings = 1 });
            grid.Cells.Add(new List<Cell>());
            Assert.Throws<ArgumentException>(() => selector.SelectEntrance(grid, new Random(0)));
        }

        #endregion

        #region FindBestEntranceExitPair

        [Fact]
        public void FindBestEntranceExitPair_PicksBestEntrance()
        {
            // Build a grid where one ring-0 cell has a longer path to the outer ring
            var selector = new EntranceExitSelector(new PathFinder());
            var config = new MazeConfiguration { Rings = 2, MinCoverage = 0 };
            var grid = new MazeGrid(config);

            // Two separate inner cells, each with their own path to an outer cell
            // inner1 -> outer1 (direct, path=2)
            // inner2 -> mid -> outer2 (longer, path=3)
            // No connection between inner1 and inner2 — they're in separate subtrees
            var inner1 = new Cell { RingIndex = 0, CellIndex = 0 };
            var inner2 = new Cell { RingIndex = 0, CellIndex = 1 };
            var mid = new Cell { RingIndex = 0, CellIndex = 2 };
            var outer1 = new Cell { RingIndex = 1, CellIndex = 0 };
            var outer2 = new Cell { RingIndex = 1, CellIndex = 1 };

            inner1.CreatePassage(outer1); outer1.CreatePassage(inner1);
            inner2.CreatePassage(mid); mid.CreatePassage(inner2);
            mid.CreatePassage(outer2); outer2.CreatePassage(mid);

            grid.Cells.Add(new List<Cell> { inner1, inner2, mid });
            grid.Cells.Add(new List<Cell> { outer1, outer2 });

            var (entrance, exit, path) = selector.FindBestEntranceExitPair(grid);

            // inner2 → mid → outer2 = 3 cells, vs inner1 → outer1 = 2 cells
            Assert.Equal(inner2, entrance);
            Assert.Equal(outer2, exit);
            Assert.Equal(3, path.Count);
        }

        [Fact]
        public void FindBestEntranceExitPair_EmptyGrid_Throws()
        {
            var selector = new EntranceExitSelector(new PathFinder());
            var grid = new MazeGrid(new MazeConfiguration { Rings = 1 });
            Assert.Throws<ArgumentException>(() => selector.FindBestEntranceExitPair(grid));
        }

        [Fact]
        public void FindBestEntranceExitPair_SingleInnerCell_Works()
        {
            var selector = new EntranceExitSelector(new PathFinder());
            var grid = new MazeGrid(new MazeConfiguration { Rings = 2 });

            var inner = new Cell { RingIndex = 0, CellIndex = 0 };
            var outer = new Cell { RingIndex = 1, CellIndex = 0 };
            inner.CreatePassage(outer); outer.CreatePassage(inner);

            grid.Cells.Add(new List<Cell> { inner });
            grid.Cells.Add(new List<Cell> { outer });

            var (entrance, exit, path) = selector.FindBestEntranceExitPair(grid);
            Assert.Equal(inner, entrance);
            Assert.Equal(outer, exit);
            Assert.Equal(2, path.Count);
        }

        [Fact]
        public void FindBestEntranceExitPair_RealGrid_PicksOptimal()
        {
            var selector = new EntranceExitSelector(new PathFinder());
            var grid = TestGridFactory.CreateGeneratedGrid(5, seed: 42);

            var (entrance, exit, path) = selector.FindBestEntranceExitPair(grid);

            Assert.Equal(0, entrance.RingIndex);
            Assert.Equal(4, exit.RingIndex);
            Assert.True(path.Count > 1);
            Assert.Equal(entrance, path[0]);
            Assert.Equal(exit, path[path.Count - 1]);
        }

        #endregion

        #region FindOptimalAndCreateOpenings

        [Fact]
        public void FindOptimal_SucceedsOnFirstTry()
        {
            var selector = new EntranceExitSelector(new PathFinder());
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

            var (foundEntrance, foundExit) = selector.FindOptimalAndCreateOpenings(
                grid, mockGenerator.Object, config.MinCoverage);

            Assert.Equal(entrance, foundEntrance);
            Assert.Equal(exit, foundExit);
            Assert.True(foundExit.IsExit);
            mockGenerator.Verify(g => g.GenerateMaze(It.IsAny<MazeGrid>()), Times.Never);
        }

        [Fact]
        public void FindOptimal_RegeneratesWhenCoverageTooLow()
        {
            var selector = new EntranceExitSelector(new PathFinder());
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
                    entrance.ClearPassages();
                    exit1.ClearPassages();
                    exit2.ClearPassages();
                    entrance.CreatePassage(exit1); exit1.CreatePassage(entrance);
                    exit1.CreatePassage(exit2); exit2.CreatePassage(exit1);
                });

            var (foundEntrance, foundExit) = selector.FindOptimalAndCreateOpenings(
                grid, mockGenerator.Object, config.MinCoverage, 2);

            Assert.Equal(0, foundEntrance.RingIndex);
            Assert.Equal(1, foundExit.RingIndex);
            Assert.True(foundExit.IsExit);
            mockGenerator.Verify(g => g.GenerateMaze(It.IsAny<MazeGrid>()), Times.Once);
        }

        [Fact]
        public void FindOptimal_Throws_WhenMaxRetriesExhausted()
        {
            var selector = new EntranceExitSelector(new PathFinder());
            var mockGenerator = new Mock<IMazeGenerator>();
            var config = new MazeConfiguration { Rings = 2, MinCoverage = 99 };
            var grid = new MazeGrid(config);

            var entrance = new Cell { RingIndex = 0, CellIndex = 0 };
            var exit = new Cell { RingIndex = 1, CellIndex = 0 };
            var extra = new Cell { RingIndex = 1, CellIndex = 1 };

            grid.Cells.Add(new List<Cell> { entrance });
            grid.Cells.Add(new List<Cell> { exit, extra });

            entrance.CreatePassage(exit); exit.CreatePassage(entrance);

            Assert.Throws<InvalidOperationException>(() =>
                selector.FindOptimalAndCreateOpenings(
                    grid, mockGenerator.Object, 99, maxRetries: 2));
        }

        [Fact]
        public void FindOptimal_MarksExitIsExit()
        {
            var selector = new EntranceExitSelector(new PathFinder());
            var mockGenerator = new Mock<IMazeGenerator>();
            var config = new MazeConfiguration { Rings = 2, MinCoverage = 0 };
            var grid = new MazeGrid(config);

            var entrance = new Cell { RingIndex = 0, CellIndex = 0 };
            var exit = new Cell { RingIndex = 1, CellIndex = 0 };
            grid.Cells.Add(new List<Cell> { entrance });
            grid.Cells.Add(new List<Cell> { exit });
            entrance.CreatePassage(exit); exit.CreatePassage(entrance);

            var (_, foundExit) = selector.FindOptimalAndCreateOpenings(
                grid, mockGenerator.Object, 0);

            Assert.True(foundExit.IsExit);
        }

        [Fact]
        public void FindOptimal_CallsGenerateMaze_CorrectNumberOfTimes()
        {
            var selector = new EntranceExitSelector(new PathFinder());
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
                selector.FindOptimalAndCreateOpenings(
                    grid, mockGenerator.Object, 99, maxRetries: 3);
            }
            catch (InvalidOperationException) { }

            mockGenerator.Verify(g => g.GenerateMaze(It.IsAny<MazeGrid>()), Times.Exactly(2));
        }

        [Fact]
        public void FindOptimalAndCreateOpenings_OuterRingUnreachable_ThrowsInvalidOperationException()
        {
            var selector = new EntranceExitSelector(new PathFinder());
            var mockGenerator = new Mock<IMazeGenerator>();
            var grid = new MazeGrid(new MazeConfiguration { Rings = 2 });

            var entrance = new Cell { RingIndex = 0, CellIndex = 0 };
            var inner2 = new Cell { RingIndex = 0, CellIndex = 1 };
            var outerCell = new Cell { RingIndex = 1, CellIndex = 0 };

            entrance.CreatePassage(inner2);
            inner2.CreatePassage(entrance);

            grid.Cells.Add(new List<Cell> { entrance, inner2 });
            grid.Cells.Add(new List<Cell> { outerCell });

            Assert.Throws<InvalidOperationException>(() =>
                selector.FindOptimalAndCreateOpenings(
                    grid, mockGenerator.Object, minCoverage: 0, maxRetries: 1));
        }

        #endregion
    }
}
