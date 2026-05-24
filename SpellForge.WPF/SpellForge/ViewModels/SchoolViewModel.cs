using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpellForge.Models;
using SpellForge.Views.Dialogs;

namespace SpellForge.ViewModels;

// ── Ring modifier row ─────────────────────────────────────────────
public partial class RingModRowVM : ObservableObject
{
    private readonly string _group;
    private readonly string _school;
    private Spell  _spell;
    private Action _onChange;

    public string Group  { get; }
    public string Label  { get; }
    public string Color  { get; }
    public SolidColorBrush ColorBrush { get; }
    public int Max => 3;

    [ObservableProperty] private int _count;

    partial void OnCountChanged(int value) => OnPropertyChanged(nameof(CountDisplay));

    public string CountDisplay => Count switch
    {
        0 => "○ ○ ○",
        1 => "● ○ ○",
        2 => "● ● ○",
        _ => "● ● ●",
    };

    public RingModRowVM(string group, string label, string color, string school, Spell spell, Action onChange)
    {
        _group    = group;
        Group     = group;
        Label     = label;
        Color     = color;
        _school   = school;
        _spell    = spell;
        _onChange = onChange;
        ColorBrush = new SolidColorBrush(
            (System.Windows.Media.Color)ColorConverter.ConvertFromString(color));

        ReadCount();
    }

    private void ReadCount()
    {
        int v = 0;
        if (_spell.RingMods.TryGetValue(_school, out var rm)) rm.TryGetValue(_group, out v);
        Count = v;
    }

    [RelayCommand(CanExecute = nameof(CanIncrement))]
    private void Increment()
    {
        if (!CanIncrement()) return;
        Count++;
        EnsureDict();
        _spell.RingMods[_school][_group] = Count;
        _onChange();
    }

    private bool CanIncrement()
    {
        if (Count >= 3) return false;
        // School cap: adding the first pip to this school would activate a new school
        bool alreadyActive = _spell.AllSchools.Contains(_school);
        if (!alreadyActive && _spell.AllSchools.Count >= Spell.MaxSchools) return false;
        // Ring-mod cap: no more pips allowed at current base level
        if (_spell.TotalRingMods >= _spell.RingModCap) return false;
        return true;
    }

    [RelayCommand]
    private void Decrement()
    {
        if (Count <= 0) return;
        Count--;
        EnsureDict();
        _spell.RingMods[_school][_group] = Count;
        _onChange();
    }

    /// <summary>Re-read count from the current spell instance.</summary>
    public void Refresh()
    {
        ReadCount();
        IncrementCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Point this row at a new spell and callback.</summary>
    public void Rebuild(Spell newSpell, Action newOnChange)
    {
        _spell    = newSpell;
        _onChange = newOnChange;
        ReadCount();
    }

    private void EnsureDict()
    {
        if (!_spell.RingMods.ContainsKey(_school))
            _spell.RingMods[_school] = new Dictionary<string, int>();
    }
}

// ── Ability row ───────────────────────────────────────────────────
public partial class AbilityRowVM : ObservableObject
{
    private readonly string _name;
    private readonly string _school;
    private Spell  _spell;
    private Action _onChange;

    public string Name      { get; }
    public string Desc      { get; }
    public int    Cost      { get; }
    public string CostLabel { get; }
    public SolidColorBrush ColorBrush { get; } = new(
        (System.Windows.Media.Color)ColorConverter.ConvertFromString("#c8a840"));

    [ObservableProperty] private int _count;

    partial void OnCountChanged(int value) => OnPropertyChanged(nameof(CountDisplay));

    public string CountDisplay => Count switch
    {
        0 => "○ ○ ○",
        1 => "● ○ ○",
        2 => "● ● ○",
        _ => "● ● ●",
    };

    public AbilityRowVM(string name, AbilityDef def, string school, Spell spell, Action onChange)
    {
        _name     = name;
        _school   = school;
        _spell    = spell;
        _onChange = onChange;
        Name      = name;
        Desc      = def.Desc;
        Cost      = def.Cost;
        CostLabel = $"({Cost} pt)";

        ReadCount();
    }

    private void ReadCount()
    {
        int v = 0;
        if (_spell.SchoolAbilities.TryGetValue(_school, out var abs)) abs.TryGetValue(_name, out v);
        Count = v;
    }

    [RelayCommand(CanExecute = nameof(CanIncrement))]
    private void Increment()
    {
        if (!CanIncrement()) return;
        Count++;
        EnsureDict();
        _spell.SchoolAbilities[_school][_name] = Count;
        _onChange();
    }

    private bool CanIncrement()
    {
        if (Count >= 3) return false;
        // School cap: purchasing the first ability in this school would activate a new school
        bool alreadyActive = _spell.AllSchools.Contains(_school);
        if (!alreadyActive && _spell.AllSchools.Count >= Spell.MaxSchools) return false;
        return true;
    }

    [RelayCommand]
    private void Decrement()
    {
        if (Count <= 0) return;
        Count--;
        EnsureDict();
        _spell.SchoolAbilities[_school][_name] = Count;
        _onChange();
    }

