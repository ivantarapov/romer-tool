using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Globalization;
using Romer.Core;

namespace Romer.UI.Controls;

public partial class RomerCanvas : UserControl
{
    private const double BaseCenter = 180;
    private const double GridOffset = 60;
    private const double GridSize = 240;
    private const double GridMin = 49;
    private const double GridMax = 311;
    private const int GridDivisions = 10;
    private const double MinScale = 0.2;
    private const double MaxScale = 8.0;

    private InteractionMode _interactionMode;
    private Point _startPoint;
    private double _startX;
    private double _startY;
    private double _startScaleX;
    private double _startScaleY;
    private double _startRotation;
    private double _startAngle;
    private ResizeHandleKind _activeResizeHandle;
    private Point _resizeDraggedLocalCorner;
    private Point _resizeAnchorLocalCorner;
    private Point _resizeAnchorWorldPoint;

    public RomerCanvas()
    {
        InitializeComponent();
        AddHandler(MouseMoveEvent, new MouseEventHandler(OnMouseMove), true);
        AddHandler(MouseLeftButtonUpEvent, new MouseButtonEventHandler(OnMouseLeftButtonUp), true);
        AddHandler(MouseLeaveEvent, new MouseEventHandler(OnMouseLeave), true);
        AddHandler(MouseWheelEvent, new MouseWheelEventHandler(OnMouseWheel), true);
        Loaded += (_, _) => PublishWindowPosition();
    }

    public event EventHandler<TransformState>? TransformChanged;
    public event EventHandler<string>? CursorCoordinateChanged;
    public event EventHandler<string>? WindowPositionChanged;

    public static readonly DependencyProperty RomerTemplateProperty = DependencyProperty.Register(
        nameof(RomerTemplate),
        typeof(RomerTemplate),
        typeof(RomerCanvas),
        new PropertyMetadata(null, OnTemplateChanged));

    public static readonly DependencyProperty IsLockedProperty = DependencyProperty.Register(
        nameof(IsLocked),
        typeof(bool),
        typeof(RomerCanvas),
        new PropertyMetadata(false, OnLockChanged));

    public RomerTemplate? RomerTemplate
    {
        get => (RomerTemplate?)GetValue(RomerTemplateProperty);
        set => SetValue(RomerTemplateProperty, value);
    }

    public bool IsLocked
    {
        get => (bool)GetValue(IsLockedProperty);
        set => SetValue(IsLockedProperty, value);
    }

    public TransformState TransformState
    {
        get => new(TranslatePart.X, TranslatePart.Y, ScalePart.ScaleX, ScalePart.ScaleY, RotatePart.Angle);
        set
        {
            ScalePart.ScaleX = value.ScaleX;
            ScalePart.ScaleY = value.ScaleY;
            RotatePart.Angle = value.RotationDegrees;
            TranslatePart.X = value.X;
            TranslatePart.Y = value.Y;
            PublishWindowPosition();
        }
    }

    public void RefreshWindowPosition() => PublishWindowPosition();

