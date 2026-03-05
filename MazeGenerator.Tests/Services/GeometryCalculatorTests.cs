using MazeGenerator.Services;
using Xunit;

namespace MazeGenerator.Tests.Services
{
    public class GeometryCalculatorTests
    {
        [Fact]
        public void CalculateCellCounts_SingleRing_ReturnsNonEmpty()
        {
            var counts = GeometryCalculator.CalculateCellCounts(1, 20.0, 30.0);
            Assert.Single(counts);
            Assert.True(counts[0] > 0);
        }

        [Fact]
        public void CalculateCellCounts_InnerRingMinimumSix()
        {
            // Very small inner radius → raw count would be low, but minimum is 6
            var counts = GeometryCalculator.CalculateCellCounts(1, 1.0, 2.0);
            Assert.True(counts[0] >= 6);
        }

        [Fact]
        public void CalculateCellCounts_InnerRingAlwaysEven()
        {
            // Test multiple configurations to verify inner ring is always even
            for (var innerRadius = 5.0; innerRadius <= 50.0; innerRadius += 5.0)
            {
                var counts = GeometryCalculator.CalculateCellCounts(1, innerRadius, 10.0);
                Assert.True(counts[0] % 2 == 0, $"Inner ring count {counts[0]} is not even for innerRadius={innerRadius}");
            }
        }

        [Fact]
        public void CalculateCellCounts_OuterRingsNonDecreasing()
        {
            var counts = GeometryCalculator.CalculateCellCounts(5, 20.0, 30.0);
            for (var i = 1; i < counts.Count; i++)
            {
                Assert.True(counts[i] >= counts[i - 1],
                    $"Ring {i} count ({counts[i]}) < ring {i - 1} count ({counts[i - 1]})");
            }
        }

        [Fact]
        public void CalculateCellCounts_RespectsMaxCellsForOpenings()
        {
            // Very thick walls should cap the number of cells
            var countsThick = GeometryCalculator.CalculateCellCounts(3, 20.0, 30.0, wallThickness: 8.0);
            var countsThin = GeometryCalculator.CalculateCellCounts(3, 20.0, 30.0, wallThickness: 1.0);

            // Thick walls must result in fewer or equal cells than thin walls in every ring
            for (var i = 0; i < countsThick.Count; i++)
            {
                Assert.True(countsThick[i] <= countsThin[i],
                    $"Ring {i}: thick-wall count ({countsThick[i]}) should be <= thin-wall count ({countsThin[i]})");
            }
        }

        [Fact]
        public void CalculateCellAngles_FirstCell_StartsAtZero()
        {
            var (start, _) = GeometryCalculator.CalculateCellAngles(0, 8);
            Assert.Equal(0.0, start);
        }

        [Fact]
        public void CalculateCellAngles_SpansCorrectAngle()
        {
            var (start, end) = GeometryCalculator.CalculateCellAngles(0, 8);
            var expectedSpan = 2 * Math.PI / 8;
            Assert.Equal(expectedSpan, end - start, 10);
        }

        [Fact]
        public void CalculateCellAngles_LastCellEndsAt2Pi()
        {
            var cellCount = 8;
            var (_, end) = GeometryCalculator.CalculateCellAngles(cellCount - 1, cellCount);
            Assert.Equal(2 * Math.PI, end, 10);
        }

        [Fact]
        public void GetRingRadii_ReturnsCorrectRadii()
        {
            var (inner, outer) = GeometryCalculator.GetRingRadii(2, 20.0, 10.0);
            // ringIndex=2: inner = 20 + 2*10 = 40, outer = 40 + 10 = 50
            Assert.Equal(40.0, inner);
            Assert.Equal(50.0, outer);
        }

        [Fact]
        public void ValidateCellCounts_EmptyList_ReturnsFalse()
        {
            Assert.False(GeometryCalculator.ValidateCellCounts(new List<int>()));
        }

        [Fact]
        public void ValidateCellCounts_ZeroCount_ReturnsFalse()
        {
            Assert.False(GeometryCalculator.ValidateCellCounts(new List<int> { 6, 0, 12 }));
        }

