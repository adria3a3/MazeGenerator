namespace MazeGenerator.Models;

public class Cell
{
    public int RingIndex { get; init; }

    public int CellIndex { get; init; }

    public double AngleStart { get; init; }

    public double AngleEnd { get; init; }

    public double RadiusInner { get; init; }

    public double RadiusOuter { get; init; }

    public bool Visited { get; set; }

    public bool IsExit { get; set; }

    public Cell? ClockwiseNeighbor { get; set; }

    public Cell? CounterClockwiseNeighbor { get; set; }

    public List<Cell> InwardNeighbors { get; } = new List<Cell>();

    public List<Cell> OutwardNeighbors { get; } = new List<Cell>();

    private readonly List<Cell> _passages = new List<Cell>();

    public IReadOnlyList<Cell> Passages => _passages;

    public IReadOnlyList<Cell> GetPassableNeighbors()
    {
        return _passages;
    }

    public void CreatePassage(Cell neighbor)
    {
        if (!_passages.Contains(neighbor))
        {
            _passages.Add(neighbor);
        }
    }

    public void ClearPassages()
    {
        _passages.Clear();
    }

    public override string ToString()
    {
        return $"Cell[R{RingIndex},C{CellIndex}]";
    }
}
