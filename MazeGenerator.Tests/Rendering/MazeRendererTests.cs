﻿
using MazeGenerator.Models;
using MazeGenerator.Rendering;
using MazeGenerator.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace MazeGenerator.Tests.Rendering
{
    public class MazeRendererTests
    {
        private static MazeGrid CreateFullMazeGrid(int rings = 2, int seed = 42) =>
            TestGridFactory.CreateGeneratedGrid(rings, seed);

        private static string TempPdfPath() =>
            Path.Combine(Path.GetTempPath(), $"maze_test_{Guid.NewGuid():N}.pdf");

        [Fact]
        public void RenderMazeToPdf_CreatesOutputFile()
        {
            var renderer = new MazeRenderer();
            var grid = CreateFullMazeGrid();
            var outputPath = TempPdfPath();

            try
            {
                renderer.RenderMazeToPdf(grid, outputPath);

                Assert.True(File.Exists(outputPath));
                Assert.True(new FileInfo(outputPath).Length > 0);
            }
            finally
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
            }
        }

        [Fact]
        public void RenderMazeWithSolutionToPdf_CreatesOutputFile()
        {
            var renderer = new MazeRenderer();
            var grid = CreateFullMazeGrid(rings: 3);

            // Use the production flow so the solution path is genuinely non-empty
            var pathFinder = new PathFinder();
            var generator = new MazeGenerator.Services.MazeGenerator(42);
            var (entrance, exit) = pathFinder.FindOptimalAndCreateOpenings(grid, generator, new Random(42), 0);
            grid.Entrance = entrance;
            grid.Exit = exit;
            grid.SolutionPath = pathFinder.FindPath(entrance, exit);

            Assert.NotEmpty(grid.SolutionPath); // Ensures solution rendering is actually exercised

            var outputPath = TempPdfPath();

            try
            {
                renderer.RenderMazeWithSolutionToPdf(grid, outputPath);

                Assert.True(File.Exists(outputPath));
                Assert.True(new FileInfo(outputPath).Length > 0);
            }
            finally
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
            }
        }

        [Fact]
        public void RenderMazeToPdf_WithEntranceAndExit_DrawsBoundaryOpenings()
        {
            var renderer = new MazeRenderer();
            var grid = CreateFullMazeGrid();

            // Set entrance and exit for boundary opening rendering
            grid.Entrance = grid.Cells[0][0];
            grid.Exit = grid.Cells[grid.Cells.Count - 1][0];
            grid.Exit.IsExit = true;

            var outputPath = TempPdfPath();

            try
            {
                renderer.RenderMazeToPdf(grid, outputPath);

                Assert.True(File.Exists(outputPath));
                Assert.True(new FileInfo(outputPath).Length > 0);
            }
            finally
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
            }
        }

        [Fact]
        public void RenderMazeToPdf_NoEntranceNoExit_DrawsCompleteBoundaries()
        {
            var renderer = new MazeRenderer();
            var grid = CreateFullMazeGrid();

            // Ensure no entrance/exit
            grid.Entrance = null;
            grid.Exit = null;

            var outputPath = TempPdfPath();

            try
            {
                renderer.RenderMazeToPdf(grid, outputPath);

                Assert.True(File.Exists(outputPath));
                Assert.True(new FileInfo(outputPath).Length > 0);
            }
            finally
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
            }
        }

        [Fact]
        public void RenderMazeToPdf_DegeneratePassageOverlap_DoesNotThrow()
        {
            // A cell whose outward passage neighbor has angles near 2π — after the renderer's
            // angle-shift logic the overlap is negative, exercising the degenerate-overlap guard
            // in DrawPartialArc (the passage is silently dropped and the full arc is drawn).
            var config = new MazeConfiguration
            {
                Rings = 2,
                PageWidth = 400,
                PageHeight = 600,
                Margin = 20,
                InnerRadius = 20,
                WallThickness = 2.0
            };
            var grid = new MazeGrid(config);

            // Ring 0, cell 0: angles [0.15, 0.25]
            var innerCell = new Cell
            {
                RingIndex = 0, CellIndex = 0,
                AngleStart = 0.15, AngleEnd = 0.25,
                RadiusInner = 20, RadiusOuter = 50
            };

            // Ring 1, cell 0: angles [6.2, 6.28] — near 2π, won't truly overlap with innerCell
            // after the renderer shifts it by -2π into the frame of innerCell.
            var outerCell = new Cell
            {
                RingIndex = 1, CellIndex = 0,
                AngleStart = 6.2, AngleEnd = 6.28,
                RadiusInner = 50, RadiusOuter = 80
            };

            innerCell.CreatePassage(outerCell);
            outerCell.CreatePassage(innerCell);
            innerCell.OutwardNeighbors.Add(outerCell);
            outerCell.InwardNeighbors.Add(innerCell);

            grid.Cells.Add(new List<Cell> { innerCell });
            grid.Cells.Add(new List<Cell> { outerCell });

            var renderer = new MazeRenderer();
            var outputPath = Path.Combine(Path.GetTempPath(), $"maze_degenerate_{Guid.NewGuid():N}.pdf");
            try
            {
                renderer.RenderMazeToPdf(grid, outputPath);
                Assert.True(File.Exists(outputPath));
            }
            finally
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
            }
        }

        [Fact]
        public void RenderMazeWithSolutionToPdf_EmptySolution_SkipsSolutionDrawing()
        {
            var renderer = new MazeRenderer();
            var grid = CreateFullMazeGrid();

            // Empty solution path — should not draw solution
            grid.SolutionPath = new List<Cell>();

            var outputPath = TempPdfPath();

            try
            {
                renderer.RenderMazeWithSolutionToPdf(grid, outputPath);

                Assert.True(File.Exists(outputPath));
                Assert.True(new FileInfo(outputPath).Length > 0);
            }
            finally
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
            }
        }
    }
}