        [Fact]
        public void ValidateCellCounts_DecreasingCounts_ReturnsFalse()
        {
            // Early decrease should fail
            Assert.False(GeometryCalculator.ValidateCellCounts(new List<int> { 12, 6, 12, 24, 24 }));
        }

        [Fact]
        public void ValidateCellCounts_SecondToLastRingDecrease_ReturnsFalse()
        {
            // A decrease in the second-to-last ring must also fail — only the outermost ring is exempt
            Assert.False(GeometryCalculator.ValidateCellCounts(new List<int> { 6, 12, 24, 12, 24 }));
        }

        [Fact]
        public void GetAngularOverlap_WrapsAroundBoundary_ReturnsCorrectOverlap()
        {
            // Arc 1: [5.9, 6.4] straddles 2π (≈6.283), covering [5.9, 6.283] ∪ [0, 0.117].
            // Arc 2: [0.0, 0.5]. Genuine overlap = [0, 0.117] ≈ 0.117 radians.
            var overlap = GeometryCalculator.GetAngularOverlap(5.9, 5.9 + 0.5, 0.0, 0.5);
            Assert.True(overlap > 0.05 && overlap < 0.2, $"Expected overlap ~0.117, got {overlap}");
        }

        [Fact]
        public void AnglesOverlap_OverlappingAngles_ReturnsTrue()
        {
            Assert.True(GeometryCalculator.AnglesOverlap(0, Math.PI / 2, Math.PI / 4, Math.PI));
        }

        [Fact]
        public void AnglesOverlap_NonOverlapping_ReturnsFalse()
        {
            Assert.False(GeometryCalculator.AnglesOverlap(0, Math.PI / 4, Math.PI / 2, Math.PI));
        }

        [Fact]
        public void GetAngularOverlap_IdenticalRanges_ReturnsFullSpan()
        {
            var overlap = GeometryCalculator.GetAngularOverlap(0, Math.PI, 0, Math.PI);
            Assert.Equal(Math.PI, overlap, 10);
        }

        [Fact]
        public void GetAngularOverlap_PartialOverlap_ReturnsCorrectValue()
        {
            // [0, π/2] and [π/4, 3π/4] overlap in [π/4, π/2] = span of π/4
            var overlap = GeometryCalculator.GetAngularOverlap(0, Math.PI / 2, Math.PI / 4, Math.PI * 3 / 4);
            Assert.Equal(Math.PI / 4, overlap, 10);
        }

        [Fact]
        public void GetAngularOverlap_NoOverlap_ReturnsNonPositive()
        {
            var overlap = GeometryCalculator.GetAngularOverlap(0, Math.PI / 4, Math.PI / 2, Math.PI);
            Assert.True(overlap <= 0);
        }

        [Fact]
        public void CalculateCellCounts_OddInnerRingRawCount_ResultIsEven()
        {
            // Large radius + thin walls: capped rawCount comes out as 15 (odd),
            // triggering the count++ branch in ApplyAlignmentRules for ring 0.
            var counts = GeometryCalculator.CalculateCellCounts(1, 50.0, 10.0, wallThickness: 0.5);
            Assert.Single(counts);
            Assert.True(counts[0] % 2 == 0, $"Inner ring count {counts[0]} must be even");
            Assert.True(counts[0] >= 16, "Odd raw count (15) should have been rounded up to 16");
        }

        [Fact]
        public void ValidateCellCounts_DecreaseInOutermostRingOnly_ReturnsTrue()
        {
            // A decrease is permitted only in the last ring; [6, 12, 8] should pass.
            Assert.True(GeometryCalculator.ValidateCellCounts(new List<int> { 6, 12, 8 }));
        }

        [Fact]
        public void GetAngularOverlap_NegativeStartAngle_NormalizesAndComputesCorrectly()
        {
            // After normalization: arc1 becomes [7π/4, 9π/4], arc2 stays [0, π/2].
            // The period-shifted comparison (o2) yields overlap = π/4.
            var overlap = GeometryCalculator.GetAngularOverlap(-Math.PI / 4, Math.PI / 4, 0, Math.PI / 2);
            Assert.Equal(Math.PI / 4, overlap, 10);
        }
    }
}


