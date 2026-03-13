namespace MazeGenerator.Models;

public enum PageSizeName
{
    A4,
    A3,
    A2,
    Letter,
    Legal,
    Tabloid
}

public static class PageSize
{
    public static (double Width, double Height) GetDimensions(PageSizeName name)
    {
        return name switch
        {
            PageSizeName.A4 => (595.28, 841.89),       // 210 × 297 mm
            PageSizeName.A3 => (841.89, 1190.55),      // 297 × 420 mm
            PageSizeName.A2 => (1190.55, 1683.78),     // 420 × 594 mm
            PageSizeName.Letter => (612.0, 792.0),      // 8.5 × 11 in
            PageSizeName.Legal => (612.0, 1008.0),      // 8.5 × 14 in
            PageSizeName.Tabloid => (792.0, 1224.0),    // 11 × 17 in
            _ => throw new ArgumentException($"Unknown page size: {name}", nameof(name))
        };
    }
}
