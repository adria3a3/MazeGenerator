namespace MazeGenerator.Rendering;

public record LineSegment(double X1, double Y1, double X2, double Y2);

public record ArcSegment(double CenterX, double CenterY, double Radius, double StartAngleRadians, double SweepAngleRadians);

public record MazeDrawCommands(
    List<LineSegment> WallLines,
    List<ArcSegment> WallArcs,
    List<LineSegment> SolutionLines,
    List<ArcSegment> SolutionArcs,
    double PageWidth,
    double PageHeight,
    double WallThickness,
    double SolutionThickness);
