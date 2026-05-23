namespace SpellForge.Models;

// ── Records ──────────────────────────────────────────────────────
public record AbilityDef(int Cost, string Desc);
public record SchoolDef(
    string Color, string Symbol, string Short, string Desc,
    IReadOnlyDictionary<string, string> RingMods,
    IReadOnlyDictionary<string, AbilityDef> Abilities,
    bool IsCrown = true);

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

public static class GameData
{
    // ── Runes ──────────────────────────────────────────────────────
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

    // ── Path glyphs (8 per path, used for orbital runes) ──────────
    public static readonly IReadOnlyDictionary<string, string[]> SchoolGlyphs = new Dictionary<string, string[]>
    {
        ["Wardlight"]  = ["⛤","✦","☉","✸","⊕","☙","✺","⊙"],
        ["Ironcraft"]  = ["⚒","⛊","⊠","⌾","⎔","◧","⬡","◈"],
        ["Thornvine"]  = ["🌿","❀","⚘","❧","✤","☙","❆","✿"],
        ["Stonesong"]  = ["⛰","◉","⌬","⍟","⬢","△","▽","◭"],
        ["Shadowmark"] = ["☽","☾","◆","◌","◍","▽","◮","⊞"],
        ["Bonecraft"]  = ["☠","☿","♄","⚸","⊗","✝","☽","☾"],
        ["Stormcall"]  = ["⚡","✸","✺","☄","⊕","⊗","∞","✦"],
        ["Bloodrite"]  = ["✚","♂","⊗","⌬","◈","⍟","⎔","⬡"],
    };

    // ── Eldersigns (Capstones) ────────────────────────────────────
    public static readonly IReadOnlyDictionary<string, CapstoneDef> Capstones = new Dictionary<string, CapstoneDef>
    {
        ["Wardlight"] = new(
            "Eternal Vigil",
            "Every ally you can see cannot die while you stand. Death stops at your word.",
            "⛤", "⛤✦☉✸⛤✦☉✸", "#FFD700"),
        ["Ironcraft"] = new(
            "The Unbreakable Law",
            "Name a single prohibition. Any creature that breaks it is immediately struck by iron judgment.",
            "⚒", "⚒⛊⊠⌾⚒⛊⊠⌾", "#C08040"),
        ["Thornvine"] = new(
            "The Living World",
            "The wild rises for you. For one day every tree, root, and beast in the region obeys your word.",
            "🌿", "🌿❀⚘❧🌿❀⚘❧", "#50A060"),
        ["Stonesong"] = new(
            "Voice of Ages",
            "Speak with the memory of the earth itself. Ask any question that has ever been witnessed by stone.",
            "⛰", "⛰◉⌬⍟⛰◉⌬⍟", "#9E7E4A"),
        ["Shadowmark"] = new(
            "The Last Shadow",
            "You become darkness itself for one scene. Invisible, intangible — but you cannot act physically.",
            "☽", "☽☾◆◌☽☾◆◌", "#8866AA"),
        ["Bonecraft"] = new(
            "Kingdom of Bones",
            "Every corpse in sight rises and obeys. They do not stop while you breathe.",
            "☠", "☠☿♄⚸☠☿♄⚸", "#9B30FF"),
        ["Stormcall"] = new(
            "The Endless Storm",
            "Summon a catastrophic storm centered on you lasting one day. You cannot be harmed within it.",
            "⚡", "⚡✸✺☄⚡✸✺☄", "#4499FF"),
        ["Bloodrite"] = new(
            "Red Ascension",
            "Spend half your current HP. Until next dawn deal double damage on every strike and heal from every kill.",
            "✚", "✚♂⊗⌬✚♂⊗⌬", "#CC3333"),
    };

