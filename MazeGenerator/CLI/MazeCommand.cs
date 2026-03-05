using CommandLine;
using MazeGenerator.Models;

namespace MazeGenerator.CLI;

public class Options
{
    [Option('r', "rings", Default = 20, HelpText = "Number of concentric rings in the maze (1-100).")]
    public int Rings { get; init; }

    [Option('c', "min-coverage", Default = 50, HelpText = "Minimum solution path coverage as a percentage (0-100).")]
    public int MinCoverage { get; init; }

    [Option('s', "seed", Required = false, HelpText = "Random seed for reproducible maze generation (optional).")]
    public int? Seed { get; init; }

    [Option('o', "output", Default = "circular_maze", HelpText = "Base name for output files (without extension).")]
    public string OutputBaseName { get; init; } = "circular_maze";

    [Option("wall-thickness", Default = 2.0, HelpText = "Wall line thickness in points (0.5-10.0).")]
    public double WallThickness { get; init; }

    [Option("no-solution", Default = false, HelpText = "Skip generation of solution file.")]
    public bool NoSolution { get; init; }
}

public static class MazeCommand
{
    public static int Execute(string[] args)
    {
        return Parser.Default.ParseArguments<Options>(args)
            .MapResult(
                RunWithOptions,
                _ => 1
            );
    }

