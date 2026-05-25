namespace SpellForge.Models;

// ── Records ──────────────────────────────────────────────────────
public record AbilityDef(int Cost, string Desc);
public record SchoolDef(
    string Color, string Symbol, string Short, string Desc,
    IReadOnlyDictionary<string, string> RingMods,
    IReadOnlyDictionary<string, AbilityDef> Abilities);

public record NodeDef(string Name, int Cost, string Rune, string Glyph, string Desc, string Effect);
public record SubtypeDef(string Color, string Rune, string Modification, string Desc);
public record ElementDef(
    string Color, string Symbol, string Rune,
    string Modification, string Desc,
    IReadOnlyList<NodeDef> Nodes,
    IReadOnlyDictionary<string, SubtypeDef>? Subtypes = null);

public record CapstoneDef(string Name, string Desc, string Glyph, string Ring, string Color);
public record ElementOverride(string Modification, string Desc);
public record ModDef(string Cat, int Cost, int Max, string Desc);
public record NegModDef(string Name, string Desc, int Cost = 2);
public record LevelEntry(int Lo, int Hi, string Name, string Color);

/// <summary>One of the five attrition grades (severity 0–4).</summary>
public record AttritionDef(string Name, string Symbol, string Desc, string Color, int Severity);
public record DomainDef(string Symbol, string Color);

public static class GameData
{
    // ── Runes ──────────────────────────────────────────────────
    public const string RunesAll =
        "ᚠᚢᚦᚨᚱᚲᚷᚹᚺᚾᛁᛃᛇᛈᛉᛊᛏᛒᛖᛗᛚᛜᛞᛟ" +  // Elder
        "ᚠᚢᚦᚬᚱᚴᚼᚾᛁᛅᛋᛏᛒᛘᛚ" +             // Younger
        "ᛡᛢᛣᛤᛥᛮᛯᛰ" +                     // Medieval
        "ᚁᚂᚃᚄᚅᚆᚇᚈᚉᚊᚋᚌᚍᚎᚏᚐᚑᚒᚓᚔᚕᚖᚗᚘᚙᚚ" +  // Ogham
        "☿♄♃♂☉♀☽⊕⊗⊙⊛⊚⊜⊝◉◎◌◍⊞⊟⊠⊡";     // Alchemical

    public static readonly IReadOnlyDictionary<string, string[]> ModRunes = new Dictionary<string, string[]>
    {
        ["Range"]     = ["ᚱ","ᚠ","ᛁ","ᛏ","ᛊ","⊕"],
        ["Duration"]  = ["ᛞ","ᛟ","ᚦ","ᛖ","ᚹ","⊙"],
        ["Area"]      = ["ᚨ","ᚷ","ᛃ","ᛈ","ᛚ","◎"],
        ["Power"]     = ["ᛒ","ᛗ","ᚾ","ᛜ","ᚺ","⊗"],
        ["Casting"]   = ["ᛇ","ᚲ","ᚢ","ᛦ","ᛡ","⊛"],
        ["Special"]   = ["ᛥ","ᛣ","ᛤ","ᛢ","ᛰ","⊚"],
        ["Attrition"] = ["⊖","⚸","⚙","☠","✸","⊗"],
    };

    public static readonly IReadOnlyDictionary<string, string> CatColors = new Dictionary<string, string>
    {
        ["Range"]     = "#ff6060", ["Duration"]  = "#60ee88", ["Area"]    = "#6088ff",
        ["Power"]     = "#ffd060", ["Casting"]   = "#ff88ff", ["Special"] = "#88ffff",
        ["Attrition"] = "#ff7744",
    };

    // ── Attrition types (severity 0 → 4) ───────────────────────
    public static readonly IReadOnlyList<AttritionDef> AttritionTypes = new List<AttritionDef>
    {
        new("None",      "—",  "No attrition on hit.",
            "#666666", 0),
        new("Basic",     "⊖",  "Target player crosses off any 1 equipment or skill of their choice.",
            "#aaaaaa", 1),
        new("Flesh",     "⚸",  "1 randomly selected skill is crossed off.",
            "#88aaff", 2),
        new("Equipment", "⚙",  "1 randomly selected equipment is crossed off.",
            "#ffaa44", 3),
        new("Destroy",   "☠",  "1 random equipment is permanently destroyed; 1 random skill is crossed off.",
            "#ff6666", 4),
        new("Brutal",    "✸",  "Roll 1d6; that many random slots (equipment + skills combined) are crossed off.",
            "#ff2222", 5),
    };

    public static readonly string[] RingGroups = ["Range", "Duration", "Area", "Power"];

    // ── School glyphs (8 per school) ────────────────────────────
    public static readonly IReadOnlyDictionary<string, string[]> SchoolGlyphs = new Dictionary<string, string[]>
    {
        // Dark Magic
        ["Blood"]      = ["⬥","⬦","♥","♦","⊕","⊗","✸","◈"],
        ["Death"]      = ["☠","☽","☾","⚰","⊗","☿","♄","✝"],
        ["Shadow"]     = ["◗","◖","◐","◑","◌","◍","◎","◉"],
        ["Undeath"]    = ["⚰","☠","☽","⚸","⊗","♄","◈","✝"],
        ["Curse"]      = ["⊗","⊛","⊚","⊝","⊞","⊟","⊠","⊡"],
        // Light Magic
        ["Healing"]    = ["✚","✙","✛","✜","⊕","☉","✦","◉"],
        ["Shielding"]  = ["⛨","⛊","⊕","◧","◨","◩","◪","⊠"],
        ["Banishment"] = ["⊘","◎","⊙","☉","✦","⊕","⊗","◌"],
        ["Radiance"]   = ["☀","✦","☉","✸","⊕","◎","⊛","✺"],
        ["Revelation"] = ["◎","◉","⊚","⍜","⍚","⍛","⌭","◌"],
        // Nature Magic
        ["Elemental"]  = ["⌬","⊕","⊗","◈","⊙","⍥","◎","⊛"],
        ["Plants"]     = ["⚘","❀","✿","☙","❧","⊕","◌","⬡"],
        ["Animals"]    = ["♞","♜","♟","⊕","◌","◈","★","✦"],
        ["Aura"]       = ["◌","◍","◎","◉","⊕","⊗","⊙","⊛"],
        ["Storm"]      = ["⚡","⍉","⍥","△","⌀","◉","⍨","⌬"],
        // Planar Magic
        ["Time"]       = ["⌛","⍨","⍩","⍪","⍫","⍬","⍭","⌬"],
        ["Space"]      = ["◉","⌖","◎","⍥","⍉","⌀","⊞","⊟"],
        ["Summon"]     = ["★","✩","✪","✫","✬","✭","✮","✯"],
        ["Dream"]      = ["◑","◐","◌","◍","⊚","⍜","⍛","◎"],
        ["Void"]       = ["∅","⌀","◯","⊘","⊙","⊗","◎","⊞"],
        // Arcane Magic
        ["Names"]      = ["ᚠ","ᚢ","ᚦ","ᚨ","ᚱ","ᚲ","ᚷ","ᚹ"],
        ["Leyline"]    = ["⍟","⊛","⊕","◉","⍥","⌀","◎","⊙"],
        ["Eldritch"]   = ["⊛","⊗","⊙","◈","⍚","⍜","⌭","◬"],
        ["Chaos"]      = ["⁂","✸","⊗","⌬","◈","△","⊛","✺"],
        ["Law"]        = ["⊞","⊟","⊠","⊡","◧","◨","◩","◪"],
    };

