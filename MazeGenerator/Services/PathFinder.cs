using MazeGenerator.Models;

namespace MazeGenerator.Services;

public class PathFinder
{
    public List<Cell> FindPath(Cell entrance, Cell exit)
    {
        // BFS to find path.
        var queue = new Queue<Cell>();
        var visited = new HashSet<Cell>();
        var parent = new Dictionary<Cell, Cell>();

        queue.Enqueue(entrance);
        visited.Add(entrance);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current == exit)
            {
                return ReconstructPath(parent, entrance, exit);
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

    private List<Cell> ReconstructPath(Dictionary<Cell, Cell> parent, Cell entrance, Cell exit)
    {
        var path = new List<Cell>();
        var current = exit;

        while (current != entrance)
        {
            path.Add(current);
            if (!parent.ContainsKey(current))
            {
                // Path broken (shouldn't happen)
                return new List<Cell>();
            }
            current = parent[current];
        }

        path.Add(entrance);
        path.Reverse();
        return path;
    }

    public Cell SelectEntrance(MazeGrid grid, Random random)
    {
        if (grid.Cells.Count == 0 || grid.Cells[0].Count == 0)
            throw new ArgumentException("Grid has no cells in the innermost ring.", nameof(grid));

        var innerRing = grid.Cells[0];
        return innerRing[random.Next(innerRing.Count)];
    }

    public Cell SelectExit(MazeGrid grid, Random random)
    {
        if (grid.Cells.Count == 0)
            throw new ArgumentException("Grid has no rings.", nameof(grid));

        var outerRing = grid.Cells[grid.Cells.Count - 1];
        if (outerRing.Count == 0)
            throw new ArgumentException("Grid has no cells in the outermost ring.", nameof(grid));

        var exit = outerRing[random.Next(outerRing.Count)];
        exit.IsExit = true;
        return exit;
    }

    public void MarkSolutionPath(List<Cell> path)
    {
        // Path is stored in grid.SolutionPath, no need to mark individual cells
    }

    public double CalculateCoverage(int pathLength, int totalCells)
    {
        if (totalCells == 0)
            return 0.0;

        return (double)pathLength / totalCells * 100.0;
    }

    private (Cell farthest, List<Cell> path) FindFarthestCell(Cell start)
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

            // Track the cell with the greatest BFS distance from start
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

    private (Cell farthest, List<Cell> path) FindFarthestCellInRing(Cell start, List<Cell> targetRing)
    {
        var queue = new Queue<Cell>();
        var visited = new HashSet<Cell>();
        var parent = new Dictionary<Cell, Cell>();
        var distances = new Dictionary<Cell, int>();

        queue.Enqueue(start);
        visited.Add(start);
        distances[start] = 0;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var currentDistance = distances[current];

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

        Cell? farthest = null;
        var maxDistance = -1;

        foreach (var cell in targetRing)
        {
            if (distances.ContainsKey(cell) && distances[cell] > maxDistance)
            {
                maxDistance = distances[cell];
                farthest = cell;
            }
        }

        if (farthest == null)
            throw new InvalidOperationException("No cell in the target ring is reachable from the start cell.");

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

    public (Cell entrance, Cell exit) FindOptimalAndCreateOpenings(
        MazeGrid grid,
        IMazeGenerator generator,
        Random random,
        int minCoverage,
        int maxRetries = 10,
        Action<string>? log = null)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            // 1. Select a random entrance on the innermost ring (ring 0).
            var innerRing = grid.Cells[0];
            var entrance = innerRing[random.Next(innerRing.Count)];

            // 2. Find the best exit on the outermost ring.
            var outerRing = grid.Cells[grid.Cells.Count - 1];
            var (exit, path) = FindFarthestCellInRing(entrance, outerRing);

            // 3. Check if the path meets the minimum coverage.
            var coverage = CalculateCoverage(path.Count, grid.TotalCells);
            if (coverage >= minCoverage)
            {
                CreateOpenings(exit);
                return (entrance, exit);
            }

            // Coverage not met — regenerate and try again.
            if (i < maxRetries - 1)
            {
                log?.Invoke($"  Path coverage {coverage:F1}% < {minCoverage}%. Regenerating maze (attempt {i + 2}/{maxRetries})...");
                generator.GenerateMaze(grid);
            }
        }

        throw new InvalidOperationException(
            $"Failed to generate a maze with at least {minCoverage}% solution coverage after {maxRetries} attempts.");
    }

    private void CreateOpenings(Cell exit)
    {
        // The renderer is responsible for not drawing the outermost wall of the exit.
        exit.IsExit = true;
    }

    public List<Cell> FindSolution(MazeGrid grid)
    {
        var startCell = grid.Entrance;
        var exitCell = grid.GetExitCell();

        if (startCell == null || exitCell == null)
            return new List<Cell>();

        return FindPath(startCell, exitCell);
    }
}
