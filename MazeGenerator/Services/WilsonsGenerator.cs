using MazeGenerator.Models;

namespace MazeGenerator.Services;

public class WilsonsGenerator : IMazeGenerator
{
    private readonly Random _random;

    public WilsonsGenerator(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public void GenerateMaze(MazeGrid grid)
    {
        MazeGeneratorHelper.ResetGrid(grid);

        var allCells = grid.GetAllCells().ToList();
        var inMaze = new HashSet<Cell>();

        // Add a random starting cell to the maze
        var firstCell = allCells[_random.Next(allCells.Count)];
        firstCell.Visited = true;
        inMaze.Add(firstCell);

        // Process cells in random order
        var remaining = new List<Cell>(allCells);
        remaining.Remove(firstCell);
        for (var i = remaining.Count - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (remaining[i], remaining[j]) = (remaining[j], remaining[i]);
        }

        foreach (var startCell in remaining)
        {
            if (inMaze.Contains(startCell))
                continue;

            // Loop-erased random walk: store next-step for each cell on the walk
            var next = new Dictionary<Cell, Cell>();
            var current = startCell;

            while (!inMaze.Contains(current))
            {
                var neighbors = MazeGeneratorHelper.GetAllNeighbors(current);
                var chosen = neighbors[_random.Next(neighbors.Count)];
                next[current] = chosen;
                current = chosen;
            }

            // Walk from startCell following the next pointers (loop-free due to overwrites)
            current = startCell;
            while (!inMaze.Contains(current))
            {
                var nextCell = next[current];
                current.CreatePassage(nextCell);
                nextCell.CreatePassage(current);
                current.Visited = true;
                inMaze.Add(current);
                current = nextCell;
            }
        }

    }
}
