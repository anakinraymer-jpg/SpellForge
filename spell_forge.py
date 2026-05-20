#!/usr/bin/env python3
"""
╔══════════════════════════════════════════════════════════════╗
║            S P E L L F O R G E   v6.0                       ║
║       RPG Magic Circle Spell Construction System             ║
╚══════════════════════════════════════════════════════════════╝

ARCHITECTURE OVERVIEW
─────────────────────
Single-file Python 3.8+ application using tkinter for the GUI.
No external dependencies beyond the standard library.

RUN:
    python spell_forge.py

LAYOUT (three resizable panes):
  Left  — Identity tab, Elements tab, one tab per school (10 total)
  Centre — Magic circle canvas (zoom/pan with scroll + right-drag)
  Right  — Calculator, Modifiers, Synergies, Guide, Library tabs

MAGIC CIRCLE LAYERS (bottom → top):
  1. Parchment background  (_draw_deep_bg)
  2. Spell-level rings     (_draw_level_rings)   ← behind everything
  3. Outer frame           (_draw_outer_frame)   ← double border + connector ring
  4. Inner sacred geometry (_draw_main_geometry) ← chord web, confined to R*0.50
  5. Element band          (_draw_element_band)  ← constellation nodes at R*0.55
  6. School modules × 10  (_draw_school_modules) ← always visible on outer ring
  7. Center modifier hub   (_draw_center_hub)
  8. Status bar text

SCHOOL MODULES (all 10 always rendered):
  • Outer rune ring (r*0.84): one rune per school ability — bright if purchased
  • Inner rune ring (r*0.64): ring modifier runes — bright if filled
  • 8 radial spokes, central pentagon, school symbol in centre
  • Active (selected) schools = full brightness; inactive = 20% dim

DATA MODEL (Spell dataclass):
  primary_school      str
  secondary_schools   List[str]
  school_abilities    Dict[school, Dict[ability_name, count]]
  ring_mods           Dict[school, Dict[group, count]]   ← 0-3 per group
  global_mods         Dict[mod_name, count]
  elements            Dict[el_name, True | subtype_str]
  element_nodes       Dict[el_name, Dict[node_name, count]]
  subelement_nodes    Dict["el1,el2", Dict[node_name, count]]
  circle_sizes        Dict[school, float]   ← 0.4–2.2× scale
  custom_effects      List[str]

KNOWN EXTENSION POINTS:
  • SCHOOLS dict — add new schools here
  • ELEMENTS dict — add/modify elements and their node trees
  • SUBELEMENT_NODES dict — sub-element upgrade constellations
  • CAPSTONES dict — glyph + ring + name + desc per school
  • DEFAULT_GLOBAL_MODS — spell modifier library
  • LEVEL_TABLE — 15 level thresholds

SAVE/LOAD: JSON via filedialog. Export: PNG (requires Pillow) or .txt.
"""

import tkinter as tk
from tkinter import ttk, messagebox, filedialog
import math, json
from dataclasses import dataclass, field, asdict
from typing import List, Dict, Optional, Tuple
from copy import deepcopy

# ═══════════════════════════════════════════════════════════════
#  RUNE / GLYPH SETS
# ═══════════════════════════════════════════════════════════════
RUNES_ELDER = "ᚠᚢᚦᚨᚱᚲᚷᚹᚺᚾᛁᛃᛇᛈᛉᛊᛏᛒᛖᛗᛚᛜᛞᛟ"
RUNES_YOUNG = "ᚠᚢᚦᚬᚱᚴᚼᚾᛁᛅᛋᛏᛒᛘᛚ"
RUNES_MED   = "ᛡᛢᛣᛤᛥᛮᛯᛰ"
RUNES_OGHAM = "ᚁᚂᚃᚄᚅᚆᚇᚈᚉᚊᚋᚌᚍᚎᚏᚐᚑᚒᚓᚔᚕᚖᚗᚘᚙᚚ"
ALCH_SYMS   = "☿♄♃♂☉♀☽⊕⊗⊙⊛⊚⊜⊝◉◎◌◍⊞⊟⊠⊡"
RUNES_ALL   = RUNES_ELDER + RUNES_YOUNG + RUNES_MED + RUNES_OGHAM + ALCH_SYMS

MOD_RUNES = {
    "Range":    ["ᚱ","ᚠ","ᛁ","ᛏ","ᛊ","⊕"],
    "Duration": ["ᛞ","ᛟ","ᚦ","ᛖ","ᚹ","⊙"],
    "Area":     ["ᚨ","ᚷ","ᛃ","ᛈ","ᛚ","◎"],
    "Power":    ["ᛒ","ᛗ","ᚾ","ᛜ","ᚺ","⊗"],
    "Casting":  ["ᛇ","ᚲ","ᚢ","ᛦ","ᛡ","⊛"],
    "Special":  ["ᛥ","ᛣ","ᛤ","ᛢ","ᛰ","⊚"],
}

CAT_COLORS = {
    "Range":"#ff6060","Duration":"#60ee88","Area":"#6088ff",
    "Power":"#ffd060","Casting":"#ff88ff","Special":"#88ffff",
}

SCHOOL_GLYPHS = {
    "Evocation":     ["⚡","✸","✺","☄","⊕","⊗","∞","✦"],
    "Transmutation": ["⚗","⚙","⌬","◈","⍟","⎔","⬡","⌾"],
    "Enchantment":   ["✿","❀","⚜","☙","❧","✤","❆","⚘"],
    "Space":         ["◉","⌖","◎","⍥","⍉","⌀","⊞","⊟"],
    "Divination":    ["⊚","◬","⌭","⍚","⍛","⍜","◌","◍"],
    "Time":          ["⌛","⍨","⍩","⍪","⍫","⍬","⍭","⌬"],
    "Necromancy":    ["☽","☾","☠","☿","♄","⚸","⊗","✝"],
    "Conjuration":   ["★","✩","✪","✫","✬","✭","✮","✯"],
    "Illusion":      ["◆","⬡","⬢","△","▽","◭","◮","◈"],
    "Abjuration":    ["⛨","⛊","⊕","◧","◨","◩","◪","⊠"],
}

# ═══════════════════════════════════════════════════════════════
#  CAPSTONE ABILITIES + SIGILS
# ═══════════════════════════════════════════════════════════════
CAPSTONES = {
    # "glyph": single large Unicode character; "ring": 8-char orbital rune string
    "Evocation": {
        "name": "Apocalyptic Barrage",
        "desc": "Unleash 10 simultaneous blasts of any element, ignoring all resistances and immunities.",
        "glyph":"☄","ring":"⚡✸⚡✦⚡✸⚡✦",
        "color": "#FF6600",
    },
    "Transmutation": {
        "name": "Philosopher's Rewrite",
        "desc": "Permanently alter the fundamental nature of any creature, object, or location.",
        "glyph":"⚗","ring":"⌬◈⚙⎔⌬◈⚙⎔",
        "color": "#FFD700",
    },
    "Enchantment": {
        "name": "Absolute Dominion",
        "desc": "Your will becomes law for all creatures you can perceive. No save allowed.",
        "glyph":"❁","ring":"✿⚜❧✤✿⚜❧✤",
        "color": "#FF69B4",
    },
    "Space": {
        "name": "Infinite Fold",
        "desc": "Collapse all distance to zero. Instant transit to any point in the multiverse.",
        "glyph":"◎","ring":"◉⊞⍥⌀◉⊞⍥⌀",
        "color": "#1E90FF",
    },
    "Divination": {
        "name": "Omniscient Moment",
        "desc": "Perceive all timelines, all truths, all minds. Ask any three questions.",
        "glyph":"⊚","ring":"◬⍚⌭◍◬⍚⌭◍",
        "color": "#C8C8FF",
    },
    "Time": {
        "name": "Temporal Supremacy",
        "desc": "Stop time for up to 1 hour. Act freely while the world is frozen.",
        "glyph":"⌛","ring":"⍭⍩⍪⍨⍭⍩⍪⍨",
        "color": "#00CED1",
    },
    "Necromancy": {
        "name": "Death Transcendence",
        "desc": "Become unkillable for 24 hours. Raise all slain as permanent undead servants.",
        "glyph":"☠","ring":"☽☾☿♄☽☾☿♄",
        "color": "#9400D3",
    },
    "Conjuration": {
        "name": "Grand Summoning",
        "desc": "Summon up to 20 CR20 entities from any plane, permanently bound and obedient.",
        "glyph":"★","ring":"✮✭✬✩✮✭✬✩",
        "color": "#32CD32",
    },
    "Illusion": {
        "name": "Reality Overwrite",
        "desc": "Replace all sensory reality for every creature within 1 mile with your constructed world.",
        "glyph":"◆","ring":"◈△▽⬡◈△▽⬡",
        "color": "#FF8C00",
    },
    "Abjuration": {
        "name": "Absolute Sanctuary",
        "desc": "Impenetrable ward over 1 mile. Nothing enters or exits without your permission.",
        "glyph":"⛨","ring":"◩⊕⛊◧◩⊕⛊◧",
        "color": "#C0C0C0",
    },
}

# ═══════════════════════════════════════════════════════════════
#  ELEMENTS + SUB-ELEMENT CONNECTIONS
# ═══════════════════════════════════════════════════════════════
ELEMENT_CONNECTIONS: Dict[Tuple[str,str],str] = {
    ("Fire","Water"):    "Steam — scalding vapour, obscures vision, burns through armour",
    ("Fire","Earth"):    "Magma — molten rock, unstoppable, leaves scorched terrain",
    ("Fire","Wind"):     "Firestorm — whirling inferno, empowers all fire effects",
    ("Fire","Celestial"):"Divine Flame — purifying holy fire, destroys undead",
    ("Water","Earth"):   "Mud — entangles and slows targets, difficult terrain",
    ("Water","Wind"):    "Ice Storm — freezing blizzard, area slow, slick surfaces",
    ("Water","Celestial"):"Holy Water — purifies corruption, disrupts fiends",
    ("Earth","Wind"):    "Dust Devil — blinding dust, concealment, minor damage",
    ("Earth","Celestial"):"Sacred Stone — permanent glyphs etched in unbreakable rock",
    ("Wind","Celestial"):"Tempest — divine storm, deafens, discharges radiant lightning",
}

# Each element has "nodes": list of upgrade dicts {name, cost, rune, desc, effect}
# Purchased nodes stored in spell.element_nodes[el][node_name] = count
ELEMENTS = {
    "Fire": {
        "color":"#FF4500","symbol":"🜂","rune":"ᚠ",
        "modification":"Damage Type: Fire  |  +2 damage dice  |  Targets ignite (1d6/round)",
        "desc":"Fire element — burning, ignition, heat-based devastation.",
        "nodes":[
            {"name":"Ignite",    "cost":1,"rune":"ᚠ","glyph":"🜂","desc":"Targets ignite: 1d6 fire/turn","effect":"+1d6 ongoing fire"},
            {"name":"Inferno",   "cost":2,"rune":"ᚱ","glyph":"⚡","desc":"Blast radius +10ft","effect":"+10ft area"},
            {"name":"Searing",   "cost":2,"rune":"⊕","glyph":"✸","desc":"Fire pierces cold resistance","effect":"Pierce cold resistance"},
            {"name":"Conflagrate","cost":3,"rune":"ᚷ","glyph":"☄","desc":"Chain fire between targets","effect":"Chain to 2nd target"},
            {"name":"Pyre",      "cost":3,"rune":"ᛁ","glyph":"✺","desc":"Leave persistent fire zone","effect":"10ft fire zone 2 rounds"},
        ],
    },
    "Water": {
        "color":"#1E90FF","symbol":"🜄","rune":"ᚹ",
        "modification":"Damage Type: Cold  |  Slow target (½ speed)  |  Extinguish fire",
        "desc":"Water element — cold, slowing, and flowing force.",
        "nodes":[
            {"name":"Chill",     "cost":1,"rune":"ᚹ","glyph":"🜄","desc":"Target slowed ½ speed","effect":"½ speed 1 round"},
            {"name":"Freeze",    "cost":2,"rune":"ᛟ","glyph":"❄","desc":"Target restrained on failed save","effect":"Restrained 1 round"},
            {"name":"Torrent",   "cost":2,"rune":"ᚦ","glyph":"⌀","desc":"Push target 20ft","effect":"Push 20ft"},
            {"name":"Maelstrom", "cost":3,"rune":"⊙","glyph":"◉","desc":"Pull all targets toward centre","effect":"Pull 15ft"},
            {"name":"Deluge",    "cost":3,"rune":"ᛚ","glyph":"◎","desc":"Douse all fire effects in area","effect":"Extinguish zone"},
        ],
    },
    "Earth": {
        "color":"#8B4513","symbol":"🜃","rune":"ᚦ",
        "modification":"Damage Type: Bludgeoning  |  Knock Prone  |  +4 to Hit",
        "desc":"Earth element — weight, force, and immovability.",
        "nodes":[
            {"name":"Tremor",    "cost":1,"rune":"ᚦ","glyph":"🜃","desc":"Knock prone on hit","effect":"Knock prone"},
            {"name":"Boulder",   "cost":2,"rune":"ᛒ","glyph":"⌬","desc":"+1d8 bludgeoning damage","effect":"+1d8 damage"},
            {"name":"Entomb",    "cost":2,"rune":"ᛗ","glyph":"⎔","desc":"Restrain target in stone","effect":"Restrained 1 round"},
            {"name":"Quake",     "cost":3,"rune":"ᚾ","glyph":"◈","desc":"All in area fall prone","effect":"Area prone"},
            {"name":"Monolith",  "cost":3,"rune":"ᛜ","glyph":"⬡","desc":"Create impassable stone wall","effect":"Stone wall 20ft"},
        ],
    },
    "Wind": {
        "color":"#87CEEB","symbol":"🜁","rune":"ᚨ",
        "modification":"Damage Type: Thunder  |  Push 30ft  |  Fly Speed granted",
        "desc":"Wind element — thunder, movement, and airborne force.",
        "nodes":[
            {"name":"Gust",      "cost":1,"rune":"ᚨ","glyph":"🜁","desc":"Push target 15ft","effect":"Push 15ft"},
            {"name":"Updraft",   "cost":2,"rune":"ᚷ","glyph":"△","desc":"Caster gains fly speed 30ft","effect":"Fly 30ft 1 round"},
            {"name":"Deafen",    "cost":2,"rune":"ᛦ","glyph":"◌","desc":"Thunder deafens on hit","effect":"Deafened 1 round"},
            {"name":"Cyclone",   "cost":3,"rune":"ᛡ","glyph":"⍥","desc":"Spin targets: disadvantage attacks","effect":"Disadvantage attacks"},
            {"name":"Tempest",   "cost":3,"rune":"⊛","glyph":"⍉","desc":"Lightning strikes random target","effect":"Random lightning 2d6"},
        ],
    },
    "Celestial": {
        "color":"#FFD700","symbol":"✦","rune":"⊕",
        "modification":"Choose Subtype: Radiant / Necrotic / Psychic",
        "desc":"Celestial element — divine or eldritch energy in three subtypes.",
        "subtypes":{
            "Radiant": {"color":"#FFFFAA","rune":"☉",
                "modification":"Radiant damage  |  Heal allies = damage dealt  |  Undead ×2",
                "desc":"Divine light heals allies and destroys undead."},
            "Necrotic": {"color":"#AA44FF","rune":"☽",
                "modification":"Necrotic damage  |  Drain max HP  |  Empower undead",
                "desc":"Life drain reduces max HP and strengthens undead."},
            "Psychic": {"color":"#FF88FF","rune":"⊚",
                "modification":"Psychic damage  |  Stun 1 round  |  Int save or confused",
                "desc":"Mind-shattering force stuns and confuses targets."},
        },
        "nodes":[
            {"name":"Hallow",    "cost":1,"rune":"⊕","glyph":"✦","desc":"Area becomes sacred ground","effect":"Hallowed zone"},
            {"name":"Smite",     "cost":2,"rune":"☉","glyph":"✸","desc":"+1d8 radiant/necrotic/psychic","effect":"+1d8 typed damage"},
            {"name":"Ward",      "cost":2,"rune":"☽","glyph":"⊚","desc":"Allies gain +2 AC in aura","effect":"+2 AC aura"},
            {"name":"Ascend",    "cost":3,"rune":"⊛","glyph":"⍜","desc":"Temporary invulnerability 1 round","effect":"Invulnerable 1 round"},
            {"name":"Judgment",  "cost":3,"rune":"⊗","glyph":"⍚","desc":"Double damage vs opposite alignment","effect":"×2 vs opposed"},
        ],
    },
}

# Sub-element constellation nodes (appear when both parents active)
SUBELEMENT_NODES: Dict[Tuple[str,str],List[dict]] = {
    ("Fire","Water"): [
        {"name":"Steam Veil",  "cost":1,"rune":"ᚠ","glyph":"⌀","desc":"Obscuring steam cloud 10ft","effect":"Obscured zone"},
        {"name":"Scald",       "cost":2,"rune":"ᚹ","glyph":"◎","desc":"Fire+Cold combo: 1d6 each","effect":"+1d6 fire +1d6 cold"},
        {"name":"Geyser",      "cost":3,"rune":"⊕","glyph":"✺","desc":"Erupting steam pillar, push+burn","effect":"Push+ignite"},
    ],
    ("Fire","Earth"): [
        {"name":"Lava Crack",  "cost":1,"rune":"ᚦ","glyph":"⌬","desc":"Molten fissure in ground","effect":"Difficult terrain+fire"},
        {"name":"Magma Surge", "cost":2,"rune":"ᚠ","glyph":"☄","desc":"Eruption: 2d6 fire+bludgeon","effect":"2d6 hybrid damage"},
        {"name":"Slag",        "cost":3,"rune":"ᛒ","glyph":"⎔","desc":"Solidify target in cooled lava","effect":"Restrained permanent"},
    ],
    ("Fire","Wind"): [
        {"name":"Firestorm",   "cost":1,"rune":"ᚨ","glyph":"✸","desc":"Spinning fire 20ft radius","effect":"20ft fire zone"},
        {"name":"Phoenix Wing","cost":2,"rune":"ᚱ","glyph":"⍥","desc":"Fire+fly: blazing charge","effect":"Fly+fire trail"},
        {"name":"Inferno Wind","cost":3,"rune":"⊛","glyph":"⊗","desc":"All fire effects maximised","effect":"Max fire damage"},
    ],
    ("Fire","Celestial"): [
        {"name":"Seraphs Flame","cost":1,"rune":"☉","glyph":"✦","desc":"Divine fire heals allies","effect":"Fire heals allies"},
        {"name":"Purgation",   "cost":2,"rune":"⊕","glyph":"⊕","desc":"Burn away curses and conditions","effect":"Remove 1 condition"},
        {"name":"Immolation",  "cost":3,"rune":"⊗","glyph":"☄","desc":"Full body fire halo, aura damage","effect":"5ft fire aura"},
    ],
    ("Water","Earth"): [
        {"name":"Mudslide",    "cost":1,"rune":"ᚦ","glyph":"🜃","desc":"Engulf area in thick mud","effect":"Restrained+difficult"},
        {"name":"Bog Pull",    "cost":2,"rune":"ᚹ","glyph":"◉","desc":"Sink target, hold prone","effect":"Prone+restrained"},
        {"name":"Earthen Wave","cost":3,"rune":"ᛗ","glyph":"◈","desc":"Tidal wave of mud: area+pull","effect":"Area pull+prone"},
    ],
    ("Water","Wind"): [
        {"name":"Blizzard",    "cost":1,"rune":"ᚨ","glyph":"❄","desc":"Snowstorm reduces visibility","effect":"Heavily obscured"},
        {"name":"Ice Lance",   "cost":2,"rune":"ᛟ","glyph":"⌀","desc":"Piercing ice spear, ignore AC","effect":"+2d6 pierce+cold"},
        {"name":"Hailstorm",   "cost":3,"rune":"⊙","glyph":"⍉","desc":"Hail: stun+prone+cold","effect":"Area stun+prone"},
    ],
    ("Water","Celestial"): [
        {"name":"Holy Font",   "cost":1,"rune":"☉","glyph":"🜄","desc":"Blessed water purifies","effect":"Remove poison/curse"},
        {"name":"Tide of Life","cost":2,"rune":"⊕","glyph":"◎","desc":"Healing wave to all allies","effect":"Heal 2d6 allies"},
        {"name":"Absolution",  "cost":3,"rune":"⊛","glyph":"✦","desc":"Wash away undeath","effect":"Destroy undead ≤CR5"},
    ],
    ("Earth","Wind"): [
        {"name":"Dust Devil",  "cost":1,"rune":"ᚨ","glyph":"⍥","desc":"Blinding dust vortex","effect":"Blinded 1 round"},
        {"name":"Rock Gust",   "cost":2,"rune":"ᚦ","glyph":"⌬","desc":"Debris launch: 1d10 piercing","effect":"1d10 piercing"},
        {"name":"Landslide",   "cost":3,"rune":"ᚷ","glyph":"△","desc":"Massive debris avalanche","effect":"Area prone+buried"},
    ],
    ("Earth","Celestial"): [
        {"name":"Sacred Stone","cost":1,"rune":"⊕","glyph":"🜃","desc":"Permanent ward glyph in earth","effect":"Permanent ward"},
        {"name":"Rune Seal",   "cost":2,"rune":"☉","glyph":"⊠","desc":"Anti-magic zone in stone","effect":"Anti-magic zone"},
        {"name":"Earthen Tomb","cost":3,"rune":"⊗","glyph":"⎔","desc":"Entomb target in holy stone","effect":"Imprisoned"},
    ],
    ("Wind","Celestial"): [
        {"name":"Tempest Call","cost":1,"rune":"☙","glyph":"🜁","desc":"Divine storm arrives","effect":"Storm zone 30ft"},
        {"name":"Angel Rush",  "cost":2,"rune":"⊕","glyph":"△","desc":"Celestial speed: move+attack twice","effect":"Extra move+attack"},
        {"name":"Maelstrom",   "cost":3,"rune":"⊛","glyph":"⍚","desc":"Divine whirlwind: lift+radiant","effect":"Fly+radiant aura"},
    ],
}

