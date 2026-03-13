using MazeGenerator.Models;

namespace MazeGenerator.Rendering;

public static class RendererFactory
{
    public static IMazeRenderer Create(OutputFormat format, MazeWallCalculator? calculator = null)
    {
        calculator ??= new MazeWallCalculator();
        return format switch
        {
            OutputFormat.Pdf => new PdfMazeRenderer(calculator),
            OutputFormat.Svg => new SvgMazeRenderer(calculator),
            OutputFormat.Png => new PngMazeRenderer(calculator),
            _ => throw new ArgumentException($"Unknown output format: {format}", nameof(format))
        };
    }
}
