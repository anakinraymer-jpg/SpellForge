using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SpellForge.Models;
using SpellForge.Views.Controls;
using SpellForge.Views.Dialogs;

namespace SpellForge.ViewModels;

public partial class SpellViewModel : ViewModelBase
{
    [ObservableProperty] private Spell _spell = new();
    [ObservableProperty] private string _statusText = "Ready";
    [ObservableProperty] private string _currentFilePath = "";

    // ── Library (static reference data) ──────────────────────────
    public IReadOnlyList<LibrarySchoolItem>    LibrarySchools   { get; }
    public IReadOnlyList<LibraryElementItem>   LibraryElements  { get; }
    public IReadOnlyList<LibraryCapstoneItem>  LibraryCapstones { get; }

    private static IReadOnlyList<LibrarySchoolItem> BuildLibrarySchools()
    {
        return GameData.SchoolOrder.Select(name =>
        {
            var def  = GameData.Schools[name];
            var abs  = string.Join("  ·  ", def.Abilities.Select(kv => $"{kv.Key} ({kv.Value.Cost}pt)"));
            return new LibrarySchoolItem(name, def.Symbol, def.Desc, def.Color, abs);
        }).ToList();
    }

    private static IReadOnlyList<LibraryElementItem> BuildLibraryElements()
    {
        return GameData.Elements.Select(kv =>
        {
            var nodes = string.Join("  ·  ", kv.Value.Nodes.Select(n => $"{n.Glyph} {n.Name} ({n.Cost}pt)"));
            return new LibraryElementItem(kv.Key, kv.Value.Symbol, kv.Value.Modification, kv.Value.Color, nodes);
        }).ToList();
    }

    private static IReadOnlyList<LibraryCapstoneItem> BuildLibraryCapstones()
    {
        return GameData.SchoolOrder.Select(name =>
        {
            var cap = GameData.Capstones[name];
            return new LibraryCapstoneItem(name, cap.Name, cap.Desc, cap.Glyph, cap.Color);
        }).ToList();
    }

    public SpellViewModel()
    {
        LibrarySchools   = BuildLibrarySchools();
        LibraryElements  = BuildLibraryElements();
        LibraryCapstones = BuildLibraryCapstones();
        Spell.PropertyChanged += (_, _) => RefreshDerived();
        BuildLevelThresholds();
        BuildElementViewModels();
        BuildSchoolViewModels();
        BuildGlobalModCategories();
    }

    // ── Level thresholds for the Calculator panel ─────────────────
    public ObservableCollection<LevelThresholdItem> LevelThresholds { get; } = new();

    private void BuildLevelThresholds()
    {
        foreach (var entry in GameData.LevelTable)
            LevelThresholds.Add(new LevelThresholdItem(
                $"{entry.Lo}–{entry.Hi}", entry.Name, entry.Color));
    }

    // ── Element view models ───────────────────────────────────────
    public ObservableCollection<ElementRowViewModel> ElementViewModels { get; } = new();

    private void BuildElementViewModels()
    {
        ElementViewModels.Clear();
        foreach (var (name, def) in GameData.Elements)
            ElementViewModels.Add(new ElementRowViewModel(name, def, Spell, OnSpellDataChanged));
    }

    // ── School view models ────────────────────────────────────────
    public ObservableCollection<SchoolViewModel> SchoolViewModels { get; } = new();

    private void BuildSchoolViewModels()
    {
        if (SchoolViewModels.Count == 0)
        {
            foreach (var name in GameData.SchoolOrder)
                SchoolViewModels.Add(new SchoolViewModel(name, GameData.Schools[name], Spell, OnSpellDataChanged));
        }
        else
        {
            foreach (var vm in SchoolViewModels)
                vm.Rebuild(Spell, OnSpellDataChanged);
        }
    }

    // ── Global mod categories ─────────────────────────────────────
    public ObservableCollection<GlobalModCategoryVM> GlobalModCategories { get; } = new();