    // ── Forces (Elements) ─────────────────────────────────────────
    public static readonly IReadOnlyDictionary<string, ElementDef> Elements = new Dictionary<string, ElementDef>
    {
        ["Flame"] = new(
            "#FF4500", "🜂", "ᚠ",
            "Damage Type: Fire  |  +2 damage dice  |  Targets ignite (1d6/round)",
            "Flame — the forge's friend and the ruin's herald. Burns, purifies, and consumes without mercy.",
            new List<NodeDef>
            {
                new("Ignite",      1, "ᚠ", "🜂", "Targets ignite: 1d6 fire each round",      "+1d6 ongoing fire"),
                new("Forge-Heat",  2, "ᚱ", "⚡", "Metal becomes workable; objects pliable",   "Forge or melt objects"),
                new("Sear",        2, "⊕", "✸", "Burn away armor: target AC -1",              "AC reduced 1"),
                new("Wildfire",    3, "ᚷ", "☄", "Uncontrolled spread to adjacent targets",    "Chain to 2nd target"),
                new("Pyre",        3, "ᛁ", "✺", "Leave a persistent fire zone",               "10ft fire zone 2 rounds"),
            }),
        ["Tide"] = new(
            "#1E90FF", "🜄", "ᚹ",
            "Damage Type: Cold  |  Slow target (½ speed)  |  Heal or harm",
            "Tide — cold flow that heals the living and drowns those who fight it.",
            new List<NodeDef>
            {
                new("Chill",      1, "ᚹ", "🜄", "Target slowed to half speed",              "½ speed 1 round"),
                new("Bind",       2, "ᛟ", "❄", "Freeze: restrained on failed save",         "Restrained 1 round"),
                new("Surge",      2, "ᚦ", "⌀", "Push target 20ft in any direction",          "Push 20ft"),
                new("Undertow",   3, "⊙", "◉", "Pull all targets toward centre",             "Pull 15ft"),
                new("Life-Water", 3, "ᛚ", "◎", "Healing tide: restore 2d4 to target",        "Heal 2d4"),
            }),
        ["Stone"] = new(
            "#8B4513", "🜃", "ᚦ",
            "Damage Type: Bludgeoning  |  Knock Prone  |  +4 to Hit",
            "Stone — ancient, heavy, and patient. Hits hard and holds forever.",
            new List<NodeDef>
            {
                new("Tremor",   1, "ᚦ", "🜃", "Knock prone on hit",               "Knock prone"),
                new("Boulder",  2, "ᛒ", "⌬", "+1d8 bludgeoning damage",          "+1d8 damage"),
                new("Entomb",   2, "ᛗ", "⎔", "Restrain target in rising stone",   "Restrained 1 round"),
                new("Quake",    3, "ᚾ", "◈", "All in area fall prone",            "Area prone"),
                new("Monolith", 3, "ᛜ", "⬡", "Create impassable stone wall",      "Stone wall 20ft"),
            }),
        ["Gale"] = new(
            "#87CEEB", "🜁", "ᚨ",
            "Damage Type: Thunder  |  Push 30ft  |  Fly Speed granted",
            "Gale — wild storm-wind that liberates, carries, and destroys in equal measure.",
            new List<NodeDef>
            {
                new("Gust",     1, "ᚨ", "🜁", "Push target 15ft",                    "Push 15ft"),
                new("Updraft",  2, "ᚷ", "△", "Caster gains fly speed 30ft",          "Fly 30ft 1 round"),
                new("Deafen",   2, "ᛦ", "◌", "Thunder deafens on hit",              "Deafened 1 round"),
                new("Cyclone",  3, "ᛡ", "⍥", "Spin targets: disadvantage on attacks","Disadvantage attacks"),
                new("Tempest",  3, "⊛", "⍉", "Lightning strikes a random target",   "Random lightning 2d6"),
            }),
        ["Veil"] = new(
            "#CC99FF", "✦", "⊕",
            "Choose Aspect: Crown (holy) / Skull (dread) / Neither (ethereal)",
            "The Veil — the membrane between life and death, law and chaos, Crown and Skull.",
            new List<NodeDef>
            {
                new("Hallow",    1, "⊕", "✦", "Area becomes sacred or cursed ground",   "Hallowed/Cursed zone"),
                new("Smite",     2, "☉", "✸", "+1d8 typed damage (holy/dread/psychic)", "+1d8 typed damage"),
                new("Ward",      2, "☽", "⊚", "Allies or enemies affected by aura +/-2","±2 in aura"),
                new("Ascend",    3, "⊛", "⍜", "Temporary invulnerability 1 round",      "Invulnerable 1 round"),
                new("Judgment",  3, "⊗", "⍚", "Double damage vs opposed aspect",        "×2 vs opposed"),
            },
            new Dictionary<string, SubtypeDef>
            {
                ["Crown"] = new("#FFFFAA", "☉",
                    "Radiant damage  |  Heal allies = damage dealt  |  Undead ×2",
                    "Divine light heals the faithful and scorches the risen dead."),
                ["Skull"] = new("#AA44FF", "☽",
                    "Necrotic damage  |  Drain max HP  |  Empower undead",
                    "Death-touch drains life and empowers anything already dead."),
                ["Ethereal"]  = new("#CC88FF", "⊚",
                    "Psychic damage  |  Stun 1 round  |  Pass through matter briefly",
                    "Pure Veil energy phases through flesh and stone alike."),
            }),
    };