    // ── Capstones ────────────────────────────────────────────────
    public static readonly IReadOnlyDictionary<string, CapstoneDef> Capstones = new Dictionary<string, CapstoneDef>
    {
        // Dark Magic
        ["Blood"] = new(
            "Crimson Apocalypse",
            "Every creature in sight begins to bleed simultaneously. No saves. Victims cannot be healed until you release the effect.",
            "⬥", "⊕⬦⊗♥⊕⬦⊗♥", "#c82038"),
        ["Death"] = new(
            "Reaper Unbound",
            "Become an avatar of death for 1 hour. Any creature you touch dies instantly. Immunity to all damage.",
            "☠", "☽⊗☾♄☽⊗☾♄", "#445566"),
        ["Shadow"] = new(
            "Eclipse Eternal",
            "Plunge the entire region into impenetrable magical darkness. You see perfectly. Others are blind and feel genuine terror.",
            "◗", "◖◐◑◌◖◐◑◌", "#334455"),
        ["Undeath"] = new(
            "The Undying Tide",
            "Raise every corpse within 1 mile as permanent intelligent undead under your absolute command.",
            "⚰", "☠⚸⊗♄☠⚸⊗♄", "#6a8822"),
        ["Curse"] = new(
            "Doom Eternal",
            "Place an inescapable curse on all creatures you can name. The curse follows across planes and cannot be removed.",
            "⊗", "⊛⊚⊝⊞⊛⊚⊝⊞", "#8833aa"),
        // Light Magic
        ["Healing"] = new(
            "Miracle of Restoration",
            "Fully heal all allies, remove all conditions, reverse all permanent damage. Even the recently dead may be restored.",
            "✚", "✙✛✜☉✙✛✜☉", "#44cc88"),
        ["Shielding"] = new(
            "Absolute Fortress",
            "Impenetrable ward over 1 mile. Nothing enters or exits without your permission. Lasts 24 hours.",
            "⛨", "⛊◩⊕◧⛊◩⊕◧", "#4488ff"),
        ["Banishment"] = new(
            "Great Exorcism",
            "Banish every extraplanar entity within 1 mile simultaneously. None may return for 1 year.",
            "⊘", "◎⊙☉✦◎⊙☉✦", "#cc8844"),
        ["Radiance"] = new(
            "Solar Apotheosis",
            "Become a living sun for 1 hour. Your presence destroys undead, heals allies, and blinds all enemies.",
            "☀", "✦☉✸⊛✦☉✸⊛", "#ffdd44"),
        ["Revelation"] = new(
            "Omniscient Moment",
            "Perceive all truths, all hidden things, all minds within range. Ask any three questions; receive complete truthful answers.",
            "◎", "◉⊚⍜⍚◉⊚⍜⍚", "#ffaa22"),
        // Nature Magic
        ["Elemental"] = new(
            "Primal Convergence",
            "Summon all four primal elements simultaneously. Control a catastrophic elemental storm that reshapes the landscape.",
            "⌬", "⊕⊗◈⊙⊕⊗◈⊙", "#ff8833"),
        ["Plants"] = new(
            "World Tree",
            "Raise an immense World Tree that roots the area in life. All allies regenerate; all enemies are entangled permanently.",
            "⚘", "❀✿☙❧❀✿☙❧", "#44aa44"),
        ["Animals"] = new(
            "Call of the Wild",
            "Every beast within 50 miles answers your call, arriving within 1 hour. They obey absolutely and fight to the death.",
            "♞", "♜♟⊕◌♜♟⊕◌", "#aa6633"),
        ["Aura"] = new(
            "Convergent Field",
            "Merge all party auras into one overwhelming field. Allies are invincible; enemies are repelled and weakened.",
            "◌", "◍◎◉⊕◍◎◉⊕", "#88ccaa"),
        ["Storm"] = new(
            "Tempest God",
            "Full control over weather in 10-mile radius for 1 hour. Call lightning at will; immunity to all weather effects.",
            "⚡", "⍉⍥△⌀⍉⍥△⌀", "#6677cc"),
        // Planar Magic
        ["Time"] = new(
            "Temporal Supremacy",
            "Stop time for up to 1 hour. Act freely while the world is frozen. Cannot be countered.",
            "⌛", "⍭⍩⍪⍨⍭⍩⍪⍨", "#8888ff"),
        ["Space"] = new(
            "Infinite Fold",
            "Collapse all distance to zero. Instant transit to any point in the multiverse. Move anything to anywhere.",
            "◉", "⊞⍥⌀⌖⊞⍥⌀⌖", "#445599"),
        ["Summon"] = new(
            "Grand Convocation",
            "Summon up to 20 CR20 entities from any plane, permanently bound and obedient to your will.",
            "★", "✮✭✬✩✮✭✬✩", "#cc66aa"),
        ["Dream"] = new(
            "Dreamweave Reality",
            "Force all creatures in range into a shared dream. You control every element of that reality absolutely.",
            "◑", "◐◌◍⊚◐◌◍⊚", "#9966cc"),
        ["Void"] = new(
            "Singularity",
            "Open a gravitational singularity that consumes everything in a 100ft radius. Nothing — matter or magic — survives.",
            "∅", "⌀◯⊘⊗⌀◯⊘⊗", "#553388"),
        // Arcane Magic
        ["Names"] = new(
            "The Unspoken Word",
            "Speak a single True Name that binds all creatures who hear it. They cannot act against your will until released.",
            "ᚠ", "ᚢᚦᚨᚱᚲᚢᚦᚨ", "#aabbcc"),
        ["Leyline"] = new(
            "Node Awakening",
            "Awaken every dormant leyline in the region. Reshape the magical landscape; permanently alter the area's properties.",
            "⍟", "⊛⊕◉⍥⊛⊕◉⍥", "#44aacc"),
        ["Eldritch"] = new(
            "Star Spawn Revelation",
            "Tear the veil between realities. Entities from beyond flood through. Even allies must save or be driven mad.",
            "⊛", "⊗⊙◈⍚⊗⊙◈⍚", "#cc8833"),
        ["Chaos"] = new(
            "Unmaking",
            "Reality itself unravels in 100ft. All rules, physical laws, and magical effects suspend. Anything can happen.",
            "⁂", "✸⊗⌬◈✸⊗⌬◈", "#ff5522"),
        ["Law"] = new(
            "Divine Edict",
            "Declare one absolute law. All creatures in the region must obey it as physical compulsion. Lasts 24 hours.",
            "⊞", "⊟⊠⊡◧⊟⊠⊡◧", "#8899aa"),
    };

    // ── Elements ─────────────────────────────────────────────────
    public static readonly IReadOnlyDictionary<string, ElementDef> Elements = new Dictionary<string, ElementDef>
    {
        ["Fire"] = new(
            "#FF4500", "🜂", "ᚠ",
            "Damage Type: Fire  |  +2 damage dice  |  Targets ignite (1d6/round)",
            "Fire element — burning, ignition, heat-based devastation.",
            new List<NodeDef>
            {
                new("Ignite",     1, "ᚠ", "🜂", "Targets ignite: 1d6 fire/turn",       "+1d6 ongoing fire"),
                new("Inferno",    2, "ᚱ", "⚡", "Blast radius +10ft",                  "+10ft area"),
                new("Searing",    2, "⊕", "✸", "Fire pierces cold resistance",         "Pierce cold resistance"),
                new("Conflagrate",3, "ᚷ", "☄", "Chain fire between targets",           "Chain to 2nd target"),
                new("Pyre",       3, "ᛁ", "✺", "Leave persistent fire zone",           "10ft fire zone 2 rounds"),
            }),
        ["Water"] = new(
            "#1E90FF", "🜄", "ᚹ",
            "Damage Type: Cold  |  Slow target (½ speed)  |  Extinguish fire",
            "Water element — cold, slowing, and flowing force.",
            new List<NodeDef>
            {
                new("Chill",     1, "ᚹ", "🜄", "Target slowed ½ speed",              "½ speed 1 round"),
                new("Freeze",    2, "ᛟ", "❄", "Target restrained on failed save",    "Restrained 1 round"),
                new("Torrent",   2, "ᚦ", "⌀", "Push target 20ft",                   "Push 20ft"),
                new("Maelstrom", 3, "⊙", "◉", "Pull all targets toward centre",      "Pull 15ft"),
                new("Deluge",    3, "ᛚ", "◎", "Douse all fire effects in area",      "Extinguish zone"),
            }),
        ["Earth"] = new(
            "#8B4513", "🜃", "ᚦ",
            "Damage Type: Bludgeoning  |  Knock Prone  |  +4 to Hit",
            "Earth element — weight, force, and immovability.",
            new List<NodeDef>
            {
                new("Tremor",  1, "ᚦ", "🜃", "Knock prone on hit",                "Knock prone"),
                new("Boulder", 2, "ᛒ", "⌬", "+1d8 bludgeoning damage",           "+1d8 damage"),
                new("Entomb",  2, "ᛗ", "⎔", "Restrain target in stone",          "Restrained 1 round"),
                new("Quake",   3, "ᚾ", "◈", "All in area fall prone",            "Area prone"),
                new("Monolith",3, "ᛜ", "⬡", "Create impassable stone wall",      "Stone wall 20ft"),
            }),
        ["Wind"] = new(
            "#87CEEB", "🜁", "ᚨ",
            "Damage Type: Thunder  |  Push 30ft  |  Fly Speed granted",
            "Wind element — thunder, movement, and airborne force.",
            new List<NodeDef>
            {
                new("Gust",    1, "ᚨ", "🜁", "Push target 15ft",                     "Push 15ft"),
                new("Updraft", 2, "ᚷ", "△", "Caster gains fly speed 30ft",           "Fly 30ft 1 round"),
                new("Deafen",  2, "ᛦ", "◌", "Thunder deafens on hit",               "Deafened 1 round"),
                new("Cyclone", 3, "ᛡ", "⍥", "Spin targets: disadvantage attacks",   "Disadvantage attacks"),
                new("Tempest", 3, "⊛", "⍉", "Lightning strikes random target",      "Random lightning 2d6"),
            }),
        ["Celestial"] = new(
            "#FFD700", "✦", "⊕",
            "Choose Subtype: Radiant / Necrotic / Psychic",
            "Celestial element — divine or eldritch energy in three subtypes.",
            new List<NodeDef>
            {
                new("Hallow",   1, "⊕", "✦", "Area becomes sacred ground",            "Hallowed zone"),
                new("Smite",    2, "☉", "✸", "+1d8 radiant/necrotic/psychic",         "+1d8 typed damage"),
                new("Ward",     2, "☽", "⊚", "Allies gain +2 AC in aura",             "+2 AC aura"),
                new("Ascend",   3, "⊛", "⍜", "Temporary invulnerability 1 round",     "Invulnerable 1 round"),
                new("Judgment", 3, "⊗", "⍚", "Double damage vs opposite alignment",   "×2 vs opposed"),
            },
            new Dictionary<string, SubtypeDef>
            {
                ["Radiant"]  = new("#FFFFAA", "☉",
                    "Radiant damage  |  Heal allies = damage dealt  |  Undead ×2",
                    "Divine light heals allies and destroys undead."),
                ["Necrotic"] = new("#AA44FF", "☽",
                    "Necrotic damage  |  Drain max HP  |  Empower undead",
                    "Life drain reduces max HP and strengthens undead."),
                ["Psychic"]  = new("#FF88FF", "⊚",
                    "Psychic damage  |  Stun 1 round  |  Int save or confused",
                    "Mind-shattering force stuns and confuses targets."),
            }),
    };

    // ── Element connections ───────────────────────────────────────
    public static readonly IReadOnlyDictionary<(string, string), string> ElementConnections =
        new Dictionary<(string, string), string>
        {
            [("Fire","Water")]     = "Steam — scalding vapour, obscures vision, burns through armour",
            [("Fire","Earth")]     = "Magma — molten rock, unstoppable, leaves scorched terrain",
            [("Fire","Wind")]      = "Firestorm — whirling inferno, empowers all fire effects",
            [("Fire","Celestial")] = "Divine Flame — purifying holy fire, destroys undead",
            [("Water","Earth")]    = "Mud — entangles and slows targets, difficult terrain",
            [("Water","Wind")]     = "Ice Storm — freezing blizzard, area slow, slick surfaces",
            [("Water","Celestial")]= "Holy Water — purifies corruption, disrupts fiends",
            [("Earth","Wind")]     = "Dust Devil — blinding dust, concealment, minor damage",
            [("Earth","Celestial")]= "Sacred Stone — permanent glyphs etched in unbreakable rock",
            [("Wind","Celestial")] = "Tempest — divine storm, deafens, discharges radiant lightning",
        };

