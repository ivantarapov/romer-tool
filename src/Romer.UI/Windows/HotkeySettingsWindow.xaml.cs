using System.Collections.ObjectModel;
using System.Windows;
using Romer.Core;

namespace Romer.UI.Windows;

public partial class HotkeySettingsWindow : Window
{
    private readonly ObservableCollection<HotkeyRow> _rows;

    public HotkeySettingsWindow(IEnumerable<HotkeyBinding> bindings)
    {
        InitializeComponent();

        _rows = new ObservableCollection<HotkeyRow>(
            bindings
                .OrderBy(b => b.Action)
                .Select(b => new HotkeyRow
                {
                    Action = b.Action,
                    Gesture = HotkeyGestureParser.Format(b.Modifiers, b.Key)
                }));

        BindingsGrid.ItemsSource = _rows;
    }

    public IReadOnlyList<HotkeyBinding>? UpdatedBindings { get; private set; }

    private void Apply_OnClick(object sender, RoutedEventArgs e)
    {
        var candidate = new List<HotkeyBinding>();

        foreach (var row in _rows)
        {
            if (!HotkeyGestureParser.TryParse(row.Gesture, out var mods, out var key))
            {
                MessageBox.Show(this, $"Invalid gesture: {row.Gesture}", "Invalid hotkey", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            candidate.Add(new HotkeyBinding(row.Action, mods, key));
        }

        if (!HotkeyBindingValidator.TryValidate(candidate, out var error))
        {
            MessageBox.Show(this, error, "Invalid hotkeys", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        UpdatedBindings = candidate;
        DialogResult = true;
        Close();
    }

    private void Cancel_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    public sealed class HotkeyRow
    {
        public HotkeyAction Action { get; init; }

        public string Gesture { get; set; } = string.Empty;
    }
}
