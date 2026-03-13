namespace MazeGenerator.Models;

public class MazeGrid
{
    public MazeConfiguration Configuration { get; }

    public List<List<Cell>> Cells { get; set; } = new List<List<Cell>>();

    public MazeSolution? Solution { get; set; }

    public double CenterX { get; }

    public double CenterY { get; }

    public double UsableRadius { get; }

    public double RingWidth { get; }

    public List<int> CellCounts { get; set; } = new List<int>();

    public int TotalCells => Cells.Sum(ring => ring.Count);

    public MazeGrid(MazeConfiguration configuration)
    {
        Configuration = configuration;

        CenterX = Configuration.PageWidth / 2.0;
        CenterY = Configuration.PageHeight / 2.0;

        var maxRadiusX = (Configuration.PageWidth / 2.0) - Configuration.Margin;
        var maxRadiusY = (Configuration.PageHeight / 2.0) - Configuration.Margin;
        UsableRadius = Math.Min(maxRadiusX, maxRadiusY);

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
