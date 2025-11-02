using MazeGenerator.Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace MazeGenerator.Rendering;

public class MazeRenderer
{
    public void RenderMazeToPdf(MazeGrid grid, string outputPath)
    {
        using var document = CreatePdfDocument();
        var page = document.Pages[0];
        using var gfx = XGraphics.FromPdfPage(page);
        
        DrawMaze(gfx, grid, drawSolution: false);
        
        document.Save(outputPath);
        Console.WriteLine($"✓ Saved maze to: {outputPath}");
    }
    
    public void RenderMazeWithSolutionToPdf(MazeGrid grid, string outputPath)
    {
        using var document = CreatePdfDocument();
        var page = document.Pages[0];
        using var gfx = XGraphics.FromPdfPage(page);
        
        DrawMaze(gfx, grid, drawSolution: true);
        
        document.Save(outputPath);
        Console.WriteLine($"✓ Saved solution to: {outputPath}");
    }
    
    private PdfDocument CreatePdfDocument()
    {
        var document = new PdfDocument();
        document.Info.Title = "Circular Maze";
        document.Info.Creator = "Circular Maze Generator";
        
        var page = document.AddPage();
        // A2 size in points (72 points = 1 inch, A2 = 420×594 mm)
        page.Width = XUnit.FromMillimeter(420);  // A2 width
        page.Height = XUnit.FromMillimeter(594); // A2 height
        
        return document;
    }
    
    private void DrawMaze(XGraphics gfx, MazeGrid grid, bool drawSolution)
    {
        var config = grid.Configuration;
        var wallPen = new XPen(XColors.Black, config.WallThickness);
        wallPen.LineCap = XLineCap.Flat; // Use Flat (Butt) cap to ensure openings are exact size
        
        // Draw the maze structure
        DrawMazeWalls(gfx, grid, wallPen);
        
        // Draw solution path if requested
        if (drawSolution && grid.SolutionPath.Count > 0)
        {
            DrawSolutionPath(gfx, grid);
        }
    }
    
    private void DrawMazeWalls(XGraphics gfx, MazeGrid grid, XPen wallPen)
    {
        var centerX = grid.CenterX;
        var centerY = grid.CenterY;
        var wallThickness = grid.Configuration.WallThickness;

        // First, draw the complete inner and outer boundary rings
        DrawBoundaryRings(gfx, grid, wallPen);

        foreach (var cell in grid.GetAllCells())
        {
            var passages = cell.GetPassableNeighbors();

            // --- Draw Tangential Walls (between cells in the same ring) ---
            if (cell.CounterClockwiseNeighbor != null && !passages.Contains(cell.CounterClockwiseNeighbor))
            {
                DrawRadialLine(gfx, wallPen, centerX, centerY, cell.AngleStart, cell.RadiusInner, cell.RadiusOuter);
            }
            
            // --- Draw Radial Walls (between rings) ---
            // Inward-facing wall (arc) - only for cells NOT in the innermost ring
            if (cell.RingIndex > 0)
            {
                if (!passages.Any(p => p.RingIndex < cell.RingIndex))
                {
                    // No inward passages - draw complete arc
                    DrawArc(gfx, wallPen, centerX, centerY, cell.RadiusInner, cell.AngleStart, cell.AngleEnd);
                }
                else
                {
                    // Has inward passages - draw arc with openings
                    var inwardPassages = passages.Where(p => p.RingIndex < cell.RingIndex).ToList();
                    DrawPartialArc(gfx, wallPen, centerX, centerY, cell.RadiusInner, cell.AngleStart, cell.AngleEnd, inwardPassages, wallThickness);
                }
            }

            // Outward-facing wall (arc) - only for cells NOT in the outermost ring
            if (cell.RingIndex < grid.Cells.Count - 1)
            {
                if (!passages.Any(p => p.RingIndex > cell.RingIndex))
                {
                    // No outward passages - draw complete arc
                    DrawArc(gfx, wallPen, centerX, centerY, cell.RadiusOuter, cell.AngleStart, cell.AngleEnd);
                }
                else
                {
                    // Has outward passages - draw arc with openings
                    var outwardPassages = passages.Where(p => p.RingIndex > cell.RingIndex).ToList();
                    DrawPartialArc(gfx, wallPen, centerX, centerY, cell.RadiusOuter, cell.AngleStart, cell.AngleEnd, outwardPassages, wallThickness);
                }
            }
        }
    }

