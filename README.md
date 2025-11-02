﻿﻿# Circular Maze Generator

A C# console application that generates circular maze puzzles as high-quality PDF files suitable for A3 printing.

## Features

- **Circular Maze Generation**: Creates perfect mazes with configurable concentric rings
- **Smart Pathfinding**: Uses BFS algorithm to find optimal entrance/exit positions
- **PDF Output**: Vector graphics for crisp printing at any size
- **Solution Generation**: Automatically creates both puzzle and solution PDFs
- **Customizable**: Configure ring count, coverage requirements, wall thickness, and more
- **Reproducible**: Seed-based generation for consistent results

## Requirements

- .NET 9.0 or later
- Windows, macOS, or Linux

## Installation

```bash
# Clone or download the project
cd C:\Users\Adri\RiderProjects\MazeGenerator

# Build the project
dotnet build
```

## Usage

### Basic Usage

```bash
# Generate a simple maze with 8 rings
dotnet run --project MazeGenerator -- -r 8 -o my_maze
```

This will create:
- `my_maze.pdf` - The maze puzzle
- `my_maze_solution.pdf` - The maze with solution path

### Advanced Usage

```bash
# Maze with specific coverage requirement (50% minimum)
dotnet run --project MazeGenerator -- -r 10 -c 50 -o complex_maze

# Reproducible maze with seed
dotnet run --project MazeGenerator -- -r 6 -s 123 -o seeded_maze

# Custom wall thickness (default is 2.0)
dotnet run --project MazeGenerator -- -r 8 -w 3.0 -o thick_walls

# Generate puzzle only (no solution)
dotnet run --project MazeGenerator -- -r 8 --no-solution -o puzzle_only
```

### Command-Line Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--rings` | `-r` | Number of concentric rings (3-20) | 8 |
| `--coverage` | `-c` | Minimum path coverage percentage (0-100) | 40 |
| `--output` | `-o` | Output filename (without extension) | `maze` |
| `--seed` | `-s` | Random seed for reproducible generation | Random |
| `--wall-thickness` | `-w` | Wall thickness in mm | 2.0 |
| `--no-solution` | | Skip solution PDF generation | false |
| `--help` | `-h` | Show help information | - |

### Examples

```bash
# Quick test maze
dotnet run --project MazeGenerator -- -r 5 -o test

# Complex maze for printing
dotnet run --project MazeGenerator -- -r 12 -c 60 -w 2.5 -o printable_maze

# Reproducible set of mazes
dotnet run --project MazeGenerator -- -r 8 -s 100 -o maze_100
dotnet run --project MazeGenerator -- -r 8 -s 101 -o maze_101
dotnet run --project MazeGenerator -- -r 8 -s 102 -o maze_102
```

## How It Works

1. **Grid Creation**: Builds a circular grid with the specified number of rings and segments
2. **Maze Generation**: Uses Recursive Backtracking to create a perfect maze
3. **Path Finding**: BFS algorithm finds the longest path for optimal entrance/exit placement
4. **Coverage Check**: Ensures the solution path meets the minimum coverage requirement
5. **PDF Rendering**: Generates high-quality vector graphics on A3 paper (297mm × 420mm)

## Output Format

- **Paper Size**: A3 (297mm × 420mm)
- **Format**: PDF with vector graphics
- **Maze Position**: Centered on page
- **Solution Path**: Red line overlay (in solution PDF)
- **Quality**: Suitable for professional printing

## Project Structure

```
MazeGenerator/
├── MazeGenerator/              # Main application
│   ├── CLI/
│   │   └── MazeCommand.cs      # Command-line interface
│   ├── Models/
│   │   ├── Cell.cs             # Cell representation
│   │   ├── MazeConfiguration.cs # Configuration settings
│   │   ├── MazeGrid.cs         # Grid structure
│   │   └── WallDirection.cs    # Wall directions enum
│   ├── Services/
│   │   ├── GeometryCalculator.cs # Geometric calculations
│   │   ├── GridBuilder.cs      # Grid construction
│   │   ├── MazeGenerator.cs    # Maze generation algorithm
│   │   └── PathFinder.cs       # BFS pathfinding
│   ├── Rendering/
│   │   └── MazeRenderer.cs     # PDF rendering
│   └── Program.cs              # Entry point
├── docs/                       # Development documentation
│   └── README.md               # Documentation index
├── test-outputs/               # Test PDF files
│   └── README.md               # Test outputs guide
├── README.md                   # This file
└── MazeGenerator.sln           # Solution file
```

## Dependencies

- **CommandLineParser**: CLI argument parsing
- **PdfSharp**: PDF generation and vector graphics
- **SkiaSharp**: Additional graphics support

## License

This project is provided as-is for educational and personal use.

## Documentation

For detailed development documentation, see the [docs/](docs/) folder:

### Available Documentation

- **[REQUIREMENTS_VALIDATION_CHECKLIST.md](docs/REQUIREMENTS_VALIDATION_CHECKLIST.md)** - Comprehensive checklist for validating requirements compliance
- **[TEST_PLAN.md](docs/TEST_PLAN.md)** - Test planning and testing strategies
- **Implementation Guides** - Technical details on maze rendering and solution paths
- **Bug Fixes** - History of bugs fixed during development

## Contributing

This is a complete, standalone project. Feel free to fork and modify for your needs.

## Status

✅ **Production Ready** - All core functionality is complete and tested.