    private void BuildGlobalModCategories()
    {
        GlobalModCategories.Clear();
        foreach (var cat in GameData.CatColors.Keys)
        {
            var catVm = new GlobalModCategoryVM
            {
                Name       = cat,
                Color      = GameData.CatColors[cat],
                ColorBrush = new SolidColorBrush(
                    (Color)System.Windows.Media.ColorConverter.ConvertFromString(GameData.CatColors[cat])),
            };
            foreach (var (name, def) in GameData.DefaultGlobalMods.Where(kv => kv.Value.Cat == cat))
                catVm.Mods.Add(new GlobalModRowVM(name, def, Spell, OnSpellDataChanged));
            // Also show any custom mods in this category
            foreach (var (name, def) in Spell.CustomMods.Where(kv => kv.Value.Cat == cat))
                catVm.Mods.Add(new GlobalModRowVM(name, def, Spell, OnSpellDataChanged));
            GlobalModCategories.Add(catVm);
        }
    }

    [RelayCommand]
    private void AddCustomMod()
    {
        var dlg = new ModEditorDialog { Owner = Application.Current.MainWindow };
        if (dlg.ShowDialog() != true || dlg.Result == null) return;
        var (name, def) = dlg.Result.Value;
        if (Spell.CustomMods.ContainsKey(name) || GameData.DefaultGlobalMods.ContainsKey(name))
        {
            MessageBox.Show($"A modifier named \"{name}\" already exists.", "Duplicate Name",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        Spell.CustomMods[name] = def;
        var catVm = GlobalModCategories.FirstOrDefault(c => c.Name == def.Cat);
        catVm?.Mods.Add(new GlobalModRowVM(name, def, Spell, OnSpellDataChanged));
    }

    [RelayCommand]
    private void ManageDrawbacks()
    {
        var dlg = new DrawbackPickerDialog(Spell) { Owner = Application.Current.MainWindow };
        dlg.ShowDialog();
        RefreshDerived();
    }

    // ── If-Then / When-Then conditions ────────────────────────────
    [ObservableProperty] private string _newIfText   = "";
    [ObservableProperty] private string _newThenText = "";
    [ObservableProperty] private string _newWhenText   = "";
    [ObservableProperty] private string _newWThenText  = "";

    [RelayCommand]
    private void AddIfThen()
    {
        if (string.IsNullOrWhiteSpace(NewIfText)) return;
        Spell.IfThenConditions.Add(new ConditionEntry(NewIfText.Trim(), NewThenText.Trim()));
        NewIfText = NewThenText = "";
    }

    [RelayCommand]
    private void RemoveIfThen(ConditionEntry entry) => Spell.IfThenConditions.Remove(entry);

    [RelayCommand]
    private void AddWhenThen()
    {
        if (string.IsNullOrWhiteSpace(NewWhenText)) return;
        Spell.WhenThenConditions.Add(new ConditionEntry(NewWhenText.Trim(), NewWThenText.Trim()));
        NewWhenText = NewWThenText = "";
    }

    [RelayCommand]
    private void RemoveWhenThen(ConditionEntry entry) => Spell.WhenThenConditions.Remove(entry);

    // ── Shared callback from row VMs ──────────────────────────────
    private void OnSpellDataChanged() => RefreshDerived();

    // ── Sub-element groups ────────────────────────────────────────
    public ObservableCollection<SubelementGroupVM> ActiveSubelements { get; } = new();
    private HashSet<string> _lastActiveElsForSubelements = new();

    private void RefreshSubelements()
    {
        var activeEls = GameData.Elements.Keys
            .Where(el => Spell.Elements.TryGetValue(el, out var v) && v != null)
            .ToHashSet();

        // If the set of active elements hasn't changed, just refresh existing row counts.
        if (activeEls.SetEquals(_lastActiveElsForSubelements))
        {
            foreach (var grp in ActiveSubelements)
                foreach (var row in grp.NodeRows) row.Refresh();
            return;
        }

        _lastActiveElsForSubelements = activeEls;
        ActiveSubelements.Clear();

        foreach (var ((el1, el2), nodes) in GameData.SubelementNodes)
        {
            if (!activeEls.Contains(el1) || !activeEls.Contains(el2)) continue;
            var pairKey = $"{el1},{el2}";
            var d1 = GameData.Elements[el1];
            var d2 = GameData.Elements[el2];
            var label = $"{d1.Symbol} {el1}  +  {d2.Symbol} {el2}";
            var conn  = GameData.ElementConnections.TryGetValue((el1, el2), out var c) ? c : "";
            var grp   = new SubelementGroupVM { Label = label, ConnectionDesc = conn };
            foreach (var node in nodes)
                grp.NodeRows.Add(new SubelNodeRowVM(pairKey, node, Spell, OnSpellDataChanged));
            ActiveSubelements.Add(grp);
        }
    }

    // ── Active synergies ──────────────────────────────────────────
    public ObservableCollection<SynergyItem> ActiveSynergies { get; } = new();

    private void RefreshSynergies()
    {
        ActiveSynergies.Clear();
        var schools = Spell.AllSchools;
        var seen = new HashSet<(string, string)>();
        for (int i = 0; i < schools.Count; i++)
        for (int j = i + 1; j < schools.Count; j++)
        {
            var pair = GameData.SchoolPair(schools[i], schools[j]);
            if (pair == null || seen.Contains(pair.Value)) continue;
            seen.Add(pair.Value);
            bool cap = Spell.CapstoneActive(schools[i]) && Spell.CapstoneActive(schools[j]);
            var s1 = GameData.Schools[schools[i]];
            var s2 = GameData.Schools[schools[j]];
            ActiveSynergies.Add(new SynergyItem(
                $"{s1.Symbol} {schools[i]} + {s2.Symbol} {schools[j]}{(cap ? "  ⚜" : "  ○")}",
                GameData.SchoolConnections[pair.Value],
                cap ? "#FFD700" : ColorHelper.Blend(s1.Color, s2.Color, 0.5)));
        }
    }

    // ── Spell effects summary ─────────────────────────────────────
    public string SpellEffectsSummary
    {
        get
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"{Spell.Name}  ·  {Spell.LevelName}  ·  {Spell.TotalPoints} pts");
            foreach (var school in Spell.AllSchools)
                sb.AppendLine($"  {GameData.Schools[school].Symbol} {school}");
            if (Spell.Elements.Any(e => e.Value != null))
            {
                sb.AppendLine("Elements:");
                foreach (var (el, val) in Spell.Elements)
                    if (val != null) sb.AppendLine($"  {GameData.Elements[el].Symbol} {el}{(val.Length > 0 ? $" [{val}]" : "")}");
            }
            return sb.ToString();
        }
    }

