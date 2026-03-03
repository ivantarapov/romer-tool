using System.Windows;
using Romer.Core;

namespace Romer.Core.Tests;

public sealed class TransformStateTests
{
    [Fact]
    public void ToMatrix_AppliesTranslationScaleAndRotation()
    {
        var state = new TransformState(20, -10, 2.0, 2.0, 90);
        var center = new Point(100, 100);

        var matrix = state.ToMatrix(center);
        var transformedCenter = matrix.Transform(center);

        Assert.Equal(120, transformedCenter.X, 6);
        Assert.Equal(90, transformedCenter.Y, 6);

        var transformedRight = matrix.Transform(new Point(110, 100));
        Assert.True(transformedRight.Y > transformedCenter.Y);
    }
}
