using MazeGenerator.Models;

namespace MazeGenerator.Services;

public class MazeGenerator
{
    private readonly Random _random;

    public MazeGenerator(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public void GenerateMaze(MazeGrid grid)
    {
        // Reset all cells to a default state
        foreach (var cell in grid.GetAllCells())
        {
            cell.Visited = false;
            cell.Passages.Clear(); // Reset passages
            cell.IsExit = false; // Reset exit flag
        }
        
        // Also clear the grid's entrance, exit, and solution path
        grid.Entrance = null;
        grid.Exit = null;
        grid.SolutionPath.Clear();

        // Start from a random cell in the maze
        var stack = new Stack<Cell>();
        var startCell = grid.Cells[_random.Next(grid.Cells.Count)][0];
        
        startCell.Visited = true;
        stack.Push(startCell);

        // Iterative depth-first search
        while (stack.Count > 0)
        {
            var current = stack.Peek();
            var unvisitedNeighbors = GetUnvisitedNeighbors(current);

            if (unvisitedNeighbors.Count > 0)
            {
                // Randomly choose an unvisited neighbor
                var chosen = unvisitedNeighbors[_random.Next(unvisitedNeighbors.Count)];
                
                // Remove wall between current and chosen
                RemoveWallBetween(current, chosen);
                
                // Mark chosen as visited and push to stack
                chosen.Visited = true;
                stack.Push(chosen);
            }
            else
            {
                // No unvisited neighbors, backtrack
                stack.Pop();
            }
        }
    }

    private List<Cell> GetUnvisitedNeighbors(Cell cell)
    {
        var unvisited = new List<Cell>();

        // Check clockwise neighbor
        if (cell.ClockwiseNeighbor != null && !cell.ClockwiseNeighbor.Visited)
            unvisited.Add(cell.ClockwiseNeighbor);

        // Check counter-clockwise neighbor
        if (cell.CounterClockwiseNeighbor != null && !cell.CounterClockwiseNeighbor.Visited)
            unvisited.Add(cell.CounterClockwiseNeighbor);

        // Check inward neighbors
        foreach (var neighbor in cell.InwardNeighbors)
        {
            if (!neighbor.Visited)
                unvisited.Add(neighbor);
        }

        // Check outward neighbors
        foreach (var neighbor in cell.OutwardNeighbors)
        {
            if (!neighbor.Visited)
                unvisited.Add(neighbor);
        }

        return unvisited;
    }

    private void RemoveWallBetween(Cell current, Cell neighbor)
    {
        current.CreatePassage(neighbor);
        neighbor.CreatePassage(current);
    }
}
