using MazeGenerator.Models;

namespace MazeGenerator.Services;

public class KruskalsGenerator : IMazeGenerator
{
    private readonly Random _random;

    public KruskalsGenerator(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public void GenerateMaze(MazeGrid grid)
    {
        MazeGeneratorHelper.ResetGrid(grid);

        var allCells = grid.GetAllCells().ToList();

        // Union-Find: each cell starts as its own set
        var parent = new Dictionary<Cell, Cell>();
        var rank = new Dictionary<Cell, int>();
        foreach (var cell in allCells)
        {
            parent[cell] = cell;
            rank[cell] = 0;
            cell.Visited = true;
        }

        // Collect all edges (each pair once) using stable cell index ordering
        var edges = new List<(Cell a, Cell b)>();
        var cellIndex = new Dictionary<Cell, int>();
        for (var i = 0; i < allCells.Count; i++)
            cellIndex[allCells[i]] = i;

        foreach (var cell in allCells)
        {
            var ci = cellIndex[cell];
            foreach (var neighbor in MazeGeneratorHelper.GetAllNeighbors(cell))
            {
                if (ci < cellIndex[neighbor])
                    edges.Add((cell, neighbor));
            }
        }

        // Fisher-Yates shuffle
        for (var i = edges.Count - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (edges[i], edges[j]) = (edges[j], edges[i]);
        }

        foreach (var (a, b) in edges)
        {
            var rootA = Find(parent, a);
            var rootB = Find(parent, b);

            if (rootA != rootB)
            {
                Union(parent, rank, rootA, rootB);
                a.CreatePassage(b);
                b.CreatePassage(a);
            }
        }
    }

    private static Cell Find(Dictionary<Cell, Cell> parent, Cell cell)
    {
        while (parent[cell] != cell)
        {
            parent[cell] = parent[parent[cell]]; // Path compression
            cell = parent[cell];
        }
        return cell;
    }

    private static void Union(Dictionary<Cell, Cell> parent, Dictionary<Cell, int> rank, Cell a, Cell b)
    {
        if (rank[a] < rank[b])
            parent[a] = b;
        else if (rank[a] > rank[b])
            parent[b] = a;
        else
        {
            parent[b] = a;
            rank[a]++;
        }
    }
}