    // ── Force connections ─────────────────────────────────────────
    public static readonly IReadOnlyDictionary<(string, string), string> ElementConnections =
        new Dictionary<(string, string), string>
        {
            [("Flame","Tide")]  = "Scalding Mist — burning vapour obscures vision and sears through armor",
            [("Flame","Stone")] = "Magma — molten rock unstoppable, leaves scorched terrain and slag",
            [("Flame","Gale")]  = "Firestorm — whirling inferno empowers all flame and scatters foes",
            [("Flame","Veil")]  = "Pyre-Light — holy or cursed fire; purifies corruption or marks the doomed",
            [("Tide","Stone")]  = "Bog Mire — entangles and slows all targets, difficult terrain spreads",
            [("Tide","Gale")]   = "Ice Storm — freezing blizzard slows all and coats surfaces in black ice",
            [("Tide","Veil")]   = "Veil-Water — blessed or cursed tide; heals faithful or rots the marked",
            [("Stone","Gale")]  = "Dust Devil — blinding debris cloud, concealment and minor bludgeon",
            [("Stone","Veil")]  = "Sealed Earth — permanent glyphs carved in unbreakable ancient stone",
            [("Gale","Veil")]   = "Sky-Wrath — divine or cursed storm; deafens all and calls radiant lightning",
        };

    // ── Sub-Force nodes ───────────────────────────────────────────
    public static readonly IReadOnlyDictionary<(string, string), NodeDef[]> SubelementNodes =
        new Dictionary<(string, string), NodeDef[]>
        {
            [("Flame","Tide")] =
            [
                new("Steam Veil",   1, "ᚠ", "⌀", "Obscuring steam cloud 10ft",         "Obscured zone"),
                new("Scald",        2, "ᚹ", "◎", "Fire+Cold combo: 1d6 each",           "+1d6 fire +1d6 cold"),
                new("Geyser",       3, "⊕", "✺", "Erupting steam pillar: push+burn",    "Push+ignite"),
            ],
            [("Flame","Stone")] =
            [
                new("Lava Crack",   1, "ᚦ", "⌬", "Molten fissure in ground",            "Difficult terrain+fire"),
                new("Magma Surge",  2, "ᚠ", "☄", "Eruption: 2d6 fire+bludgeon",         "2d6 hybrid damage"),
                new("Slag",         3, "ᛒ", "⎔", "Solidify target in cooled lava",      "Restrained permanent"),
            ],
            [("Flame","Gale")] =
            [
                new("Firestorm",    1, "ᚨ", "✸", "Spinning fire 20ft radius",           "20ft fire zone"),
                new("Phoenix Wing", 2, "ᚱ", "⍥", "Fire+fly: blazing charge",            "Fly+fire trail"),
                new("Inferno Wind", 3, "⊛", "⊗", "All fire effects maximised",          "Max fire damage"),
            ],
            [("Flame","Veil")] =
            [
                new("Pyre-Sign",    1, "☉", "✦", "Divine fire heals or curses",         "Heals or curses"),
                new("Purgation",    2, "⊕", "⊕", "Burn away curses and conditions",     "Remove 1 condition"),
                new("Immolation",   3, "⊗", "☄", "Full body halo; aura damage",         "5ft fire aura"),
            ],
            [("Tide","Stone")] =
            [
                new("Mudslide",     1, "ᚦ", "🜃", "Engulf area in thick mud",           "Restrained+difficult"),
                new("Bog Pull",     2, "ᚹ", "◉", "Sink target, hold prone",             "Prone+restrained"),
                new("Earthen Wave", 3, "ᛗ", "◈", "Tidal wave of mud: area+pull",        "Area pull+prone"),
            ],
            [("Tide","Gale")] =
            [
                new("Blizzard",     1, "ᚨ", "❄", "Snowstorm reduces visibility",        "Heavily obscured"),
                new("Ice Lance",    2, "ᛟ", "⌀", "Piercing ice spear, ignore AC",       "+2d6 pierce+cold"),
                new("Hailstorm",    3, "⊙", "⍉", "Hail: stun+prone+cold",               "Area stun+prone"),
            ],
            [("Tide","Veil")] =
            [
                new("Holy Font",    1, "☉", "🜄", "Blessed water purifies the marked",  "Remove poison/curse"),
                new("Tide of Life", 2, "⊕", "◎", "Healing wave to all nearby allies",   "Heal 2d6 allies"),
                new("Absolution",   3, "⊛", "✦", "Wash away undeath and damnation",     "Destroy undead ≤CR5"),
            ],
            [("Stone","Gale")] =
            [
                new("Dust Devil",   1, "ᚨ", "⍥", "Blinding dust vortex",               "Blinded 1 round"),
                new("Rock Gust",    2, "ᚦ", "⌬", "Debris launch: 1d10 piercing",        "1d10 piercing"),
                new("Landslide",    3, "ᚷ", "△", "Massive debris avalanche",            "Area prone+buried"),
            ],
            [("Stone","Veil")] =
            [
                new("Carved Glyph", 1, "⊕", "🜃", "Permanent ward glyph in the earth", "Permanent ward"),
                new("Rune Seal",    2, "☉", "⊠", "Anti-magic zone sealed in stone",    "Anti-magic zone"),
                new("Earthen Tomb", 3, "⊗", "⎔", "Entomb target in holy or cursed stone","Imprisoned"),
            ],
            [("Gale","Veil")] =
            [
                new("Sky-Sign",     1, "☙", "🜁", "Divine or cursed storm arrives",     "Storm zone 30ft"),
                new("Wind Rush",    2, "⊕", "△", "Celestial speed: move+act twice",     "Extra move+attack"),
                new("Sky-Wrath",    3, "⊛", "⍚", "Divine whirlwind: lift+typed damage", "Fly+typed aura"),
            ],
        };

