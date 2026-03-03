using System.Windows;
using System.Windows.Media;

namespace Romer.Core;

public sealed record TransformState(double X, double Y, double ScaleX, double ScaleY, double RotationDegrees)
{
    public Matrix ToMatrix(Point center)
    {
        var matrix = Matrix.Identity;
        matrix.Translate(-center.X, -center.Y);
        matrix.Scale(ScaleX, ScaleY);
        matrix.Rotate(RotationDegrees);
        matrix.Translate(center.X + X, center.Y + Y);
        return matrix;
    }
}
