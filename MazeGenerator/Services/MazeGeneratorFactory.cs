using MazeGenerator.Models;

namespace MazeGenerator.Services;

public static class MazeGeneratorFactory
{
    public static IMazeGenerator Create(MazeAlgorithm algorithm, int? seed = null)
    {
        return algorithm switch
        {
            MazeAlgorithm.DfsBacktracker => new DfsBacktrackerGenerator(seed),
            MazeAlgorithm.Prims => new PrimsGenerator(seed),
            MazeAlgorithm.Kruskals => new KruskalsGenerator(seed),
            MazeAlgorithm.Wilsons => new WilsonsGenerator(seed),
            _ => throw new ArgumentException($"Unknown maze algorithm: {algorithm}", nameof(algorithm))
        };
    }
}
