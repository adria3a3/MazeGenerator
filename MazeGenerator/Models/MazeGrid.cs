namespace MazeGenerator.Models;

public class MazeGrid
{
    public MazeConfiguration Configuration { get; set; }
    
    public List<List<Cell>> Cells { get; set; } = new List<List<Cell>>();
    
    public Cell? Entrance { get; set; }
    
    public Cell? Exit { get; set; }
    
    public List<Cell> SolutionPath { get; set; } = new List<Cell>();
    
    public double CenterX { get; set; }
    
    public double CenterY { get; set; }
    
    public double UsableRadius { get; set; }
    
    public double RingWidth { get; set; }
    
    public List<int> CellCounts { get; set; } = new List<int>();
    
    public int TotalCells => Cells.Sum(ring => ring.Count);
    
    public MazeGrid(MazeConfiguration configuration)
    {
        Configuration = configuration;
        
        // Calculate center position
        CenterX = Configuration.PageWidth / 2.0;
        CenterY = Configuration.PageHeight / 2.0;
        
        // Calculate usable radius (distance from center to edge minus margin)
        var maxRadiusX = (Configuration.PageWidth / 2.0) - Configuration.Margin;
        var maxRadiusY = (Configuration.PageHeight / 2.0) - Configuration.Margin;
        UsableRadius = Math.Min(maxRadiusX, maxRadiusY);
        
        // Calculate ring width
        RingWidth = (UsableRadius - Configuration.InnerRadius) / Configuration.Rings;
    }

    public void Initialize()
    {
        Services.GridBuilder.BuildGrid(this);
    }
    
    public IEnumerable<Cell> GetAllCells()
    {
        return Cells.SelectMany(ring => ring);
    }
    
    public Cell? GetExitCell()
    {
        return GetAllCells().FirstOrDefault(c => c.IsExit);
    }
}
