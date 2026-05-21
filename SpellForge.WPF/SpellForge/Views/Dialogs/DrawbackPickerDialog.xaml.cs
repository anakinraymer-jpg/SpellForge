using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using SpellForge.Models;

namespace SpellForge.Views.Dialogs;

// ── Per-row VM (light, lives only while dialog is open) ──────────────
public partial class DrawbackItemVM : ObservableObject
{
    private readonly Spell _spell;

    public string Name { get; }
    public string Desc { get; }

    public bool IsActive
    {
        get => _spell.DrawbackBuys.ContainsKey($"neg/{Name}");
        set
        {
            if (value) _spell.DrawbackBuys[$"neg/{Name}"] = Name;
            else       _spell.DrawbackBuys.Remove($"neg/{Name}");
            OnPropertyChanged();
        }
    }

    public DrawbackItemVM(Spell spell, string name, string desc)
    {
        _spell = spell;
        Name   = name;
        Desc   = desc;
    }
}

// ── Dialog ────────────────────────────────────────────────────────────
public partial class DrawbackPickerDialog : Window
{
    private readonly Spell _spell;
    private readonly ObservableCollection<DrawbackItemVM> _items;

    public DrawbackPickerDialog(Spell spell)
    {
        InitializeComponent();
        _spell = spell;

        _items = new ObservableCollection<DrawbackItemVM>(
            GameData.DefaultNegativeMods
                .Select(m => new DrawbackItemVM(spell, m.Name, m.Desc))
                .Concat(spell.CustomNegMods
                    .Select(m => new DrawbackItemVM(spell, m.Name, m.Desc))));

        DrawbackList.ItemsSource = _items;
    }

    private void AddCustom_Click(object sender, RoutedEventArgs e)
    {
        var name = CustomNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name)) { CustomNameBox.Focus(); return; }
        if (_items.Any(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            CustomNameBox.SelectAll();
            CustomNameBox.Focus();
            return;
        }
        var desc   = CustomDescBox.Text.Trim();
        var negMod = new NegModDef(name, desc);
        _spell.CustomNegMods.Add(negMod);
        _items.Add(new DrawbackItemVM(_spell, name, desc));
        CustomNameBox.Text = "";
        CustomDescBox.Text = "";
        CustomNameBox.Focus();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
