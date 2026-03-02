using MazeGenerator.CLI;
using Xunit;

namespace MazeGenerator.Tests.CLI
{
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

        [Fact]
        public void Execute_ValidArgs_Returns0_AndCreatesPdf()
        {
            var tempDir = CreateTempDir();
            var baseName = Path.Combine(tempDir, "test_maze");
            try
            {
                var result = MazeCommand.Execute(new[]
                {
                    "--rings", "3",
                    "--seed", "42",
                    "--min-coverage", "0",
                    "-o", baseName
                });

                Assert.Equal(0, result);
                Assert.True(File.Exists($"{baseName}.pdf"));
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
            var baseName = Path.Combine(tempDir, "test_maze");
            try
            {
                var result = MazeCommand.Execute(new[]
                {
                    "--rings", "0",
                    "-o", baseName
                });

                Assert.Equal(1, result);
                Assert.False(File.Exists($"{baseName}.pdf"));
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
            var baseName = Path.Combine(tempDir, "test_maze");
            try
            {
                var result = MazeCommand.Execute(new[]
                {
                    "--rings", "3",
                    "--seed", "42",
                    "--min-coverage", "0",
                    "--no-solution",
                    "-o", baseName
                });

                Assert.Equal(0, result);
                Assert.True(File.Exists($"{baseName}.pdf"));
                Assert.False(File.Exists($"{baseName}_solution.pdf"));
            }
            finally
            {
                CleanupDir(tempDir);
            }
        }

        [Fact]
        public void Execute_WithSeed_ProducesDeterministicOutput()
        {
            var tempDir1 = CreateTempDir();
            var tempDir2 = CreateTempDir();
            var baseName1 = Path.Combine(tempDir1, "maze1");
            var baseName2 = Path.Combine(tempDir2, "maze2");
            try
            {
                MazeCommand.Execute(new[] { "--rings", "3", "--seed", "123", "--min-coverage", "0", "-o", baseName1 });
                MazeCommand.Execute(new[] { "--rings", "3", "--seed", "123", "--min-coverage", "0", "-o", baseName2 });

                var size1 = new FileInfo($"{baseName1}.pdf").Length;
                var size2 = new FileInfo($"{baseName2}.pdf").Length;
                Assert.Equal(size1, size2);
            }
            finally
            {
                CleanupDir(tempDir1);
                CleanupDir(tempDir2);
            }
        }

        [Fact]
        public void Execute_InvalidWallThickness_Returns1()
        {
            var tempDir = CreateTempDir();
            var baseName = Path.Combine(tempDir, "test_maze");
            try
            {
                var result = MazeCommand.Execute(new[]
                {
                    "--rings", "3",
                    "--wall-thickness", "0.1",
                    "-o", baseName
                });

                Assert.Equal(1, result);
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
            var baseName = Path.Combine(tempDir, "test_maze");
            try
            {
                var result = MazeCommand.Execute(new[]
                {
                    "--rings", "3",
                    "--seed", "42",
                    "--min-coverage", "0",
                    "-o", baseName
                });

                Assert.Equal(0, result);
                Assert.True(File.Exists($"{baseName}.pdf"));
                Assert.True(File.Exists($"{baseName}_solution.pdf"));
            }
            finally
            {
                CleanupDir(tempDir);
            }
        }
    }
}


