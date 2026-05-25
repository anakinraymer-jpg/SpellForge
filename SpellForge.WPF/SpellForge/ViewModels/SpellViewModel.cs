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
    public IReadOnlyList<LibrarySchoolItem>   LibrarySchools   { get; }
    public IReadOnlyList<LibraryCapstoneItem> LibraryCapstones { get; }

    private static IReadOnlyList<LibrarySchoolItem> BuildLibrarySchools()
    {
        return GameData.SchoolOrder.Select(name =>
        {
            var def  = GameData.Schools[name];
            var abs  = string.Join("  ·  ", def.Abilities.Select(kv => $"{kv.Key} ({kv.Value.Cost}pt)"));
            return new LibrarySchoolItem(name, def.Symbol, def.Desc, def.Color, abs);
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
        LibraryCapstones = BuildLibraryCapstones();
        Spell.PropertyChanged += (_, _) => RefreshDerived();
        BuildLevelThresholds();
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

    // ── Refresh (reentrancy-guarded) ──────────────────────────────
    private bool _isRefreshing;

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
            return sb.ToString();
        }
    }

    // ── Active drawbacks list ─────────────────────────────────────
    public IEnumerable<string> ActiveDrawbacks => Spell.DrawbackBuys.Values;

    // ── Attrition ─────────────────────────────────────────────────
    /// <summary>Ordered list of attrition type names for ComboBox binding.</summary>
    public IReadOnlyList<string> AttritionTypeOptions { get; } =
        GameData.AttritionTypes.Select(a => a.Name).ToList();

    /// <summary>Full definition for the currently selected attrition type.</summary>
    public AttritionDef? SelectedAttritionDef =>
        GameData.AttritionTypes.FirstOrDefault(a => a.Name == Spell.AttritionType);

    // ── Validation visibility helpers ─────────────────────────────
    /// <summary>Collapsed when all limits are OK; Visible when at least one warning exists.</summary>
    public Visibility ValidationWarningsVisibility =>
        Spell.IsValid ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>Visible when all limits are OK; Collapsed when there are warnings.</summary>
    public Visibility NoWarningsVisibility =>
        Spell.IsValid ? Visibility.Visible : Visibility.Collapsed;

    private void RefreshDerived()
    {
        if (_isRefreshing) return;
        _isRefreshing = true;
        try
        {
            Spell.NotifyAllChanged();
            OnPropertyChanged(nameof(SpellEffectsSummary));
            OnPropertyChanged(nameof(ActiveDrawbacks));
            OnPropertyChanged(nameof(ValidationWarningsVisibility));
            OnPropertyChanged(nameof(NoWarningsVisibility));
            OnPropertyChanged(nameof(SelectedAttritionDef));
            RefreshSynergies();
            foreach (var vm in SchoolViewModels) vm.Refresh();
            foreach (var cat in GlobalModCategories)
                foreach (var mod in cat.Mods) mod.Refresh();
            StatusText = $"{Spell.LevelName}  ·  {Spell.TotalPoints} pts  ·  {Spell.AllSchools.Count} school(s)";
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    // ── Commands ─────────────────────────────────────────────────
    [RelayCommand]
    private void NewSpell()
    {
        Spell = new Spell();
        Spell.PropertyChanged += (_, _) => RefreshDerived();
        OnPropertyChanged(nameof(Spell));
        NewIfText = NewThenText = NewWhenText = NewWThenText = "";
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

