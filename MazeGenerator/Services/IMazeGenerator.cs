using MazeGenerator.Models;

namespace MazeGenerator.Services
{
    public interface IMazeGenerator
    {
        void GenerateMaze(MazeGrid grid);
    }
}
