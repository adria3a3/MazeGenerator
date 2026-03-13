using MazeGenerator.Models;
using MazeGenerator.Rendering;
using MazeGenerator.Services;
using Xunit;
using Xunit.Abstractions;

namespace MazeGenerator.Tests.Rendering
{
    /// <summary>
    /// Verifies solution path segments don't visually overlap with walls.
    /// </summary>
    public class SolutionPathCrossesWallTests
    {
        private readonly ITestOutputHelper _output;

        public SolutionPathCrossesWallTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(MazeAlgorithm.DfsBacktracker, 5, 42)]
        [InlineData(MazeAlgorithm.Prims, 5, 42)]
        [InlineData(MazeAlgorithm.Kruskals, 5, 42)]
        [InlineData(MazeAlgorithm.Wilsons, 5, 42)]
        [InlineData(MazeAlgorithm.Prims, 10, 42)]
        [InlineData(MazeAlgorithm.DfsBacktracker, 10, 42)]
        [InlineData(MazeAlgorithm.Prims, 20, 42)]
        public void SolutionLines_DontOverlapWallLines(MazeAlgorithm algorithm, int rings, int seed)
        {
            var grid = CreateGrid(rings);
            var gen = MazeGeneratorFactory.Create(algorithm, seed);
            gen.GenerateMaze(grid);

            var pathFinder = new PathFinder();
            var selector = new EntranceExitSelector(pathFinder);
            var (entrance, exit, bestPath) = selector.FindBestEntranceExitPair(grid);
            exit.IsExit = true;
            var solution = new MazeSolution(entrance, exit, bestPath,
                pathFinder.CalculateCoverage(bestPath.Count, grid.TotalCells));

            var calc = new MazeWallCalculator();
            var commands = calc.Calculate(grid, solution);

            var issues = new List<string>();

            // Check solution lines don't run collinear with wall lines
            foreach (var sLine in commands.SolutionLines)
            {
                foreach (var wLine in commands.WallLines)
                {
                    if (LinesNearlyCollinearAndOverlap(sLine, wLine, grid.CenterX, grid.CenterY))
                    {
                        issues.Add(
                            $"Solution line ({sLine.X1:F1},{sLine.Y1:F1})->({sLine.X2:F1},{sLine.Y2:F1}) " +
                            $"overlaps wall line ({wLine.X1:F1},{wLine.Y1:F1})->({wLine.X2:F1},{wLine.Y2:F1})");
                    }
                }
            }

            foreach (var issue in issues)
                _output.WriteLine(issue);

            Assert.Empty(issues);
        }

        /// <summary>
        /// Two lines are problematic if they're at nearly the same angle from center
        /// and their radial ranges overlap (making them visually collinear).
        /// </summary>
        private static bool LinesNearlyCollinearAndOverlap(LineSegment a, LineSegment b, double cx, double cy)
        {
            var aAngle = LineAngle(a, cx, cy);
            var bAngle = LineAngle(b, cx, cy);

            // Angle difference (handle wrap-around)
            var angleDiff = Math.Abs(aAngle - bAngle);
            if (angleDiff > Math.PI) angleDiff = 2 * Math.PI - angleDiff;

            // Must be nearly the same angle. At the overlap radius, the angular
            // separation translates to a linear distance. We need at least
            // (wallStroke/2 + solutionStroke/2) = ~1.6pt clearance.
            // Use 3pt / min_radius as the threshold for visual overlap.
            // At the overlap radius, angular separation = linear distance.
            // Need at least ~1.6pt (wallStroke/2 + solutionStroke/2) clearance.
            var (aRMin, aRMax) = RadialRange(a, cx, cy);
            var (bRMin, bRMax) = RadialRange(b, cx, cy);
            var minRadius = Math.Max(10, Math.Min(Math.Min(aRMin, aRMax), Math.Min(bRMin, bRMax)));
            var visualThreshold = 5.0 / minRadius;
            if (angleDiff > visualThreshold) return false;

            // Check radial overlap
            var aMin = aRMin; var aMax = aRMax;
            var bMin = bRMin; var bMax = bRMax;

            var overlap = Math.Min(aMax, bMax) - Math.Max(aMin, bMin);
            // Must have significant radial overlap AND be close enough linearly
            // that stroke widths would visually touch (wall=2pt + solution=1.2pt = 3.2pt total)
            if (overlap < 2.0) return false;

            // Compute actual linear distance at the overlap midpoint.
            // Visual overlap occurs when center-to-center distance < wallStroke/2 + solutionStroke/2.
            // Wall stroke = 2pt (half = 1pt), solution stroke = 1.2pt (half = 0.6pt).
            // Actual visual overlap threshold = 1.6pt.
            var overlapMidR = (Math.Max(aMin, bMin) + Math.Min(aMax, bMax)) / 2.0;
            var linearDist = angleDiff * overlapMidR;
            return linearDist < 1.6;
        }

        private static double LineAngle(LineSegment line, double cx, double cy)
        {
            var mx = (line.X1 + line.X2) / 2.0 - cx;
            var my = (line.Y1 + line.Y2) / 2.0 - cy;
            var a = Math.Atan2(my, mx);
            return a < 0 ? a + 2 * Math.PI : a;
        }

        private static (double min, double max) RadialRange(LineSegment line, double cx, double cy)
        {
            var r1 = Math.Sqrt((line.X1 - cx) * (line.X1 - cx) + (line.Y1 - cy) * (line.Y1 - cy));
            var r2 = Math.Sqrt((line.X2 - cx) * (line.X2 - cx) + (line.Y2 - cy) * (line.Y2 - cy));
            return (Math.Min(r1, r2), Math.Max(r1, r2));
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
