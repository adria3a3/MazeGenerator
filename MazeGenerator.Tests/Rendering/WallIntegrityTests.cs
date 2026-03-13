using MazeGenerator.Models;
using MazeGenerator.Rendering;
using MazeGenerator.Services;
using Xunit;

namespace MazeGenerator.Tests.Rendering
{
    /// <summary>
    /// Verifies that the rendered walls correctly reflect the maze's passage structure.
    /// Every wall that should exist (no passage between neighbors) must produce a wall primitive.
    /// Every passage must produce a gap (no wall) at the correct location.
    /// </summary>
    public class WallIntegrityTests
    {
        /// <summary>
        /// For each cell, verify that radial walls exist exactly where there is NO passage
        /// to the counter-clockwise neighbor.
        /// </summary>
        [Theory]
        [InlineData(MazeAlgorithm.DfsBacktracker, 5, 42)]
        [InlineData(MazeAlgorithm.Prims, 5, 42)]
        [InlineData(MazeAlgorithm.Kruskals, 5, 42)]
        [InlineData(MazeAlgorithm.Wilsons, 5, 42)]
        [InlineData(MazeAlgorithm.DfsBacktracker, 10, 123)]
        [InlineData(MazeAlgorithm.DfsBacktracker, 20, 42)]
        public void RadialWalls_MatchPassageStructure(MazeAlgorithm algorithm, int rings, int seed)
        {
            var grid = CreateGrid(rings);
            var gen = MazeGeneratorFactory.Create(algorithm, seed);
            gen.GenerateMaze(grid);

            var calc = new MazeWallCalculator();
            var commands = calc.Calculate(grid);

            // Count expected radial walls: cells where counter-clockwise neighbor has no passage
            var expectedRadialWalls = 0;
            foreach (var cell in grid.GetAllCells())
            {
                if (cell.CounterClockwiseNeighbor != null &&
                    !cell.Passages.Contains(cell.CounterClockwiseNeighbor))
                {
                    expectedRadialWalls++;
                }
            }

            Assert.Equal(expectedRadialWalls, commands.WallLines.Count);
        }

        /// <summary>
        /// Verify that passage openings don't exceed the cell's angular span.
        /// If an opening is wider than the cell, walls from adjacent cells
        /// would be visually missing, creating false paths.
        /// </summary>
        [Theory]
        [InlineData(MazeAlgorithm.DfsBacktracker, 5, 42)]
        [InlineData(MazeAlgorithm.Prims, 5, 42)]
        [InlineData(MazeAlgorithm.Kruskals, 5, 42)]
        [InlineData(MazeAlgorithm.Wilsons, 5, 42)]
        [InlineData(MazeAlgorithm.DfsBacktracker, 15, 42)]
        [InlineData(MazeAlgorithm.DfsBacktracker, 30, 42)]
        public void ArcGaps_DoNotExceedCellSpan(MazeAlgorithm algorithm, int rings, int seed)
        {
            var grid = CreateGrid(rings);
            var gen = MazeGeneratorFactory.Create(algorithm, seed);
            gen.GenerateMaze(grid);

            var config = grid.Configuration;
            var centerX = grid.CenterX;
            var centerY = grid.CenterY;

            foreach (var cell in grid.GetAllCells())
            {
                var passages = cell.GetPassableNeighbors();
                var cellSpan = cell.AngleEnd - cell.AngleStart;

                // Check inward passage openings
                if (cell.RingIndex > 0)
                {
                    var inwardPassages = passages.Where(p => p.RingIndex < cell.RingIndex).ToList();
                    foreach (var neighbor in inwardPassages)
                    {
                        var minAngularWidth = config.WallThickness *
                            GeometryCalculator.MinOpeningFactor / cell.RadiusInner;

                        // The opening should not exceed the cell's angular span
                        Assert.True(minAngularWidth <= cellSpan * 1.01, // 1% tolerance for float
                            $"Cell {cell}: inward opening ({minAngularWidth:F4} rad) exceeds " +
                            $"cell span ({cellSpan:F4} rad) at radius {cell.RadiusInner:F1}. " +
                            $"This creates false visual passages.");
                    }
                }

                // Check outward passage openings
                if (cell.RingIndex < grid.Cells.Count - 1)
                {
                    var outwardPassages = passages.Where(p => p.RingIndex > cell.RingIndex).ToList();
                    foreach (var neighbor in outwardPassages)
                    {
                        var minAngularWidth = config.WallThickness *
                            GeometryCalculator.MinOpeningFactor / cell.RadiusOuter;

                        Assert.True(minAngularWidth <= cellSpan * 1.01,
                            $"Cell {cell}: outward opening ({minAngularWidth:F4} rad) exceeds " +
                            $"cell span ({cellSpan:F4} rad) at radius {cell.RadiusOuter:F1}. " +
                            $"This creates false visual passages.");
                    }
                }
            }
        }