    // ── Rune Paths ────────────────────────────────────────────────
    public static readonly IReadOnlyList<string> SchoolOrder =
        ["Wardlight","Ironcraft","Thornvine","Stonesong",
         "Shadowmark","Bonecraft","Stormcall","Bloodrite"];

    public static readonly IReadOnlyDictionary<string, SchoolDef> Schools = new Dictionary<string, SchoolDef>
    {
        // ── CROWN PATHS ──────────────────────────────────────────
        ["Wardlight"] = new(
            "#FFD080", "⛤", "Holy fire, wards, and divine protection",
            "Wardlight channels holy light into wards and weapons. Mystics of Wardlight guard the innocent, banish the risen, and call down judgment on the wicked. Crown-aligned.",
            new Dictionary<string, string>
            {
                ["Range"] = "Holy Reach", ["Duration"] = "Vigil Hold",
                ["Area"]  = "Blessed Field", ["Power"] = "Divine Smite",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Holy Mark"]      = new(1, "Brand shines in dark; undead must save or flee toward you."),
                ["Mend"]           = new(1, "Close a wound; heal 1d6. Each purchase upgrades the die."),
                ["Banish"]         = new(2, "Force an unnatural creature to flee for one round per purchase."),
                ["Smite"]          = new(2, "Strike with holy fire; +1d6 radiant damage per purchase."),
                ["Ward Circle"]    = new(2, "Allies inside the ward gain +1 Armor. Each purchase expands it."),
                ["Truth Flame"]    = new(2, "Illusions and disguises burn away within the ward's radius."),
                ["Purge Curse"]    = new(3, "Remove a Doom Mark or curse from a creature per purchase."),
                ["Crown's Shield"] = new(3, "Target cannot be reduced below 1 HP for one round per purchase."),
            },
            IsCrown: true),

        ["Ironcraft"] = new(
            "#C08040", "⚒", "Iron-binding, enchantment of objects, fortification",
            "Ironcraft binds magic to iron and stone — enchanting gear, sealing doors, raising walls. These Mystics are builders and defenders, not warriors. Crown-aligned.",
            new Dictionary<string, string>
            {
                ["Range"] = "Bind Range", ["Duration"] = "Seal Hold",
                ["Area"]  = "Forge Reach", ["Power"] = "Iron Grip",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Temper"]       = new(1, "Grant a weapon or armor +1 to hit or AC for the scene."),
                ["Seal"]         = new(1, "Lock or unlock a door or container. Magical seals resist mundane entry."),
                ["Endure"]       = new(1, "Infuse toughness; target gains Tough Hide until dawn."),
                ["Chain"]        = new(2, "Bind a creature with spectral iron chains. Save each round to break."),
                ["Forge-Gift"]   = new(2, "Create a simple iron tool or weapon from raw material present."),
                ["Runecarve"]    = new(2, "Etch a permanent minor enchantment into any object."),
                ["Unbreakable"]  = new(3, "Object or structure cannot be destroyed by normal means until dawn."),
                ["Iron Prison"]  = new(3, "Imprison a creature in an iron cage rising from the earth."),
            },
            IsCrown: true),

        ["Thornvine"] = new(
            "#50A060", "🌿", "Wild growth, beast-speech, thorn and sap",
            "Thornvine draws on the living wild — beast-speech, root and stone, healing sap and venomous thorn. Neither tame nor cruel, just the world as it is. Crown-aligned.",
            new Dictionary<string, string>
            {
                ["Range"] = "Wild Reach", ["Duration"] = "Bloom Hold",
                ["Area"]  = "Tangle Spread", ["Power"] = "Thorn Bite",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Speak-to-Beast"] = new(1, "Commune with animals; understand their intent and emotional state."),
                ["Mend Wound"]     = new(1, "Close a wound; heal 1d6. Each purchase upgrades the die."),
                ["Root"]           = new(1, "Grasping roots hold a creature. Move saves each round to escape."),
                ["Thorn Lash"]     = new(2, "Vine whip: 1d8 slashing and target bleeds 1 per round."),
                ["Beast Call"]     = new(2, "Summon a nearby animal as an ally for the scene."),
                ["Venom"]          = new(2, "Poison on hit; 1d4 damage per round until tended."),
                ["Regrowth"]       = new(3, "Restore a shattered limb or cure a permanent wound over one rest."),
                ["Living Wall"]    = new(3, "Massive thorn barrier impassable for the scene."),
            },
            IsCrown: true),

        ["Stonesong"] = new(
            "#9E7E4A", "⛰", "Ancient earth memory, endurance, weight",
            "Stonesong communes with the ancient memory of earth and stone. Slow but supremely enduring, these Mystics feel time differently — every rock a library of ages. Crown-aligned.",
            new Dictionary<string, string>
            {
                ["Range"] = "Deep Reach", ["Duration"] = "Age Hold",
                ["Area"]  = "Stone Spread", ["Power"] = "Crush Force",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Stone Skin"]     = new(1, "Target gains +1 Armor from hardened flesh. Stacks per purchase."),
                ["Read Stone"]     = new(1, "Sense the history of a place or object by touching it."),
                ["Tremor-Sense"]   = new(1, "Feel movement through the earth in a wide radius around you."),
                ["Crush"]          = new(2, "Column of rock erupts and smashes: 2d6 bludgeoning."),
                ["Bury"]           = new(2, "Swallow a creature into the earth. It claws free with time."),
                ["Recall"]         = new(2, "Extract a complete memory from stone or ancient soil."),
                ["Earthen Armor"]  = new(3, "Full plate of living stone encases target. +4 Armor, very slow."),
                ["Monolith"]       = new(3, "Raise a standing stone that channels ley-power for one day."),
            },
            IsCrown: true),

        // ── SKULL PATHS ──────────────────────────────────────────
        ["Shadowmark"] = new(
            "#8866AA", "☽", "Darkness, fear, deception, shadow-step",
            "Shadowmark walks the edge between seen and unseen, using fear, shadow, and deception as weapons. What cannot be seen cannot be fought. Skull-aligned.",
            new Dictionary<string, string>
            {
                ["Range"] = "Shadow Reach", ["Duration"] = "Fade Hold",
                ["Area"]  = "Haunt Spread", ["Power"] = "Fear Bite",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Cloak"]       = new(1, "Become invisible to casual sight. Alert creatures may still sense you."),
                ["Shadow Step"] = new(1, "Step through shadow; teleport 30ft between areas of darkness."),
                ["Fear"]        = new(2, "Target must save or flee. Critical miss causes lasting dread."),
                ["Blind"]       = new(2, "Darkness descends on the target; blinded for the scene."),
                ["Haunt"]       = new(2, "Plant a phantom in the target's senses; it returns each night."),
                ["Unmake"]      = new(3, "Dissolve a small non-magical object completely into shadow."),
                ["Death Mark"]  = new(3, "Mark a target; next creature to strike them deals double damage."),
                ["Rift Veil"]   = new(3, "Entire area plunged in impenetrable magical darkness."),
            },
            IsCrown: false),

        ["Bonecraft"] = new(
            "#9B30FF", "☠", "Life drain, undead command, death and corruption",
            "Bonecraft plunders life force and commands the dead. Powerful but hungry — every casting leaves a mark. Bonecraft Mystics smell faintly of the grave. Skull-aligned.",
            new Dictionary<string, string>
            {
                ["Range"] = "Death Reach", ["Duration"] = "Undead Hold",
                ["Area"]  = "Necrotic Spread", ["Power"] = "Drain Bite",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Drain"]       = new(1, "Suck the strength from a target; -1 to all their rolls until rest."),
                ["Raise Dead"]  = new(1, "Animate a corpse as a shambling servant. One limit per purchase."),
                ["Rot"]         = new(2, "Flesh rots: 1d8 necrotic and target Armor drops by 1."),
                ["Wither"]      = new(2, "Target loses 1 max HP per round for three rounds."),
                ["Harvest"]     = new(2, "Steal HP from target and grant them to an ally or yourself."),
                ["Death Ward"]  = new(2, "Target stabilises at 0 HP once. Won't rise but won't die."),
                ["Soul Cage"]   = new(3, "Trap a departing soul; prevent resurrection until you release it."),
                ["Lich-Touch"]  = new(3, "Transform the target into a mindless undead servant permanently."),
            },
            IsCrown: false),

        ["Stormcall"] = new(
            "#4499FF", "⚡", "Raw chaos, lightning, wild surges, uncontrolled power",
            "Stormcall is raw chaos shaped poorly into words. It hits hard and fast but no Stormcall Mystic has ever been accused of subtlety — or sanity. Wild surges are expected. Skull-aligned.",
            new Dictionary<string, string>
            {
                ["Range"] = "Storm Reach", ["Duration"] = "Surge Hold",
                ["Area"]  = "Arc Spread", ["Power"] = "Thunder Bite",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Lightning Strike"] = new(1, "Bolt of lightning: 1d10 damage. Can arc to an adjacent target."),
                ["Wild Surge"]       = new(1, "Uncontrolled burst of random energy. Roll d6 for random chaos."),
                ["Thunder Crack"]    = new(1, "Deafening blast; target deafened and staggered for 1 round."),
                ["Storm Wall"]       = new(2, "Curtain of crackling lightning. Entering deals 2d6; difficult terrain."),
                ["Arc Chain"]        = new(2, "Lightning jumps between up to three targets."),
                ["Unleash"]          = new(2, "Double damage dice but also roll once on wild surge table."),
                ["Tempest"]          = new(3, "Summon a local storm for the scene; ranged attacks at disadvantage."),
                ["Unravel"]          = new(3, "Strip and dissipate all magical protections on target."),
            },
            IsCrown: false),

        ["Bloodrite"] = new(
            "#CC3333", "✚", "Sacrifice, oath-magic, war, transformation",
            "Bloodrite ties magic to sacrifice and transformation. Power is measured in what you give up. Bloodrite Mystics are feared for what they do to themselves, not just to others. Skull-aligned.",
            new Dictionary<string, string>
            {
                ["Range"] = "Oath Reach", ["Duration"] = "Rite Hold",
                ["Area"]  = "Sacrifice Spread", ["Power"] = "War Bite",
            },
            new Dictionary<string, AbilityDef>
            {
                ["Blood Oath"]         = new(1, "Bind a creature to an oath with a blood mark. Break it = 1d10."),
                ["War Cry"]            = new(1, "Scream empowers all nearby allies; +1 to attacks for 1 round."),
                ["Transform"]          = new(2, "Assume a beast-form aspect; +2 to one stat, -1 to another."),
                ["Bleed"]              = new(2, "Open a wound that refuses to close; 1d4 per round, cumulative."),
                ["Empower Through Pain"]= new(2, "Take 1d6 damage to deal +2d6 on next attack this round."),
                ["Mark of Ruin"]       = new(2, "Branded target takes +2 damage from all sources until mark fades."),
                ["Berserker"]          = new(3, "Blood rage: +4 damage, ignore first wound, cannot retreat."),
                ["Blood Price"]        = new(3, "Sacrifice HP to fuel a massively amplified spell effect."),
            },
            IsCrown: false),
    };

