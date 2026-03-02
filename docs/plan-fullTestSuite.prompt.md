## Plan: Full Test Suite for MazeGenerator Solution (with Integration Tests)

Create a comprehensive test suite of **~88 tests** covering all code paths across every class in the solution. Organize into a matching folder structure under `MazeGenerator.Tests/`. Consolidate the 3 existing duplicated PathFinder test files, and add integration tests for the full CLI pipeline.

---

### File Structure

```
MazeGenerator.Tests/
  Models/
    CellTests.cs                    (NEW — 7 tests)
    MazeConfigurationTests.cs       (NEW — 6 tests)
    MazeGridTests.cs                (NEW — 8 tests)
  Services/
    GeometryCalculatorTests.cs      (NEW — 14 tests)
    GridBuilderTests.cs             (NEW — 5 tests)
    MazeGeneratorTests.cs           (NEW — 6 tests)
    MazeValidationTests.cs          (NEW — 9 tests)
    PathFinderTests.cs              (REPLACE — consolidate + expand to 22 tests)
  Rendering/
    MazeRendererTests.cs            (REPLACE — expand to 5 tests)
  CLI/
    MazeCommandIntegrationTests.cs  (NEW — 6 tests)
```

**Delete** these 3 superseded files after consolidation:
- `PathFinderOptimalPathTests.cs` (root)
- `PathFinderTests.cs` (root)
- `Services/PathFinderTests.cs`

---

### Steps

#### Step 1: Create [Models/CellTests.cs](new) — 7 tests

Test all public members and branches in `Cell.cs`:

| # | Test name | Covers |
|---|-----------|--------|
| 1 | `DefaultPropertyValues_AreCorrect` | All default values: `RingIndex=0`, `Visited=false`, `IsExit=false`, null neighbors |
| 2 | `InwardNeighbors_InitializedEmpty` | Line 25 — list initialized |
| 3 | `OutwardNeighbors_InitializedEmpty` | Line 27 — list initialized |
| 4 | `GetPassableNeighbors_ReturnsPassagesList` | Lines 31–34 — returns `Passages` |
| 5 | `CreatePassage_AddsNeighborToPassages` | Lines 36–42 — add branch |
| 6 | `CreatePassage_DoesNotAddDuplicate` | Line 38 — `Contains` guard returns early |
| 7 | `ToString_ReturnsFormattedString` | Lines 44–47 — `"Cell[R3,C7]"` format |

---

#### Step 2: Create [Models/MazeConfigurationTests.cs](new) — 6 tests

Test all validation branches in `MazeConfiguration.cs`:

| # | Test name | Covers |
|---|-----------|--------|
| 1 | `Validate_ValidConfig_ReturnsNoErrors` | Lines 25–42 — happy path |
| 2 | `Validate_RingsTooLow_ReturnsError` | Line 29 — `Rings < 1` |
| 3 | `Validate_RingsTooHigh_ReturnsError` | Line 29 — `Rings > 100` |
| 4 | `Validate_MinCoverageOutOfRange_ReturnsError` | Line 32 — both `< 0` and `> 100` via `[Theory]` |
| 5 | `Validate_WallThicknessOutOfRange_ReturnsError` | Line 35 — both `< 0.5` and `> 10` via `[Theory]` |
| 6 | `Validate_EmptyOutputBaseName_ReturnsError` | Line 38 — empty/whitespace string |

---

#### Step 3: Create [Models/MazeGridTests.cs](new) — 8 tests

Test constructor calculations and query methods in `MazeGrid.cs`:

| # | Test name | Covers |
|---|-----------|--------|
| 1 | `Constructor_CalculatesCenterX` | Line 32 — `PageWidth / 2.0` |
| 2 | `Constructor_CalculatesCenterY` | Line 33 — `PageHeight / 2.0` |
| 3 | `Constructor_CalculatesUsableRadius_UsesMinOfXY` | Lines 36–38 — landscape page forces min |
| 4 | `Constructor_CalculatesRingWidth` | Line 41 — `(UsableRadius - InnerRadius) / Rings` |
| 5 | `TotalCells_SumsAllRings` | Line 25 — 2 rings with 3+5 cells = 8 |
| 6 | `TotalCells_EmptyGrid_ReturnsZero` | Line 25 — empty `Cells` |
| 7 | `GetExitCell_ReturnsFirstCellWithIsExit` | Lines 54–57 — one cell has `IsExit=true` |
| 8 | `GetExitCell_NoExitCell_ReturnsNull` | Lines 54–57 — no match |

