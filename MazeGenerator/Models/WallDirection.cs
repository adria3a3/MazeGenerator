namespace MazeGenerator.Models;

[Flags]
public enum WallDirection
{
    None = 0,
    Clockwise = 1,
    CounterClockwise = 2,
    Inward = 4,
    Outward = 8,
    All = Clockwise | CounterClockwise | Inward | Outward
}