    // ── Sub-element nodes ─────────────────────────────────────────
    public static readonly IReadOnlyDictionary<(string, string), NodeDef[]> SubelementNodes =
        new Dictionary<(string, string), NodeDef[]>
        {
            [("Fire","Water")] =
            [
                new("Steam Veil", 1, "ᚠ", "⌀", "Obscuring steam cloud 10ft",    "Obscured zone"),
                new("Scald",      2, "ᚹ", "◎", "Fire+Cold combo: 1d6 each",     "+1d6 fire +1d6 cold"),
                new("Geyser",     3, "⊕", "✺", "Erupting steam pillar, push+burn","Push+ignite"),
            ],
            [("Fire","Earth")] =
            [
                new("Lava Crack",  1, "ᚦ", "⌬", "Molten fissure in ground",          "Difficult terrain+fire"),
                new("Magma Surge", 2, "ᚠ", "☄", "Eruption: 2d6 fire+bludgeon",       "2d6 hybrid damage"),
                new("Slag",        3, "ᛒ", "⎔", "Solidify target in cooled lava",    "Restrained permanent"),
            ],
            [("Fire","Wind")] =
            [
                new("Firestorm",    1, "ᚨ", "✸", "Spinning fire 20ft radius",     "20ft fire zone"),
                new("Phoenix Wing", 2, "ᚱ", "⍥", "Fire+fly: blazing charge",      "Fly+fire trail"),
                new("Inferno Wind", 3, "⊛", "⊗", "All fire effects maximised",    "Max fire damage"),
            ],
            [("Fire","Celestial")] =
            [
                new("Seraphs Flame",1, "☉", "✦", "Divine fire heals allies",          "Fire heals allies"),
                new("Purgation",    2, "⊕", "⊕", "Burn away curses and conditions",   "Remove 1 condition"),
                new("Immolation",   3, "⊗", "☄", "Full body fire halo, aura damage",  "5ft fire aura"),
            ],
            [("Water","Earth")] =
            [
                new("Mudslide",     1, "ᚦ", "🜃", "Engulf area in thick mud",         "Restrained+difficult"),
                new("Bog Pull",     2, "ᚹ", "◉", "Sink target, hold prone",           "Prone+restrained"),
                new("Earthen Wave", 3, "ᛗ", "◈", "Tidal wave of mud: area+pull",      "Area pull+prone"),
            ],
            [("Water","Wind")] =
            [
                new("Blizzard",  1, "ᚨ", "❄", "Snowstorm reduces visibility",  "Heavily obscured"),
                new("Ice Lance", 2, "ᛟ", "⌀", "Piercing ice spear, ignore AC", "+2d6 pierce+cold"),
                new("Hailstorm", 3, "⊙", "⍉", "Hail: stun+prone+cold",         "Area stun+prone"),
            ],
            [("Water","Celestial")] =
            [
                new("Holy Font",    1, "☉", "🜄", "Blessed water purifies",        "Remove poison/curse"),
                new("Tide of Life", 2, "⊕", "◎", "Healing wave to all allies",    "Heal 2d6 allies"),
                new("Absolution",   3, "⊛", "✦", "Wash away undeath",             "Destroy undead ≤CR5"),
            ],
            [("Earth","Wind")] =
            [
                new("Dust Devil", 1, "ᚨ", "⍥", "Blinding dust vortex",         "Blinded 1 round"),
                new("Rock Gust",  2, "ᚦ", "⌬", "Debris launch: 1d10 piercing", "1d10 piercing"),
                new("Landslide",  3, "ᚷ", "△", "Massive debris avalanche",      "Area prone+buried"),
            ],
            [("Earth","Celestial")] =
            [
                new("Sacred Stone",1, "⊕", "🜃", "Permanent ward glyph in earth",  "Permanent ward"),
                new("Rune Seal",   2, "☉", "⊠", "Anti-magic zone in stone",       "Anti-magic zone"),
                new("Earthen Tomb",3, "⊗", "⎔", "Entomb target in holy stone",    "Imprisoned"),
            ],
            [("Wind","Celestial")] =
            [
                new("Tempest Call",1, "☙", "🜁", "Divine storm arrives",                      "Storm zone 30ft"),
                new("Angel Rush",  2, "⊕", "△", "Celestial speed: move+attack twice",         "Extra move+attack"),
                new("Maelstrom",   3, "⊛", "⍚", "Divine whirlwind: lift+radiant",             "Fly+radiant aura"),
            ],
        };

    // ── Schools ───────────────────────────────────────────────────
    /// <summary>Domain symbol + accent color for each of the 5 domains.</summary>
    public static readonly IReadOnlyDictionary<string, DomainDef> DomainInfo =
        new Dictionary<string, DomainDef>
        {
            ["Dark"]   = new("☠",  "#bb1133"),
            ["Light"]  = new("✦",  "#ccbb33"),
            ["Nature"] = new("⚘",  "#338833"),
            ["Planar"] = new("⌛",  "#3355bb"),
            ["Arcane"] = new("ᚠ",  "#4488aa"),
        };

    /// <summary>Domain name → ordered list of its 5 school names.</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> SchoolDomains =
        new Dictionary<string, IReadOnlyList<string>>
        {
            ["Dark"]   = ["Blood","Death","Shadow","Undeath","Curse"],
            ["Light"]  = ["Healing","Shielding","Banishment","Radiance","Revelation"],
            ["Nature"] = ["Elemental","Plants","Animals","Aura","Storm"],
            ["Planar"] = ["Time","Space","Summon","Dream","Void"],
            ["Arcane"] = ["Names","Leyline","Eldritch","Chaos","Law"],
        };

    public static readonly IReadOnlyList<string> SchoolOrder =
        ["Blood","Death","Shadow","Undeath","Curse",
         "Healing","Shielding","Banishment","Radiance","Revelation",
         "Elemental","Plants","Animals","Aura","Storm",
         "Time","Space","Summon","Dream","Void",
         "Names","Leyline","Eldritch","Chaos","Law"];

