using System.Windows;
using Romer.App.Runtime;

namespace Romer.App;

public partial class App : System.Windows.Application
{
    private ApplicationController? _controller;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _controller = new ApplicationController(this);
        _controller.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _controller?.Dispose();
        base.OnExit(e);
    }
}
