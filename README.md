# Circular Maze Generator

A C# .NET 9.0 command-line application that generates circular mazes with configurable parameters and exports them to PDF format.

## Features

- **Circular Grid Structure**: Generates mazes with concentric rings (1-100 rings)
- **Perfect Maze Generation**: Creates perfect mazes (exactly one path between any two cells)
- **Solution Path Finding**: Automatically finds and marks the optimal solution path
- **Customizable Coverage**: Ensures solution paths meet minimum coverage requirements (0-100%)
- **PDF Export**: Generates high-quality PDF files for both maze and solution
- **Reproducible Generation**: Support for random seeds for consistent maze generation
- **Configurable Appearance**: Adjustable wall thickness and styling options

## Requirements

- .NET 9.0 SDK or later
- Windows, macOS, or Linux

## Dependencies

- CommandLine - Command-line argument parsing
- PdfSharp - PDF generation and rendering
- SkiaSharp - Graphics rendering
- Microsoft.Extensions.DependencyInjection - Dependency injection
- Microsoft.Extensions.Logging - Logging infrastructure

## Installation

1. Clone the repository:
```bash
git clone <repository-url>
cd MazeGenerator
```

2. Build the project:
```bash
dotnet build
```

3. Run the application:
```bash
dotnet run --project MazeGenerator
```

Or build a release version:
```bash
dotnet build -c Release
```

## Usage

### Basic Usage

Generate a default maze (20 rings, 50% coverage):
```bash
MazeGenerator.exe
```

### Command-Line Options

| Option | Short | Default | Description |
|--------|-------|---------|-------------|
| `--rings` | `-r` | 20 | Number of concentric rings in the maze (1-100) |
| `--min-coverage` | `-c` | 50 | Minimum solution path coverage as a percentage (0-100) |
| `--seed` | `-s` | Random | Random seed for reproducible maze generation (optional) |
| `--output` | `-o` | circular_maze | Base name for output files (without extension) |
| `--wall-thickness` | | 2.0 | Wall line thickness in points (0.5-10.0) |
| `--no-solution` | | false | Skip generation of solution file |
| `--braid` | | 0.0 | Probability (0.0-1.0) of removing dead ends to create a braided maze |

### Examples

Generate a maze with 30 rings and 75% minimum coverage:
```bash
MazeGenerator.exe --rings 30 --min-coverage 75
```

Generate a reproducible maze with a specific seed:
```bash
MazeGenerator.exe --seed 12345 --output my_maze
```

Generate a maze without a solution file:
```bash
MazeGenerator.exe --no-solution
```

Generate a braided maze (reduces dead ends):
```bash
MazeGenerator.exe --braid 0.3
```

Customize wall thickness:
```bash
MazeGenerator.exe --wall-thickness 3.5
```

## Output Files

The application generates the following PDF files:

- `{output-name}.pdf` - The maze without solution
- `{output-name}_solution.pdf` - The maze with solution path highlighted (unless `--no-solution` is specified)

## Project Structure

```
MazeGenerator/
├── CLI/
│   └── MazeCommand.cs           # Command-line interface and options
├── Models/
│   ├── Cell.cs                  # Cell representation
│   ├── MazeConfiguration.cs     # Configuration model
│   ├── MazeGrid.cs              # Grid structure
│   └── WallDirection.cs         # Wall direction enumeration
├── Services/
│   ├── GeometryCalculator.cs    # Geometric calculations
│   ├── GridBuilder.cs           # Grid construction logic
│   ├── MazeGenerator.cs         # Maze generation algorithm
│   ├── MazeValidation.cs        # Maze validation utilities
│   └── PathFinder.cs            # Solution path finding
├── Rendering/
│   └── MazeRenderer.cs          # PDF rendering logic
└── Program.cs                   # Application entry point
```

## Algorithm

1. **Grid Initialization**: Creates a circular grid with the specified number of rings
2. **Maze Generation**: Uses a recursive backtracking algorithm to create a perfect maze
3. **Solution Path Finding**: Finds the optimal entrance/exit pair and calculates the solution path
4. **PDF Rendering**: Renders the maze and solution to high-quality PDF files

## License

This project is provided as-is for educational and personal use.

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

