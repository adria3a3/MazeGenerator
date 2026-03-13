using MazeGenerator.Models;

namespace MazeGenerator.Services;

public class EntranceExitSelector : IEntranceExitSelector
{
    private readonly IPathFinder _pathFinder;

    public EntranceExitSelector(IPathFinder pathFinder)
    {
        _pathFinder = pathFinder;
    }

    public (Cell entrance, Cell exit) FindOptimalAndCreateOpenings(
        MazeGrid grid,
        IMazeGenerator generator,
        int minCoverage,
        int maxRetries = 10,
        Action<string>? log = null)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            // Try ALL ring-0 entrances and pick the one with the longest path to the outer ring.
            var (bestEntrance, bestExit, bestPath) = FindBestEntranceExitPair(grid);

            var coverage = _pathFinder.CalculateCoverage(bestPath.Count, grid.TotalCells);
            if (coverage >= minCoverage)
            {
                CreateOpenings(bestExit);
                return (bestEntrance, bestExit);
            }

            if (i < maxRetries - 1)
            {
                log?.Invoke($"  Path coverage {coverage:F1}% < {minCoverage}%. Regenerating maze (attempt {i + 2}/{maxRetries})...");
                generator.GenerateMaze(grid);
            }
        }

        throw new InvalidOperationException(
            $"Failed to generate a maze with at least {minCoverage}% solution coverage after {maxRetries} attempts.");
    }

    internal (Cell entrance, Cell exit, List<Cell> path) FindBestEntranceExitPair(MazeGrid grid)
    {
        if (grid.Cells.Count == 0 || grid.Cells[0].Count == 0)
            throw new ArgumentException("Grid has no cells in the innermost ring.", nameof(grid));

        var innerRing = grid.Cells[0];
        var outerRing = grid.Cells[grid.Cells.Count - 1];

        // Strategy 1: Try all ring-0 entrances, find farthest outer-ring exit for each
        Cell? bestEntrance = null;
        Cell? bestExit = null;
        List<Cell>? bestPath = null;
        var bestLength = -1;

        foreach (var candidate in innerRing)
        {
            var (exit, path) = FindFarthestCellInRing(candidate, outerRing);
            if (path.Count > bestLength)
            {
                bestLength = path.Count;
                bestEntrance = candidate;
                bestExit = exit;
                bestPath = path;
            }
        }

        return (bestEntrance!, bestExit!, bestPath!);
    }


    internal Cell SelectEntrance(MazeGrid grid, Random random)
    {
        if (grid.Cells.Count == 0 || grid.Cells[0].Count == 0)
            throw new ArgumentException("Grid has no cells in the innermost ring.", nameof(grid));

        var innerRing = grid.Cells[0];
        return innerRing[random.Next(innerRing.Count)];
    }

    private static void CreateOpenings(Cell exit)
    {
        exit.IsExit = true;
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

    private static List<Cell> ReconstructPath(Dictionary<Cell, Cell> parent, Cell start, Cell end)
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
}
