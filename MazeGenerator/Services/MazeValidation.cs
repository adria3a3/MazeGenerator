using MazeGenerator.Models;

namespace MazeGenerator.Services;

public static class MazeValidation
{
    public static bool IsPerfectMaze(MazeGrid grid)
    {
        if (grid.TotalCells == 0)
            return false;

        // 1. Check if all cells were visited during generation (ensures connectivity).
        // The generation algorithm should have marked all cells as visited.
        var visitedCellCount = grid.Cells.SelectMany(ring => ring).Count(cell => cell.Visited);
        if (visitedCellCount != grid.TotalCells)
        {
            return false; // Not all cells are part of the maze.
        }

        // 2. Check if the number of passages (edges) is correct for a spanning tree.
        // For a connected graph with N nodes to be a tree, it must have N-1 edges.
        var totalConnections = grid.Cells.SelectMany(ring => ring).Sum(cell => cell.Passages.Count);
        var passages = totalConnections / 2; // Each passage is counted twice (once per cell).

        return passages == grid.TotalCells - 1 && !HasCycles(grid);
    }

    public static bool HasCycles(MazeGrid grid)
    {
        if (grid.TotalCells == 0)
            return false;

        var visited = new HashSet<Cell>();
        var stack = new Stack<(Cell cell, Cell? parent)>();

        foreach (var startCell in grid.GetAllCells())
        {
            if (!visited.Contains(startCell))
            {
                stack.Push((startCell, null));

                while (stack.Count > 0)
                {
                    var (current, parent) = stack.Pop();

                    if (visited.Contains(current))
                        return true; // Cycle detected

                    visited.Add(current);

                    foreach (var neighbor in current.GetPassableNeighbors())
                    {
                        if (neighbor != parent)
                        {
                            stack.Push((neighbor, current));
                        }
                    }
                }
            }
        }

        return false;
    }
}
