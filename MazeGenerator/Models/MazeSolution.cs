namespace MazeGenerator.Models;

public record MazeSolution(Cell Entrance, Cell Exit, List<Cell> Path, double Coverage);
