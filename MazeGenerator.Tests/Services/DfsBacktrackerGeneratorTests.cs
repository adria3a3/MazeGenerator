using MazeGenerator.Services;

namespace MazeGenerator.Tests.Services
{
    public class DfsBacktrackerGeneratorTests : MazeAlgorithmTestBase
    {
        protected override IMazeGenerator CreateGenerator(int? seed = null) =>
            new DfsBacktrackerGenerator(seed);
    }
}
