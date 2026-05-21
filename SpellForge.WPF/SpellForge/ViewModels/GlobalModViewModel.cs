using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SpellForge.Models;

namespace SpellForge.ViewModels;

// ── Global modifier row ───────────────────────────────────────────
public partial class GlobalModRowVM : ObservableObject
{
    private readonly string _name;
    private Spell  _spell;
    private Action _onChange;

    public string          Name          { get; }
    public string          Category      { get; }
    public string          CatColor      { get; }
    public SolidColorBrush CatColorBrush { get; }
    public int             Cost          { get; }
    public int             Max           { get; }
    public string          Desc          { get; }
    public string          CostLabel     { get; }

    [ObservableProperty] private int _count;

    partial void OnCountChanged(int value) => OnPropertyChanged(nameof(CountDisplay));

    public string CountDisplay
    {
        get
        {
            int slots = Math.Min(Max, 6);
            var sb    = new System.Text.StringBuilder();
            for (int i = 0; i < slots; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(i < Count ? '●' : '○');
            }
            return sb.ToString();
        }
    }

    public GlobalModRowVM(string name, ModDef def, Spell spell, Action onChange)
    {
        _name     = name;
        _spell    = spell;
        _onChange = onChange;

        Name     = name;
        Category = def.Cat;
        Cost     = def.Cost;
        Max      = def.Max;
        Desc     = def.Desc;
        CostLabel = Cost >= 0 ? $"(+{Cost} pt)" : $"({Cost} pt)";

        CatColor = GameData.CatColors.TryGetValue(def.Cat, out var c) ? c : "#cccccc";
        CatColorBrush = new SolidColorBrush(
            (System.Windows.Media.Color)ColorConverter.ConvertFromString(CatColor));

        ReadCount();
    }

    private void ReadCount()
    {
        _spell.GlobalMods.TryGetValue(_name, out var v);
        Count = v;
    }

    [RelayCommand]
    private void Increment()
    {
        if (Count >= Max) return;
        Count++;
        _spell.GlobalMods[_name] = Count;
        _onChange();
    }

    [RelayCommand]
    private void Decrement()
    {
        if (Count <= 0) return;
        Count--;
        _spell.GlobalMods[_name] = Count;
        _onChange();
    }

    /// <summary>Re-read count from current spell.</summary>
    public void Refresh() => ReadCount();

    /// <summary>Point this row at a replacement spell.</summary>
    public void Rebuild(Spell newSpell, Action newOnChange)
    {
        _spell    = newSpell;
        _onChange = newOnChange;
        ReadCount();
    }
}

// ── Category grouping VM (plain class, no change notifications needed) ──
public class GlobalModCategoryVM
{
    public string          Name       { get; set; } = "";
    public string          Color      { get; set; } = "";
    public SolidColorBrush ColorBrush { get; set; } = new(Colors.White);
    public ObservableCollection<GlobalModRowVM> Mods { get; } = new();
}