    // ── Guide text ────────────────────────────────────────────────
    public string GuideText
    {
        get
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("SYNERGY CONNECTIONS\n");
            foreach (var ((a, b), name) in GameData.SchoolConnections)
                sb.AppendLine($"{a} + {b}\n  → {name}\n");
            sb.AppendLine("ELEMENTS\n");
            foreach (var (el, def) in GameData.Elements)
                sb.AppendLine($"{def.Symbol} {el}\n  {def.Desc}\n  {def.Modification}\n");
            return sb.ToString();
        }
    }

    // ── Active drawbacks list ─────────────────────────────────────
    public IEnumerable<string> ActiveDrawbacks => Spell.DrawbackBuys.Values;

    private void RefreshDerived()
    {
        Spell.NotifyAllChanged();
        OnPropertyChanged(nameof(SpellEffectsSummary));
        OnPropertyChanged(nameof(ActiveDrawbacks));
        RefreshSynergies();
        foreach (var vm in SchoolViewModels) vm.Refresh();
        foreach (var cat in GlobalModCategories)
            foreach (var mod in cat.Mods) mod.Refresh();
        foreach (var el in ElementViewModels)
            foreach (var node in el.NodeRows) node.Refresh();
        RefreshSubelements();
        StatusText = $"{Spell.LevelName}  ·  {Spell.TotalPoints} pts  ·  {Spell.AllSchools.Count} school(s)";
    }

    // ── Commands ─────────────────────────────────────────────────
    [RelayCommand]
    private void NewSpell()
    {
        Spell = new Spell();
        Spell.PropertyChanged += (_, _) => RefreshDerived();
        OnPropertyChanged(nameof(Spell));
        NewIfText = NewThenText = NewWhenText = NewWThenText = "";
        _lastActiveElsForSubelements = new HashSet<string>();
        BuildElementViewModels();
        BuildSchoolViewModels();
        BuildGlobalModCategories();
        RefreshDerived();
        CurrentFilePath = "";
    }

    [RelayCommand]
    private void OpenSpell()
    {
        var dlg = new OpenFileDialog { Filter = "SpellForge files (*.json)|*.json|All files (*.*)|*.*" };
        if (dlg.ShowDialog() != true) return;
        var loaded = SpellSerializer.LoadFromFile(dlg.FileName);
        if (loaded == null) { MessageBox.Show("Could not load spell."); return; }
        Spell = loaded;
        Spell.PropertyChanged += (_, _) => RefreshDerived();
        OnPropertyChanged(nameof(Spell));
        NewIfText = NewThenText = NewWhenText = NewWThenText = "";
        _lastActiveElsForSubelements = new HashSet<string>();
        BuildElementViewModels();
        BuildSchoolViewModels();
        BuildGlobalModCategories();
        RefreshDerived();
        CurrentFilePath = dlg.FileName;
    }

    [RelayCommand]
    private void SaveSpell()
    {
        if (string.IsNullOrEmpty(CurrentFilePath)) { SaveAsSpell(); return; }
        SpellSerializer.SaveToFile(Spell, CurrentFilePath);
        StatusText = $"Saved: {CurrentFilePath}";
    }

    [RelayCommand]
    private void SaveAsSpell()
    {
        var dlg = new SaveFileDialog { Filter = "SpellForge files (*.json)|*.json", DefaultExt = "json" };
        if (dlg.ShowDialog() != true) return;
        CurrentFilePath = dlg.FileName;
        SpellSerializer.SaveToFile(Spell, CurrentFilePath);
        StatusText = $"Saved: {CurrentFilePath}";
    }

    [RelayCommand] private void ZoomIn()    => MagicCircleCanvas.GlobalZoomIn?.Invoke();
    [RelayCommand] private void ZoomOut()   => MagicCircleCanvas.GlobalZoomOut?.Invoke();
    [RelayCommand] private void ZoomReset() => MagicCircleCanvas.GlobalZoomReset?.Invoke();
}