    private void DrawBoundaryRings(XGraphics gfx, MazeGrid grid, XPen wallPen)
    {
        var centerX = grid.CenterX;
        var centerY = grid.CenterY;
        var wallThickness = grid.Configuration.WallThickness;
        
        // Get the innermost and outermost radii
        var innerRing = grid.Cells[0];
        var outerRing = grid.Cells[grid.Cells.Count - 1];
        
        var innermostRadius = innerRing[0].RadiusInner;
        var outermostRadius = outerRing[0].RadiusOuter;
        
        // Draw the innermost circle with opening for entrance
        if (grid.Entrance != null)
        {
            // Calculate opening size: at least 15 times the wall (line) thickness
            var minArcLength = wallThickness * 15.0;
            var minAngularWidth = minArcLength / innermostRadius;
            
            var entranceMidAngle = (grid.Entrance.AngleStart + grid.Entrance.AngleEnd) / 2.0;
            var openingStart = entranceMidAngle - minAngularWidth / 2.0;
            var openingEnd = entranceMidAngle + minAngularWidth / 2.0;
            
            // Draw the inner circle in two arcs, skipping the entrance opening
            // First arc: from opening end to opening start (going the long way)
            DrawArc(gfx, wallPen, centerX, centerY, innermostRadius, openingEnd, openingStart + 2 * Math.PI);
        }
        else
        {
            // No entrance defined, draw complete circle
            DrawArc(gfx, wallPen, centerX, centerY, innermostRadius, 0, 2 * Math.PI);
        }
        
        // Draw the outermost circle with opening for exit
        if (grid.Exit != null)
        {
            // Calculate opening size: at least 15 times the wall (line) thickness
            var minArcLength = wallThickness * 15.0;
            var minAngularWidth = minArcLength / outermostRadius;
            
            var exitMidAngle = (grid.Exit.AngleStart + grid.Exit.AngleEnd) / 2.0;
            var openingStart = exitMidAngle - minAngularWidth / 2.0;
            var openingEnd = exitMidAngle + minAngularWidth / 2.0;
            
            // Draw the outer circle in two arcs, skipping the exit opening
            // First arc: from opening end to opening start (going the long way)
            DrawArc(gfx, wallPen, centerX, centerY, outermostRadius, openingEnd, openingStart + 2 * Math.PI);
        }
        else
        {
            // No exit defined, draw complete circle
            DrawArc(gfx, wallPen, centerX, centerY, outermostRadius, 0, 2 * Math.PI);
        }
    }
    
    private void DrawRadialLine(XGraphics gfx, XPen pen, double centerX, double centerY,
        double angle, double innerR, double outerR)
    {
        var x1 = centerX + innerR * Math.Cos(angle);
        var y1 = centerY + innerR * Math.Sin(angle);
        var x2 = centerX + outerR * Math.Cos(angle);
        var y2 = centerY + outerR * Math.Sin(angle);
        
        gfx.DrawLine(pen, x1, y1, x2, y2);
    }

    private void DrawPartialArc(XGraphics gfx, XPen pen, double centerX, double centerY,
        double radius, double cellStartAngle, double cellEndAngle, List<Cell> connectedNeighbors, double wallThickness, bool isEntranceOrExit = false)
    {
        if (isEntranceOrExit)
        {
            // For entrances and exits, we don't draw any wall, creating a full opening.
            return;
        }
        
        // Minimum opening size: at least 15 times the wall (line) thickness as required
        var minArcLength = wallThickness * 15.0;
        var minAngularWidth = minArcLength / radius;

        // Get the angular ranges of all passages
        var passages = connectedNeighbors.Select(n =>
        {
            var overlapStart = Math.Max(cellStartAngle, n.AngleStart);
            var overlapEnd = Math.Min(cellEndAngle, n.AngleEnd);
            var overlapMid = (overlapStart + overlapEnd) / 2.0;
            
            // Use the calculated minimum angular width for openings
            var angularWidth = minAngularWidth;
            
            return (start: overlapMid - angularWidth / 2.0, end: overlapMid + angularWidth / 2.0);
            
        }).OrderBy(p => p.start).ToList();

        var currentAngle = cellStartAngle;

        // Draw wall segments between the passages
        foreach (var (passageStart, passageEnd) in passages)
        {
            if (passageStart > currentAngle)
            {
                DrawArc(gfx, pen, centerX, centerY, radius, currentAngle, passageStart);
            }
            currentAngle = passageEnd;
        }

        // Draw the final wall segment after the last passage
        if (currentAngle < cellEndAngle)
        {
            DrawArc(gfx, pen, centerX, centerY, radius, currentAngle, cellEndAngle);
        }
    }
    
