using System.Windows;
using System.Windows.Interop;
using Romer.Core;
using Romer.UI.Interop;
using Romer.UI.Services;

namespace Romer.UI.Windows;

public partial class OverlayWindow : Window
{
    private readonly MonitorService _monitorService;
    private readonly FileLogger _logger;

    public OverlayWindow(MonitorService monitorService, FileLogger logger)
    {
        _monitorService = monitorService;
        _logger = logger;
        InitializeComponent();
        SourceInitialized += OnSourceInitialized;
        RomerCanvasControl.TransformChanged += (_, state) => TransformChanged?.Invoke(this, state);
        RomerCanvasControl.CursorCoordinateChanged += (_, text) => CursorCoordinateText.Text = $"Grid: {text}";
        RomerCanvasControl.WindowPositionChanged += (_, text) => WindowPositionText.Text = $"Window: {text}";
    }

    public event EventHandler<TransformState>? TransformChanged;

    public TransformState Transform
    {
        get => RomerCanvasControl.TransformState;
        set => RomerCanvasControl.TransformState = value;
    }

    public bool IsLocked
    {
        get => RomerCanvasControl.IsLocked;
        set => RomerCanvasControl.IsLocked = value;
    }

    public RomerTemplate? ActiveTemplate
    {
        get => RomerCanvasControl.RomerTemplate;
        set
        {
            RomerCanvasControl.RomerTemplate = value;
            UpdateStatus();
        }
    }

    public bool IsClickThroughEnabled { get; private set; }

    public void SummonOnActiveMonitor(bool centerTemplate)
    {
        var bounds = _monitorService.GetActiveMonitorBoundsDip();
        Left = bounds.Left;
        Top = bounds.Top;
        Width = bounds.Width;
        Height = bounds.Height;

        if (centerTemplate)
        {
            var centered = new TransformState((bounds.Width - RomerCanvasControl.Width) / 2, (bounds.Height - RomerCanvasControl.Height) / 2, 1.0, 1.0, 0);
            Transform = centered;
        }

        Show();
        RomerCanvasControl.RefreshWindowPosition();
        UpdateStatus();
        _logger.Info("Overlay summoned", new { bounds.Left, bounds.Top, bounds.Width, bounds.Height, centerTemplate });
    }

    public void ToggleClickThrough(bool enabled)
    {
        IsClickThroughEnabled = enabled;
        var hwnd = new WindowInteropHelper(this).Handle;
        var style = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GwlExStyle).ToInt64();

        if (enabled)
        {
            style |= NativeMethods.WsExTransparent;
        }
        else
        {
            style &= ~NativeMethods.WsExTransparent;
        }

        _ = NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GwlExStyle, new IntPtr(style));
        UpdateStatus();
    }

    public void UpdateStatus()
    {
        var templateLabel = ActiveTemplate?.DisplayName ?? "No template";
        var lockLabel = IsLocked ? "LOCKED" : "UNLOCKED";
        var modeLabel = IsClickThroughEnabled ? "CLICK-THROUGH" : "INTERACTIVE";
        StatusText.Text = $"{templateLabel} | {lockLabel} | {modeLabel}";
    }

    public void SetHotkeyHints(string hints)
    {
        HotkeyHintsText.Text = hints;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var style = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GwlExStyle).ToInt64();
        style |= NativeMethods.WsExToolWindow;
        _ = NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GwlExStyle, new IntPtr(style));
    }
}
