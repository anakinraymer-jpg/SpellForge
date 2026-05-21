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
    public DrawbackPickerDialog(Spell spell)
    {
        InitializeComponent();

        // Combine built-in and custom negative mods
        var items = GameData.DefaultNegativeMods
            .Select(m => new DrawbackItemVM(spell, m.Name, m.Desc))
            .Concat(spell.CustomNegMods
                .Select(m => new DrawbackItemVM(spell, m.Name, m.Desc)))
            .ToList();

        DrawbackList.ItemsSource = items;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
