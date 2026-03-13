using MazeGenerator.Models;

namespace MazeGenerator.Rendering;

public class MazeWallCalculator
{
    public MazeDrawCommands Calculate(MazeGrid grid, MazeSolution? solution = null)
    {
        var config = grid.Configuration;
        var centerX = grid.CenterX;
        var centerY = grid.CenterY;
        var wallThickness = config.WallThickness;

        var wallLines = new List<LineSegment>();
        var wallArcs = new List<ArcSegment>();

        // Outer boundary ring (with exit gap)
        CalculateBoundaryRing(grid, solution, centerX, centerY, wallThickness, wallArcs);

        // Inner boundary ring (with entrance gap) — prevents accessing all ring-0 cells from center
        CalculateInnerBoundary(grid, solution, centerX, centerY, wallLines, wallArcs);

        var ringWidth = grid.RingWidth;

        // Radial walls (between cells in the same ring)
        foreach (var cell in grid.GetAllCells())
        {
            var passages = cell.GetPassableNeighbors();
            if (cell.CounterClockwiseNeighbor != null && !passages.Contains(cell.CounterClockwiseNeighbor))
            {
                wallLines.Add(CalculateRadialLine(centerX, centerY, cell.AngleStart, cell.RadiusInner, cell.RadiusOuter));
            }
        }

        // Ring boundary arcs — one continuous arc per boundary with gaps for ALL passages.
        // This avoids duplicates and correctly handles passages to non-adjacent cells.
        for (var ringIdx = 0; ringIdx < grid.Cells.Count - 1; ringIdx++)
        {
            var innerRing = grid.Cells[ringIdx];
            var boundaryRadius = innerRing[0].RadiusOuter;

            // Collect all passage gaps crossing this boundary.
            var gaps = new List<(double start, double end)>();
            foreach (var innerCell in innerRing)
            {
                foreach (var outerCell in innerCell.Passages.Where(p => p.RingIndex == ringIdx + 1))
                {
                    var overlapStart = Math.Max(innerCell.AngleStart, outerCell.AngleStart);
                    var overlapEnd = Math.Min(innerCell.AngleEnd, outerCell.AngleEnd);
                    if (overlapEnd <= overlapStart) continue;

                    gaps.Add((overlapStart, overlapEnd));
                }
            }

            // Sort gaps and draw the boundary arc with all gaps
            gaps = gaps.OrderBy(g => g.start).ToList();
            var currentAngle = 0.0;
            foreach (var (gapStart, gapEnd) in gaps)
            {
                if (gapStart > currentAngle)
                    wallArcs.Add(new ArcSegment(centerX, centerY, boundaryRadius, currentAngle, gapStart - currentAngle));
                currentAngle = gapEnd;
            }
            if (currentAngle < 2 * Math.PI)
                wallArcs.Add(new ArcSegment(centerX, centerY, boundaryRadius, currentAngle, 2 * Math.PI - currentAngle));
        }

        // Solution path
        var solutionLines = new List<LineSegment>();
        var solutionArcs = new List<ArcSegment>();

        var solutionPath = solution?.Path;
        if (solutionPath != null && solutionPath.Count > 0)
        {
            var entrance = solution!.Entrance;
            // Entrance extension line: from inner boundary to first cell's midpoint
            var entranceMidAngle = (entrance.AngleStart + entrance.AngleEnd) / 2.0;
            var innerRadius = config.InnerRadius;
            var firstCellMidRadius = (solutionPath[0].RadiusInner + solutionPath[0].RadiusOuter) / 2.0;
            solutionLines.Add(new LineSegment(
                centerX + innerRadius * Math.Cos(entranceMidAngle),
                centerY + innerRadius * Math.Sin(entranceMidAngle),
                centerX + firstCellMidRadius * Math.Cos(entranceMidAngle),
                centerY + firstCellMidRadius * Math.Sin(entranceMidAngle)));

            // Path segments
            for (var i = 0; i < solutionPath.Count - 1; i++)
                CalculateSolutionSegment(solutionLines, solutionArcs, solutionPath[i], solutionPath[i + 1], centerX, centerY);

            // Exit extension line: from last cell's midpoint to outside the outer boundary
            var exit = solution.Exit;
            var lastCell = solutionPath[solutionPath.Count - 1];
            var exitMidAngle = (exit.AngleStart + exit.AngleEnd) / 2.0;
            var exitStartRadius = (lastCell.RadiusInner + lastCell.RadiusOuter) / 2.0;
            var endRadius = exit.RadiusOuter + grid.RingWidth * 0.3;
            solutionLines.Add(new LineSegment(
                centerX + exitStartRadius * Math.Cos(exitMidAngle),
                centerY + exitStartRadius * Math.Sin(exitMidAngle),
                centerX + endRadius * Math.Cos(exitMidAngle),
                centerY + endRadius * Math.Sin(exitMidAngle)));
        }

        return new MazeDrawCommands(
            wallLines, wallArcs, solutionLines, solutionArcs,
            config.PageWidth, config.PageHeight,
            wallThickness, wallThickness * 0.6);
    }

