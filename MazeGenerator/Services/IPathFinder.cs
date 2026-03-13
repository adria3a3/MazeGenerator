using MazeGenerator.Models;

namespace MazeGenerator.Services;

public interface IPathFinder
{
    List<Cell> FindPath(Cell start, Cell end);
    double CalculateCoverage(int pathLength, int totalCells);
    (Cell endpoint1, Cell endpoint2, List<Cell> path) FindDiameter(MazeGrid grid);
}
