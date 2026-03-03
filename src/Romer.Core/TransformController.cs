using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Romer.Core;

public sealed class TransformController : INotifyPropertyChanged
{
    private const double MinScale = 0.2;
    private const double MaxScale = 8.0;

    private double _x;
    private double _y;
    private double _scaleX = 1.0;
    private double _scaleY = 1.0;
    private double _rotationDegrees;

    public event PropertyChangedEventHandler? PropertyChanged;

    public double X
    {
        get => _x;
        set => SetField(ref _x, value);
    }

    public double Y
    {
        get => _y;
        set => SetField(ref _y, value);
    }

    public double ScaleX
    {
        get => _scaleX;
        set => SetField(ref _scaleX, Math.Clamp(value, MinScale, MaxScale));
    }

    public double ScaleY
    {
        get => _scaleY;
        set => SetField(ref _scaleY, Math.Clamp(value, MinScale, MaxScale));
    }

    public double RotationDegrees
    {
        get => _rotationDegrees;
        set => SetField(ref _rotationDegrees, NormalizeDegrees(value));
    }

    public TransformState Snapshot() => new(X, Y, ScaleX, ScaleY, RotationDegrees);

    public void Restore(TransformState state)
    {
        _x = state.X;
        _y = state.Y;
        _scaleX = Math.Clamp(state.ScaleX, MinScale, MaxScale);
        _scaleY = Math.Clamp(state.ScaleY, MinScale, MaxScale);
        _rotationDegrees = NormalizeDegrees(state.RotationDegrees);
        OnPropertyChanged(string.Empty);
    }

    public void Reset()
    {
        _x = 0;
        _y = 0;
        _scaleX = 1.0;
        _scaleY = 1.0;
        _rotationDegrees = 0;
        OnPropertyChanged(string.Empty);
    }

    private bool SetField(ref double field, double value, [CallerMemberName] string? propertyName = null)
    {
        if (Math.Abs(field - value) < 0.0001)
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private static double NormalizeDegrees(double value)
    {
        var normalized = value % 360;
        return normalized < 0 ? normalized + 360 : normalized;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
