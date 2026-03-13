using MazeGenerator.Models;
using Xunit;

namespace MazeGenerator.Tests.Models
{
    public class MazeSolutionTests
    {
        [Fact]
        public void Constructor_SetsAllProperties()
        {
            var entrance = new Cell { RingIndex = 0, CellIndex = 0 };
            var exit = new Cell { RingIndex = 2, CellIndex = 5 };
            var path = new List<Cell> { entrance, new Cell(), exit };

            var solution = new MazeSolution(entrance, exit, path, 75.5);

            Assert.Equal(entrance, solution.Entrance);
            Assert.Equal(exit, solution.Exit);
            Assert.Equal(3, solution.Path.Count);
            Assert.Equal(75.5, solution.Coverage);
        }

        [Fact]
        public void RecordEquality_SameReferences_AreEqual()
        {
            var entrance = new Cell();
            var exit = new Cell();
            var path = new List<Cell> { entrance, exit };

            // Record equality: same cell refs + same list ref + same coverage
            var s1 = new MazeSolution(entrance, exit, path, 50.0);
            var s2 = new MazeSolution(entrance, exit, path, 50.0);

            Assert.Equal(s1, s2);
        }

        [Fact]
        public void RecordEquality_DifferentListInstances_AreNotEqual()
        {
            // List<T> uses reference equality, so two separate lists are not equal
            // even with the same contents — this is by design for records with mutable collections
            var entrance = new Cell();
            var exit = new Cell();

            var s1 = new MazeSolution(entrance, exit, new List<Cell> { entrance, exit }, 50.0);
            var s2 = new MazeSolution(entrance, exit, new List<Cell> { entrance, exit }, 50.0);

            Assert.NotEqual(s1, s2);
        }

        [Fact]
        public void RecordEquality_DifferentCoverage_AreNotEqual()
        {
            var entrance = new Cell();
            var exit = new Cell();
            var path = new List<Cell> { entrance, exit };

            var s1 = new MazeSolution(entrance, exit, path, 50.0);
            var s2 = new MazeSolution(entrance, exit, path, 60.0);

            Assert.NotEqual(s1, s2);
        }

        [Fact]
        public void MazeGrid_Solution_DefaultIsNull()
        {
            var grid = new MazeGrid(new MazeConfiguration { Rings = 1 });
            Assert.Null(grid.Solution);
        }

        [Fact]
        public void MazeGrid_Solution_CanBeSet()
        {
            var grid = new MazeGrid(new MazeConfiguration { Rings = 1 });
            var entrance = new Cell();
            var exit = new Cell();
            var solution = new MazeSolution(entrance, exit, new List<Cell>(), 0);

            grid.Solution = solution;

            Assert.NotNull(grid.Solution);
            Assert.Equal(entrance, grid.Solution.Entrance);
        }
    }
}