// ── Supporting VM types ───────────────────────────────────────────

public record LibrarySchoolItem(string Name, string Symbol, string Desc, string Color, string AbilityList)
{
    public System.Windows.Media.SolidColorBrush ColorBrush =>
        new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(Color));
}

public record LibraryElementItem(string Name, string Symbol, string Modification, string Color, string NodeList)
{
    public System.Windows.Media.SolidColorBrush ColorBrush =>
        new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(Color));
}

public record LibraryCapstoneItem(string SchoolName, string Name, string Desc, string Glyph, string Color)
{
    public System.Windows.Media.SolidColorBrush ColorBrush =>
        new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(Color));
}

public record LevelThresholdItem(string Range, string Name, string Color)
{
    public System.Windows.Media.SolidColorBrush ColorBrush =>
        new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(Color));
}

public record SynergyItem(string Header, string Description, string Color)
{
    public System.Windows.Media.SolidColorBrush ColorBrush =>
        new((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(Color));
}

// ── Sub-element node row ──────────────────────────────────────────
public partial class SubelNodeRowVM : ObservableObject
{
    private readonly string _pairKey; // "El1,El2"
    private readonly string _name;
    private readonly Spell  _spell;
    private readonly Action _onChange;

    public string Name      { get; }
    public string Rune      { get; }
    public string Glyph     { get; }
    public string Desc      { get; }
    public int    Cost      { get; }
    public string CostLabel { get; }

