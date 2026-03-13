using MazeGenerator.Rendering;
using Xunit;

namespace MazeGenerator.Tests.Rendering
{
    public class DrawingPrimitivesTests
    {
        [Fact]
        public void LineSegment_RecordConstruction()
        {
            var line = new LineSegment(1.0, 2.0, 3.0, 4.0);
            Assert.Equal(1.0, line.X1);
            Assert.Equal(2.0, line.Y1);
            Assert.Equal(3.0, line.X2);
            Assert.Equal(4.0, line.Y2);
        }

        [Fact]
        public void ArcSegment_RecordConstruction()
        {
            var arc = new ArcSegment(10.0, 20.0, 5.0, 0.5, 1.0);
            Assert.Equal(10.0, arc.CenterX);
            Assert.Equal(20.0, arc.CenterY);
            Assert.Equal(5.0, arc.Radius);
            Assert.Equal(0.5, arc.StartAngleRadians);
            Assert.Equal(1.0, arc.SweepAngleRadians);
        }

        [Fact]
        public void MazeDrawCommands_RecordConstruction()
        {
            var commands = new MazeDrawCommands(
                new List<LineSegment>(), new List<ArcSegment>(),
                new List<LineSegment>(), new List<ArcSegment>(),
                400, 600, 2.0, 1.2);

            Assert.Equal(400, commands.PageWidth);
            Assert.Equal(600, commands.PageHeight);
            Assert.Equal(2.0, commands.WallThickness);
            Assert.Equal(1.2, commands.SolutionThickness);
            Assert.Empty(commands.WallLines);
        }
    }
}
