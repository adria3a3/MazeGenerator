using MazeGenerator.Models;
using MazeGenerator.Services;
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
    }

    public void RenderMazeWithSolutionToPdf(MazeGrid grid, string outputPath)
    {
        using var document = CreatePdfDocument();
        var page = document.Pages[0];
        using var gfx = XGraphics.FromPdfPage(page);

        DrawMaze(gfx, grid, drawSolution: true);

        document.Save(outputPath);
    }

    private PdfDocument CreatePdfDocument()
    {
        var document = new PdfDocument();
        document.Info.Title = "Circular Maze";
        document.Info.Creator = "Circular Maze Generator";

        var page = document.AddPage();
        // A2 size in points (72 points = 1 inch, A2 = 420×594 mm)
        page.Width = XUnit.FromMillimeter(420);
        page.Height = XUnit.FromMillimeter(594);

        return document;
    }

    private void DrawMaze(XGraphics gfx, MazeGrid grid, bool drawSolution)
    {
        var config = grid.Configuration;
        var wallPen = new XPen(XColors.Black, config.WallThickness);
        wallPen.LineCap = XLineCap.Flat;

        DrawMazeWalls(gfx, grid, wallPen);

        if (drawSolution && grid.SolutionPath.Count > 0)
            DrawSolutionPath(gfx, grid);
    }

    private void DrawMazeWalls(XGraphics gfx, MazeGrid grid, XPen wallPen)
    {
        var centerX = grid.CenterX;
        var centerY = grid.CenterY;
        var wallThickness = grid.Configuration.WallThickness;

        // Draw the outer boundary circle (with exit gap if applicable).
        // The innermost circle is intentionally never drawn — the center is always open.
        DrawBoundaryRings(gfx, grid, wallPen);

        foreach (var cell in grid.GetAllCells())
        {
            var passages = cell.GetPassableNeighbors();

            // --- Tangential walls (between cells in the same ring) ---
            if (cell.CounterClockwiseNeighbor != null && !passages.Contains(cell.CounterClockwiseNeighbor))
            {
                DrawRadialLine(gfx, wallPen, centerX, centerY, cell.AngleStart, cell.RadiusInner, cell.RadiusOuter);
            }

            // --- Inward-facing arc — only for rings other than the innermost ---
            if (cell.RingIndex > 0)
            {
                if (!passages.Any(p => p.RingIndex < cell.RingIndex))
                {
                    DrawArc(gfx, wallPen, centerX, centerY, cell.RadiusInner, cell.AngleStart, cell.AngleEnd);
                }
                else
                {
                    var inwardPassages = passages.Where(p => p.RingIndex < cell.RingIndex).ToList();
                    DrawPartialArc(gfx, wallPen, centerX, centerY, cell.RadiusInner, cell.AngleStart, cell.AngleEnd, inwardPassages, wallThickness);
                }
            }

            // --- Outward-facing arc — only for rings other than the outermost ---
            if (cell.RingIndex < grid.Cells.Count - 1)
            {
                if (!passages.Any(p => p.RingIndex > cell.RingIndex))
                {
                    DrawArc(gfx, wallPen, centerX, centerY, cell.RadiusOuter, cell.AngleStart, cell.AngleEnd);
                }
                else
                {
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

        // The innermost circle is never drawn: the center is always the open goal.

        // Draw the outermost circle with a gap for the exit.
        var outermostRadius = grid.Cells[grid.Cells.Count - 1][0].RadiusOuter;

        if (grid.Exit != null)
        {
            var minAngularWidth = wallThickness * GeometryCalculator.MinOpeningFactor / outermostRadius;
            var exitMidAngle = (grid.Exit.AngleStart + grid.Exit.AngleEnd) / 2.0;
            var openingEnd = exitMidAngle + minAngularWidth / 2.0;

            // Normalize to [0, 2π) so PDFSharp never receives a negative or out-of-range start angle.
            const double twoPi = 2 * Math.PI;
            var arcStart = ((openingEnd % twoPi) + twoPi) % twoPi;
            DrawArc(gfx, wallPen, centerX, centerY, outermostRadius, arcStart, arcStart + 2 * Math.PI - minAngularWidth);
        }
        else
        {
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
        double radius, double cellStartAngle, double cellEndAngle, List<Cell> connectedNeighbors, double wallThickness)
    {
        var minAngularWidth = wallThickness * GeometryCalculator.MinOpeningFactor / radius;

        var passages = connectedNeighbors.Select(n =>
        {
            var nStart = n.AngleStart;
            var nEnd = n.AngleEnd;
            if (nEnd < nStart) nEnd += 2 * Math.PI;

            // Shift the neighbor into the cell's angular frame to handle the 0/2π wrap-around.
            if (nEnd < cellStartAngle) { nStart += 2 * Math.PI; nEnd += 2 * Math.PI; }
            else if (nStart > cellEndAngle) { nStart -= 2 * Math.PI; nEnd -= 2 * Math.PI; }

            var overlapStart = Math.Max(cellStartAngle, nStart);
            var overlapEnd = Math.Min(cellEndAngle, nEnd);
            if (overlapEnd <= overlapStart)
                return (start: cellStartAngle, end: cellStartAngle); // degenerate — no actual overlap

            var overlapMid = (overlapStart + overlapEnd) / 2.0;
            var angularWidth = Math.Max(minAngularWidth, overlapEnd - overlapStart);

            return (start: overlapMid - angularWidth / 2.0, end: overlapMid + angularWidth / 2.0);
        }).Where(p => p.end > p.start).OrderBy(p => p.start).ToList();

        var currentAngle = cellStartAngle;

        foreach (var (passageStart, passageEnd) in passages)
        {
            if (passageStart > currentAngle)
                DrawArc(gfx, pen, centerX, centerY, radius, currentAngle, passageStart);

            currentAngle = passageEnd;
        }

        if (currentAngle < cellEndAngle)
            DrawArc(gfx, pen, centerX, centerY, radius, currentAngle, cellEndAngle);
    }

    private void DrawArc(XGraphics gfx, XPen pen, double centerX, double centerY,
        double radius, double angleStart, double angleEnd)
    {
        var sweepDeg = (angleEnd - angleStart) * 180.0 / Math.PI;
        if (sweepDeg <= 0) return; // Guard: degenerate or inverted arc

        var startDeg = angleStart * 180.0 / Math.PI;
        var diameter = radius * 2;

        gfx.DrawArc(pen, centerX - radius, centerY - radius, diameter, diameter, startDeg, sweepDeg);
    }

    private void DrawSolutionPath(XGraphics gfx, MazeGrid grid)
    {
        if (grid.SolutionPath.Count < 1) return;

        var solutionPen = new XPen(XColors.Red, grid.Configuration.WallThickness * 0.6);
        solutionPen.LineJoin = XLineJoin.Round;
        solutionPen.LineCap = XLineCap.Round;
        var centerX = grid.CenterX;
        var centerY = grid.CenterY;

        // Draw entrance extension: a line from the geometric center to the first cell's midpoint.
        // This works naturally because the innermost circle is never drawn (center always open).
        if (grid.Entrance != null)
        {
            var entranceMidAngle = (grid.Entrance.AngleStart + grid.Entrance.AngleEnd) / 2.0;
            var firstCellMidRadius = (grid.SolutionPath[0].RadiusInner + grid.SolutionPath[0].RadiusOuter) / 2.0;

            gfx.DrawLine(solutionPen,
                centerX, centerY,
                centerX + firstCellMidRadius * Math.Cos(entranceMidAngle),
                centerY + firstCellMidRadius * Math.Sin(entranceMidAngle));
        }

        // Draw the path segment by segment.
        for (var i = 0; i < grid.SolutionPath.Count - 1; i++)
            DrawSolutionSegment(gfx, solutionPen, grid.SolutionPath[i], grid.SolutionPath[i + 1], centerX, centerY);

        // Draw exit extension: from the last cell's midpoint to just outside the outer boundary.
        if (grid.Exit != null)
        {
            var lastCell = grid.SolutionPath[grid.SolutionPath.Count - 1];
            var exitMidAngle = (grid.Exit.AngleStart + grid.Exit.AngleEnd) / 2.0;
            var startRadius = (lastCell.RadiusInner + lastCell.RadiusOuter) / 2.0;
            var endRadius = grid.Exit.RadiusOuter + grid.RingWidth * 0.3;

            gfx.DrawLine(solutionPen,
                centerX + startRadius * Math.Cos(exitMidAngle),
                centerY + startRadius * Math.Sin(exitMidAngle),
                centerX + endRadius * Math.Cos(exitMidAngle),
                centerY + endRadius * Math.Sin(exitMidAngle));
        }
    }

    private void DrawSolutionSegment(XGraphics gfx, XPen pen, Cell cell1, Cell cell2, double centerX, double centerY)
    {
        // Same ring — tangential movement: draw arc at mid-radius
        if (cell1.RingIndex == cell2.RingIndex)
        {
            var midRadius = (cell1.RadiusInner + cell1.RadiusOuter) / 2.0;
            var angle1 = (cell1.AngleStart + cell1.AngleEnd) / 2.0;
            var angle2 = (cell2.AngleStart + cell2.AngleEnd) / 2.0;

            var angularDiff = angle2 - angle1;
            while (angularDiff > Math.PI) angularDiff -= 2 * Math.PI;
            while (angularDiff < -Math.PI) angularDiff += 2 * Math.PI;

            var startDeg = angle1 * 180.0 / Math.PI;
            var sweepDeg = angularDiff * 180.0 / Math.PI;
            var diameter = midRadius * 2;

            gfx.DrawArc(pen, centerX - midRadius, centerY - midRadius, diameter, diameter, startDeg, sweepDeg);
        }
        // Adjacent rings — radial movement: L-shaped arc + line + arc
        else if (Math.Abs(cell1.RingIndex - cell2.RingIndex) == 1)
        {
            // Shift cell2's angular range into cell1's frame to handle the 0/2π wrap-around.
            var s1 = cell1.AngleStart; var e1 = cell1.AngleEnd;
            var s2 = cell2.AngleStart; var e2 = cell2.AngleEnd;
            if (e2 < s1) { s2 += 2 * Math.PI; e2 += 2 * Math.PI; }
            else if (s2 > e1) { s2 -= 2 * Math.PI; e2 -= 2 * Math.PI; }
            var passageMidAngle = (Math.Max(s1, s2) + Math.Min(e1, e2)) / 2.0;

            var cell1MidRadius = (cell1.RadiusInner + cell1.RadiusOuter) / 2.0;
            var cell2MidRadius = (cell2.RadiusInner + cell2.RadiusOuter) / 2.0;
            var cell1MidAngle = (cell1.AngleStart + cell1.AngleEnd) / 2.0;
            var cell2MidAngle = (cell2.AngleStart + cell2.AngleEnd) / 2.0;

            // Tangential arc in cell1
            var angle1Diff = passageMidAngle - cell1MidAngle;
            while (angle1Diff > Math.PI) angle1Diff -= 2 * Math.PI;
            while (angle1Diff < -Math.PI) angle1Diff += 2 * Math.PI;

            if (Math.Abs(angle1Diff) > 0.001)
            {
                var d1 = cell1MidRadius * 2;
                gfx.DrawArc(pen, centerX - cell1MidRadius, centerY - cell1MidRadius, d1, d1,
                    cell1MidAngle * 180.0 / Math.PI, angle1Diff * 180.0 / Math.PI);
            }

            // Radial line through the passage
            gfx.DrawLine(pen,
                centerX + cell1MidRadius * Math.Cos(passageMidAngle),
                centerY + cell1MidRadius * Math.Sin(passageMidAngle),
                centerX + cell2MidRadius * Math.Cos(passageMidAngle),
                centerY + cell2MidRadius * Math.Sin(passageMidAngle));

            // Tangential arc in cell2
            var angle2Diff = cell2MidAngle - passageMidAngle;
            while (angle2Diff > Math.PI) angle2Diff -= 2 * Math.PI;
            while (angle2Diff < -Math.PI) angle2Diff += 2 * Math.PI;

            if (Math.Abs(angle2Diff) > 0.001)
            {
                var d2 = cell2MidRadius * 2;
                gfx.DrawArc(pen, centerX - cell2MidRadius, centerY - cell2MidRadius, d2, d2,
                    passageMidAngle * 180.0 / Math.PI, angle2Diff * 180.0 / Math.PI);
            }
        }
    }
}