    // ── Path connections (synergies) ──────────────────────────────
    public static readonly IReadOnlyDictionary<(string, string), string> SchoolConnections =
        new Dictionary<(string, string), string>
        {
            // Crown-Crown
            [("Wardlight","Ironcraft")]   = "Sacred Forge — holy fire tempers iron; warded weapons are blessed and unbreakable",
            [("Wardlight","Thornvine")]   = "Holy Grove — healing magic doubled within sacred ground; the wild is blessed",
            [("Wardlight","Stonesong")]   = "Eternal Bastion — ward becomes an unbreakable stone sanctuary until dawn",
            [("Ironcraft","Thornvine")]   = "Living Iron — tools grow and self-repair; vines bind without ever rusting",
            [("Ironcraft","Stonesong")]   = "Deep Forge — draw ore from living stone; ancient memory flows into the craft",
            [("Thornvine","Stonesong")]   = "Old Root — roots reach bedrock; terrain reshapes to wild and ancient forms",
            // Skull-Skull
            [("Shadowmark","Bonecraft")]  = "The Unseen Dead — undead vanish from sight entirely; death and shadow united",
            [("Shadowmark","Stormcall")]  = "Black Storm — lightning strikes from nowhere; chaos comes out of shadow",
            [("Shadowmark","Bloodrite")]  = "Assassin's Mark — target blood-marked through shadow; strike from any range",
            [("Bonecraft","Stormcall")]   = "Death Storm — surging lightning raises the slain as they fall in its wake",
            [("Bonecraft","Bloodrite")]   = "Hunger of Bone — sacrifice empowers the undead; living blood feeds the risen",
            [("Stormcall","Bloodrite")]   = "War Surge — blood sacrifice unleashes pure chaos; berserk storm of ruin",
            // Crown-Skull
            [("Wardlight","Shadowmark")]  = "Half-Veil — holy wards catch what shadows hide; light traps the darkness",
            [("Wardlight","Bonecraft")]   = "Death's Ward — the ward bars return of the dead; holy fire burns the risen",
            [("Wardlight","Stormcall")]   = "Storm of Heaven — divine lightning; pure chaos shaped by holy will",
            [("Wardlight","Bloodrite")]   = "Sacred Wound — self-harm empowers holy might; suffering as holy offering",
            [("Ironcraft","Shadowmark")]  = "Shadowforged — blades that phase through armor; seals no living eye can find",
            [("Ironcraft","Bonecraft")]   = "Bone Iron — soldiers of iron and bone; animate plate armor walks",
            [("Ironcraft","Stormcall")]   = "Stormsteel — lightning forged into weapons; thundering war-craft",
            [("Ironcraft","Bloodrite")]   = "Blood-Bound Iron — weapons sealed with a blood oath; indestructible war-bond",
            [("Thornvine","Shadowmark")]  = "Shadow Wood — the forest watches unseen; vines and shade hunt as one",
            [("Thornvine","Bonecraft")]   = "Death Bloom — poisoned decay flowers; roots drink the stolen life-force",
            [("Thornvine","Stormcall")]   = "Wild Storm — lightning calls the beasts; charged thorns answer the gale",
            [("Thornvine","Bloodrite")]   = "Blood Harvest — wounds feed the roots; sacrifice empowers the wild",
            [("Stonesong","Shadowmark")]  = "The Deep Dark — stone passages of shadow; ancient buried secrets uncovered",
            [("Stonesong","Bonecraft")]   = "Bone Mound — the dead walk from barrow ground; mass ancient rising",
            [("Stonesong","Stormcall")]   = "Thunder Rock — lightning strikes carve stone; geological violence",
            [("Stonesong","Bloodrite")]   = "Blood Rune — blood carved in stone endures forever; ancient power binding",
        };