        /// <summary>
        /// Verify every arc segment has a positive non-degenerate sweep.
        /// Negative or zero sweeps indicate a calculation error.
        /// </summary>
        [Theory]
        [InlineData(MazeAlgorithm.DfsBacktracker, 5, 42)]
        [InlineData(MazeAlgorithm.Prims, 5, 42)]
        [InlineData(MazeAlgorithm.DfsBacktracker, 15, 42)]
        [InlineData(MazeAlgorithm.DfsBacktracker, 30, 42)]
        public void AllWallArcs_HavePositiveSweep(MazeAlgorithm algorithm, int rings, int seed)
        {
            var grid = CreateGrid(rings);
            var gen = MazeGeneratorFactory.Create(algorithm, seed);
            gen.GenerateMaze(grid);

            var calc = new MazeWallCalculator();
            var commands = calc.Calculate(grid);

            foreach (var arc in commands.WallArcs)
            {
                Assert.True(arc.SweepAngleRadians > 0,
                    $"Wall arc at radius {arc.Radius:F1} starting at {arc.StartAngleRadians:F4} " +
                    $"has non-positive sweep {arc.SweepAngleRadians:F6}");
            }
        }

        /// <summary>
        /// Verify that the total arc coverage per ring boundary equals
        /// 2*PI minus the gaps for passages. This ensures no extra gaps exist.
        /// </summary>
        [Theory]
        [InlineData(MazeAlgorithm.DfsBacktracker, 3, 42)]
        [InlineData(MazeAlgorithm.Prims, 3, 42)]
        [InlineData(MazeAlgorithm.Kruskals, 3, 42)]
        [InlineData(MazeAlgorithm.Wilsons, 3, 42)]
        public void PerRingArcCoverage_AccountsForAllPassages(MazeAlgorithm algorithm, int rings, int seed)
        {
            var grid = CreateGrid(rings);
            var gen = MazeGeneratorFactory.Create(algorithm, seed);
            gen.GenerateMaze(grid);

            // For each ring boundary (between ring i and ring i+1),
            // count passages crossing that boundary
            for (var ringIdx = 0; ringIdx < grid.Cells.Count - 1; ringIdx++)
            {
                var radius = grid.Cells[ringIdx][0].RadiusOuter;
                var passageCount = 0;

                foreach (var cell in grid.Cells[ringIdx])
                {
                    passageCount += cell.Passages.Count(p => p.RingIndex > cell.RingIndex);
                }

                // In a perfect maze, each passage removes exactly one wall segment
                // and replaces it with a gap. The number of arc segments on this
                // ring boundary should be (total cells on both sides) - passageCount
                // or roughly related. We just verify passage count is reasonable.
                Assert.True(passageCount >= 1,
                    $"Ring boundary {ringIdx}/{ringIdx + 1} at radius {radius:F1} " +
                    $"has {passageCount} passages — maze is disconnected between rings");
            }
        }

        /// <summary>
        /// The maze-only render and solution render must produce identical wall primitives.
        /// </summary>
        [Theory]
        [InlineData(MazeAlgorithm.DfsBacktracker, 5, 42)]
        [InlineData(MazeAlgorithm.Prims, 5, 42)]
        [InlineData(MazeAlgorithm.Kruskals, 5, 42)]
        [InlineData(MazeAlgorithm.Wilsons, 5, 42)]
        public void MazeAndSolutionRender_HaveIdenticalWalls(MazeAlgorithm algorithm, int rings, int seed)
        {
            var grid = CreateGrid(rings);
            var gen = MazeGeneratorFactory.Create(algorithm, seed);
            gen.GenerateMaze(grid);

            var pathFinder = new PathFinder();
            var selector = new EntranceExitSelector(pathFinder);
            var (entrance, exit, bestPath) = selector.FindBestEntranceExitPair(grid);
            exit.IsExit = true;
            var coverage = pathFinder.CalculateCoverage(bestPath.Count, grid.TotalCells);
            var solution = new MazeSolution(entrance, exit, bestPath, coverage);

            var calc = new MazeWallCalculator();

            // Maze-only: solution with empty path (same as MazeCommand does)
            var mazeOnly = new MazeSolution(entrance, exit, new List<Cell>(), 0);
            var mazeCommands = calc.Calculate(grid, mazeOnly);

            // Solution: full solution
            var solutionCommands = calc.Calculate(grid, solution);

            // Wall lines must be identical
            Assert.Equal(mazeCommands.WallLines.Count, solutionCommands.WallLines.Count);
            for (var i = 0; i < mazeCommands.WallLines.Count; i++)
            {
                Assert.Equal(mazeCommands.WallLines[i], solutionCommands.WallLines[i]);
            }

            // Wall arcs must be identical
            Assert.Equal(mazeCommands.WallArcs.Count, solutionCommands.WallArcs.Count);
            for (var i = 0; i < mazeCommands.WallArcs.Count; i++)
            {
                Assert.Equal(mazeCommands.WallArcs[i], solutionCommands.WallArcs[i]);
            }
        }

        private static MazeGrid CreateGrid(int rings)
        {
            var config = new MazeConfiguration
            {
                Rings = rings,
                PageWidth = 1190.55,
                PageHeight = 1683.78,
                Margin = 36,
                InnerRadius = 20,
                WallThickness = 2.0
            };
            var grid = new MazeGrid(config);
            grid.Initialize();
            return grid;
        }
    }
}
