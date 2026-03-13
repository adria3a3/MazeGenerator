using System.Globalization;
using System.Text;
using MazeGenerator.Models;

namespace MazeGenerator.Rendering;

public class SvgMazeRenderer : IMazeRenderer
{
    private readonly MazeWallCalculator _calculator;

    public SvgMazeRenderer(MazeWallCalculator calculator)
    {
        _calculator = calculator;
    }

    public string FileExtension => ".svg";

    public void Render(MazeGrid grid, MazeSolution? solution, string outputPath)
    {
        var commands = _calculator.Calculate(grid, solution);
        var sb = new StringBuilder();
        var ci = CultureInfo.InvariantCulture;

        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{commands.PageWidth.ToString(ci)}\" height=\"{commands.PageHeight.ToString(ci)}\" viewBox=\"0 0 {commands.PageWidth.ToString(ci)} {commands.PageHeight.ToString(ci)}\">");

        // Wall elements
        sb.AppendLine($"<g stroke=\"black\" stroke-width=\"{commands.WallThickness.ToString(ci)}\" fill=\"none\" stroke-linecap=\"butt\">");

        foreach (var line in commands.WallLines)
        {
            sb.AppendLine($"  <line x1=\"{line.X1.ToString(ci)}\" y1=\"{line.Y1.ToString(ci)}\" x2=\"{line.X2.ToString(ci)}\" y2=\"{line.Y2.ToString(ci)}\"/>");
        }

        foreach (var arc in commands.WallArcs)
        {
            var path = ArcToSvgPath(arc);
            if (path != null)
                sb.AppendLine($"  <path d=\"{path}\"/>");
        }

        sb.AppendLine("</g>");

        // Solution elements
        if (commands.SolutionLines.Count > 0 || commands.SolutionArcs.Count > 0)
        {
            sb.AppendLine($"<g stroke=\"red\" stroke-width=\"{commands.SolutionThickness.ToString(ci)}\" fill=\"none\" stroke-linecap=\"round\" stroke-linejoin=\"round\">");

            foreach (var line in commands.SolutionLines)
            {
                sb.AppendLine($"  <line x1=\"{line.X1.ToString(ci)}\" y1=\"{line.Y1.ToString(ci)}\" x2=\"{line.X2.ToString(ci)}\" y2=\"{line.Y2.ToString(ci)}\"/>");
            }

            foreach (var arc in commands.SolutionArcs)
            {
                var path = ArcToSvgPath(arc);
                if (path != null)
                    sb.AppendLine($"  <path d=\"{path}\"/>");
            }

            sb.AppendLine("</g>");
        }

        sb.AppendLine("</svg>");

        File.WriteAllText(outputPath, sb.ToString());
    }

    private static string? ArcToSvgPath(ArcSegment arc)
    {
        var ci = CultureInfo.InvariantCulture;
        var absSweepDeg = Math.Abs(arc.SweepAngleRadians * 180.0 / Math.PI);
        if (absSweepDeg < 0.001) return null;

        // SVG arcs with sweep >= 360° have nearly-identical start/end points,
        // which browsers render as nothing. Split into two half-arcs.
        if (absSweepDeg >= 359.999)
        {
            var half = arc.SweepAngleRadians / 2.0;
            var mid = arc.StartAngleRadians + half;
            var x1 = arc.CenterX + arc.Radius * Math.Cos(arc.StartAngleRadians);
            var y1 = arc.CenterY + arc.Radius * Math.Sin(arc.StartAngleRadians);
            var xm = arc.CenterX + arc.Radius * Math.Cos(mid);
            var ym = arc.CenterY + arc.Radius * Math.Sin(mid);
            var r = arc.Radius.ToString(ci);
            var sf = arc.SweepAngleRadians > 0 ? 1 : 0;
            return $"M {x1.ToString(ci)} {y1.ToString(ci)} A {r} {r} 0 1 {sf} {xm.ToString(ci)} {ym.ToString(ci)} A {r} {r} 0 1 {sf} {x1.ToString(ci)} {y1.ToString(ci)}";
        }

        var startAngle = arc.StartAngleRadians;
        var endAngle = arc.StartAngleRadians + arc.SweepAngleRadians;

        var sx1 = arc.CenterX + arc.Radius * Math.Cos(startAngle);
        var sy1 = arc.CenterY + arc.Radius * Math.Sin(startAngle);
        var sx2 = arc.CenterX + arc.Radius * Math.Cos(endAngle);
        var sy2 = arc.CenterY + arc.Radius * Math.Sin(endAngle);

        var largeArc = absSweepDeg > 180 ? 1 : 0;
        var sweepFlag = arc.SweepAngleRadians > 0 ? 1 : 0;

        return $"M {sx1.ToString(ci)} {sy1.ToString(ci)} A {arc.Radius.ToString(ci)} {arc.Radius.ToString(ci)} 0 {largeArc} {sweepFlag} {sx2.ToString(ci)} {sy2.ToString(ci)}";
    }
}
