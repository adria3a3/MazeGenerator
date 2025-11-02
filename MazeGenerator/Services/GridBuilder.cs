using MazeGenerator.Models;

namespace MazeGenerator.Services;

public class GridBuilder
{
    public static void BuildGrid(MazeGrid grid)
    {
        // Step 1: Calculate cell counts for all rings, accounting for wall thickness
        grid.CellCounts = GeometryCalculator.CalculateCellCounts(
            grid.Configuration.Rings,
            grid.Configuration.InnerRadius,
            grid.RingWidth,
            grid.Configuration.WallThickness
        );
        
        // Validate cell counts
        if (!GeometryCalculator.ValidateCellCounts(grid.CellCounts))
        {
            throw new InvalidOperationException("Failed to generate valid cell counts for the maze grid.");
        }
        
        // Step 2: Create all cells
        CreateAllCells(grid);
        
        // Step 3: Establish neighbor relationships
        EstablishNeighbors(grid);
    }
    
    private static void CreateAllCells(MazeGrid grid)
    {
        grid.Cells.Clear();
        
        for (var ringIndex = 0; ringIndex < grid.Configuration.Rings; ringIndex++)
        {
            var ring = new List<Cell>();
            var cellCount = grid.CellCounts[ringIndex];
            
            // Get radii for this ring
            var (innerRadius, outerRadius) = GeometryCalculator.GetRingRadii(
                ringIndex,
                grid.Configuration.InnerRadius,
                grid.RingWidth
            );
            
            // Create each cell in the ring
            for (var cellIndex = 0; cellIndex < cellCount; cellIndex++)
            {
                var (startAngle, endAngle) = GeometryCalculator.CalculateCellAngles(cellIndex, cellCount);
                
                var cell = new Cell
                {
                    RingIndex = ringIndex,
                    CellIndex = cellIndex,
                    AngleStart = startAngle,
                    AngleEnd = endAngle,
                    RadiusInner = innerRadius,
                    RadiusOuter = outerRadius,
                    Visited = false,
                    IsExit = false
                };
                
                ring.Add(cell);
            }
            
            grid.Cells.Add(ring);
        }
    }
    
    private static void EstablishNeighbors(MazeGrid grid)
    {
        for (var ringIndex = 0; ringIndex < grid.Cells.Count; ringIndex++)
        {
            var ring = grid.Cells[ringIndex];
            var cellCount = ring.Count;
            
            for (var cellIndex = 0; cellIndex < cellCount; cellIndex++)
            {
                var cell = ring[cellIndex];
                
                // Clockwise and counter-clockwise neighbors (within same ring)
                EstablishTangentialNeighbors(cell, ring, cellIndex, cellCount);
                
                // Inward neighbors (inner ring)
                if (ringIndex > 0)
                {
                    EstablishInwardNeighbors(cell, grid.Cells[ringIndex - 1]);
                }
                
                // Outward neighbors (outer ring)
                if (ringIndex < grid.Cells.Count - 1)
                {
                    EstablishOutwardNeighbors(cell, grid.Cells[ringIndex + 1]);
                }
            }
        }
    }
    
    private static void EstablishTangentialNeighbors(Cell cell, List<Cell> ring, int cellIndex, int cellCount)
    {
        // Clockwise neighbor (next cell in ring, with wrap-around)
        var clockwiseIndex = (cellIndex + 1) % cellCount;
        cell.ClockwiseNeighbor = ring[clockwiseIndex];
        
        // Counter-clockwise neighbor (previous cell in ring, with wrap-around)
        var counterClockwiseIndex = (cellIndex - 1 + cellCount) % cellCount;
        cell.CounterClockwiseNeighbor = ring[counterClockwiseIndex];
    }
    
    private static void EstablishInwardNeighbors(Cell cell, List<Cell> innerRing)
    {
        cell.InwardNeighbors.Clear();
        
        foreach (var innerCell in innerRing)
        {
            // Any angular overlap makes them potential neighbors
            if (GeometryCalculator.GetAngularOverlap(cell.AngleStart, cell.AngleEnd, innerCell.AngleStart, innerCell.AngleEnd) > 0)
            {
                cell.InwardNeighbors.Add(innerCell);
            }
        }
    }
    
    private static void EstablishOutwardNeighbors(Cell cell, List<Cell> outerRing)
    {
        cell.OutwardNeighbors.Clear();

        foreach (var outerCell in outerRing)
        {
            // Any angular overlap makes them potential neighbors
            if (GeometryCalculator.GetAngularOverlap(cell.AngleStart, cell.AngleEnd, outerCell.AngleStart, outerCell.AngleEnd) > 0)
            {
                cell.OutwardNeighbors.Add(outerCell);
            }
        }
    }
}
