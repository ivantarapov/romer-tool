namespace Romer.Core;

public interface IHotkeyService : IDisposable
{
    event EventHandler<HotkeyAction>? HotkeyPressed;

    IReadOnlyCollection<HotkeyBinding> GetBindings();

    void RegisterDefaultBindings();

    bool UpdateBinding(HotkeyAction action, HotkeyBinding binding, out string? errorMessage);
}
