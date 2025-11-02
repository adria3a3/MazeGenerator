using MazeGenerator.Models;

namespace MazeGenerator.Services;

public class PathFinder
{
    public List<Cell> FindPath(Cell entrance, Cell exit)
    {
        // BFS to find path. The `SelectOptimalEntranceExit` method has already
        // made the exit cell a fully connected part of the graph, so we can
        // and should target it directly.
        var queue = new Queue<Cell>();
        var visited = new HashSet<Cell>();
        var parent = new Dictionary<Cell, Cell>();
        
        queue.Enqueue(entrance);
        visited.Add(entrance);
        
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            
            // Found the exit cell directly.
            if (current == exit)
            {
                return ReconstructPath(parent, entrance, exit);
            }
            
            // Explore connected neighbors (only cells we have passages to)
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
        
        // No path found
        return [];
    }
    
    private List<Cell> ReconstructPath(Dictionary<Cell, Cell> parent, Cell entrance, Cell exit)
    {
        var path = new List<Cell>();
        var current = exit;
        
        // Trace back from exit to entrance
        while (current != entrance)
        {
            path.Add(current);
            if (!parent.ContainsKey(current))
            {
                // Path broken (shouldn't happen)
                return [];
            }
            current = parent[current];
        }
        
        // Add the entrance
        path.Add(entrance);
        
        // Reverse to get path from entrance to exit
        path.Reverse();
        
        return path;
    }
    
    public Cell SelectEntrance(MazeGrid grid, Random random)
    {
        // Get the innermost ring (ring 0)
        var innerRing = grid.Cells[0];
        
        // Select a random cell from the inner ring
        var entranceIndex = random.Next(innerRing.Count);
        var entrance = innerRing[entranceIndex];
        
        return entrance;
    }
    
    public Cell SelectExit(MazeGrid grid, Random random)
    {
        // Get the outermost ring
        var outerRing = grid.Cells[grid.Cells.Count - 1];
        
        // Select a random cell from the outer ring
        var exitIndex = random.Next(outerRing.Count);
        var exit = outerRing[exitIndex];
        
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
        
        queue.Enqueue(start);
        visited.Add(start);
        
        var farthest = start;
        
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            farthest = current; // Track the last cell we process (deepest in BFS)
            
            // Explore connected neighbors
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
        
        // Reconstruct path from start to farthest
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
        
        // BFS to explore all reachable cells
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var currentDistance = distances[current];
            
            // Explore connected neighbors
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
        
        // Find the cell in targetRing with the maximum distance
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
        
        // If no cell in target ring is reachable, throw exception
        if (farthest == null)
        {
            throw new InvalidOperationException("No cell in the target ring is reachable from the start cell.");
        }
        
        // Reconstruct path from start to farthest
        var path = ReconstructPath(parent, start, farthest);
        
        return (farthest, path);
    }
    
    public (Cell endpoint1, Cell endpoint2, List<Cell> path) FindDiameter(MazeGrid grid)
    {
        // Get any starting cell (first cell in first ring)
        var anyCell = grid.Cells[0][0];
        
        // First pass: find the farthest cell from an arbitrary start point
        var (endpoint1, _) = FindFarthestCell(anyCell);
        
        // Second pass: find the farthest cell from endpoint1
        var (endpoint2, path) = FindFarthestCell(endpoint1);
        
        return (endpoint1, endpoint2, path);
    }
    
    public (Cell entrance, Cell exit) FindOptimalAndCreateOpenings(
        MazeGrid grid,
        MazeGenerator generator,
        Random random,
        int minCoverage,
        int maxRetries = 10)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            // 1. Select a random entrance on the innermost ring (ring 0).
            var innerRing = grid.Cells[0];
            var entrance = innerRing[random.Next(innerRing.Count)];

            // 2. Find the best exit on the OUTER RING (requirement: exit must be on outer ring).
            var outerRing = grid.Cells[grid.Cells.Count - 1];
            var (exit, path) = FindFarthestCellInRing(entrance, outerRing);

            // 3. Check if the path meets the minimum coverage.
            var coverage = CalculateCoverage(path.Count, grid.TotalCells);
            if (coverage >= minCoverage)
            {
                // If coverage is met, create openings and return.
                CreateOpenings(exit);
                return (entrance, exit);
            }

            // If coverage is not met, regenerate the maze and try again.
            if (i < maxRetries - 1)
            {
                Console.WriteLine($"  Path coverage {coverage:F1}% < {minCoverage}%. Regenerating maze (attempt {i + 2}/{maxRetries})...");
                generator.GenerateMaze(grid); // Regenerate
            }
        }

        // If max retries are reached, fail with an exception.
        throw new InvalidOperationException($"Failed to generate a maze with at least {minCoverage}% solution coverage after {maxRetries} attempts.");
    }

    private void CreateOpenings(Cell exit)
    {
        // The renderer is responsible for not drawing the outermost wall of the exit.
        exit.IsExit = true;
    }

    public List<Cell> FindSolution(MazeGrid grid)
    {
        // The starting point is the entire open center (ring 0)
        var startNodes = grid.Cells[0]; 
        var exitCell = grid.GetExitCell();

        if (exitCell == null)
        {
            Console.WriteLine("Error: Exit cell not found.");
            return [];
        }

        // --- Pathfinding Setup ---
        var queue = new Queue<Cell>();
        var cameFrom = new Dictionary<Cell, Cell?>();

        // Enqueue all starting cells and mark them as visited
        foreach (var startCell in startNodes)
        {
            queue.Enqueue(startCell);
            cameFrom[startCell] = null; // No predecessor for start nodes
        }
        
        var exitFound = false;

        // --- Breadth-First Search ---
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current == exitCell)
            {
                exitFound = true;
                break;
            }

            foreach (var neighbor in current.Passages)
            {
                if (!cameFrom.ContainsKey(neighbor))
                {
                    cameFrom[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }
        }

        // --- Reconstruct Path ---
        if (exitFound)
        {
            return ReconstructPathFromCenter(cameFrom, exitCell);
        }

        Console.WriteLine("Error: No solution found from the center to the exit.");
        return []; // No path found
    }

    private List<Cell> ReconstructPathFromCenter(Dictionary<Cell, Cell?> cameFrom, Cell exitCell)
    {
        var path = new List<Cell>();
        var current = exitCell;
        
        while (current != null)
        {
            path.Add(current);
            if (!cameFrom.TryGetValue(current, out var parent))
            {
                // Should not happen if exit was found
                break; 
            }
            current = parent;
        }
        
        path.Reverse();
        return path;
    }
}