    private static int RunWithOptions(Options options)
    {
        var config = new MazeConfiguration
        {
            Rings = options.Rings,
            MinCoverage = options.MinCoverage,
            Seed = options.Seed,
            OutputBaseName = options.OutputBaseName,
            WallThickness = options.WallThickness,
            NoSolution = options.NoSolution,
        };

        // Validate configuration
        var errors = config.Validate();
        if (errors.Any())
        {
            Console.Error.WriteLine("Configuration errors:");
            foreach (var error in errors)
                Console.Error.WriteLine($"  • {error}");
            return 1;
        }

        DisplayConfiguration(config);

        try
        {
            // Phase 3: Build grid
            Console.WriteLine("Building maze grid...");
            var grid = new MazeGrid(config);

            grid.Initialize();
            Console.WriteLine("✓ Grid initialized successfully");
            Console.WriteLine();
            DisplayGridStatistics(grid);

            // Phase 4: Generate maze
            Console.WriteLine("Generating maze...");
            var generator = new Services.MazeGenerator(config.Seed);
            generator.GenerateMaze(grid);
            Console.WriteLine("✓ Maze generation complete");
            DisplayMazeStatistics(grid);

            // Phase 5: Find optimal entrance/exit and create boundary openings.
            // This always runs so the maze PDF always has a proper exit gap and open center.
            Console.WriteLine("Finding entrance/exit...");
            var random = config.Seed.HasValue ? new Random(config.Seed.Value) : new Random();
            var pathFinder = new Services.PathFinder();

            var (entrance, exit) = pathFinder.FindOptimalAndCreateOpenings(
                grid, generator, random, config.MinCoverage, log: Console.WriteLine);

            Console.WriteLine($"  Entrance: Ring {entrance.RingIndex}, Cell {entrance.CellIndex}");
            Console.WriteLine($"  Exit:     Ring {exit.RingIndex}, Cell {exit.CellIndex}");

            grid.Entrance = entrance;
            grid.Exit = exit;

            // Phase 6: Find solution path (skipped when --no-solution is set)
            if (!config.NoSolution)
            {
                var solutionPath = pathFinder.FindPath(entrance, exit);
                grid.SolutionPath = solutionPath;
                Console.WriteLine("✓ Solution path found");
                DisplaySolutionStatistics(grid, solutionPath, config);
            }
            else
            {
                Console.WriteLine("Skipping solution path (--no-solution flag set)");
            }

            // Phase 7: Render PDFs
            Console.WriteLine("Rendering PDF output...");

            var renderer = new Rendering.MazeRenderer();

            var mazePdfPath = $"{config.OutputBaseName}.pdf";
            renderer.RenderMazeToPdf(grid, mazePdfPath);
            Console.WriteLine($"  ✓ Saved maze to: {mazePdfPath}");

            if (grid.SolutionPath.Count > 0)
            {
                var solutionPdfPath = $"{config.OutputBaseName}_solution.pdf";
                renderer.RenderMazeWithSolutionToPdf(grid, solutionPdfPath);
                Console.WriteLine($"  ✓ Saved solution to: {solutionPdfPath}");
            }

            Console.WriteLine();
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                  Generation Complete!                    ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static void DisplayConfiguration(MazeConfiguration config)
    {
        Console.WriteLine();
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║        Circular Maze Generator - Configuration           ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine("Maze Parameters:");
        Console.WriteLine($"  Rings:              {config.Rings}");
        Console.WriteLine($"  Min Coverage:       {config.MinCoverage}%");
        Console.WriteLine($"  Random Seed:        {(config.Seed.HasValue ? config.Seed.Value.ToString() : "Random")}");
        Console.WriteLine();
        Console.WriteLine("Output Settings:");
        Console.WriteLine($"  Base Name:          {config.OutputBaseName}");
        Console.WriteLine($"  Wall Thickness:     {config.WallThickness} pt");
        Console.WriteLine($"  Include Solution:   {(config.NoSolution ? "No" : "Yes")}");
        Console.WriteLine();
        Console.WriteLine("Page Settings:");
        Console.WriteLine($"  Format:             A2 Portrait");
        Console.WriteLine($"  Dimensions:         {config.PageWidth} × {config.PageHeight} pt");
        Console.WriteLine($"  Margin:             {config.Margin} pt");
        Console.WriteLine($"  Inner Radius:       {config.InnerRadius} pt");
        Console.WriteLine();
    }

    private static void DisplayGridStatistics(MazeGrid grid)
    {
        Console.WriteLine("Grid Statistics:");
        Console.WriteLine($"  Total Cells:        {grid.TotalCells}");
        Console.WriteLine($"  Ring Width:         {grid.RingWidth:F2} pt ({grid.RingWidth / 2.8346:F2} mm)");
        Console.WriteLine($"  Usable Radius:      {grid.UsableRadius:F2} pt ({grid.UsableRadius / 2.8346:F2} mm)");
        Console.WriteLine();
        Console.WriteLine("Cells per Ring:");

        for (var i = 0; i < grid.CellCounts.Count; i++)
        {
            var cellCount = grid.CellCounts[i];
            var (innerR, outerR) = Services.GeometryCalculator.GetRingRadii(
                i, grid.Configuration.InnerRadius, grid.RingWidth);
            var midR = (innerR + outerR) / 2.0;
            var circumference = 2 * Math.PI * midR;
            var cellArcLength = circumference / cellCount;

            Console.WriteLine($"  Ring {i + 1,2}: {cellCount,3} cells  " +
                              $"(r={midR / 2.8346:F1}mm, arc={cellArcLength / 2.8346:F1}mm)");
        }

        Console.WriteLine();
    }

    private static void DisplayMazeStatistics(MazeGrid grid)
    {
        var visitedCells = 0;
        var totalConnections = 0;

        foreach (var ring in grid.Cells)
        {
            foreach (var cell in ring)
            {
                if (cell.Visited) visitedCells++;
                totalConnections += cell.Passages.Count;
            }
        }

        var passages = totalConnections / 2;
        var allVisited = visitedCells == grid.TotalCells;
        var isPerfectMaze = allVisited && passages == grid.TotalCells - 1;

        Console.WriteLine();
        Console.WriteLine("Maze Generation Statistics:");
        Console.WriteLine($"  Visited Cells:      {visitedCells}/{grid.TotalCells}");
        Console.WriteLine($"  Passages Carved:    {passages}");
        Console.WriteLine($"  Expected (N-1):     {grid.TotalCells - 1}");
        Console.WriteLine($"  All Cells Reached:  {(allVisited ? "✓ Yes" : "✗ No")}");
        Console.WriteLine($"  Perfect Maze:       {(isPerfectMaze ? "✓ Yes (spanning tree)" : "✗ No")}");
        Console.WriteLine();
    }

    private static void DisplaySolutionStatistics(MazeGrid grid, List<Cell> solutionPath, MazeConfiguration config)
    {
        var pathFinder = new Services.PathFinder();
        var coverage = pathFinder.CalculateCoverage(solutionPath.Count, grid.TotalCells);

        Console.WriteLine();
        Console.WriteLine("Solution Path Statistics:");
        Console.WriteLine($"  Path Length:        {solutionPath.Count} cells");
        Console.WriteLine($"  Total Cells:        {grid.TotalCells}");
        Console.WriteLine($"  Coverage:           {coverage:F1}%");
        Console.WriteLine($"  Required:           {config.MinCoverage}%");
        Console.WriteLine($"  Meets Requirement:  {(coverage >= config.MinCoverage ? "✓ Yes" : "✗ No")}");

        if (config.MinCoverage > 0 && coverage >= config.MinCoverage)
        {
            Console.WriteLine();
            Console.WriteLine("✓ Coverage requirement satisfied with optimal entrance/exit selection.");
        }

        Console.WriteLine();
    }
}
