using MazeGenerator.Models;
using MazeGenerator.Rendering;
using MazeGenerator.Services;
using Xunit;
using Xunit.Abstractions;

namespace MazeGenerator.Tests.Rendering
{
    /// <summary>
    /// Verifies that every wall in the rendered output corresponds to a non-passage
    /// and every passage creates exactly one gap. Detects the "multiple solutions" visual bug.
    /// </summary>
    public class WallPassageCorrectnessTests
    {
        private readonly ITestOutputHelper _output;

        public WallPassageCorrectnessTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// Both sides of a ring boundary draw arcs at the same radius.
        /// If one side has a passage and the other doesn't, the non-passage side
        /// draws a full arc that covers the passage side's gap — blocking the passage visually.
        /// This test detects this condition.
        /// </summary>
        [Theory]
        [InlineData(MazeAlgorithm.DfsBacktracker, 5, 42)]
        [InlineData(MazeAlgorithm.Prims, 5, 42)]
        [InlineData(MazeAlgorithm.Kruskals, 5, 42)]
        [InlineData(MazeAlgorithm.Wilsons, 5, 42)]
        [InlineData(MazeAlgorithm.DfsBacktracker, 3, 42)]
        [InlineData(MazeAlgorithm.DfsBacktracker, 10, 42)]
        public void NoPassageBlockedByDuplicateWall(MazeAlgorithm algorithm, int rings, int seed)
        {
            var grid = CreateGrid(rings);
            var gen = MazeGeneratorFactory.Create(algorithm, seed);
            gen.GenerateMaze(grid);

            // For each ring boundary, check that passages aren't visually blocked.
            // A passage between inner cell A and outer cell B creates gaps in:
            //   - A's outward arc at B's angular overlap
            //   - B's inward arc at A's angular overlap
            // But other cells at the same radius also draw arcs.
            // Verify no other cell's arc overlaps with the passage gap.
            var issues = new List<string>();

            for (var ringIdx = 0; ringIdx < grid.Cells.Count - 1; ringIdx++)
            {
                var innerRing = grid.Cells[ringIdx];
                var outerRing = grid.Cells[ringIdx + 1];
                var boundaryRadius = innerRing[0].RadiusOuter; // = outerRing[0].RadiusInner

                // Collect all passage gaps at this boundary
                var passageGaps = new List<(double gapStart, double gapEnd, Cell inner, Cell outer)>();

                foreach (var innerCell in innerRing)
                {
                    foreach (var outerCell in innerCell.Passages.Where(p => p.RingIndex == ringIdx + 1))
                    {
                        var overlap = GetAngularOverlap(innerCell, outerCell);
                        if (overlap.HasValue)
                        {
                            passageGaps.Add((overlap.Value.start, overlap.Value.end, innerCell, outerCell));
                        }
                    }
                }

                // Now check: for each passage gap, verify that no OTHER cell draws a
                // solid wall arc that covers this gap.
                // An outer cell with NO passage to the inner ring draws a full inward arc.
                // If that cell's angular range overlaps with the passage gap, it blocks it.
                foreach (var (gapStart, gapEnd, passageInner, passageOuter) in passageGaps)
                {
                    // Check outer ring cells that don't have this passage
                    foreach (var outerCell in outerRing)
                    {
                        if (outerCell == passageOuter) continue; // This cell has the passage

                        // Does this cell have ANY inward passage?
                        var hasInwardPassage = outerCell.Passages.Any(p => p.RingIndex == ringIdx);

                        if (!hasInwardPassage)
                        {
                            // This cell draws a FULL inward arc from AngleStart to AngleEnd
                            // Check if it overlaps with the passage gap
                            var cellOverlap = GetRangeOverlap(gapStart, gapEnd, outerCell.AngleStart, outerCell.AngleEnd);
                            if (cellOverlap > 1e-6)
                            {
                                issues.Add(
                                    $"Ring {ringIdx}/{ringIdx + 1}: passage {passageInner}↔{passageOuter} " +
                                    $"gap [{gapStart:F3},{gapEnd:F3}] blocked by {outerCell}'s full inward arc " +
                                    $"[{outerCell.AngleStart:F3},{outerCell.AngleEnd:F3}] overlap={cellOverlap:F4}rad");
                            }
                        }
                    }

                    // Check inner ring cells that don't have this passage
                    foreach (var innerCell in innerRing)
                    {
                        if (innerCell == passageInner) continue;

                        var hasOutwardPassage = innerCell.Passages.Any(p => p.RingIndex == ringIdx + 1);

                        if (!hasOutwardPassage)
                        {
                            var cellOverlap = GetRangeOverlap(gapStart, gapEnd, innerCell.AngleStart, innerCell.AngleEnd);
                            if (cellOverlap > 1e-6)
                            {
                                issues.Add(
                                    $"Ring {ringIdx}/{ringIdx + 1}: passage {passageInner}↔{passageOuter} " +
                                    $"gap [{gapStart:F3},{gapEnd:F3}] blocked by {innerCell}'s full outward arc " +
                                    $"[{innerCell.AngleStart:F3},{innerCell.AngleEnd:F3}] overlap={cellOverlap:F4}rad");
                            }
                        }
                    }
                }
            }

            foreach (var issue in issues)
                _output.WriteLine(issue);

            Assert.Empty(issues);
        }

        private static (double start, double end)? GetAngularOverlap(Cell inner, Cell outer)
        {
            var s1 = inner.AngleStart; var e1 = inner.AngleEnd;
            var s2 = outer.AngleStart; var e2 = outer.AngleEnd;

            var overlapStart = Math.Max(s1, s2);
            var overlapEnd = Math.Min(e1, e2);

            if (overlapEnd <= overlapStart) return null;
            return (overlapStart, overlapEnd);
        }

        private static double GetRangeOverlap(double s1, double e1, double s2, double e2)
        {
            return Math.Max(0, Math.Min(e1, e2) - Math.Max(s1, s2));
        }

        private static MazeGrid CreateGrid(int rings)
        {
            var config = new MazeConfiguration
            {
                Rings = rings,
                PageWidth = 595.28,
                PageHeight = 841.89,
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