    public static readonly IReadOnlyDictionary<string, SchoolDef> Schools = new Dictionary<string, SchoolDef>
    {
        // ── Dark Magic ────────────────────────────────────────────
        ["Blood"] = new(
            "#c82038", "⬥", "Manipulate blood and life force",
            "Blood magic wields the very essence of life as a weapon. Blood mages drain foes to fuel their power, puppet victims through their own veins, and weave hemorrhages that no armour can stop.",
            new Dictionary<string, string>
            {
                ["Range"] = "Blood Range", ["Duration"] = "Hemorrhage Duration",
                ["Area"]  = "Blood Field",  ["Power"]   = "Life Drain",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Hemorrhage"]       = new(1, "Target bleeds: 1d4 damage per round until healed. Each purchase adds 1d4."),
                ["Blood Drain"]      = new(2, "Gain temporary HP equal to damage dealt per purchase."),
                ["Vital Strike"]     = new(1, "Gain +1 damage die against any target already bleeding per purchase."),
                ["Blood Puppet"]     = new(2, "Control target on failed CON save. Each purchase extends duration 1 round."),
                ["Crimson Armor"]    = new(2, "Animate surrounding blood as a shield; +2 AC per purchase."),
                ["Blood Trace"]      = new(1, "Track any creature you have drawn blood from across any distance."),
                ["Hemorrhagic Wave"] = new(2, "Erupt blood from a bleeding creature in a 15ft burst per purchase."),
                ["Coagulate"]        = new(3, "Instantly stop all bleeding on a target, or force clotting in an enemy's wounds."),
                ["Blood Pact"]       = new(3, "Bind two creatures; 50% of damage one takes transfers to the other."),
                ["Exsanguinate"]     = new(4, "Drain all blood from a target. Instant death on failed CON save."),
            }),
        ["Death"] = new(
            "#445566", "☠", "Channel the power of death and entropy",
            "Death magic taps into the inexorable force of mortality. Death mages sense the dying, drain life in a glance, and call down the mark of the Reaper on those their finger points at.",
            new Dictionary<string, string>
            {
                ["Range"] = "Death Reach", ["Duration"] = "Necrosis Duration",
                ["Area"]  = "Death Field",  ["Power"]   = "Death Force",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Death Touch"]    = new(1, "+1d6 necrotic damage per purchase on a successful hit."),
                ["Life Sense"]     = new(1, "Sense all living creatures within range; know their HP totals."),
                ["Decay"]          = new(2, "Rot objects and structures rapidly; deals extra damage to constructs."),
                ["Soul Sight"]     = new(2, "See souls, spirits, and the recently dead as glowing forms."),
                ["Death Knell"]    = new(2, "Instantly kill any target at or below 10% of their max HP."),
                ["Necrotic Aura"]  = new(3, "Emit a 10ft aura dealing 1d6 necrotic per round to all nearby creatures."),
                ["Speak With Dead"]= new(2, "Commune with a corpse dead for up to 1 week per purchase."),
                ["Death Ward"]     = new(2, "Protected target stabilises at 0 HP once; rises with 1 HP per purchase."),
                ["Reaper's Mark"]  = new(3, "Marked target suffers +50% damage from all sources until mark removed."),
                ["Soul Harvest"]   = new(4, "Trap a soul on death; gain permanent +1 to all rolls per soul harvested."),
            }),
        ["Shadow"] = new(
            "#334455", "◗", "Shape darkness and move through shadows",
            "Shadow magic treats darkness as a physical medium. Shadow mages slip between shadows as easily as stepping through a doorway, and can plunge a battlefield into void-black silence.",
            new Dictionary<string, string>
            {
                ["Range"] = "Shadow Reach", ["Duration"] = "Darkness Duration",
                ["Area"]  = "Shadow Field",  ["Power"]   = "Obscuration",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Darkness"]        = new(1, "Create magical darkness in a 10ft radius per purchase; darkvision fails."),
                ["Shadow Step"]     = new(1, "Teleport between any two areas of shadow up to 60ft per purchase."),
                ["Umbral Cloak"]    = new(2, "Become invisible in dim light or darkness; auto-succeeds Stealth."),
                ["Shadow Blade"]    = new(2, "Conjure a weapon of pure shadow: +1d8 psychic damage per purchase."),
                ["Blinding Dark"]   = new(2, "Target blinded in magical darkness even with darkvision per purchase."),
                ["Shadow Puppet"]   = new(3, "Animate a target's shadow to attack them: 1d6 psychic per round."),
                ["Void Walk"]       = new(2, "Pass through solid objects by briefly entering the shadow plane."),
                ["Shade Form"]      = new(3, "Become incorporeal for 1 round; immune to all physical damage."),
                ["Nightmare Shroud"]= new(3, "Target simultaneously frightened and blinded until end of next turn."),
                ["Eclipse"]         = new(4, "Plunge the entire battlefield into impenetrable magical darkness."),
            }),
        ["Undeath"] = new(
            "#6a8822", "⚰", "Raise and command the undead",
            "Undeath magic tears the departed back from the grave and binds them to the caster's will. Masters of Undeath command armies of the risen and can walk the line of death themselves.",
            new Dictionary<string, string>
            {
                ["Range"] = "Reanimate Range", ["Duration"] = "Undead Duration",
                ["Area"]  = "Corpse Field",     ["Power"]   = "Rot Force",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Reanimate"]       = new(1, "Raise a corpse as a zombie or skeleton. Each purchase unlocks a higher undead tier."),
                ["Undead Control"]  = new(1, "Control one additional undead creature per purchase."),
                ["Necrotic Infusion"]= new(2, "Empower a controlled undead: +1d6 damage or +2 AC per purchase."),
                ["Corpse Explosion"] = new(2, "Detonate a corpse in a 10ft burst; 2d6 necrotic per purchase."),
                ["Bone Shield"]      = new(2, "Animate bone fragments as armour; +2 AC per purchase."),
                ["Death Speech"]     = new(1, "Controlled undead can speak and relay information to the caster."),
                ["Dark Resurrection"]= new(3, "Raise a creature as intelligent undead retaining its memories."),
                ["Undying Tide"]     = new(3, "Slain undead rise again after 1 round with half HP."),
                ["Lichcraft"]        = new(4, "Transfer your soul to a phylactery; your body is destroyed but you persist."),
                ["Undead Army"]      = new(4, "Raise all corpses in a 60ft radius simultaneously."),
            }),
        ["Curse"] = new(
            "#8833aa", "⊗", "Lay hexes, blights, and supernatural misfortune",
            "Curse magic rewrites fate against a target. Curse mages stack misfortunes, wither vitality, and plant hexes that spread like plague. The most potent curses follow the marked across planes.",
            new Dictionary<string, string>
            {
                ["Range"] = "Curse Range", ["Duration"] = "Hex Duration",
                ["Area"]  = "Blight Field", ["Power"]   = "Affliction",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Hex"]            = new(1, "Target has disadvantage on chosen ability checks per purchase."),
                ["Misfortune"]     = new(2, "Target rolls all dice twice and takes the worse result."),
                ["Wither"]         = new(1, "Reduce target's max HP by 1d6 per purchase until they rest."),
                ["Cursed Mark"]    = new(2, "Curse transfers to the creature that kills the marked target."),
                ["Binding Curse"]  = new(2, "Target cannot use one chosen ability or action per purchase."),
                ["Plague Curse"]   = new(2, "Curse spreads to creatures that touch the afflicted target."),
                ["Doom"]           = new(3, "Target dies at end of duration if curse not removed. No damage, just death."),
                ["Entropy Curse"]  = new(3, "Target's equipment degrades: -1 to AC and attack rolls per round."),
                ["Soul Blight"]    = new(3, "Permanently reduce a chosen ability score by 2 until magically cured."),
                ["True Name Hex"]  = new(4, "Use target's true name to make the curse impossible to remove by normal means."),
            }),

        // ── Light Magic ───────────────────────────────────────────
        ["Healing"] = new(
            "#44cc88", "✚", "Restore vitality and cure afflictions",
            "Healing magic mends flesh, banishes sickness, and recalls the dying from the edge. The most powerful Healers can reverse death itself, restoring the slain as if they had never fallen.",
            new Dictionary<string, string>
            {
                ["Range"] = "Heal Range", ["Duration"] = "Restoration Duration",
                ["Area"]  = "Healing Field", ["Power"]  = "Mend Potency",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Restore HP"]         = new(1, "Heal 1d6 HP per purchase on a successful casting."),
                ["Mend Wounds"]        = new(1, "Cure ongoing bleed, poison, or disease effects per purchase."),
                ["Greater Heal"]       = new(2, "Add +1d8 to all healing rolls from this spell per purchase."),
                ["Regeneration"]       = new(3, "Target regenerates 1d4 HP per round per purchase for the duration."),
                ["Remove Condition"]   = new(2, "Cure one condition (blinded, poisoned, stunned, etc.) per purchase."),
                ["Healing Aura"]       = new(2, "Radiate a 10ft healing aura; allies regain 1 HP per round per purchase."),
                ["Revivify"]           = new(3, "Restore a creature to 1 HP if they died within the last round."),
                ["Mass Heal"]          = new(3, "Simultaneously heal all allies in range for the full amount."),
                ["Resurrection"]       = new(4, "Return a creature from death. They rise with half their max HP."),
                ["True Restoration"]   = new(4, "Remove curses, diseases, permanent damage, and reverse magical aging."),
            }),
        ["Shielding"] = new(
            "#4488ff", "⛨", "Create magical barriers and protective wards",
            "Shielding magic raises barriers between harm and the defended. Shield mages can wrap allies in impenetrable fields, reflect hostile spells, and make a fortress of any location.",
            new Dictionary<string, string>
            {
                ["Range"] = "Ward Range", ["Duration"] = "Shield Duration",
                ["Area"]  = "Barrier Coverage", ["Power"] = "Ward Strength",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Ward"]           = new(1, "Create a magical barrier with 10 HP per purchase."),
                ["Shield Bonus"]   = new(1, "+1 AC bonus to the target per purchase while the spell is active."),
                ["Damage Reduction"]= new(2, "Reduce all incoming damage by 1d4 per purchase."),
                ["Spell Reflect"]  = new(2, "Blocked spells reflect back at the caster per purchase."),
                ["Aegis"]          = new(2, "Grant immunity to one chosen damage type per purchase."),
                ["Sanctuary"]      = new(3, "Attackers must succeed on a WIS save or be unable to target protected creatures."),
                ["Spell Barrier"]  = new(2, "Automatically attempt to counterspell one incoming spell per round."),
                ["Iron Skin"]      = new(3, "Harden a creature's flesh; +5 AC and resistance to physical damage."),
                ["Impenetrable"]   = new(4, "Grant full damage immunity for 1 round per purchase."),
                ["Fortress"]       = new(4, "Create an immovable ward over an area; nothing passes in or out."),
            }),
        ["Banishment"] = new(
            "#cc8844", "⊘", "Exorcise, dispel, and exile otherworldly forces",
            "Banishment magic drives out what does not belong. Banishment mages are anathema to fiends, spirits, and the undead — and the most skilled can seal a planar intrusion permanently.",
            new Dictionary<string, string>
            {
                ["Range"] = "Banish Range", ["Duration"] = "Exile Duration",
                ["Area"]  = "Holy Field",   ["Power"]    = "Banish Potency",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Exorcise"]       = new(1, "Drive out spirits, fiends, or celestials. Each purchase raises the CR cap."),
                ["Holy Brand"]     = new(1, "Branded target takes +1d6 radiant damage per purchase on each hit."),
                ["Turn Undead"]    = new(2, "Cause undead to flee on failed WIS save per purchase."),
                ["Dispel Magic"]   = new(2, "End one active magical effect per purchase."),
                ["Exile"]          = new(3, "Send a target to its home plane; it cannot voluntarily return."),
                ["Holy Ground"]    = new(2, "Sanctify an area; undead and fiends cannot enter."),
                ["True Strike"]    = new(2, "+5 to attack and damage rolls against unholy creatures per purchase."),
                ["Consecrate"]     = new(2, "Nullify all dark magic in a zone for the duration."),
                ["Planar Lock"]    = new(3, "Prevent all planar travel, teleportation, and summoning in area."),
                ["Absolute Exile"] = new(4, "Banish target to a sealed demiplane; no means of return exists."),
            }),
        ["Radiance"] = new(
            "#ffdd44", "☀", "Channel blinding light and purifying radiance",
            "Radiance magic embodies light in its most overwhelming form. Radiance mages scorch the undead, blind foes with solar flares, and emit a presence so bright enemies crumble before them.",
            new Dictionary<string, string>
            {
                ["Range"] = "Light Range", ["Duration"] = "Radiance Duration",
                ["Area"]  = "Illumination", ["Power"]   = "Sear Potency",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Light Beam"]      = new(1, "+1d6 radiant damage per purchase on a successful hit."),
                ["Blinding Flash"]  = new(2, "Target blinded for 1 round on failed CON save per purchase."),
                ["Illuminate"]      = new(1, "Reveal all invisible and hidden creatures in area per purchase radius."),
                ["Searing Light"]   = new(2, "Deal double damage to undead, shadow creatures, and darkness-dwellers."),
                ["Halo"]            = new(2, "Radiant aura; adjacent allies regain 1d4 HP per round per purchase."),
                ["Solar Flare"]     = new(3, "AoE radiant burst; destroys all magical darkness in area."),
                ["Purify"]          = new(2, "Remove poisons and diseases from targets in area per purchase."),
                ["Beacon"]          = new(2, "All attacks against the marked target gain advantage for the duration."),
                ["Sunburst"]        = new(3, "Massive radiant explosion; all in area blinded and take 4d6 radiant."),
                ["Divine Radiance"] = new(4, "All your attacks become radiant and ignore resistance and immunity."),
            }),
        ["Revelation"] = new(
            "#ffaa22", "◎", "Pierce illusions and reveal hidden truths",
            "Revelation magic strips away deception. Revelation mages see through disguise, illusion, and lie — and can grant their allies the gift of perfect foresight in the moment before catastrophe.",
            new Dictionary<string, string>
            {
                ["Range"] = "Sight Range", ["Duration"] = "Vision Duration",
                ["Area"]  = "Clarity Field", ["Power"]  = "Truth Depth",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Truth Sight"]       = new(1, "See invisible creatures, pierce illusions, and see true forms per purchase."),
                ["Mind Read"]         = new(2, "Read the surface thoughts of a willing or failing target."),
                ["Foresight"]         = new(2, "+5 bonus to AC and saving throws for 1 round per purchase."),
                ["Expose Weakness"]   = new(2, "Reveal target's lowest saving throw; allies gain advantage against it."),
                ["True Name"]         = new(3, "Learn a creature's true name; gain magical authority over it."),
                ["Oracle Vision"]     = new(2, "See 1d4 rounds into the future per purchase; act on that knowledge."),
                ["Magical Audit"]     = new(1, "Identify all magical effects, items, and auras on a target or area."),
                ["See All"]           = new(3, "Gain 360-degree vision; you cannot be surprised."),
                ["Revelation Burst"]  = new(3, "Strip all illusions and false identities from every creature in area."),
                ["Omniscience"]       = new(4, "Ask one factual question; receive a complete and truthful answer."),
            }),

        // ── Nature Magic ──────────────────────────────────────────
        ["Elemental"] = new(
            "#ff8833", "⌬", "Wield the four primal elemental forces",
            "Elemental magic channels the raw primal forces of fire, water, earth, and wind. Elemental mages freely switch between elements and can merge multiple forces into devastating combinations.",
            new Dictionary<string, string>
            {
                ["Range"] = "Element Range", ["Duration"] = "Force Duration",
                ["Area"]  = "Element Field",  ["Power"]   = "Raw Force",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Element Channel"]     = new(1, "Add one additional elemental damage type to this spell per purchase."),
                ["Primal Force"]        = new(1, "+1 damage die of the chosen element per purchase."),
                ["Element Surge"]       = new(2, "Double the damage dice of one chosen element for this casting."),
                ["Elemental Resilience"]= new(2, "Gain resistance to one element of your choice per purchase."),
                ["Elemental Form"]      = new(3, "Transform into an elemental of chosen type for 1 minute."),
                ["Elemental Summon"]    = new(2, "Call an elemental creature to serve you per purchase CR tier."),
                ["Elemental Affinity"]  = new(2, "Chosen element spells cost 1 fewer point to cast per purchase."),
                ["Wild Element"]        = new(3, "Unleash chaotic burst of all four elements simultaneously in area."),
                ["Master Channel"]      = new(3, "Channel two different elements simultaneously in a single spell."),
                ["Primal Manifestation"]= new(4, "Briefly manifest an avatar of primal elemental force in the area."),
            }),
        ["Plants"] = new(
            "#44aa44", "⚘", "Grow, animate, and weaponise plant life",
            "Plants magic commands living vegetation. Plants mages entangle enemies in sudden undergrowth, raise forests overnight, and breathe spores that confuse or poison any who inhale them.",
            new Dictionary<string, string>
            {
                ["Range"] = "Growth Range", ["Duration"] = "Entangle Duration",
                ["Area"]  = "Growth Field",  ["Power"]   = "Root Force",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Entangle"]           = new(1, "Restrain targets in vines in a 10ft radius per purchase."),
                ["Rapid Growth"]       = new(1, "Grow plants up to 10ft tall per purchase in seconds."),
                ["Thornwall"]          = new(2, "Raise a wall of thorns; 2d4 piercing to any who pass through."),
                ["Spore Cloud"]        = new(2, "Release a cloud of spores; confused or poisoned on failed CON save."),
                ["Living Wood"]        = new(2, "Animate trees and wooden objects as servants per purchase."),
                ["Photosynthetic Heal"]= new(2, "Restore HP to targets in sunlight; 1d6 per round per purchase."),
                ["Root Network"]       = new(3, "Sense and communicate through plant root networks in area."),
                ["Overgrowth"]         = new(3, "Cover a 30ft area in impassable dense growth instantly."),
                ["Seed of Life"]       = new(3, "Plant a seed that grows into a protective healing tree over 1 minute."),
                ["World Tree"]         = new(4, "Manifest a massive tree; all allies in area regenerate HP each round."),
            }),
        ["Animals"] = new(
            "#aa6633", "♞", "Commune with and command beasts",
            "Animals magic bridges the gap between civilised caster and wild beast. Animals mages speak with any creature, summon packs on a word, and can temporarily don the senses of an eagle or wolf.",
            new Dictionary<string, string>
            {
                ["Range"] = "Summon Range", ["Duration"] = "Bond Duration",
                ["Area"]  = "Pack Field",    ["Power"]   = "Beast Power",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Beast Call"]    = new(1, "Summon animals of appropriate type per purchase CR tier."),
                ["Animal Speech"] = new(1, "Speak with and fully understand all animals."),
                ["Pack Tactics"]  = new(2, "Summoned animals gain pack-tactics advantage on attack rolls per purchase."),
                ["Bestial Roar"]  = new(2, "Frighten all enemies in area on failed WIS save per purchase."),
                ["Wild Sense"]    = new(1, "Gain one beast sense (scent, echolocation, infrared) per purchase."),
                ["Beast Bond"]    = new(2, "Form a permanent telepathic bond with one animal companion."),
                ["Feral Strike"]  = new(2, "Gain natural attacks of a chosen beast type; +1d6 per purchase."),
                ["Alpha Command"] = new(3, "Any beast obeys single-word commands without taming or roll."),
                ["Swarm Form"]    = new(3, "Transform into a swarm of insects for 1 minute; immune to single-target effects."),
                ["Great Beast"]   = new(4, "Summon a legendary beast (dragon, roc, or kraken tier) to serve you."),
            }),
        ["Aura"] = new(
            "#88ccaa", "◌", "Project and read magical aura fields",
            "Aura magic senses and manipulates the invisible field that all living things emit. Aura mages heal allies through resonance, shatter enemy concentration, and expand their presence across a battlefield.",
            new Dictionary<string, string>
            {
                ["Range"] = "Aura Range", ["Duration"] = "Aura Duration",
                ["Area"]  = "Aura Radius", ["Power"]   = "Aura Potency",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Aura Sense"]      = new(1, "Read the magical and emotional aura of any creature or object."),
                ["Project Aura"]    = new(2, "Extend your aura outward to affect nearby creatures per purchase radius."),
                ["Aura Shield"]     = new(2, "Your aura blocks all mind-affecting magic targeting you per purchase."),
                ["Resonant Aura"]   = new(2, "Aura deals 1d4 damage to enemies that enter or start their turn in it."),
                ["Cleansing Field"] = new(2, "Remove conditions from allies within your aura radius per purchase."),
                ["Empowering Aura"] = new(3, "Allies within aura gain +1d4 to all attack and ability rolls per purchase."),
                ["Aura Overload"]   = new(3, "Expand aura to 40ft radius and knock all enemies in it prone."),
                ["Soul Reading"]    = new(2, "Read the alignment, intent, and deepest desire of a target via their aura."),
                ["Aura Transfer"]   = new(3, "Give your protective aura entirely to another creature for the duration."),
                ["Convergent Field"]= new(4, "Merge all allies' auras into one overwhelming field; enemies repelled."),
            }),
        ["Storm"] = new(
            "#6677cc", "⚡", "Command lightning, thunder, and violent weather",
            "Storm magic channels the raw energy of the sky. Storm mages call lightning with a gesture, send hurricanes to scatter armies, and ride the thunderhead as easily as walking.",
            new Dictionary<string, string>
            {
                ["Range"] = "Storm Range", ["Duration"] = "Tempest Duration",
                ["Area"]  = "Storm Field",  ["Power"]   = "Lightning Force",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Lightning Bolt"]   = new(1, "+1d6 lightning damage per purchase on a successful hit."),
                ["Thunderclap"]      = new(2, "Deafen and knock prone on failed CON save per purchase."),
                ["Storm Stride"]     = new(1, "Move through storm or rain as a 40ft teleport per purchase."),
                ["Gale Force"]       = new(2, "Wind pushes all targets in arc 20ft per purchase."),
                ["Static Charge"]    = new(2, "Electrify target; next creature to hit it takes 1d6 lightning."),
                ["Storm Sense"]      = new(1, "Perfect weather sense; you are immune to all storm and weather effects."),
                ["Squall"]           = new(3, "Raise a localised storm in 30ft; ongoing lightning and hail each round."),
                ["Chain Lightning"]  = new(2, "Lightning arcs to an additional target at half damage per purchase."),
                ["Hurricane Force"]  = new(3, "Massive wind lifts all Large or smaller creatures off the ground."),
                ["Tempest God"]      = new(4, "Full control over weather in a 10-mile radius for 1 hour."),
            }),

        // ── Planar Magic ──────────────────────────────────────────
        ["Time"] = new(
            "#8888ff", "⌛", "Manipulate the flow and fabric of time",
            "Time magic bends the river of moments. Time mages slow enemies to a crawl, snap allies into blurs of action, unravel recent events, and at the peak of mastery, step entirely outside the timestream.",
            new Dictionary<string, string>
            {
                ["Range"] = "Temporal Reach", ["Duration"] = "Effect Duration",
                ["Area"]  = "Time Field",      ["Power"]   = "Chronos Force",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Time Slow"]      = new(1, "Reduce target's speed and number of actions per purchase."),
                ["Haste"]          = new(1, "Grant target extra speed and an additional action per purchase."),
                ["Rewind"]         = new(2, "Reverse time on a target or object by 1 round per purchase."),
                ["Time Stop"]      = new(3, "Freeze time; each purchase adds one additional frozen round."),
                ["Age Strike"]     = new(2, "Age target 10 years per purchase (reversed by Remove Curse)."),
                ["Time Loop"]      = new(3, "Create a time loop; max 3 repetitions base, +1 per purchase."),
                ["Phase Step"]     = new(2, "Phase out of timestream as a reaction to avoid one attack."),
                ["Future Echo"]    = new(2, "See 1 round into own future; gain advantage on all rolls this round."),
                ["Paradox Step"]   = new(4, "Act twice in one round by borrowing time from an alternate timeline."),
                ["Entropy Field"]  = new(3, "Objects and structures crumble at accelerated rate per round."),
            }),
        ["Space"] = new(
            "#445599", "⍥", "Bend, fold, and traverse spatial dimensions",
            "Space magic treats distance as a malleable medium. Space mages compress rooms, redirect incoming attacks through pocket rifts, and step between locations as a human steps through a door.",
            new Dictionary<string, string>
            {
                ["Range"] = "Portal Range", ["Duration"] = "Fold Duration",
                ["Area"]  = "Rift Breadth",  ["Power"]   = "Spatial Distortion",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Blink"]               = new(1, "Teleport up to 30ft per purchase as part of movement."),
                ["Portal"]              = new(2, "Open a stable portal between two points per purchase distance."),
                ["Pocket Dimension"]    = new(2, "Access extradimensional storage; ×10 capacity per purchase."),
                ["Spatial Compression"] = new(2, "Compress an area; creatures inside take damage per purchase."),
                ["Redirect"]            = new(2, "Micro-portal redirects one incoming attack as a reaction."),
                ["Gravity Shift"]       = new(3, "Alter local gravity vector in a 20ft cube per purchase."),
                ["Dimensional Anchor"]  = new(3, "Prevent all teleportation and planar travel in zone."),
                ["Phase Walk"]          = new(2, "Move through solid objects by briefly entering folded space."),
                ["Void Rift"]           = new(4, "Open a vacuum rift; pulls creatures in and deals 2d8 per round."),
                ["Infinite Fold"]       = new(4, "Collapse all distance to zero; teleport to any point instantly."),
            }),
        ["Summon"] = new(
            "#cc66aa", "★", "Call entities, objects, and substances from afar",
            "Summon magic reaches across planes to drag beings and materials into the caster's presence. Summon mages bind powerful entities with iron contracts and open permanent gates to distant realms.",
            new Dictionary<string, string>
            {
                ["Range"] = "Summon Range", ["Duration"] = "Binding Duration",
                ["Area"]  = "Conjure Area",  ["Power"]   = "Entity Tier",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Call Creature"]    = new(1, "Summon a creature per purchase CR tier."),
                ["Bind"]             = new(2, "Bound creature cannot disobey direct commands per purchase."),
                ["Summon Count"]     = new(1, "Summon one additional creature simultaneously per purchase."),
                ["Duration Bond"]    = new(1, "Extend how long summoned creatures remain per purchase."),
                ["Shared Senses"]    = new(2, "Establish a telepathic link with one summoned creature at any range."),
                ["Material Conjure"] = new(1, "Conjure a non-magical substance; double volume per purchase."),
                ["Planar Gate"]      = new(3, "Open a two-way portal to a target plane."),
                ["Sacrifice Empower"]= new(2, "Spend HP to empower a summoned creature; +1d6 to its attacks per purchase."),
                ["Permanent Binding"]= new(4, "Summoned creature bound permanently with no duration limit."),
                ["Legion Call"]      = new(4, "Summon up to 20 creatures simultaneously in a single casting."),
            }),
        ["Dream"] = new(
            "#9966cc", "◑", "Enter and shape the realm of dreams",
            "Dream magic bridges the waking world and the realm of sleep. Dream mages send their minds into others' dreams, create shared dreamscapes, and at the apex can tear dream matter into waking reality.",
            new Dictionary<string, string>
            {
                ["Range"] = "Dream Reach", ["Duration"] = "Vision Duration",
                ["Area"]  = "Dream Field",  ["Power"]   = "Oneiric Force",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Sleep"]           = new(1, "Target falls asleep on failed WIS save per purchase."),
                ["Enter Dream"]     = new(2, "Enter and experience a sleeping creature's active dream."),
                ["Nightmare"]       = new(2, "Inflict a terrifying nightmare; target frightened on waking."),
                ["Dream Send"]      = new(2, "Send a vivid message through a sleeping creature's dream."),
                ["Lucid Walking"]   = new(2, "Physically move through the dream plane for 1 minute."),
                ["Dream Cage"]      = new(3, "Trap a target in a looping dream; they are helpless until freed."),
                ["Mass Sleep"]      = new(3, "Put all targets in area to sleep simultaneously per purchase."),
                ["Dream Shaping"]   = new(2, "Construct and control a full dream-world environment."),
                ["Dreamsteal"]      = new(3, "Extract a memory or trained skill from a sleeping target."),
                ["Dreamweave Reality"]= new(4, "Manifest one element of a dream as a physical object or effect."),
            }),
        ["Void"] = new(
            "#553388", "∅", "Harness nothingness and gravitational annihilation",
            "Void magic commands the spaces between everything. Void mages suppress magic in a gesture, drag enemies into crushing singularities, and can walk the null space between planes unharmed.",
            new Dictionary<string, string>
            {
                ["Range"] = "Void Reach",  ["Duration"] = "Null Duration",
                ["Area"]  = "Void Field",   ["Power"]   = "Entropy Force",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Null Field"]      = new(1, "Suppress all magic in a 10ft radius per purchase for duration."),
                ["Annihilate"]      = new(2, "+1d8 void damage; double against non-magical objects per purchase."),
                ["Dispel"]          = new(2, "End one active magical effect per purchase."),
                ["Gravity Well"]    = new(2, "Pull all creatures within 30ft toward a chosen point per purchase."),
                ["Void Touch"]      = new(2, "Strikes bypass all armour; target's armour AC bonus ignored."),
                ["Entropic Decay"]  = new(3, "Target's max HP reduced by 10% each round for the duration."),
                ["Null Cage"]       = new(3, "Imprison a target in void space; all magic fails while caged."),
                ["Anti-Magic"]      = new(4, "Create a full anti-magic zone; no magic functions in area."),
                ["Void Walk"]       = new(3, "Step into the void for 1 round; invisible and untargetable."),
                ["Singularity"]     = new(4, "Massive gravitational collapse in 100ft; nothing survives."),
            }),

        // ── Arcane Magic ──────────────────────────────────────────
        ["Names"] = new(
            "#aabbcc", "ᚠ", "Bind reality through the power of true names",
            "Names magic operates on the fundamental truth that everything that exists has a true name, and knowing it grants power. Names mages can bind, rename, curse, or erase a creature from existence.",
            new Dictionary<string, string>
            {
                ["Range"] = "Name Range", ["Duration"] = "Binding Duration",
                ["Area"]  = "Naming Field", ["Power"]  = "True Name Force",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Name Bind"]      = new(2, "Speak a target's true name to compel compliance on failed CHA save."),
                ["Rename"]         = new(3, "Alter a creature's name; this changes one of their core traits."),
                ["Name Ward"]      = new(2, "Protect your own true name from being known, spoken, or used against you."),
                ["Name Curse"]     = new(2, "Attach a curse to a target's name; it follows them across planes."),
                ["Language Mastery"]= new(1, "Speak and understand any one language per purchase."),
                ["Word of Power"]  = new(3, "Speak one word that stuns, sleeps, binds, or kills on failed save."),
                ["Name Erase"]     = new(4, "Erase a target's name from all records and all living memory."),
                ["Naming Magic"]   = new(2, "Speak a name for an object or creature to give it one new property."),
                ["Babel Curse"]    = new(2, "Target can only speak in a language no one present understands."),
                ["True Name Seal"] = new(4, "Lock a target's name in a seal; they cannot act against you while sealed."),
            }),
        ["Leyline"] = new(
            "#44aacc", "⍟", "Tap into and redirect the world's magical veins",
            "Leyline magic draws on the hidden channels of magical energy that run through every world. Leyline mages amplify their spells by tapping flowing power, and can reshape local magic permanently.",
            new Dictionary<string, string>
            {
                ["Range"] = "Line Range", ["Duration"] = "Channel Duration",
                ["Area"]  = "Ley Field",   ["Power"]   = "Leyline Potency",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Line Tap"]     = new(1, "Draw power from the nearest leyline; boost the next spell cast this turn."),
                ["Channel Flow"] = new(2, "Route leyline energy through your body; all spells empowered for 3 rounds."),
                ["Line Sight"]   = new(1, "Detect and map all leylines in a 1-mile area."),
                ["Energy Surge"] = new(2, "Leyline surge doubles the range or area of your next spell."),
                ["Disrupt Line"] = new(3, "Sabotage a leyline; suppress magic for all others in zone for 1 hour."),
                ["Line Anchor"]  = new(2, "Anchor to a leyline; maintain concentration spells without concentration checks."),
                ["Convergence"]  = new(3, "Draw two leylines together; massive power surge empowers all spells for 1 minute."),
                ["Ley Ward"]     = new(2, "Raise a self-sustaining ward powered by leyline; persists 1 hour without concentration."),
                ["Line Master"]  = new(3, "Control a leyline; redirect its energy to empower allies or weapons."),
                ["Node Awakening"]= new(4, "Awaken a dormant leyline node; transform the area's magical properties permanently."),
            }),
        ["Eldritch"] = new(
            "#cc8833", "⊛", "Channel forces from beyond known reality",
            "Eldritch magic draws power from the spaces outside reality. Eldritch mages wield force blasts that defy physics, warp geometry, and open rifts to the Far Realm from which sanity-breaking entities emerge.",
            new Dictionary<string, string>
            {
                ["Range"] = "Eldritch Range", ["Duration"] = "Aberrant Duration",
                ["Area"]  = "Eldritch Field",  ["Power"]   = "Cosmic Force",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Eldritch Blast"]    = new(1, "+1d8 force damage per purchase; ignores armour type."),
                ["Aberrant Form"]     = new(2, "Grow a monstrous feature (tentacle, eye, beak); gain its attack."),
                ["Madness Touch"]     = new(2, "Target suffers a random madness effect on failed INT save per purchase."),
                ["Void Gaze"]         = new(2, "Gaze attack; frightened and prone on failed WIS save."),
                ["Reality Crack"]     = new(3, "Tear a rift to the Far Realm; aberrant entities may emerge."),
                ["Mind Fracture"]     = new(2, "Shatter target's concentration; all active spells and effects end."),
                ["Eldritch Shroud"]   = new(2, "Wrap yourself in eldritch energy; melee attackers take 1d6 force."),
                ["Cosmic Horror"]     = new(3, "All creatures in area make WIS save or be incapacitated 1 round."),
                ["Unnatural Geometry"]= new(3, "Warp space in area; movement costs double and distances are unpredictable."),
                ["Star Spawn"]        = new(4, "Call an entity from beyond. It cannot be controlled and attacks all."),
            }),
        ["Chaos"] = new(
            "#ff5522", "⁂", "Unleash wild magic and entropic randomness",
            "Chaos magic is power without order. Chaos mages surf probability, detonate wild surges, and at the pinnacle can briefly unmake the rules that govern reality itself in their immediate vicinity.",
            new Dictionary<string, string>
            {
                ["Range"] = "Chaos Range", ["Duration"] = "Entropy Duration",
                ["Area"]  = "Chaos Field",  ["Power"]   = "Wild Force",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Wild Surge"]        = new(1, "Trigger a random magical effect on target or in area per purchase."),
                ["Chaos Bolt"]        = new(2, "Deal damage of a random type; rolls twice per purchase, take higher."),
                ["Entropic Burst"]    = new(2, "Randomly remove one condition per purchase from any creature in area."),
                ["Warp Reality"]      = new(3, "Reality briefly breaks; all rules suspended for 1 round in area."),
                ["Probability Shift"] = new(2, "Force a reroll of any one die result per purchase; take either result."),
                ["Luck Drain"]        = new(2, "Remove all luck bonuses from target; transfer them to yourself."),
                ["Random Polymorph"]  = new(3, "Transform target into a random creature form on failed CON save."),
                ["Cascade"]           = new(3, "Additional random magical effects erupt near target each round."),
                ["Chaos Form"]        = new(4, "Embody pure chaos for 1 minute; immune to all targeted effects."),
                ["Unmaking"]          = new(4, "Target partially unmade; loses one random ability score point each round."),
            }),
        ["Law"] = new(
            "#8899aa", "⊞", "Impose absolute order and binding commands",
            "Law magic encodes rules into the fabric of reality. Law mages bind enemies to stillness, compel truth, and can declare edicts that every creature in a region is physically incapable of breaking.",
            new Dictionary<string, string>
            {
                ["Range"] = "Edict Range", ["Duration"] = "Law Duration",
                ["Area"]  = "Order Field",  ["Power"]   = "Command Force",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Edict"]          = new(2, "Command target to follow one specific rule; violations cause 1d6 damage."),
                ["Binding Contract"]= new(2, "Magical contract; violations deal 2d6 damage to the breaker per purchase."),
                ["Order Field"]    = new(2, "No creature in area can act deceptively or chaotically per purchase."),
                ["Suppress"]       = new(1, "Suppress one magical effect in area per purchase."),
                ["Impose"]         = new(2, "Force target to take one specific action on their next turn."),
                ["Law Ward"]       = new(2, "Create a ward that only lawful creatures can pass through."),
                ["Immutable Law"]  = new(3, "Declare one rule; all in area must obey it as physical compulsion."),
                ["Truth Compel"]   = new(3, "Target cannot speak falsehoods for the duration; no saving throw."),
                ["Sentence"]       = new(3, "Magical judgment; target is incapacitated pending your verdict."),
                ["Divine Law"]     = new(4, "Declare one absolute law; becomes physical reality for 1 hour."),
            }),
    };