    private void DrawArc(XGraphics gfx, XPen pen, double centerX, double centerY,
        double radius, double angleStart, double angleEnd)
    {
        // PDFsharp uses degrees, convert from radians
        var startDeg = angleStart * 180.0 / Math.PI;
        var sweepDeg = (angleEnd - angleStart) * 180.0 / Math.PI;
        
        var diameter = radius * 2;
        var x = centerX - radius;
        var y = centerY - radius;
        
        gfx.DrawArc(pen, x, y, diameter, diameter, startDeg, sweepDeg);
    }
    
    private void DrawSolutionPath(XGraphics gfx, MazeGrid grid)
    {
        if (grid.SolutionPath.Count < 1) return;

        var solutionPen = new XPen(XColors.Red, grid.Configuration.WallThickness * 0.6);
        solutionPen.LineJoin = XLineJoin.Round;
        solutionPen.LineCap = XLineCap.Round;
        var centerX = grid.CenterX;
        var centerY = grid.CenterY;

        // Draw entrance extension
        if (grid.Entrance != null)
        {
            var entranceMidAngle = (grid.Entrance.AngleStart + grid.Entrance.AngleEnd) / 2.0;
            var startRadius = grid.Entrance.RadiusInner - grid.RingWidth * 0.3;
            var endRadius = (grid.SolutionPath[0].RadiusInner + grid.SolutionPath[0].RadiusOuter) / 2.0;

            var x1 = centerX + startRadius * Math.Cos(entranceMidAngle);
            var y1 = centerY + startRadius * Math.Sin(entranceMidAngle);
            var x2 = centerX + endRadius * Math.Cos(entranceMidAngle);
            var y2 = centerY + endRadius * Math.Sin(entranceMidAngle);
            
            gfx.DrawLine(solutionPen, x1, y1, x2, y2);
        }

        // Draw the path using arcs for tangential movement and lines for radial movement
        for (var i = 0; i < grid.SolutionPath.Count - 1; i++)
        {
            var cell1 = grid.SolutionPath[i];
            var cell2 = grid.SolutionPath[i + 1];

            DrawSolutionSegment(gfx, solutionPen, cell1, cell2, centerX, centerY);
        }

        // Draw exit extension
        if (grid.Exit != null)
        {
            var lastCell = grid.SolutionPath[grid.SolutionPath.Count - 1];
            var exitMidAngle = (grid.Exit.AngleStart + grid.Exit.AngleEnd) / 2.0;
            var startRadius = (lastCell.RadiusInner + lastCell.RadiusOuter) / 2.0;
            var endRadius = grid.Exit.RadiusOuter + grid.RingWidth * 0.3;

            var x1 = centerX + startRadius * Math.Cos(exitMidAngle);
            var y1 = centerY + startRadius * Math.Sin(exitMidAngle);
            var x2 = centerX + endRadius * Math.Cos(exitMidAngle);
            var y2 = centerY + endRadius * Math.Sin(exitMidAngle);

            gfx.DrawLine(solutionPen, x1, y1, x2, y2);
        }
    }
    
