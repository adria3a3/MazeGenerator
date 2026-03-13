using MazeGenerator.Models;

namespace MazeGenerator.Services;

public interface IEntranceExitSelector
{
    (Cell entrance, Cell exit) FindOptimalAndCreateOpenings(
        MazeGrid grid,
        IMazeGenerator generator,
        int minCoverage,
        int maxRetries = 10,
        Action<string>? log = null);
}
