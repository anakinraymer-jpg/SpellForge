using System.Windows;
using System.Windows.Controls;
using SpellForge.Models;

namespace SpellForge.Views.Dialogs;

public partial class ModEditorDialog : Window
{
    /// <summary>Set when user presses Add. Null if cancelled.</summary>
    public (string Name, ModDef Def)? Result { get; private set; }

    public ModEditorDialog() => InitializeComponent();

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            NameBox.Focus();
            return;
        }
        var cat  = (CatBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Special";
        int cost = int.TryParse(CostBox.Text, out int c) ? c : 0;
        int max  = int.TryParse(MaxBox.Text,  out int m) ? Math.Max(1, m) : 1;
        var desc = DescBox.Text.Trim();
        Result = (name, new ModDef(cat, cost, max, desc));
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
