using MazeGenerator.Services;

namespace MazeGenerator.Tests.Services
{
    public class WilsonsGeneratorTests : MazeAlgorithmTestBase
    {
        protected override IMazeGenerator CreateGenerator(int? seed = null) =>
            new WilsonsGenerator(seed);
    }
}
