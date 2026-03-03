using System.Windows;

namespace Romer.Core;

public sealed record RomerLine(Point Start, Point End, double StrokeThickness = 1.0);
