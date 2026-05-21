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

    public SpellViewModel()
    {
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

    // ── Shared callback from row VMs ──────────────────────────────
    private void OnSpellDataChanged() => RefreshDerived();

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
        StatusText = $"{Spell.LevelName}  ·  {Spell.TotalPoints} pts  ·  {Spell.AllSchools.Count} school(s)";
    }

    // ── Commands ─────────────────────────────────────────────────
    [RelayCommand]
    private void NewSpell()
    {
        Spell = new Spell();
        Spell.PropertyChanged += (_, _) => RefreshDerived();
        OnPropertyChanged(nameof(Spell));
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

    public string HeaderText   => $"{_def.Symbol}  {_name}  [{_def.Rune}]";
    public string Modification => _def.Modification;
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
