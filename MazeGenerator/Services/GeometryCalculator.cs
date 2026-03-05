namespace MazeGenerator.Services;

public class GeometryCalculator
{
    /// <summary>Minimum opening = wall thickness × this factor.</summary>
    public const double MinOpeningFactor = 15.0;

    public static List<int> CalculateCellCounts(int rings, double innerRadius, double ringWidth, double wallThickness = 2.0)
    {
        var cellCounts = new List<int>();

        // Minimum opening width required (MinOpeningFactor × wall thickness)
        var minOpeningWidth = wallThickness * MinOpeningFactor;
        
        for (var i = 0; i < rings; i++)
        {
            var ringNumber = i + 1; // Ring 1 is innermost
            
            // Calculate mid-radius of this ring
            var midRadius = innerRadius + (ringNumber - 0.5) * ringWidth;
            
            // Calculate circumference at mid-radius
            var circumference = 2 * Math.PI * midRadius;
            
            // Estimate number of cells to fit around this ring
            // Each cell should have arc length approximately equal to ring width
            var rawCount = (int)Math.Round(circumference / ringWidth);
            
            // REQUIREMENT 3: Ensure cell arc width is large enough for openings
            // Cell arc width = circumference / cellCount
            // We need: arcWidth >= 3.0 * minOpeningWidth (to leave room for walls)
            var maxCellsForOpenings = circumference / (3.0 * minOpeningWidth);
            if (rawCount > maxCellsForOpenings)
            {
                rawCount = (int)Math.Floor(maxCellsForOpenings);
            }
            
            // Apply minimum for inner rings
            if (i == 0)
            {
                // Ring 1 should have at least 6 cells for aesthetics
                rawCount = Math.Max(rawCount, 6);
            }
            
            // Apply alignment rules
            var adjustedCount = ApplyAlignmentRules(rawCount, cellCounts, i);
            
            cellCounts.Add(adjustedCount);
        }
        
        return cellCounts;
    }
    
    private static int ApplyAlignmentRules(int rawCount, List<int> previousCounts, int ringIndex)
    {
        // Ring 1 (index 0): Use raw count but ensure it's even and >= 6
        if (ringIndex == 0)
        {
            var count = Math.Max(rawCount, 6);
            // Prefer even numbers for better symmetry
            if (count % 2 == 1)
                count++;
            return count;
        }
        
        // For subsequent rings, try to maintain divisibility relationships
        var prevCount = previousCounts[ringIndex - 1];
        
        // Try to make current count a multiple of previous count
        // Test multipliers: 2x, 1.5x, 1x (same), or slightly different
        int[] multipliers = { 2, 3 }; // Will divide to get 2x, 1.5x
        int[] divisors = { 1, 2 };
        
        var bestCount = rawCount;
        var minDifference = int.MaxValue;
        
        foreach (var mult in multipliers)
        {
            foreach (var div in divisors)
            {
                var candidate = (prevCount * mult) / div;
                var difference = Math.Abs(candidate - rawCount);
                
                // Only consider if within 30% of raw count
                if (difference < rawCount * 0.3 && difference < minDifference)
                {
                    minDifference = difference;
                    bestCount = candidate;
                }
            }
        }
        
        // Also consider the raw count if it's close to a multiple
        if (Math.Abs(rawCount - bestCount) < 3)
        {
            bestCount = rawCount;
        }
        
        // Ensure we're not decreasing (outer rings should have same or more cells)
        bestCount = Math.Max(bestCount, prevCount);
        
        return bestCount;
    }
    
    public static (double startAngle, double endAngle) CalculateCellAngles(int cellIndex, int cellCount)
    {
        // Each cell spans an equal portion of the full circle
        var angleSpan = (2 * Math.PI) / cellCount;
        
        // Start angle for this cell (0 radians = positive X-axis, increases counter-clockwise)
        var startAngle = cellIndex * angleSpan;
        var endAngle = startAngle + angleSpan;
        
        return (startAngle, endAngle);
    }
    
    public static (double innerRadius, double outerRadius) GetRingRadii(int ringIndex, double innerRadius, double ringWidth)
    {
        var ringInner = innerRadius + (ringIndex * ringWidth);
        var ringOuter = ringInner + ringWidth;
        
        return (ringInner, ringOuter);
    }
    
    public static bool ValidateCellCounts(List<int> cellCounts)
    {
        if (cellCounts.Count == 0)
            return false;
        
        // Check that all counts are positive
        if (cellCounts.Any(c => c <= 0))
            return false;
        
        // Check that counts are non-decreasing (outer rings should have >= cells than inner)
        for (var i = 1; i < cellCounts.Count; i++)
        {
            if (cellCounts[i] < cellCounts[i - 1])
            {
                // Allow a decrease only in the outermost ring
                if (i < cellCounts.Count - 1)
                    return false;
            }
        }
        
        return true;
    }
    
    public static bool AnglesOverlap(double start1, double end1, double start2, double end2)
    {
        // This is a simplified check. For a more robust solution, consider the minimum required overlap.
        var overlap = GetAngularOverlap(start1, end1, start2, end2);
        
        // A minimal overlap is required to be considered a true overlap.
        // This avoids issues with floating-point inaccuracies.
        return overlap > 1e-9;
    }

    /// <summary>
    /// Computes the angular overlap between two arcs on a circle.
    /// Precondition: each individual arc span must be less than π radians.
    /// If spans could be larger, more than one of the three period-shifted
    /// comparisons could be simultaneously positive, potentially overestimating the result.
    /// Cell arcs in this codebase are always much smaller than π.
    /// </summary>
    public static double GetAngularOverlap(double start1, double end1, double start2, double end2)
    {
        // Normalize angles to handle wrap-around cases gracefully
        start1 = NormalizeAngle(start1);
        end1 = NormalizeAngle(end1);
        start2 = NormalizeAngle(start2);
        end2 = NormalizeAngle(end2);

        if (end1 < start1) end1 += 2 * Math.PI;
        if (end2 < start2) end2 += 2 * Math.PI;

        // Check all three relative period alignments to handle arcs that straddle the 0/2π boundary.
        const double twoPi = 2 * Math.PI;
        var o1 = Math.Min(end1, end2) - Math.Max(start1, start2);
        var o2 = Math.Min(end1, end2 + twoPi) - Math.Max(start1, start2 + twoPi);
        var o3 = Math.Min(end1 + twoPi, end2) - Math.Max(start1 + twoPi, start2);
        return Math.Max(0.0, Math.Max(o1, Math.Max(o2, o3)));
    }
    
    private static double NormalizeAngle(double angle)
    {
        while (angle < 0)
            angle += 2 * Math.PI;
        while (angle >= 2 * Math.PI)
            angle -= 2 * Math.PI;
        return angle;
    }
}
