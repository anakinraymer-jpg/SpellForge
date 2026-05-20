# SpellForge — Design Prompt History

This file records every major design instruction given during the development of SpellForge, so Claude Code can understand the intent behind each feature and continue development coherently.

---

## Core Vision

Build an RPG spell construction tool where the user builds spells by selecting schools of magic, buying abilities with a point system, and watches a procedurally-drawn magic circle update in real time.

---

## Prompt 1 — Initial Feature Set

> "Make the central magic circle a magic circle that contains the global modifiers, instead of the primary school, and give them each a section within their magic circle. Make the ring modifier show as a rune on the magic circle when selected. Give each school of magic a capstone ability when all ring modifiers are filled in its own magic circle, include a cool image related to the school of magic when the capstone is fulfilled. Add selectable elements from Fire, Water, Earth, Wind, and Celestial. Celestial subcategories are Radiant, Necrotic, and Psychic. When selected the elements show as concentric rings within the magic circle. Have each element control a major modification, and display the information. Make it so that all parts of the magic circle do not change size when zooming in and add the ability to zoom in centered on the mouse."

**What was built:**
- Center circle = global modifier hub with 6 category sections
- Ring modifiers show as runes when filled
- 10 school capstones with a sigil + orbital rune ring (glyph/ring keys per school)
- Elements: Fire/Water/Earth/Wind/Celestial with Radiant/Necrotic/Psychic subtypes
- Elements displayed as orbital glyph nodes (moved from concentric rings later)
- Zoom centred on mouse cursor; geometry scales with zoom, text does not

---

## Prompt 2 — Bug Fix: `'str' object is not callable`

> "Reference this error to find and apply fixes: TypeError: 'str' object is not callable at `self._w(wx,wy)`"

**Root cause:** `tkinter.Canvas` sets `self._w = ".!canvas"` (a string) on every Canvas instance during `__init__`, permanently shadowing any method named `_w`. Fixed by renaming the world→canvas transform method to `_tc`.

---

## Prompt 3 — Remove Outer Rings + Fixed-Size Zoom

> "Remove the outer layers of colored rings around all magic circles. Make it so that when I zoom in nothing shrinks or zooms in with me."

**What changed:**
- Removed gradient glow ovals around each school module
- Removed background radial glow
- Removed hub glow circles
- Zoom redesigned: `_tc()` scales world coords by zoom so positions move; radii use `rs = r * self._zoom` so geometry scales; text uses fixed font sizes so text doesn't resize

---

## Prompt 4 — Fix All Remaining Zoom Issues + Add Features

> "Many parts of the magic circle still don't work with the zoom in function. Display spell level on the magic circle by adding concentric rings of glyphs with borders for each ring, equidistant, one ring per level. Change elemental affinities into glyph nodes between the middle magic circle and the school magic circles. Add sub-elements that can interconnect between major elements. Add ability to edit Elemental Affinities, School Abilities, Ring Modifiers, and Capstones. Change capstone image to one singular image and not a cluster."

**What changed:**
- Fixed ALL `create_arc`, `create_oval` calls to use `rs = r * self._zoom`
- Added `_arc_ring_w()` helper to replace raw `create_arc` everywhere
- Level rings: 15 concentric rings spanning full circle, one per LEVEL_TABLE threshold
- Elements redesigned as orbital glyph constellation nodes at `R*0.55`
- Sub-element connections drawn as dashed lines with midpoint diamonds
- `ELEMENT_CONNECTIONS` dict added (10 pairs)
- `SUBELEMENT_NODES` dict (10 pairs × 3 upgrade nodes each)
- `ElementEditor`, `CapstoneEditor` dialogs added
- ✎ Edit buttons on SchoolAbilityPanel and ElementPanel
- Capstone: single large glyph + 8-char orbital rune ring (replaced multi-row cluster)

---

## Prompt 5 — Fix Glyph Shrinking + Constellations + Parchment

> "The glyph and symbol images shrink when zooming in, please fix that. Add a unique display ring for each level threshold filling the entire magic circle, put it on a layer behind everything. Make elemental nodes a cluster of nodes like constellations that work with the point buy system. Add a similar system for sub-elements. Make the design fantasy as if appearing on parchment paper."

**What changed:**
- `_text_w` font scaling removed — text is now always fixed pixel size
- Level rings redesigned: all 15 always present, spanning `R*0.97` → `R*0.03`, filled = reached, ghost = not yet
- Element constellation: each element has a centre node + up to 5 satellite upgrade nodes (fan-spread)
- Sub-element mini-constellations appear at midpoint between parent nodes when both active
- `Spell` dataclass: added `element_nodes` and `subelement_nodes` fields
- `total_points` updated to count element and sub-element node costs
- ElementPanel: Constellation Upgrades node buy rows added
- Parchment background: 12-layer gradient, ink ring stains, 18 foxing age-spots, crease lines, ghost geometry
- All ring/module/hub borders shifted to warm parchment ink tones (`#e8d8a0`, `#c8a060`)

---

## Prompt 6 — Reference Image Layout (Current)

> "Make the magic circle appear like the attached image [MC3.png — dark-background grimoire style with 10 school circles on outer ring connected by a necklace ring]. Place the magic circles of the schools of magic on the outer circle. Make the schools of magic visible and present instead of appearing when selected. Have school abilities appear as runes on the outer ring for the magic circles of the schools of magic, and ring modifiers as runes on the inner ring. Have elemental affinities branch out as the middle ring."

**What changed:**
- All 10 schools always visible — `_compute_node_positions` now uses `list(SCHOOLS.keys())` at fixed positions on `R*0.82`
- Active (primary/secondary) = full brightness; inactive = 20% dim but fully drawn
- `_draw_outer_rings` replaced by `_draw_outer_frame(R, pos)`: double border, tick marks, connector/necklace ring at `R*0.82`, chord web between all 10 node positions
- `_draw_module` completely redesigned:
  - **Outer rune ring** (`r*0.84`): one rune per ability, bright+dot if purchased
  - **Inner rune ring** (`r*0.64`): ring modifier runes (12 positions), bright if filled
  - 8 radial spokes, central pentagon, school symbol
- Inner geometry web confined to `R*0.50` (chord web + hexagram + concentric rings)
- Element band at `R*0.55` (between geometry web and school circles)
- `_draw_connection_lines` made subtle (used only within outer frame)
- "No schools" guard removed — circle always draws

---

## Design Principles Established Through Development

1. **Single file** — entire app in `spell_forge.py`, no external modules needed at runtime
2. **Text never scales with zoom** — all fonts are fixed pixel sizes
3. **Geometry always scales with zoom** — every `create_oval`/`create_arc` uses `rs = r * self._zoom`
4. **All 10 schools always visible** — no "select to see" gates; inactive schools are dim
5. **Parchment aesthetic** — warm browns/ambers, ink-style borders, no cold blues
6. **Point-buy everywhere** — abilities, ring mods, global mods, element nodes all cost spell points
7. **Save/load as JSON** — `Spell.to_dict()` / `Spell.from_dict()`

---

## Reference Image Notes (MC3.png)

The reference magic circle (a dark grimoire-style image) shows:
- 10 octagonal/circular school sub-circles on the outer ring, connected by a continuous necklace band
- Each sub-circle has multi-ring detail (double border, spoke pattern, symbol in centre)
- A chord web connecting all 10 nodes through the interior
- Concentric rings filling the interior
- A central circle with its own internal geometry
- Latin-style arc text around the outermost border
- Small diamond/symbol accent markers between the outer border and the school circles

The SpellForge implementation matches this layout while adding colour, rune inscriptions, the element band, and the point-buy interactivity.