    // ── Casting properties ────────────────────────────────────────
    public static readonly IReadOnlyDictionary<string, ModDef> DefaultGlobalMods =
        new Dictionary<string, ModDef>
        {
            ["Reach: Touch"]         = new("Range",    0, 1, "Requires physical contact with the target."),
            ["Reach: Close 10ft"]    = new("Range",    1, 1, "Short weapon-reach range."),
            ["Reach: Near 60ft"]     = new("Range",    2, 1, "Standard line-of-sight range."),
            ["Reach: Far 300ft"]     = new("Range",    3, 1, "Extreme battlefield range."),
            ["Reach: Sight"]         = new("Range",    4, 1, "Anywhere you can directly see."),
            ["Reach: Horizon"]       = new("Range",    5, 1, "Crosses any distance, even planar."),
            ["Hold: Flash"]          = new("Duration", 0, 1, "A single instant; no lingering effect."),
            ["Hold: Breath"]         = new("Duration", 1, 1, "Lasts until your next turn."),
            ["Hold: Held"]           = new("Duration", 1, 1, "Maintained while you hold concentration."),
            ["Hold: Scene"]          = new("Duration", 2, 1, "Lasts for the current scene (≈10 rounds)."),
            ["Hold: Watch"]          = new("Duration", 3, 1, "Lasts several hours until broken."),
            ["Hold: Dawn"]           = new("Duration", 4, 1, "Persists until the next dawn."),
            ["Hold: Permanent"]      = new("Duration", 7, 1, "Persists until deliberately dispelled."),
            ["Spread: Single"]       = new("Area",     0, 1, "Affects exactly one creature."),
            ["Spread: Cone 15ft"]    = new("Area",     1, 1, "15-foot cone from caster."),
            ["Spread: Line 30ft"]    = new("Area",     1, 1, "30-foot line, 5 feet wide."),
            ["Spread: Burst 10ft"]   = new("Area",     2, 1, "10-foot radius sphere."),
            ["Spread: Burst 30ft"]   = new("Area",     3, 1, "30-foot radius sphere."),
            ["Spread: Many Targets"] = new("Area",     2, 5, "Choose up to 3 targets. Each purchase adds 2."),
            ["Spread: Aura"]         = new("Area",     2, 3, "Radiates 10ft from caster; each purchase +10ft."),
            ["Spread: Zone 60ft"]    = new("Area",     4, 1, "60-foot radius sphere."),
            ["Bite: Weak d4"]        = new("Power",    0, 1, "Minimal effect, barely a cantrip."),
            ["Bite: Sharp d6"]       = new("Power",    2, 1, "Standard combat-grade power."),
            ["Bite: Fierce d8"]      = new("Power",    4, 1, "Dangerous and memorable."),
            ["Bite: Dread d10"]      = new("Power",    6, 1, "Significantly threatening."),
            ["Bite: Mighty d12"]     = new("Power",    8, 1, "Legendary magnitude."),
            ["Bite: Elder 2d10"]     = new("Power",   10, 1, "Eldersign-level devastation."),
            ["Extra Damage Die"]     = new("Power",    2,10, "Add one additional damage die per purchase."),
            ["Cast: Free"]           = new("Casting",  3, 1, "No action economy cost."),
            ["Cast: Quick"]          = new("Casting",  2, 1, "Uses your quick action."),
            ["Cast: Standard"]       = new("Casting",  0, 1, "Uses your primary action."),
            ["Cast: Full Round"]     = new("Casting", -1, 1, "Entire round to cast; roll penalty."),
            ["Cast: Rite 1 Min"]     = new("Casting", -2, 1, "1-minute ritual cast."),
            ["Cast: Deep Rite"]      = new("Casting", -3, 1, "10-minute deep ritual. Reach and area doubled."),
            ["Silent Casting"]       = new("Special",  1, 1, "Removes spoken component."),
            ["Gestless"]             = new("Special",  1, 1, "Removes somatic component."),
            ["Subtle Sign"]          = new("Special",  2, 1, "Both spoken and somatic suppressed."),
            ["Maximised"]            = new("Special",  2, 1, "All variable effects use maximum value."),
            ["Empowered"]            = new("Special",  2, 2, "Reroll damage dice; take the higher result."),
            ["Twin Sign"]            = new("Special",  3, 1, "Target a second creature simultaneously."),
            ["Raised DC"]            = new("Special",  2, 3, "Raise saving throw DC by 2 per purchase."),
            ["Stubborn Weave"]       = new("Special",  2, 1, "Dispel attempts against this sign have disadvantage."),
            ["Clean Spread"]         = new("Special",  2, 1, "Exclude chosen creatures from area of effect."),
            ["Delayed Trigger"]      = new("Special",  2, 3, "Delay activation until a trigger (up to 24hr each)."),
            ["Volley"]               = new("Special",  2, 3, "Each purchase adds one additional volley strike."),
            ["Extended Reach"]       = new("Special",  1, 5, "Double the sign's reach per purchase."),
        };

