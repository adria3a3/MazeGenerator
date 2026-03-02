using MazeGenerator.Models;

namespace MazeGenerator.Tests;

/// <summary>Shared helpers for creating test grids, eliminating per-file duplication.</summary>
internal static class TestGridFactory
{
    internal static MazeGrid CreateInitializedGrid(int rings = 3)
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

    internal static MazeGrid CreateGeneratedGrid(int rings = 3, int seed = 42)
    {
        var grid = CreateInitializedGrid(rings);
        new MazeGenerator.Services.MazeGenerator(seed).GenerateMaze(grid);
        return grid;
    }
}
