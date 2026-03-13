using MazeGenerator.Services;

namespace MazeGenerator.Tests.Services
{
    public class KruskalsGeneratorTests : MazeAlgorithmTestBase
    {
        protected override IMazeGenerator CreateGenerator(int? seed = null) =>
            new KruskalsGenerator(seed);
    }
}
