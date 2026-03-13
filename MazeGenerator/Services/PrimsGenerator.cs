using MazeGenerator.Models;

namespace MazeGenerator.Services;

public class PrimsGenerator : IMazeGenerator
{
    private readonly Random _random;

    public PrimsGenerator(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public void GenerateMaze(MazeGrid grid)
    {
        MazeGeneratorHelper.ResetGrid(grid);

        // Pick a random starting cell
        var startRing = grid.Cells[_random.Next(grid.Cells.Count)];
        var startCell = startRing[_random.Next(startRing.Count)];
        startCell.Visited = true;

        // Build the frontier: all edges from visited cells to unvisited neighbors
        var frontier = new List<(Cell visited, Cell unvisited)>();
        AddFrontierEdges(startCell, frontier);

        while (frontier.Count > 0)
        {
            // Pick a random frontier edge
            var index = _random.Next(frontier.Count);
            var (visited, unvisited) = frontier[index];

            // Swap-remove for O(1)
            frontier[index] = frontier[frontier.Count - 1];
            frontier.RemoveAt(frontier.Count - 1);

            if (unvisited.Visited)
                continue;

            // Carve passage
            visited.CreatePassage(unvisited);
            unvisited.CreatePassage(visited);
            unvisited.Visited = true;

            AddFrontierEdges(unvisited, frontier);
        }
    }

    private static void AddFrontierEdges(Cell cell, List<(Cell visited, Cell unvisited)> frontier)
    {
        foreach (var neighbor in MazeGeneratorHelper.GetAllNeighbors(cell))
        {
            if (!neighbor.Visited)
            {
                frontier.Add((cell, neighbor));
            }
        }
    }
}