---

#### Step 4: Create [Services/GeometryCalculatorTests.cs](new) — 14 tests

Test all public methods and branches in `GeometryCalculator.cs`:

| # | Test name | Covers |
|---|-----------|--------|
| 1 | `CalculateCellCounts_SingleRing_ReturnsNonEmpty` | Lines 12–48 — basic output |
| 2 | `CalculateCellCounts_InnerRingMinimumSix` | Line 39 — `Math.Max(rawCount, 6)` |
| 3 | `CalculateCellCounts_InnerRingAlwaysEven` | Lines 56–60 — even enforcement |
| 4 | `CalculateCellCounts_OuterRingsNonDecreasing` | Line 97 — `Math.Max(bestCount, prevCount)` |
| 5 | `CalculateCellCounts_RespectsMaxCellsForOpenings` | Lines 29–33 — high wall thickness caps count |
| 6 | `CalculateCellAngles_FirstCell_StartsAtZero` | Lines 102–112 — `cellIndex=0` |
| 7 | `CalculateCellAngles_SpansCorrectAngle` | Lines 105–109 — `2π / count` |
| 8 | `CalculateCellAngles_LastCellEndsAt2Pi` | Lines 105–109 — last cell boundary |
| 9 | `GetRingRadii_ReturnsCorrectRadii` | Lines 114–120 |
| 10 | `ValidateCellCounts_EmptyList_ReturnsFalse` | Lines 123–125 |
| 11 | `ValidateCellCounts_ZeroCount_ReturnsFalse` | Lines 128–129 |
| 12 | `ValidateCellCounts_DecreasingCounts_ReturnsFalse` | Lines 132–140 |
| 13 | `AnglesOverlap_OverlappingAngles_ReturnsTrue` | Lines 145–153 |
| 14 | `AnglesOverlap_NonOverlapping_ReturnsFalse` | Lines 145–153 |

---

#### Step 5: Create [Services/GridBuilderTests.cs](new) — 5 tests

Test grid construction in `GridBuilder.cs`:

| # | Test name | Covers |
|---|-----------|--------|
| 1 | `BuildGrid_CreatesCells_ForAllRings` | Lines 24, 30–67 — correct ring count and cell count |
| 2 | `BuildGrid_EstablishesClockwiseNeighbors_WithWrapAround` | Lines 99–108 — last cell's clockwise = first cell |
| 3 | `BuildGrid_EstablishesInwardNeighbors` | Lines 110–121 — inner ring cells linked |
| 4 | `BuildGrid_EstablishesOutwardNeighbors` | Lines 124–136 — outer ring cells linked |
| 5 | `BuildGrid_CellsHaveCorrectAnglesAndRadii` | Lines 49–58 — geometry values set |

---

#### Step 6: Create [Services/MazeGeneratorTests.cs](new) — 6 tests

Test maze generation in `MazeGenerator.cs`:

| # | Test name | Covers |
|---|-----------|--------|
| 1 | `GenerateMaze_AllCellsVisited` | Lines 33, 51 — every cell `Visited=true` |
| 2 | `GenerateMaze_CreatesSpanningTree_NMinus1Passages` | Lines 48, 93–94 — passage count = totalCells-1 |
| 3 | `GenerateMaze_ResetsStateBeforeGeneration` | Lines 17–27 — clears `Entrance`, `Exit`, `SolutionPath`, passages |
| 4 | `GenerateMaze_DeterministicWithSameSeed` | Lines 9–11 — same seed produces identical maze |
| 5 | `GenerateMaze_DifferentSeeds_ProduceDifferentMazes` | Confirm different seeds differ |
| 6 | `GenerateMaze_BacktracksWhenNoUnvisitedNeighbors` | Lines 54–58 — stack pop path exercised (implicit in any generation) |

Each test creates a `MazeGrid` with `Initialize()`, then calls `GenerateMaze`.

---

#### Step 7: Create [Services/MazeValidationTests.cs](new) — 9 tests

Test all branches in `MazeValidation.cs`:

