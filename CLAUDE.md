# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build MazeGenerator.sln

# Run all tests
dotnet test MazeGenerator.Tests/MazeGenerator.Tests.csproj

# Run a single test class
dotnet test MazeGenerator.Tests/MazeGenerator.Tests.csproj --filter "FullyQualifiedName~MazeGeneratorServiceTests"

# Run a single test method
dotnet test MazeGenerator.Tests/MazeGenerator.Tests.csproj --filter "FullyQualifiedName~GenerateMaze_AllCellsVisited"

# Run the generator (outputs files in the current directory)
dotnet run --project MazeGenerator/MazeGenerator.csproj -- --rings 20 --min-coverage 50

# Publish single-file executable
dotnet publish MazeGenerator/MazeGenerator.csproj
```

## CLI Options

| Flag | Default | Description |
|------|---------|-------------|
| `-r`, `--rings` | 20 | Number of concentric rings (1-100) |
| `-c`, `--min-coverage` | 50 | Minimum solution path coverage % (0-100) |
| `-s`, `--seed` | random | Integer seed for reproducible output |
| `-o`, `--output` | `circular_maze` | Base name for output files |
| `--wall-thickness` | 2.0 | Wall line thickness in points (0.5-10) |
| `--no-solution` | false | Skip solution file generation |
| `-a`, `--algorithm` | `DfsBacktracker` | Algorithm: `DfsBacktracker`, `Prims`, `Kruskals`, `Wilsons` |
| `-f`, `--format` | `Pdf` | Output format: `Pdf`, `Svg`, `Png` |
| `-p`, `--page-size` | `A2` | Page size: `A4`, `A3`, `A2`, `Letter`, `Legal`, `Tabloid` |

Outputs `<name>.<ext>` (maze) and `<name>_solution.<ext>` (maze + red solution line).

## Architecture

The pipeline runs in phases, driven by `CLI/MazeCommand.cs`. Services are resolved via DI (`DI/ServiceRegistration.cs`):

1. **Configuration** — `Models/MazeConfiguration.cs` holds all parameters. `Validate()` returns a list of error strings.
2. **DI setup** — `DI/ServiceRegistration.cs` builds an `IServiceProvider` from the config, wiring up algorithm, path finder, entrance/exit selector, and renderer.
3. **Grid construction** — `MazeGrid` delegates to `Services/GridBuilder.cs`, which calls `GeometryCalculator` to determine cell counts per ring and then establishes all neighbor links.
4. **Maze generation** — One of four algorithms (selected via `IMazeGenerator`):
   - `DfsBacktrackerGenerator` — iterative depth-first search (recursive backtracker)
   - `PrimsGenerator` — randomized Prim's (frontier-based)
   - `KruskalsGenerator` — randomized Kruskal's (shuffle walls + union-find)
   - `WilsonsGenerator` — loop-erased random walk (uniform spanning tree)
   All produce a perfect maze (spanning tree). `MazeGeneratorFactory` instantiates the right one.
5. **Entrance/exit selection** — `Services/EntranceExitSelector` (implements `IEntranceExitSelector`) picks a random entrance on ring 0, uses BFS to find the farthest reachable cell on the outermost ring as exit. Retries up to 10 times if `minCoverage` is not met.
6. **Solution path** — BFS via `PathFinder.FindPath` (implements `IPathFinder`) traces the shortest path. Result stored in `MazeSolution` record on `MazeGrid.Solution`.
7. **Rendering** — `MazeWallCalculator` computes abstract drawing primitives (`LineSegment`, `ArcSegment`). Renderers translate these to their format:
   - `PdfMazeRenderer` — PDFsharp
   - `SvgMazeRenderer` — raw SVG XML (no external deps)
   - `PngMazeRenderer` — SkiaSharp

### Key data structures

- **`Cell`** — polar-coordinate cell with init-only structural properties: `RingIndex`, `CellIndex`, `AngleStart/End` (radians), `RadiusInner/Outer`. Neighbors: `ClockwiseNeighbor`, `CounterClockwiseNeighbor`, `InwardNeighbors`, `OutwardNeighbors`. Passages encapsulated: `IReadOnlyList<Cell> Passages`, mutated via `CreatePassage()` / `ClearPassages()`.
- **`MazeGrid`** — 2D `List<List<Cell>>` (rings x cells). Holds `Solution` (`MazeSolution?`) and layout metrics.
- **`MazeSolution`** — record: `Entrance`, `Exit`, `Path`, `Coverage`.
- **`MazeDrawCommands`** — record containing `WallLines`, `WallArcs`, `SolutionLines`, `SolutionArcs` plus page/thickness metadata.

### Cell count algorithm (`GeometryCalculator.CalculateCellCounts`)

Each ring's cell count is derived from its midpoint circumference divided by `ringWidth`, then adjusted so:
- Passage openings are at least `wallThickness x 15` points wide (`MinOpeningFactor = 15`).
- Outer rings have >= as many cells as the ring inside them.
- Alignment prefers multiples/half-multiples of the previous ring's count for cleaner radial walls.

### Coordinate system

All angles are in **radians**, measured from the positive X-axis, increasing counter-clockwise (standard math convention). PDFsharp receives degrees. Center of the page is the maze center.

## Test structure

Tests mirror the main project's folder layout under `MazeGenerator.Tests/`. A shared `TestGridFactory` creates small initialized/generated grids for unit tests. Algorithm tests use a shared `MazeAlgorithmTestBase` base class. The test framework is xUnit with Moq.
