using MazeGenerator.Services;

namespace MazeGenerator.Tests.Services
{
    public class PrimsGeneratorTests : MazeAlgorithmTestBase
    {
        protected override IMazeGenerator CreateGenerator(int? seed = null) =>
            new PrimsGenerator(seed);
    }
}