    /// <summary>Re-read count from the current spell instance.</summary>
    public void Refresh()
    {
        ReadCount();
        IncrementCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Point this row at a new spell and callback.</summary>
    public void Rebuild(Spell newSpell, Action newOnChange)
    {
        _spell    = newSpell;
        _onChange = newOnChange;
        ReadCount();
    }

    private void EnsureDict()
    {
        if (!_spell.SchoolAbilities.ContainsKey(_school))
            _spell.SchoolAbilities[_school] = new Dictionary<string, int>();
    }
}

// ── School view model ─────────────────────────────────────────────
public partial class SchoolViewModel : ObservableObject
{
    private static readonly IReadOnlyDictionary<string, string> RingColors =
        new Dictionary<string, string>
        {
            ["Range"]    = "#ff8080",
            ["Duration"] = "#80ee88",
            ["Area"]     = "#8088ff",
            ["Power"]    = "#ffe080",
        };

    public string          Name       { get; }
    public string          Symbol     { get; }
    public string          Desc       { get; }
    public string          Color      { get; }
    public SolidColorBrush ColorBrush { get; }

    public ObservableCollection<RingModRowVM> RingMods  { get; } = new();
    public ObservableCollection<AbilityRowVM> Abilities { get; } = new();

    // ── Circle size ───────────────────────────────────────────────
    public double CircleSize
    {
        get => _spell.CircleSizes.TryGetValue(Name, out var s) ? s : 1.0;
        set
        {
            _spell.CircleSizes[Name] = Math.Round(value, 2);
            OnPropertyChanged();
            OnPropertyChanged(nameof(CircleSizeLabel));
            _onChange();
        }
    }
    public string CircleSizeLabel => $"{CircleSize:F1}×";

    // ── Capstone ──────────────────────────────────────────────────
    private Spell  _spell;
    private Action _onChange;

    public CapstoneDef EffectiveCapstone =>
        _spell.CustomCapstones.TryGetValue(Name, out var c) ? c : GameData.Capstones[Name];

    public Visibility CapstoneActiveVisibility =>
        _spell.CapstoneActive(Name) ? Visibility.Visible : Visibility.Collapsed;

    public Visibility CapstoneLockedVisibility =>
        _spell.CapstoneActive(Name) ? Visibility.Collapsed : Visibility.Visible;

    [RelayCommand]
    private void EditCapstone()
    {
        var dlg = new CapstoneEditorDialog(Name, EffectiveCapstone)
        {
            Owner = Application.Current.MainWindow,
        };
        if (dlg.ShowDialog() == true && dlg.Result != null)
        {
            _spell.CustomCapstones[Name] = dlg.Result;
            OnPropertyChanged(nameof(EffectiveCapstone));
            OnPropertyChanged(nameof(CapstoneActiveVisibility));
            _onChange();
        }
    }

    // ─────────────────────────────────────────────────────────────
    public SchoolViewModel(string name, SchoolDef def, Spell spell, Action onChange)
    {
        Name       = name;
        Symbol     = def.Symbol;
        Desc       = def.Desc;
        Color      = def.Color;
        ColorBrush = new SolidColorBrush(
            (System.Windows.Media.Color)ColorConverter.ConvertFromString(def.Color));
        _spell     = spell;
        _onChange  = onChange;

        BuildRows(def, spell, onChange);
    }

    private void BuildRows(SchoolDef def, Spell spell, Action onChange)
    {
        RingMods.Clear();
        foreach (var group in GameData.RingGroups)
        {
            var label = def.RingMods.TryGetValue(group, out var lbl) ? lbl : group;
            var color = RingColors.TryGetValue(group, out var col) ? col : "#cccccc";
            RingMods.Add(new RingModRowVM(group, label, color, Name, spell, onChange));
        }

        Abilities.Clear();
        foreach (var (aName, aDef) in def.Abilities)
            Abilities.Add(new AbilityRowVM(aName, aDef, Name, spell, onChange));
    }

    /// <summary>Re-read all row counts from current spell (e.g. after undo/load).</summary>
    public void Refresh()
    {
        foreach (var row in RingMods)  row.Refresh();
        foreach (var row in Abilities) row.Refresh();
        OnPropertyChanged(nameof(CircleSize));
        OnPropertyChanged(nameof(CircleSizeLabel));
        OnPropertyChanged(nameof(EffectiveCapstone));
        OnPropertyChanged(nameof(CapstoneActiveVisibility));
        OnPropertyChanged(nameof(CapstoneLockedVisibility));
    }

    /// <summary>Point all rows at a replacement spell (New / Open).</summary>
    public void Rebuild(Spell newSpell, Action newOnChange)
    {
        _spell    = newSpell;
        _onChange = newOnChange;
        foreach (var row in RingMods)  row.Rebuild(newSpell, newOnChange);
        foreach (var row in Abilities) row.Rebuild(newSpell, newOnChange);
        OnPropertyChanged(nameof(CircleSize));
        OnPropertyChanged(nameof(CircleSizeLabel));
    }
}