    // ── School connections (synergies) ────────────────────────────
    public static readonly IReadOnlyDictionary<(string, string), string> SchoolConnections =
        new Dictionary<(string, string), string>
        {
            // ── Dark Magic (within-domain) ─────────────────────────
            [("Blood","Death")]    = "Sanguine Reaping — each hit drains life force and hemorrhages simultaneously",
            [("Blood","Shadow")]   = "Crimson Veil — blood spray forms an obscuring cloud of dark vapour",
            [("Blood","Undeath")]  = "Vitae Tide — blood animated corpses rise stronger and hungrier",
            [("Blood","Curse")]    = "Corrupted Vein — curses spread through the target's own bloodstream",
            [("Death","Shadow")]   = "Wraith Form — death energy given shadow; caster becomes partially incorporeal",
            [("Death","Undeath")]  = "Perfected Undead — risen dead are more powerful and fully intelligent",
            [("Death","Curse")]    = "Doom Mark — death and curse fuse; marked target cannot be saved from death",
            [("Shadow","Undeath")] = "Shadow Horde — undead move and attack through shadows silently",
            [("Shadow","Curse")]   = "Haunting Hex — curse follows through darkness; cannot be avoided by light",
            [("Undeath","Curse")]  = "Blighted Corpse — risen undead carry and spread curses on their attacks",

            // ── Light Magic (within-domain) ────────────────────────
            [("Healing","Shielding")]  = "Regenerative Ward — shields restore HP to those within them each round",
            [("Healing","Banishment")] = "Purge and Mend — banishment expels the wound along with the spirit",
            [("Healing","Radiance")]   = "Luminous Restoration — healing also radiates light, damaging undead nearby",
            [("Healing","Revelation")] = "Diagnostic Vision — see exact injuries and ailments before healing",
            [("Shielding","Banishment")]= "Sacred Barrier — ward automatically triggers banishment on contact",
            [("Shielding","Radiance")] = "Mirror Shield — ward reflects radiant energy as a blinding flash",
            [("Shielding","Revelation")]= "Clarity Field — ward strips illusions and reveals all within its radius",
            [("Banishment","Radiance")]= "Divine Condemnation — banishment fueled by radiant light scorches as it exiles",
            [("Banishment","Revelation")]= "True Exile — revelation confirms target is otherworldly before banishing",
            [("Radiance","Revelation")]= "Solar Truth — radiant light strips deception from any it touches",

            // ── Nature Magic (within-domain) ───────────────────────
            [("Elemental","Plants")]  = "Storm Bloom — plants grow and wield elemental power on command",
            [("Elemental","Animals")] = "Primal Bond — summoned animals infused with elemental energy",
            [("Elemental","Aura")]    = "Elemental Resonance — aura radiates chosen element to all nearby",
            [("Elemental","Storm")]   = "Primal Tempest — unleash all four elements in a catastrophic storm",
            [("Plants","Animals")]    = "Living Forest — plants and animals work in concert as one ecosystem",
            [("Plants","Aura")]       = "Root Aura — aura extends through root networks across entire area",
            [("Plants","Storm")]      = "Seed Storm — storm scatters and instantly grows tangling plant life",
            [("Animals","Aura")]      = "Pack Aura — aura empowers all allied beasts in its radius",
            [("Animals","Storm")]     = "Wild Hunt — animals ride the storm, moving at double speed",
            [("Aura","Storm")]        = "Tempest Field — storm and aura merge; electricity arcs through the field",

            // ── Planar Magic (within-domain) ───────────────────────
            [("Time","Space")]    = "Chrono-Fold — teleport through both space and time simultaneously",
            [("Time","Summon")]   = "Summon From Past — call a creature or object from a previous moment",
            [("Time","Dream")]    = "Eternal Dreaming — trapped in a dream that replays forever",
            [("Time","Void")]     = "Decay Pulse — accelerate time to crumble targets to dust",
            [("Space","Summon")]  = "Dimensional Gate — summon directly from across any plane",
            [("Space","Dream")]   = "Mirror Realm — pocket dimension disguised as an empty dream space",
            [("Space","Void")]    = "Null Space — imprison a target in a folded void pocket",
            [("Summon","Dream")]  = "Dream Manifest — conjure matter pulled from within a shared dream",
            [("Summon","Void")]   = "Void Caller — summon entities native to the void between planes",
            [("Dream","Void")]    = "Dreamless Void — target enters a void sleep with no dreams; helpless",

            // ── Arcane Magic (within-domain) ───────────────────────
            [("Names","Leyline")]   = "Named Line — leyline responds only to one who speaks its true name",
            [("Names","Eldritch")]  = "True Horror — speak the true name of an eldritch entity to bind it",
            [("Names","Chaos")]     = "Unnamed Chaos — strip names from reality; all order collapses",
            [("Names","Law")]       = "True Name Seal — law enforced through the binding power of true names",
            [("Leyline","Eldritch")]= "Eldritch Tap — draw eldritch power from a corrupted leyline node",
            [("Leyline","Chaos")]   = "Surge Line — sabotaged leyline erupts in random wild magic surges",
            [("Leyline","Law")]     = "Law Line — leyline energy enforces an edict across its entire length",
            [("Eldritch","Chaos")]  = "Unravelling — eldritch and chaos fuse; reality dissolves in the area",
            [("Eldritch","Law")]    = "Forbidden Edict — an eldritch entity bound to enforce one absolute law",
            [("Chaos","Law")]       = "Entropic Order — law and chaos war in the area; unpredictable results",

            // ── Cross-domain synergies ─────────────────────────────
            [("Blood","Healing")]    = "Life Exchange — blood magic siphoned to fuel healing of allies",
            [("Death","Summon")]     = "Undead Summons — summon pre-raised undead from a stored cache",
            [("Shadow","Dream")]     = "Nightmare Realm — shadow pours into the dream plane; night terrors manifest",
            [("Curse","Law")]        = "Cursed Edict — law binds a curse permanently; no saving throw possible",
            [("Undeath","Time")]     = "Ageless Undead — risen dead are frozen in time; cannot be re-killed",
            [("Radiance","Storm")]   = "Divine Thunder — lightning strikes carry radiant energy; destroys undead",
            [("Revelation","Names")] = "True Naming — revelation reveals the true name; naming seals the power",
            [("Elemental","Chaos")]  = "Primal Chaos — elemental forces erupt randomly in all four types at once",
            [("Aura","Leyline")]     = "Ley Aura — aura draws from leyline energy; limitless, self-sustaining",
            [("Plants","Void")]      = "Entropy Growth — void-touched plants drain life from all around them",
            [("Animals","Time")]     = "Ancient Beast — summon a primordial animal ancestor of vast power",
            [("Shielding","Law")]    = "Iron Edict — ward enforced by law magic; violation punishes the attacker",
            [("Banishment","Void")]  = "Absolute Annihilation — banish to void; target ceases to exist entirely",
            [("Eldritch","Dream")]   = "Cosmic Nightmare — eldritch entities cross into and corrupt the dream plane",
            [("Leyline","Space")]    = "Dimensional Ley — leylines used as instant-travel conduits across the world",
        };

