using System.Windows.Input;

namespace Romer.Core;

public static class HotkeyGestureParser
{
    public static bool TryParse(string text, out ModifierKeys modifiers, out Key key)
    {
        modifiers = ModifierKeys.None;
        key = Key.None;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var parts = text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
        {
            return false;
        }

        foreach (var part in parts[..^1])
        {
            if (!TryParseModifier(part, out var parsed))
            {
                return false;
            }

            modifiers |= parsed;
        }

        return Enum.TryParse(parts[^1], true, out key) && key != Key.None;
    }

    public static string Format(ModifierKeys modifiers, Key key)
    {
        var tokens = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control))
        {
            tokens.Add("Ctrl");
        }

        if (modifiers.HasFlag(ModifierKeys.Alt))
        {
            tokens.Add("Alt");
        }

        if (modifiers.HasFlag(ModifierKeys.Shift))
        {
            tokens.Add("Shift");
        }

        if (modifiers.HasFlag(ModifierKeys.Windows))
        {
            tokens.Add("Win");
        }

        tokens.Add(key.ToString());
        return string.Join('+', tokens);
    }

    private static bool TryParseModifier(string token, out ModifierKeys modifier)
    {
        modifier = token.ToUpperInvariant() switch
        {
            "CTRL" or "CONTROL" => ModifierKeys.Control,
            "ALT" => ModifierKeys.Alt,
            "SHIFT" => ModifierKeys.Shift,
            "WIN" or "WINDOWS" => ModifierKeys.Windows,
            _ => ModifierKeys.None
        };

        return modifier != ModifierKeys.None;
    }
}