    private void DrawSolutionSegment(XGraphics gfx, XPen pen, Cell cell1, Cell cell2, double centerX, double centerY)
    {
        // Same ring - tangential movement: use ARC drawing for perfect curve
        if (cell1.RingIndex == cell2.RingIndex)
        {
            var midRadius = (cell1.RadiusInner + cell1.RadiusOuter) / 2.0;
            var angle1 = (cell1.AngleStart + cell1.AngleEnd) / 2.0;
            var angle2 = (cell2.AngleStart + cell2.AngleEnd) / 2.0;

            // Calculate angular difference
            var angularDiff = angle2 - angle1;
            while (angularDiff > Math.PI) angularDiff -= 2 * Math.PI;
            while (angularDiff < -Math.PI) angularDiff += 2 * Math.PI;

            // Draw an actual arc (not polyline approximation)
            var startDeg = angle1 * 180.0 / Math.PI;
            var sweepDeg = angularDiff * 180.0 / Math.PI;
            var diameter = midRadius * 2;
            var x = centerX - midRadius;
            var y = centerY - midRadius;

            gfx.DrawArc(pen, x, y, diameter, diameter, startDeg, sweepDeg);
        }
        // Adjacent rings - radial movement: use L-shaped path
        else if (Math.Abs(cell1.RingIndex - cell2.RingIndex) == 1)
        {
            var passageAngleStart = Math.Max(cell1.AngleStart, cell2.AngleStart);
            var passageAngleEnd = Math.Min(cell1.AngleEnd, cell2.AngleEnd);
            var passageMidAngle = (passageAngleStart + passageAngleEnd) / 2.0;

            var cell1MidRadius = (cell1.RadiusInner + cell1.RadiusOuter) / 2.0;
            var cell2MidRadius = (cell2.RadiusInner + cell2.RadiusOuter) / 2.0;

            var cell1MidAngle = (cell1.AngleStart + cell1.AngleEnd) / 2.0;
            var cell2MidAngle = (cell2.AngleStart + cell2.AngleEnd) / 2.0;

            // Point 1: Center of cell1
            var x1 = centerX + cell1MidRadius * Math.Cos(cell1MidAngle);
            var y1 = centerY + cell1MidRadius * Math.Sin(cell1MidAngle);

            // Point 2: Passage point at cell1's mid-radius
            var x2 = centerX + cell1MidRadius * Math.Cos(passageMidAngle);
            var y2 = centerY + cell1MidRadius * Math.Sin(passageMidAngle);

            // Point 3: Passage point at cell2's mid-radius
            var x3 = centerX + cell2MidRadius * Math.Cos(passageMidAngle);
            var y3 = centerY + cell2MidRadius * Math.Sin(passageMidAngle);

            // Point 4: Center of cell2
            var x4 = centerX + cell2MidRadius * Math.Cos(cell2MidAngle);
            var y4 = centerY + cell2MidRadius * Math.Sin(cell2MidAngle);

            // Draw tangential arc in cell1 (if needed)
            var angle1Diff = passageMidAngle - cell1MidAngle;
            while (angle1Diff > Math.PI) angle1Diff -= 2 * Math.PI;
            while (angle1Diff < -Math.PI) angle1Diff += 2 * Math.PI;

            if (Math.Abs(angle1Diff) > 0.001)
            {
                var startDeg1 = cell1MidAngle * 180.0 / Math.PI;
                var sweepDeg1 = angle1Diff * 180.0 / Math.PI;
                var diameter1 = cell1MidRadius * 2;
                gfx.DrawArc(pen, centerX - cell1MidRadius, centerY - cell1MidRadius, diameter1, diameter1, startDeg1, sweepDeg1);
            }

            // Draw radial line through passage
            gfx.DrawLine(pen, x2, y2, x3, y3);

            // Draw tangential arc in cell2 (if needed)
            var angle2Diff = cell2MidAngle - passageMidAngle;
            while (angle2Diff > Math.PI) angle2Diff -= 2 * Math.PI;
            while (angle2Diff < -Math.PI) angle2Diff += 2 * Math.PI;

            if (Math.Abs(angle2Diff) > 0.001)
            {
                var startDeg2 = passageMidAngle * 180.0 / Math.PI;
                var sweepDeg2 = angle2Diff * 180.0 / Math.PI;
                var diameter2 = cell2MidRadius * 2;
                gfx.DrawArc(pen, centerX - cell2MidRadius, centerY - cell2MidRadius, diameter2, diameter2, startDeg2, sweepDeg2);
            }
        }
    }
}
