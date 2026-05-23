using System.Windows.Media;
using SpellForge.Models;

namespace SpellForge.ViewModels;

// ── Per-school contribution to each core dimension ────────────────
public record SchoolDimContrib(
    string School, string Symbol, string Color,
    int Range, int Duration, int Area, int Power)
{
    public SolidColorBrush ColorBrush =>
        new((System.Windows.Media.Color)ColorConverter.ConvertFromString(Color));
}

// ── One active effect line (ability / node / special mod) ─────────
public record SchoolEffectLine(
    string Glyph, string Source, string Color,
    string Name, string Desc)
{
    public SolidColorBrush ColorBrush =>
        new((System.Windows.Media.Color)ColorConverter.ConvertFromString(Color));
}

/// <summary>
/// Interprets a <see cref="Spell"/> through the Crown &amp; Skull lens and
/// produces a ready-to-play stat block.
///
/// Core design: ring mods on every active school feed the four spell
/// dimensions (Range · Duration · Area · Power).  The SpellCard takes
/// the <em>maximum</em> value across schools for each dimension so that
/// adding a second school immediately amplifies the whole sign.
/// Selected global mods (Reach / Hold / Spread / Bite) override the
/// ring-derived defaults when present.
/// </summary>
public class SpellCardVM
{
    // ── Header ────────────────────────────────────────────────────
    public string          SpellName          { get; }
    public string          TierText           { get; }
    public string          TierColor          { get; }
    public SolidColorBrush TierColorBrush     { get; }
    public string          AlignmentText      { get; }
    public string          AlignmentColor     { get; }
    public SolidColorBrush AlignmentColorBrush{ get; }
    public string          SchoolsLine        { get; }
    public string          ElementsLine       { get; }

    // ── Core properties ───────────────────────────────────────────
    public string ActionText   { get; }   // casting action cost
    public string CastsText    { get; }   // number of casts
    public string EffectText   { get; }   // dice notation
    public string DurationText { get; }   // hold duration
    public string RangeText    { get; }   // reach
    public string AreaText     { get; }   // spread
    public string SomaticsText { get; }   // somatic/verbal requirements

    // ── Interconnect: per-school ring contributions ───────────────
    public IReadOnlyList<SchoolDimContrib> SchoolContribs { get; }
    public int MaxRange    { get; }
    public int MaxDuration { get; }
    public int MaxArea     { get; }
    public int MaxPower    { get; }

    // ── Active effects (school abilities + nodes + special mods) ──
    public IReadOnlyList<SchoolEffectLine> SchoolEffects { get; }

    // ── Drawback limitations ──────────────────────────────────────
    public IReadOnlyList<string> Limitations { get; }
    public bool HasLimitations => Limitations.Count > 0;

    // ── Special flags ─────────────────────────────────────────────
    public bool            HasUnstable         { get; }
    public bool            HasInfernal         { get; }
    public bool            HasCapstone         { get; }
    public string          UnstableText        { get; }
    public string          UnstableColor       { get; }
    public SolidColorBrush UnstableColorBrush  { get; }

    // ── Footer ────────────────────────────────────────────────────
    public string PointsText { get; }

    // ── Visibility helpers ────────────────────────────────────────
    public bool HasEffects      => SchoolEffects.Count > 0;
    public bool HasSchoolContribs => SchoolContribs.Count > 0;

    // ─────────────────────────────────────────────────────────────

    private static readonly string[] RangeLevels =
        ["Touch", "Near (10 ft)", "Far (60 ft)", "Distant (300 ft+)"];
    private static readonly string[] DurationLevels =
        ["Flash (instant)", "Next turn", "Scene (~10 rounds)", "Watch (hours)"];
    private static readonly string[] AreaLevels =
        ["Single target", "Cone (15 ft)", "Burst (30 ft)", "Zone (60 ft)"];
    private static readonly string[] PowerDice =
        ["D6", "D8", "D10", "D12"];

    // Tier thresholds and labels
    private static readonly int[]    TierCuts   = [0, 8, 18, 35, 55, 80, 110, 145];
    private static readonly string[] TierNames  =
        ["Novice", "Apprentice", "Journeyman", "Adept",
         "Expert", "Master", "Elder", "Legendary"];
    private static readonly string[] TierColors =
        ["#888888","#88aaff","#88ffaa","#ffd060",
         "#ff8844","#ff4488","#cc44ff","#ffffff"];

