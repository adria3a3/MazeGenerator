using MazeGenerator.Models;

namespace MazeGenerator.Services;

internal static class MazeGeneratorHelper
{
    internal static void ResetGrid(MazeGrid grid)
    {
        foreach (var cell in grid.GetAllCells())
        {
            cell.Visited = false;
            cell.ClearPassages();
            cell.IsExit = false;
        }

        grid.Solution = null;
    }

    internal static List<Cell> GetAllNeighbors(Cell cell)
    {
        var neighbors = new List<Cell>();

        if (cell.ClockwiseNeighbor != null)
            neighbors.Add(cell.ClockwiseNeighbor);
        if (cell.CounterClockwiseNeighbor != null)
            neighbors.Add(cell.CounterClockwiseNeighbor);
        neighbors.AddRange(cell.InwardNeighbors);
        neighbors.AddRange(cell.OutwardNeighbors);

        return neighbors;
    }
}
