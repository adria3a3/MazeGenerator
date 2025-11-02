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

    [Option("braid", Default = 0.0, HelpText = "Probability (0.0-1.0) of removing dead ends to create a braided maze.")]
    public double Braid { get; init; }
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
            {
                Console.Error.WriteLine($"  • {error}");
            }
            return 1;
        }

        // Display configuration
        DisplayConfiguration(config);

        // Phase 3: Build grid
        Console.WriteLine("Building maze grid...");
        var grid = new MazeGrid(config);
        
        try
        {
            grid.Initialize();
            Console.WriteLine("✓ Grid initialized successfully");
            Console.WriteLine();
            
            // Display grid statistics
            DisplayGridStatistics(grid);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error building grid: {ex.Message}");
            return 1;
        }

        // Phase 4: Generate maze
        Console.WriteLine("Generating maze...");
        var generator = new Services.MazeGenerator(config.Seed);
        try
        {
            generator.GenerateMaze(grid);
            
            Console.WriteLine("✓ Maze generation complete");

            // We only validate for perfection if braiding is not applied.
            if (!Services.MazeValidation.IsPerfectMaze(grid))
            {
                Console.Error.WriteLine("✗ Error: Maze generation failed. The resulting maze is not a perfect maze.");
                DisplayMazeStatistics(grid); // Display stats for debugging
                return 1;
            }
            
            // Display generation statistics
            DisplayMazeStatistics(grid);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error generating maze: {ex.Message}");
            return 1;
        }

        // Phase 5 & 6: Find solution path with optimal entrance/exit selection
        // Skip this phase entirely if --no-solution flag is set
        if (!config.NoSolution)
        {
            Console.WriteLine("Finding solution path...");
            try
            {
                var random = config.Seed.HasValue ? new Random(config.Seed.Value) : new Random();
                var pathFinder = new Services.PathFinder();

                // 1. Find the optimal entrance/exit and create openings.
                // This method will also handle maze regeneration if coverage is not met.
                var (entrance, exit) = pathFinder.FindOptimalAndCreateOpenings(grid, generator, random, config.MinCoverage);

                var selectionMethod = "Optimal (diameter-based)";
                Console.WriteLine($"  Selection Method:   {selectionMethod}");
                Console.WriteLine($"  Entrance: Ring {entrance.RingIndex}, Cell {entrance.CellIndex}");
                Console.WriteLine($"  Exit:     Ring {exit.RingIndex}, Cell {exit.CellIndex}");

                // 2. Find the path now that the grid data is correct.
                var solutionPath = pathFinder.FindPath(entrance, exit);
                
                if (solutionPath.Count == 0)
                {
                    Console.Error.WriteLine("✗ Error: No path found from entrance to exit!");
                    return 1;
                }
                
                // Mark cells on solution path
                pathFinder.MarkSolutionPath(solutionPath);
                
                // Store the solution path in the grid for rendering
                grid.SolutionPath = solutionPath;
                grid.Entrance = entrance;
                grid.Exit = exit;
                
                Console.WriteLine("✓ Solution path found");
                
                // Display solution statistics
                DisplaySolutionStatistics(grid, solutionPath, config);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error finding solution path: {ex.Message}");
                return 1;
            }
        }
        else
        {
            Console.WriteLine("Skipping solution path generation (--no-solution flag set)");
        }

        // Phase 7: Render PDFs
        Console.WriteLine("Rendering PDF output...");
        try
        {
            var renderer = new Rendering.MazeRenderer();
            
            // Always render the maze without solution
            var mazePdfPath = $"{config.OutputBaseName}.pdf";
            renderer.RenderMazeToPdf(grid, mazePdfPath);
            
            // Render solution PDF only if a solution path exists
            if (grid.SolutionPath.Count > 0)
            {
                var solutionPdfPath = $"{config.OutputBaseName}_solution.pdf";
                renderer.RenderMazeWithSolutionToPdf(grid, solutionPdfPath);
            }
            
            Console.WriteLine("✓ PDF rendering complete");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error rendering PDF: {ex.Message}");
            return 1;
        }

        Console.WriteLine();
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                  Generation Complete!                    ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");

        return 0;
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
                i, 
                grid.Configuration.InnerRadius, 
                grid.RingWidth
            );
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
        // Count visited cells
        var visitedCells = 0;
        var totalConnections = 0;
        
        foreach (var ring in grid.Cells)
        {
            foreach (var cell in ring)
            {
                if (cell.Visited)
                    visitedCells++;
                
                // Count connections (each connection counted from both sides)
                totalConnections += cell.Passages.Count;
            }
        }
        
        // Divide by 2 since each passage is counted twice (once from each side)
        var passages = totalConnections / 2;
        var allVisited = visitedCells == grid.TotalCells;
        var isPerfectMaze = allVisited && (passages == grid.TotalCells - 1);
        
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
        var meetsCoverage = config.MinCoverage == 0 || coverage >= config.MinCoverage;
        
        Console.WriteLine();
        Console.WriteLine("Solution Path Statistics:");
        Console.WriteLine($"  Path Length:        {solutionPath.Count} cells");
        Console.WriteLine($"  Total Cells:        {grid.TotalCells}");
        Console.WriteLine($"  Coverage:           {coverage:F1}%");
        Console.WriteLine($"  Required:           {config.MinCoverage}%");
        Console.WriteLine($"  Meets Requirement:  {(meetsCoverage ? "✓ Yes" : "✗ No")}");
        
        if (!meetsCoverage)
        {
            Console.WriteLine();
            Console.WriteLine($"⚠ Warning: Solution path coverage ({coverage:F1}%) is below minimum ({config.MinCoverage}%)");
            Console.WriteLine("  This may indicate the maze structure or coverage requirement needs adjustment.");
        }
        else if (config.MinCoverage > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"✓ Coverage requirement satisfied with optimal entrance/exit selection.");
        }
        
        Console.WriteLine();
    }
}