    // ─────────────────────────────────────────────────────────────
    public SpellCardVM(Spell spell)
    {
        SpellName  = string.IsNullOrWhiteSpace(spell.Name) ? "Unnamed Sign" : spell.Name;
        PointsText = $"{spell.TotalPoints} Hero Points";

        // ── Tier ─────────────────────────────────────────────────
        int pts  = spell.TotalPoints;
        int tier = 0;
        for (int i = TierCuts.Length - 1; i >= 0; i--)
            if (pts >= TierCuts[i]) { tier = i; break; }
        TierText       = TierNames[tier];
        TierColor      = TierColors[tier];
        TierColorBrush = MakeBrush(TierColor);

        // ── Alignment ────────────────────────────────────────────
        var activeSchools = spell.AllSchools;
        int crownCount = activeSchools.Count(s =>
            GameData.Schools.TryGetValue(s, out var sd) && sd.IsCrown);
        int skullCount  = activeSchools.Count - crownCount;

        (AlignmentText, AlignmentColor) = (crownCount, skullCount) switch
        {
            (> 0, > 0) => ("Mixed Alignment ⚠",   "#ffaa44"),
            (_,   > 0) => ("Skull-Marked ☠",      "#cc44cc"),
            (> 0, _)   => ("Crown-Bound ⛤",        "#FFD080"),
            _          => ("Unbound",               "#888888"),
        };
        AlignmentColorBrush = MakeBrush(AlignmentColor);

        // ── Schools / elements summary lines ─────────────────────
        SchoolsLine = activeSchools.Count > 0
            ? string.Join("  ·  ",
                activeSchools.Select(s =>
                    GameData.Schools.TryGetValue(s, out var sd)
                        ? $"{sd.Symbol} {s}" : s))
            : "(no schools active)";

        var activeEls = spell.Elements
            .Where(kv => kv.Value != null)
            .Select(kv => kv.Key).ToList();
        ElementsLine = activeEls.Count > 0
            ? string.Join("  +  ",
                activeEls.Select(e =>
                    GameData.Elements.TryGetValue(e, out var ed)
                        ? $"{ed.Symbol} {e}" : e))
            : "";

        // ── Ring mod maxima across all schools ────────────────────
        int maxR = 0, maxD = 0, maxA = 0, maxP = 0, powerSchoolCount = 0;
        var contribs = new List<SchoolDimContrib>();

        foreach (var school in activeSchools)
        {
            if (!spell.RingMods.TryGetValue(school, out var rm)) rm = new();
            int r = rm.GetValueOrDefault("Range");
            int d = rm.GetValueOrDefault("Duration");
            int a = rm.GetValueOrDefault("Area");
            int p = rm.GetValueOrDefault("Power");
            maxR = Math.Max(maxR, r);
            maxD = Math.Max(maxD, d);
            maxA = Math.Max(maxA, a);
            maxP = Math.Max(maxP, p);
            if (p > 0) powerSchoolCount++;
            if (r > 0 || d > 0 || a > 0 || p > 0)
            {
                contribs.Add(new SchoolDimContrib(
                    school, GameData.Schools[school].Symbol,
                    GameData.Schools[school].Color, r, d, a, p));
            }
        }
        SchoolContribs = contribs;
        MaxRange    = maxR;
        MaxDuration = maxD;
        MaxArea     = maxA;
        MaxPower    = maxP;

        // ── Scan global mods for overrides ───────────────────────
        string? reachMod    = null;
        string? holdMod     = null;
        string? spreadMod   = null;
        string? biteMod     = null;
        string? castingMod  = null;
        int     extraDice   = 0;   // from "Extra Damage Die" × count

        foreach (var (modName, cnt) in spell.GlobalMods)
        {
            if (cnt <= 0) continue;
            ModDef? md = null;
            GameData.DefaultGlobalMods.TryGetValue(modName, out md);
            if (md == null) spell.CustomMods.TryGetValue(modName, out md);
            if (md == null) continue;

            switch (md.Cat)
            {
                case "Range":
                    reachMod = modName;
                    break;
                case "Duration":
                    holdMod = modName;
                    break;
                case "Area":
                    spreadMod = modName;
                    break;
                case "Power":
                    if (modName == "Extra Damage Die") extraDice += cnt;
                    else                               biteMod    = modName;
                    break;
                case "Casting":
                    // Prefer the most restrictive (most negative cost) for display
                    if (castingMod == null) castingMod = modName;
                    else if (md.Cost < (GameData.DefaultGlobalMods.TryGetValue(castingMod, out var cm) ? cm.Cost : 0))
                        castingMod = modName;
                    break;
            }
        }

        // ── Range ─────────────────────────────────────────────────
        RangeText = reachMod ?? (maxR < RangeLevels.Length ? RangeLevels[maxR] : RangeLevels[^1]);

        // ── Duration ─────────────────────────────────────────────
        DurationText = holdMod ?? (maxD < DurationLevels.Length ? DurationLevels[maxD] : DurationLevels[^1]);

        // ── Area ─────────────────────────────────────────────────
        AreaText = spreadMod ?? (maxA < AreaLevels.Length ? AreaLevels[maxA] : AreaLevels[^1]);

        // ── Effect (power) ────────────────────────────────────────
        if (biteMod != null)
        {
            // Derive die from mod name
            string die = biteMod.Contains("2d10") ? "2D10"
                       : biteMod.Contains("d12")  ? "D12"
                       : biteMod.Contains("d10")  ? "D10"
                       : biteMod.Contains("d8")   ? "D8"
                       : biteMod.Contains("d6")   ? "D6"
                       : biteMod.Contains("d4")   ? "D4"
                       : "D6";
            int diceCount = 1 + extraDice;
            EffectText = diceCount > 1 ? $"{diceCount}{die}" : die;
        }
        else
        {
            string die  = maxP < PowerDice.Length ? PowerDice[maxP] : PowerDice[^1];
            int count   = Math.Max(1, powerSchoolCount) + extraDice;
            EffectText  = count > 1 ? $"{count}{die}" : die;
        }

        // Append element flavour
        if (activeEls.Count > 0)
            EffectText += $"  [{string.Join(" + ", activeEls)}]";

        // ── Action cost ───────────────────────────────────────────
        ActionText = castingMod ?? "Cast: Standard (primary action)";

        // ── Casts ─────────────────────────────────────────────────
        CastsText = "1 cast";   // C&S default; no built-in "extra cast" mod yet

        // ── Somatics ─────────────────────────────────────────────
        bool silent   = spell.GlobalMods.GetValueOrDefault("Silent Casting")  > 0;
        bool gestless = spell.GlobalMods.GetValueOrDefault("Gestless")         > 0;
        bool subtle   = spell.GlobalMods.GetValueOrDefault("Subtle Sign")      > 0;
        SomaticsText = subtle   ? "None (subtle sign)"
                     : (silent && gestless) ? "None"
                     : silent   ? "Gestures only"
                     : gestless ? "Spoken word only"
                     : "Audible words + both hands";

        // ── Active effects ────────────────────────────────────────
        var effects = new List<SchoolEffectLine>();

        // Special / Casting global mods first (non-core-dimension ones)
        foreach (var (modName, cnt) in spell.GlobalMods)
        {
            if (cnt <= 0) continue;
            ModDef? md = null;
            GameData.DefaultGlobalMods.TryGetValue(modName, out md);
            if (md == null) spell.CustomMods.TryGetValue(modName, out md);
            if (md == null) continue;
            if (md.Cat != "Special" && md.Cat != "Power") continue;
            // Skip core-dimension overrides already shown
            if (modName is "Silent Casting" or "Gestless" or "Subtle Sign") continue;
            string stacks = cnt > 1 ? $" ×{cnt}" : "";
            effects.Add(new SchoolEffectLine("⊛", md.Cat, "#88ffff", modName + stacks, md.Desc));
        }

        // School abilities
        foreach (var school in activeSchools)
        {
            if (!GameData.Schools.TryGetValue(school, out var sd)) continue;
            if (!spell.SchoolAbilities.TryGetValue(school, out var abs)) continue;
            foreach (var (abName, cnt) in abs)
            {
                if (cnt <= 0) continue;
                if (!sd.Abilities.TryGetValue(abName, out var ad)) continue;
                string stacks = cnt > 1 ? $" ×{cnt}" : "";
                effects.Add(new SchoolEffectLine(
                    sd.Symbol, school, sd.Color, abName + stacks, ad.Desc));
            }
            // Capstone
            if (spell.CapstoneActive(school))
            {
                var cap = spell.CustomCapstones.TryGetValue(school, out var cc)
                          ? cc : GameData.Capstones[school];
                effects.Add(new SchoolEffectLine(
                    cap.Glyph, school, sd.Color,
                    $"⚜ CAPSTONE: {cap.Name}", cap.Desc));
            }
        }

        // Element nodes
        foreach (var (el, nodes) in spell.ElementNodes)
        {
            if (!GameData.Elements.TryGetValue(el, out var ed)) continue;
            if (spell.Elements.GetValueOrDefault(el) == null) continue;
            foreach (var (nName, cnt) in nodes)
            {
                if (cnt <= 0) continue;
                var nd = ed.Nodes.FirstOrDefault(x => x.Name == nName);
                if (nd == null) continue;
                string stacks = cnt > 1 ? $" ×{cnt}" : "";
                effects.Add(new SchoolEffectLine(
                    nd.Glyph, el, ed.Color, nName + stacks, nd.Effect));
            }
        }

        // Sub-element nodes
        foreach (var (pairKey, nodes) in spell.SubelementNodes)
        {
            var parts = pairKey.Split(',');
            if (parts.Length != 2) continue;
            if (!GameData.SubelementNodes.TryGetValue((parts[0], parts[1]), out var defs)) continue;
            if (spell.Elements.GetValueOrDefault(parts[0]) == null ||
                spell.Elements.GetValueOrDefault(parts[1]) == null) continue;
            foreach (var (nName, cnt) in nodes)
            {
                if (cnt <= 0) continue;
                var nd = defs.FirstOrDefault(x => x.Name == nName);
                if (nd == null) continue;
                string stacks = cnt > 1 ? $" ×{cnt}" : "";
                var col1 = GameData.Elements.TryGetValue(parts[0], out var e1) ? e1.Color : "#888888";
                effects.Add(new SchoolEffectLine(
                    nd.Glyph, $"{parts[0]} + {parts[1]}", col1, nName + stacks, nd.Effect));
            }
        }

        // Custom effects text lines
        foreach (var fx in spell.CustomEffects)
            if (!string.IsNullOrWhiteSpace(fx))
                effects.Add(new SchoolEffectLine("✦", "Custom", "#aaaaaa", fx, ""));

        // If-Then / When-Then conditions
        foreach (var c in spell.IfThenConditions)
            effects.Add(new SchoolEffectLine("▸", "Trigger", "#88ccff",
                $"IF {c.IfOrWhenText}", $"THEN {c.ThenText}"));
        foreach (var c in spell.WhenThenConditions)
            effects.Add(new SchoolEffectLine("▸", "Trigger", "#88ffcc",
                $"WHEN {c.IfOrWhenText}", $"THEN {c.ThenText}"));

        SchoolEffects = effects;

        // ── Limitations ───────────────────────────────────────────
        Limitations = spell.DrawbackBuys.Values.ToList();

        // ── Unstable / Infernal ───────────────────────────────────
        bool hasBonecraft  = activeSchools.Contains("Bonecraft");
        bool hasShadowmark = activeSchools.Contains("Shadowmark");

        HasInfernal = hasBonecraft &&
            spell.SchoolAbilities.TryGetValue("Bonecraft", out var bcAbs) &&
            bcAbs.Values.Any(v => v > 0);
        HasUnstable = hasShadowmark &&
            spell.SchoolAbilities.TryGetValue("Shadowmark", out var smAbs) &&
            smAbs.Values.Any(v => v > 0);
        HasCapstone = activeSchools.Any(s => spell.CapstoneActive(s));

        (UnstableText, UnstableColor) = HasInfernal
            ? ("INFERNAL — Roll Magic skill or face Infernal consequence", "#cc44cc")
            : HasUnstable
            ? ("UNSTABLE — Roll Magic skill or consult Unstable table", "#ff8844")
            : ("", "#888888");
        UnstableColorBrush = MakeBrush(UnstableColor);
    }

    private static SolidColorBrush MakeBrush(string hex) =>
        new((System.Windows.Media.Color)ColorConverter.ConvertFromString(hex));
}
