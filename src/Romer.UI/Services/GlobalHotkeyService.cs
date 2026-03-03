using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Romer.Core;
using Romer.UI.Interop;

namespace Romer.UI.Services;

public sealed class GlobalHotkeyService : IHotkeyService
{
    private readonly Dictionary<int, HotkeyBinding> _bindingsById = new();
    private readonly Dictionary<HotkeyAction, HotkeyBinding> _bindingsByAction = new();
    private readonly HwndSource _source;
    private readonly FileLogger _logger;
    private int _nextId = 1;

    public GlobalHotkeyService(Window owner, FileLogger logger)
    {
        _logger = logger;
        var helper = new WindowInteropHelper(owner);
        if (helper.Handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Window handle not available for hotkey registration.");
        }

        _source = HwndSource.FromHwnd(helper.Handle)
            ?? throw new InvalidOperationException("Unable to create HwndSource for hotkey registration.");
        _source.AddHook(WndProc);
    }

    public event EventHandler<HotkeyAction>? HotkeyPressed;

    public IReadOnlyCollection<HotkeyBinding> GetBindings() => _bindingsByAction.Values.ToList();

    public void RegisterDefaultBindings()
    {
        var defaults = new[]
        {
            new HotkeyBinding(HotkeyAction.ToggleOverlay, ModifierKeys.Control | ModifierKeys.Alt, Key.R),
            new HotkeyBinding(HotkeyAction.ToggleClickThrough, ModifierKeys.Control | ModifierKeys.Alt, Key.T),
            new HotkeyBinding(HotkeyAction.ToggleLock, ModifierKeys.Control | ModifierKeys.Alt, Key.L),
            new HotkeyBinding(HotkeyAction.CycleTemplate, ModifierKeys.Control | ModifierKeys.Alt, Key.S),
            new HotkeyBinding(HotkeyAction.QuitApp, ModifierKeys.Control | ModifierKeys.Alt, Key.Q)
        };

        RegisterBindings(defaults, throwOnFailure: false);
    }

    public bool UpdateBinding(HotkeyAction action, HotkeyBinding binding, out string? errorMessage)
    {
        var candidate = _bindingsByAction
            .Where(kv => kv.Key != action)
            .Select(kv => kv.Value)
            .Append(binding)
            .ToList();

        if (!HotkeyBindingValidator.TryValidate(candidate, out errorMessage))
        {
            return false;
        }

        var updated = _bindingsByAction
            .Select(kv => kv.Key == action ? binding : kv.Value)
            .DistinctBy(b => b.Action)
            .ToArray();

        UnregisterAll();
        try
        {
            RegisterBindings(updated, throwOnFailure: true);
            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            _logger.Error("Hotkey update failed", new { action, binding = binding.ToString(), ex.Message });
            UnregisterAll();
            RegisterDefaultBindings();
            return false;
        }
    }

    public void Dispose()
    {
        _source.RemoveHook(WndProc);
        UnregisterAll();
    }

    private void RegisterBindings(IEnumerable<HotkeyBinding> bindings, bool throwOnFailure)
    {
        _bindingsByAction.Clear();
        foreach (var binding in bindings)
        {
            var id = _nextId++;
            var modifiers = ToNativeModifiers(binding.Modifiers);
            var vk = (uint)KeyInterop.VirtualKeyFromKey(binding.Key);
            var ok = NativeMethods.RegisterHotKey(_source.Handle, id, modifiers, vk);
            if (!ok)
            {
                var message = $"Unable to register hotkey {binding}.";
                if (throwOnFailure)
                {
                    throw new InvalidOperationException(message);
                }

                _logger.Error(message);
                continue;
            }

            _bindingsById[id] = binding;
            _bindingsByAction[binding.Action] = binding;
            _logger.Info("Hotkey registered", new { action = binding.Action.ToString(), gesture = binding.ToString() });
        }
    }

    private void UnregisterAll()
    {
        foreach (var id in _bindingsById.Keys)
        {
            _ = NativeMethods.UnregisterHotKey(_source.Handle, id);
        }

        _bindingsById.Clear();
        _bindingsByAction.Clear();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != NativeMethods.WmHotKey)
        {
            return IntPtr.Zero;
        }

        var id = wParam.ToInt32();
        if (_bindingsById.TryGetValue(id, out var binding))
        {
            HotkeyPressed?.Invoke(this, binding.Action);
            handled = true;
        }

        return IntPtr.Zero;
    }

    private static uint ToNativeModifiers(ModifierKeys modifiers)
    {
        uint result = 0;

        if (modifiers.HasFlag(ModifierKeys.Alt))
        {
            result |= 0x1;
        }

        if (modifiers.HasFlag(ModifierKeys.Control))
        {
            result |= 0x2;
        }

        if (modifiers.HasFlag(ModifierKeys.Shift))
        {
            result |= 0x4;
        }

        if (modifiers.HasFlag(ModifierKeys.Windows))
        {
            result |= 0x8;
        }

        return result;
    }
}