    // ── Doom Marks (Drawbacks) ────────────────────────────────────
    public static readonly IReadOnlyList<NegModDef> DefaultNegativeMods = new List<NegModDef>
    {
        // ── Minor (1 pt) — narrative flavour, negligible mechanical impact
        new("Spoken Word",      "The sign cannot be cast silently; the activation word is always audible.",       1),
        new("Glowing Mark",     "Sign leaves a faint rune glyph visible at the casting site for 1 hour.",        1),
        new("Loud Sign",        "Casting announces itself with a sharp crack of sound heard 60ft away.",         1),
        new("Wild Gesture",     "Requires a full dramatic casting gesture visible to all nearby.",                1),

        // ── Moderate (2 pts) — meaningful tactical drawbacks
        new("Wild Surge",       "15% misfire chance — sign targets a random creature in range.",                  2),
        new("Blood Price",      "Caster takes 1d6 damage each time this sign is cast.",                          2),
        new("Bone Tribute",     "Requires a bone, tooth, or feather from a slain creature as a component.",      2),
        new("Anchored",         "Cannot move more than 10ft while maintaining this effect.",                     2),
        new("Telegraphed",      "Targets receive one full round of warning before the sign activates.",          2),
        new("Brittle Weave",    "Any damage to the caster shatters this sign instantly.",                        2),
        new("Doom Dice",        "Roll d6 on the Doom table in addition to the normal effect.",                   2),
        new("Cracked Ward",     "Any physical blow against the caster ends this sign immediately.",              2),
        new("Recovery Needed",  "Cannot cast again until you skip one action to recover breath.",                2),
        new("Skull Fog",        "Caster has disadvantage on all Wit rolls until next rest.",                     2),
        new("Rooted",           "Sign ends if caster moves more than 30ft from the point of casting.",          2),

        // ── Severe (3 pts) — major mechanical or permanent penalties
        new("Stunned",          "Caster is stunned for 1 round after casting this sign.",                        3),
        new("Aged",             "Caster physically ages 1 year each time this sign is cast.",                    3),
        new("Bone-Tired",       "Caster gains 1 Fatigue mark immediately after casting.",                        3),
        new("Skull-Marked",     "Caster suffers 1 point of permanent Constitution reduction per casting.",       3),
        new("Debt to Fate",     "Caster's next saving throw is automatically failed — no roll allowed.",         3),
    };

    // ── Skill Die table ───────────────────────────────────────────
    public static readonly IReadOnlyList<LevelEntry> LevelTable = new List<LevelEntry>
    {
        new(  0,  2, "Hedge Sign",  "#888888"),
        new(  3,  6, "d4 Sign",     "#aa88ee"),
        new(  7, 12, "d6 Craft",    "#8899ff"),
        new( 13, 20, "d8 Craft",    "#4488ee"),
        new( 21, 30, "d10 Mystic",  "#44ccbb"),
        new( 31, 44, "d12 Mystic",  "#44dd88"),
        new( 45, 60, "d20 Elder",   "#99dd22"),
        new( 61, 80, "Elder Sign",  "#ffcc00"),
        new( 81,100, "Dread Sign",  "#ff8800"),
        new(101,999, "Living Myth", "#ff2200"),
    };

    // ── Helper: find path pair for synergy ───────────────────────
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
