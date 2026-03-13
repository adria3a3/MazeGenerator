using MazeGenerator.Models;

namespace MazeGenerator.Services;

public class PathFinder : IPathFinder
{
    public List<Cell> FindPath(Cell start, Cell end)
    {
        var queue = new Queue<Cell>();
        var visited = new HashSet<Cell>();
        var parent = new Dictionary<Cell, Cell>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current == end)
            {
                return ReconstructPath(parent, start, end);
            }

            foreach (var neighbor in current.GetPassableNeighbors())
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    parent[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }
        }

        return new List<Cell>();
    }

    private List<Cell> ReconstructPath(Dictionary<Cell, Cell> parent, Cell start, Cell end)
    {
        var path = new List<Cell>();
        var current = end;

        while (current != start)
        {
            path.Add(current);
            current = parent[current];
        }

        path.Add(start);
        path.Reverse();
        return path;
    }

    public double CalculateCoverage(int pathLength, int totalCells)
    {
        if (totalCells == 0)
            return 0.0;

        return (double)pathLength / totalCells * 100.0;
    }

    internal (Cell farthest, List<Cell> path) FindFarthestCell(Cell start)
    {
        var queue = new Queue<Cell>();
        var visited = new HashSet<Cell>();
        var parent = new Dictionary<Cell, Cell>();
        var distances = new Dictionary<Cell, int>();

        queue.Enqueue(start);
        visited.Add(start);
        distances[start] = 0;

        var farthest = start;
        var maxDistance = 0;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var currentDistance = distances[current];

            if (currentDistance > maxDistance)
            {
                maxDistance = currentDistance;
                farthest = current;
            }

            foreach (var neighbor in current.GetPassableNeighbors())
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    parent[neighbor] = current;
                    distances[neighbor] = currentDistance + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }

        var path = ReconstructPath(parent, start, farthest);
        return (farthest, path);
    }

    public (Cell endpoint1, Cell endpoint2, List<Cell> path) FindDiameter(MazeGrid grid)
    {
        if (grid.Cells.Count == 0 || grid.Cells[0].Count == 0)
            throw new ArgumentException("Grid has no cells.", nameof(grid));

        var anyCell = grid.Cells[0][0];
        var (endpoint1, _) = FindFarthestCell(anyCell);
        var (endpoint2, path) = FindFarthestCell(endpoint1);
        return (endpoint1, endpoint2, path);
    }

    internal List<Cell> FindSolution(MazeGrid grid)
    {
        var startCell = grid.Solution?.Entrance;
        var exitCell = grid.GetExitCell();

        if (startCell == null || exitCell == null)
            return new List<Cell>();

        return FindPath(startCell, exitCell);
    }
}
