using System.Windows.Input;

namespace Romer.Core;

public static class HotkeyBindingValidator
{
    public static bool TryValidate(IReadOnlyCollection<HotkeyBinding> bindings, out string? error)
    {
        var duplicate = bindings
            .GroupBy(b => (b.Modifiers, b.Key))
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate is not null)
        {
            error = $"Duplicate hotkey: {HotkeyGestureParser.Format(duplicate.Key.Modifiers, duplicate.Key.Key)}";
            return false;
        }

        foreach (var binding in bindings)
        {
            if (binding.Key is Key.None)
            {
                error = $"Invalid key for action {binding.Action}.";
                return false;
            }

            if (binding.Modifiers == ModifierKeys.None)
            {
                error = $"Action {binding.Action} must include at least one modifier key.";
                return false;
            }
        }

        error = null;
        return true;
    }
}