    [ObservableProperty] private int _count;
    partial void OnCountChanged(int value) => OnPropertyChanged(nameof(CountDisplay));

    public string CountDisplay => Count switch
    {
        0 => "○ ○ ○",
        1 => "● ○ ○",
        2 => "● ● ○",
        _ => "● ● ●",
    };

    public SubelNodeRowVM(string pairKey, NodeDef def, Spell spell, Action onChange)
    {
        _pairKey  = pairKey;
        _name     = def.Name;
        _spell    = spell;
        _onChange = onChange;
        Name      = def.Name;
        Rune      = def.Rune;
        Glyph     = def.Glyph;
        Desc      = def.Desc;
        Cost      = def.Cost;
        CostLabel = $"({Cost} pt)";
        ReadCount();
    }

    private void ReadCount()
    {
        int v = 0;
        if (_spell.SubelementNodes.TryGetValue(_pairKey, out var nodes))
            nodes.TryGetValue(_name, out v);
        Count = v;
    }

    [RelayCommand]
    private void Increment()
    {
        if (Count >= 3) return;
        Count++;
        EnsureDict();
        _spell.SubelementNodes[_pairKey][_name] = Count;
        _onChange();
    }

    [RelayCommand]
    private void Decrement()
    {
        if (Count <= 0) return;
        Count--;
        EnsureDict();
        _spell.SubelementNodes[_pairKey][_name] = Count;
        _onChange();
    }

    public void Refresh() => ReadCount();

    private void EnsureDict()
    {
        if (!_spell.SubelementNodes.ContainsKey(_pairKey))
            _spell.SubelementNodes[_pairKey] = new Dictionary<string, int>();
    }
}

// ── Sub-element group (one per active element pair) ────────────────
public class SubelementGroupVM
{
    public string Label          { get; set; } = "";
    public string ConnectionDesc { get; set; } = "";
    public ObservableCollection<SubelNodeRowVM> NodeRows { get; } = new();
}

// ── Element node (upgrade) row ────────────────────────────────────
public partial class ElementNodeRowVM : ObservableObject
{
    private readonly string _element;
    private readonly string _name;
    private readonly Spell  _spell;
    private readonly Action _onChange;

    public string Name      { get; }
    public string Rune      { get; }
    public string Glyph     { get; }
    public string Desc      { get; }
    public int    Cost      { get; }
    public string CostLabel { get; }
    public System.Windows.Media.SolidColorBrush ColorBrush { get; }

    [ObservableProperty] private int _count;
    partial void OnCountChanged(int value) => OnPropertyChanged(nameof(CountDisplay));

    public string CountDisplay => Count switch
    {
        0 => "○ ○ ○",
        1 => "● ○ ○",
        2 => "● ● ○",
        _ => "● ● ●",
    };

    public ElementNodeRowVM(string element, NodeDef def,
                            System.Windows.Media.Color elColor,
                            Spell spell, Action onChange)
    {
        _element  = element;
        _name     = def.Name;
        _spell    = spell;
        _onChange = onChange;
        Name      = def.Name;
        Rune      = def.Rune;
        Glyph     = def.Glyph;
        Desc      = def.Desc;
        Cost      = def.Cost;
        CostLabel = $"({Cost} pt)";
        ColorBrush = new System.Windows.Media.SolidColorBrush(elColor);
        ReadCount();
    }

    private void ReadCount()
    {
        int v = 0;
        if (_spell.ElementNodes.TryGetValue(_element, out var nodes))
            nodes.TryGetValue(_name, out v);
        Count = v;
    }

    [RelayCommand]
    private void Increment()
    {
        if (Count >= 3) return;
        Count++;
        EnsureDict();
        _spell.ElementNodes[_element][_name] = Count;
        _onChange();
    }

    [RelayCommand]
    private void Decrement()
    {
        if (Count <= 0) return;
        Count--;
        EnsureDict();
        _spell.ElementNodes[_element][_name] = Count;
        _onChange();
    }

    public void Refresh() => ReadCount();

