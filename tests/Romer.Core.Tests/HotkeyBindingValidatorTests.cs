using System.Windows.Input;
using Romer.Core;

namespace Romer.Core.Tests;

public sealed class HotkeyBindingValidatorTests
{
    [Fact]
    public void RejectsDuplicateBindings()
    {
        var bindings = new[]
        {
            new HotkeyBinding(HotkeyAction.ToggleOverlay, ModifierKeys.Control | ModifierKeys.Alt, Key.R),
            new HotkeyBinding(HotkeyAction.ToggleLock, ModifierKeys.Control | ModifierKeys.Alt, Key.R)
        };

        var valid = HotkeyBindingValidator.TryValidate(bindings, out var error);

        Assert.False(valid);
        Assert.Contains("Duplicate hotkey", error);
    }

    [Fact]
    public void RejectsMissingModifiers()
    {
        var bindings = new[]
        {
            new HotkeyBinding(HotkeyAction.ToggleOverlay, ModifierKeys.None, Key.R)
        };

        var valid = HotkeyBindingValidator.TryValidate(bindings, out var error);

        Assert.False(valid);
        Assert.Contains("must include at least one modifier", error);
    }
}
