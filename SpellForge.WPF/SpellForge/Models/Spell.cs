using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SpellForge.Models;

public partial class Spell : ObservableObject
{
    [ObservableProperty] private string _name          = "Unnamed Spell";
    [ObservableProperty] private string _description   = "";
    /// <summary>Which attrition grade this spell applies on a successful hit.</summary>
    [ObservableProperty] private string _attritionType = "None";
    /// <summary>Explicitly designated primary school.  Empty = none chosen.</summary>
    [ObservableProperty] private string _primarySchool = "";

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
    // per-school capstone overrides (null entry = use GameData defaults)
    public Dictionary<string, CapstoneDef> CustomCapstones { get; set; } = new();
    // user-added custom global modifier definitions
    public Dictionary<string, ModDef> CustomMods { get; set; } = new();
    // per-element text overrides (modification label + description)
    public Dictionary<string, ElementOverride> CustomElementDescs { get; set; } = new();
    // if-then conditions
    public ObservableCollection<ConditionEntry> IfThenConditions { get; set; } = new();
    // when-then conditions
    public ObservableCollection<ConditionEntry> WhenThenConditions { get; set; } = new();

    // ── Rule constants ────────────────────────────────────────────
    public const int MaxSchools  = 3;
    public const int MaxElements = 3;   // already enforced in UI; mirrored here for validation

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

    /// <summary>The school that renders in the centre of the magic circle.
    /// Returns <see cref="PrimarySchool"/> if it is currently active; otherwise empty.</summary>
    [JsonIgnore]
    public string PrimarySchoolResolved
    {
        get
        {
            var all = AllSchools;
            if (!string.IsNullOrEmpty(PrimarySchool) && all.Contains(PrimarySchool))
                return PrimarySchool;
            return "";
        }
    }

    // ── Ring-mod cap helpers ──────────────────────────────────────
    /// <summary>Sum of all ring-mod pips currently purchased across all schools.</summary>
    [JsonIgnore]
    public int TotalRingMods
    {
        get
        {
            int n = 0;
            foreach (var groups in RingMods.Values)
                foreach (var cnt in groups.Values)
                    n += cnt;
            return n;
        }
    }

    /// <summary>Points spent on non-ring-mod items (abilities, global mods, elements…).
    /// Used to compute RingModCap without ring mods inflating their own cap.</summary>
    [JsonIgnore]
    public int BasePoints => TotalPoints - TotalRingMods;

    /// <summary>Maximum total ring-mod pips the spell may have.
    /// = LevelTableIndex(BasePoints) × 3, clamped to [2, 36].
    /// Cantrip base (index 0) gives cap 2, Omnipotent base (index 14) gives cap 36 (=12 ring × 3 max).</summary>
    [JsonIgnore]
    public int RingModCap => Math.Clamp(GameData.LevelTableIndex(BasePoints) * 3, 2, 36);

    /// <summary>How many more ring-mod pips may still be purchased.</summary>
    [JsonIgnore]
    public int RingModsRemaining => Math.Max(0, RingModCap - TotalRingMods);

    // ── Validation ────────────────────────────────────────────────
    /// <summary>Human-readable list of rule violations.  Empty = legal spell.</summary>
    [JsonIgnore]
    public IReadOnlyList<string> ValidationWarnings
    {
        get
        {
            var w = new List<string>();
            int sc = AllSchools.Count;
            if (sc > MaxSchools)
                w.Add($"Too many schools  ({sc} active / {MaxSchools} max)");
            int el = Elements.Values.Count(v => v != null);
            if (el > MaxElements)
                w.Add($"Too many elements  ({el} active / {MaxElements} max)");
            int rm = TotalRingMods;
            if (rm > RingModCap)
                w.Add($"Ring mods exceed cap  ({rm} used / {RingModCap} allowed at base level)");
            return w;
        }
    }

