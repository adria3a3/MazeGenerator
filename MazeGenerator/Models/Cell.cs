namespace MazeGenerator.Models;

public class Cell
{
    public int RingIndex { get; set; }
    
    public int CellIndex { get; set; }
    
    public double AngleStart { get; set; }
    
    public double AngleEnd { get; set; }
    
    public double RadiusInner { get; set; }
    
    public double RadiusOuter { get; set; }
    
    public bool Visited { get; set; }
    
    public bool IsExit { get; set; }
    
    public Cell? ClockwiseNeighbor { get; set; }
    
    public Cell? CounterClockwiseNeighbor { get; set; }
    
    public List<Cell> InwardNeighbors { get; } = new List<Cell>();
    
    public List<Cell> OutwardNeighbors { get; } = new List<Cell>();
    
    public List<Cell> Passages { get; } = new List<Cell>();

    public IReadOnlyList<Cell> GetPassableNeighbors()
    {
        return Passages;
    }

    public void CreatePassage(Cell neighbor)
    {
        if (!Passages.Contains(neighbor))
        {
            Passages.Add(neighbor);
        }
    }

    public override string ToString()
    {
        return $"Cell[R{RingIndex},C{CellIndex}]";
    }
}