# ═══════════════════════════════════════════════════════════════
#  SCHOOLS
# ═══════════════════════════════════════════════════════════════
SCHOOLS: Dict[str,dict] = {
    "Evocation":{"color":"#FF4500","symbol":"⚡","short":"Raw elemental force and energy projection",
        "desc":"Evocation channels raw elemental forces into bolts, blasts, waves, and explosions. It rewards intensity, precision, and sheer power. Masters can fill a battlefield with cascading pillars of flame.",
        "ring_mods":{"Range":"Blast Range","Duration":"Burn Duration","Area":"Explosion Radius","Power":"Raw Damage"},
        "abilities":{
            "Damage Die Upgrade":{"cost":1,"desc":"Upgrade damage die one step per purchase (d4→d6→d8→d10→d12→2d6→2d8→2d10)."},
            "Elemental Type":{"cost":1,"desc":"Add a second elemental damage type per purchase."},
            "Penetrating Energy":{"cost":2,"desc":"Ignore resistance. Buy twice to ignore immunity."},
            "Splash Damage":{"cost":1,"desc":"Adjacent creatures take half damage. Each purchase extends splash 5ft."},
            "Lingering Burn":{"cost":2,"desc":"Target takes ongoing damage equal to spellcasting mod each turn."},
            "Escalating Power":{"cost":2,"desc":"Each sustained round adds +1 damage die."},
            "Chain Lightning":{"cost":3,"desc":"Energy arcs to another target at half damage on hit."},
            "Overload":{"cost":3,"desc":"Bonus action to double damage dice; suffer 1d6 backlash."},
            "Shaped Charge":{"cost":2,"desc":"Exclude creatures equal to casting mod from AoE."},
            "Critical Eruption":{"cost":2,"desc":"On critical hit, erupt outward hitting all within 5ft."},
        }},
    "Transmutation":{"color":"#FFD700","symbol":"⚗","short":"Alter the properties of matter and creatures",
        "desc":"Transmutation rewrites fundamental properties of matter and living beings. Stone becomes iron, lead becomes gold, a man becomes a beast. Deep mastery rewrites magic itself.",
        "ring_mods":{"Range":"Transform Range","Duration":"Change Duration","Area":"Reshape Area","Power":"Transform Depth"},
        "abilities":{
            "Magnitude":{"cost":1,"desc":"Increase scale of transformation per purchase."},
            "Attribute Shift":{"cost":2,"desc":"Raise or lower a creature's ability score by 2 per purchase."},
            "Property Steal":{"cost":3,"desc":"Transfer a material property from one substance to another."},
            "Polymorph Depth":{"cost":2,"desc":"Transform deeper systems per purchase (skin→muscle→skeleton→spirit)."},
            "Reversal Ward":{"cost":2,"desc":"The transmutation resists dispelling per purchase."},
            "Rapid Reshape":{"cost":1,"desc":"Reduce casting time one step per purchase."},
            "Mass Transmute":{"cost":2,"desc":"Affect one additional target per purchase."},
            "Volatile Conversion":{"cost":3,"desc":"Transmuted substance explodes after 1d4 rounds."},
            "Subtle Shift":{"cost":2,"desc":"Transmutation invisible to the eye."},
            "Permanent Alteration":{"cost":4,"desc":"Change persists until magically removed."},
        }},
    "Enchantment":{"color":"#FF69B4","symbol":"✿","short":"Influence minds and bind magical compulsions",
        "desc":"Enchantment touches and rewrites the minds of thinking creatures. Enchanters weave compulsions, suggestions, fear, love, and obedience into the neural fabric of their targets.",
        "ring_mods":{"Range":"Charm Range","Duration":"Compulsion Duration","Area":"Mind Reach","Power":"Will Override"},
        "abilities":{
            "Compulsion Strength":{"cost":1,"desc":"Raise DC to resist by 2 per purchase."},
            "Mind Depth":{"cost":2,"desc":"Reach deeper cognitive layers per purchase."},
            "Affect Immune Minds":{"cost":3,"desc":"Bypass charm immunity categories per purchase."},
            "Persistent Whisper":{"cost":2,"desc":"Residual bias lingers after duration ends."},
            "Cascade Charm":{"cost":2,"desc":"On resistance, enchantment hops to nearest creature per purchase."},
            "Emotional Amplifier":{"cost":2,"desc":"Layer an emotion onto the enchantment."},
            "Memory Edit":{"cost":3,"desc":"Alter or erase up to 1 minute of memory per purchase."},
            "Group Mind":{"cost":3,"desc":"Affect creatures equal to Charisma modifier simultaneously."},
            "Triggered Command":{"cost":2,"desc":"Lies dormant until trigger condition met."},
            "Soul Anchor":{"cost":4,"desc":"Persists across planar travel and mild death."},
        }},
    "Space":{"color":"#1E90FF","symbol":"◉","short":"Bend, fold, and traverse spatial dimensions",
        "desc":"Space magic treats physical dimensions as a medium to compress, stretch, fold, and redirect. A Space mage understands distance is a convention, not a law.",
        "ring_mods":{"Range":"Portal Range","Duration":"Fold Duration","Area":"Rift Breadth","Power":"Spatial Distortion"},
        "abilities":{
            "Teleport Range":{"cost":1,"desc":"Extend teleportation distance per purchase."},
            "Passengers":{"cost":1,"desc":"Bring one additional creature per purchase."},
            "Precision Targeting":{"cost":2,"desc":"Reduce teleportation error. Buy twice for perfect arrival."},
            "Spatial Compression":{"cost":2,"desc":"Compress an area; creatures squeezed take damage per purchase."},
            "Redirect":{"cost":2,"desc":"Micro-portal redirects incoming attack as a reaction."},
            "Dimensional Pocket":{"cost":2,"desc":"Extradimensional storage, capacity ×10 per purchase."},
            "Rift Anchor":{"cost":3,"desc":"Stable persistent portal between two locations."},
            "Blink Step":{"cost":1,"desc":"Teleport up to 15ft as part of movement per purchase."},
            "Gravity Override":{"cost":3,"desc":"Alter local gravity vector in a 20ft cube per purchase."},
            "Void Rift":{"cost":4,"desc":"Vacuum rift damages and pulls creatures each round."},
        }},
    "Divination":{"color":"#C8C8FF","symbol":"⊚","short":"Perceive hidden truths and future possibilities",
        "desc":"Divination reveals what is hidden, distant, or yet to come. A Diviner who knows the enemy's position, weaknesses, and plans before battle has already won.",
        "ring_mods":{"Range":"Scry Range","Duration":"Vision Duration","Area":"Sight Radius","Power":"Truth Penetration"},
        "abilities":{
            "Scry Range":{"cost":1,"desc":"Extend scrying range per purchase: building→city→continent→world→planes."},
            "Information Depth":{"cost":1,"desc":"Reveal deeper truth per purchase."},
            "Duration of Sight":{"cost":1,"desc":"Double divination window duration per purchase."},
            "Precognition Depth":{"cost":2,"desc":"See further into the future per purchase."},
            "Magical Detection":{"cost":1,"desc":"Identify magical auras in more detail per purchase."},
            "Truth Compulsion":{"cost":3,"desc":"Target cannot knowingly conceal information."},
            "Probability Map":{"cost":3,"desc":"See branching probability trees as percentages."},
            "Invisible Eye":{"cost":2,"desc":"Scrying sensor invisible and undetectable."},
            "Sense Emotion":{"cost":1,"desc":"Read surface emotions of creatures per purchase."},
            "Retrocognition":{"cost":3,"desc":"Read history of a location or object per purchase."},
        }},
    "Time":{"color":"#00CED1","symbol":"⌛","short":"Manipulate the flow and fabric of time",
        "desc":"Time magic manipulates temporal flow — slowing enemies, accelerating allies, reversing events, looping moments, or aging matter to dust. True Time mages step outside the timestream entirely.",
        "ring_mods":{"Range":"Temporal Reach","Duration":"Effect Duration","Area":"Time Field","Power":"Chronos Force"},
        "abilities":{
            "Time Slow Degree":{"cost":1,"desc":"Increase time dilation per purchase."},
            "Haste Degree":{"cost":1,"desc":"Increase time acceleration per purchase."},
            "Rewind Depth":{"cost":2,"desc":"Reverse time per action or round per purchase."},
            "Time Stop Duration":{"cost":3,"desc":"Freeze time; each purchase adds one frozen round."},
            "Aging Strike":{"cost":2,"desc":"Age target 10 years per purchase."},
            "Loop Anchor":{"cost":3,"desc":"Create a time loop, max 3 repetitions base."},
            "Temporal Shield":{"cost":2,"desc":"Phase out of timestream to dodge as a reaction."},
            "Future Echo":{"cost":2,"desc":"See 1 round into own future; advantage on all rolls."},
            "Paradox Step":{"cost":4,"desc":"Act twice in one round via alternate timeline."},
            "Entropy Field":{"cost":3,"desc":"Objects and structures crumble per round."},
        }},
    "Necromancy":{"color":"#9400D3","symbol":"☠","short":"Command life force, death, and undeath",
        "desc":"Necromancy operates at the boundary between life and death. Necromancers drain life to fuel their power, reanimate corpses as tireless soldiers, and communicate with the departed.",
        "ring_mods":{"Range":"Death Range","Duration":"Undead Duration","Area":"Necrotic Field","Power":"Life Drain"},
        "abilities":{
            "Undead Tier":{"cost":1,"desc":"Unlock more powerful undead categories per purchase."},
            "Undead Count":{"cost":1,"desc":"Control one additional undead per purchase."},
            "Life Drain Potency":{"cost":1,"desc":"Add 1d6 necrotic damage per purchase; half to caster as THP."},
            "Soul Cage":{"cost":3,"desc":"Trap soul, prevent resurrection, extend per purchase."},
            "Death Ward":{"cost":2,"desc":"Creature stabilizes at 0 HP once per purchase, rises with 1 HP."},
            "Corpse Explosion":{"cost":2,"desc":"Detonate corpse as bonus action; increase per purchase."},
            "Deathsight":{"cost":1,"desc":"See HP auras; sense undead through walls per purchase."},
            "Bone Armor":{"cost":2,"desc":"+2 AC from bone fragments; +1 AC per purchase."},
            "Contagion Spread":{"cost":2,"desc":"Effect spreads from slain to nearby creatures per purchase."},
            "Phylactery Bond":{"cost":4,"desc":"Bind target's life to an object — cannot die while it exists."},
        }},
    "Conjuration":{"color":"#32CD32","symbol":"★","short":"Summon entities, objects, and substances",
        "desc":"Conjuration reaches across space, dimensions, and planes to bring beings, objects, or materials to the caster. From practical conjuring of supplies to summoning archons and rifting planes.",
        "ring_mods":{"Range":"Summon Range","Duration":"Binding Duration","Area":"Conjure Area","Power":"Entity Tier"},
        "abilities":{
            "Summoned Tier":{"cost":1,"desc":"Unlock more powerful entity categories per purchase."},
            "Summoned Count":{"cost":1,"desc":"Summon one additional entity per purchase."},
            "Entity Duration":{"cost":1,"desc":"Extend how long summoned entities remain per purchase."},
            "Obedience Binding":{"cost":2,"desc":"Entity cannot disobey direct commands per purchase."},
            "Material Conjure":{"cost":1,"desc":"Conjure non-magical substance; double volume per purchase."},
            "Planar Anchor":{"cost":3,"desc":"Prevent entity from being dismissed or plane-shifted."},
            "Shared Senses":{"cost":2,"desc":"Telepathic link with summoned entity at unlimited range."},
            "Sacrifice Fuel":{"cost":2,"desc":"Spend HP to empower summoned entity per purchase."},
            "Rift Permanence":{"cost":4,"desc":"Summon portal remains open as two-way passage."},
            "Instant Recall":{"cost":2,"desc":"Dismiss all summoned entities simultaneously as bonus action."},
        }},
    "Illusion":{"color":"#FF8C00","symbol":"◆","short":"Craft false realities and deceive the senses",
        "desc":"Illusion creates false sensory information that bypasses the conscious mind and registers as real. A master Illusionist can make phantom soldiers that trigger genuine fear responses.",
        "ring_mods":{"Range":"Illusion Reach","Duration":"Mirage Duration","Area":"Illusion Size","Power":"Belief Anchor"},
        "abilities":{
            "Sensory Channels":{"cost":1,"desc":"Add one sense per purchase (sight free; add sound, smell, touch, taste, pain)."},
            "Illusion Fidelity":{"cost":1,"desc":"Raise Investigation DC to detect by 3 per purchase."},
            "Illusion Size":{"cost":1,"desc":"Increase maximum size per purchase."},
            "Quasi-Reality":{"cost":3,"desc":"Illusion deals psychic damage to believers."},
            "Lingering Image":{"cost":2,"desc":"Remains after concentration ends, fading over 1 minute."},
            "Mirror Image":{"cost":2,"desc":"Create 1d4+1 illusory duplicates per purchase."},
            "Programmed Behaviour":{"cost":2,"desc":"Illusion acts autonomously on a script per purchase."},
            "Invisibility Depth":{"cost":1,"desc":"Extend invisibility tier per purchase."},
            "Mass Hallucination":{"cost":3,"desc":"Affects all creatures with individually crafted versions."},
            "Reality Anchor":{"cost":4,"desc":"Even disbelievers still perceive it as real."},
        }},
    "Abjuration":{"color":"#C0C0C0","symbol":"⛨","short":"Ward, protect, banish, and negate magic",
        "desc":"Abjuration erects barriers, creates wards, counters spells, banishes entities, and strips enchantments. An Abjurer who has prepared a location is effectively omnipotent within it.",
        "ring_mods":{"Range":"Ward Range","Duration":"Shield Duration","Area":"Ward Coverage","Power":"Barrier Strength"},
        "abilities":{
            "Ward Strength":{"cost":1,"desc":"Add 10 HP or raise damage threshold by 5 per purchase."},
            "Counter Potency":{"cost":1,"desc":"+2 to dispel check per purchase."},
            "Ward Coverage":{"cost":1,"desc":"Extend ward area per purchase."},
            "Planar Exclusion":{"cost":2,"desc":"Bar one creature type or planar origin per purchase."},
            "Spell Reflection":{"cost":3,"desc":"Blocked spells reflect back at caster per purchase."},
            "Anti-Magic Shell":{"cost":4,"desc":"Suppress magical effects; extend radius per purchase."},
            "Alarm Trigger":{"cost":1,"desc":"Alert on trigger rather than blocking per purchase."},
            "Absorb Energy":{"cost":2,"desc":"Ward converts spell damage to temporary HP per purchase."},
            "Glyph of Warding":{"cost":2,"desc":"Store a spell in a glyph triggered by condition."},
            "Dimensional Lock":{"cost":3,"desc":"Prevent teleportation, planar travel, summoning in area."},
        }},
}

SCHOOL_CONNECTIONS: Dict[Tuple[str,str],str] = {
    ("Evocation","Transmutation"):"Catalytic Surge — damage also reshapes targets structurally",
    ("Evocation","Necromancy"):"Soul Scorch — energy damage drains life force simultaneously",
    ("Evocation","Conjuration"):"Elemental Calling — summon beings of your damage type",
    ("Evocation","Space"):"Rift Blast — teleport projectiles through micro-portals",
    ("Evocation","Abjuration"):"Spell Absorb — absorb incoming energy to power your blast",
    ("Evocation","Illusion"):"Phantom Fire — illusory damage that still burns the mind",
    ("Evocation","Time"):"Temporal Detonation — delay a blast to trigger later",
    ("Evocation","Enchantment"):"Awe Strike — raw power forces a fear save on the target",
    ("Evocation","Divination"):"Guided Missile — divination locks perfect aim onto targets",
    ("Transmutation","Time"):"Temporal Morph — reshape a target across multiple time states",
    ("Transmutation","Space"):"Phase Shift — transmute the spatial coordinates of an object",
    ("Transmutation","Necromancy"):"Corpse Alchemy — animate dead by transmuting their matter",
    ("Transmutation","Illusion"):"Mimic Matter — illusions that fool even magical detection",
    ("Transmutation","Enchantment"):"Mind over Matter — reshape self to affect mental state",
    ("Transmutation","Divination"):"Material Truth — read an object's full history by touch",
    ("Transmutation","Conjuration"):"Forge Summons — conjure and transmute simultaneously",
    ("Transmutation","Abjuration"):"Living Ward — transform flesh into a magical barrier",
    ("Enchantment","Illusion"):"Phantasmal Compulsion — illusion plants commands as memory",
    ("Enchantment","Time"):"Memory Weave — rewrite recollections with temporal precision",
    ("Enchantment","Necromancy"):"Soul Binding — bind a spirit's will directly to a command",
    ("Enchantment","Divination"):"Mind Probe — read and subtly influence surface thoughts",
    ("Enchantment","Space"):"Compel Step — enchanted target teleports on command",
    ("Enchantment","Conjuration"):"Loyal Summons — summoned entity is permanently charmed",
    ("Enchantment","Abjuration"):"Psychic Ward — ward fueled by the minds of those it protects",
    ("Space","Conjuration"):"Dimensional Gate — summon directly from across any plane",
    ("Space","Time"):"Chrono-Fold — teleport through both space and time at once",
    ("Space","Abjuration"):"Null Space — imprison a target in a sealed pocket dimension",
    ("Space","Necromancy"):"Death Step — step between life and death as spatial planes",
    ("Space","Divination"):"Spatial Scry — see any location you can spatially pinpoint",
    ("Space","Illusion"):"Mirror Realm — spatial pocket disguised as empty space",
    ("Divination","Time"):"True Sight of Ages — perceive past and future simultaneously",
    ("Divination","Necromancy"):"Speak with Dead — commune with souls in the afterlife",
    ("Divination","Illusion"):"False Oracle — your divination misleads other seers instead",
    ("Divination","Conjuration"):"Scry and Call — summon directly to your scrying focus",
    ("Divination","Abjuration"):"Arcane Lock — detect and permanently seal magical intrusions",
    ("Time","Necromancy"):"Decay Pulse — accelerate aging of targets to crumbling dust",
    ("Time","Abjuration"):"Temporal Ward — rewind damage suffered within the last round",
    ("Time","Illusion"):"Echo Image — project an image of yourself from the past",
    ("Time","Conjuration"):"Summon From Past — call a creature from a previous moment",
    ("Necromancy","Conjuration"):"Undead Summons — summon pre-raised undead from storage",
    ("Necromancy","Illusion"):"Death Mirage — illusion so vivid it can kill through belief",
    ("Necromancy","Abjuration"):"Corpse Ward — animate a corpse specifically as a shield",
    ("Conjuration","Illusion"):"Dream Manifest — conjure matter from within a living illusion",
    ("Conjuration","Abjuration"):"Ward Circle — banish summoned entities on contact with ward",
    ("Illusion","Abjuration"):"Mirror Ward — ward reflects images, confusing attackers",
}
def school_pair(a,b):
    if (a,b) in SCHOOL_CONNECTIONS: return (a,b)
    if (b,a) in SCHOOL_CONNECTIONS: return (b,a)
    return None

RING_GROUPS=["Range","Duration","Area","Power"]

DEFAULT_GLOBAL_MODS: Dict[str,dict] = {
    "Range: Touch":{"cat":"Range","cost":0,"max":1,"desc":"Requires physical contact."},
    "Range: Close 10ft":{"cat":"Range","cost":1,"max":1,"desc":"Short weapon-reach range."},
    "Range: Medium 60ft":{"cat":"Range","cost":2,"max":1,"desc":"Standard tactical combat range."},
    "Range: Long 300ft":{"cat":"Range","cost":3,"max":1,"desc":"Extreme battlefield range."},
    "Range: Sight":{"cat":"Range","cost":4,"max":1,"desc":"Anywhere you can directly see."},
    "Range: Planar":{"cat":"Range","cost":5,"max":1,"desc":"Can cross planar boundaries."},
    "Duration: Instant":{"cat":"Duration","cost":0,"max":1,"desc":"Single moment; no persistent magic."},
    "Duration: 1 Round":{"cat":"Duration","cost":1,"max":1,"desc":"Lasts until your next turn."},
    "Duration: Concentration":{"cat":"Duration","cost":1,"max":1,"desc":"Maintained as long as you concentrate."},
    "Duration: 1 Minute":{"cat":"Duration","cost":2,"max":1,"desc":"10 combat rounds."},
    "Duration: 10 Minutes":{"cat":"Duration","cost":3,"max":1,"desc":"Medium persistence."},
    "Duration: 1 Hour":{"cat":"Duration","cost":4,"max":1,"desc":"Long-lasting enhancement."},
    "Duration: 8 Hours":{"cat":"Duration","cost":5,"max":1,"desc":"Persists through a full rest."},
    "Duration: Permanent":{"cat":"Duration","cost":7,"max":1,"desc":"Persists until dispelled."},
    "Area: Single Target":{"cat":"Area","cost":0,"max":1,"desc":"Affects exactly one creature."},
    "Area: Cone 15ft":{"cat":"Area","cost":1,"max":1,"desc":"15-foot cone from caster."},
    "Area: Line 30ft":{"cat":"Area","cost":1,"max":1,"desc":"30-foot line, 5 feet wide."},
    "Area: Burst 10ft":{"cat":"Area","cost":2,"max":1,"desc":"10-foot radius sphere."},
    "Area: Burst 30ft":{"cat":"Area","cost":3,"max":1,"desc":"30-foot radius sphere."},
    "Area: Multi-Target":{"cat":"Area","cost":2,"max":5,"desc":"Choose up to 3 targets. Each purchase adds 2 more."},
    "Area: Aura Self":{"cat":"Area","cost":2,"max":3,"desc":"Radiates 10ft from caster. Each purchase adds 10ft."},
    "Area: Zone 60ft":{"cat":"Area","cost":4,"max":1,"desc":"60-foot radius sphere."},
    "Power: Weak d4":{"cat":"Power","cost":0,"max":1,"desc":"Minimal effect."},
    "Power: Moderate d6":{"cat":"Power","cost":2,"max":1,"desc":"Standard combat-grade power."},
    "Power: Strong d8":{"cat":"Power","cost":4,"max":1,"desc":"Above-average effect."},
    "Power: Powerful d10":{"cat":"Power","cost":6,"max":1,"desc":"Significant and threatening."},
    "Power: Mighty d12":{"cat":"Power","cost":8,"max":1,"desc":"Devastating single-hit power."},
    "Power: Epic 2d10":{"cat":"Power","cost":10,"max":1,"desc":"Legendary magnitude."},
    "Extra Damage Die":{"cat":"Power","cost":2,"max":10,"desc":"Add one additional damage die per purchase."},
    "Cast: Free Action":{"cat":"Casting","cost":3,"max":1,"desc":"No action economy cost."},
    "Cast: Bonus Action":{"cat":"Casting","cost":2,"max":1,"desc":"Uses your bonus action."},
    "Cast: Standard":{"cat":"Casting","cost":0,"max":1,"desc":"Uses your primary action."},
    "Cast: Full Round":{"cat":"Casting","cost":-1,"max":1,"desc":"Entire round to cast; DC +1."},
    "Cast: 1 Min Ritual":{"cat":"Casting","cost":-2,"max":1,"desc":"1-minute ritual."},
    "Cast: 10 Min Ritual":{"cat":"Casting","cost":-3,"max":1,"desc":"10-minute deep ritual. Range and area doubled."},
    "Silent Spell":{"cat":"Special","cost":1,"max":1,"desc":"Removes verbal component."},
    "Still Spell":{"cat":"Special","cost":1,"max":1,"desc":"Removes somatic component."},
    "Subtle Spell":{"cat":"Special","cost":2,"max":1,"desc":"Both verbal and somatic suppressed."},
    "Maximized":{"cat":"Special","cost":2,"max":1,"desc":"All variable effects use maximum value."},
    "Empowered":{"cat":"Special","cost":2,"max":2,"desc":"Reroll damage dice; take higher result."},
    "Twinned Spell":{"cat":"Special","cost":3,"max":1,"desc":"Target a second creature simultaneously."},
    "Heightened DC":{"cat":"Special","cost":2,"max":3,"desc":"Raise saving throw DC by 2 per purchase."},
    "Persistent Effect":{"cat":"Special","cost":2,"max":1,"desc":"Dispel checks against it have disadvantage."},
    "Selective Targeting":{"cat":"Special","cost":2,"max":1,"desc":"Exclude creatures from area of effect."},
    "Triggered Delay":{"cat":"Special","cost":2,"max":3,"desc":"Delay until trigger (up to 24hr per purchase)."},
    "Volley":{"cat":"Special","cost":2,"max":3,"desc":"Each purchase adds one additional volley strike."},
    "Extended Range":{"cat":"Special","cost":1,"max":5,"desc":"Double the spell's range per purchase."},
}
GLOBAL_MODS: Dict[str,dict] = deepcopy(DEFAULT_GLOBAL_MODS)

# ── Negative Modifiers (drawback system) ─────────────────────────
DEFAULT_NEGATIVE_MODS = [
    {"name":"Unstable Casting",   "desc":"15% chance to misfire — spell targets a random creature in range."},
    {"name":"Mana Burn",          "desc":"Caster takes 1d6 psychic damage each time this spell is cast."},
    {"name":"Verbal Tell",        "desc":"Spell cannot be cast silently; the activation word is always audible."},
    {"name":"Material Cost",      "desc":"Requires a 50gp material component that is consumed on casting."},
    {"name":"Concentration Lock", "desc":"While this spell is active, no other concentration spell can be maintained."},
    {"name":"Telegraphed",        "desc":"Targets receive one full round of warning before the spell activates."},
    {"name":"Backlash",           "desc":"Caster is stunned for 1 round after casting this spell."},
    {"name":"Soul Cost",          "desc":"Caster ages 1 year each time this spell is cast (magical aging)."},
    {"name":"Brittle Focus",      "desc":"Any damage to the caster automatically breaks concentration."},
    {"name":"Conspicuous Aura",   "desc":"Spell leaves a glowing magical residue visible for 1 hour after use."},
    {"name":"Exhausting",         "desc":"Caster gains 1 level of exhaustion immediately after casting."},
    {"name":"Wild Surge",         "desc":"Roll on the wild magic surge table in addition to normal effect."},
    {"name":"Fragile Ward",       "desc":"Any physical hit against the caster ends the spell instantly."},
    {"name":"Obvious Display",    "desc":"Casting requires a dramatic 6-second visible somatic performance."},
    {"name":"Cooldown",           "desc":"This spell cannot be cast again for 1d4 rounds after use."},
    {"name":"Mind Fog",           "desc":"Caster has disadvantage on all Intelligence checks until next rest."},
    {"name":"Tethered Power",     "desc":"Spell ends if caster moves more than 30 ft from the point of casting."},
    {"name":"Corrupting Touch",   "desc":"Caster suffers 1 point of permanent Constitution reduction."},
    {"name":"Screaming Glyph",    "desc":"Caster's voice becomes magically amplified for 1 hour after casting."},
    {"name":"Fate Debt",          "desc":"The caster's next saving throw is automatically failed, no roll."},
]
NEGATIVE_MODS = list(DEFAULT_NEGATIVE_MODS)   # runtime list; can be extended

LEVEL_TABLE=[
    (0,2,"Cantrip","#888888"),(3,6,"1st Level","#aaaaff"),(7,10,"2nd Level","#88aaff"),
    (11,14,"3rd Level","#66aaff"),(15,20,"4th Level","#44aaee"),(21,26,"5th Level","#22aadd"),
    (27,32,"6th Level","#00aacc"),(33,40,"7th Level","#00ccaa"),(41,50,"8th Level","#00cc88"),
    (51,60,"9th Level","#33cc66"),(61,72,"Legendary","#99cc22"),(73,84,"Mythic","#ccaa00"),
    (85,98,"Divine","#cc7700"),(99,112,"Cosmic","#cc4400"),(113,999,"Omnipotent","#ff2200"),
]
def pts_to_level(pts):
    for i,(lo,hi,name,col) in enumerate(LEVEL_TABLE):
        if pts<=hi: return i,name,col
    return len(LEVEL_TABLE)-1,LEVEL_TABLE[-1][2],LEVEL_TABLE[-1][3]

# ═══════════════════════════════════════════════════════════════
#  DATA MODEL
# ═══════════════════════════════════════════════════════════════
@dataclass
class Spell:
    name:str="Unnamed Spell"
    description:str=""
    school_abilities:Dict[str,Dict[str,int]]=field(default_factory=dict)
    global_mods:Dict[str,int]=field(default_factory=dict)
    ring_mods:Dict[str,Dict[str,int]]=field(default_factory=dict)
    custom_effects:List[str]=field(default_factory=list)
    circle_sizes:Dict[str,float]=field(default_factory=dict)
    # Elements: {"Fire":True, "Celestial":"Radiant", ...}
    elements:Dict[str,object]=field(default_factory=dict)
    # Element constellation nodes bought: {"Fire":{"Ignite":1}, ...}
    element_nodes:Dict[str,Dict[str,int]]=field(default_factory=dict)
    # Sub-element nodes bought: {("Fire","Water"):{"Steam Veil":1}, ...}
    subelement_nodes:Dict[str,Dict[str,int]]=field(default_factory=dict)
    # Drawback purchases: key="ability/school/name" or "mod/name" or "ringmod/school/grp"
    # value = name of negative modifier taken as drawback
    drawback_buys: Dict[str,str] = field(default_factory=dict)
    # Custom negative mods added by user (spell-level; also synced to NEGATIVE_MODS on load)
    custom_neg_mods: List[dict] = field(default_factory=list)
    ifthen_conditions: List[dict] = field(default_factory=list)
    # Each condition: {"name": str, "if_schools": List[str], "then_text": str}
    when_then_conditions: List[dict] = field(default_factory=list)
    # Each: {"when_text": str, "then_text": str}

    @property
    def all_schools(self):
        """Schools that have any spend (abilities or ring mods > 0). Order matches SCHOOLS dict."""
        result=[]
        for school in SCHOOLS:
            if (any(v>0 for v in self.school_abilities.get(school,{}).values()) or
                    any(v>0 for v in self.ring_mods.get(school,{}).values())):
                result.append(school)
        return result

    @property
    def normal_item_count(self):
        """Count of distinct purchased items (each ability/ring-group/mod/element counts as 1)."""
        n=0
        for abs_ in self.school_abilities.values():
            n+=sum(1 for c in abs_.values() if c>0)
        for cnt in self.global_mods.values():
            if cnt>0: n+=1
        for groups in self.ring_mods.values():
            for cnt in groups.values():
                if cnt>0: n+=1
        for val in self.elements.values():
            if val: n+=1
        for nodes in self.element_nodes.values():
            n+=sum(1 for c in nodes.values() if c>0)
        return n

    @property
    def is_complete(self):
        """Spell is valid when normally-purchased items outnumber drawback items."""
        db=len(self.drawback_buys)
        if db==0: return True
        return (self.normal_item_count-db)>db

    @property
    def total_points(self):
        pts=0
        for school,abs_ in self.school_abilities.items():
            ab_map=SCHOOLS.get(school,{}).get("abilities",{})
            for ab,cnt in abs_.items(): pts+=ab_map.get(ab,{}).get("cost",0)*cnt
        for mod,cnt in self.global_mods.items(): pts+=GLOBAL_MODS.get(mod,{}).get("cost",0)*cnt
        for school,groups in self.ring_mods.items():
            for grp,cnt in groups.items(): pts+=cnt
        # Elements cost 2 pts each (subtype adds 1 more)
        for el,val in self.elements.items():
            if val: pts+=2
            if isinstance(val,str): pts+=1
        # Element node costs
        for el,nodes in self.element_nodes.items():
            el_data=ELEMENTS.get(el,{})
            for nname,cnt in nodes.items():
                for nd in el_data.get("nodes",[]):
                    if nd["name"]==nname: pts+=nd["cost"]*cnt; break
        # Sub-element node costs
        for key_str,nodes in self.subelement_nodes.items():
            key=tuple(key_str.split(",",1)) if isinstance(key_str,str) else key_str
            pair=key if key in SUBELEMENT_NODES else (key[1],key[0]) if len(key)==2 and (key[1],key[0]) in SUBELEMENT_NODES else None
            if pair:
                for nname,cnt in nodes.items():
                    for nd in SUBELEMENT_NODES[pair]:
                        if nd["name"]==nname: pts+=nd["cost"]*cnt; break
        # Subtract costs of drawback-purchased items (they cost 0 pts)
        for key in self.drawback_buys:
            if key.startswith("ability/"):
                parts=key.split("/",2)
                if len(parts)==3:
                    sc,ab=parts[1],parts[2]
                    pts-=SCHOOLS.get(sc,{}).get("abilities",{}).get(ab,{}).get("cost",0)
            elif key.startswith("mod/"):
                mname=key[4:]
                pts-=GLOBAL_MODS.get(mname,{}).get("cost",0)
            elif key.startswith("ringmod/"):
                pts-=1  # each drawback ring-mod slot costs 1 normally
        return max(0,pts)

    @property
    def level_info(self): return pts_to_level(self.total_points)

    @property
    def active_connections(self):
        schools=self.all_schools; res=[]; seen=set()
        for i in range(len(schools)):
            for j in range(i+1,len(schools)):
                p=school_pair(schools[i],schools[j])
                if p and p not in seen:
                    cap=self.capstone_active(schools[i]) and self.capstone_active(schools[j])
                    res.append((schools[i],schools[j],SCHOOL_CONNECTIONS[p],cap))
                    seen.add(p)
        return res

    def capstone_active(self,school):
        rd=self.ring_mods.get(school,{})
        return all(rd.get(g,0)>=3 for g in RING_GROUPS)

    def to_dict(self): return asdict(self)
    @classmethod
    def from_dict(cls,d):
        valid={k:v for k,v in d.items() if k in cls.__dataclass_fields__}
        return cls(**valid)