    // ── Global modifiers ─────────────────────────────────────────
    public static readonly IReadOnlyDictionary<string, ModDef> DefaultGlobalMods =
        new Dictionary<string, ModDef>
        {
            ["Range: Touch"]         = new("Range",    0, 1, "Requires physical contact."),
            ["Range: Close 10ft"]    = new("Range",    1, 1, "Short weapon-reach range."),
            ["Range: Medium 60ft"]   = new("Range",    2, 1, "Standard tactical combat range."),
            ["Range: Long 300ft"]    = new("Range",    3, 1, "Extreme battlefield range."),
            ["Range: Sight"]         = new("Range",    4, 1, "Anywhere you can directly see."),
            ["Range: Planar"]        = new("Range",    5, 1, "Can cross planar boundaries."),
            ["Duration: Instant"]    = new("Duration", 0, 1, "Single moment; no persistent magic."),
            ["Duration: 1 Round"]    = new("Duration", 1, 1, "Lasts until your next turn."),
            ["Duration: Concentration"] = new("Duration", 1, 1, "Maintained as long as you concentrate."),
            ["Duration: 1 Minute"]   = new("Duration", 2, 1, "10 combat rounds."),
            ["Duration: 10 Minutes"] = new("Duration", 3, 1, "Medium persistence."),
            ["Duration: 1 Hour"]     = new("Duration", 4, 1, "Long-lasting enhancement."),
            ["Duration: 8 Hours"]    = new("Duration", 5, 1, "Persists through a full rest."),
            ["Duration: Permanent"]  = new("Duration", 7, 1, "Persists until dispelled."),
            ["Area: Single Target"]  = new("Area",     0, 1, "Affects exactly one creature."),
            ["Area: Cone 15ft"]      = new("Area",     1, 1, "15-foot cone from caster."),
            ["Area: Line 30ft"]      = new("Area",     1, 1, "30-foot line, 5 feet wide."),
            ["Area: Burst 10ft"]     = new("Area",     2, 1, "10-foot radius sphere."),
            ["Area: Burst 30ft"]     = new("Area",     3, 1, "30-foot radius sphere."),
            ["Area: Multi-Target"]   = new("Area",     2, 5, "Choose up to 3 targets. Each purchase adds 2 more."),
            ["Area: Aura Self"]      = new("Area",     2, 3, "Radiates 10ft from caster. Each purchase adds 10ft."),
            ["Area: Zone 60ft"]      = new("Area",     4, 1, "60-foot radius sphere."),
            ["Power: Weak d4"]       = new("Power",    0, 1, "Minimal effect."),
            ["Power: Moderate d6"]   = new("Power",    2, 1, "Standard combat-grade power."),
            ["Power: Strong d8"]     = new("Power",    4, 1, "Above-average effect."),
            ["Power: Powerful d10"]  = new("Power",    6, 1, "Significant and threatening."),
            ["Power: Mighty d12"]    = new("Power",    8, 1, "Devastating single-hit power."),
            ["Power: Epic 2d10"]     = new("Power",   10, 1, "Legendary magnitude."),
            ["Extra Damage Die"]     = new("Power",    2,10, "Add one additional damage die per purchase."),
            ["Cast: Free Action"]    = new("Casting",  3, 1, "No action cost; does not consume your Action for the phase."),
            ["Cast: Hurried"]        = new("Casting",  1, 1, "Cast and move in one phase; all enemies gain +5 to attack for the round."),
            ["Cast: Standard"]       = new("Casting",  0, 1, "Uses your Action for the phase (normal combat casting)."),
            ["Cast: Full Round"]     = new("Casting", -1, 1, "Channels across all 5 phases of 1 round; enemies gain +5 to attack while casting."),
            ["Cast: 1 Min Ritual"]   = new("Casting", -2, 1, "12-round ritual; cannot be interrupted without breaking the spell."),
            ["Cast: 10 Min Ritual"]  = new("Casting", -3, 1, "120-round deep ritual; range and area doubled on completion."),
            ["Silent Spell"]         = new("Special",  1, 1, "Removes verbal component."),
            ["Still Spell"]          = new("Special",  1, 1, "Removes somatic component."),
            ["Subtle Spell"]         = new("Special",  2, 1, "Both verbal and somatic suppressed."),
            ["Maximized"]            = new("Special",  2, 1, "All variable effects use maximum value."),
            ["Empowered"]            = new("Special",  2, 2, "Reroll damage dice; take higher result."),
            ["Twinned Spell"]        = new("Special",  3, 1, "Target a second creature simultaneously."),
            ["Heightened DC"]        = new("Special",  2, 3, "Raise saving throw DC by 2 per purchase."),
            ["Persistent Effect"]    = new("Special",  2, 1, "Dispel checks against it have disadvantage."),
            ["Selective Targeting"]  = new("Special",  2, 1, "Exclude creatures from area of effect."),
            ["Triggered Delay"]      = new("Special",  2, 3, "Delay until trigger (up to 24hr per purchase)."),
            ["Volley"]               = new("Special",  2, 3, "Each purchase adds one additional volley strike."),
            ["Extended Range"]       = new("Special",  1, 5, "Double the spell's range per purchase."),
            // ── Attrition modifiers ───────────────────────────────
            ["Attrition: Escalate"]  = new("Attrition", 2, 4,
                "Upgrade the spell's attrition type by 1 grade per purchase " +
                "(None → Basic → Flesh → Equipment → Destroy → Brutal)."),
            ["Attrition: Controlled"] = new("Attrition", 2, 1,
                "Target player chooses which slot(s) to lose, regardless of attrition type. " +
                "Converts random selection into player choice."),
            ["Attrition: Repeat"]    = new("Attrition", 3, 3,
                "Apply the spell's attrition an additional time per purchase " +
                "(each application rolled/resolved separately)."),
        };

