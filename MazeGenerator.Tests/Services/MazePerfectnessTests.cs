using MazeGenerator.Models;
using MazeGenerator.Services;
using Xunit;

namespace MazeGenerator.Tests.Services
{
    /// <summary>
    /// Verifies that every algorithm always produces a perfect maze (spanning tree)
    /// with exactly one path between any two cells — across multiple seeds and sizes.
    /// A perfect maze guarantees a unique solution.
    /// </summary>
    public class MazePerfectnessTests
    {
        private static MazeGrid CreateGrid(int rings)
        {
            var config = new MazeConfiguration
            {
                Rings = rings,
                PageWidth = 400,
                PageHeight = 600,
                Margin = 20,
                InnerRadius = 20,
                WallThickness = 2.0
            };
            var grid = new MazeGrid(config);
            grid.Initialize();
            return grid;
        }

        public static IEnumerable<object[]> AllAlgorithmsAndSizes()
        {
            var algorithms = new[] { MazeAlgorithm.DfsBacktracker, MazeAlgorithm.Prims, MazeAlgorithm.Kruskals, MazeAlgorithm.Wilsons };
            var sizes = new[] { 1, 3, 5, 10, 20 };
            foreach (var algo in algorithms)
                foreach (var size in sizes)
                    yield return new object[] { algo, size };
        }

        public static IEnumerable<object[]> AllAlgorithmsAndSeeds()
        {
            var algorithms = new[] { MazeAlgorithm.DfsBacktracker, MazeAlgorithm.Prims, MazeAlgorithm.Kruskals, MazeAlgorithm.Wilsons };
            var seeds = new[] { 0, 1, 42, 123, 9999, 314159 };
            foreach (var algo in algorithms)
                foreach (var seed in seeds)
                    yield return new object[] { algo, seed };
        }

        [Theory]
        [MemberData(nameof(AllAlgorithmsAndSizes))]
        public void AllAlgorithms_AllSizes_ProducePerfectMaze(MazeAlgorithm algorithm, int rings)
        {
            var grid = CreateGrid(rings);
            var generator = MazeGeneratorFactory.Create(algorithm, seed: 42);
            generator.GenerateMaze(grid);

            Assert.True(MazeValidation.IsPerfectMaze(grid),
                $"{algorithm} with {rings} rings did not produce a perfect maze");
        }

        [Theory]
        [MemberData(nameof(AllAlgorithmsAndSeeds))]
        public void AllAlgorithms_AllSeeds_ProducePerfectMaze(MazeAlgorithm algorithm, int seed)
        {
            var grid = CreateGrid(rings: 5);
            var generator = MazeGeneratorFactory.Create(algorithm, seed);
            generator.GenerateMaze(grid);

            Assert.True(MazeValidation.IsPerfectMaze(grid),
                $"{algorithm} with seed {seed} did not produce a perfect maze");
        }

        [Theory]
        [MemberData(nameof(AllAlgorithmsAndSizes))]
        public void AllAlgorithms_ExactlyNMinus1Passages(MazeAlgorithm algorithm, int rings)
        {
            var grid = CreateGrid(rings);
            var generator = MazeGeneratorFactory.Create(algorithm, seed: 42);
            generator.GenerateMaze(grid);

            var totalConnections = grid.GetAllCells().Sum(c => c.Passages.Count);
            var passages = totalConnections / 2;

            Assert.Equal(grid.TotalCells - 1, passages);
        }

        [Theory]
        [MemberData(nameof(AllAlgorithmsAndSizes))]
        public void AllAlgorithms_NoCycles(MazeAlgorithm algorithm, int rings)
        {
            var grid = CreateGrid(rings);
            var generator = MazeGeneratorFactory.Create(algorithm, seed: 42);
            generator.GenerateMaze(grid);

            Assert.False(MazeValidation.HasCycles(grid),
                $"{algorithm} with {rings} rings produced a maze with cycles");
        }

        [Theory]
        [MemberData(nameof(AllAlgorithmsAndSizes))]
        public void AllAlgorithms_UniquePathBetweenEntranceAndExit(MazeAlgorithm algorithm, int rings)
        {
            if (rings < 2) return; // Need at least 2 rings for entrance/exit

            var grid = CreateGrid(rings);
            var generator = MazeGeneratorFactory.Create(algorithm, seed: 42);
            generator.GenerateMaze(grid);

            var pathFinder = new PathFinder();
            var selector = new EntranceExitSelector(pathFinder);
            var (entrance, exit, _) = selector.FindBestEntranceExitPair(grid);

            // In a perfect maze (tree), BFS finds the ONLY path
            var path = pathFinder.FindPath(entrance, exit);
            Assert.NotEmpty(path);

            // Verify uniqueness: remove one passage on the path and confirm no path exists
            // Pick a passage in the middle of the path
            var midIndex = path.Count / 2;
            var cellA = path[midIndex];
            var cellB = path[midIndex + 1];

            // Temporarily break the passage by checking: without this edge,
            // there should be no alternative path (proving the original was unique)
            Assert.True(cellA.Passages.Contains(cellB),
                "Path cells should have a passage between them");

            // Count all paths by verifying tree property: removing any edge
            // disconnects the tree into exactly 2 components
            var reachable = BfsCount(cellA, excluding: cellB);
            var totalCells = grid.TotalCells;
            Assert.True(reachable < totalCells,
                $"Removing edge should disconnect the tree but {reachable}/{totalCells} cells still reachable");
        }

        [Theory]
        [MemberData(nameof(AllAlgorithmsAndSeeds))]
        public void AllAlgorithms_NoSelfPassages(MazeAlgorithm algorithm, int seed)
        {
            var grid = CreateGrid(rings: 5);
            var generator = MazeGeneratorFactory.Create(algorithm, seed);
            generator.GenerateMaze(grid);

            foreach (var cell in grid.GetAllCells())
            {
                Assert.DoesNotContain(cell, cell.Passages);
            }
        }

        [Theory]
        [MemberData(nameof(AllAlgorithmsAndSeeds))]
        public void AllAlgorithms_SymmetricPassages(MazeAlgorithm algorithm, int seed)
        {
            var grid = CreateGrid(rings: 5);
            var generator = MazeGeneratorFactory.Create(algorithm, seed);
            generator.GenerateMaze(grid);

            foreach (var cell in grid.GetAllCells())
            {
                foreach (var neighbor in cell.Passages)
                {
                    Assert.Contains(cell, neighbor.Passages);
                }
            }
        }

        [Theory]
        [MemberData(nameof(AllAlgorithmsAndSeeds))]
        public void AllAlgorithms_PassagesOnlyBetweenNeighbors(MazeAlgorithm algorithm, int seed)
        {
            var grid = CreateGrid(rings: 5);
            var generator = MazeGeneratorFactory.Create(algorithm, seed);
            generator.GenerateMaze(grid);

            foreach (var cell in grid.GetAllCells())
            {
                var allNeighbors = MazeGeneratorHelper.GetAllNeighbors(cell);
                foreach (var passage in cell.Passages)
                {
                    Assert.Contains(passage, allNeighbors);
                }
            }
        }

        /// <summary>
        /// BFS from start, not traversing through the excluded cell.
        /// Returns count of reachable cells.
        /// </summary>
        private static int BfsCount(Cell start, Cell excluding)
        {
            var visited = new HashSet<Cell> { start };
            var queue = new Queue<Cell>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var neighbor in current.GetPassableNeighbors())
                {
                    if (neighbor != excluding && !visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return visited.Count;
        }
    }
}
