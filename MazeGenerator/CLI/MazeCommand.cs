using CommandLine;
using MazeGenerator.DI;
using MazeGenerator.Models;
using MazeGenerator.Rendering;
using MazeGenerator.Services;
using Microsoft.Extensions.DependencyInjection;

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

    [Option('a', "algorithm", Default = MazeAlgorithm.DfsBacktracker, HelpText = "Maze generation algorithm: DfsBacktracker, Prims, Kruskals, Wilsons.")]
    public MazeAlgorithm Algorithm { get; init; }

    [Option('f', "format", Default = OutputFormat.Pdf, HelpText = "Output format: Pdf, Svg, Png.")]
    public OutputFormat Format { get; init; }

    [Option('p', "page-size", Default = PageSizeName.A2, HelpText = "Page size: A4, A3, A2, Letter, Legal, Tabloid.")]
    public PageSizeName PageSize { get; init; }
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
        var (pageWidth, pageHeight) = PageSize.GetDimensions(options.PageSize);
        var config = new MazeConfiguration
        {
            Rings = options.Rings,
            MinCoverage = options.MinCoverage,
            Seed = options.Seed,
            OutputBaseName = options.OutputBaseName,
            WallThickness = options.WallThickness,
            NoSolution = options.NoSolution,
            Algorithm = options.Algorithm,
            OutputFormat = options.Format,
            PageSizeName = options.PageSize,
            PageWidth = pageWidth,
            PageHeight = pageHeight,
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
            var sp = ServiceRegistration.BuildServiceProvider(config);
            var generator = sp.GetRequiredService<IMazeGenerator>();
            var pathFinder = sp.GetRequiredService<IPathFinder>();
            var entranceExitSelector = sp.GetRequiredService<IEntranceExitSelector>();
            var outputRenderer = sp.GetRequiredService<IMazeRenderer>();

            // Build grid
            Console.WriteLine("Building maze grid...");
            var grid = new MazeGrid(config);
            grid.Initialize();
            Console.WriteLine("✓ Grid initialized successfully");
            Console.WriteLine();
            DisplayGridStatistics(grid);

            // Generate maze
            Console.WriteLine($"Generating maze (algorithm: {config.Algorithm})...");
            generator.GenerateMaze(grid);
            Console.WriteLine("✓ Maze generation complete");
            DisplayMazeStatistics(grid);

            // Find optimal entrance/exit
            Console.WriteLine("Finding entrance/exit...");
            var (entrance, exit) = entranceExitSelector.FindOptimalAndCreateOpenings(
                grid, generator, config.MinCoverage, log: Console.WriteLine);

            Console.WriteLine($"  Entrance: Ring {entrance.RingIndex}, Cell {entrance.CellIndex}");
            Console.WriteLine($"  Exit:     Ring {exit.RingIndex}, Cell {exit.CellIndex}");

            // Find solution path
            MazeSolution? solution = null;
            if (!config.NoSolution)
            {
                var solutionPath = pathFinder.FindPath(entrance, exit);
                var coverage = pathFinder.CalculateCoverage(solutionPath.Count, grid.TotalCells);
                solution = new MazeSolution(entrance, exit, solutionPath, coverage);
                grid.Solution = solution;
                Console.WriteLine("✓ Solution path found");
                DisplaySolutionStatistics(solution, grid.TotalCells, config);
            }
            else
            {
                solution = new MazeSolution(entrance, exit, new List<Cell>(), 0);
                grid.Solution = solution;
                Console.WriteLine("Skipping solution path (--no-solution flag set)");
            }

            // Render output
            Console.WriteLine($"Rendering {config.OutputFormat} output...");
            var ext = outputRenderer.FileExtension;

            // Maze-only: pass entrance/exit (for boundary gap) but no path (no red line)
            var mazeOnlySolution = new MazeSolution(entrance, exit, new List<Cell>(), 0);
            var mazePath = $"{config.OutputBaseName}{ext}";
            outputRenderer.Render(grid, mazeOnlySolution, mazePath);
            Console.WriteLine($"  ✓ Saved maze to: {mazePath}");

            if (solution.Path.Count > 0)
            {
                var solutionOutputPath = $"{config.OutputBaseName}_solution{ext}";
                outputRenderer.Render(grid, solution, solutionOutputPath);
                Console.WriteLine($"  ✓ Saved solution to: {solutionOutputPath}");
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
        Console.WriteLine($"  Algorithm:          {config.Algorithm}");
        Console.WriteLine($"  Random Seed:        {(config.Seed.HasValue ? config.Seed.Value.ToString() : "Random")}");
        Console.WriteLine();
        Console.WriteLine("Output Settings:");
        Console.WriteLine($"  Base Name:          {config.OutputBaseName}");
        Console.WriteLine($"  Wall Thickness:     {config.WallThickness} pt");
        Console.WriteLine($"  Include Solution:   {(config.NoSolution ? "No" : "Yes")}");
        Console.WriteLine();
        Console.WriteLine("Page Settings:");
        Console.WriteLine($"  Format:             {config.PageSizeName} Portrait");
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

    private static void DisplaySolutionStatistics(MazeSolution solution, int totalCells, MazeConfiguration config)
    {
        Console.WriteLine();
        Console.WriteLine("Solution Path Statistics:");
        Console.WriteLine($"  Path Length:        {solution.Path.Count} cells");
        Console.WriteLine($"  Total Cells:        {totalCells}");
        Console.WriteLine($"  Coverage:           {solution.Coverage:F1}%");
        Console.WriteLine($"  Required:           {config.MinCoverage}%");
        Console.WriteLine($"  Meets Requirement:  {(solution.Coverage >= config.MinCoverage ? "✓ Yes" : "✗ No")}");

        if (config.MinCoverage > 0 && solution.Coverage >= config.MinCoverage)
        {
            Console.WriteLine();
            Console.WriteLine("✓ Coverage requirement satisfied with optimal entrance/exit selection.");
        }

        Console.WriteLine();
    }
}
