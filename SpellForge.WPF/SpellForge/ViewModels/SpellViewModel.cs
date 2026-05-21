using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SpellForge.Models;
using SpellForge.Views.Controls;

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
            ElementViewModels.Add(new ElementRowViewModel(name, def, Spell, RefreshDerived));
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

    private void RefreshDerived()
    {
        OnPropertyChanged(nameof(SpellEffectsSummary));
        RefreshSynergies();
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

public partial class ElementRowViewModel : ObservableObject
{
    private readonly string _name;
    private readonly ElementDef _def;
    private readonly Spell _spell;
    private readonly Action _onChange;

    public string HeaderText   => $"{_def.Symbol}  {_name}  [{_def.Rune}]";
    public string Modification => _def.Modification;
    public System.Windows.Media.Color SchoolColor =>
        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_def.Color);
    public System.Windows.Media.SolidColorBrush ColorBrush => new(SchoolColor);

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
            _onChange();
        }
    }

    public ElementRowViewModel(string name, ElementDef def, Spell spell, Action onChange)
    {
        _name = name; _def = def; _spell = spell; _onChange = onChange;
    }
}
