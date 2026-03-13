using MazeGenerator.Models;
using MazeGenerator.Rendering;
using MazeGenerator.Services;
using Xunit;
using Xunit.Abstractions;

namespace MazeGenerator.Tests.Rendering;

/// <summary>
/// Broad sweep of maze configurations to detect rendering issues:
/// - Solution lines overlapping wall lines (collinearity)
/// - Ring boundary gaps that are too small to be visible
/// </summary>
public class RenderingDiagnosticTests
{
    private readonly ITestOutputHelper _output;

    public RenderingDiagnosticTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public static IEnumerable<object[]> MazeConfigurations()
    {
        var algorithms = new[] { MazeAlgorithm.DfsBacktracker, MazeAlgorithm.Prims, MazeAlgorithm.Kruskals, MazeAlgorithm.Wilsons };
        var rings = new[] { 3, 5, 8, 10, 15, 20, 30 };
        var seeds = new[] { 1, 7, 42, 99, 123, 256, 1000 };

        foreach (var algo in algorithms)
            foreach (var r in rings)
                foreach (var seed in seeds)
                    yield return new object[] { algo, r, seed };
    }

    [Theory]
    [MemberData(nameof(MazeConfigurations))]
    public void NoSolutionWallOverlap(MazeAlgorithm algorithm, int rings, int seed)
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
        foreach (var sLine in commands.SolutionLines)
        {
            foreach (var wLine in commands.WallLines)
            {
                if (LinesOverlap(sLine, wLine, grid.CenterX, grid.CenterY))
                {
                    issues.Add(
                        $"Solution ({sLine.X1:F0},{sLine.Y1:F0})->({sLine.X2:F0},{sLine.Y2:F0}) " +
                        $"overlaps wall ({wLine.X1:F0},{wLine.Y1:F0})->({wLine.X2:F0},{wLine.Y2:F0})");
                }
            }
        }

        foreach (var issue in issues)
            _output.WriteLine(issue);

        Assert.Empty(issues);
    }

    [Theory]
    [MemberData(nameof(MazeConfigurations))]
    public void NoTooSmallGaps(MazeAlgorithm algorithm, int rings, int seed)
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

        // Check ring boundary gaps: for each boundary, compute gap sizes
        // and flag any that are too small to be visible (< 2pt linear width)
        for (var ringIdx = 0; ringIdx < grid.Cells.Count - 1; ringIdx++)
        {
            var innerRing = grid.Cells[ringIdx];
            var boundaryRadius = innerRing[0].RadiusOuter;

            foreach (var innerCell in innerRing)
            {
                foreach (var outerCell in innerCell.Passages.Where(p => p.RingIndex == ringIdx + 1))
                {
                    var overlapStart = Math.Max(innerCell.AngleStart, outerCell.AngleStart);
                    var overlapEnd = Math.Min(innerCell.AngleEnd, outerCell.AngleEnd);
                    if (overlapEnd <= overlapStart) continue;

                    var gapAngular = overlapEnd - overlapStart;
                    var gapLinear = gapAngular * boundaryRadius;

                    if (gapLinear < 2.0)
                    {
                        issues.Add(
                            $"Ring {ringIdx}/{ringIdx + 1} at r={boundaryRadius:F0}: " +
                            $"gap between {innerCell} and {outerCell} is only {gapLinear:F2}pt " +
                            $"({gapAngular * 180 / Math.PI:F3}°)");
                    }
                }
            }
        }

        // Check inner boundary gap
        if (solution.Entrance != null)
        {
            var innerRadius = grid.Configuration.InnerRadius;
            var entranceSpan = solution.Entrance.AngleEnd - solution.Entrance.AngleStart;
            var entranceGapLinear = entranceSpan * innerRadius;
            if (entranceGapLinear < 2.0)
            {
                issues.Add($"Inner boundary entrance gap is only {entranceGapLinear:F2}pt");
            }
        }

        // Check outer boundary gap
        if (solution.Exit != null)
        {
            var outermostRadius = grid.Cells[grid.Cells.Count - 1][0].RadiusOuter;
            var exitSpan = solution.Exit.AngleEnd - solution.Exit.AngleStart;
            var exitGapLinear = exitSpan * outermostRadius;
            if (exitGapLinear < 2.0)
            {
                issues.Add($"Outer boundary exit gap is only {exitGapLinear:F2}pt");
            }
        }

        foreach (var issue in issues)
            _output.WriteLine(issue);

        Assert.Empty(issues);
    }

    [Theory]
    [MemberData(nameof(MazeConfigurations))]
    public void AllArcSweepsPositive(MazeAlgorithm algorithm, int rings, int seed)
    {
        var grid = CreateGrid(rings);
        var gen = MazeGeneratorFactory.Create(algorithm, seed);
        gen.GenerateMaze(grid);

        var calc = new MazeWallCalculator();
        var commands = calc.Calculate(grid);

        var issues = new List<string>();
        foreach (var arc in commands.WallArcs)
        {
            if (arc.SweepAngleRadians <= 0)
            {
                issues.Add(
                    $"Arc at r={arc.Radius:F0} start={arc.StartAngleRadians:F4} " +
                    $"has non-positive sweep {arc.SweepAngleRadians:F6}");
            }
        }

        foreach (var issue in issues)
            _output.WriteLine(issue);

        Assert.Empty(issues);
    }

    [Theory]
    [MemberData(nameof(MazeConfigurations))]
    public void WallsIdenticalBetweenMazeAndSolution(MazeAlgorithm algorithm, int rings, int seed)
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

        var mazeOnly = new MazeSolution(entrance, exit, new List<Cell>(), 0);
        var mazeCommands = calc.Calculate(grid, mazeOnly);
        var solutionCommands = calc.Calculate(grid, solution);

        Assert.Equal(mazeCommands.WallLines.Count, solutionCommands.WallLines.Count);
        Assert.Equal(mazeCommands.WallArcs.Count, solutionCommands.WallArcs.Count);

        for (var i = 0; i < mazeCommands.WallLines.Count; i++)
            Assert.Equal(mazeCommands.WallLines[i], solutionCommands.WallLines[i]);

        for (var i = 0; i < mazeCommands.WallArcs.Count; i++)
            Assert.Equal(mazeCommands.WallArcs[i], solutionCommands.WallArcs[i]);
    }

    private static bool LinesOverlap(LineSegment a, LineSegment b, double cx, double cy)
    {
        var aAngle = LineAngle(a, cx, cy);
        var bAngle = LineAngle(b, cx, cy);

        var angleDiff = Math.Abs(aAngle - bAngle);
        if (angleDiff > Math.PI) angleDiff = 2 * Math.PI - angleDiff;

        var (aRMin, aRMax) = RadialRange(a, cx, cy);
        var (bRMin, bRMax) = RadialRange(b, cx, cy);
        var minRadius = Math.Max(10, Math.Min(Math.Min(aRMin, aRMax), Math.Min(bRMin, bRMax)));
        var visualThreshold = 5.0 / minRadius;
        if (angleDiff > visualThreshold) return false;

        var overlap = Math.Min(aRMax, bRMax) - Math.Max(aRMin, bRMin);
        if (overlap < 2.0) return false;

        var overlapMidR = (Math.Max(aRMin, bRMin) + Math.Min(aRMax, bRMax)) / 2.0;
        var linearDist = angleDiff * overlapMidR;
        return linearDist < 1.6; // wallStroke/2 + solutionStroke/2 = 1pt + 0.6pt
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