    private void CalculateBoundaryRing(MazeGrid grid, MazeSolution? solution,
        double centerX, double centerY, double wallThickness, List<ArcSegment> arcs)
    {
        var outermostRadius = grid.Cells[grid.Cells.Count - 1][0].RadiusOuter;
        var exitCell = solution?.Exit;

        if (exitCell != null)
        {
            // Gap matches the exit cell's full angular span
            var gapAngularWidth = exitCell.AngleEnd - exitCell.AngleStart;
            var exitMidAngle = (exitCell.AngleStart + exitCell.AngleEnd) / 2.0;
            var openingEnd = exitMidAngle + gapAngularWidth / 2.0;

            const double twoPi = 2 * Math.PI;
            var arcStart = ((openingEnd % twoPi) + twoPi) % twoPi;
            arcs.Add(new ArcSegment(centerX, centerY, outermostRadius, arcStart, twoPi - gapAngularWidth));
        }
        else
        {
            arcs.Add(new ArcSegment(centerX, centerY, outermostRadius, 0, 2 * Math.PI));
        }
    }

    private void CalculateInnerBoundary(MazeGrid grid, MazeSolution? solution,
        double centerX, double centerY,
        List<LineSegment> lines, List<ArcSegment> arcs)
    {
        var innerRadius = grid.Configuration.InnerRadius;
        var entranceCell = solution?.Entrance;

        if (entranceCell != null)
        {
            // Gap matches the entrance cell's angular span exactly,
            // so the gap aligns with the radial walls on either side.
            var gapWidth = entranceCell.AngleEnd - entranceCell.AngleStart;

            const double twoPi = 2 * Math.PI;
            var arcStart = ((entranceCell.AngleEnd % twoPi) + twoPi) % twoPi;
            arcs.Add(new ArcSegment(centerX, centerY, innerRadius, arcStart, twoPi - gapWidth));
        }
        else
        {
            arcs.Add(new ArcSegment(centerX, centerY, innerRadius, 0, 2 * Math.PI));
        }
    }

    private static LineSegment CalculateRadialLine(double centerX, double centerY, double angle, double innerR, double outerR)
    {
        return new LineSegment(
            centerX + innerR * Math.Cos(angle),
            centerY + innerR * Math.Sin(angle),
            centerX + outerR * Math.Cos(angle),
            centerY + outerR * Math.Sin(angle));
    }

    private void CalculateSolutionSegment(List<LineSegment> lines, List<ArcSegment> arcs,
        Cell cell1, Cell cell2, double centerX, double centerY)
    {
        if (cell1.RingIndex == cell2.RingIndex)
        {
            // Same ring: tangential arc
            var midRadius = (cell1.RadiusInner + cell1.RadiusOuter) / 2.0;
            var angle1 = (cell1.AngleStart + cell1.AngleEnd) / 2.0;
            var angle2 = (cell2.AngleStart + cell2.AngleEnd) / 2.0;

            var angularDiff = angle2 - angle1;
            while (angularDiff > Math.PI) angularDiff -= 2 * Math.PI;
            while (angularDiff < -Math.PI) angularDiff += 2 * Math.PI;

            arcs.Add(new ArcSegment(centerX, centerY, midRadius, angle1, angularDiff));
        }
        else if (Math.Abs(cell1.RingIndex - cell2.RingIndex) == 1)
        {
            // Adjacent rings: L-shaped path (arc + radial line + arc).
            var s1 = cell1.AngleStart; var e1 = cell1.AngleEnd;
            var s2 = cell2.AngleStart; var e2 = cell2.AngleEnd;
            if (e2 < s1) { s2 += 2 * Math.PI; e2 += 2 * Math.PI; }
            else if (s2 > e1) { s2 -= 2 * Math.PI; e2 -= 2 * Math.PI; }

            var passageMidAngle = (Math.Max(s1, s2) + Math.Min(e1, e2)) / 2.0;

            var cell1MidRadius = (cell1.RadiusInner + cell1.RadiusOuter) / 2.0;
            var cell2MidRadius = (cell2.RadiusInner + cell2.RadiusOuter) / 2.0;
            var cell1MidAngle = (cell1.AngleStart + cell1.AngleEnd) / 2.0;
            var cell2MidAngle = (cell2.AngleStart + cell2.AngleEnd) / 2.0;

            // Arc in cell1
            var angle1Diff = passageMidAngle - cell1MidAngle;
            while (angle1Diff > Math.PI) angle1Diff -= 2 * Math.PI;
            while (angle1Diff < -Math.PI) angle1Diff += 2 * Math.PI;

            if (Math.Abs(angle1Diff) > 0.001)
                arcs.Add(new ArcSegment(centerX, centerY, cell1MidRadius, cell1MidAngle, angle1Diff));

            // Radial line through the passage
            lines.Add(new LineSegment(
                centerX + cell1MidRadius * Math.Cos(passageMidAngle),
                centerY + cell1MidRadius * Math.Sin(passageMidAngle),
                centerX + cell2MidRadius * Math.Cos(passageMidAngle),
                centerY + cell2MidRadius * Math.Sin(passageMidAngle)));

            // Arc in cell2
            var angle2Diff = cell2MidAngle - passageMidAngle;
            while (angle2Diff > Math.PI) angle2Diff -= 2 * Math.PI;
            while (angle2Diff < -Math.PI) angle2Diff += 2 * Math.PI;

            if (Math.Abs(angle2Diff) > 0.001)
                arcs.Add(new ArcSegment(centerX, centerY, cell2MidRadius, passageMidAngle, angle2Diff));
        }
    }

}
