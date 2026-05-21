using System.Windows;
using SpellForge.Models;

namespace SpellForge.Views.Dialogs;

public partial class ElementEditorDialog : Window
{
    private readonly string _defaultMod;
    private readonly string _defaultDesc;

    /// <summary>
    ///   Non-null when user saves. Null signals "reset to default" (Reset btn) or cancel.
    ///   IsReset distinguishes the two.
    /// </summary>
    public ElementOverride? Result  { get; private set; }
    public bool             IsReset { get; private set; }

    public ElementEditorDialog(string elementName, string currentMod, string currentDesc,
                                string defaultMod,  string defaultDesc)
    {
        InitializeComponent();
        Title        = $"Edit Element — {elementName}";
        _defaultMod  = defaultMod;
        _defaultDesc = defaultDesc;
        ModBox.Text  = currentMod;
        DescBox.Text = currentDesc;
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        Result = new ElementOverride(ModBox.Text.Trim(), DescBox.Text.Trim());
        DialogResult = true;
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        IsReset      = true;
        Result       = null;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
