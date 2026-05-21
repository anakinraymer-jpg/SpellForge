using System.Windows;
using SpellForge.Models;

namespace SpellForge.Views.Dialogs;

public partial class CapstoneEditorDialog : Window
{
    private readonly string _origColor;

    /// <summary>Populated when the user presses Save. Null if cancelled.</summary>
    public CapstoneDef? Result { get; private set; }

    public CapstoneEditorDialog(string schoolName, CapstoneDef def)
    {
        InitializeComponent();
        Title       = $"Edit Capstone — {schoolName}";
        _origColor  = def.Color;
        NameBox.Text  = def.Name;
        GlyphBox.Text = def.Glyph;
        RingBox.Text  = def.Ring;
        DescBox.Text  = def.Desc;
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        var ring = RingBox.Text.PadRight(8)[..8];
        Result = new CapstoneDef(
            NameBox.Text.Trim(),
            DescBox.Text.Trim(),
            GlyphBox.Text.Length > 0 ? GlyphBox.Text[..1] : "?",
            ring,
            _origColor);
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