| # | Test name | Covers |
|---|-----------|--------|
| 1 | `IsPerfectMaze_EmptyGrid_ReturnsFalse` | Lines 9–10 |
| 2 | `IsPerfectMaze_PerfectMaze_ReturnsTrue` | Lines 14–25 — generated via `MazeGenerator` |
| 3 | `IsPerfectMaze_NotAllVisited_ReturnsFalse` | Lines 15–18 — one cell `Visited=false` |
| 4 | `IsPerfectMaze_TooManyPassages_ReturnsFalse` | Lines 22–25 — add extra passage to break N-1 |
| 5 | `IsPerfectMaze_WithCycle_ReturnsFalse` | Line 25 — `HasCycles` returns true |
| 6 | `HasCycles_EmptyGrid_ReturnsFalse` | Lines 30–31 |
| 7 | `HasCycles_TreeGraph_ReturnsFalse` | Lines 28–62 — valid spanning tree |
| 8 | `HasCycles_WithCycle_ReturnsTrue` | Lines 46–47 — triangle of mutual passages |
| 9 | `HasCycles_DisconnectedComponents_NoCycles_ReturnsFalse` | Lines 36–38 — multiple components, none cyclic |

---

#### Step 8: Replace [Services/PathFinderTests.cs] — 22 tests

Consolidate all 3 existing PathFinder test files into one, add missing branches:

**`FindPath`** (4 tests)
| # | Test | Status |
|---|------|--------|
| 1 | `FindPath_PathExists_ReturnsCorrectPath` | Exists |
| 2 | `FindPath_NoPathExists_ReturnsEmptyList` | Exists |
| 3 | `FindPath_EntranceIsExit_ReturnsSingleCell` | Exists |
| 4 | `FindPath_BranchingGraph_ReturnsShortest` | **NEW** — diamond graph, verify BFS picks shortest |

**`SelectEntrance / SelectExit`** (3 tests)
| # | Test | Status |
|---|------|--------|
| 5 | `SelectEntrance_ReturnsFromInnerRing` | Exists |
| 6 | `SelectExit_ReturnsFromOuterRing_AndSetsIsExit` | Exists |
| 7 | `SelectEntrance_DeterministicWithSeed` | **NEW** — same seed → same cell |

**`CalculateCoverage`** (3 tests)
| # | Test | Status |
|---|------|--------|
| 8 | `CalculateCoverage_ReturnsCorrectPercentage` | Exists |
| 9 | `CalculateCoverage_ZeroTotalCells_ReturnsZero` | **NEW** — line 104 |
| 10 | `CalculateCoverage_FullCoverage_Returns100` | **NEW** |

**`FindDiameter`** (2 tests)
| # | Test | Status |
|---|------|--------|
| 11 | `FindDiameter_LinearChain_ReturnsEndpoints` | Exists |
| 12 | `FindDiameter_SingleCell_ReturnsSameEndpoints` | **NEW** |

**`FindOptimalAndCreateOpenings`** (5 tests)
| # | Test | Status |
|---|------|--------|
| 13 | `FindOptimal_SucceedsOnFirstTry` | Exists |
| 14 | `FindOptimal_RegeneratesWhenCoverageTooLow` | Exists |
| 15 | `FindOptimal_Throws_WhenMaxRetriesExhausted` | **NEW** — mock always returns short path → `InvalidOperationException` at line 248 |
| 16 | `FindOptimal_MarksExitIsExit` | **NEW** — verify `exit.IsExit == true` after success |
| 17 | `FindOptimal_CallsGenerateMaze_CorrectNumberOfTimes` | **NEW** — 3 retries, verify mock called exactly 2 times |

**`FindSolution`** (5 tests)
| # | Test | Status |
|---|------|--------|
| 18 | `FindSolution_NullEntrance_ReturnsEmpty` | Exists |
| 19 | `FindSolution_DirectPath_ReturnsCorrectPath` | Exists |
| 20 | `FindSolution_NullExit_ReturnsEmpty` | **NEW** — entrance set, no `IsExit` cell |
| 21 | `FindSolution_DisconnectedExit_ReturnsEmpty` | **NEW** — line 306 |
| 22 | `FindSolution_MultiHopPath_ReturnsCorrectOrder` | **NEW** — 4 cells, verify entrance→…→exit order |

---

#### Step 9: Replace [Rendering/MazeRendererTests.cs] — 5 tests