    private static void OnTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RomerCanvas canvas)
        {
            canvas.RenderTemplate();
        }
    }

    private static void OnLockChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RomerCanvas canvas)
        {
            canvas.LockBadge.Visibility = canvas.IsLocked ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void RenderTemplate()
    {
        if (RomerTemplate is null)
        {
            TemplatePath.Data = Geometry.Empty;
            MarkingsLayer.Children.Clear();
            return;
        }

        var geometryGroup = new GeometryGroup();
        foreach (var line in RomerTemplate.GeometryDefinition)
        {
            var figure = new PathFigure { StartPoint = ToCanvas(line.Start), IsClosed = false, IsFilled = false };
            figure.Segments.Add(new LineSegment(ToCanvas(line.End), true));
            geometryGroup.Children.Add(new PathGeometry([figure]));
        }

        TemplatePath.Data = geometryGroup;
        TemplatePathShadow.Data = geometryGroup;
        RenderAlignmentMarkings();
    }

    private static Point ToCanvas(Point source)
    {
        return new Point(GridOffset + source.X, GridOffset + source.Y);
    }

    private void RenderAlignmentMarkings()
    {
        MarkingsLayer.Children.Clear();

        var step = GridSize / GridDivisions;
        for (var i = 0; i <= GridDivisions; i++)
        {
            var p = GridOffset + (step * i);
            var major = true;
            var tick = major ? 10 : 6;

            MarkingsLayer.Children.Add(new Line
            {
                X1 = p,
                Y1 = GridOffset + GridSize,
                X2 = p,
                Y2 = GridOffset + GridSize + tick,
                Stroke = Brushes.WhiteSmoke,
                StrokeThickness = major ? 1.2 : 0.9
            });

            MarkingsLayer.Children.Add(new Line
            {
                X1 = GridOffset + GridSize,
                Y1 = p,
                X2 = GridOffset + GridSize + tick,
                Y2 = p,
                Stroke = Brushes.WhiteSmoke,
                StrokeThickness = major ? 1.2 : 0.9
            });

            // X increases right-to-left, Y increases bottom-to-top.
            var labelValue = (GridDivisions - i).ToString("00");
            var bottomLabel = CreateMarkingLabel(labelValue, major);
            Canvas.SetLeft(bottomLabel, p - 12);
            Canvas.SetTop(bottomLabel, GridOffset + GridSize + 12);
            MarkingsLayer.Children.Add(bottomLabel);

            var rightLabel = CreateMarkingLabel(labelValue, major);
            Canvas.SetLeft(rightLabel, GridOffset + GridSize + 13);
            Canvas.SetTop(rightLabel, p - 9);
            MarkingsLayer.Children.Add(rightLabel);
        }

        // Add minor ticks halfway between each whole number.
        for (var i = 0; i < GridDivisions; i++)
        {
            var p = GridOffset + (step * i) + (step / 2.0);
            const double minorTick = 5;
            const double minorStroke = 0.8;

            MarkingsLayer.Children.Add(new Line
            {
                X1 = p,
                Y1 = GridOffset + GridSize,
                X2 = p,
                Y2 = GridOffset + GridSize + minorTick,
                Stroke = Brushes.WhiteSmoke,
                StrokeThickness = minorStroke,
                Opacity = 0.85
            });

            MarkingsLayer.Children.Add(new Line
            {
                X1 = GridOffset + GridSize,
                Y1 = p,
                X2 = GridOffset + GridSize + minorTick,
                Y2 = p,
                Stroke = Brushes.WhiteSmoke,
                StrokeThickness = minorStroke,
                Opacity = 0.85
            });
        }

        MarkingsLayer.Children.Add(new Line
        {
            X1 = GridOffset + (GridSize / 2),
            Y1 = GridOffset - 4,
            X2 = GridOffset + (GridSize / 2),
            Y2 = GridOffset + GridSize + 4,
            Stroke = Brushes.WhiteSmoke,
            StrokeThickness = 0.8,
            Opacity = 0.4
        });

        MarkingsLayer.Children.Add(new Line
        {
            X1 = GridOffset - 4,
            Y1 = GridOffset + (GridSize / 2),
            X2 = GridOffset + GridSize + 4,
            Y2 = GridOffset + (GridSize / 2),
            Stroke = Brushes.WhiteSmoke,
            StrokeThickness = 0.8,
            Opacity = 0.4
        });
    }

    private static Border CreateMarkingLabel(string text, bool major)
    {
        return new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(168, 18, 18, 18)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(210, 240, 245, 250)),
            BorderThickness = new Thickness(major ? 0.8 : 0.6),
            CornerRadius = new CornerRadius(2),
            Padding = major ? new Thickness(2, 0, 2, 0) : new Thickness(1.5, 0, 1.5, 0),
            Child = new TextBlock
            {
                Text = text,
                Foreground = Brushes.WhiteSmoke,
                FontSize = major ? 10 : 9,
                FontWeight = major ? FontWeights.SemiBold : FontWeights.Medium
            }
        };
    }

    private void DragSurface_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (IsLocked)
        {
            return;
        }

        StartInteraction(InteractionMode.Move, e.GetPosition(this));
        e.Handled = true;
    }

    private void ResizeHandle_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (IsLocked)
        {
            return;
        }

        _activeResizeHandle = sender switch
        {
            var s when ReferenceEquals(s, TopLeftHandle) => ResizeHandleKind.TopLeft,
            var s when ReferenceEquals(s, TopRightHandle) => ResizeHandleKind.TopRight,
            var s when ReferenceEquals(s, BottomLeftHandle) => ResizeHandleKind.BottomLeft,
            _ => ResizeHandleKind.BottomRight
        };

        StartInteraction(InteractionMode.Resize, e.GetPosition(this));
        (_resizeDraggedLocalCorner, _resizeAnchorLocalCorner) = GetResizeCorners(_activeResizeHandle);
        _resizeAnchorWorldPoint = TransformLocalPoint(
            _resizeAnchorLocalCorner,
            _startScaleX,
            _startScaleY,
            _startRotation,
            _startX,
            _startY);
        e.Handled = true;
    }

    private void RotateHandle_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (IsLocked)
        {
            return;
        }

        StartInteraction(InteractionMode.Rotate, e.GetPosition(this));
        _startAngle = AngleFromCenter(_startPoint);
        e.Handled = true;
    }

    private void StartInteraction(InteractionMode mode, Point position)
    {
        _interactionMode = mode;
        _startPoint = position;
        _startX = TranslatePart.X;
        _startY = TranslatePart.Y;
        _startScaleX = ScalePart.ScaleX;
        _startScaleY = ScalePart.ScaleY;
        _startRotation = RotatePart.Angle;

        CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        PublishCursorCoordinate(e.GetPosition(this));

        if (_interactionMode == InteractionMode.None || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var point = e.GetPosition(this);

        switch (_interactionMode)
        {
            case InteractionMode.Move:
                var delta = point - _startPoint;
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    if (Math.Abs(delta.X) >= Math.Abs(delta.Y))
                    {
                        delta.Y = 0;
                    }
                    else
                    {
                        delta.X = 0;
                    }
                }

                TranslatePart.X = _startX + delta.X;
                TranslatePart.Y = _startY + delta.Y;
                break;
            case InteractionMode.Resize:
                var localDiagonal = new Vector(
                    _resizeDraggedLocalCorner.X - _resizeAnchorLocalCorner.X,
                    _resizeDraggedLocalCorner.Y - _resizeAnchorLocalCorner.Y);
                if (Math.Abs(localDiagonal.X) > 0.0001 && Math.Abs(localDiagonal.Y) > 0.0001)
                {
                    var worldFromAnchor = point - _resizeAnchorWorldPoint;
                    var rotatedWorld = InverseRotate(worldFromAnchor, _startRotation);
                    var diagonalUnit = localDiagonal;
                    diagonalUnit.Normalize();
                    var projectedLength = Vector.Multiply(rotatedWorld, diagonalUnit);
                    var nextUniformScale = Math.Clamp(projectedLength / localDiagonal.Length, MinScale, MaxScale);

                    ScalePart.ScaleX = nextUniformScale;
                    ScalePart.ScaleY = nextUniformScale;

                    var anchorOffsetLocal = new Vector(
                        _resizeAnchorLocalCorner.X - BaseCenter,
                        _resizeAnchorLocalCorner.Y - BaseCenter);

                    var scaledAnchorOffset = new Vector(
                        anchorOffsetLocal.X * nextUniformScale,
                        anchorOffsetLocal.Y * nextUniformScale);

                    var rotatedAnchorOffset = Rotate(scaledAnchorOffset, _startRotation);
                    TranslatePart.X = _resizeAnchorWorldPoint.X - BaseCenter - rotatedAnchorOffset.X;
                    TranslatePart.Y = _resizeAnchorWorldPoint.Y - BaseCenter - rotatedAnchorOffset.Y;
                }

                break;
            case InteractionMode.Rotate:
                var currentAngle = AngleFromCenter(point);
                RotatePart.Angle = NormalizeDegrees(_startRotation + (currentAngle - _startAngle));
                break;
        }

        PublishTransform();
        PublishWindowPosition();
        e.Handled = true;
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        CursorCoordinateChanged?.Invoke(this, "(--; --)");
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (IsLocked || e.Delta == 0)
        {
            return;
        }

        const double notch = 120.0;
        const double stepPerNotch = 1.5;
        var movement = (e.Delta / notch) * stepPerNotch;

        // Side scroll in WPF is typically represented as Shift+wheel; nudge horizontally then.
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            TranslatePart.X += movement;
        }
        else
        {
            // Scroll up nudges romer up (negative Y); scroll down nudges it down.
            TranslatePart.Y -= movement;
        }

        PublishTransform();
        PublishWindowPosition();
        PublishCursorCoordinate(e.GetPosition(this));
        e.Handled = true;
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_interactionMode == InteractionMode.None)
        {
            return;
        }

        _interactionMode = InteractionMode.None;
        _activeResizeHandle = ResizeHandleKind.None;
        ReleaseMouseCapture();
        PublishTransform();
        PublishWindowPosition();
        e.Handled = true;
    }

    private static double NormalizeDegrees(double value)
    {
        var normalized = value % 360;
        return normalized < 0 ? normalized + 360 : normalized;
    }

    private static Vector Rotate(Vector vector, double angleDegrees)
    {
        var radians = angleDegrees * Math.PI / 180.0;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        return new Vector(
            (vector.X * cos) - (vector.Y * sin),
            (vector.X * sin) + (vector.Y * cos));
    }

    private static Vector InverseRotate(Vector vector, double angleDegrees)
        => Rotate(vector, -angleDegrees);

    private static Point TransformLocalPoint(Point localPoint, double scaleX, double scaleY, double rotationDegrees, double translateX, double translateY)
    {
        var offset = new Vector(localPoint.X - BaseCenter, localPoint.Y - BaseCenter);
        var scaled = new Vector(offset.X * scaleX, offset.Y * scaleY);
        var rotated = Rotate(scaled, rotationDegrees);
        return new Point(BaseCenter + rotated.X + translateX, BaseCenter + rotated.Y + translateY);
    }

    private static (Point drag, Point anchor) GetResizeCorners(ResizeHandleKind handle)
    {
        var topLeft = new Point(GridMin, GridMin);
        var topRight = new Point(GridMax, GridMin);
        var bottomLeft = new Point(GridMin, GridMax);
        var bottomRight = new Point(GridMax, GridMax);

        return handle switch
        {
            ResizeHandleKind.TopLeft => (topLeft, bottomRight),
            ResizeHandleKind.TopRight => (topRight, bottomLeft),
            ResizeHandleKind.BottomLeft => (bottomLeft, topRight),
            _ => (bottomRight, topLeft)
        };
    }

    private static double AngleFromCenter(Point point)
    {
        var dx = point.X - BaseCenter;
        var dy = point.Y - BaseCenter;
        return Math.Atan2(dy, dx) * 180 / Math.PI;
    }

    private void PublishTransform()
    {
        TransformChanged?.Invoke(this, TransformState);
    }

    private void PublishWindowPosition()
    {
        var window = Window.GetWindow(this);
        if (window is null || window.ActualWidth <= 1 || window.ActualHeight <= 1)
        {
            WindowPositionChanged?.Invoke(this, "(--; --)");
            return;
        }

        var maxX = Math.Max(1.0, window.ActualWidth - Width);
        var maxY = Math.Max(1.0, window.ActualHeight - Height);

        var xPercent = Math.Clamp((TranslatePart.X / maxX) * 100.0, 0.0, 100.0);
        var yPercent = Math.Clamp((TranslatePart.Y / maxY) * 100.0, 0.0, 100.0);
        var xText = xPercent.ToString("00.00", CultureInfo.InvariantCulture);
        var yText = yPercent.ToString("00.00", CultureInfo.InvariantCulture);
        WindowPositionChanged?.Invoke(this, $"({xText}%, {yText}%)");
    }

    private void PublishCursorCoordinate(Point worldPoint)
    {
        if (!TryGetLocalPoint(worldPoint, out var localPoint))
        {
            CursorCoordinateChanged?.Invoke(this, "(--; --)");
            return;
        }

        var localX = localPoint.X - GridOffset;
        var localY = localPoint.Y - GridOffset;
        if (localX < 0 || localX > GridSize || localY < 0 || localY > GridSize)
        {
            CursorCoordinateChanged?.Invoke(this, "(--; --)");
            return;
        }

        var east = 10.0 - ((localX / GridSize) * 10.0);
        var north = 10.0 - ((localY / GridSize) * 10.0);
        east = Math.Clamp(east, 0.0, 10.0);
        north = Math.Clamp(north, 0.0, 10.0);

        var northText = north.ToString("00.00", CultureInfo.InvariantCulture);
        var eastText = east.ToString("00.00", CultureInfo.InvariantCulture);
        CursorCoordinateChanged?.Invoke(this, $"({northText}N; {eastText}E)");
    }

    private bool TryGetLocalPoint(Point worldPoint, out Point localPoint)
    {
        var matrix = Matrix.Identity;
        matrix.Translate(-BaseCenter, -BaseCenter);
        matrix.Scale(ScalePart.ScaleX, ScalePart.ScaleY);
        matrix.Rotate(RotatePart.Angle);
        matrix.Translate(BaseCenter + TranslatePart.X, BaseCenter + TranslatePart.Y);

        if (!matrix.HasInverse)
        {
            localPoint = default;
            return false;
        }

        matrix.Invert();
        localPoint = matrix.Transform(worldPoint);
        return true;
    }

    private enum InteractionMode
    {
        None,
        Move,
        Resize,
        Rotate
    }

    private enum ResizeHandleKind
    {
        None,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }
}
