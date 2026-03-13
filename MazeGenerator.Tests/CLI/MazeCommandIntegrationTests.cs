using MazeGenerator.CLI;
using Xunit;

namespace MazeGenerator.Tests.CLI
{
    [Collection("IntegrationTests")]
    public class MazeCommandIntegrationTests
    {
        private static string CreateTempDir()
        {
            var dir = Path.Combine(Path.GetTempPath(), $"maze_integration_{Guid.NewGuid():N}");
            Directory.CreateDirectory(dir);
            return dir;
        }

        private static void CleanupDir(string dir)
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        /// <summary>
        /// Runs an action with the working directory temporarily set to <paramref name="dir"/>.
        /// OutputBaseName validation rejects absolute paths, so tests must cd into the temp dir.
        /// </summary>
        private static void RunInDir(string dir, Action action)
        {
            var savedDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(dir);
            try
            {
                action();
            }
            finally
            {
                Directory.SetCurrentDirectory(savedDir);
            }
        }

        [Fact]
        public void Execute_ValidArgs_Returns0_AndCreatesPdf()
        {
            var tempDir = CreateTempDir();
            try
            {
                RunInDir(tempDir, () =>
                {
                    var result = MazeCommand.Execute(new[]
                    {
                        "--rings", "3",
                        "--seed", "42",
                        "--min-coverage", "0",
                        "-o", "test_maze"
                    });

                    Assert.Equal(0, result);
                    Assert.True(File.Exists(Path.Combine(tempDir, "test_maze.pdf")));
                });
            }
            finally
            {
                CleanupDir(tempDir);
            }
        }

        [Fact]
        public void Execute_InvalidRings_Returns1()
        {
            var tempDir = CreateTempDir();
            try
            {
                RunInDir(tempDir, () =>
                {
                    var result = MazeCommand.Execute(new[]
                    {
                        "--rings", "0",
                        "-o", "test_maze"
                    });

                    Assert.Equal(1, result);
                    Assert.False(File.Exists(Path.Combine(tempDir, "test_maze.pdf")));
                });
            }
            finally
            {
                CleanupDir(tempDir);
            }
        }

        [Fact]
        public void Execute_NoSolutionFlag_SkipsSolutionPdf()
        {
            var tempDir = CreateTempDir();
            try
            {
                RunInDir(tempDir, () =>
                {
                    var result = MazeCommand.Execute(new[]
                    {
                        "--rings", "3",
                        "--seed", "42",
                        "--min-coverage", "0",
                        "--no-solution",
                        "-o", "test_maze"
                    });

                    Assert.Equal(0, result);
                    Assert.True(File.Exists(Path.Combine(tempDir, "test_maze.pdf")));
                    Assert.False(File.Exists(Path.Combine(tempDir, "test_maze_solution.pdf")));
                });
            }
            finally
            {
                CleanupDir(tempDir);
            }
        }

        [Fact]
        public void Execute_WithSeed_ProducesDeterministicOutput()
        {
            var tempDir = CreateTempDir();
            try
            {
                RunInDir(tempDir, () =>
                {
                    MazeCommand.Execute(new[] { "--rings", "3", "--seed", "123", "--min-coverage", "0", "-o", "maze1" });
                    MazeCommand.Execute(new[] { "--rings", "3", "--seed", "123", "--min-coverage", "0", "-o", "maze2" });

                    var size1 = new FileInfo(Path.Combine(tempDir, "maze1.pdf")).Length;
                    var size2 = new FileInfo(Path.Combine(tempDir, "maze2.pdf")).Length;
                    Assert.Equal(size1, size2);
                });
            }
            finally
            {
                CleanupDir(tempDir);
            }
        }

        [Fact]
        public void Execute_UnparsableArgument_Returns1()
        {
            // CommandLineParser cannot convert "not-a-number" to int -> error handler returns 1.
            var result = MazeCommand.Execute(new[] { "--rings", "not-a-number" });
            Assert.Equal(1, result);
        }

        [Fact]
        public void Execute_PositiveMinCoverage_Succeeds_AndShowsCoverageSatisfiedMessage()
        {
            var tempDir = CreateTempDir();
            try
            {
                RunInDir(tempDir, () =>
                {
                    var result = MazeCommand.Execute(new[]
                    {
                        "--rings", "5",
                        "--seed", "42",
                        "--min-coverage", "30",
                        "-o", "test_maze"
                    });
                    Assert.Equal(0, result);
                    Assert.True(File.Exists(Path.Combine(tempDir, "test_maze.pdf")));
                });
            }
            finally
            {
                CleanupDir(tempDir);
            }
        }

        [Fact]
        public void Execute_OutputWithPathSeparator_Returns1()
        {
            // Path separators in output name are rejected by validation.
            var tempDir = CreateTempDir();
            try
            {
                RunInDir(tempDir, () =>
                {
                    var result = MazeCommand.Execute(new[]
                    {
                        "--rings", "3",
                        "--seed", "42",
                        "--min-coverage", "0",
                        "-o", "no_such_subdir/maze"
                    });
                    Assert.Equal(1, result);
                });
            }
            finally
            {
                CleanupDir(tempDir);
            }
        }

        [Fact]
        public void Execute_InvalidWallThickness_Returns1()
        {
            var tempDir = CreateTempDir();
            try
            {
                RunInDir(tempDir, () =>
                {
                    var result = MazeCommand.Execute(new[]
                    {
                        "--rings", "3",
                        "--wall-thickness", "0.1",
                        "-o", "test_maze"
                    });

                    Assert.Equal(1, result);
                });
            }
            finally
            {
                CleanupDir(tempDir);
            }
        }

        [Fact]
        public void Execute_WithSolution_CreatesBothPdfs()
        {
            var tempDir = CreateTempDir();
            try
            {
                RunInDir(tempDir, () =>
                {
                    var result = MazeCommand.Execute(new[]
                    {
                        "--rings", "3",
                        "--seed", "42",
                        "--min-coverage", "0",
                        "-o", "test_maze"
                    });

                    Assert.Equal(0, result);
                    Assert.True(File.Exists(Path.Combine(tempDir, "test_maze.pdf")));
                    Assert.True(File.Exists(Path.Combine(tempDir, "test_maze_solution.pdf")));
                });
            }
            finally
            {
                CleanupDir(tempDir);
            }
        }

        [Fact]
        public void Execute_AbsoluteOutputPath_Returns1()
        {
            var tempDir = CreateTempDir();
            try
            {
                var absolutePath = Path.Combine(tempDir, "test_maze");
                var result = MazeCommand.Execute(new[]
                {
                    "--rings", "3",
                    "--seed", "42",
                    "--min-coverage", "0",
                    "-o", absolutePath
                });

                Assert.Equal(1, result);
            }
            finally
            {
                CleanupDir(tempDir);
            }
        }
    }
}