Test rendering paths in `MazeRenderer.cs`. Each test writes to a temp file and cleans up in `finally`:

| # | Test name | Covers |
|---|-----------|--------|
| 1 | `RenderMazeToPdf_CreatesOutputFile` | Exists — smoke test |
| 2 | `RenderMazeWithSolutionToPdf_CreatesOutputFile` | **NEW** — set `Entrance`, `Exit`, `SolutionPath` → exercises `DrawSolutionPath` (lines 245–294), both tangential and radial `DrawSolutionSegment` branches |
| 3 | `RenderMazeToPdf_WithEntranceAndExit_DrawsBoundaryOpenings` | **NEW** — exercises `DrawBoundaryRings` lines 131–170 (entrance/exit opening arcs) |
| 4 | `RenderMazeToPdf_NoEntranceNoExit_DrawsCompleteBoundaries` | **NEW** — exercises lines 146–148, 168–169 (full circles) |
| 5 | `RenderMazeWithSolutionToPdf_EmptySolution_SkipsSolutionDrawing` | **NEW** — exercises line 57 guard (`SolutionPath.Count == 0`) |

---

#### Step 10: Create [CLI/MazeCommandIntegrationTests.cs](new) — 6 tests

Full pipeline integration tests for `MazeCommand.cs`. Each test calls `MazeCommand.Execute(args)` with a temp output path and validates return code + file existence:

| # | Test name | Covers |
|---|-----------|--------|
| 1 | `Execute_ValidArgs_Returns0_AndCreatesPdf` | Lines 34–196 — full happy path with `--rings 3 --seed 42 -o tempPath` |
| 2 | `Execute_InvalidRings_Returns1` | Lines 55–64 — `--rings 0` triggers validation error |
| 3 | `Execute_NoSolutionFlag_SkipsSolutionPdf` | Lines 116–164 — `--no-solution` flag, verify only maze PDF exists, no solution PDF |
| 4 | `Execute_WithSeed_ProducesDeterministicOutput` | Run twice with same seed, verify both PDFs have identical byte size |
| 5 | `Execute_InvalidWallThickness_Returns1` | Lines 55–64 — `--wall-thickness 0.1` triggers validation |
| 6 | `Execute_WithSolution_CreatesBothPdfs` | Lines 166–181 — verify both `{name}.pdf` and `{name}_solution.pdf` exist |

Each test uses a unique temp directory, cleaned up in `finally`.

---

#### Step 11: Delete superseded files

Remove these 3 files whose tests are now consolidated into `Services/PathFinderTests.cs`:
- `PathFinderOptimalPathTests.cs` (root)
- `PathFinderTests.cs` (root)
- `Services/PathFinderTests.cs` (old version)

---

#### Step 12: Build and run all tests

Run `dotnet test MazeGenerator.sln` from solution root. Fix any compilation errors, then verify all ~88 tests pass.

---

### Summary

| Test file | Tests | New | Existing |
|-----------|-------|-----|----------|
| Models/CellTests.cs | 7 | 7 | 0 |
| Models/MazeConfigurationTests.cs | 6 | 6 | 0 |
| Models/MazeGridTests.cs | 8 | 8 | 0 |
| Services/GeometryCalculatorTests.cs | 14 | 14 | 0 |
| Services/GridBuilderTests.cs | 5 | 5 | 0 |
| Services/MazeGeneratorTests.cs | 6 | 6 | 0 |
| Services/MazeValidationTests.cs | 9 | 9 | 0 |
| Services/PathFinderTests.cs | 22 | 10 | 12 |
| Rendering/MazeRendererTests.cs | 5 | 4 | 1 |
| CLI/MazeCommandIntegrationTests.cs | 6 | 6 | 0 |
| **Total** | **88** | **75** | **13** |

### Further Considerations
1. **`WallDirection` enum**: It's a `[Flags]` enum with no methods — no tests needed unless you want combinatorial flag assertions. *Recommend: skip.*
2. **`Program.cs`**: Just calls `MazeCommand.Execute(args)` — fully covered by the CLI integration tests. *Recommend: skip separate test.*
3. **Test parallelism**: The integration tests write PDFs to disk. Each test should use a unique temp directory (`Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))`) and clean up in `finally` to allow safe parallel execution.