    private void EnsureDict()
    {
        if (!_spell.ElementNodes.ContainsKey(_element))
            _spell.ElementNodes[_element] = new Dictionary<string, int>();
    }
}

// ── Element row ───────────────────────────────────────────────────
public partial class ElementRowViewModel : ObservableObject
{
    private readonly string     _name;
    private readonly ElementDef _def;
    private readonly Spell      _spell;
    private readonly Action     _onChange;

    public string HeaderText         => $"{_def.Symbol}  {_name}  [{_def.Rune}]";
    public string EffectiveModification =>
        _spell.CustomElementDescs.TryGetValue(_name, out var ov) ? ov.Modification : _def.Modification;
    public string EffectiveDesc =>
        _spell.CustomElementDescs.TryGetValue(_name, out var ov) ? ov.Desc : _def.Desc;

    [RelayCommand]
    private void EditElement()
    {
        var cur = _spell.CustomElementDescs.TryGetValue(_name, out var ov) ? ov
                  : new ElementOverride(_def.Modification, _def.Desc);
        var dlg = new ElementEditorDialog(_name,
            cur.Modification, cur.Desc,
            _def.Modification, _def.Desc)
        {
            Owner = Application.Current.MainWindow,
        };
        if (dlg.ShowDialog() != true) return;
        if (dlg.IsReset)
            _spell.CustomElementDescs.Remove(_name);
        else if (dlg.Result != null)
            _spell.CustomElementDescs[_name] = dlg.Result;
        OnPropertyChanged(nameof(EffectiveModification));
        OnPropertyChanged(nameof(EffectiveDesc));
        _onChange();
    }

    // kept for back-compat bindings
    public string Modification => EffectiveModification;
    public System.Windows.Media.Color SchoolColor =>
        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_def.Color);
    public System.Windows.Media.SolidColorBrush ColorBrush => new(SchoolColor);

    // ── Active toggle ─────────────────────────────────────────────
    public bool IsActive
    {
        get => _spell.Elements.TryGetValue(_name, out var v) && v != null;
        set
        {
            if (value)
            {
                int active = _spell.Elements.Values.Count(v => v != null);
                if (active >= 3)
                {
                    MessageBox.Show("Maximum 3 Elemental Affinities can be active at once.",
                                    "Limit Reached", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                _spell.Elements[_name] = "";
            }
            else
            {
                _spell.Elements.Remove(_name);
                _spell.ElementNodes.Remove(_name);
            }
            OnPropertyChanged();
            OnPropertyChanged(nameof(SubtypeVisibility));
            OnPropertyChanged(nameof(NodeSectionVisibility));
            _onChange();
        }
    }

    // ── Celestial subtype ─────────────────────────────────────────
    public bool IsCelestial => _name == "Celestial";

    public Visibility SubtypeVisibility =>
        (IsActive && IsCelestial) ? Visibility.Visible : Visibility.Collapsed;

    public Visibility NodeSectionVisibility =>
        IsActive ? Visibility.Visible : Visibility.Collapsed;

    public IReadOnlyList<string> SubtypeOptions =>
        IsCelestial && _def.Subtypes != null
            ? _def.Subtypes.Keys.ToList()
            : Array.Empty<string>();

    public string SelectedSubtypeText
    {
        get
        {
            if (!_spell.Elements.TryGetValue(_name, out var v) || v == null) return "";
            return v;
        }
        set
        {
            if (_spell.Elements.ContainsKey(_name))
                _spell.Elements[_name] = value ?? "";
            OnPropertyChanged();
            _onChange();
        }
    }

    // ── Upgrade node rows ─────────────────────────────────────────
    public ObservableCollection<ElementNodeRowVM> NodeRows { get; } = new();

    // ─────────────────────────────────────────────────────────────
    public ElementRowViewModel(string name, ElementDef def, Spell spell, Action onChange)
    {
        _name     = name;
        _def      = def;
        _spell    = spell;
        _onChange = onChange;

        foreach (var node in def.Nodes)
            NodeRows.Add(new ElementNodeRowVM(name, node, SchoolColor, spell, onChange));
    }
}
