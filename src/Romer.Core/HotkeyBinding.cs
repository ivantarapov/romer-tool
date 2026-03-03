using System.Windows.Input;

namespace Romer.Core;

public sealed record HotkeyBinding(HotkeyAction Action, ModifierKeys Modifiers, Key Key)
{
    public override string ToString() => HotkeyGestureParser.Format(Modifiers, Key);
}
