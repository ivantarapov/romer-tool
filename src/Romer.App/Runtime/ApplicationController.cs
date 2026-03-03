using System.Drawing;
using System.Windows;
using Forms = System.Windows.Forms;
using Romer.Core;
using Romer.UI.Services;
using Romer.UI.Windows;

namespace Romer.App.Runtime;

public sealed class ApplicationController : IDisposable
{
    private readonly System.Windows.Application _app;
    private readonly FileLogger _logger;
    private readonly TemplateRegistry _templates;
    private readonly OverlayStateController _state;
    private readonly TransformController _transform;
    private readonly OverlayWindow _overlayWindow;
    private readonly GlobalHotkeyService _hotkeys;
    private readonly Forms.NotifyIcon _notifyIcon;

    private bool _hasSummoned;

    public ApplicationController(System.Windows.Application app)
    {
        _app = app;
        _logger = new FileLogger("romer");
        _templates = new TemplateRegistry();

        var defaultTemplate = _templates.GetTemplates().First();
        _state = new OverlayStateController(defaultTemplate.Id);
        _transform = new TransformController();

        _overlayWindow = new OverlayWindow(new MonitorService(), _logger)
        {
            ActiveTemplate = defaultTemplate,
            IsLocked = false
        };

        _overlayWindow.TransformChanged += (_, state) => _transform.Restore(state);

        _overlayWindow.Show();
        _overlayWindow.Hide();

        _hotkeys = new GlobalHotkeyService(_overlayWindow, _logger);
        _hotkeys.HotkeyPressed += OnHotkeyPressed;
        _hotkeys.RegisterDefaultBindings();
        UpdateOverlayHotkeyHints();

        _notifyIcon = BuildTrayIcon();
        _logger.Info("Application started");

        if (_hotkeys.GetBindings().Count < 5)
        {
            _notifyIcon.ShowBalloonTip(
                4000,
                "Romer",
                "Some default hotkeys could not be registered. Open Hotkey Settings from the tray to adjust bindings.",
                Forms.ToolTipIcon.Warning);
        }
    }

    public void Start()
    {
        _notifyIcon.Visible = true;
        SummonOrHide();
    }

    public void Dispose()
    {
        _hotkeys.HotkeyPressed -= OnHotkeyPressed;
        _hotkeys.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _overlayWindow.Close();
        _logger.Info("Application stopped");
    }

    private void OnHotkeyPressed(object? sender, HotkeyAction action)
    {
        _app.Dispatcher.Invoke(() =>
        {
            switch (action)
            {
                case HotkeyAction.ToggleOverlay:
                    SummonOrHide();
                    break;
                case HotkeyAction.ToggleClickThrough:
                    ToggleClickThrough();
                    break;
                case HotkeyAction.ToggleLock:
                    ToggleLock();
                    break;
                case HotkeyAction.CycleTemplate:
                    CycleTemplate();
                    break;
                case HotkeyAction.QuitApp:
                    _app.Shutdown();
                    break;
            }
        });
    }

