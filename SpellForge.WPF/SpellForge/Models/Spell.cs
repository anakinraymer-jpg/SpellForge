using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SpellForge.Models;

public partial class Spell : ObservableObject
{
    [ObservableProperty] private string _name = "Unnamed Spell";
    [ObservableProperty] private string _description = "";

    // school → {abilityName → count}
    public Dictionary<string, Dictionary<string, int>> SchoolAbilities { get; set; } = new();
    // mod name → count
    public Dictionary<string, int> GlobalMods { get; set; } = new();
    // school → {groupName → 0-3}
    public Dictionary<string, Dictionary<string, int>> RingMods { get; set; } = new();
    // free-text custom effects
    public ObservableCollection<string> CustomEffects { get; set; } = new();
    // school → size multiplier (0.4–2.2)
    public Dictionary<string, double> CircleSizes { get; set; } = new();
    // element name → null (inactive), "" (active no subtype), or subtype string
    public Dictionary<string, string?> Elements { get; set; } = new();
    // element name → {nodeName → count}
    public Dictionary<string, Dictionary<string, int>> ElementNodes { get; set; } = new();
    // "el1,el2" → {nodeName → count}
    public Dictionary<string, Dictionary<string, int>> SubelementNodes { get; set; } = new();
    // drawback key → negative mod name
    public Dictionary<string, string> DrawbackBuys { get; set; } = new();
    // user-added negative mods
    public ObservableCollection<NegModDef> CustomNegMods { get; set; } = new();
    // if-then conditions
    public ObservableCollection<ConditionEntry> IfThenConditions { get; set; } = new();
    // when-then conditions
    public ObservableCollection<ConditionEntry> WhenThenConditions { get; set; } = new();

    // ── Derived properties ────────────────────────────────────────
    [JsonIgnore]
    public IReadOnlyList<string> AllSchools
    {
        get
        {
            var result = new List<string>();
            foreach (var school in GameData.SchoolOrder)
            {
                bool hasAbility = SchoolAbilities.TryGetValue(school, out var abs)
                                  && abs.Values.Any(v => v > 0);
                bool hasRingMod = RingMods.TryGetValue(school, out var rm)
                                  && rm.Values.Any(v => v > 0);
                if (hasAbility || hasRingMod) result.Add(school);
            }
            return result;
        }
    }

    [JsonIgnore]
    public int NormalItemCount
    {
        get
        {
            int n = 0;
            foreach (var abs in SchoolAbilities.Values)
                n += abs.Values.Count(c => c > 0);
            foreach (var cnt in GlobalMods.Values)
                if (cnt > 0) n++;
            foreach (var groups in RingMods.Values)
                foreach (var cnt in groups.Values)
                    if (cnt > 0) n++;
            foreach (var val in Elements.Values)
                if (val != null) n++;
            foreach (var nodes in ElementNodes.Values)
                n += nodes.Values.Count(c => c > 0);
            return n;
        }
    }

    [JsonIgnore]
    public bool IsComplete
    {
        get
        {
            int db = DrawbackBuys.Count;
            if (db == 0) return true;
            return (NormalItemCount - db) > db;
        }
    }

    [JsonIgnore]
    public int TotalPoints
    {
        get
        {
            int pts = 0;
            foreach (var (school, abs) in SchoolAbilities)
            {
                var abMap = GameData.Schools.TryGetValue(school, out var sd)
                            ? sd.Abilities : null;
                if (abMap == null) continue;
                foreach (var (ab, cnt) in abs)
                    if (abMap.TryGetValue(ab, out var def)) pts += def.Cost * cnt;
            }
            foreach (var (mod, cnt) in GlobalMods)
                if (GameData.DefaultGlobalMods.TryGetValue(mod, out var md)) pts += md.Cost * cnt;
            foreach (var groups in RingMods.Values)
                foreach (var cnt in groups.Values) pts += cnt;
            foreach (var (el, val) in Elements)
            {
                if (val == null) continue;
                pts += 2;
                if (val.Length > 0) pts += 1; // subtype
            }
            foreach (var (el, nodes) in ElementNodes)
            {
                if (!GameData.Elements.TryGetValue(el, out var ed)) continue;
                foreach (var (nname, cnt) in nodes)
                {
                    var nd = ed.Nodes.FirstOrDefault(x => x.Name == nname);
                    if (nd != null) pts += nd.Cost * cnt;
                }
            }
            // Subtract drawback costs
            foreach (var key in DrawbackBuys.Keys)
            {
                if (key.StartsWith("ability/"))
                {
                    var parts = key.Split('/', 3);
                    if (parts.Length == 3 &&
                        GameData.Schools.TryGetValue(parts[1], out var sd) &&
                        sd.Abilities.TryGetValue(parts[2], out var ad))
                        pts -= ad.Cost;
                }
                else if (key.StartsWith("mod/"))
                {
                    if (GameData.DefaultGlobalMods.TryGetValue(key[4..], out var md))
                        pts -= md.Cost;
                }
                else if (key.StartsWith("ringmod/"))
                    pts -= 1;
            }
            return Math.Max(0, pts);
        }
    }

    [JsonIgnore]
    public LevelEntry LevelInfo => GameData.PtsToLevel(TotalPoints);

    [JsonIgnore]
    public string LevelName => LevelInfo.Name;

    [JsonIgnore]
    public double LevelProgress
    {
        get
        {
            var entry = LevelInfo;
            int range = entry.Hi - entry.Lo;
            if (range <= 0) return 100;
            return Math.Min(100, (double)(TotalPoints - entry.Lo) / range * 100);
        }
    }

    public bool CapstoneActive(string school)
    {
        if (!RingMods.TryGetValue(school, out var rd)) return false;
        return GameData.RingGroups.All(g => rd.GetValueOrDefault(g) >= 3);
    }

    public void NotifyAllChanged()
    {
        OnPropertyChanged(nameof(AllSchools));
        OnPropertyChanged(nameof(TotalPoints));
        OnPropertyChanged(nameof(LevelInfo));
        OnPropertyChanged(nameof(LevelName));
        OnPropertyChanged(nameof(LevelProgress));
        OnPropertyChanged(nameof(NormalItemCount));
        OnPropertyChanged(nameof(IsComplete));
    }
}

public record ConditionEntry(string IfOrWhenText, string ThenText);
