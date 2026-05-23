using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using SpellForge.Models;

namespace SpellForge.Views.Dialogs;

// ── Per-row VM ────────────────────────────────────────────────────────
public partial class DrawbackItemVM : ObservableObject
{
    private readonly Spell   _spell;
    private readonly Action  _onChanged;

    public string Name    { get; }
    public string Desc    { get; }
    public int    Cost    { get; }
    public string CostTag => $"−{Cost} pt";

    public bool IsActive
    {
        get => _spell.DrawbackBuys.ContainsKey($"neg/{Name}");
        set
        {
            if (value) _spell.DrawbackBuys[$"neg/{Name}"] = Name;
            else       _spell.DrawbackBuys.Remove($"neg/{Name}");
            OnPropertyChanged();
            _onChanged();
        }
    }

    public DrawbackItemVM(Spell spell, string name, string desc, int cost, Action onChanged)
    {
        _spell     = spell;
        _onChanged = onChanged;
        Name       = name;
        Desc       = desc;
        Cost       = cost;
    }
}

// ── Dialog ────────────────────────────────────────────────────────────
public partial class DrawbackPickerDialog : Window
{
    private readonly Spell _spell;
    private readonly ObservableCollection<DrawbackItemVM> _items = new();

    public DrawbackPickerDialog(Spell spell)
    {
        InitializeComponent();
        _spell = spell;
        RebuildList();
        DrawbackList.ItemsSource = _items;
        UpdateTotals();
    }

    private void RebuildList()
    {
        _items.Clear();
        foreach (var m in GameData.DefaultNegativeMods)
            _items.Add(new DrawbackItemVM(_spell, m.Name, m.Desc, m.Cost, UpdateTotals));
        foreach (var m in _spell.CustomNegMods)
            _items.Add(new DrawbackItemVM(_spell, m.Name, m.Desc, m.Cost, UpdateTotals));
    }

    private void UpdateTotals()
    {
        int refund   = _items.Where(i => i.IsActive).Sum(i => i.Cost);
        int active   = _items.Count(i => i.IsActive);
        TotalLabel.Text = active == 0
            ? "No drawbacks selected."
            : $"{active} drawback{(active == 1 ? "" : "s")} selected  ·  −{refund} pts refunded";
    }

    private void AddCustom_Click(object sender, RoutedEventArgs e)
    {
        var name = CustomNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name)) { CustomNameBox.Focus(); return; }
        if (_items.Any(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        { CustomNameBox.SelectAll(); CustomNameBox.Focus(); return; }

        int cost = 2;
        if (int.TryParse(CustomCostBox.Text.Trim(), out int parsed))
            cost = Math.Clamp(parsed, 1, 5);

        var desc   = CustomDescBox.Text.Trim();
        var negMod = new NegModDef(name, desc, cost);
        _spell.CustomNegMods.Add(negMod);
        _items.Add(new DrawbackItemVM(_spell, name, desc, cost, UpdateTotals));
        CustomNameBox.Text = "";
        CustomDescBox.Text = "";
        CustomCostBox.Text = "2";
        CustomNameBox.Focus();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
