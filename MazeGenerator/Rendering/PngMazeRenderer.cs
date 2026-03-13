using MazeGenerator.Models;
using SkiaSharp;

namespace MazeGenerator.Rendering;

public class PngMazeRenderer : IMazeRenderer
{
    private readonly MazeWallCalculator _calculator;

    public PngMazeRenderer(MazeWallCalculator calculator)
    {
        _calculator = calculator;
    }

    public string FileExtension => ".png";

    public void Render(MazeGrid grid, MazeSolution? solution, string outputPath)
    {
        var commands = _calculator.Calculate(grid, solution);
        var width = (int)Math.Ceiling(commands.PageWidth);
        var height = (int)Math.Ceiling(commands.PageHeight);

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        using var wallPaint = new SKPaint
        {
            Color = SKColors.Black,
            StrokeWidth = (float)commands.WallThickness,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Butt
        };

        foreach (var line in commands.WallLines)
            canvas.DrawLine((float)line.X1, (float)line.Y1, (float)line.X2, (float)line.Y2, wallPaint);

        foreach (var arc in commands.WallArcs)
            DrawArc(canvas, wallPaint, arc);

        if (commands.SolutionLines.Count > 0 || commands.SolutionArcs.Count > 0)
        {
            using var solutionPaint = new SKPaint
            {
                Color = SKColors.Red,
                StrokeWidth = (float)commands.SolutionThickness,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round
            };

            foreach (var line in commands.SolutionLines)
                canvas.DrawLine((float)line.X1, (float)line.Y1, (float)line.X2, (float)line.Y2, solutionPaint);

            foreach (var arc in commands.SolutionArcs)
                DrawArc(canvas, solutionPaint, arc);
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(outputPath);
        data.SaveTo(stream);
    }

    private static void DrawArc(SKCanvas canvas, SKPaint paint, ArcSegment arc)
    {
        var sweepDeg = (float)(arc.SweepAngleRadians * 180.0 / Math.PI);
        if (Math.Abs(sweepDeg) < 0.001f) return;

        var startDeg = (float)(arc.StartAngleRadians * 180.0 / Math.PI);
        var r = (float)arc.Radius;
        var cx = (float)arc.CenterX;
        var cy = (float)arc.CenterY;

        var rect = new SKRect(cx - r, cy - r, cx + r, cy + r);

        using var path = new SKPath();
        path.AddArc(rect, startDeg, sweepDeg);
        canvas.DrawPath(path, paint);
    }
}