# ═══════════════════════════════════════════════════════════════
#  MAGIC CIRCLE CANVAS
# ═══════════════════════════════════════════════════════════════
class MagicCircleCanvas(tk.Canvas):
    BG="#1a1208"  # deep parchment-brown background
    # Fixed pixel sizes (independent of zoom)
    OUTER_R     = 370   # overall circle radius in canvas pixels
    NODE_R_PRI  = 50    # school module base radius (all schools same size)
    NODE_R_SEC  = 38    # used for necklace ring outer bound calculation
    CENTER_R    = 96    # central modifier hub radius

    # Radii for modifier category circles (inside the center hub)
    _MOD_ORBIT = 62   # world units from hub center to mod-circle centre
    _MOD_CR    = 19   # radius of each modifier category circle

    def __init__(self,master,**kw):
        kw.setdefault("bg",self.BG); kw.setdefault("highlightthickness",0)
        super().__init__(master,**kw)
        self.spell:Optional[Spell]=None
        self._zoom=1.0
        self._ox=0.0; self._oy=0.0
        self._drag_start=None
        self._centered=False
        self._on_change=None          # callback → app._refresh()
        self._hit_zones=[]            # [(wx,wy,wr,callback,tooltip_text), ...]
        self._tooltip_win=None
        self._tooltip_after=None
        self._tooltip_text=""
        self._tooltip_cx=0; self._tooltip_cy=0
        self._rb3_start=None      # right-click start pos for drag-vs-click detection
        self._rb3_dragged=False
        self.bind("<Configure>",lambda _:self._redraw())
        self.bind("<MouseWheel>",self._on_wheel)
        self.bind("<Button-4>",self._on_wheel)
        self.bind("<Button-5>",self._on_wheel)
        self.bind("<ButtonPress-2>",self._pan_start)
        self.bind("<B2-Motion>",self._pan_move)
        self.bind("<ButtonPress-3>",self._rb3_press)
        self.bind("<B3-Motion>",self._rb3_drag)
        self.bind("<ButtonRelease-3>",self._rb3_release)
        self.bind("<Button-1>",self._on_click)
        self.bind("<Motion>",self._on_motion)
        self.bind("<Leave>",self._hide_tooltip)

    def load(self,spell): self.spell=spell; self._redraw()
    def zoom_in(self,x=None,y=None):  self._apply_zoom(1.15,x,y)
    def zoom_out(self,x=None,y=None): self._apply_zoom(1/1.15,x,y)
    def zoom_reset(self): self._zoom=1.0; self._ox=0.0; self._oy=0.0; self._centered=False; self._redraw()

    def _apply_zoom(self,factor,mx=None,my=None):
        W,H=self.winfo_width(),self.winfo_height()
        if mx is None: mx=W/2
        if my is None: my=H/2
        old=self._zoom
        self._zoom=max(0.20,min(5.0,self._zoom*factor))
        # Zoom centred on mouse: the world-point under the cursor stays fixed.
        scale=self._zoom/old
        self._ox=mx-(mx-self._ox)*scale
        self._oy=my-(my-self._oy)*scale
        self._redraw()

    def _on_wheel(self,e):
        if e.num==4 or e.delta>0: self.zoom_in(e.x,e.y)
        else: self.zoom_out(e.x,e.y)
    def _pan_start(self,e): self._drag_start=(e.x,e.y)
    def _pan_move(self,e):
        if self._drag_start:
            self._ox+=e.x-self._drag_start[0]; self._oy+=e.y-self._drag_start[1]
            self._drag_start=(e.x,e.y); self._redraw()

    def _rb3_press(self,e):
        self._rb3_start=(e.x,e.y); self._rb3_dragged=False; self._drag_start=(e.x,e.y)
    def _rb3_drag(self,e):
        self._rb3_dragged=True; self._pan_move(e)
    def _rb3_release(self,e):
        if not self._rb3_dragged:
            self._show_canvas_menu(e)
        self._drag_start=None; self._rb3_start=None; self._rb3_dragged=False

    def _show_canvas_menu(self,e):
        hz=self._nearest_hz(e.x,e.y)
        if not hz or len(hz)<3 or hz[2] is None: return
        info=hz[2]
        key=info.get("key",""); name=info.get("name",""); cost=info.get("cost",0)
        already_drawback=(key in self.spell.drawback_buys)
        menu=tk.Menu(self,tearoff=0,bg="#12121e",fg="#ccd8ff",
                     activebackground="#2233aa",activeforeground="#ffffff",bd=0)
        if already_drawback:
            menu.add_command(label=f"Remove drawback on '{name}'",
                             command=lambda:self._remove_drawback(key))
        else:
            if cost>0:
                menu.add_command(label=f"Take as Drawback — FREE  (choose penalty)",
                                 command=lambda:self._pick_drawback(key,name,cost,info))
            menu.add_command(label="Remove purchase",
                             command=lambda:self._canvas_remove(info))
        try: menu.tk_popup(e.x_root,e.y_root)
        finally: menu.grab_release()

    def _pick_drawback(self,key,name,cost,info):
        dlg=DrawbackPickerDialog(self,name,NEGATIVE_MODS,
            on_confirm=lambda neg_name: self._apply_drawback(key,name,neg_name,info))

    def _apply_drawback(self,key,name,neg_name,info):
        # Make sure the item is purchased (count=1 if not already)
        t=info.get("type","")
        if t=="ability":
            sc=info["school"]; ab=info["name"]
            self.spell.school_abilities.setdefault(sc,{}).setdefault(ab,0)
            if self.spell.school_abilities[sc].get(ab,0)<1:
                self.spell.school_abilities[sc][ab]=1
        elif t=="mod":
            mn=info["name"]
            if self.spell.global_mods.get(mn,0)<1:
                self.spell.global_mods[mn]=1
        elif t=="ringmod":
            sc=info["school"]; g=info["grp"]
            rm=self.spell.ring_mods.setdefault(sc,{})
            if rm.get(g,0)<1: rm[g]=1
        self.spell.drawback_buys[key]=neg_name
        if self._on_change: self._on_change()
        self._redraw()

    def _remove_drawback(self,key):
        self.spell.drawback_buys.pop(key,None)
        if self._on_change: self._on_change()
        self._redraw()

    def _canvas_remove(self,info):
        t=info.get("type","")
        if t=="ability":
            sc=info["school"]; ab=info["name"]
            self.spell.school_abilities.get(sc,{}).pop(ab,None)
            self.spell.drawback_buys.pop(info.get("key",""),None)
        elif t=="mod":
            mn=info["name"]; self.spell.global_mods.pop(mn,None)
            self.spell.drawback_buys.pop(info.get("key",""),None)
        elif t=="ringmod":
            sc=info["school"]; g=info["grp"]
            self.spell.ring_mods.setdefault(sc,{})[g]=0
            self.spell.drawback_buys.pop(info.get("key",""),None)
        if self._on_change: self._on_change()
        self._redraw()

    # ── Hit-zone / tooltip / click ────────────────────────────────
    def _wx_wy(self,ex,ey):
        return (ex-self._ox)/self._zoom, (ey-self._oy)/self._zoom

    def _nearest_hz(self,ex,ey):
        wx,wy=self._wx_wy(ex,ey)
        best=None; bd=float('inf')
        for hz in self._hit_zones:
            hx,hy,hr,cb,tip=hz[:5]
            info=hz[5] if len(hz)>5 else None
            d=math.hypot(wx-hx,wy-hy)
            if d<hr and d<bd: best=(cb,tip,info); bd=d
        return best

    def _on_click(self,e):
        self._hide_tooltip()
        hz=self._nearest_hz(e.x,e.y)
        if hz:
            hz[0]()  # call the action callback

    def _on_motion(self,e):
        if self._tooltip_after:
            self.after_cancel(self._tooltip_after); self._tooltip_after=None
        hz=self._nearest_hz(e.x,e.y)
        if hz:
            self._tooltip_text=hz[1]
            self._tooltip_cx=e.x; self._tooltip_cy=e.y
            self._tooltip_after=self.after(350,self._show_tooltip)
        else:
            self._hide_tooltip()

    def _show_tooltip(self):
        self._tooltip_after=None
        if self._tooltip_win:
            try: self._tooltip_win.destroy()
            except: pass
        tw=tk.Toplevel(self); tw.wm_overrideredirect(True)
        sx=self.winfo_rootx()+self._tooltip_cx+16
        sy=self.winfo_rooty()+self._tooltip_cy+10
        tw.wm_geometry(f"+{sx}+{sy}")
        tk.Label(tw,text=self._tooltip_text,justify="left",
                 bg="#12121e",fg="#ccd8ff",font=("TkFixedFont",8),
                 relief="solid",bd=1,padx=6,pady=4).pack()
        self._tooltip_win=tw

    def _hide_tooltip(self,event=None):
        if self._tooltip_after:
            self.after_cancel(self._tooltip_after); self._tooltip_after=None
        if self._tooltip_win:
            try: self._tooltip_win.destroy()
            except: pass
            self._tooltip_win=None

    # world → canvas coordinate
    def _tc(self,wx,wy):
        return self._ox+wx*self._zoom, self._oy+wy*self._zoom

    # ── drawing primitives (world coords) ────────────────────────
    def _b(self,c1,c2,t):
        def p(c): c=c.lstrip("#"); return int(c[:2],16),int(c[2:4],16),int(c[4:6],16)
        r1,g1,b1=p(c1); r2,g2,b2=p(c2)
        return "#{:02x}{:02x}{:02x}".format(int(r1+(r2-r1)*t),int(g1+(g2-g1)*t),int(b1+(b2-b1)*t))
    def _f(self,c,a): return self._b(self.BG,c,a)

    def _wpt(self,wx,wy,r,deg):
        """World-space point on circle."""
        a=math.radians(deg-90); return wx+r*math.cos(a), wy+r*math.sin(a)

    def _circle_w(self,wx,wy,r,**kw):
        cx,cy=self._tc(wx,wy); rs=r*self._zoom
        self.create_oval(cx-rs,cy-rs,cx+rs,cy+rs,**kw)

    def _ring_w(self,wx,wy,r,color,w=1,dash=None):
        cx,cy=self._tc(wx,wy); rs=r*self._zoom
        kw={"outline":color,"width":w,"fill":""}
        if dash: kw["dash"]=dash
        self.create_oval(cx-rs,cy-rs,cx+rs,cy+rs,**kw)

    def _line_w(self,wx1,wy1,wx2,wy2,**kw):
        x1,y1=self._tc(wx1,wy1); x2,y2=self._tc(wx2,wy2)
        self.create_line(x1,y1,x2,y2,**kw)

    def _poly_w(self,pts_world,**kw):
        pts=[c for wx,wy in pts_world for c in self._tc(wx,wy)]
        if len(pts)>=6: self.create_polygon(pts,**kw)

    def _text_w(self,wx,wy,**kw):
        """Draw text at world position. Font size is FIXED — does not scale with zoom."""
        cx,cy=self._tc(wx,wy)
        self.create_text(cx,cy,**kw)

    def _arc_w(self,wx,wy,r,start,extent,**kw):
        cx,cy=self._tc(wx,wy); rs=r*self._zoom
        self.create_arc(cx-rs,cy-rs,cx+rs,cy+rs,start=start,extent=extent,**kw)

    def _arc_ring_w(self,wx,wy,r,start_deg,extent_deg,**kw):
        cx,cy=self._tc(wx,wy); rs=r*self._zoom
        self.create_arc(cx-rs,cy-rs,cx+rs,cy+rs,
                        start=start_deg-90,extent=extent_deg,style=tk.ARC,**kw)

    def _wedge_w(self,wx,wy,r_in,r_out,d_start,d_end,color):
        """Filled wedge (annular sector) at world position, fixed pixel radius."""
        steps=max(4,abs(int(d_end-d_start)))
        pts_out=[]; pts_in=[]
        step=(d_end-d_start)/steps
        for i in range(steps+1):
            d=d_start+i*step
            pts_out.append(self._wpt(wx,wy,r_out,d))
            pts_in.append(self._wpt(wx,wy,r_in,d))
        pts_in.reverse()
        all_pts=pts_out+pts_in
        if len(all_pts)>=3:
            flat=[c for wx2,wy2 in all_pts for c in self._tc(wx2,wy2)]
            self.create_polygon(flat,fill=color,outline=color,width=0)

    def _star_w(self,wx,wy,R,r_in,n,off=0,**kw):
        pts=[]
        for i in range(n):
            pts.append(self._wpt(wx,wy,R,i*(360/n)+off))
            pts.append(self._wpt(wx,wy,r_in,i*(360/n)+(180/n)+off))
        self._poly_w(pts,**kw)

    def _poly_n_w(self,wx,wy,r,n,off=0,**kw):
        pts=[self._wpt(wx,wy,r,i*(360/n)+off) for i in range(n)]
        self._poly_w(pts,**kw)

    def _radials_w(self,wx,wy,r1,r2,n,off=0,color="#223",w=1):
        for i in range(n):
            a=i*(360/n)+off; x1,y1=self._wpt(wx,wy,r1,a); x2,y2=self._wpt(wx,wy,r2,a)
            self._line_w(x1,y1,x2,y2,fill=color,width=w)

    def _arc_text_w(self,wx,wy,r,text,start_deg,color,fontsize=6,step_deg=4.5):
        if not text: return
        total=len(text)*step_deg; angle=start_deg-total/2
        for ch in text:
            ax,ay=self._wpt(wx,wy,r,angle); cx,cy=self._tc(ax,ay)
            self.create_text(cx,cy,text=ch,fill=color,font=("Georgia",fontsize),angle=-(angle-90))
            angle+=step_deg

    # ── MAIN REDRAW ───────────────────────────────────────────────
    def _redraw(self):
        self._hit_zones=[]
        self._hide_tooltip()
        self.delete("all")
        W,H=self.winfo_width(),self.winfo_height()
        if W<10 or H<10:
            # Canvas not yet mapped - schedule retry
            self.after(50, self._redraw)
            return
        try:
            self._do_redraw(W,H)
        except Exception as _err:
            import traceback as _tb
            _tb.print_exc()
            self.create_text(W/2,H/2-20,text="Circle draw error:",fill="#ff6666",font=("TkFixedFont",10))
            self.create_text(W/2,H/2+5,text=str(_err)[:80],fill="#ff4444",font=("TkFixedFont",9))
        return

    def _do_redraw(self,W,H):

        # World origin = screen centre initially, then panned
        # We set _ox,_oy so that world (0,0) maps to canvas origin
        # On first draw, centre in canvas
        if not self._centered:
            self._ox=W/2; self._oy=H/2
            self._centered=True

        R=self.OUTER_R
        self._draw_deep_bg(R)
        pos=self._compute_node_positions(R)
        self._draw_outer_frame(R,pos)
        self._draw_main_geometry(R,pos)
        self._draw_element_band(R)
        self._draw_ifthen_markers(pos)
        self._draw_school_modules(pos)
        self._draw_center_hub()
        self._draw_drawback_rings()
        self._draw_when_then_ring()
        self._draw_status_bar(W,H)

    def _draw_deep_bg(self,R):
        """Rich parchment-paper fantasy background — aged, stained, crinkled."""
        W=self.winfo_width(); H=self.winfo_height()
        # Base parchment fill
        parch="#2c1f08"; dark="#1a1208"; aged="#3d2b10"; mid="#251808"
        self.create_rectangle(0,0,W,H,fill=parch,outline="")
        # Radial gradient — darker edge, warmer centre
        for i in range(12,0,-1):
            r=R*1.55*i/12
            t=1.0-i/12
            self._circle_w(0,0,r,fill=self._b(dark,mid,t*0.55),outline="")
        # Ink ring stains (parchment bleed)
        cx,cy=self._tc(0,0)
        for frac,w,alpha in [(1.50,4,0.45),(1.28,2,0.30),(1.08,3,0.22),(0.88,1,0.18)]:
            rs=R*frac*self._zoom
            ic=self._b("#0a0600","#1a0e04",alpha)
            self.create_oval(cx-rs,cy-rs,cx+rs,cy+rs,fill="",outline=ic,
                             width=max(1,int(w*self._zoom)))
        # Aged crease lines across the parchment
        crease="#120c04"; crease2="#1e1408"
        self.create_line(W*0.02,H*0.12,W*0.18,H*0.02,fill=crease,width=1)
        self.create_line(W*0.82,H*0.98,W*0.98,H*0.82,fill=crease,width=1)
        self.create_line(W*0.05,H*0.90,W*0.25,H*0.98,fill=crease2,width=1)
        self.create_line(W*0.75,H*0.02,W*0.95,H*0.10,fill=crease2,width=1)
        # Subtle foxing spots (age spots on parchment)
        import random; rng=random.Random(42)
        for _ in range(18):
            fx=rng.randint(int(W*0.05),int(W*0.95))
            fy=rng.randint(int(H*0.05),int(H*0.95))
            fr=rng.randint(2,8)
            fc=self._b("#0a0600","#2a1a08",rng.uniform(0.3,0.7))
            self.create_oval(fx-fr,fy-fr,fx+fr,fy+fr,fill=fc,outline="")
        # Warm inner brightening under circle
        for i in range(6,0,-1):
            r2=R*0.92*i/6
            self._circle_w(0,0,r2,fill=self._b(dark,aged,0.07*(7-i)),outline="")
        # Faint sacred geometry ghost on parchment surface
        ghost=self._f("#4a3015",0.08)
        self._ring_w(0,0,R*0.88,ghost,w=1)
        self._ring_w(0,0,R*0.65,ghost,w=1)
        self._ring_w(0,0,R*0.40,ghost,w=1)

    def _crescent_pts(self, a_deg, span_out=26, span_in=13, n=32):
        """
        Crescent moon polygon curving ALONG the outer ring at the school's angular position.
        Outer arc lies just outside the main ring; inner arc is concave, making tapered tips.
        span_out > span_in produces the crescent's pointed horns.
        """
        import math
        R_out = self.OUTER_R + 14   # just beyond the outer ring
        R_in  = self.OUTER_R - 20   # inside the outer ring
        a  = math.radians(a_deg)
        ho = math.radians(span_out)
        hi = math.radians(span_in)
        pts = []
        # Outer arc CCW from a-ho to a+ho (wider span)
        for i in range(n + 1):
            ang = a - ho + i * 2 * ho / n
            pts.append((R_out * math.cos(ang), R_out * math.sin(ang)))
        # Inner arc CW from a+hi to a-hi (narrower span → tapers to crescent tips)
        for i in range(n + 1):
            ang = a + hi - i * 2 * hi / n
            pts.append((R_in * math.cos(ang), R_in * math.sin(ang)))
        return pts

    def _draw_ifthen_markers(self, pos):
        """Draw ring-curving crescent markers for If-Then conditions (behind school circles)."""
        import math
        s = self.spell
        if not s: return
        conds = getattr(s, 'ifthen_conditions', [])
        if not conds: return

        # Recursively collect all IF schools from conditions and their chains
        def _collect(cond_list, depth=0):
            result = {}
            for cond in cond_list:
                for sc in cond.get('if_schools', []):
                    if sc in pos:
                        result.setdefault(sc, []).append((cond.get('name','?'), depth))
                for sub in cond.get('chain', []):
                    for sc, entries in _collect([sub], depth+1).items():
                        result.setdefault(sc, []).extend(entries)
            return result

        marked = _collect(conds)
        school_list = list(SCHOOLS.keys())
        n_sc = len(school_list)
        for idx, school in enumerate(school_list):
            if school not in marked: continue
            a_deg = idx * (360 / n_sc)
            c = SCHOOLS[school]['color']
            entries = marked[school]
            pts = self._crescent_pts(a_deg)
            self._poly_w(pts, fill=self._f(c, 0.22), outline='', smooth=True)
            self._poly_w(pts, fill='', outline=self._b(c, '#ffffff', 0.65),
                         width=2, smooth=True)
            # Label at outer tip
            ca = math.cos(math.radians(a_deg)); sa = math.sin(math.radians(a_deg))
            R_lbl = self.OUTER_R + 14
            self._text_w(R_lbl * ca, R_lbl * sa, text=f"✦{len(entries)}",
                         fill=self._b(c, '#ffffff', 0.85),
                         font=('TkDefaultFont', 7, 'bold'))

    def _draw_outer_frame(self,R,pos):
        s=self.spell; _,lvl,col=s.level_info
        active_sc=set(s.all_schools)
        ink=self._b("#e8d8a0","#ffffff",0.55); ink2=self._b("#c8a060","#aa8844",0.45)
        # Double outer border
        self._ring_w(0,0,R,ink,w=3)
        self._ring_w(0,0,R*0.978,ink2,w=1)
        # Tick marks
        for i in range(72):
            a=i*5; ww=3 if i%6==0 else 1
            r1=R*(0.956 if i%6==0 else 0.966)
            x1,y1=self._wpt(0,0,r1,a); x2,y2=self._wpt(0,0,R*0.978,a)
            self._line_w(x1,y1,x2,y2,fill=ink2,width=ww)
        # Connector ring (necklace track behind school circles)
        school_r=R*0.82
        self._ring_w(0,0,school_r,ink2,w=2)
        self._ring_w(0,0,school_r+self.NODE_R_SEC*1.40,ink2,w=1)
        # Chord web connecting all 10 school nodes
        school_list=list(SCHOOLS.keys()); n=len(school_list)
        spts=[self._wpt(0,0,school_r,i*(360/n)) for i in range(n)]
        for i in range(n):
            for j in range(i+2,n-1):
                x1,y1=spts[i]; x2,y2=spts[j]
                self._line_w(x1,y1,x2,y2,fill=self._f(ink2,0.07),width=1)
        # School symbol accent between outer ring and school circle
        for i,sch in enumerate(school_list):
            a=i*(360/n); active=(sch in active_sc)
            c2=SCHOOLS[sch]["color"]
            dx,dy=self._wpt(0,0,R*0.950,a)
            self._text_w(dx,dy,text="◆",fill=c2 if active else self._f(c2,0.30),
                         font=("TkDefaultFont",5))
        # Spell name arc
        if s.name:
            txt=" ✦ "+s.name.upper()+" ✦ "+lvl.upper()+" ✦ "
            self._arc_text_w(0,0,R*0.990,txt,0,col,fontsize=6,step_deg=3.8)
        # Synergy connections between selected schools
        self._draw_connection_lines(pos)

    def _compute_node_positions(self,R):
        """All 10 schools always at fixed evenly-spaced positions on outer ring."""
        pos={}
        school_list=list(SCHOOLS.keys())   # fixed order, always all 10
        n=len(school_list); node_r=R*0.82
        for i,sch in enumerate(school_list):
            a=i*(360/n); pos[sch]=self._wpt(0,0,node_r,a)
        return pos

    def _draw_main_geometry(self,R,pos):
        """Inner sacred geometry web confined within element band (R*0.50)."""
        s=self.spell; _asc=s.all_schools
        c=SCHOOLS[_asc[0]]["color"] if _asc else "#8899bb"
        web_r=R*0.48; n=10
        self._ring_w(0,0,R*0.50,self._f(c,0.18),w=1)
        pts=[self._wpt(0,0,web_r,i*(360/n)) for i in range(n)]
        for i in range(n):
            for j in range(i+2,n):
                p1=pts[i]; p2=pts[j]
                self._line_w(p1[0],p1[1],p2[0],p2[1],fill=self._f(c,0.08),width=1)
        self._poly_n_w(0,0,web_r,5,fill=self._f(c,0.04),outline=self._f(c,0.22),width=1)
        self._poly_n_w(0,0,web_r,5,off=36,fill="",outline=self._f(c,0.14),width=1,dash=(4,4))
        for frac,alpha in [(0.48,0.18),(0.38,0.14),(0.28,0.12),(0.18,0.10)]:
            self._ring_w(0,0,R*frac,self._f(c,alpha),w=1)
        self._poly_n_w(0,0,R*0.42,3,fill="",outline=self._f(c,0.20),width=1)
        self._poly_n_w(0,0,R*0.42,3,off=60,fill="",outline=self._f(c,0.16),width=1,dash=(4,3))
        self._poly_n_w(0,0,R*0.45,7,fill="",outline=self._f(c,0.08),width=1,dash=(2,5))
        for p in pts:
            self._circle_w(p[0],p[1],2,fill=self._f(c,0.35),outline="")

    def _draw_connection_lines(self,pos):
        s=self.spell
        for s1,s2,_,cap in s.active_connections:
            if s1 not in pos or s2 not in pos: continue
            x1,y1=pos[s1]; x2,y2=pos[s2]
            mid=self._b(SCHOOLS[s1]["color"],SCHOOLS[s2]["color"],0.5)
            if cap:
                self._line_w(x1,y1,x2,y2,fill=self._f("#FFD700",0.55),width=2,dash=(6,3))
            else:
                self._line_w(x1,y1,x2,y2,fill=self._f(mid,0.30),width=2)
            mx,my=(x1+x2)/2,(y1+y2)/2
            seed=sum(ord(c) for c in s1+s2)
            self._text_w(mx,my,text=RUNES_ALL[seed%len(RUNES_ALL)],
                         fill=self._f("#FFD700" if cap else mid,0.70),font=("TkFixedFont",6))

    def _draw_school_modules(self,pos):
        for school,(wx,wy) in pos.items():
            self._draw_module(wx,wy,school)

    def _draw_module(self,wx,wy,school):
        """School circle: outer ring = ability runes, inner ring = ring mod runes.
        All 10 schools always visible. All abilities/mods shown (dim=unselected, bright=selected).
        Hit zones registered for click-to-toggle and hover tooltip."""
        s=self.spell; c=SCHOOLS[school]["color"]
        active=(school in s.all_schools)
        r=self.NODE_R_PRI*s.circle_sizes.get(school,1.0)
        cap=s.capstone_active(school)
        bright=1.0 if active else 0.20
        ink=self._b("#e8d8a0","#ffffff",0.55)
        ab_data=SCHOOLS[school].get("abilities",{})

        # Capstone gold outer pulse
        if cap and active:
            self._ring_w(wx,wy,r*1.38,self._f("#FFD700",0.40),w=1,dash=(4,3))
            self._ring_w(wx,wy,r*1.30,"#FFD700",w=2)

        # Double outer border
        oc=self._b(ink,c,0.35) if active else self._f(c,0.22)
        self._ring_w(wx,wy,r,oc,w=2 if active else 1)
        self._ring_w(wx,wy,r*0.92,self._f(c,0.30*bright),w=1)

        # ── OUTER RUNE RING — school abilities (ALL always visible) ──
        ab_names=list(ab_data.keys())
        ab_dict=s.school_abilities.get(school,{})
        n_ab=len(ab_names); ab_r=r*0.84; hz_r=max(7,r*0.18)
        for i,abn in enumerate(ab_names[:12]):
            a=i*(360/max(12,n_ab))
            rx,ry=self._wpt(wx,wy,ab_r,a)
            cnt=ab_dict.get(abn,0)
            seed=sum(ord(ch) for ch in abn)
            rune=RUNES_ALL[seed%len(RUNES_ALL)]
            bought=(cnt>0)
            if bought:
                self._circle_w(rx,ry,3,fill=self._f(c,0.65),outline="")
                self._text_w(rx,ry,text=rune,fill=self._b(c,"#ffffff",0.82),
                             font=("TkFixedFont",max(6,int(r*0.20))))
            else:
                # Dim but visible
                self._circle_w(rx,ry,2,fill=self._f(c,0.28*bright),outline="")
                self._text_w(rx,ry,text=rune,fill=self._f(c,0.50*bright),
                             font=("TkFixedFont",max(5,int(r*0.16))))
            # Hit zone — click cycles purchase count
            ab_info=ab_data.get(abn,{})
            tip=f"{abn}\nCost: {ab_info.get('cost',1)} pt/purchase\n{ab_info.get('desc','')}"
            def _ab_cb(sc=school,ab=abn):
                self.spell.school_abilities.setdefault(sc,{})[ab]=(
                    self.spell.school_abilities.get(sc,{}).get(ab,0)+1)%4
                if self._on_change: self._on_change()
                self._redraw()
            ab_info_d={"type":"ability","school":school,"name":abn,"cost":ab_info.get("cost",1),"key":f"ability/{school}/{abn}"}
            is_drawback=f"ability/{school}/{abn}" in (self.spell.drawback_buys or {})
            dot_col=self._f("#ffffff",0.45) if is_drawback else self._f(c,0.65)
            if is_drawback:
                self._ring_w(rx,ry,5,self._f("#ffffff",0.55),w=1)
            self._hit_zones.append((rx,ry,hz_r,_ab_cb,tip,ab_info_d))

        # ── INNER RUNE RING — ring modifiers (ALL slots always visible) ──
        grp_c={"Range":"#ff8080","Duration":"#80ee88","Area":"#8088ff","Power":"#ffe080"}
        grp_names=["Range","Duration","Area","Power"]
        ring_data=s.ring_mods.get(school,{})
        mod_r=r*0.64; hz_r2=max(6,r*0.14)
        school_rm_labels=SCHOOLS[school].get("ring_mods",{})
        for gi,grp in enumerate(grp_names):
            fill_cnt=ring_data.get(grp,0)
            gc=grp_c[grp]
            for slot in range(3):
                a=(gi*3+slot)*(360/12)
                rx2,ry2=self._wpt(wx,wy,mod_r,a)
                filled=(slot<fill_cnt)
                rune_list=MOD_RUNES.get(grp,["?"])
                rune=rune_list[min(slot,len(rune_list)-1)]
                if filled:
                    self._circle_w(rx2,ry2,3,fill=self._f(gc,0.55),outline="")
                    self._text_w(rx2,ry2,text=rune,fill=self._b(gc,"#ffffff",0.75),
                                 font=("TkFixedFont",max(5,int(r*0.20))))
                else:
                    self._circle_w(rx2,ry2,2,fill=self._f(gc,0.22*bright),outline="")
                    self._text_w(rx2,ry2,text=rune,fill=self._f(gc,0.42*bright),
                                 font=("TkFixedFont",max(5,int(r*0.15))))
                lbl=school_rm_labels.get(grp,grp)
                tip2=f"{school} — {grp} ({lbl})\nSlot {slot+1}/3  (click to advance)"
                def _rm_cb(sc=school,g=grp,sl=slot):
                    rm=self.spell.ring_mods.setdefault(sc,{})
                    cur=rm.get(g,0)
                    # Click slot: fill up to this slot+1, or unfill if already at this slot
                    rm[g]=(sl+1) if cur<=sl else sl
                    if self._on_change: self._on_change()
                    self._redraw()
                rm_key=f"ringmod/{school}/{grp}"
                rm_info_d={"type":"ringmod","school":school,"grp":grp,"slot":slot,"name":f"{school} {grp} mod","cost":1,"key":rm_key}
                is_drawback_rm=(rm_key in (self.spell.drawback_buys or {}))
                if is_drawback_rm and slot==0:
                    self._ring_w(rx2,ry2,5,self._f("#ffffff",0.55),w=1)
                self._hit_zones.append((rx2,ry2,hz_r2,_rm_cb,tip2,rm_info_d))

        # Concentric inner rings
        self._ring_w(wx,wy,r*0.52,self._f(c,0.30*bright),w=1)
        self._ring_w(wx,wy,r*0.35,self._f(c,0.22*bright),w=1)

        # Radial spokes (8)
        for i in range(8):
            a=i*45
            x1,y1=self._wpt(wx,wy,r*0.08,a); x2,y2=self._wpt(wx,wy,r*0.88,a)
            self._line_w(x1,y1,x2,y2,fill=self._f(c,0.18*bright),width=1)

        # Central polygon
        self._poly_n_w(wx,wy,r*0.32,5,fill=self._f(c,0.08*bright),
                       outline=self._f(c,0.40*bright),width=1)

        # School symbol
        sym=SCHOOLS[school]["symbol"]; fsize=max(8,int(r*0.48))
        self._text_w(wx,wy,text=sym,
                     fill=self._b(c,"#ffffff",0.55) if active else self._f(c,0.30),
                     font=("TkDefaultFont",fsize))

        # Capstone sigil (active only)
        if cap and active:
            cd=CAPSTONES.get(school,{}); cap_c=cd.get("color","#FFD700")
            glyph=cd.get("glyph","⚜"); ring_str=cd.get("ring","✦✦✦✦✦✦✦✦")
            self._circle_w(wx,wy,r*0.26,fill=self._f(cap_c,0.28),outline="")
            self._text_w(wx,wy,text=glyph,fill=cap_c,
                         font=("TkDefaultFont",max(14,int(r*0.58))))
            for k,ch in enumerate(ring_str):
                aa=k*(360/len(ring_str)); rx3,ry3=self._wpt(wx,wy,r*0.42,aa)
                self._text_w(rx3,ry3,text=ch,fill=self._f(cap_c,0.85),
                             font=("TkDefaultFont",max(6,int(r*0.15))))
            self._text_w(wx,wy+r*1.52,text=f"⚜ {cd.get('name','')} ⚜",
                         fill=cap_c,font=("Georgia",5,"bold italic"))

        # School name label
        lc=self._b(c,"#e8d8a0",0.50) if active else self._f(c,0.22)
        self._text_w(wx,wy+r+10,text=school,fill=lc,font=("Georgia",6,"italic"))

    def _draw_12ring(self,wx,wy,r_in,r_out,school,c,spell):
        """12-section ring showing ring mod fills and rune labels."""
        grp_c={"Range":"#ff6060","Duration":"#60ee88","Area":"#6088ff","Power":"#ffd060"}
        grp_names=["Range","Duration","Area","Power"]
        ring_data=spell.ring_mods.get(school,{})
        labels=SCHOOLS[school].get("ring_mods",{})
        ri=r_in*1.04; ro=r_out

        for sect in range(12):
            grp=grp_names[sect//3]; gc=grp_c[grp]
            fill_count=ring_data.get(grp,0)
            filled=(sect%3)<fill_count
            d_start=sect*30; d_end=d_start+28
            fill_col=self._f(gc,0.60) if filled else self._f(gc,0.06)
            self._wedge_w(wx,wy,ri,ro,d_start,d_end,fill_col)
            # Border arc
            self._arc_ring_w(wx,wy,ro,d_start,28,outline=self._f(gc,0.5),width=1)
            # Major divider
            if sect%3==0:
                x1,y1=self._wpt(wx,wy,ri,d_start); x2,y2=self._wpt(wx,wy,ro,d_start)
                self._line_w(x1,y1,x2,y2,fill=gc,width=2)
            # Label in mid-section: use rune if filled, abbrev if not
            if sect%3==1:
                lbl=labels.get(grp,grp); mid_d=d_start+14
                lx,ly=self._wpt(wx,wy,(ri+ro)/2,mid_d)
                if filled:
                    # Show rune for this ring mod group
                    rune_list=MOD_RUNES.get(grp,["?"]); rune_idx=fill_count-1
                    rune=rune_list[min(rune_idx,len(rune_list)-1)]
                    self._text_w(lx,ly,text=rune,fill=self._f(gc,0.95),font=("TkFixedFont",7),angle=-(mid_d-90))
                else:
                    abbr=lbl[:2].upper()
                    self._text_w(lx,ly,text=abbr,fill=self._f(gc,0.65),font=("TkFixedFont",5),angle=-(mid_d-90))
        # Border rings
        self._ring_w(wx,wy,ro,self._f(c,0.55),w=1)
        self._ring_w(wx,wy,ri,self._f(c,0.35),w=1)


    def _draw_elem_node(self,wx,wy,symbol,color,r,bought=False,label="",rune=""):
        """Draw a single constellation node (element or sub-element upgrade)."""
        fc=self._f(color,0.55 if bought else 0.18)
        oc=color if bought else self._f(color,0.45)
        self._circle_w(wx,wy,r,fill=fc,outline="")
        self._ring_w(wx,wy,r,oc,w=2 if bought else 1)
        if bought:
            self._ring_w(wx,wy,r*1.35,self._f(color,0.25),w=1)
        self._text_w(wx,wy,text=symbol,fill=color if bought else self._f(color,0.55),
                     font=("TkDefaultFont",max(6,int(r*0.9))))
        if label:
            lx,ly=self._wpt(wx,wy,r+8,180)
            self._text_w(lx,ly,text=label,fill=self._f(color,0.70),font=("TkFixedFont",5))
        if rune:
            rx,ry=self._wpt(wx,wy,r+8,0)
            self._text_w(rx,ry,text=rune,fill=self._f(color,0.60),font=("TkFixedFont",6))

    def _draw_element_band(self,R):
        """Draw active elements as constellations: centre node + satellite upgrade nodes.
        Sub-elements appear between their two parent nodes when both are active."""
        s=self.spell
        active_elems=[(el,val) for el,val in s.elements.items() if val]
        if not active_elems: return

        # Centre nodes sit on an orbital ring; satellite nodes fan outward
        orbit_r=R*0.55
        centre_nr=14          # centre node radius — middle band
        sat_nr=8              # satellite node radius
        sat_offset=32         # distance from centre to satellite

        n=len(active_elems)
        elem_pos={}  # el -> world (wx,wy) of centre node

        # ── Sub-element connection lines (draw first, behind everything) ────
        for i,(el1,_) in enumerate(active_elems):
            for j,(el2,_) in enumerate(active_elems):
                if i>=j: continue
                pair=((el1,el2) if (el1,el2) in ELEMENT_CONNECTIONS
                      else ((el2,el1) if (el2,el1) in ELEMENT_CONNECTIONS else None))
                if not pair: continue
                a1=i*(360/n); a2=j*(360/n)
                x1,y1=self._wpt(0,0,orbit_r,a1)
                x2,y2=self._wpt(0,0,orbit_r,a2)
                c1=ELEMENTS[el1]["color"]; c2=ELEMENTS[el2]["color"]
                mc=self._b(c1,c2,0.5)
                self._line_w(x1,y1,x2,y2,fill=self._f(mc,0.28),width=1,dash=(5,4))

        # ── Element centre nodes and their constellations ────────────────────
        for i,(el,val) in enumerate(active_elems):
            a=i*(360/n)
            cx,cy=self._wpt(0,0,orbit_r,a)
            elem_pos[el]=(cx,cy)
            edata=ELEMENTS[el]

            # Colour: subtype override for Celestial
            if el=="Celestial" and isinstance(val,str) and val in edata.get("subtypes",{}):
                ec=edata["subtypes"][val]["color"]
                sub_rune=edata["subtypes"][val]["rune"]
                elabel=f"{el}:{val}"
            else:
                ec=edata["color"]; elabel=el; sub_rune=edata.get("rune","")

            # Spoke line from hub to centre node
            hx,hy=self._wpt(0,0,self.CENTER_R+4,a)
            self._line_w(hx,hy,cx,cy,fill=self._f(ec,0.25),width=1,dash=(4,4))

            # Centre node
            self._draw_elem_node(cx,cy,edata["symbol"],ec,centre_nr,
                                 label=elabel,rune=sub_rune)

            # Constellation satellite nodes (upgrades)
            nodes=edata.get("nodes",[])
            bought_nodes=s.element_nodes.get(el,{})
            nsat=len(nodes)
            # Fan the satellites outward from the spoke direction
            # Spread ±60° around outward direction (away from centre hub)
            fan_angles=[a+d for d in ([-60,-30,0,30,60] if nsat>=5 else
                                       [-45,0,45] if nsat>=3 else [0])]
            for k,nd in enumerate(nodes):
                if k>=len(fan_angles): break
                ang=fan_angles[k]
                sx,sy=self._wpt(cx,cy,sat_offset,ang)
                bought=bought_nodes.get(nd["name"],0)>0
                # Line from centre node to satellite
                self._line_w(cx,cy,sx,sy,fill=self._f(ec,0.30 if bought else 0.15),
                             width=1,dash=None if bought else (3,4))
                self._draw_elem_node(sx,sy,nd["glyph"],ec,sat_nr,
                                     bought=bought,
                                     label=nd["name"][:8],
                                     rune=nd["rune"])
                # If bought, show cost label
                if bought:
                    self._text_w(sx,sy+sat_nr+8,text=nd["effect"][:14],
                                 fill=self._f(ec,0.70),font=("TkFixedFont",5))

        # ── Sub-element mini-constellations ──────────────────────────────────
        for i,(el1,_) in enumerate(active_elems):
            for j,(el2,_) in enumerate(active_elems):
                if i>=j: continue
                pair=((el1,el2) if (el1,el2) in SUBELEMENT_NODES
                      else ((el2,el1) if (el2,el1) in SUBELEMENT_NODES else None))
                if not pair: continue
                if pair not in SUBELEMENT_NODES: continue
                nodes=SUBELEMENT_NODES[pair]
                # Sub-constellation centre = midpoint between parent nodes
                p1x,p1y=elem_pos.get(el1,(0,0))
                p2x,p2y=elem_pos.get(el2,(0,0))
                scx=(p1x+p2x)/2; scy=(p1y+p2y)/2
                # Shift slightly outward from origin so it doesn't sit dead centre
                dist=max(1,math.hypot(scx,scy))
                scx+=scx/dist*10; scy+=scy/dist*10

                c1=ELEMENTS[el1]["color"]; c2=ELEMENTS[el2]["color"]
                mc=self._b(c1,c2,0.5)

                # Sub-element label diamond
                self._poly_w([(scx,scy-5),(scx+5,scy),(scx,scy+5),(scx-5,scy)],
                             fill=self._f(mc,0.45),outline="")
                effect_name=ELEMENT_CONNECTIONS.get(pair,ELEMENT_CONNECTIONS.get((pair[1],pair[0]),""))
                self._text_w(scx,scy,text=RUNES_ALL[hash(pair[0]+pair[1])%len(RUNES_ALL)],
                             fill=mc,font=("TkFixedFont",7))

                # Three satellite upgrade nodes fanning from sub-centre
                key_str=f"{pair[0]},{pair[1]}"
                bought_sub=s.subelement_nodes.get(key_str,
                           s.subelement_nodes.get(f"{pair[1]},{pair[0]}",{}))
                # Direction: perpendicular to line between parents
                dx,dy=p2x-p1x,p2y-p1y
                perp_a=math.degrees(math.atan2(dy,dx))+90
                for k,nd in enumerate(nodes):
                    d_off=perp_a + (k-1)*40
                    nx,ny=self._wpt(scx,scy,28,d_off)
                    bought=bought_sub.get(nd["name"],0)>0
                    self._line_w(scx,scy,nx,ny,
                                 fill=self._f(mc,0.35 if bought else 0.15),width=1,
                                 dash=None if bought else (3,4))
                    self._draw_elem_node(nx,ny,nd["glyph"],mc,7,
                                         bought=bought,
                                         label=nd["name"][:8])

    # ── Level symbol helpers (world-space, drawn in hub centre) ──────────────
    def _draw_fist_w(self,wx,wy,r,color):
        """Golden fist: palm + 4 knuckle bumps."""
        p0=r*0.18
        pts=[(wx-r*0.55,wy+p0),(wx+r*0.55,wy+p0),(wx+r*0.55,wy+r*0.65),(wx-r*0.55,wy+r*0.65)]
        self._poly_w(pts,fill=self._f(color,0.75),outline=color,width=1)
        for i in range(4):
            kx=wx+(-r*0.42+i*r*0.28); ky=wy-r*0.02
            self._circle_w(kx,ky,r*0.20,fill=self._f(color,0.80),outline=color)

    def _draw_crown_w(self,wx,wy,r,color):
        """Golden crown: base band + 5 pointed peaks."""
        by=r*0.12
        base=[(wx-r*0.72,wy+by),(wx+r*0.72,wy+by),(wx+r*0.72,wy+r*0.55),(wx-r*0.72,wy+r*0.55)]
        self._poly_w(base,fill=self._f(color,0.70),outline=color,width=1)
        for px in [-0.68,-0.34,0,0.34,0.68]:
            tip=[(wx+px*r-r*0.16,wy+by),(wx+px*r,wy-r*0.52),(wx+px*r+r*0.16,wy+by)]
            self._poly_w(tip,fill=self._f(color,0.90),outline=color,width=1)

    def _draw_wings_w(self,wx,wy,r,color):
        """White wings: two sweeping arcs from centre."""
        for side in [-1,1]:
            pts=[(wx,wy)]
            for i in range(10):
                t=i/9; ang=180+side*(20+t*110); rad=r*(0.25+t*0.80)
                pts.append(self._wpt(wx,wy,rad,ang))
            pts.append((wx,wy))
            self._poly_w(pts,fill=self._f(color,0.25),outline=color,width=1)

    def _draw_sun_w(self,wx,wy,r,color):
        """White sun: central disc + 12 radiating rays."""
        self._circle_w(wx,wy,r*0.32,fill=self._f(color,0.70),outline=color)
        for i in range(12):
            a=i*30; x1,y1=self._wpt(wx,wy,r*0.42,a); x2,y2=self._wpt(wx,wy,r*0.88,a)
            self._line_w(x1,y1,x2,y2,fill=color,width=2)

    def _draw_stars_w(self,wx,wy,r,color):
        """Circle of 7 white stars."""
        for i in range(7):
            a=i*(360/7); sx,sy=self._wpt(wx,wy,r*0.62,a)
            self._star_w(sx,sy,r*0.25,r*0.12,5,fill=self._f(color,0.85),outline=color,width=1)

    def _draw_mod_circles(self):
        """6 modifier-category mini-circles placed inside the center hub.
        Aligned with the hub's 6 category arc sections.
        All mods shown as rune nodes — dim=inactive, bright=active.
        Click to toggle; hover for tooltip."""
        orbit=self._MOD_ORBIT; cr=self._MOD_CR
        cats=list(CAT_COLORS.keys()); n=len(cats)
        seg=360/n; s=self.spell
        for i,cat in enumerate(cats):
            # Centre angle aligns with mid-point of the category arc section
            angle=i*seg + seg/2
            cx,cy=self._wpt(0,0,orbit,angle)
            gc=CAT_COLORS[cat]
            cat_mods=[(mn,md) for mn,md in GLOBAL_MODS.items() if md["cat"]==cat]
            has_active=any(s.global_mods.get(mn,0)>0 for mn,_ in cat_mods)

            # Circle body
            self._circle_w(cx,cy,cr,fill=self._f(gc,0.12 if has_active else 0.05),outline="")
            self._ring_w(cx,cy,cr,self._f(gc,0.60 if has_active else 0.30),w=2 if has_active else 1)
            self._ring_w(cx,cy,cr*0.68,self._f(gc,0.18),w=1)
            # 4 radial spokes inside circle
            for k in range(4):
                a2=angle+k*90
                p1=self._wpt(cx,cy,cr*0.12,a2); p2=self._wpt(cx,cy,cr*0.62,a2)
                self._line_w(p1[0],p1[1],p2[0],p2[1],fill=self._f(gc,0.18),width=1)

            # Category rune at centre
            base_rune=MOD_RUNES[cat][0]
            self._text_w(cx,cy,text=base_rune,
                         fill=self._b(gc,"#ffffff",0.75) if has_active else self._f(gc,0.45),
                         font=("TkFixedFont",max(6,int(cr*0.50))))

            # Mod rune nodes on outer ring
            nm=len(cat_mods)
            if nm==0: continue
            node_r=cr*0.82; hz_r=max(5,cr*0.30)
            for j,(mn,md) in enumerate(cat_mods):
                node_a=j*(360/nm)
                rx,ry=self._wpt(cx,cy,node_r,node_a)
                cnt=s.global_mods.get(mn,0)
                bought=(cnt>0)
                seed=sum(ord(ch) for ch in mn)
                rune=RUNES_ALL[seed%len(RUNES_ALL)]
                if bought:
                    self._circle_w(rx,ry,3,fill=self._f(gc,0.65),outline=gc)
                    self._text_w(rx,ry,text=rune,fill=self._b(gc,"#ffffff",0.80),
                                 font=("TkFixedFont",max(5,int(cr*0.30))))
                    if cnt>1:
                        nx2,ny2=self._wpt(rx,ry,5,315)
                        self._text_w(nx2,ny2,text=str(cnt),fill=gc,font=("TkFixedFont",4))
                else:
                    self._circle_w(rx,ry,2,fill=self._f(gc,0.14),outline="")
                    self._text_w(rx,ry,text=rune,fill=self._f(gc,0.35),
                                 font=("TkFixedFont",max(5,int(cr*0.26))))
                cost=md.get("cost",0); mx=md.get("max",1)
                tip=f"{mn}\nCategory: {cat}  Cost: {cost} pt  Max: {mx}\n{md.get('desc','')}"
                def _mod_cb(mod_name=mn,max_v=mx):
                    cur=self.spell.global_mods.get(mod_name,0)
                    self.spell.global_mods[mod_name]=(cur+1)%(max_v+1)
                    if self._on_change: self._on_change()
                    self._redraw()
                mod_key=f"mod/{mn}"
                mod_info_d={"type":"mod","name":mn,"cost":cost,"max":mx,"key":mod_key}
                is_drawback_mod=(mod_key in (self.spell.drawback_buys or {}))
                if is_drawback_mod:
                    self._ring_w(rx,ry,4,self._f("#ffffff",0.60),w=1)
                self._hit_zones.append((rx,ry,hz_r,_mod_cb,tip,mod_info_d))

    def _draw_center_hub(self):
        """Central hub — 6 category mod circles inside, level display at centre.
        Draw order: background → dividers → mod circles → level display → borders."""
        r=self.CENTER_R
        CATS=list(CAT_COLORS.keys()); nc=len(CATS); seg=360/nc

        # ── Background fill ──────────────────────────────────────
        self._circle_w(0,0,r,fill=self._f("#1a1208",0.60),outline="")

        # ── Faint single inner ring (behind mod circles) ─────────
        self._ring_w(0,0,r*0.42,self._f("#6688aa",0.18),w=1)

        # ── Category section radial dividers ─────────────────────
        for i in range(nc):
            a=i*seg
            x1,y1=self._wpt(0,0,r*0.30,a); x2,y2=self._wpt(0,0,r*0.92,a)
            self._line_w(x1,y1,x2,y2,fill=self._f("#aaaacc",0.22),width=1)

        # ── 6 modifier category circles (drawn here, inside hub) ──
        self._draw_mod_circles()

        # ── Centre level gem (drawn on top of mod circles) ───────
        lvl_idx,lvl_name,lvl_col=self.spell.level_info
        r_gem=r*0.28
        self._circle_w(0,0,r_gem*1.35,fill=self._f(lvl_col,0.25),outline="")
        self._ring_w(0,0,r_gem*1.35,self._b(lvl_col,"#ffffff",0.40),w=2)
        if lvl_idx<=9:
            disp="C" if lvl_idx==0 else str(lvl_idx)
            self._text_w(0,0,text=disp,fill=lvl_col,font=("Georgia",max(18,int(r_gem*1.5)),"bold"))
        elif lvl_idx==10: self._draw_fist_w(0,0,r_gem,"#FFD700")
        elif lvl_idx==11: self._draw_crown_w(0,0,r_gem,"#FFD700")
        elif lvl_idx==12: self._draw_wings_w(0,0,r_gem,"#FFFFFF")
        elif lvl_idx==13: self._draw_sun_w(0,0,r_gem,"#FFFFFF")
        else:             self._draw_stars_w(0,0,r_gem,"#FFFFFF")
        self._text_w(0,r_gem*1.92,text=lvl_name.upper(),
                     fill=self._b(lvl_col,"#aabbdd",0.50),font=("TkFixedFont",5))

        # ── Outer border + category colour arcs (drawn last = on top) ──
        self._ring_w(0,0,r,self._b("#e8d8a0","#ffffff",0.55),w=2)
        self._ring_w(0,0,r*0.94,self._f("#6688aa",0.20),w=1)
        # Dot ring just inside outer border
        for i in range(16):
            a=i*(360/16); dx,dy=self._wpt(0,0,r*0.91,a)
            self._circle_w(dx,dy,2 if i%4==0 else 1,fill=self._f("#aabbdd",0.40),outline="")
        # Category colour arcs with rune labels
        for i,cat in enumerate(CATS):
            gc=CAT_COLORS[cat]; a_s=i*seg
            self._arc_ring_w(0,0,r,a_s,seg-3,outline=self._f(gc,0.50),width=3)
            mid_a=a_s+seg/2; lx,ly=self._wpt(0,0,r*0.87,mid_a)
            self._text_w(lx,ly,text=MOD_RUNES[cat][0],fill=self._f(gc,0.65),
                         font=("TkFixedFont",6),angle=-(mid_a-90))

    def _draw_drawback_rings(self):
        """Draw hollow white rings around the center hub for each active drawback."""
        if not self.spell or not self.spell.drawback_buys: return
        keys=list(self.spell.drawback_buys.keys())
        n=len(keys); orbit=self.CENTER_R+18; ring_r=7
        for i,key in enumerate(keys):
            neg_name=self.spell.drawback_buys[key]
            a=i*(360/max(n,1))
            rx,ry=self._wpt(0,0,orbit,a)
            # Hollow white ring — no fill, white outline
            self._circle_w(rx,ry,ring_r,fill="",outline="")
            self._ring_w(rx,ry,ring_r,"#ffffff",w=2)
            self._ring_w(rx,ry,ring_r*0.5,self._f("#ffffff",0.25),w=1)
            # Tooltip hit zone
            tip=f"Drawback: {neg_name}"
            nm_data=next((m for m in NEGATIVE_MODS if m["name"]==neg_name),None)
            if nm_data: tip+=f"\n{nm_data['desc']}"
            tip+=f"\n(from: {key.split('/',1)[-1]})"
            self._hit_zones.append((rx,ry,ring_r+4,lambda:None,tip,None))

    def _draw_when_then_ring(self):
        """Draw a decorative rune ring around the hub for When-Then conditions."""
        s = self.spell
        if not s: return
        conds = getattr(s, 'when_then_conditions', [])
        if not conds: return
        orbit = self.CENTER_R + 30
        n = len(conds)
        palette = ["#ff9960","#60dd88","#6080ff","#ffdd60","#ff88ff",
                   "#88ffff","#ff6080","#88ddff","#ddff88","#ffaa88"]
        # Draw the ring track
        self._ring_w(0, 0, orbit, self._f("#8899bb", 0.20), w=1, dash=(3, 4))
        for i, cond in enumerate(conds):
            a = i * (360 / n)
            rx, ry = self._wpt(0, 0, orbit, a)
            c = palette[i % len(palette)]
            when_t = cond.get('when_text', '')
            then_t = cond.get('then_text', '')
            seed = (sum(ord(ch) for ch in (when_t + then_t)) + i * 37) % len(RUNES_ALL)
            rune = RUNES_ALL[seed]
            # Node circle
            self._circle_w(rx, ry, 7, fill=self._f(c, 0.28), outline='')
            self._ring_w(rx, ry, 7, self._b(c, '#ffffff', 0.60), w=1)
            self._text_w(rx, ry, text=rune,
                         fill=self._b(c, '#ffffff', 0.88), font=('TkFixedFont', 8))
            # Hit zone with tooltip
            tip = f"WHEN: {when_t or '(none)'}\nTHEN: {then_t or '(none)'}"
            self._hit_zones.append((rx, ry, 9, lambda: None, tip, None))

    def _draw_status_bar(self,W,H):
        s=self.spell; _,lvl,col=s.level_info; pts=s.total_points
        cx,cy=self._tc(0,self.OUTER_R+14)
        self.create_text(cx,cy,
                         text=f"{lvl}  ·  {pts} pts  ·  {self._zoom:.2f}×  [scroll=zoom  R-drag=pan]",
                         fill=self._b("#445566",col,0.7),font=("Georgia",9,"italic"))
        if s.drawback_buys and not s.is_complete:
            wx,wy=self._tc(0,self.OUTER_R+26)
            self.create_text(wx,wy,text="⚠ more drawbacks than purchases — add normal items",
                             fill="#ff5555",font=("TkDefaultFont",7,"bold"),anchor="center")

    def export_png(self,fp):
        try:
            from PIL import ImageGrab
            x,y=self.winfo_rootx(),self.winfo_rooty()
            ImageGrab.grab(bbox=(x,y,x+self.winfo_width(),y+self.winfo_height())).save(fp); return True
        except ImportError:
            messagebox.showerror("Export Error","Pillow required.\npip install Pillow"); return False
        except Exception as e:
            messagebox.showerror("Export Error",str(e)); return False


# ═══════════════════════════════════════════════════════════════
#  DRAG-RESIZABLE THREE-PANE FRAME
# ═══════════════════════════════════════════════════════════════
class DragPane(tk.Frame):
    SASH_W=6; SC="#1a2a3a"; SH="#3355ff"
    def __init__(self,master,**kw):
        kw.setdefault("bg","#0a0a14"); super().__init__(master,**kw)
        self._lw=400; self._rw=450; self._drag=None; self._build()
    def _build(self):
        self.left=tk.Frame(self,bg="#0a0a14"); self.sash1=tk.Frame(self,bg=self.SC,cursor="sb_h_double_arrow",width=self.SASH_W)
        self.center=tk.Frame(self,bg="#0a0a14"); self.sash2=tk.Frame(self,bg=self.SC,cursor="sb_h_double_arrow",width=self.SASH_W)
        self.right=tk.Frame(self,bg="#0a0a14")
        for s in (self.sash1,self.sash2):
            s.bind("<Enter>",lambda e,w=s:w.configure(bg=self.SH)); s.bind("<Leave>",lambda e,w=s:w.configure(bg=self.SC))
            s.bind("<ButtonPress-1>",self._start); s.bind("<B1-Motion>",self._move); s.bind("<ButtonRelease-1>",self._end)
        self.bind("<Configure>",lambda _:self._layout())
    def _start(self,e): self._drag={"s":e.widget,"x":e.x_root,"lw":self._lw,"rw":self._rw}
    def _move(self,e):
        if not self._drag: return
        dx=e.x_root-self._drag["x"]; W=self.winfo_width()-2*self.SASH_W
        if self._drag["s"] is self.sash1: self._lw=max(200,min(self._drag["lw"]+dx,W-self._rw-200))
        else: self._rw=max(200,min(self._drag["rw"]-dx,W-self._lw-200))
        self._layout()
    def _end(self,e): self._drag=None
    def _layout(self):
        W=self.winfo_width(); H=self.winfo_height()
        if W<10: return
        cw=max(100,W-self._lw-self._rw-2*self.SASH_W); s1=self._lw; s2=s1+self.SASH_W+cw
        self.left.place(x=0,y=0,width=self._lw,height=H); self.sash1.place(x=s1,y=0,width=self.SASH_W,height=H)
        self.center.place(x=s1+self.SASH_W,y=0,width=cw,height=H); self.sash2.place(x=s2,y=0,width=self.SASH_W,height=H)
        self.right.place(x=s2+self.SASH_W,y=0,width=self._rw,height=H)


# ═══════════════════════════════════════════════════════════════
#  SCHOOL ABILITY PANEL
# ═══════════════════════════════════════════════════════════════
class SchoolAbilityPanel(tk.Frame):
    def __init__(self,master,school,spell,on_change,**kw):
        kw.setdefault("bg","#0a0a14"); super().__init__(master,**kw)
        self.school=school; self.spell=spell; self.on_change=on_change
        self._cvars: Dict[str,tk.StringVar]={}
        self._rcvars: Dict[str,tk.StringVar]={}
        self._size_var:Optional[tk.DoubleVar]=None; self._size_lbl=None
        self._cap_lbl=None; self._build()

    def _build(self):
        bg="#0a0a14"; c=SCHOOLS[self.school]["color"]
        tk.Label(self,text=f"{SCHOOLS[self.school]['symbol']}  {self.school}",bg=bg,fg=c,font=("Georgia",12,"bold")).pack(pady=(10,4))
        tk.Label(self,text=SCHOOLS[self.school]["short"],bg=bg,fg="#8899bb",font=("Georgia",8,"italic"),wraplength=360).pack()
        df=tk.Frame(self,bg="#0d0d1f",relief="solid",bd=1); df.pack(fill=tk.X,padx=8,pady=(4,6))
        tk.Label(df,text=SCHOOLS[self.school]["desc"],bg="#0d0d1f",fg="#556677",font=("Georgia",8),wraplength=350,justify=tk.LEFT,padx=6,pady=4).pack()
        # Size slider
        sf=tk.Frame(self,bg=bg); sf.pack(fill=tk.X,padx=8,pady=(0,4))
        tk.Label(sf,text="Module size:",bg=bg,fg="#8899bb",font=("Georgia",9)).pack(side=tk.LEFT)
        self._size_var=tk.DoubleVar(value=self.spell.circle_sizes.get(self.school,1.0))
        sl=ttk.Scale(sf,from_=0.4,to=2.2,orient=tk.HORIZONTAL,variable=self._size_var,command=self._on_size,length=120); sl.pack(side=tk.LEFT,padx=6)
        self._size_lbl=tk.Label(sf,text="1.0×",bg=bg,fg=c,font=("TkFixedFont",9),width=5); self._size_lbl.pack(side=tk.LEFT)
        # Capstone status
        self._cap_lbl=tk.Label(self,text="",bg=bg,font=("Georgia",9,"bold"),wraplength=350,justify=tk.LEFT,padx=8); self._cap_lbl.pack(fill=tk.X)
        # Ring mods
        tk.Frame(self,bg=c,height=1).pack(fill=tk.X,padx=8,pady=2)
        tk.Label(self,text="Ring Modifiers  (fill all 4 groups × 3 to unlock Capstone)",bg=bg,fg="#445566",font=("Georgia",8,"italic")).pack(anchor=tk.W,padx=8,pady=(4,2))
        grp_c={"Range":"#ff6060","Duration":"#60ee88","Area":"#6088ff","Power":"#ffd060"}
        ring_dict=self.spell.ring_mods.setdefault(self.school,{})
        rlabels=SCHOOLS[self.school].get("ring_mods",{})
        for grp in RING_GROUPS:
            lbl=rlabels.get(grp,grp); gc=grp_c[grp]
            row=tk.Frame(self,bg=bg); row.pack(fill=tk.X,padx=10,pady=1)
            tk.Button(row,text="−",width=2,bg="#1a1a2e",fg="#ff5555",relief="flat",font=("TkFixedFont",10),bd=0,
                      command=lambda g=grp:self._radj(g,-1)).pack(side=tk.LEFT)
            cv=tk.StringVar(value=str(ring_dict.get(grp,0))); self._rcvars[grp]=cv
            tk.Label(row,textvariable=cv,bg=bg,fg=gc,font=("TkFixedFont",10),width=2).pack(side=tk.LEFT)
            tk.Button(row,text="+",width=2,bg="#1a1a2e",fg="#55ff77",relief="flat",font=("TkFixedFont",10),bd=0,
                      command=lambda g=grp:self._radj(g,+1)).pack(side=tk.LEFT,padx=(0,6))
            tk.Label(row,text=f"{grp}: {lbl}",bg=bg,fg=gc,font=("Georgia",9)).pack(side=tk.LEFT,fill=tk.X,expand=True)
            rune=MOD_RUNES.get(grp,["?"])[0]
            tk.Label(row,text=f"{rune} [+1/sect·max 3]",bg=bg,fg="#334455",font=("TkFixedFont",8)).pack(side=tk.RIGHT,padx=4)
        tk.Frame(self,bg="#1a2233",height=1).pack(fill=tk.X,padx=8,pady=4)
        tk.Label(self,text="School Abilities",bg=bg,fg="#445566",font=("Georgia",8,"italic")).pack(anchor=tk.W,padx=8,pady=(0,4))
        ab_dict=self.spell.school_abilities.setdefault(self.school,{})
        for ab,abd in SCHOOLS[self.school].get("abilities",{}).items():
            self._arow(ab,abd,c,bg,ab_dict)
        # Capstone info box — shows synergies unlocked when capstone achieved
        cap_data=CAPSTONES.get(self.school,{})
        if cap_data:
            cf=tk.Frame(self,bg="#0a0f0a",relief="solid",bd=1); cf.pack(fill=tk.X,padx=8,pady=(8,4))
            ch=tk.Frame(cf,bg="#0a0f0a"); ch.pack(fill=tk.X)
            tk.Label(ch,text=f"⚜ CAPSTONE: {cap_data['name']}",bg="#0a0f0a",fg="#FFD700",font=("Georgia",9,"bold"),wraplength=270,justify=tk.LEFT).pack(side=tk.LEFT,fill=tk.X,expand=True,padx=6,pady=(4,2))
            ttk.Button(ch,text="✎ Edit",command=lambda s=self.school:CapstoneEditor(self,s,self.on_change)).pack(side=tk.RIGHT,padx=4,pady=2)
            tk.Label(cf,text="Fill all 4 ring groups to 3/3 to unlock these synergies:",bg="#0a0f0a",fg="#556644",font=("Georgia",8,"italic"),wraplength=340,justify=tk.LEFT).pack(fill=tk.X,padx=6,pady=(2,2))
            for (s1,s2),sname in SCHOOL_CONNECTIONS.items():
                if s1==self.school or s2==self.school:
                    other=s2 if s1==self.school else s1
                    oc=SCHOOLS[other]["color"]
                    srow=tk.Frame(cf,bg="#0a0f0a"); srow.pack(fill=tk.X,padx=8,pady=1)
                    tk.Label(srow,text=f"⊕ {other}:",bg="#0a0f0a",fg=oc,font=("Georgia",8,"bold")).pack(side=tk.LEFT)
                    tk.Label(srow,text=sname.split("—")[0].strip(),bg="#0a0f0a",fg="#8a9a5a",font=("Georgia",8),wraplength=230,justify=tk.LEFT).pack(side=tk.LEFT,padx=4)

    def _arow(self,ab,abd,c,bg,ab_dict):
        outer=tk.Frame(self,bg=bg); outer.pack(fill=tk.X,padx=6,pady=1)
        row=tk.Frame(outer,bg=bg); row.pack(fill=tk.X)
        tk.Button(row,text="−",width=2,bg="#1a1a2e",fg="#ff5555",relief="flat",font=("TkFixedFont",10),bd=0,command=lambda n=ab:self._adj(n,-1)).pack(side=tk.LEFT)
        cv=tk.StringVar(value=str(ab_dict.get(ab,0))); self._cvars[ab]=cv
        tk.Label(row,textvariable=cv,bg=bg,fg=c,font=("TkFixedFont",10),width=3).pack(side=tk.LEFT)
        tk.Button(row,text="+",width=2,bg="#1a1a2e",fg="#55ff77",relief="flat",font=("TkFixedFont",10),bd=0,command=lambda n=ab:self._adj(n,+1)).pack(side=tk.LEFT,padx=(0,6))
        tk.Label(row,text=ab,bg=bg,fg="#c8d0e8",font=("Georgia",9),anchor="w").pack(side=tk.LEFT,fill=tk.X,expand=True)
        cs=f"+{abd['cost']}" if abd["cost"]>=0 else str(abd["cost"])
        tk.Label(row,text=f"[{cs}/×]",bg=bg,fg="#445566",font=("TkFixedFont",8)).pack(side=tk.RIGHT,padx=4)
        tk.Label(outer,text=f"  ↳ {abd['desc']}",bg="#090914",fg="#3a4a5a",font=("Georgia",8,"italic"),wraplength=350,justify=tk.LEFT,anchor="w",padx=12).pack(fill=tk.X)

    def _adj(self,ab,delta):
        d=self.spell.school_abilities.setdefault(self.school,{}); d[ab]=max(0,d.get(ab,0)+delta); self._cvars[ab].set(str(d[ab])); self.on_change()
    def _radj(self,grp,delta):
        d=self.spell.ring_mods.setdefault(self.school,{}); d[grp]=max(0,min(3,d.get(grp,0)+delta)); self._rcvars[grp].set(str(d[grp])); self._update_cap(); self.on_change()
    def _on_size(self,_=None):
        v=round(self._size_var.get(),2); self.spell.circle_sizes[self.school]=v
        if self._size_lbl: self._size_lbl.configure(text=f"{v:.1f}×"); self.on_change()
    def _update_cap(self):
        if self._cap_lbl is None: return
        cap=self.spell.capstone_active(self.school)
        if cap:
            n_syn=sum(1 for (s1,s2) in SCHOOL_CONNECTIONS if s1==self.school or s2==self.school)
            self._cap_lbl.configure(text=f"⚜ CAPSTONE ACTIVE — {n_syn} synergies unlocked!",fg="#FFD700")
        else:
            rd=self.spell.ring_mods.get(self.school,{}); filled=sum(1 for g in RING_GROUPS if rd.get(g,0)>=3)
            self._cap_lbl.configure(text=f"Capstone: {filled}/4 groups filled",fg="#445566")
    def refresh(self):
        ab=self.spell.school_abilities.get(self.school,{})
        for k,v in self._cvars.items(): v.set(str(ab.get(k,0)))
        rd=self.spell.ring_mods.get(self.school,{})
        for k,v in self._rcvars.items(): v.set(str(rd.get(k,0)))
        if self._size_var:
            sv=self.spell.circle_sizes.get(self.school,1.0); self._size_var.set(sv)
            if self._size_lbl: self._size_lbl.configure(text=f"{sv:.1f}×")
        self._update_cap()


# ═══════════════════════════════════════════════════════════════
#  ELEMENT SELECTOR PANEL
# ═══════════════════════════════════════════════════════════════
class ElementPanel(tk.Frame):
    def __init__(self,master,spell,on_change,**kw):
        kw.setdefault("bg","#0a0a14"); super().__init__(master,**kw)
        self.spell=spell; self.on_change=on_change; self._build()

    def _build(self):
        bg="#0a0a14"
        tk.Label(self,text="✦ Elemental Affinities",bg=bg,fg="#c8a840",font=("Georgia",12,"bold")).pack(pady=(10,6))
        tk.Label(self,text="Active elements appear as concentric rings in the magic circle.\nEach element controls a major modification to the spell.",
                 bg=bg,fg="#556677",font=("Georgia",8,"italic"),wraplength=360,justify=tk.LEFT).pack(padx=8,pady=(0,8))
        for el,edata in ELEMENTS.items():
            self._elem_block(el,edata,bg)

    def _elem_block(self,el,edata,bg):
        c=edata["color"]
        frm=tk.Frame(self,bg="#0d0d1a",relief="solid",bd=1); frm.pack(fill=tk.X,padx=6,pady=4)
        hdr=tk.Frame(frm,bg="#0d0d1a"); hdr.pack(fill=tk.X)
        # Toggle button
        var=tk.BooleanVar(value=bool(self.spell.elements.get(el,False)))
        cb=tk.Checkbutton(hdr,text=f"{edata['symbol']} {el}  [{edata['rune']}]",variable=var,
                          command=lambda e=el,v=var:self._toggle(e,v),
                          bg="#0d0d1a",fg=c,selectcolor="#1a1a2e",activebackground="#0d0d1a",
                          activeforeground=c,font=("Georgia",11,"bold"),anchor="w",relief="flat",highlightthickness=0)
        cb.pack(side=tk.LEFT,padx=6,pady=4)
        ttk.Button(hdr,text="✎",width=3,command=lambda e=el:ElementEditor(self,e,self.on_change)).pack(side=tk.RIGHT,padx=2,pady=4)
        tk.Label(hdr,text=f"+2 pts",bg="#0d0d1a",fg="#445566",font=("TkFixedFont",8)).pack(side=tk.RIGHT,padx=4)
        # Description
        tk.Label(frm,text=edata["desc"],bg="#0d0d1a",fg="#667788",font=("Georgia",8),wraplength=340,justify=tk.LEFT).pack(fill=tk.X,padx=10,pady=(0,4))
        # Modification
        mod_frm=tk.Frame(frm,bg="#0a0a14"); mod_frm.pack(fill=tk.X,padx=8,pady=(0,4))
        tk.Label(mod_frm,text="Effect:",bg="#0a0a14",fg="#8899aa",font=("Georgia",8,"bold")).pack(side=tk.LEFT)
        tk.Label(mod_frm,text=edata["modification"],bg="#0a0a14",fg=c,font=("Georgia",8,"italic"),wraplength=280).pack(side=tk.LEFT,padx=4)
        # Celestial subtypes
        if "subtypes" in edata:
            sub_frm=tk.Frame(frm,bg="#0d0d1a"); sub_frm.pack(fill=tk.X,padx=12,pady=(0,4))
            tk.Label(sub_frm,text="Subtype (+1 pt):",bg="#0d0d1a",fg="#8899aa",font=("Georgia",8)).pack(side=tk.LEFT)
            sub_var=tk.StringVar(value=self.spell.elements.get(el,"") if isinstance(self.spell.elements.get(el),str) else "")
            for sname,sdata in edata["subtypes"].items():
                rb=tk.Radiobutton(sub_frm,text=f"{sdata['rune']} {sname}",variable=sub_var,value=sname,
                                  command=lambda e=el,sv=sub_var:self._set_subtype(e,sv),
                                  bg="#0d0d1a",fg=sdata["color"],selectcolor="#1a1a2e",
                                  activebackground="#0d0d1a",activeforeground=sdata["color"],
                                  font=("Georgia",9),relief="flat",highlightthickness=0)
                rb.pack(side=tk.LEFT,padx=4)
        # Constellation upgrade nodes
        nodes=edata.get("nodes",[])
        if nodes:
            nf=tk.Frame(frm,bg="#090910",relief="solid",bd=1); nf.pack(fill=tk.X,padx=10,pady=(2,6))
            tk.Label(nf,text="✦ Constellation Upgrades:",bg="#090910",fg=c,font=("Georgia",8,"bold"),padx=6).pack(anchor=tk.W,pady=(4,0))
            for nd in nodes:
                nrow=tk.Frame(nf,bg="#090910"); nrow.pack(fill=tk.X,padx=6,pady=1)
                tk.Button(nrow,text="−",width=2,bg="#0d0d1a",fg="#ff5555",relief="flat",font=("TkFixedFont",9),bd=0,
                    command=lambda e=el,n=nd["name"]:self._adj_node(e,n,-1)).pack(side=tk.LEFT)
                nv=tk.StringVar(value=str(self.spell.element_nodes.get(el,{}).get(nd["name"],0)))
                if not hasattr(self,"_node_vars"): self._node_vars={}
                self._node_vars[f"{el}:{nd['name']}"]=nv
                tk.Label(nrow,textvariable=nv,bg="#090910",fg=c,font=("TkFixedFont",9),width=2).pack(side=tk.LEFT)
                tk.Button(nrow,text="+",width=2,bg="#0d0d1a",fg="#55ff77",relief="flat",font=("TkFixedFont",9),bd=0,
                    command=lambda e=el,n=nd["name"]:self._adj_node(e,n,+1)).pack(side=tk.LEFT,padx=(0,4))
                tk.Label(nrow,text=f"{nd['glyph']} {nd['name']}",bg="#090910",fg="#c8c0a0",font=("Georgia",8)).pack(side=tk.LEFT)
                tk.Label(nrow,text=f"[+{nd['cost']}]",bg="#090910",fg="#445544",font=("TkFixedFont",7)).pack(side=tk.RIGHT,padx=2)
                tk.Label(nf,text=f"   ↳ {nd['desc']}",bg="#090910",fg="#3a4a3a",font=("Georgia",7,"italic"),
                         wraplength=330,justify=tk.LEFT,anchor="w").pack(fill=tk.X,padx=12)

    def _toggle(self,el,var):
        if var.get(): self.spell.elements[el]=True
        else:
            self.spell.elements.pop(el,None)
            self.spell.element_nodes.pop(el,None)
        self.on_change()

    def _set_subtype(self,el,sub_var):
        st=sub_var.get()
        if st: self.spell.elements[el]=st
        self.on_change()

    def _adj_node(self,el,node_name,delta):
        d=self.spell.element_nodes.setdefault(el,{})
        d[node_name]=max(0,d.get(node_name,0)+delta)
        key=f"{el}:{node_name}"
        if hasattr(self,"_node_vars") and key in self._node_vars:
            self._node_vars[key].set(str(d[node_name]))
        self.on_change()


# ═══════════════════════════════════════════════════════════════
#  ELEMENT EDITOR DIALOG
# ═══════════════════════════════════════════════════════════════
class ElementEditor(tk.Toplevel):
    def __init__(self,parent,elem_name,cb):
        super().__init__(parent); self.elem_name=elem_name; self.cb=cb
        self.title(f"Edit Element: {elem_name}"); self.configure(bg="#0a0a14"); self.grab_set(); self.resizable(False,False)
        bg="#0a0a14"; fg="#c8d0e8"; edata=ELEMENTS.get(elem_name,{})
        tk.Label(self,text=f"{edata.get('symbol','')} {elem_name}",bg=bg,fg=edata.get("color","#fff"),font=("Georgia",13,"bold")).pack(pady=(10,4))
        tk.Label(self,text="Effect / Modification:",bg=bg,fg="#8899bb",font=("Georgia",9)).pack(anchor=tk.W,padx=12)
        self._mv=tk.StringVar(value=edata.get("modification",""))
        tk.Entry(self,textvariable=self._mv,bg="#111120",fg=fg,insertbackground=fg,relief="solid",bd=1,width=44).pack(padx=12,pady=4)
        tk.Label(self,text="Description:",bg=bg,fg="#8899bb",font=("Georgia",9)).pack(anchor=tk.W,padx=12)
        self._dv=tk.Text(self,height=3,width=44,bg="#111120",fg=fg,insertbackground=fg,relief="solid",bd=1,font=("Georgia",9),wrap=tk.WORD)
        self._dv.pack(padx=12,pady=4); self._dv.insert("1.0",edata.get("desc",""))
        tk.Label(self,text="Active sub-element connections:",bg=bg,fg="#8899bb",font=("Georgia",8)).pack(anchor=tk.W,padx=12)
        ct=tk.Text(self,height=4,width=44,bg="#0d0d1a",fg="#667788",relief="solid",bd=1,font=("Courier",8),state="normal")
        ct.pack(padx=12,pady=4)
        for (a,b),eff in ELEMENT_CONNECTIONS.items():
            if a==elem_name or b==elem_name:
                ct.insert(tk.END,f"+ {b if a==elem_name else a}: {eff[:55]}\n")
        ct.configure(state="disabled")
        bf=tk.Frame(self,bg=bg); bf.pack(pady=10)
        ttk.Button(bf,text="  Save  ",command=self._save).pack(side=tk.LEFT,padx=6)
        ttk.Button(bf,text="Cancel",command=self.destroy).pack(side=tk.LEFT,padx=6)
        self.geometry("+%d+%d"%(parent.winfo_rootx()+60,parent.winfo_rooty()+60))
    def _save(self):
        ELEMENTS[self.elem_name]["modification"]=self._mv.get().strip()
        ELEMENTS[self.elem_name]["desc"]=self._dv.get("1.0",tk.END).strip()
        self.cb(); self.destroy()

# ═══════════════════════════════════════════════════════════════
#  CAPSTONE EDITOR DIALOG
# ═══════════════════════════════════════════════════════════════
class CapstoneEditor(tk.Toplevel):
    def __init__(self,parent,school,cb):
        super().__init__(parent); self.school=school; self.cb=cb
        self.title(f"Edit Capstone: {school}"); self.configure(bg="#0a0a14"); self.grab_set(); self.resizable(False,False)
        bg="#0a0a14"; fg="#c8d0e8"; cd=CAPSTONES.get(school,{}); sc=SCHOOLS[school]["color"]
        tk.Label(self,text=f"⚜ {school} Capstone",bg=bg,fg=sc,font=("Georgia",12,"bold")).pack(pady=(10,4))
        tk.Label(self,text="Capstone Name:",bg=bg,fg="#8899bb",font=("Georgia",9)).pack(anchor=tk.W,padx=12)
        self._nv=tk.StringVar(value=cd.get("name",""))
        tk.Entry(self,textvariable=self._nv,bg="#111120",fg=fg,insertbackground=fg,relief="solid",bd=1,width=40).pack(padx=12,pady=4)
        tk.Label(self,text="Description:",bg=bg,fg="#8899bb",font=("Georgia",9)).pack(anchor=tk.W,padx=12)
        self._dv=tk.Text(self,height=4,width=44,bg="#111120",fg=fg,insertbackground=fg,relief="solid",bd=1,font=("Georgia",9),wrap=tk.WORD)
        self._dv.pack(padx=12,pady=4); self._dv.insert("1.0",cd.get("desc",""))
        rf=tk.Frame(self,bg=bg); rf.pack(fill=tk.X,padx=12,pady=4)
        tk.Label(rf,text="Sigil (1 char):",bg=bg,fg="#8899bb",font=("Georgia",9)).pack(side=tk.LEFT)
        self._gv=tk.StringVar(value=cd.get("glyph","⚜"))
        tk.Entry(rf,textvariable=self._gv,bg="#111120",fg=fg,insertbackground=fg,relief="solid",bd=1,width=4).pack(side=tk.LEFT,padx=6)
        tk.Label(rf,text="Orbital (8 chars):",bg=bg,fg="#8899bb",font=("Georgia",9)).pack(side=tk.LEFT,padx=(12,0))
        self._rv=tk.StringVar(value=cd.get("ring","✦✦✦✦✦✦✦✦"))
        tk.Entry(rf,textvariable=self._rv,bg="#111120",fg=fg,insertbackground=fg,relief="solid",bd=1,width=12).pack(side=tk.LEFT,padx=6)
        bf=tk.Frame(self,bg=bg); bf.pack(pady=10)
        ttk.Button(bf,text="  Save  ",command=self._save).pack(side=tk.LEFT,padx=6)
        ttk.Button(bf,text="Cancel",command=self.destroy).pack(side=tk.LEFT,padx=6)
        self.geometry("+%d+%d"%(parent.winfo_rootx()+60,parent.winfo_rooty()+60))
    def _save(self):
        CAPSTONES[self.school]["name"]=self._nv.get().strip()
        CAPSTONES[self.school]["desc"]=self._dv.get("1.0",tk.END).strip()
        CAPSTONES[self.school]["glyph"]=self._gv.get()[:1] or "⚜"
        CAPSTONES[self.school]["ring"]=(self._rv.get()+"✦✦✦✦✦✦✦✦")[:8]
        self.cb(); self.destroy()

# ═══════════════════════════════════════════════════════════════
#  GLOBAL MOD PANEL
# ═══════════════════════════════════════════════════════════════
class GlobalModPanel(tk.Frame):
    def __init__(self,master,spell,on_change,**kw):
        kw.setdefault("bg","#0a0a14"); super().__init__(master,**kw)
        self.spell=spell; self.on_change=on_change; self._cvars:Dict[str,tk.StringVar]={}; self._rows=None; self._build()
    def _build(self):
        bg="#0a0a14"
        hdr=tk.Frame(self,bg=bg); hdr.pack(fill=tk.X,padx=6,pady=4)
        tk.Label(hdr,text="Global Modifiers",bg=bg,fg="#c8a840",font=("Georgia",11,"bold")).pack(side=tk.LEFT)
        ttk.Button(hdr,text="+ New",command=self._add).pack(side=tk.RIGHT,padx=2)
        self._rows=tk.Frame(self,bg=bg); self._rows.pack(fill=tk.BOTH,expand=True); self._rebuild()
    def _rebuild(self):
        for w in self._rows.winfo_children(): w.destroy(); self._cvars.clear()
        bg="#0a0a14"
        cats:Dict[str,List[str]]={}
        for m,d in GLOBAL_MODS.items(): cats.setdefault(d["cat"],[]).append(m)
        for cat,mods in cats.items():
            gc=CAT_COLORS.get(cat,"#aaa")
            hf=tk.Frame(self._rows,bg=bg); hf.pack(fill=tk.X,padx=6,pady=(6,2))
            tk.Frame(hf,bg=gc,height=1).pack(fill=tk.X)
            rune=MOD_RUNES.get(cat,["?"])[0]
            tk.Label(hf,text=f"  {rune} {cat}  ",bg=bg,fg=gc,font=("Georgia",9,"bold italic")).pack(anchor=tk.W)
            for mod in mods: self._mrow(mod,gc,bg)
    def _mrow(self,mn,color,bg):
        d=GLOBAL_MODS[mn]; outer=tk.Frame(self._rows,bg=bg); outer.pack(fill=tk.X,padx=6,pady=1)
        row=tk.Frame(outer,bg=bg); row.pack(fill=tk.X)
        tk.Button(row,text="−",width=2,bg="#1a1a2e",fg="#ff5555",relief="flat",font=("TkFixedFont",10),bd=0,command=lambda n=mn:self._adj(n,-1)).pack(side=tk.LEFT)
        cv=tk.StringVar(value=str(self.spell.global_mods.get(mn,0))); self._cvars[mn]=cv
        tk.Label(row,textvariable=cv,bg=bg,fg=color,font=("TkFixedFont",10),width=3).pack(side=tk.LEFT)
        tk.Button(row,text="+",width=2,bg="#1a1a2e",fg="#55ff77",relief="flat",font=("TkFixedFont",10),bd=0,command=lambda n=mn:self._adj(n,+1)).pack(side=tk.LEFT,padx=(0,4))
        tk.Label(row,text=mn,bg=bg,fg="#c8d0e8",font=("Georgia",9),anchor="w").pack(side=tk.LEFT,fill=tk.X,expand=True)
        cs=f"+{d['cost']}" if d["cost"]>=0 else str(d["cost"]); ms=f"max {d['max']}×" if d["max"]<99 else "∞"
        rune=MOD_RUNES.get(d["cat"],["?"])[0]
        tk.Label(row,text=f"{rune}[{cs}·{ms}]",bg=bg,fg="#445566",font=("TkFixedFont",8)).pack(side=tk.RIGHT,padx=1)
        tk.Button(row,text="✎",width=2,bg="#1a1a2e",fg="#88aaff",relief="flat",font=("TkFixedFont",9),bd=0,command=lambda n=mn:self._edit(n)).pack(side=tk.RIGHT,padx=1)
        tk.Button(row,text="✕",width=2,bg="#1a1a2e",fg="#ff4444",relief="flat",font=("TkFixedFont",9),bd=0,command=lambda n=mn:self._rm(n)).pack(side=tk.RIGHT,padx=1)
        tk.Label(outer,text=f"  ↳ {d['desc']}",bg="#090914",fg="#3a4a5a",font=("Georgia",8,"italic"),wraplength=360,justify=tk.LEFT,anchor="w",padx=12).pack(fill=tk.X)
    def _adj(self,mn,delta):
        mx=GLOBAL_MODS[mn]["max"]; new=max(0,min(mx,self.spell.global_mods.get(mn,0)+delta))
        self.spell.global_mods[mn]=new
        if mn in self._cvars: self._cvars[mn].set(str(new))
        self.on_change()
    def _add(self): ModEditor(self,GLOBAL_MODS,None,self.spell,self._after)
    def _edit(self,n): ModEditor(self,GLOBAL_MODS,n,self.spell,self._after)
    def _rm(self,n):
        if messagebox.askyesno("Remove",f"Remove '{n}'?"):
            GLOBAL_MODS.pop(n,None); self.spell.global_mods.pop(n,None); self._after()
    def _after(self): self._rebuild(); self.on_change()
    def refresh(self):
        for n,cv in self._cvars.items(): cv.set(str(self.spell.global_mods.get(n,0)))


# ═══════════════════════════════════════════════════════════════
#  MOD EDITOR DIALOG
# ═══════════════════════════════════════════════════════════════
class ModEditor(tk.Toplevel):
    CATS=["Range","Duration","Area","Power","Casting","Special"]
    def __init__(self,parent,mods,existing,spell,cb):
        super().__init__(parent); self.mods=mods; self.existing=existing; self.spell=spell; self.cb=cb
        self.title("Edit Modifier" if existing else "New Modifier"); self.configure(bg="#0a0a14"); self.grab_set(); self.resizable(False,False)
        bg="#0a0a14"; fg="#c8d0e8"; d=mods.get(existing,{}) if existing else {}
        tk.Label(self,text="Name",bg=bg,fg=fg,font=("Georgia",10,"bold")).pack(pady=(10,0))
        self._nv=tk.StringVar(value=existing or "New Modifier")
        tk.Entry(self,textvariable=self._nv,bg="#111120",fg=fg,insertbackground=fg,relief="solid",bd=1,width=36).pack(padx=12,pady=4)
        rf=tk.Frame(self,bg=bg); rf.pack(fill=tk.X,padx=12,pady=4)
        tk.Label(rf,text="Cat:",bg=bg,fg="#8899bb",font=("Georgia",9)).pack(side=tk.LEFT)
        self._cv=tk.StringVar(value=d.get("cat","Special"))
        ttk.Combobox(rf,textvariable=self._cv,values=self.CATS,state="readonly",width=10).pack(side=tk.LEFT,padx=4)
        tk.Label(rf,text="Cost:",bg=bg,fg="#8899bb",font=("Georgia",9)).pack(side=tk.LEFT,padx=(10,0))
        self._costv=tk.IntVar(value=d.get("cost",1))
        tk.Spinbox(rf,textvariable=self._costv,from_=-5,to=20,width=4,bg="#111120",fg=fg,buttonbackground="#1a1a2e").pack(side=tk.LEFT,padx=4)
        tk.Label(rf,text="Max:",bg=bg,fg="#8899bb",font=("Georgia",9)).pack(side=tk.LEFT,padx=(10,0))
        self._mv=tk.IntVar(value=d.get("max",1))
        tk.Spinbox(rf,textvariable=self._mv,from_=1,to=99,width=4,bg="#111120",fg=fg,buttonbackground="#1a1a2e").pack(side=tk.LEFT,padx=4)
        tk.Label(self,text="Description",bg=bg,fg=fg,font=("Georgia",9)).pack(anchor=tk.W,padx=12)
        self._dt=tk.Text(self,height=4,width=46,bg="#111120",fg=fg,insertbackground=fg,relief="solid",bd=1,font=("Georgia",9),wrap=tk.WORD)
        self._dt.pack(padx=12,pady=4); self._dt.insert("1.0",d.get("desc",""))
        bf=tk.Frame(self,bg=bg); bf.pack(pady=10)
        ttk.Button(bf,text="  Save  ",command=self._save).pack(side=tk.LEFT,padx=6)
        ttk.Button(bf,text="Cancel",command=self.destroy).pack(side=tk.LEFT,padx=6)
        self.geometry("+%d+%d"%(parent.winfo_rootx()+80,parent.winfo_rooty()+80))
    def _save(self):
        nn=self._nv.get().strip()
        if not nn: messagebox.showerror("Error","Name cannot be empty."); return
        if nn!=self.existing and nn in self.mods: messagebox.showerror("Error",f"'{nn}' already exists."); return
        if self.existing and self.existing!=nn:
            cnt=self.spell.global_mods.pop(self.existing,0); self.mods.pop(self.existing); self.spell.global_mods[nn]=cnt
        self.mods[nn]={"cat":self._cv.get(),"cost":int(self._costv.get()),"max":int(self._mv.get()),"desc":self._dt.get("1.0",tk.END).strip()}
        self.cb(); self.destroy()


# ═══════════════════════════════════════════════════════════════
#  CALCULATOR PANEL
# ═══════════════════════════════════════════════════════════════
class CalculatorPanel(tk.Frame):
    def __init__(self,master,spell,**kw):
        kw.setdefault("bg","#0a0a14"); super().__init__(master,**kw)
        self.spell=spell; self._build()
    def _build(self):
        bg="#0a0a14"
        tk.Label(self,text="✦ Point Calculator",bg=bg,fg="#c8a840",font=("Georgia",12,"bold")).pack(pady=(10,4))
        self.pts_lbl=tk.Label(self,text="0",bg=bg,fg="white",font=("Georgia",40,"bold")); self.pts_lbl.pack()
        tk.Label(self,text="total spell points",bg=bg,fg="#445566",font=("Georgia",9,"italic")).pack()
        self.lvl_lbl=tk.Label(self,text="Cantrip",bg=bg,fg="#888888",font=("Georgia",20,"bold")); self.lvl_lbl.pack(pady=(4,0))
        self.prog_var=tk.DoubleVar(value=0)
        ttk.Progressbar(self,variable=self.prog_var,maximum=100,length=320).pack(fill=tk.X,padx=20,pady=6)
        self.next_lbl=tk.Label(self,text="",bg=bg,fg="#445566",font=("Georgia",8)); self.next_lbl.pack()
        tk.Frame(self,bg="#1a2a3a",height=1).pack(fill=tk.X,padx=12,pady=8)
        tk.Label(self,text="Cost Breakdown",bg=bg,fg="#c8a840",font=("Georgia",9,"bold")).pack(anchor=tk.W,padx=12)
        self.bd=tk.Text(self,bg="#070710",fg="#8899aa",relief="flat",font=("Courier",9),state="disabled",height=20,wrap=tk.WORD,padx=8,pady=6)
        self.bd.pack(fill=tk.BOTH,expand=True,padx=6,pady=2)
        tk.Frame(self,bg="#1a2a3a",height=1).pack(fill=tk.X,padx=12,pady=6)
        tk.Label(self,text="Level Thresholds",bg=bg,fg="#c8a840",font=("Georgia",9,"bold")).pack(anchor=tk.W,padx=12)
        self.tbl=tk.Text(self,bg="#070710",fg="#667788",relief="flat",font=("Courier",8),state="disabled",height=15,wrap=tk.NONE,padx=6,pady=4)
        self.tbl.pack(fill=tk.X,padx=6,pady=(0,8))

    def refresh(self):
        s=self.spell; pts=s.total_points; lvl_idx,lvl_name,lvl_col=s.level_info
        self.pts_lbl.configure(text=str(pts)); self.lvl_lbl.configure(text=lvl_name,fg=lvl_col)
        lo,hi=LEVEL_TABLE[lvl_idx][0],LEVEL_TABLE[lvl_idx][1]
        self.prog_var.set(min(100,max(0,((pts-lo)/max(1,hi-lo+1))*100)))
        if lvl_idx<len(LEVEL_TABLE)-1: self.next_lbl.configure(text=f"{LEVEL_TABLE[lvl_idx+1][0]-pts} pts until {LEVEL_TABLE[lvl_idx+1][2]}")
        else: self.next_lbl.configure(text="Maximum level achieved")
        lines=[f"SPELL: {s.name or '(unnamed)'}",""]
        if s.all_schools:
            lines.append("ACTIVE SCHOOLS:")
            for sc in s.all_schools: lines.append(f"  {sc}")
            lines.append("")
        s_sub=0
        for school in s.all_schools:
            ab_dict=s.school_abilities.get(school,{}); active=[(k,v) for k,v in ab_dict.items() if v>0]
            rd=s.ring_mods.get(school,{}); rt=sum(rd.values())
            cap=s.capstone_active(school)
            if active or rt:
                lines.append(f"── {school.upper()} ──{' ⚜ CAPSTONE' if cap else ''}")
                for ab,cnt in active:
                    c2=SCHOOLS[school]["abilities"].get(ab,{}).get("cost",0); sub=c2*cnt; s_sub+=sub
                    lines.append(f"  {ab[:20]:<20} ×{cnt} = +{sub}")
                for grp,cnt in rd.items():
                    if cnt:
                        rune=MOD_RUNES.get(grp,["?"])[0]; rl=SCHOOLS[school].get("ring_mods",{}).get(grp,grp)
                        lines.append(f"  {rune} Ring:{grp:<8} ({rl[:10]}) ×{cnt} = +{cnt}"); s_sub+=cnt
        if s_sub: lines.append(f"  {'school subtotal':<26} +{s_sub}"); lines.append("")
        m_sub=0; active_mods=[(k,v) for k,v in s.global_mods.items() if v>0]
        if active_mods:
            lines.append("── GLOBAL MODIFIERS ──")
            for mod,cnt in active_mods:
                mc=GLOBAL_MODS.get(mod,{}).get("cost",0); sub=mc*cnt; m_sub+=sub
                rune=MOD_RUNES.get(GLOBAL_MODS.get(mod,{}).get("cat","Special"),["?"])[0]
                s2=f"+{sub}" if sub>=0 else str(sub); lines.append(f"  {rune} {mod[:18]:<18} ×{cnt} = {s2}")
            lines.append(f"  {'mod subtotal':<26} {'+' if m_sub>=0 else ''}{m_sub}"); lines.append("")
        e_sub=0; active_elems=[(el,val) for el,val in s.elements.items() if val]
        if active_elems:
            lines.append("── ELEMENTS ──")
            for el,val in active_elems:
                ep=2+(1 if isinstance(val,str) else 0); e_sub+=ep
                sym=ELEMENTS[el]["symbol"]; sub_txt=f" ({val})" if isinstance(val,str) else ""
                lines.append(f"  {sym} {el}{sub_txt:<12} +{ep}")
            lines.append(f"  {'element subtotal':<26} +{e_sub}"); lines.append("")
        db_cnt=len(s.drawback_buys); norm_cnt=s.normal_item_count-db_cnt
        compl_txt="" if s.is_complete else f"  ⚠ INCOMPLETE ({db_cnt} drawbacks vs {norm_cnt} normal)\n"
        lines+=["─"*40,f"  TOTAL                              = {pts}","",
                compl_txt+f"  Schools: {len(s.all_schools)}",
                f"  Synergies: {len(s.active_connections)}",
                f"  Capstones: {sum(1 for sc in s.all_schools if s.capstone_active(sc))}",
                f"  Elements: {sum(1 for v in s.elements.values() if v)}"]
        self.bd.configure(state="normal"); self.bd.delete("1.0",tk.END); self.bd.insert("1.0","\n".join(lines)); self.bd.configure(state="disabled")
        self.tbl.configure(state="normal"); self.tbl.delete("1.0",tk.END)
        for i,(lo2,hi2,name,col) in enumerate(LEVEL_TABLE):
            m=" ◀ HERE" if i==lvl_idx else ""; line=f"  {name:<12} {lo2:>4}–{hi2:<5} pts{m}\n"
            tag=f"lt{i}"; self.tbl.insert("end",line,tag)
            self.tbl.tag_configure(tag,foreground=col if i==lvl_idx else "#445566",font=("Courier",8,"bold") if i==lvl_idx else ("Courier",8))
        self.tbl.configure(state="disabled")


# ═══════════════════════════════════════════════════════════════
#  DRAWBACK PICKER DIALOG
# ═══════════════════════════════════════════════════════════════
class DrawbackPickerDialog(tk.Toplevel):
    BG="#0a0a14"; PBG="#0d0d1a"; TXT="#c8d0e8"; ACC="#3355ff"; GOLD="#c8a840"
    def __init__(self,master,item_name,neg_mods,on_confirm,**kw):
        super().__init__(master,**kw)
        self.title("Choose Drawback"); self.configure(bg=self.BG)
        self.geometry("460x420"); self.resizable(False,False)
        self.transient(master); self.grab_set()
        self._on_confirm=on_confirm; self._sel=tk.StringVar()
        tk.Label(self,text=f"Take '{item_name}' for FREE",bg=self.BG,fg=self.GOLD,
                 font=("Georgia",11,"bold italic")).pack(pady=(12,2))
        tk.Label(self,text="Choose a penalty drawback that will affect this spell:",
                 bg=self.BG,fg=self.TXT,font=("Georgia",9)).pack(pady=(0,8))
        frm=tk.Frame(self,bg=self.PBG,bd=1,relief="solid"); frm.pack(fill=tk.BOTH,expand=True,padx=12,pady=4)
        sb=ttk.Scrollbar(frm,orient="vertical"); sb.pack(side=tk.RIGHT,fill=tk.Y)
        self._lb=tk.Listbox(frm,bg=self.PBG,fg=self.TXT,selectbackground=self.ACC,
                            font=("Georgia",9),relief="flat",activestyle="none",
                            yscrollcommand=sb.set)
        self._lb.pack(side=tk.LEFT,fill=tk.BOTH,expand=True)
        sb.config(command=self._lb.yview)
        for m in neg_mods: self._lb.insert(tk.END, m["name"])
        self._desc=tk.Text(self,height=3,bg=self.PBG,fg="#8899bb",font=("Georgia",8),
                           relief="flat",wrap=tk.WORD,state="disabled",bd=0)
        self._desc.pack(fill=tk.X,padx=12,pady=4)
        self._lb.bind("<<ListboxSelect>>",self._on_sel)
        bf=tk.Frame(self,bg=self.BG); bf.pack(fill=tk.X,padx=12,pady=8)
        ttk.Button(bf,text="✕ Cancel",command=self.destroy,style="D.TButton").pack(side=tk.RIGHT,padx=4)
        ttk.Button(bf,text="✔ Apply Drawback",command=self._confirm).pack(side=tk.RIGHT,padx=4)
        self._neg_mods=neg_mods
    def _on_sel(self,_):
        sel=self._lb.curselection()
        if not sel: return
        m=self._neg_mods[sel[0]]
        self._desc.configure(state="normal"); self._desc.delete("1.0",tk.END)
        self._desc.insert("1.0",m["desc"]); self._desc.configure(state="disabled")
    def _confirm(self):
        sel=self._lb.curselection()
        if not sel: return
        name=self._neg_mods[sel[0]]["name"]
        self.destroy(); self._on_confirm(name)

# ═══════════════════════════════════════════════════════════════
#  NEGATIVE MOD EDITOR
# ═══════════════════════════════════════════════════════════════
class NegativeModEditor(tk.Toplevel):
    BG="#0a0a14"; PBG="#0d0d1a"; TXT="#c8d0e8"; ACC="#3355ff"; GOLD="#c8a840"; EBG="#111120"
    def __init__(self,master,**kw):
        super().__init__(master,**kw)
        self.title("Edit Negative Modifiers"); self.configure(bg=self.BG)
        self.geometry("520x500"); self.transient(master); self.grab_set()
        tk.Label(self,text="Negative Modifier Library",bg=self.BG,fg=self.GOLD,
                 font=("Georgia",12,"bold italic")).pack(pady=(12,4))
        tk.Label(self,text="Right-click or use buttons to add/edit/remove entries.",
                 bg=self.BG,fg="#556677",font=("Georgia",8,"italic")).pack()
        frm=tk.Frame(self,bg=self.PBG,bd=1,relief="solid"); frm.pack(fill=tk.BOTH,expand=True,padx=12,pady=6)
        sb=ttk.Scrollbar(frm,orient="vertical"); sb.pack(side=tk.RIGHT,fill=tk.Y)
        self._lb=tk.Listbox(frm,bg=self.PBG,fg=self.TXT,selectbackground=self.ACC,
                            font=("Courier",9),relief="flat",activestyle="none",
                            yscrollcommand=sb.set)
        self._lb.pack(side=tk.LEFT,fill=tk.BOTH,expand=True)
        sb.config(command=self._lb.yview)
        self._desc=tk.Text(self,height=3,bg=self.PBG,fg="#8899bb",font=("Georgia",8),
                           relief="flat",wrap=tk.WORD,state="disabled",bd=0)
        self._desc.pack(fill=tk.X,padx=12,pady=2)
        bf=tk.Frame(self,bg=self.BG); bf.pack(fill=tk.X,padx=12,pady=8)
        ttk.Button(bf,text="+ Add",command=self._add).pack(side=tk.LEFT,padx=2)
        ttk.Button(bf,text="✎ Edit",command=self._edit).pack(side=tk.LEFT,padx=2)
        ttk.Button(bf,text="✕ Remove",command=self._remove,style="D.TButton").pack(side=tk.LEFT,padx=2)
        ttk.Button(bf,text="Close",command=self.destroy).pack(side=tk.RIGHT,padx=2)
        self._lb.bind("<<ListboxSelect>>",self._on_sel)
        self._refresh()
    def _refresh(self):
        self._lb.delete(0,tk.END)
        for m in NEGATIVE_MODS:
            tag="[default]" if m in DEFAULT_NEGATIVE_MODS else "[custom] "
            self._lb.insert(tk.END,f"{tag} {m['name']}")
    def _on_sel(self,_):
        sel=self._lb.curselection()
        if not sel: return
        m=NEGATIVE_MODS[sel[0]]
        self._desc.configure(state="normal"); self._desc.delete("1.0",tk.END)
        self._desc.insert("1.0",m["desc"]); self._desc.configure(state="disabled")
    def _add(self):
        self._open_form("Add Negative Modifier","","",is_new=True)
    def _edit(self):
        sel=self._lb.curselection()
        if not sel: return
        m=NEGATIVE_MODS[sel[0]]
        self._open_form("Edit Negative Modifier",m["name"],m["desc"],idx=sel[0])
    def _remove(self):
        sel=self._lb.curselection()
        if not sel: return
        m=NEGATIVE_MODS[sel[0]]
        if m in DEFAULT_NEGATIVE_MODS:
            messagebox.showinfo("Cannot Remove","Default negative modifiers cannot be removed."); return
        NEGATIVE_MODS.remove(m); self._refresh()
    def _open_form(self,title,name,desc,is_new=False,idx=None):
        d=tk.Toplevel(self); d.title(title); d.configure(bg=self.BG)
        d.geometry("400x240"); d.transient(self); d.grab_set()
        tk.Label(d,text="Name:",bg=self.BG,fg=self.TXT,font=("Georgia",9)).pack(anchor=tk.W,padx=12,pady=(12,2))
        nv=tk.StringVar(value=name)
        ttk.Entry(d,textvariable=nv,width=40).pack(fill=tk.X,padx=12)
        tk.Label(d,text="Description:",bg=self.BG,fg=self.TXT,font=("Georgia",9)).pack(anchor=tk.W,padx=12,pady=(8,2))
        dt=tk.Text(d,height=4,bg=self.EBG,fg=self.TXT,insertbackground=self.TXT,
                   relief="solid",bd=1,font=("Georgia",9),wrap=tk.WORD)
        dt.pack(fill=tk.X,padx=12); dt.insert("1.0",desc)
        def _save():
            n2=nv.get().strip(); d2=dt.get("1.0",tk.END).strip()
            if not n2: return
            if is_new: NEGATIVE_MODS.append({"name":n2,"desc":d2})
            elif idx is not None: NEGATIVE_MODS[idx]={"name":n2,"desc":d2}
            d.destroy(); self._refresh()
        bf=tk.Frame(d,bg=self.BG); bf.pack(fill=tk.X,padx=12,pady=8)
        ttk.Button(bf,text="Save",command=_save).pack(side=tk.RIGHT,padx=4)
        ttk.Button(bf,text="Cancel",command=d.destroy,style="D.TButton").pack(side=tk.RIGHT,padx=4)

# ═══════════════════════════════════════════════════════════════
#  IF-THEN CONDITION EDITOR
# ═══════════════════════════════════════════════════════════════
class IfThenEditor(tk.Toplevel):
    BG="#0a0a14"; PBG="#0d0d1a"; TXT="#c8d0e8"; ACC="#3355ff"; GOLD="#c8a840"; EBG="#111120"

    def __init__(self, master, spell, cond=None, on_save=None, depth=0, **kw):
        super().__init__(master, **kw)
        self._spell = spell; self._on_save = on_save; self._depth = depth
        self._cond = cond or {}
        title = ("Chain: " if depth > 0 else "") + ("Edit" if cond else "New") + " If-Then Condition"
        self.title(title); self.configure(bg=self.BG)
        self.geometry("560x600"); self.transient(master); self.grab_set()

        tk.Label(self, text="If-Then Condition",
                 bg=self.BG, fg=self.GOLD,
                 font=("Georgia", 11, "bold italic")).pack(pady=(8, 2))

        # Name
        nf = tk.Frame(self, bg=self.BG); nf.pack(fill=tk.X, padx=10, pady=2)
        tk.Label(nf, text="Name / Label:", bg=self.BG, fg=self.TXT,
                 font=("Georgia", 9)).pack(anchor=tk.W)
        self._nv = tk.StringVar(value=self._cond.get("name", ""))
        ttk.Entry(nf, textvariable=self._nv, width=50).pack(fill=tk.X)

        # ── Notebook ──────────────────────────────────────────────
        nb = ttk.Notebook(self); nb.pack(fill=tk.BOTH, expand=True, padx=10, pady=4)

        # Tab 1: IF + THEN schools
        t1 = tk.Frame(nb, bg=self.PBG); nb.add(t1, text="  IF / THEN  ")
        self._build_if_then_tab(t1)

        # Tab 2: THEN Mods & Abilities
        t2 = tk.Frame(nb, bg=self.PBG); nb.add(t2, text="  Mods & Abilities  ")
        self._build_mods_tab(t2)

        # Tab 3: Chain
        t3 = tk.Frame(nb, bg=self.PBG); nb.add(t3, text="  Chain  ")
        self._build_chain_tab(t3)

        # Buttons
        bf = tk.Frame(self, bg=self.BG); bf.pack(fill=tk.X, padx=10, pady=6)
        ttk.Button(bf, text="Save", command=self._save).pack(side=tk.RIGHT, padx=4)
        ttk.Button(bf, text="Cancel", command=self.destroy,
                   style="D.TButton").pack(side=tk.RIGHT, padx=4)

    def _build_if_then_tab(self, f):
        # IF schools
        tk.Label(f, text="IF  — schools that must be active:",
                 bg=self.PBG, fg=self.GOLD, font=("Georgia", 9, "bold")).pack(anchor=tk.W, padx=8, pady=(8,0))
        tk.Label(f, text="Hold Ctrl to select multiple.",
                 bg=self.PBG, fg="#445566", font=("Georgia", 7, "italic")).pack(anchor=tk.W, padx=8)
        if_frm = tk.Frame(f, bg=self.EBG); if_frm.pack(fill=tk.X, padx=8, pady=4)
        if_sb = ttk.Scrollbar(if_frm, orient="vertical"); if_sb.pack(side=tk.RIGHT, fill=tk.Y)
        self._if_lb = tk.Listbox(if_frm, bg=self.EBG, fg=self.TXT, selectbackground=self.ACC,
                                  font=("Courier", 9), relief="flat", activestyle="none",
                                  selectmode=tk.MULTIPLE, height=5, yscrollcommand=if_sb.set)
        self._if_lb.pack(side=tk.LEFT, fill=tk.X, expand=True)
        if_sb.config(command=self._if_lb.yview)
        self._sc_keys = list(SCHOOLS.keys())
        active_sc = set(self._spell.all_schools)
        for sc in self._sc_keys:
            self._if_lb.insert(tk.END, f"{'◉' if sc in active_sc else '○'}  {sc}")
        for sc in self._cond.get("if_schools", []):
            if sc in self._sc_keys:
                self._if_lb.selection_set(self._sc_keys.index(sc))

        # THEN schools
        tk.Label(f, text="THEN  — resulting schools / effects:",
                 bg=self.PBG, fg="#60dd88", font=("Georgia", 9, "bold")).pack(anchor=tk.W, padx=8, pady=(8,0))
        then_frm = tk.Frame(f, bg=self.EBG); then_frm.pack(fill=tk.X, padx=8, pady=4)
        then_sb = ttk.Scrollbar(then_frm, orient="vertical"); then_sb.pack(side=tk.RIGHT, fill=tk.Y)
        self._then_sc_lb = tk.Listbox(then_frm, bg=self.EBG, fg=self.TXT, selectbackground="#226644",
                                      font=("Courier", 9), relief="flat", activestyle="none",
                                      selectmode=tk.MULTIPLE, height=5, yscrollcommand=then_sb.set)
        self._then_sc_lb.pack(side=tk.LEFT, fill=tk.X, expand=True)
        then_sb.config(command=self._then_sc_lb.yview)
        for sc in self._sc_keys:
            self._then_sc_lb.insert(tk.END, f"{'◉' if sc in active_sc else '○'}  {sc}")
        for sc in self._cond.get("then_schools", []):
            if sc in self._sc_keys:
                self._then_sc_lb.selection_set(self._sc_keys.index(sc))

        # THEN free text
        tk.Label(f, text="THEN  — effect description (optional):",
                 bg=self.PBG, fg="#60dd88", font=("Georgia", 9, "bold")).pack(anchor=tk.W, padx=8, pady=(8,0))
        self._then_t = tk.Text(f, height=3, bg=self.EBG, fg=self.TXT,
                               insertbackground=self.TXT, relief="solid", bd=1,
                               font=("Georgia", 9), wrap=tk.WORD)
        self._then_t.pack(fill=tk.X, padx=8, pady=(0,4))
        self._then_t.insert("1.0", self._cond.get("then_text", ""))

    def _build_mods_tab(self, f):
        # THEN modifiers
        tk.Label(f, text="THEN  — global modifiers associated with this condition:",
                 bg=self.PBG, fg="#60dd88", font=("Georgia", 9, "bold")).pack(anchor=tk.W, padx=8, pady=(8,0))
        mf = tk.Frame(f, bg=self.EBG); mf.pack(fill=tk.X, padx=8, pady=4)
        m_sb = ttk.Scrollbar(mf, orient="vertical"); m_sb.pack(side=tk.RIGHT, fill=tk.Y)
        self._mod_lb = tk.Listbox(mf, bg=self.EBG, fg=self.TXT, selectbackground="#226644",
                                   font=("Courier", 8), relief="flat", activestyle="none",
                                   selectmode=tk.MULTIPLE, height=6, yscrollcommand=m_sb.set)
        self._mod_lb.pack(side=tk.LEFT, fill=tk.X, expand=True)
        m_sb.config(command=self._mod_lb.yview)
        self._mod_keys = list(GLOBAL_MODS.keys())
        then_mods = set(self._cond.get("then_modifiers", []))
        for mn in self._mod_keys:
            cat = GLOBAL_MODS[mn].get("cat","")
            self._mod_lb.insert(tk.END, f"[{cat[:4]}] {mn}")
            if mn in then_mods:
                self._mod_lb.selection_set(self._mod_keys.index(mn))

        # THEN abilities
        tk.Label(f, text="THEN  — school abilities associated with this condition:",
                 bg=self.PBG, fg="#60dd88", font=("Georgia", 9, "bold")).pack(anchor=tk.W, padx=8, pady=(8,0))
        af = tk.Frame(f, bg=self.EBG); af.pack(fill=tk.BOTH, expand=True, padx=8, pady=4)
        a_sb = ttk.Scrollbar(af, orient="vertical"); a_sb.pack(side=tk.RIGHT, fill=tk.Y)
        self._ab_lb = tk.Listbox(af, bg=self.EBG, fg=self.TXT, selectbackground="#226644",
                                   font=("Courier", 8), relief="flat", activestyle="none",
                                   selectmode=tk.MULTIPLE, height=8, yscrollcommand=a_sb.set)
        self._ab_lb.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        a_sb.config(command=self._ab_lb.yview)
        self._ab_keys = []  # list of (school, ability_name)
        then_abs = self._cond.get("then_abilities", {})
        for sc in SCHOOLS:
            for abn in SCHOOLS[sc].get("abilities", {}):
                self._ab_keys.append((sc, abn))
                self._ab_lb.insert(tk.END, f"{SCHOOLS[sc]['symbol']} {sc[:8]}: {abn}")
                if abn in then_abs.get(sc, []):
                    self._ab_lb.selection_set(len(self._ab_keys) - 1)
        a_sb.config(command=self._ab_lb.yview)

    def _build_chain_tab(self, f):
        tk.Label(f, text="Chained conditions — fire after this one resolves:",
                 bg=self.PBG, fg="#aaddff", font=("Georgia", 9, "bold")).pack(anchor=tk.W, padx=8, pady=(8,0))
        tk.Label(f, text="Chains inherit the context of the parent condition.",
                 bg=self.PBG, fg="#445566", font=("Georgia", 7, "italic")).pack(anchor=tk.W, padx=8)
        cf = tk.Frame(f, bg=self.EBG); cf.pack(fill=tk.BOTH, expand=True, padx=8, pady=4)
        c_sb = ttk.Scrollbar(cf, orient="vertical"); c_sb.pack(side=tk.RIGHT, fill=tk.Y)
        self._chain_lb = tk.Listbox(cf, bg=self.EBG, fg=self.TXT, selectbackground=self.ACC,
                                     font=("Courier", 9), relief="flat", activestyle="none",
                                     height=8, yscrollcommand=c_sb.set)
        self._chain_lb.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        c_sb.config(command=self._chain_lb.yview)
        self._chain_data = list(self._cond.get("chain", []))
        self._refresh_chain_lb()
        bf = tk.Frame(f, bg=self.PBG); bf.pack(fill=tk.X, padx=8, pady=4)
        ttk.Button(bf, text="+ Add Chain", command=self._chain_add).pack(side=tk.LEFT, padx=2)
        ttk.Button(bf, text="✎ Edit", command=self._chain_edit).pack(side=tk.LEFT, padx=2)
        ttk.Button(bf, text="✕ Remove", command=self._chain_remove,
                   style="D.TButton").pack(side=tk.LEFT, padx=2)

    def _refresh_chain_lb(self):
        self._chain_lb.delete(0, tk.END)
        for c in self._chain_data:
            ifs = ', '.join(c.get('if_schools', []) or ['—'])
            thens = ', '.join(c.get('then_schools', []) or [])
            nm = c.get('name', '')
            label = f"IF {ifs}"
            if thens: label += f"  → {thens}"
            if nm:    label += f"  [{nm}]"
            self._chain_lb.insert(tk.END, label)

    def _chain_add(self):
        IfThenEditor(self, self._spell, depth=self._depth + 1,
                     on_save=lambda c: (self._chain_data.append(c), self._refresh_chain_lb()))

    def _chain_edit(self):
        sel = self._chain_lb.curselection()
        if not sel or sel[0] >= len(self._chain_data): return
        idx = sel[0]; existing = self._chain_data[idx]
        def _upd(c, i=idx):
            self._chain_data[i] = c; self._refresh_chain_lb()
        IfThenEditor(self, self._spell, cond=existing, depth=self._depth + 1, on_save=_upd)

    def _chain_remove(self):
        sel = self._chain_lb.curselection()
        if not sel or sel[0] >= len(self._chain_data): return
        del self._chain_data[sel[0]]; self._refresh_chain_lb()

    def _save(self):
        if_schools  = [self._sc_keys[i] for i in self._if_lb.curselection()]
        then_schools = [self._sc_keys[i] for i in self._then_sc_lb.curselection()]
        then_mods   = [self._mod_keys[i] for i in self._mod_lb.curselection()]
        # Build then_abilities dict
        then_abs = {}
        for i in self._ab_lb.curselection():
            sc, abn = self._ab_keys[i]
            then_abs.setdefault(sc, []).append(abn)
        cond = {
            "name":           self._nv.get().strip(),
            "if_schools":     if_schools,
            "then_schools":   then_schools,
            "then_modifiers": then_mods,
            "then_abilities": then_abs,
            "then_text":      self._then_t.get("1.0", tk.END).strip(),
            "chain":          self._chain_data,
        }
        if self._on_save: self._on_save(cond)
        self.destroy()

# ═══════════════════════════════════════════════════════════════
#  WHEN-THEN CONDITION EDITOR
# ═══════════════════════════════════════════════════════════════
class WhenThenEditor(tk.Toplevel):
    BG="#0a0a14"; PBG="#0d0d1a"; TXT="#c8d0e8"; ACC="#3355ff"; GOLD="#c8a840"; EBG="#111120"

    def __init__(self, master, cond=None, on_save=None, **kw):
        super().__init__(master, **kw)
        self._on_save = on_save
        self.title("Edit When-Then" if cond else "New When-Then")
        self.configure(bg=self.BG); self.geometry("440x360")
        self.transient(master); self.grab_set()

        tk.Label(self, text="When-Then Condition",
                 bg=self.BG, fg=self.GOLD,
                 font=("Georgia", 11, "bold italic")).pack(pady=(10, 4))
        tk.Label(self, text="Free-form trigger and effect. Displayed as a rune on the core ring.",
                 bg=self.BG, fg="#445566", font=("Georgia", 8, "italic")).pack()

        # WHEN
        wf = tk.Frame(self, bg=self.BG); wf.pack(fill=tk.X, padx=12, pady=(8,2))
        tk.Label(wf, text="WHEN  (trigger / condition):",
                 bg=self.BG, fg="#aaddff", font=("Georgia", 9, "bold")).pack(anchor=tk.W)
        self._when_t = tk.Text(wf, height=4, bg=self.EBG, fg=self.TXT,
                               insertbackground=self.TXT, relief="solid", bd=1,
                               font=("Georgia", 9), wrap=tk.WORD)
        self._when_t.pack(fill=tk.X)
        if cond: self._when_t.insert("1.0", cond.get("when_text", ""))

        # THEN
        tf = tk.Frame(self, bg=self.BG); tf.pack(fill=tk.BOTH, expand=True, padx=12, pady=(8,2))
        tk.Label(tf, text="THEN  (effect / result):",
                 bg=self.BG, fg="#60dd88", font=("Georgia", 9, "bold")).pack(anchor=tk.W)
        self._then_t = tk.Text(tf, height=4, bg=self.EBG, fg=self.TXT,
                               insertbackground=self.TXT, relief="solid", bd=1,
                               font=("Georgia", 9), wrap=tk.WORD)
        self._then_t.pack(fill=tk.BOTH, expand=True)
        if cond: self._then_t.insert("1.0", cond.get("then_text", ""))

        # Buttons
        bf = tk.Frame(self, bg=self.BG); bf.pack(fill=tk.X, padx=12, pady=8)
        ttk.Button(bf, text="Save", command=self._save).pack(side=tk.RIGHT, padx=4)
        ttk.Button(bf, text="Cancel", command=self.destroy,
                   style="D.TButton").pack(side=tk.RIGHT, padx=4)

    def _save(self):
        cond = {"when_text": self._when_t.get("1.0", tk.END).strip(),
                "then_text": self._then_t.get("1.0", tk.END).strip()}
        if self._on_save: self._on_save(cond)
        self.destroy()

# ═══════════════════════════════════════════════════════════════
#  MAIN APPLICATION
# ═══════════════════════════════════════════════════════════════
class SpellForgeApp(tk.Tk):
    BG="#0a0a14"; PBG="#0d0d1a"; ACC="#3355ff"; TXT="#c8d0e8"; GOLD="#c8a840"; SUB="#445566"; EBG="#111120"

    def __init__(self):
        super().__init__()
        self.title("✦ SpellForge v5.0 — Magic Circle Designer ✦")
        self.configure(bg=self.BG); self.geometry("1680x980"); self.minsize(1200,720)
        self.spell=Spell(); self.library:List[Spell]=[]
        self._styles(); self._build(); self._refresh()

    def _styles(self):
        s=ttk.Style(self); s.theme_use("clam")
        s.configure(".",background=self.BG,foreground=self.TXT,fieldbackground=self.EBG,troughcolor=self.PBG,selectbackground=self.ACC,selectforeground="white",borderwidth=0,relief="flat")
        for n,kw in [("TFrame",{"background":self.BG}),("TLabel",{"background":self.BG,"foreground":self.TXT,"font":("Georgia",10)}),
                     ("TLabelframe",{"background":self.PBG,"foreground":self.GOLD,"font":("Georgia",10,"bold"),"borderwidth":1,"relief":"solid"}),
                     ("TLabelframe.Label",{"background":self.PBG,"foreground":self.GOLD,"font":("Georgia",10,"bold")}),
                     ("TButton",{"background":self.ACC,"foreground":"white","font":("Georgia",9,"bold"),"padding":"8 4","relief":"flat"}),
                     ("TEntry",{"fieldbackground":self.EBG,"foreground":self.TXT,"insertcolor":self.TXT,"borderwidth":1,"relief":"solid"}),
                     ("TCombobox",{"fieldbackground":self.EBG,"foreground":self.TXT}),
                     ("TScrollbar",{"background":self.PBG,"troughcolor":self.BG,"arrowcolor":self.SUB,"width":8}),
                     ("TNotebook",{"background":self.BG,"tabmargins":"2 4 0 0"}),
                     ("TNotebook.Tab",{"background":self.PBG,"foreground":self.SUB,"font":("Georgia",9),"padding":"10 4","borderwidth":0}),
                     ("TScale",{"background":self.BG,"troughcolor":self.EBG})]:
            s.configure(n,**kw)
        s.map("TButton",background=[("active","#4466ff"),("pressed","#2233cc")])
        s.configure("D.TButton",background="#882222"); s.map("D.TButton",background=[("active","#aa3333")])
        s.configure("G.TButton",background="#5a4008"); s.map("G.TButton",background=[("active","#7a5510")])
        s.map("TCombobox",fieldbackground=[("readonly",self.EBG)],foreground=[("readonly",self.TXT)])
        s.map("TNotebook.Tab",background=[("selected",self.BG)],foreground=[("selected",self.GOLD)])

    def _sf(self,parent):
        outer=tk.Frame(parent,bg=self.BG)
        c=tk.Canvas(outer,bg=self.BG,highlightthickness=0); sb=ttk.Scrollbar(outer,orient="vertical",command=c.yview)
        c.configure(yscrollcommand=sb.set); sb.pack(side=tk.RIGHT,fill=tk.Y); c.pack(side=tk.LEFT,fill=tk.BOTH,expand=True)
        inner=tk.Frame(c,bg=self.BG); c.create_window((0,0),window=inner,anchor="nw")
        inner.bind("<Configure>",lambda e,cv=c:cv.configure(scrollregion=cv.bbox("all")))
        c.bind("<MouseWheel>",lambda e,cv=c:cv.yview_scroll(int(-1*(e.delta/120)),"units"))
        return {"outer":outer,"inner":inner}

    def _build(self):
        bar=tk.Frame(self,bg=self.PBG,height=46); bar.pack(fill=tk.X)
        tk.Label(bar,text="✦ SpellForge v5",bg=self.PBG,fg=self.GOLD,font=("Georgia",15,"bold italic")).pack(side=tk.LEFT,padx=14,pady=8)
        tk.Label(bar,text="Magic Circle Spell Construction System",bg=self.PBG,fg=self.SUB,font=("Georgia",9,"italic")).pack(side=tk.LEFT,pady=8)
        for lbl,cmd,st in [("🗑 New",self._new,"D.TButton"),("📋 Text",self._etxt,"G.TButton"),("🖼 PNG",self._epng,"G.TButton"),("📂 Load",self._load,"TButton"),("💾 Save",self._save,"TButton")]:
            ttk.Button(bar,text=lbl,command=cmd,style=st).pack(side=tk.RIGHT,padx=4,pady=8)
        self.pane=DragPane(self); self.pane.pack(fill=tk.BOTH,expand=True)
        self._build_left(self.pane.left)
        cf=tk.Frame(self.pane.center,bg=self.BG); cf.pack(fill=tk.BOTH,expand=True)
        zbar=tk.Frame(cf,bg=self.PBG,height=32); zbar.pack(fill=tk.X)
        tk.Label(zbar,text="Zoom:",bg=self.PBG,fg=self.SUB,font=("Georgia",9)).pack(side=tk.LEFT,padx=8,pady=4)
        for lbl,cmd in [("−",lambda:self.circle.zoom_out()),("Reset",self.circle_reset),("+",lambda:self.circle.zoom_in())]:
            ttk.Button(zbar,text=lbl,command=cmd,width=6).pack(side=tk.LEFT,padx=2,pady=4)
        tk.Label(zbar,text="  Scroll=zoom on cursor  ·  Right-drag=pan",bg=self.PBG,fg=self.SUB,font=("Georgia",8,"italic")).pack(side=tk.LEFT,padx=8)
        self.circle=MagicCircleCanvas(cf); self.circle.pack(fill=tk.BOTH,expand=True,padx=2,pady=2)
        self.circle._on_change=self._refresh
        self._build_right(self.pane.right)

    def circle_reset(self): self.circle.zoom_reset()

    def _build_left(self,parent):
        nb=ttk.Notebook(parent); nb.pack(fill=tk.BOTH,expand=True); self._lnb=nb
        id_sc=self._sf(nb); nb.add(id_sc["outer"],text="  Identity  ")
        self._build_identity(id_sc["inner"])
        # Elements tab
        el_sc=self._sf(nb); nb.add(el_sc["outer"],text="  🜂 Elements  ")
        self.el_panel=ElementPanel(el_sc["inner"],self.spell,on_change=self._refresh)
        self.el_panel.pack(fill=tk.BOTH,expand=True)
        self._spanels:Dict[str,SchoolAbilityPanel]={}
        for school in SCHOOLS:
            sc=self._sf(nb); nb.add(sc["outer"],text=f" {SCHOOLS[school]['symbol']} ")
            p=SchoolAbilityPanel(sc["inner"],school,self.spell,on_change=self._refresh); p.pack(fill=tk.BOTH,expand=True)
            self._spanels[school]=p

    def _build_identity(self,f):
        pb=self.PBG
        frm1=ttk.LabelFrame(f,text="  Spell Identity  ",padding=8); frm1.pack(fill=tk.X,padx=6,pady=6)
        ttk.Label(frm1,text="Name").pack(anchor=tk.W)
        self.name_var=tk.StringVar(value="Unnamed Spell")
        ttk.Entry(frm1,textvariable=self.name_var).pack(fill=tk.X,pady=(0,6))
        self.name_var.trace_add("write",lambda *_:self._on_name())
        ttk.Label(frm1,text="Description").pack(anchor=tk.W)
        self.desc_text=tk.Text(frm1,height=4,bg=self.EBG,fg=self.TXT,insertbackground=self.TXT,relief="solid",borderwidth=1,font=("Georgia",9),wrap=tk.WORD)
        self.desc_text.pack(fill=tk.X)
        self.desc_text.bind("<KeyRelease>",lambda _:setattr(self.spell,"description",self.desc_text.get("1.0",tk.END).strip()))
        frm4=ttk.LabelFrame(f,text="  Custom Effects (free)  ",padding=8); frm4.pack(fill=tk.X,padx=6,pady=4)
        self.ce=ttk.Entry(frm4); self.ce.pack(fill=tk.X,pady=2)
        ttk.Button(frm4,text="+ Add",command=self._addce).pack(anchor=tk.W,pady=2)
        self.cl=tk.Listbox(frm4,height=4,bg=self.EBG,fg=self.TXT,selectbackground=self.ACC,relief="solid",borderwidth=1,font=("Georgia",9))
        self.cl.pack(fill=tk.X,pady=2)
        ttk.Button(frm4,text="✕ Remove",command=self._rmce,style="D.TButton").pack(anchor=tk.W)
        # ── If-Then Conditions ───────────────────────────────────────
        frm5=ttk.LabelFrame(f,text="  If-Then Conditions  ",padding=4)
        frm5.pack(fill=tk.X,padx=6,pady=4)
        self._ifthen_lb=tk.Listbox(frm5,height=3,bg=self.EBG,fg=self.TXT,
                                   selectbackground=self.ACC,relief="flat",
                                   font=("Courier",8),activestyle="none")
        self._ifthen_lb.pack(fill=tk.X,pady=(0,2))
        it_bf=tk.Frame(frm5,bg=self.BG); it_bf.pack(fill=tk.X)
        ttk.Button(it_bf,text="+ Add",command=self._it_add).pack(side=tk.LEFT,padx=2)
        ttk.Button(it_bf,text="✎ Edit",command=self._it_edit).pack(side=tk.LEFT,padx=2)
        ttk.Button(it_bf,text="✕ Remove",command=self._it_remove,
                   style="D.TButton").pack(side=tk.LEFT,padx=2)
        # ── When-Then Conditions ─────────────────────────────────────
        frm6=ttk.LabelFrame(f,text="  When-Then Conditions  ",padding=4)
        frm6.pack(fill=tk.X,padx=6,pady=4)
        self._wt_lb=tk.Listbox(frm6,height=3,bg=self.EBG,fg=self.TXT,
                                selectbackground=self.ACC,relief="flat",
                                font=("Courier",8),activestyle="none")
        self._wt_lb.pack(fill=tk.X,pady=(0,2))
        wt_bf=tk.Frame(frm6,bg=self.BG); wt_bf.pack(fill=tk.X)
        ttk.Button(wt_bf,text="+ Add",command=self._wt_add).pack(side=tk.LEFT,padx=2)
        ttk.Button(wt_bf,text="✎ Edit",command=self._wt_edit).pack(side=tk.LEFT,padx=2)
        ttk.Button(wt_bf,text="✕ Remove",command=self._wt_remove,
                   style="D.TButton").pack(side=tk.LEFT,padx=2)
        # ── Spell Effects (live output) ──────────────────────────────
        lfe=ttk.LabelFrame(f,text="  Spell Effects  ",padding=4)
        lfe.pack(fill=tk.BOTH,expand=True,padx=6,pady=(4,6))
        eff_sb=ttk.Scrollbar(lfe,orient="vertical"); eff_sb.pack(side=tk.RIGHT,fill=tk.Y)
        self._effects_box=tk.Text(lfe,bg=self.EBG,fg=self.TXT,relief="flat",
                                  font=("Courier",8),state="disabled",wrap=tk.WORD,
                                  padx=6,pady=4,yscrollcommand=eff_sb.set)
        self._effects_box.pack(fill=tk.BOTH,expand=True)
        eff_sb.config(command=self._effects_box.yview)

    def _build_right(self,parent):
        nb=ttk.Notebook(parent); nb.pack(fill=tk.BOTH,expand=True)
        csc=self._sf(nb); nb.add(csc["outer"],text="  ✦ Calculator  ")
        self.calc=CalculatorPanel(csc["inner"],self.spell); self.calc.pack(fill=tk.BOTH,expand=True)
        msc=self._sf(nb); nb.add(msc["outer"],text="  Modifiers  ")
        self.mp=GlobalModPanel(msc["inner"],self.spell,on_change=self._refresh); self.mp.pack(fill=tk.BOTH,expand=True)
        syn=tk.Frame(nb,bg=self.BG); nb.add(syn,text="  Synergies  ")
        tk.Label(syn,text="Active Synergies",bg=self.BG,fg=self.GOLD,font=("Georgia",10,"bold italic")).pack(pady=8)
        self.ct=tk.Text(syn,bg=self.PBG,fg=self.TXT,relief="flat",font=("Georgia",9),state="disabled",wrap=tk.WORD,padx=8,pady=6)
        self.ct.pack(fill=tk.BOTH,expand=True,padx=4,pady=4)
        gsc=self._sf(nb); nb.add(gsc["outer"],text="  Guide  ")
        gt=tk.Text(gsc["inner"],bg=self.PBG,fg="#778899",relief="flat",font=("Courier",8),state="normal",wrap=tk.WORD,padx=8,pady=6)
        gt.pack(fill=tk.BOTH,expand=True,padx=4,pady=4)
        gt.insert("end","SYNERGY CONNECTIONS\n\n","h"); gt.tag_configure("h",foreground=self.GOLD,font=("Courier",9,"bold"))
        for (a,b),name in SCHOOL_CONNECTIONS.items():
            gt.insert("end",f"{a} + {b}\n","pair"); gt.insert("end",f"  → {name}\n\n")
            gt.tag_configure("pair",foreground=self.GOLD,font=("Courier",8,"bold"))
        gt.insert("end","ELEMENTS\n\n","h")
        for el,edata in ELEMENTS.items():
            gt.insert("end",f"{edata['symbol']} {el}\n","epair"); gt.insert("end",f"  {edata['desc']}\n  Effect: {edata['modification']}\n\n")
            gt.tag_configure("epair",foreground=edata["color"],font=("Courier",8,"bold"))
        gt.configure(state="disabled")
        lib=tk.Frame(nb,bg=self.BG); nb.add(lib,text="  Library  ")
        tk.Label(lib,text="Spell Library",bg=self.BG,fg=self.GOLD,font=("Georgia",10,"bold italic")).pack(pady=6)
        self.ll=tk.Listbox(lib,bg=self.PBG,fg=self.TXT,selectbackground=self.ACC,relief="flat",font=("Georgia",9),activestyle="none")
        self.ll.pack(fill=tk.BOTH,expand=True,padx=4,pady=2)
        br=tk.Frame(lib,bg=self.BG); br.pack(fill=tk.X,padx=4,pady=4)
        ttk.Button(br,text="+ Add",command=self._ladd,style="G.TButton").pack(side=tk.LEFT,padx=2)
        ttk.Button(br,text="📖 Load",command=self._lload).pack(side=tk.LEFT,padx=2)
        ttk.Button(br,text="🗑",command=self._ldel,style="D.TButton").pack(side=tk.LEFT,padx=2)
        dsc=self._sf(nb); nb.add(dsc["outer"],text="  Drawbacks  ")
        self._build_drawbacks_tab(dsc["inner"])

    def _build_drawbacks_tab(self,f):
        # ── Drawbacks Taken ──
        hf=tk.Frame(f,bg=self.BG); hf.pack(fill=tk.X,padx=6,pady=(6,2))
        tk.Label(hf,text="Active Drawbacks",bg=self.BG,fg=self.GOLD,
                 font=("Georgia",10,"bold italic")).pack(side=tk.LEFT)
        lf=ttk.LabelFrame(f,text="  Drawbacks Taken  ",padding=4)
        lf.pack(fill=tk.X,padx=6,pady=2)
        self._db_list=tk.Text(lf,bg=self.EBG,fg=self.TXT,relief="flat",
                              font=("Courier",8),state="disabled",wrap=tk.WORD,
                              padx=4,pady=4,height=5)
        self._db_list.pack(fill=tk.BOTH,expand=True)
        tk.Label(f,text="Right-click any ability/modifier on the circle to take it free with a penalty.",
                 bg=self.BG,fg="#445566",font=("Georgia",7,"italic"),justify="left").pack(anchor=tk.W,padx=8,pady=(0,4))
        # ── Negative Modifier Library ──
        lf2=ttk.LabelFrame(f,text="  Negative Modifier Library  ",padding=4)
        lf2.pack(fill=tk.BOTH,expand=True,padx=6,pady=(2,4))
        nmf=tk.Frame(lf2,bg=self.PBG); nmf.pack(fill=tk.BOTH,expand=True)
        nm_sb=ttk.Scrollbar(nmf,orient="vertical"); nm_sb.pack(side=tk.RIGHT,fill=tk.Y)
        self._nm_lb=tk.Listbox(nmf,bg=self.PBG,fg=self.TXT,selectbackground=self.ACC,
                               font=("Courier",8),relief="flat",activestyle="none",
                               height=8,yscrollcommand=nm_sb.set)
        self._nm_lb.pack(side=tk.LEFT,fill=tk.BOTH,expand=True)
        nm_sb.config(command=self._nm_lb.yview)
        self._nm_lb.bind("<<ListboxSelect>>",self._nm_on_sel)
        self._nm_desc=tk.Text(lf2,height=2,bg=self.PBG,fg="#8899bb",font=("Georgia",8),
                              relief="flat",wrap=tk.WORD,state="disabled",bd=0)
        self._nm_desc.pack(fill=tk.X,pady=(2,0))
        nbf=tk.Frame(lf2,bg=self.BG); nbf.pack(fill=tk.X,pady=(4,0))
        ttk.Button(nbf,text="+ Add",command=self._nm_add).pack(side=tk.LEFT,padx=2)
        ttk.Button(nbf,text="✎ Edit",command=self._nm_edit).pack(side=tk.LEFT,padx=2)
        ttk.Button(nbf,text="✕ Remove",command=self._nm_remove,style="D.TButton").pack(side=tk.LEFT,padx=2)
        self._refresh_neg_mods()

    def _refresh_drawbacks(self):
        if not hasattr(self,'_db_list'): return
        self._db_list.configure(state="normal"); self._db_list.delete("1.0",tk.END)
        db=self.spell.drawback_buys
        if not db:
            self._db_list.insert("1.0","No drawbacks taken yet.\n\nRight-click an ability or\nmodifier on the circle to\ntake it free with a penalty.")
        else:
            for key,neg_name in db.items():
                parts=key.split("/",1); pretty=parts[1] if len(parts)>1 else key
                self._db_list.insert(tk.END,f"◌  {pretty}\n")
                self._db_list.insert(tk.END,f"   → {neg_name}\n\n")
        self._db_list.configure(state="disabled")

    def _refresh_effects(self):
        if not hasattr(self,'_effects_box'): return
        s=self.spell; t=self._effects_box
        t.configure(state="normal"); t.delete("1.0",tk.END)
        # ── configure tags (done first so they exist before any insert) ──
        _,lvl_name,lvl_col=s.level_info
        t.tag_configure("title",foreground="#e8d8a0",font=("Georgia",10,"bold italic"))
        t.tag_configure("lvl",foreground=lvl_col,font=("Georgia",9,"italic"))
        t.tag_configure("hdr",foreground="#c8a840",font=("Georgia",9,"bold"))
        t.tag_configure("ability",foreground="#aabbcc",font=("Courier",8))
        t.tag_configure("ringmod",foreground="#99aacc",font=("Courier",8))
        t.tag_configure("db_item",foreground="#cc8888",font=("Courier",8))
        t.tag_configure("db_key",foreground="#cc8888",font=("Courier",8))
        t.tag_configure("db_name",foreground="#ff9999",font=("Courier",8,"bold"))
        t.tag_configure("db_desc",foreground="#aa6666",font=("Courier",8,"italic"))
        t.tag_configure("capstone",foreground="#FFD700",font=("Georgia",8,"bold"))
        t.tag_configure("cap_desc",foreground="#ccaa44",font=("Georgia",8,"italic"))
        t.tag_configure("eff",foreground="#778899",font=("Georgia",8,"italic"))
        t.tag_configure("node",foreground="#88aacc",font=("Courier",8))
        t.tag_configure("sub_el",foreground="#88bbaa",font=("Courier",8))
        t.tag_configure("special",foreground="#88ffff",font=("Courier",8))
        t.tag_configure("custom",foreground="#aaddbb",font=("Georgia",8,"italic"))
        t.tag_configure("empty",foreground="#445566",font=("Georgia",9,"italic"))
        for cat,col in [("range","#ff6060"),("duration","#60ee88"),("area","#6088ff"),
                         ("power","#ffd060"),("casting","#ff88ff")]:
            t.tag_configure(f"c_{cat}",foreground=col,font=("Courier",8))
        # ── title ─────────────────────────────────────────────────────
        t.insert("end",f"  {s.name or 'Unnamed Spell'}\n","title")
        t.insert("end",f"  {lvl_name}  ·  {s.total_points} pts\n\n","lvl")
        any_content=False
        # ── casting params (Range/Duration/Area/Power/Casting mods) ──
        cast_cats=["Range","Duration","Area","Power","Casting"]
        cast_rows=[(mn,cnt) for mn,cnt in s.global_mods.items()
                   if GLOBAL_MODS.get(mn,{}).get("cat") in cast_cats and cnt>0]
        if cast_rows:
            any_content=True
            t.insert("end","CASTING\n","hdr")
            for mn,cnt in cast_rows:
                m=GLOBAL_MODS.get(mn,{}); cat=m.get("cat","").lower()
                sx=f" ×{cnt}" if cnt>1 else ""
                t.insert("end",f"  {mn}{sx}\n",f"c_{cat}")
                t.insert("end",f"    {m.get('desc','')}\n","eff")
            t.insert("end","\n")
        # ── special mods ──────────────────────────────────────────────
        spec_rows=[(mn,cnt) for mn,cnt in s.global_mods.items()
                   if GLOBAL_MODS.get(mn,{}).get("cat")=="Special" and cnt>0]
        if spec_rows:
            any_content=True
            t.insert("end","SPECIAL MODIFIERS\n","hdr")
            for mn,cnt in spec_rows:
                m=GLOBAL_MODS.get(mn,{})
                sx=f" ×{cnt}" if cnt>1 else ""
                is_db=f"mod/{mn}" in s.drawback_buys
                db_mk=" ◌" if is_db else ""
                t.insert("end",f"  {mn}{sx}{db_mk}\n","db_item" if is_db else "special")
                t.insert("end",f"    {m.get('desc','')}\n","eff")
            t.insert("end","\n")
        # ── schools ───────────────────────────────────────────────────
        active_sc=s.all_schools
        if active_sc:
            any_content=True
            t.insert("end","SCHOOLS\n","hdr")
            for school in active_sc:
                sd=SCHOOLS[school]; sym=sd["symbol"]; sc_col=sd["color"]
                cap=s.capstone_active(school)
                stag=f"st_{id(school)}"
                t.tag_configure(stag,foreground=sc_col,font=("Georgia",9,"bold"))
                cap_mark="  ⚜ CAPSTONE" if cap else ""
                t.insert("end",f"  {sym} {school}{cap_mark}\n",stag)
                # abilities
                ab_dict=s.school_abilities.get(school,{})
                for abn,cnt in ab_dict.items():
                    if cnt>0:
                        ab_info=sd.get("abilities",{}).get(abn,{})
                        sx=f" ×{cnt}" if cnt>1 else ""
                        is_db=f"ability/{school}/{abn}" in s.drawback_buys
                        db_mk=" ◌" if is_db else ""
                        t.insert("end",f"    • {abn}{sx}{db_mk}\n","db_item" if is_db else "ability")
                        t.insert("end",f"      {ab_info.get('desc','')}\n","eff")
                # ring mods
                rd=s.ring_mods.get(school,{})
                rm_labels=sd.get("ring_mods",{})
                for grp in RING_GROUPS:
                    cnt=rd.get(grp,0)
                    if cnt>0:
                        label=rm_labels.get(grp,grp)
                        is_db=f"ringmod/{school}/{grp}" in s.drawback_buys
                        db_mk=" ◌" if is_db else ""
                        t.insert("end",f"    ◦ {label} ×{cnt}{db_mk}\n","db_item" if is_db else "ringmod")
                # capstone desc
                if cap:
                    cd=CAPSTONES.get(school,{})
                    t.insert("end",f"    ⚜ {cd.get('name','')}\n","capstone")
                    t.insert("end",f"      {cd.get('desc','')}\n","cap_desc")
            t.insert("end","\n")
        # ── elements ──────────────────────────────────────────────────
        active_els=[(el,val) for el,val in s.elements.items() if val]
        if active_els:
            any_content=True
            t.insert("end","ELEMENTS\n","hdr")
            for el,val in active_els:
                ed=ELEMENTS[el]; el_tag=f"et_{el}"
                t.tag_configure(el_tag,foreground=ed["color"],font=("Georgia",9,"bold"))
                sub_txt=f"  [{val}]" if isinstance(val,str) else ""
                t.insert("end",f"  {ed['symbol']} {el}{sub_txt}\n",el_tag)
                t.insert("end",f"    {ed['modification']}\n","eff")
                # subtype extra
                if isinstance(val,str):
                    st_eff=ed.get("subtypes",{}).get(val,{}).get("modification","")
                    if st_eff: t.insert("end",f"    ({val}) {st_eff}\n","eff")
                # element nodes
                for nname,cnt in s.element_nodes.get(el,{}).items():
                    if cnt>0:
                        for nd in ed.get("nodes",[]):
                            if nd["name"]==nname:
                                sx=f" ×{cnt}" if cnt>1 else ""
                                t.insert("end",f"    + {nname}{sx}: {nd.get('effect',nd.get('desc',''))}\n","node")
                                break
            # sub-element combos
            for (el1,el2),conn_desc in ELEMENT_CONNECTIONS.items():
                if s.elements.get(el1) and s.elements.get(el2):
                    t.insert("end",f"  ⊕ {el1} + {el2}:\n","sub_el")
                    t.insert("end",f"    {conn_desc}\n","eff")
            t.insert("end","\n")
        # ── synergies ─────────────────────────────────────────────────
        conns=s.active_connections
        if conns:
            any_content=True
            t.insert("end","SYNERGIES\n","hdr")
            for s1,s2,name,is_cap in conns:
                c1=SCHOOLS[s1]["color"]; c2=SCHOOLS[s2]["color"]
                sym1=SCHOOLS[s1]["symbol"]; sym2=SCHOOLS[s2]["symbol"]
                def _hx(c): c=c.lstrip("#"); return int(c[:2],16),int(c[2:4],16),int(c[4:6],16)
                r1,g1,b1=_hx(c1); r2,g2,b2=_hx(c2)
                blend="#{:02x}{:02x}{:02x}".format((r1+r2)//2,(g1+g2)//2,(b1+b2)//2)
                stag=f"sy_{s1[:4]}{s2[:4]}"
                t.tag_configure(stag,foreground="#FFD700" if is_cap else blend,font=("Georgia",9,"bold"))
                cap_mk="  ⚜" if is_cap else "  ○"
                t.insert("end",f"  {sym1} {s1} + {sym2} {s2}{cap_mk}\n",stag)
                t.insert("end",f"    → {name}\n","eff")
            t.insert("end","\n")
        # ── if-then conditions ────────────────────────────────────────
        ifthen = getattr(s,'ifthen_conditions',[])
        if ifthen:
            any_content=True
            t.insert("end","IF-THEN CONDITIONS\n","hdr")
            def _render_cond(cond, indent="  "):
                ifs=', '.join(cond.get('if_schools',[]) or ['—'])
                thens=', '.join(cond.get('then_schools',[]) or [])
                t.insert("end",f"{indent}IF  {ifs}\n","ifthen_if")
                if thens:
                    t.insert("end",f"{indent}THEN  schools: {thens}\n","ifthen_then")
                then_mods=cond.get('then_modifiers',[])
                if then_mods:
                    t.insert("end",f"{indent}THEN  mods: {', '.join(then_mods)}\n","ifthen_then")
                then_abs=cond.get('then_abilities',{})
                for sc,abs_ in then_abs.items():
                    if abs_:
                        t.insert("end",f"{indent}THEN  {sc}: {', '.join(abs_)}\n","ifthen_then")
                txt=cond.get('then_text','')
                if txt:
                    t.insert("end",f"{indent}THEN  {txt}\n","ifthen_then")
                nm=cond.get('name','')
                if nm: t.insert("end",f"{indent}[{nm}]\n","eff")
                for sub in cond.get('chain',[]):
                    _render_cond(sub, indent+"    ↳ ")
                t.insert("end","\n")
            for cond in ifthen:
                _render_cond(cond)
        t.tag_configure("ifthen_if",foreground="#aaddff",font=("Courier",8,"bold"))
        t.tag_configure("ifthen_then",foreground="#88ccaa",font=("Courier",8))
        # ── when-then conditions ─────────────────────────────────────
        whenthen=getattr(s,'when_then_conditions',[])
        if whenthen:
            any_content=True
            t.insert("end","WHEN-THEN CONDITIONS\n","hdr")
            for cond in whenthen:
                t.insert("end",f"  WHEN  {cond.get('when_text','') or '(any)'}\n","ifthen_if")
                t.insert("end",f"  THEN  {cond.get('then_text','') or '(none)'}\n","ifthen_then")
                t.insert("end","\n")
        # ── drawbacks ─────────────────────────────────────────────────
        if s.drawback_buys:
            any_content=True
            t.insert("end","DRAWBACKS\n","hdr")
            for key,neg_name in s.drawback_buys.items():
                parts=key.split("/",1); pretty=parts[1] if len(parts)>1 else key
                t.insert("end",f"  ◌ {pretty}\n","db_key")
                neg_desc=next((m["desc"] for m in NEGATIVE_MODS if m["name"]==neg_name),"")
                t.insert("end",f"    Penalty: {neg_name}","db_name")
                if neg_desc: t.insert("end",f" — {neg_desc}","db_desc")
                t.insert("end","\n")
            t.insert("end","\n")
        # ── custom effects ────────────────────────────────────────────
        if s.custom_effects:
            any_content=True
            t.insert("end","CUSTOM EFFECTS\n","hdr")
            for e in s.custom_effects:
                t.insert("end",f"  • {e}\n","custom")
            t.insert("end","\n")
        # ── empty state ───────────────────────────────────────────────
        if not any_content:
            t.insert("end","\nNo spell components selected yet.\n\nBuy abilities, ring mods, modifiers,\nor elements to see effects here.","empty")
        t.configure(state="disabled")

    def _refresh_ifthen(self):
        if not hasattr(self,'_ifthen_lb'): return
        self._ifthen_lb.delete(0,tk.END)
        def _add(cond,prefix=""):
            ifs=', '.join(cond.get('if_schools',[]) or ['—'])
            thens=', '.join(cond.get('then_schools',[]) or [])
            nm=cond.get('name','')
            label=f"{prefix}IF {ifs}"
            if thens: label+=f"  → {thens}"
            if nm:    label+=f"  [{nm}]"
            self._ifthen_lb.insert(tk.END,label)
            for sub in cond.get('chain',[]):
                _add(sub,prefix+"  ↳ ")
        for cond in getattr(self.spell,'ifthen_conditions',[]):
            _add(cond)
    def _it_add(self):
        IfThenEditor(self,self.spell,on_save=self._it_save_new)
    def _it_edit(self):
        if not hasattr(self,'_ifthen_lb'): return
        sel=self._ifthen_lb.curselection()
        if not sel: return
        conds=getattr(self.spell,'ifthen_conditions',[])
        if sel[0]>=len(conds): return
        IfThenEditor(self,self.spell,cond=conds[sel[0]],
                     on_save=lambda c,i=sel[0]:self._it_save_edit(c,i))
    def _it_remove(self):
        if not hasattr(self,'_ifthen_lb'): return
        sel=self._ifthen_lb.curselection()
        if not sel: return
        conds=getattr(self.spell,'ifthen_conditions',[])
        if sel[0]<len(conds):
            del conds[sel[0]]
            self._refresh_ifthen(); self._refresh()
    def _it_save_new(self,cond):
        if not hasattr(self.spell,'ifthen_conditions'): self.spell.ifthen_conditions=[]
        self.spell.ifthen_conditions.append(cond)
        self._refresh_ifthen(); self._refresh()
    def _it_save_edit(self,cond,idx):
        conds=getattr(self.spell,'ifthen_conditions',[])
        if idx<len(conds): conds[idx]=cond
        self._refresh_ifthen(); self._refresh()

    def _refresh_whenthen(self):
        if not hasattr(self,'_wt_lb'): return
        self._wt_lb.delete(0,tk.END)
        for cond in getattr(self.spell,'when_then_conditions',[]):
            wt=cond.get('when_text','')[:30]; tt=cond.get('then_text','')[:30]
            self._wt_lb.insert(tk.END,f"WHEN {wt}…  →  {tt}…")
    def _wt_add(self):
        WhenThenEditor(self,on_save=self._wt_save_new)
    def _wt_edit(self):
        if not hasattr(self,'_wt_lb'): return
        sel=self._wt_lb.curselection()
        if not sel: return
        conds=getattr(self.spell,'when_then_conditions',[])
        if sel[0]>=len(conds): return
        WhenThenEditor(self,cond=conds[sel[0]],
                       on_save=lambda c,i=sel[0]:self._wt_save_edit(c,i))
    def _wt_remove(self):
        if not hasattr(self,'_wt_lb'): return
        sel=self._wt_lb.curselection()
        if not sel: return
        conds=getattr(self.spell,'when_then_conditions',[])
        if sel[0]<len(conds):
            del conds[sel[0]]
            self._refresh_whenthen(); self._refresh()
    def _wt_save_new(self,cond):
        if not hasattr(self.spell,'when_then_conditions'): self.spell.when_then_conditions=[]
        self.spell.when_then_conditions.append(cond)
        self._refresh_whenthen(); self._refresh()
    def _wt_save_edit(self,cond,idx):
        conds=getattr(self.spell,'when_then_conditions',[])
        if idx<len(conds): conds[idx]=cond
        self._refresh_whenthen(); self._refresh()

    def _refresh_neg_mods(self):
        if not hasattr(self,'_nm_lb'): return
        self._nm_lb.delete(0,tk.END)
        for m in NEGATIVE_MODS:
            tag="[def]" if m in DEFAULT_NEGATIVE_MODS else "[usr]"
            self._nm_lb.insert(tk.END,f"{tag} {m['name']}")
    def _nm_on_sel(self,_=None):
        if not hasattr(self,'_nm_lb') or not hasattr(self,'_nm_desc'): return
        sel=self._nm_lb.curselection()
        if not sel: return
        m=NEGATIVE_MODS[sel[0]]
        self._nm_desc.configure(state="normal"); self._nm_desc.delete("1.0",tk.END)
        self._nm_desc.insert("1.0",m["desc"]); self._nm_desc.configure(state="disabled")
    def _nm_add(self):
        self._nm_open_form("Add Negative Modifier","","",is_new=True)
    def _nm_edit(self):
        if not hasattr(self,'_nm_lb'): return
        sel=self._nm_lb.curselection()
        if not sel: return
        m=NEGATIVE_MODS[sel[0]]
        self._nm_open_form("Edit Negative Modifier",m["name"],m["desc"],idx=sel[0])
    def _nm_remove(self):
        if not hasattr(self,'_nm_lb'): return
        sel=self._nm_lb.curselection()
        if not sel: return
        m=NEGATIVE_MODS[sel[0]]
        if m in DEFAULT_NEGATIVE_MODS:
            messagebox.showinfo("Cannot Remove","Default negative modifiers cannot be removed.",parent=self); return
        NEGATIVE_MODS.remove(m); self._refresh_neg_mods()
    def _nm_open_form(self,title,name,desc,is_new=False,idx=None):
        d=tk.Toplevel(self); d.title(title); d.configure(bg=self.BG)
        d.geometry("400x240"); d.transient(self); d.grab_set()
        tk.Label(d,text="Name:",bg=self.BG,fg=self.TXT,font=("Georgia",9)).pack(anchor=tk.W,padx=12,pady=(12,2))
        nv=tk.StringVar(value=name)
        ttk.Entry(d,textvariable=nv,width=40).pack(fill=tk.X,padx=12)
        tk.Label(d,text="Description:",bg=self.BG,fg=self.TXT,font=("Georgia",9)).pack(anchor=tk.W,padx=12,pady=(8,2))
        dt=tk.Text(d,height=4,bg=self.EBG,fg=self.TXT,insertbackground=self.TXT,
                   relief="solid",bd=1,font=("Georgia",9),wrap=tk.WORD)
        dt.pack(fill=tk.X,padx=12); dt.insert("1.0",desc)
        def _save():
            n2=nv.get().strip(); d2=dt.get("1.0",tk.END).strip()
            if not n2: return
            if is_new: NEGATIVE_MODS.append({"name":n2,"desc":d2})
            elif idx is not None: NEGATIVE_MODS[idx]={"name":n2,"desc":d2}
            d.destroy(); self._refresh_neg_mods()
        bf=tk.Frame(d,bg=self.BG); bf.pack(fill=tk.X,padx=12,pady=8)
        ttk.Button(bf,text="Save",command=_save).pack(side=tk.RIGHT,padx=4)
        ttk.Button(bf,text="Cancel",command=d.destroy,style="D.TButton").pack(side=tk.RIGHT,padx=4)

    # ── Events ────────────────────────────────────────────────────
    @staticmethod
    def _blend(c1,c2):
        def p(c): c=c.lstrip("#"); return int(c[:2],16),int(c[2:4],16),int(c[4:6],16)
        r1,g1,b1=p(c1); r2,g2,b2=p(c2)
        return "#{:02x}{:02x}{:02x}".format((r1+r2)//2,(g1+g2)//2,(b1+b2)//2)

    def _on_name(self):
        self.spell.name=self.name_var.get()
        if hasattr(self,'circle'): self.circle.load(self.spell)
    def _addce(self):
        t=self.ce.get().strip()
        if t: self.spell.custom_effects.append(t); self.ce.delete(0,tk.END); self._ref_ce()
    def _rmce(self):
        sel=self.cl.curselection()
        if sel: del self.spell.custom_effects[sel[0]]; self._ref_ce()
    def _ladd(self): self.library.append(deepcopy(self.spell)); self._ref_lib(); messagebox.showinfo("Library",f"'{self.spell.name}' added.")
    def _lload(self):
        sel=self.ll.curselection()
        if not sel: return
        self.spell=deepcopy(self.library[sel[0]]); self._sync(); self._refresh()
    def _ldel(self):
        sel=self.ll.curselection()
        if sel: del self.library[sel[0]]; self._ref_lib()

    # ── Refresh ───────────────────────────────────────────────────
    def _refresh(self):
        if not hasattr(self,'circle') or not hasattr(self,'calc'): return
        self.circle.load(self.spell); self.calc.spell=self.spell; self.calc.refresh()
        self._ref_conn(); self._ref_ce(); self._ref_lib()
        if hasattr(self,'_spanels'):
            for s,p in self._spanels.items(): p.spell=self.spell; p.refresh()
        if hasattr(self,'mp'): self.mp.spell=self.spell; self.mp.refresh()
        if hasattr(self,'el_panel'): self.el_panel.spell=self.spell
        self._refresh_drawbacks()
        self._refresh_ifthen()
        self._refresh_whenthen()
        self._refresh_effects()
    def _ref_conn(self):
        if not hasattr(self,'ct'): return
        conns=self.spell.active_connections
        self.ct.configure(state="normal"); self.ct.delete("1.0",tk.END)
        if not conns:
            self.ct.insert("1.0","No synergies active yet.\n\nBuy abilities or ring mods across multiple schools to activate synergies.")
        else:
            golden=[c for c in conns if c[3]]
            dormant=[c for c in conns if not c[3]]
            if golden:
                self.ct.insert("end","⚜  CAPSTONE SYNERGIES  ⚜\n","hg")
                self.ct.tag_configure("hg",foreground="#FFD700",font=("Georgia",9,"bold italic"))
                for s1,s2,name,_ in golden:
                    t=f"cg{s1}{s2}"
                    self.ct.insert("end",f"\n{SCHOOLS[s1]['symbol']} {s1}  ⊕  {SCHOOLS[s2]['symbol']} {s2}\n",t)
                    self.ct.insert("end",f"→ {name}\n")
                    self.ct.tag_configure(t,foreground="#FFD700",font=("Georgia",9,"bold"))
            if dormant:
                sep="\n" if golden else ""
                self.ct.insert("end",f"{sep}○  INACTIVE SYNERGIES  ○\n","hd")
                self.ct.tag_configure("hd",foreground="#445566",font=("Georgia",9,"italic"))
                for s1,s2,name,_ in dormant:
                    t=f"cd{s1}{s2}"; fc=self._blend(SCHOOLS[s1]["color"],SCHOOLS[s2]["color"])
                    self.ct.insert("end",f"\n{SCHOOLS[s1]['symbol']} {s1}  ⊕  {SCHOOLS[s2]['symbol']} {s2}\n",t)
                    self.ct.insert("end",f"→ {name}\n")
                    self.ct.tag_configure(t,foreground=fc,font=("Georgia",9,"bold"))
        self.ct.configure(state="disabled")
    def _ref_ce(self):
        if not hasattr(self,'cl'): return
        self.cl.delete(0,tk.END); [self.cl.insert(tk.END,e) for e in self.spell.custom_effects]
    def _ref_lib(self):
        if not hasattr(self,'ll'): return
        self.ll.delete(0,tk.END)
        for sp in self.library: _,lvl,_=sp.level_info; self.ll.insert(tk.END,f"[{lvl[:3]}·{sp.total_points}pt] {sp.name}")

    # ── Sync ──────────────────────────────────────────────────────
    def _sync(self):
        self.name_var.set(self.spell.name); self.desc_text.delete("1.0",tk.END); self.desc_text.insert("1.0",self.spell.description)
        for s,p in self._spanels.items():
            p.spell=self.spell; self.spell.school_abilities.setdefault(s,{}); self.spell.ring_mods.setdefault(s,{}); p.refresh()
        self.mp.spell=self.spell; self.mp.refresh()
        self.el_panel.spell=self.spell

    # ── File I/O ──────────────────────────────────────────────────
    def _save(self):
        fp=filedialog.asksaveasfilename(defaultextension=".json",filetypes=[("JSON","*.json"),("All","*.*")],initialfile=f"{self.spell.name.replace(' ','_')}.json",title="Save")
        if fp:
            with open(fp,"w") as fh: json.dump(self.spell.to_dict(),fh,indent=2)
            messagebox.showinfo("Saved",f"Saved:\n{fp}")
    def _load(self):
        fp=filedialog.askopenfilename(filetypes=[("JSON","*.json"),("All","*.*")],title="Load")
        if fp:
            with open(fp) as fh: data=json.load(fh)
            self.spell=Spell.from_dict(data); self._sync(); self._refresh()
    def _epng(self):
        fp=filedialog.asksaveasfilename(defaultextension=".png",filetypes=[("PNG","*.png")],initialfile=f"{self.spell.name.replace(' ','_')}_circle.png",title="Export PNG")
        if fp and self.circle.export_png(fp): messagebox.showinfo("Exported",f"PNG saved:\n{fp}")
    def _etxt(self):
        fp=filedialog.asksaveasfilename(defaultextension=".txt",filetypes=[("Text","*.txt")],initialfile=f"{self.spell.name.replace(' ','_')}.txt",title="Export Text")
        if not fp: return
        s=self.spell; _,lvl,_=s.level_info
        lines=["╔═══════════════════════════════════════════════════╗",
               f"║  {s.name:<48}║",f"║  {lvl+' · '+str(s.total_points)+' points':<48}║",
               "╚═══════════════════════════════════════════════════╝","",
               f"Description: {s.description or '(none)'}","","SCHOOLS:"]
        for sc in s.all_schools: lines.append(f"  {sc}")
        for school in s.all_schools:
            ab_dict=s.school_abilities.get(school,{}); active=[(k,v) for k,v in ab_dict.items() if v>0]
            rd=s.ring_mods.get(school,{}); cap=s.capstone_active(school)
            if active or any(rd.values()):
                lines+=["",f"{school.upper()}{' — ⚜ CAPSTONE UNLOCKED' if cap else ''}:"]
                if cap: lines.append(f"  Capstone: {CAPSTONES[school]['name']} — {CAPSTONES[school]['desc']}")
                for ab,cnt in active:
                    c2=SCHOOLS[school]["abilities"].get(ab,{}).get("cost",0)
                    lines.append(f"  ×{cnt}  {ab}  ({c2*cnt} pts)"); lines.append(f"       {SCHOOLS[school]['abilities'][ab]['desc']}")
                for grp,cnt in rd.items():
                    if cnt: lines.append(f"  Ring {grp}: ×{cnt} sections ({cnt} pts)")
        if any(v>0 for v in s.global_mods.values()):
            lines+=["","GLOBAL MODIFIERS:"]
            for mod,cnt in s.global_mods.items():
                if cnt>0:
                    mc=GLOBAL_MODS.get(mod,{}).get("cost",0); rune=MOD_RUNES.get(GLOBAL_MODS[mod]["cat"],["?"])[0]
                    lines.append(f"  ×{cnt}  {rune} {mod}  ({mc*cnt} pts)"); lines.append(f"       {GLOBAL_MODS[mod]['desc']}")
        active_elems=[(el,val) for el,val in s.elements.items() if val]
        if active_elems:
            lines+=["","ELEMENTS:"]
            for el,val in active_elems:
                edata=ELEMENTS[el]; sub_txt=f" Subtype: {val}" if isinstance(val,str) else ""
                lines.append(f"  {edata['symbol']} {el}{sub_txt}")
                lines.append(f"  Effect: {edata['modification']}")
                if isinstance(val,str) and val in edata.get("subtypes",{}):
                    lines.append(f"  Subtype effect: {edata['subtypes'][val]['modification']}")
        if s.active_connections:
            lines+=["","SYNERGIES:"]; [lines.append(f"  {s1} + {s2}: {name}") for s1,s2,name,_ in s.active_connections]
        if s.custom_effects:
            lines+=["","CUSTOM EFFECTS:"]; [lines.append(f"  • {e}") for e in s.custom_effects]
        with open(fp,"w") as fh: fh.write("\n".join(lines))
        messagebox.showinfo("Exported",f"Text saved:\n{fp}")
    def _new(self):
        if messagebox.askyesno("New Spell","Clear current spell?"):
            self.spell=Spell(); self._sync(); self._refresh()

# ═══════════════════════════════════════════════════════════════
if __name__=="__main__":
    app=SpellForgeApp(); app.mainloop()
