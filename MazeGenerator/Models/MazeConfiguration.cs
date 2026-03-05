namespace MazeGenerator.Models;

public class MazeConfiguration
{
    public int Rings { get; init; } = 100;
    
    public int MinCoverage { get; init; } = 30;
    
    public int? Seed { get; init; }
    
    public string OutputBaseName { get; init; } = "circular_maze";
    
    public double WallThickness { get; init; } = 2.0;
    
    public bool NoSolution { get; init; }
    
    public double PageWidth { get; init; } = 1191.0;
    
    public double PageHeight { get; init; } = 1684.0;
    
    public double Margin { get; init; } = 36.0;
    
    public double InnerRadius { get; init; } = 20.0;
    
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (Rings is < 1 or > 100)
            errors.Add("Rings must be between 1 and 100.");

        if (MinCoverage is < 0 or > 100)
            errors.Add("MinCoverage must be between 0 and 100.");

        if (WallThickness is < 0.5 or > 10)
            errors.Add("Wall thickness must be between 0.5 and 10 points.");

        if (string.IsNullOrWhiteSpace(OutputBaseName))
            errors.Add("Output base name cannot be empty.");

        if (!string.IsNullOrWhiteSpace(OutputBaseName) && OutputBaseName.Contains(".."))
            errors.Add("Output base name must not contain directory traversal (..) sequences.");

        if (!string.IsNullOrWhiteSpace(OutputBaseName) && Path.IsPathRooted(OutputBaseName))
            errors.Add("Output base name must be a relative path.");

        if (PageWidth <= 0)
            errors.Add("PageWidth must be positive.");

        if (PageHeight <= 0)
            errors.Add("PageHeight must be positive.");

        if (Margin < 0)
            errors.Add("Margin must be non-negative.");

        if (Margin >= PageWidth / 2.0 || Margin >= PageHeight / 2.0)
            errors.Add("Margin is too large for the page dimensions.");

        if (InnerRadius <= 0)
            errors.Add("InnerRadius must be positive.");

        var usableRadius = Math.Min((PageWidth / 2.0) - Margin, (PageHeight / 2.0) - Margin);
        if (usableRadius > 0 && InnerRadius >= usableRadius)
            errors.Add($"InnerRadius ({InnerRadius}) must be less than the usable radius ({usableRadius:F1}).");

        return errors;
    }
}

