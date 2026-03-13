using MazeGenerator.Models;

namespace MazeGenerator.Rendering;

public interface IMazeRenderer
{
    string FileExtension { get; }
    void Render(MazeGrid grid, MazeSolution? solution, string outputPath);
}