    // ── Negative modifiers ────────────────────────────────────────
    // Each drawback reduces the spell's total point cost by its Cost value.
    // Rule: drawbacks may not refund more than half the spell's gross (pre-drawback) cost.
    public static readonly IReadOnlyList<NegModDef> DefaultNegativeMods = new List<NegModDef>
    {
        // ── Minor (1 pt) — narrative flavour, negligible mechanical impact
        new("Verbal Tell",        "Spell cannot be cast silently; the activation word is always audible.",          1),
        new("Conspicuous Aura",   "Spell leaves a glowing magical residue visible for 1 hour after use.",          1),
        new("Screaming Glyph",    "Caster's voice becomes magically amplified for 1 hour after casting.",          1),
        new("Obvious Display",    "Casting requires a dramatic 6-second visible somatic performance.",             1),

        // ── Moderate (2 pts) — meaningful tactical drawbacks
        new("Unstable Casting",   "15% chance to misfire — spell targets a random creature in range.",             2),
        new("Mana Burn",          "Caster takes 1d6 psychic damage each time this spell is cast.",                 2),
        new("Material Cost",      "Requires a 50gp material component that is consumed on casting.",               2),
        new("Concentration Lock", "While this spell is active, no other concentration spell can be maintained.",   2),
        new("Telegraphed",        "Targets receive one full round of warning before the spell activates.",         2),
        new("Brittle Focus",      "Any damage to the caster automatically breaks concentration.",                  2),
        new("Wild Surge",         "Roll on the wild magic surge table in addition to normal effect.",              2),
        new("Fragile Ward",       "Any physical hit against the caster ends the spell instantly.",                 2),
        new("Cooldown",           "This spell cannot be cast again for 1d4 rounds after use.",                     2),
        new("Mind Fog",           "Caster has disadvantage on all Intelligence checks until next rest.",           2),
        new("Tethered Power",     "Spell ends if caster moves more than 30 ft from the point of casting.",        2),

        // ── Severe (3 pts) — major mechanical or permanent penalties
        new("Backlash",           "Caster is stunned for 1 round after casting this spell.",                       3),
        new("Soul Cost",          "Caster ages 1 year each time this spell is cast (magical aging).",              3),
        new("Exhausting",         "Caster gains 1 level of exhaustion immediately after casting.",                 3),
        new("Corrupting Touch",   "Caster suffers 1 point of permanent Constitution reduction per casting.",       3),
        new("Fate Debt",          "The caster's next saving throw is automatically failed, no roll.",              3),
    };

