using System.Runtime.InteropServices;
using System.Windows;
using Romer.UI.Interop;

namespace Romer.UI.Services;

public sealed class MonitorService
{
    public Rect GetActiveMonitorBoundsDip()
    {
        var foreground = NativeMethods.GetForegroundWindow();
        var monitorHandle = NativeMethods.MonitorFromWindow(foreground, NativeMethods.MonitorDefaultToNearest);

        var info = new NativeMethods.MonitorInfo { Size = Marshal.SizeOf<NativeMethods.MonitorInfo>() };
        if (monitorHandle != IntPtr.Zero && NativeMethods.GetMonitorInfo(monitorHandle, ref info))
        {
            var dpi = foreground != IntPtr.Zero ? NativeMethods.GetDpiForWindow(foreground) : NativeMethods.GetDpiForSystem();
            if (dpi == 0)
            {
                dpi = 96;
            }

            return PixelsToDip(info.Monitor, (int)dpi);
        }

        const int fallbackDpi = 96;
        return PixelsToDip(new NativeMethods.Rect
        {
            Left = 0,
            Top = 0,
            Right = 1920,
            Bottom = 1080
        }, fallbackDpi);
    }

    private static Rect PixelsToDip(NativeMethods.Rect rect, int dpi)
    {
        var scale = dpi / 96.0;
        return new Rect(
            rect.Left / scale,
            rect.Top / scale,
            (rect.Right - rect.Left) / scale,
            (rect.Bottom - rect.Top) / scale);
    }
}
