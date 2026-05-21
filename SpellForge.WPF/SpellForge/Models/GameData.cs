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
public record NegModDef(string Name, string Desc);
public record LevelEntry(int Lo, int Hi, string Name, string Color);

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
        ["Range"]    = ["ᚱ","ᚠ","ᛁ","ᛏ","ᛊ","⊕"],
        ["Duration"] = ["ᛞ","ᛟ","ᚦ","ᛖ","ᚹ","⊙"],
        ["Area"]     = ["ᚨ","ᚷ","ᛃ","ᛈ","ᛚ","◎"],
        ["Power"]    = ["ᛒ","ᛗ","ᚾ","ᛜ","ᚺ","⊗"],
        ["Casting"]  = ["ᛇ","ᚲ","ᚢ","ᛦ","ᛡ","⊛"],
        ["Special"]  = ["ᛥ","ᛣ","ᛤ","ᛢ","ᛰ","⊚"],
    };

    public static readonly IReadOnlyDictionary<string, string> CatColors = new Dictionary<string, string>
    {
        ["Range"]    = "#ff6060", ["Duration"] = "#60ee88", ["Area"]    = "#6088ff",
        ["Power"]    = "#ffd060", ["Casting"]  = "#ff88ff", ["Special"] = "#88ffff",
    };

    public static readonly string[] RingGroups = ["Range", "Duration", "Area", "Power"];

    // ── School glyphs (8 per school) ────────────────────────────
    public static readonly IReadOnlyDictionary<string, string[]> SchoolGlyphs = new Dictionary<string, string[]>
    {
        ["Evocation"]     = ["⚡","✸","✺","☄","⊕","⊗","∞","✦"],
        ["Transmutation"] = ["⚗","⚙","⌬","◈","⍟","⎔","⬡","⌾"],
        ["Enchantment"]   = ["✿","❀","⚜","☙","❧","✤","❆","⚘"],
        ["Space"]         = ["◉","⌖","◎","⍥","⍉","⌀","⊞","⊟"],
        ["Divination"]    = ["⊚","◬","⌭","⍚","⍛","⍜","◌","◍"],
        ["Time"]          = ["⌛","⍨","⍩","⍪","⍫","⍬","⍭","⌬"],
        ["Necromancy"]    = ["☽","☾","☠","☿","♄","⚸","⊗","✝"],
        ["Conjuration"]   = ["★","✩","✪","✫","✬","✭","✮","✯"],
        ["Illusion"]      = ["◆","⬡","⬢","△","▽","◭","◮","◈"],
        ["Abjuration"]    = ["⛨","⛊","⊕","◧","◨","◩","◪","⊠"],
    };

    // ── Capstones ────────────────────────────────────────────────
    public static readonly IReadOnlyDictionary<string, CapstoneDef> Capstones = new Dictionary<string, CapstoneDef>
    {
        ["Evocation"] = new(
            "Apocalyptic Barrage",
            "Unleash 10 simultaneous blasts of any element, ignoring all resistances and immunities.",
            "☄", "⚡✸⚡✦⚡✸⚡✦", "#FF6600"),
        ["Transmutation"] = new(
            "Philosopher's Rewrite",
            "Permanently alter the fundamental nature of any creature, object, or location.",
            "⚗", "⌬◈⚙⎔⌬◈⚙⎔", "#FFD700"),
        ["Enchantment"] = new(
            "Absolute Dominion",
            "Your will becomes law for all creatures you can perceive. No save allowed.",
            "❁", "✿⚜❧✤✿⚜❧✤", "#FF69B4"),
        ["Space"] = new(
            "Infinite Fold",
            "Collapse all distance to zero. Instant transit to any point in the multiverse.",
            "◎", "◉⊞⍥⌀◉⊞⍥⌀", "#1E90FF"),
        ["Divination"] = new(
            "Omniscient Moment",
            "Perceive all timelines, all truths, all minds. Ask any three questions.",
            "⊚", "◬⍚⌭◍◬⍚⌭◍", "#C8C8FF"),
        ["Time"] = new(
            "Temporal Supremacy",
            "Stop time for up to 1 hour. Act freely while the world is frozen.",
            "⌛", "⍭⍩⍪⍨⍭⍩⍪⍨", "#00CED1"),
        ["Necromancy"] = new(
            "Death Transcendence",
            "Become unkillable for 24 hours. Raise all slain as permanent undead servants.",
            "☠", "☽☾☿♄☽☾☿♄", "#9400D3"),
        ["Conjuration"] = new(
            "Grand Summoning",
            "Summon up to 20 CR20 entities from any plane, permanently bound and obedient.",
            "★", "✮✭✬✩✮✭✬✩", "#32CD32"),
        ["Illusion"] = new(
            "Reality Overwrite",
            "Replace all sensory reality for every creature within 1 mile with your constructed world.",
            "◆", "◈△▽⬡◈△▽⬡", "#FF8C00"),
        ["Abjuration"] = new(
            "Absolute Sanctuary",
            "Impenetrable ward over 1 mile. Nothing enters or exits without your permission.",
            "⛨", "◩⊕⛊◧◩⊕⛊◧", "#C0C0C0"),
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
    public static readonly IReadOnlyList<string> SchoolOrder =
        ["Evocation","Transmutation","Enchantment","Space","Divination",
         "Time","Necromancy","Conjuration","Illusion","Abjuration"];

    public static readonly IReadOnlyDictionary<string, SchoolDef> Schools = new Dictionary<string, SchoolDef>
    {
        ["Evocation"] = new(
            "#FF4500", "⚡", "Raw elemental force and energy projection",
            "Evocation channels raw elemental forces into bolts, blasts, waves, and explosions. It rewards intensity, precision, and sheer power. Masters can fill a battlefield with cascading pillars of flame.",
            new Dictionary<string, string>
            {
                ["Range"] = "Blast Range", ["Duration"] = "Burn Duration",
                ["Area"]  = "Explosion Radius", ["Power"] = "Raw Damage",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Damage Die Upgrade"]  = new(1, "Upgrade damage die one step per purchase (d4→d6→d8→d10→d12→2d6→2d8→2d10)."),
                ["Elemental Type"]      = new(1, "Add a second elemental damage type per purchase."),
                ["Penetrating Energy"]  = new(2, "Ignore resistance. Buy twice to ignore immunity."),
                ["Splash Damage"]       = new(1, "Adjacent creatures take half damage. Each purchase extends splash 5ft."),
                ["Lingering Burn"]      = new(2, "Target takes ongoing damage equal to spellcasting mod each turn."),
                ["Escalating Power"]    = new(2, "Each sustained round adds +1 damage die."),
                ["Chain Lightning"]     = new(3, "Energy arcs to another target at half damage on hit."),
                ["Overload"]            = new(3, "Bonus action to double damage dice; suffer 1d6 backlash."),
                ["Shaped Charge"]       = new(2, "Exclude creatures equal to casting mod from AoE."),
                ["Critical Eruption"]   = new(2, "On critical hit, erupt outward hitting all within 5ft."),
            }),
        ["Transmutation"] = new(
            "#FFD700", "⚗", "Alter the properties of matter and creatures",
            "Transmutation rewrites fundamental properties of matter and living beings. Stone becomes iron, lead becomes gold, a man becomes a beast. Deep mastery rewrites magic itself.",
            new Dictionary<string, string>
            {
                ["Range"] = "Transform Range", ["Duration"] = "Change Duration",
                ["Area"]  = "Reshape Area", ["Power"] = "Transform Depth",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Magnitude"]           = new(1, "Increase scale of transformation per purchase."),
                ["Attribute Shift"]     = new(2, "Raise or lower a creature's ability score by 2 per purchase."),
                ["Property Steal"]      = new(3, "Transfer a material property from one substance to another."),
                ["Polymorph Depth"]     = new(2, "Transform deeper systems per purchase (skin→muscle→skeleton→spirit)."),
                ["Reversal Ward"]       = new(2, "The transmutation resists dispelling per purchase."),
                ["Rapid Reshape"]       = new(1, "Reduce casting time one step per purchase."),
                ["Mass Transmute"]      = new(2, "Affect one additional target per purchase."),
                ["Volatile Conversion"] = new(3, "Transmuted substance explodes after 1d4 rounds."),
                ["Subtle Shift"]        = new(2, "Transmutation invisible to the eye."),
                ["Permanent Alteration"]= new(4, "Change persists until magically removed."),
            }),
        ["Enchantment"] = new(
            "#FF69B4", "✿", "Influence minds and bind magical compulsions",
            "Enchantment touches and rewrites the minds of thinking creatures. Enchanters weave compulsions, suggestions, fear, love, and obedience into the neural fabric of their targets.",
            new Dictionary<string, string>
            {
                ["Range"] = "Charm Range", ["Duration"] = "Compulsion Duration",
                ["Area"]  = "Mind Reach", ["Power"] = "Will Override",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Compulsion Strength"] = new(1, "Raise DC to resist by 2 per purchase."),
                ["Mind Depth"]          = new(2, "Reach deeper cognitive layers per purchase."),
                ["Affect Immune Minds"] = new(3, "Bypass charm immunity categories per purchase."),
                ["Persistent Whisper"]  = new(2, "Residual bias lingers after duration ends."),
                ["Cascade Charm"]       = new(2, "On resistance, enchantment hops to nearest creature per purchase."),
                ["Emotional Amplifier"] = new(2, "Layer an emotion onto the enchantment."),
                ["Memory Edit"]         = new(3, "Alter or erase up to 1 minute of memory per purchase."),
                ["Group Mind"]          = new(3, "Affect creatures equal to Charisma modifier simultaneously."),
                ["Triggered Command"]   = new(2, "Lies dormant until trigger condition met."),
                ["Soul Anchor"]         = new(4, "Persists across planar travel and mild death."),
            }),
        ["Space"] = new(
            "#1E90FF", "◉", "Bend, fold, and traverse spatial dimensions",
            "Space magic treats physical dimensions as a medium to compress, stretch, fold, and redirect. A Space mage understands distance is a convention, not a law.",
            new Dictionary<string, string>
            {
                ["Range"] = "Portal Range", ["Duration"] = "Fold Duration",
                ["Area"]  = "Rift Breadth", ["Power"] = "Spatial Distortion",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Teleport Range"]       = new(1, "Extend teleportation distance per purchase."),
                ["Passengers"]           = new(1, "Bring one additional creature per purchase."),
                ["Precision Targeting"]  = new(2, "Reduce teleportation error. Buy twice for perfect arrival."),
                ["Spatial Compression"]  = new(2, "Compress an area; creatures squeezed take damage per purchase."),
                ["Redirect"]             = new(2, "Micro-portal redirects incoming attack as a reaction."),
                ["Dimensional Pocket"]   = new(2, "Extradimensional storage, capacity ×10 per purchase."),
                ["Rift Anchor"]          = new(3, "Stable persistent portal between two locations."),
                ["Blink Step"]           = new(1, "Teleport up to 15ft as part of movement per purchase."),
                ["Gravity Override"]     = new(3, "Alter local gravity vector in a 20ft cube per purchase."),
                ["Void Rift"]            = new(4, "Vacuum rift damages and pulls creatures each round."),
            }),
        ["Divination"] = new(
            "#C8C8FF", "⊚", "Perceive hidden truths and future possibilities",
            "Divination reveals what is hidden, distant, or yet to come. A Diviner who knows the enemy's position, weaknesses, and plans before battle has already won.",
            new Dictionary<string, string>
            {
                ["Range"] = "Scry Range", ["Duration"] = "Vision Duration",
                ["Area"]  = "Sight Radius", ["Power"] = "Truth Penetration",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Scry Range"]          = new(1, "Extend scrying range per purchase: building→city→continent→world→planes."),
                ["Information Depth"]   = new(1, "Reveal deeper truth per purchase."),
                ["Duration of Sight"]   = new(1, "Double divination window duration per purchase."),
                ["Precognition Depth"]  = new(2, "See further into the future per purchase."),
                ["Magical Detection"]   = new(1, "Identify magical auras in more detail per purchase."),
                ["Truth Compulsion"]    = new(3, "Target cannot knowingly conceal information."),
                ["Probability Map"]     = new(3, "See branching probability trees as percentages."),
                ["Invisible Eye"]       = new(2, "Scrying sensor invisible and undetectable."),
                ["Sense Emotion"]       = new(1, "Read surface emotions of creatures per purchase."),
                ["Retrocognition"]      = new(3, "Read history of a location or object per purchase."),
            }),
        ["Time"] = new(
            "#00CED1", "⌛", "Manipulate the flow and fabric of time",
            "Time magic manipulates temporal flow — slowing enemies, accelerating allies, reversing events, looping moments, or aging matter to dust. True Time mages step outside the timestream entirely.",
            new Dictionary<string, string>
            {
                ["Range"] = "Temporal Reach", ["Duration"] = "Effect Duration",
                ["Area"]  = "Time Field", ["Power"] = "Chronos Force",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Time Slow Degree"]  = new(1, "Increase time dilation per purchase."),
                ["Haste Degree"]      = new(1, "Increase time acceleration per purchase."),
                ["Rewind Depth"]      = new(2, "Reverse time per action or round per purchase."),
                ["Time Stop Duration"]= new(3, "Freeze time; each purchase adds one frozen round."),
                ["Aging Strike"]      = new(2, "Age target 10 years per purchase."),
                ["Loop Anchor"]       = new(3, "Create a time loop, max 3 repetitions base."),
                ["Temporal Shield"]   = new(2, "Phase out of timestream to dodge as a reaction."),
                ["Future Echo"]       = new(2, "See 1 round into own future; advantage on all rolls."),
                ["Paradox Step"]      = new(4, "Act twice in one round via alternate timeline."),
                ["Entropy Field"]     = new(3, "Objects and structures crumble per round."),
            }),
        ["Necromancy"] = new(
            "#9400D3", "☠", "Command life force, death, and undeath",
            "Necromancy operates at the boundary between life and death. Necromancers drain life to fuel their power, reanimate corpses as tireless soldiers, and communicate with the departed.",
            new Dictionary<string, string>
            {
                ["Range"] = "Death Range", ["Duration"] = "Undead Duration",
                ["Area"]  = "Necrotic Field", ["Power"] = "Life Drain",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Undead Tier"]          = new(1, "Unlock more powerful undead categories per purchase."),
                ["Undead Count"]         = new(1, "Control one additional undead per purchase."),
                ["Life Drain Potency"]   = new(1, "Add 1d6 necrotic damage per purchase; half to caster as THP."),
                ["Soul Cage"]            = new(3, "Trap soul, prevent resurrection, extend per purchase."),
                ["Death Ward"]           = new(2, "Creature stabilizes at 0 HP once per purchase, rises with 1 HP."),
                ["Corpse Explosion"]     = new(2, "Detonate corpse as bonus action; increase per purchase."),
                ["Deathsight"]           = new(1, "See HP auras; sense undead through walls per purchase."),
                ["Bone Armor"]           = new(2, "+2 AC from bone fragments; +1 AC per purchase."),
                ["Contagion Spread"]     = new(2, "Effect spreads from slain to nearby creatures per purchase."),
                ["Phylactery Bond"]      = new(4, "Bind target's life to an object — cannot die while it exists."),
            }),
        ["Conjuration"] = new(
            "#32CD32", "★", "Summon entities, objects, and substances",
            "Conjuration reaches across space, dimensions, and planes to bring beings, objects, or materials to the caster. From practical conjuring of supplies to summoning archons and rifting planes.",
            new Dictionary<string, string>
            {
                ["Range"] = "Summon Range", ["Duration"] = "Binding Duration",
                ["Area"]  = "Conjure Area", ["Power"] = "Entity Tier",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Summoned Tier"]    = new(1, "Unlock more powerful entity categories per purchase."),
                ["Summoned Count"]   = new(1, "Summon one additional entity per purchase."),
                ["Entity Duration"]  = new(1, "Extend how long summoned entities remain per purchase."),
                ["Obedience Binding"]= new(2, "Entity cannot disobey direct commands per purchase."),
                ["Material Conjure"] = new(1, "Conjure non-magical substance; double volume per purchase."),
                ["Planar Anchor"]    = new(3, "Prevent entity from being dismissed or plane-shifted."),
                ["Shared Senses"]    = new(2, "Telepathic link with summoned entity at unlimited range."),
                ["Sacrifice Fuel"]   = new(2, "Spend HP to empower summoned entity per purchase."),
                ["Rift Permanence"]  = new(4, "Summon portal remains open as two-way passage."),
                ["Instant Recall"]   = new(2, "Dismiss all summoned entities simultaneously as bonus action."),
            }),
        ["Illusion"] = new(
            "#FF8C00", "◆", "Craft false realities and deceive the senses",
            "Illusion creates false sensory information that bypasses the conscious mind and registers as real. A master Illusionist can make phantom soldiers that trigger genuine fear responses.",
            new Dictionary<string, string>
            {
                ["Range"] = "Illusion Reach", ["Duration"] = "Mirage Duration",
                ["Area"]  = "Illusion Size", ["Power"] = "Belief Anchor",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Sensory Channels"]     = new(1, "Add one sense per purchase (sight free; add sound, smell, touch, taste, pain)."),
                ["Illusion Fidelity"]    = new(1, "Raise Investigation DC to detect by 3 per purchase."),
                ["Illusion Size"]        = new(1, "Increase maximum size per purchase."),
                ["Quasi-Reality"]        = new(3, "Illusion deals psychic damage to believers."),
                ["Lingering Image"]      = new(2, "Remains after concentration ends, fading over 1 minute."),
                ["Mirror Image"]         = new(2, "Create 1d4+1 illusory duplicates per purchase."),
                ["Programmed Behaviour"] = new(2, "Illusion acts autonomously on a script per purchase."),
                ["Invisibility Depth"]   = new(1, "Extend invisibility tier per purchase."),
                ["Mass Hallucination"]   = new(3, "Affects all creatures with individually crafted versions."),
                ["Reality Anchor"]       = new(4, "Even disbelievers still perceive it as real."),
            }),
        ["Abjuration"] = new(
            "#C0C0C0", "⛨", "Ward, protect, banish, and negate magic",
            "Abjuration erects barriers, creates wards, counters spells, banishes entities, and strips enchantments. An Abjurer who has prepared a location is effectively omnipotent within it.",
            new Dictionary<string, string>
            {
                ["Range"] = "Ward Range", ["Duration"] = "Shield Duration",
                ["Area"]  = "Ward Coverage", ["Power"] = "Barrier Strength",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Ward Strength"]      = new(1, "Add 10 HP or raise damage threshold by 5 per purchase."),
                ["Counter Potency"]    = new(1, "+2 to dispel check per purchase."),
                ["Ward Coverage"]      = new(1, "Extend ward area per purchase."),
                ["Planar Exclusion"]   = new(2, "Bar one creature type or planar origin per purchase."),
                ["Spell Reflection"]   = new(3, "Blocked spells reflect back at caster per purchase."),
                ["Anti-Magic Shell"]   = new(4, "Suppress magical effects; extend radius per purchase."),
                ["Alarm Trigger"]      = new(1, "Alert on trigger rather than blocking per purchase."),
                ["Absorb Energy"]      = new(2, "Ward converts spell damage to temporary HP per purchase."),
                ["Glyph of Warding"]   = new(2, "Store a spell in a glyph triggered by condition."),
                ["Dimensional Lock"]   = new(3, "Prevent teleportation, planar travel, summoning in area."),
            }),
    };

    // ── School connections (synergies) ────────────────────────────
    public static readonly IReadOnlyDictionary<(string, string), string> SchoolConnections =
        new Dictionary<(string, string), string>
        {
            [("Evocation","Transmutation")]  = "Catalytic Surge — damage also reshapes targets structurally",
            [("Evocation","Necromancy")]      = "Soul Scorch — energy damage drains life force simultaneously",
            [("Evocation","Conjuration")]     = "Elemental Calling — summon beings of your damage type",
            [("Evocation","Space")]           = "Rift Blast — teleport projectiles through micro-portals",
            [("Evocation","Abjuration")]      = "Spell Absorb — absorb incoming energy to power your blast",
            [("Evocation","Illusion")]        = "Phantom Fire — illusory damage that still burns the mind",
            [("Evocation","Time")]            = "Temporal Detonation — delay a blast to trigger later",
            [("Evocation","Enchantment")]     = "Awe Strike — raw power forces a fear save on the target",
            [("Evocation","Divination")]      = "Guided Missile — divination locks perfect aim onto targets",
            [("Transmutation","Time")]        = "Temporal Morph — reshape a target across multiple time states",
            [("Transmutation","Space")]       = "Phase Shift — transmute the spatial coordinates of an object",
            [("Transmutation","Necromancy")]  = "Corpse Alchemy — animate dead by transmuting their matter",
            [("Transmutation","Illusion")]    = "Mimic Matter — illusions that fool even magical detection",
            [("Transmutation","Enchantment")] = "Mind over Matter — reshape self to affect mental state",
            [("Transmutation","Divination")]  = "Material Truth — read an object's full history by touch",
            [("Transmutation","Conjuration")] = "Forge Summons — conjure and transmute simultaneously",
            [("Transmutation","Abjuration")]  = "Living Ward — transform flesh into a magical barrier",
            [("Enchantment","Illusion")]      = "Phantasmal Compulsion — illusion plants commands as memory",
            [("Enchantment","Time")]          = "Memory Weave — rewrite recollections with temporal precision",
            [("Enchantment","Necromancy")]    = "Soul Binding — bind a spirit's will directly to a command",
            [("Enchantment","Divination")]    = "Mind Probe — read and subtly influence surface thoughts",
            [("Enchantment","Space")]         = "Compel Step — enchanted target teleports on command",
            [("Enchantment","Conjuration")]   = "Loyal Summons — summoned entity is permanently charmed",
            [("Enchantment","Abjuration")]    = "Psychic Ward — ward fueled by the minds of those it protects",
            [("Space","Conjuration")]         = "Dimensional Gate — summon directly from across any plane",
            [("Space","Time")]                = "Chrono-Fold — teleport through both space and time at once",
            [("Space","Abjuration")]          = "Null Space — imprison a target in a sealed pocket dimension",
            [("Space","Necromancy")]          = "Death Step — step between life and death as spatial planes",
            [("Space","Divination")]          = "Spatial Scry — see any location you can spatially pinpoint",
            [("Space","Illusion")]            = "Mirror Realm — spatial pocket disguised as empty space",
            [("Divination","Time")]           = "True Sight of Ages — perceive past and future simultaneously",
            [("Divination","Necromancy")]     = "Speak with Dead — commune with souls in the afterlife",
            [("Divination","Illusion")]       = "False Oracle — your divination misleads other seers instead",
            [("Divination","Conjuration")]    = "Scry and Call — summon directly to your scrying focus",
            [("Divination","Abjuration")]     = "Arcane Lock — detect and permanently seal magical intrusions",
            [("Time","Necromancy")]           = "Decay Pulse — accelerate aging of targets to crumbling dust",
            [("Time","Abjuration")]           = "Temporal Ward — rewind damage suffered within the last round",
            [("Time","Illusion")]             = "Echo Image — project an image of yourself from the past",
            [("Time","Conjuration")]          = "Summon From Past — call a creature from a previous moment",
            [("Necromancy","Conjuration")]    = "Undead Summons — summon pre-raised undead from storage",
            [("Necromancy","Illusion")]       = "Death Mirage — illusion so vivid it can kill through belief",
            [("Necromancy","Abjuration")]     = "Corpse Ward — animate a corpse specifically as a shield",
            [("Conjuration","Illusion")]      = "Dream Manifest — conjure matter from within a living illusion",
            [("Conjuration","Abjuration")]    = "Ward Circle — banish summoned entities on contact with ward",
            [("Illusion","Abjuration")]       = "Mirror Ward — ward reflects images, confusing attackers",
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
            ["Cast: Free Action"]    = new("Casting",  3, 1, "No action economy cost."),
            ["Cast: Bonus Action"]   = new("Casting",  2, 1, "Uses your bonus action."),
            ["Cast: Standard"]       = new("Casting",  0, 1, "Uses your primary action."),
            ["Cast: Full Round"]     = new("Casting", -1, 1, "Entire round to cast; DC +1."),
            ["Cast: 1 Min Ritual"]   = new("Casting", -2, 1, "1-minute ritual."),
            ["Cast: 10 Min Ritual"]  = new("Casting", -3, 1, "10-minute deep ritual. Range and area doubled."),
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
        };

    // ── Negative modifiers ────────────────────────────────────────
    public static readonly IReadOnlyList<NegModDef> DefaultNegativeMods = new List<NegModDef>
    {
        new("Unstable Casting",   "15% chance to misfire — spell targets a random creature in range."),
        new("Mana Burn",          "Caster takes 1d6 psychic damage each time this spell is cast."),
        new("Verbal Tell",        "Spell cannot be cast silently; the activation word is always audible."),
        new("Material Cost",      "Requires a 50gp material component that is consumed on casting."),
        new("Concentration Lock", "While this spell is active, no other concentration spell can be maintained."),
        new("Telegraphed",        "Targets receive one full round of warning before the spell activates."),
        new("Backlash",           "Caster is stunned for 1 round after casting this spell."),
        new("Soul Cost",          "Caster ages 1 year each time this spell is cast (magical aging)."),
        new("Brittle Focus",      "Any damage to the caster automatically breaks concentration."),
        new("Conspicuous Aura",   "Spell leaves a glowing magical residue visible for 1 hour after use."),
        new("Exhausting",         "Caster gains 1 level of exhaustion immediately after casting."),
        new("Wild Surge",         "Roll on the wild magic surge table in addition to normal effect."),
        new("Fragile Ward",       "Any physical hit against the caster ends the spell instantly."),
        new("Obvious Display",    "Casting requires a dramatic 6-second visible somatic performance."),
        new("Cooldown",           "This spell cannot be cast again for 1d4 rounds after use."),
        new("Mind Fog",           "Caster has disadvantage on all Intelligence checks until next rest."),
        new("Tethered Power",     "Spell ends if caster moves more than 30 ft from the point of casting."),
        new("Corrupting Touch",   "Caster suffers 1 point of permanent Constitution reduction."),
        new("Screaming Glyph",    "Caster's voice becomes magically amplified for 1 hour after casting."),
        new("Fate Debt",          "The caster's next saving throw is automatically failed, no roll."),
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

    // ── Helper: pts → level ───────────────────────────────────────
    public static LevelEntry PtsToLevel(int pts)
    {
        foreach (var entry in LevelTable)
            if (pts <= entry.Hi) return entry;
        return LevelTable[^1];
    }
}
