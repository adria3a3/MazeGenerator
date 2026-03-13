using MazeGenerator.Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace MazeGenerator.Rendering;

public class PdfMazeRenderer : IMazeRenderer
{
    private readonly MazeWallCalculator _calculator;

    public PdfMazeRenderer(MazeWallCalculator calculator)
    {
        _calculator = calculator;
    }

    public string FileExtension => ".pdf";

    public void Render(MazeGrid grid, MazeSolution? solution, string outputPath)
    {
        var commands = _calculator.Calculate(grid, solution);

        using var document = new PdfDocument();
        document.Info.Title = "Circular Maze";
        document.Info.Creator = "Circular Maze Generator";

        var page = document.AddPage();
        page.Width = XUnit.FromPoint(commands.PageWidth);
        page.Height = XUnit.FromPoint(commands.PageHeight);

        using var gfx = XGraphics.FromPdfPage(page);

        var wallPen = new XPen(XColors.Black, commands.WallThickness);
        wallPen.LineCap = XLineCap.Flat;

        foreach (var line in commands.WallLines)
            gfx.DrawLine(wallPen, line.X1, line.Y1, line.X2, line.Y2);

        foreach (var arc in commands.WallArcs)
            DrawArc(gfx, wallPen, arc);

        if (commands.SolutionLines.Count > 0 || commands.SolutionArcs.Count > 0)
        {
            var solutionPen = new XPen(XColors.Red, commands.SolutionThickness);
            solutionPen.LineJoin = XLineJoin.Round;
            solutionPen.LineCap = XLineCap.Round;

            foreach (var line in commands.SolutionLines)
                gfx.DrawLine(solutionPen, line.X1, line.Y1, line.X2, line.Y2);

            foreach (var arc in commands.SolutionArcs)
                DrawArc(gfx, solutionPen, arc);
        }

        document.Save(outputPath);
    }

    private static void DrawArc(XGraphics gfx, XPen pen, ArcSegment arc)
    {
        var sweepDeg = arc.SweepAngleRadians * 180.0 / Math.PI;
        if (Math.Abs(sweepDeg) < 0.001) return;

        var startDeg = arc.StartAngleRadians * 180.0 / Math.PI;
        var diameter = arc.Radius * 2;

        gfx.DrawArc(pen,
            arc.CenterX - arc.Radius, arc.CenterY - arc.Radius,
            diameter, diameter,
            startDeg, sweepDeg);
    }
}