    /// <summary>True when no rule violations are present.</summary>
    [JsonIgnore]
    public bool IsValid => ValidationWarnings.Count == 0;

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
            foreach (var nodes in SubelementNodes.Values)
                n += nodes.Values.Count(c => c > 0);
            return n;
        }
    }

    // Total points refunded by active drawbacks (before the 50% cap).
    [JsonIgnore]
    public int DrawbackRefund
    {
        get
        {
            int refund = 0;
            foreach (var key in DrawbackBuys.Keys)
            {
                if (key.StartsWith("neg/"))
                {
                    var name   = key[4..];
                    var found  = GameData.DefaultNegativeMods.FirstOrDefault(m => m.Name == name);
                    refund    += found?.Cost ?? CustomNegMods.FirstOrDefault(m => m.Name == name)?.Cost ?? 2;
                }
            }
            return refund;
        }
    }

    // Rule: drawback refunds may not exceed 50% of gross (pre-drawback) spell cost.
    [JsonIgnore]
    public bool IsComplete
    {
        get
        {
            if (DrawbackBuys.Count == 0) return true;
            int gross = TotalPoints + DrawbackRefund;   // total before drawbacks applied
            return DrawbackRefund <= Math.Max(1, gross / 2);
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
            {
                if (GameData.DefaultGlobalMods.TryGetValue(mod, out var md)) pts += md.Cost * cnt;
                else if (CustomMods.TryGetValue(mod, out var cm))            pts += cm.Cost * cnt;
            }
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
            foreach (var (pairKey, nodes) in SubelementNodes)
            {
                var parts = pairKey.Split(',');
                if (parts.Length != 2) continue;
                if (!GameData.SubelementNodes.TryGetValue((parts[0], parts[1]), out var defs)) continue;
                foreach (var (nname, cnt) in nodes)
                {
                    var nd = defs.FirstOrDefault(x => x.Name == nname);
                    if (nd != null) pts += nd.Cost * cnt;
                }
            }
            // ── Subtract drawback refunds ─────────────────────────
            int grossPts = pts;
            foreach (var key in DrawbackBuys.Keys)
            {
                if (key.StartsWith("neg/"))
                {
                    // Standard / custom narrative drawbacks
                    var name  = key[4..];
                    var found = GameData.DefaultNegativeMods.FirstOrDefault(m => m.Name == name);
                    pts -= found?.Cost ?? CustomNegMods.FirstOrDefault(m => m.Name == name)?.Cost ?? 2;
                }
                else if (key.StartsWith("ability/"))
                {
                    var parts = key.Split('/', 3);
                    if (parts.Length == 3 &&
                        GameData.Schools.TryGetValue(parts[1], out var sd) &&
                        sd.Abilities.TryGetValue(parts[2], out var ad))
                        pts -= ad.Cost;
                }
                else if (key.StartsWith("mod/"))
                {
                    var mname = key[4..];
                    if (GameData.DefaultGlobalMods.TryGetValue(mname, out var md))      pts -= md.Cost;
                    else if (CustomMods.TryGetValue(mname, out var cm))                 pts -= cm.Cost;
                }
                else if (key.StartsWith("ringmod/"))
                    pts -= 1;
                else if (key.StartsWith("elemnode/"))
                {
                    var kparts = key.Split('/', 3);
                    if (kparts.Length == 3 &&
                        GameData.Elements.TryGetValue(kparts[1], out var eld))
                    {
                        var nd = eld.Nodes.FirstOrDefault(x => x.Name == kparts[2]);
                        if (nd != null) pts -= nd.Cost;
                    }
                }
                else if (key.StartsWith("subelemnode/"))
                {
                    var kparts   = key.Split('/', 3);
                    if (kparts.Length == 3)
                    {
                        var subParts = kparts[1].Split(',');
                        if (subParts.Length == 2 &&
                            GameData.SubelementNodes.TryGetValue((subParts[0], subParts[1]), out var defs))
                        {
                            var nd = defs.FirstOrDefault(x => x.Name == kparts[2]);
                            if (nd != null) pts -= nd.Cost;
                        }
                    }
                }
            }
            // Enforce 50% cap: drawbacks can refund at most half of gross cost
            int maxRefund = Math.Max(0, grossPts / 2);
            int actualRefund = grossPts - pts;
            if (actualRefund > maxRefund) pts = grossPts - maxRefund;
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
        OnPropertyChanged(nameof(DrawbackRefund));
        OnPropertyChanged(nameof(IsComplete));
        // Ring-mod cap & validation
        OnPropertyChanged(nameof(TotalRingMods));
        OnPropertyChanged(nameof(BasePoints));
        OnPropertyChanged(nameof(RingModCap));
        OnPropertyChanged(nameof(RingModsRemaining));
        OnPropertyChanged(nameof(ValidationWarnings));
        OnPropertyChanged(nameof(IsValid));
        OnPropertyChanged(nameof(PrimarySchoolResolved));
    }
}

public record ConditionEntry(string IfOrWhenText, string ThenText);