    private Forms.NotifyIcon BuildTrayIcon()
    {
        var icon = new Forms.NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Romer Overlay",
            Visible = false
        };

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Show / Hide", null, (_, _) => SummonOrHide());
        menu.Items.Add("Lock / Unlock", null, (_, _) => ToggleLock());
        menu.Items.Add("Toggle Click-Through", null, (_, _) => ToggleClickThrough());
        menu.Items.Add("Switch Template", null, (_, _) => CycleTemplate());
        menu.Items.Add("Hotkey Settings", null, (_, _) => ShowHotkeySettings());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Quit", null, (_, _) => _app.Shutdown());
        icon.ContextMenuStrip = menu;
        icon.DoubleClick += (_, _) => SummonOrHide();
        return icon;
    }

    private void SummonOrHide()
    {
        var isVisible = _state.State.IsVisible;
        if (isVisible)
        {
            _overlayWindow.Hide();
            _state.SetVisible(false);
            _logger.Info("Overlay hidden");
            return;
        }

        var centerTemplate = !_hasSummoned;
        if (!centerTemplate)
        {
            _overlayWindow.Transform = _transform.Snapshot();
        }

        _overlayWindow.SummonOnActiveMonitor(centerTemplate);
        _overlayWindow.IsLocked = _state.State.IsLocked;
        _overlayWindow.ToggleClickThrough(_state.State.IsClickThrough);
        _overlayWindow.UpdateStatus();

        _state.SetVisible(true);
        _hasSummoned = true;
    }

    private void ToggleClickThrough()
    {
        var updated = _state.ToggleClickThrough();
        _overlayWindow.ToggleClickThrough(updated.IsClickThrough);
        _overlayWindow.UpdateStatus();
        _logger.Info("Click-through toggled", new { updated.IsClickThrough });
    }

    private void ToggleLock()
    {
        var updated = _state.ToggleLock();
        _overlayWindow.IsLocked = updated.IsLocked;
        _overlayWindow.UpdateStatus();
        _logger.Info("Lock toggled", new { updated.IsLocked });
    }

    private void CycleTemplate()
    {
        var templateList = _templates.GetTemplates();
        var currentId = _state.State.ActiveTemplateId;
        var currentIndex = templateList
            .Select((template, index) => new { template, index })
            .FirstOrDefault(x => x.template.Id == currentId)?.index ?? 0;

        var next = templateList[(currentIndex + 1) % templateList.Count];
        _state.SetTemplate(next.Id);

        _overlayWindow.ActiveTemplate = next;
        _overlayWindow.UpdateStatus();
        _logger.Info("Template switched", new { next.Id, next.DisplayName });
    }

    private void ShowHotkeySettings()
    {
        var dialog = new HotkeySettingsWindow(_hotkeys.GetBindings())
        {
            Owner = _overlayWindow,
            Topmost = true
        };

        var accepted = dialog.ShowDialog();
        if (accepted != true || dialog.UpdatedBindings is null)
        {
            return;
        }

        foreach (var binding in dialog.UpdatedBindings)
        {
            var ok = _hotkeys.UpdateBinding(binding.Action, binding, out var error);
            if (ok)
            {
                continue;
            }

            _logger.Error("Hotkey update rejected", new { binding.Action, Gesture = binding.ToString(), error });
            System.Windows.MessageBox.Show(
                _overlayWindow,
                error ?? "Unable to update one or more hotkeys.",
                "Hotkey update failed",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        UpdateOverlayHotkeyHints();
        _notifyIcon.ShowBalloonTip(2000, "Romer", "Hotkeys updated for this session.", Forms.ToolTipIcon.Info);
    }

    private void UpdateOverlayHotkeyHints()
    {
        var displayOrder = new[]
        {
            HotkeyAction.ToggleOverlay,
            HotkeyAction.ToggleClickThrough,
            HotkeyAction.ToggleLock,
            HotkeyAction.CycleTemplate,
            HotkeyAction.QuitApp
        };

        var bindingsByAction = _hotkeys.GetBindings().ToDictionary(b => b.Action);
        var chunks = new List<string>();
        foreach (var action in displayOrder)
        {
            if (!bindingsByAction.TryGetValue(action, out var binding))
            {
                continue;
            }

            chunks.Add($"{GetActionShortLabel(action)} {binding}");
        }

        _overlayWindow.SetHotkeyHints("Hotkeys: " + string.Join(" | ", chunks));
    }

    private static string GetActionShortLabel(HotkeyAction action) => action switch
    {
        HotkeyAction.ToggleOverlay => "Show/Hide",
        HotkeyAction.ToggleClickThrough => "Click",
        HotkeyAction.ToggleLock => "Lock",
        HotkeyAction.CycleTemplate => "Template",
        HotkeyAction.QuitApp => "Quit",
        _ => action.ToString()
    };
}