    // ── Level table ───────────────────────────────────────────────
    public static readonly IReadOnlyList<LevelEntry> LevelTable = new List<LevelEntry>
    {
        new(  0,  2, "Cantrip",    "#888888"),
        new(  3,  6, "1st Level",  "#aaaaff"),
        new(  7, 10, "2nd Level",  "#88aaff"),
        new( 11, 14, "3rd Level",  "#66aaff"),
        new( 15, 20, "4th Level",  "#44aaee"),
        new( 21, 26, "5th Level",  "#22aadd"),
        new( 27, 32, "6th Level",  "#00aacc"),
        new( 33, 40, "7th Level",  "#00ccaa"),
        new( 41, 50, "8th Level",  "#00cc88"),
        new( 51, 60, "9th Level",  "#33cc66"),
        new( 61, 72, "Legendary",  "#99cc22"),
        new( 73, 84, "Mythic",     "#ccaa00"),
        new( 85, 98, "Divine",     "#cc7700"),
        new( 99,112, "Cosmic",     "#cc4400"),
        new(113,999, "Omnipotent", "#ff2200"),
    };

    // ── Helper: find school pair for synergy ─────────────────────
    public static (string, string)? SchoolPair(string a, string b)
    {
        if (SchoolConnections.ContainsKey((a, b))) return (a, b);
        if (SchoolConnections.ContainsKey((b, a))) return (b, a);
        return null;
    }

    // ── Helper: domain name for a school ─────────────────────────
    public static string? SchoolDomainName(string school)
    {
        foreach (var (domain, list) in SchoolDomains)
            if (list.Contains(school)) return domain;
        return null;
    }

    // ── Helper: pts → level ───────────────────────────────────────
    public static LevelEntry PtsToLevel(int pts)
    {
        foreach (var entry in LevelTable)
            if (pts <= entry.Hi) return entry;
        return LevelTable[^1];
    }

    /// <summary>0-based index into LevelTable for the given pts value.
    /// Cantrip = 0, Omnipotent = 14.  Used to compute ring-mod cap.</summary>
    public static int LevelTableIndex(int pts)
    {
        for (int i = 0; i < LevelTable.Count; i++)
            if (pts <= LevelTable[i].Hi) return i;
        return LevelTable.Count - 1;
    }
}
