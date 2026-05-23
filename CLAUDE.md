# SpellForge — Claude Code Handoff

## What This Is

**SpellForge** is a single-file Python/tkinter RPG spell-construction tool. The user builds spells by selecting schools of magic, buying abilities, setting ring modifiers, choosing elemental affinities, and configuring global modifiers. A live magic circle updates in real time to reflect the spell's composition.

Run it with:
```bash
python spell_forge.py
```

No pip installs required. Python 3.8+ and tkinter (usually bundled) are the only dependencies. For PNG export, `pip install Pillow` is optional.

---

## File Structure

```
spell_forge.py     ← entire application (≈2 100 lines, single file)
CLAUDE.md          ← this file
requirements.txt   ← optional deps (Pillow for PNG export only)
```

---

## Architecture at a Glance

### GUI Layout
Three horizontally resizable panes (`DragPane`):

| Pane | Contents |
|------|----------|
| Left | Identity tab · Elements tab (🜂) · 10 school tabs (one per school) |
| Centre | `MagicCircleCanvas` — the live magic circle |
| Right | Calculator · Modifiers · Synergies · Guide · Library |

### Magic Circle Draw Order (bottom → top)
1. `_draw_deep_bg(R)` — parchment paper background with vignette, ink stains, foxing spots
2. `_draw_level_rings(R)` — 15 concentric rings spanning the full circle, one per spell-level threshold; filled = reached, ghost = not yet
3. `_draw_outer_frame(R, pos)` — double outer border rings + tick marks + necklace connector ring + chord web between all 10 school positions + synergy lines
4. `_draw_main_geometry(R, pos)` — inner sacred geometry web (10-point chord web, hexagram, pentagon, concentric rings) confined to `R*0.50`
5. `_draw_element_band(R)` — element constellation nodes at orbital radius `R*0.55`, with satellite upgrade nodes and sub-element mini-constellations
6. `_draw_school_modules(pos)` — all 10 school circles always rendered; active = full brightness, inactive = 20% dim
7. `_draw_center_hub()` — global modifier hub at centre, 6 category sections with runes for active mods
8. `_draw_status_bar(W, H)` — level/pts/zoom text

### Coordinate System
- World origin `(0, 0)` = centre of the magic circle
- Canvas coords: `_tc(wx, wy)` → `(_ox + wx*zoom, _oy + wy*zoom)`
- `_ox`, `_oy` = current pan offset; `_zoom` = scale factor (0.20 – 5.0)
- Zoom is mouse-centred: the world point under the cursor stays fixed
- **Geometry** (ovals, arcs) scales with zoom via `rs = r * self._zoom`
- **Text** does NOT scale (fixed pixel size regardless of zoom)

### Key Drawing Helpers (all take world coords)
| Helper | Purpose |
|--------|---------|
| `_tc(wx, wy)` | World → canvas coords |
| `_wpt(cx, cy, r, deg)` | Point on circle at angle |
| `_circle_w(wx, wy, r, **kw)` | Filled oval, zoom-scaled radius |
| `_ring_w(wx, wy, r, color, w, dash)` | Circle outline, zoom-scaled |
| `_arc_ring_w(wx, wy, r, start_deg, extent_deg, **kw)` | Arc outline |
| `_wedge_w(wx, wy, r_in, r_out, d_start, d_end, color)` | Filled annular sector |
| `_line_w(wx1, wy1, wx2, wy2, **kw)` | Line in world space |
| `_poly_w(pts_world, **kw)` | Polygon from world-space point list |
| `_text_w(wx, wy, **kw)` | Text at world position (font NOT zoom-scaled) |
| `_star_w(wx, wy, R, r_in, n, off, **kw)` | n-pointed star polygon |
| `_poly_n_w(wx, wy, r, n, off, **kw)` | Regular n-gon |
| `_arc_text_w(wx, wy, r, text, start_deg, color, fontsize, step_deg)` | Text curved along arc |

---

## Data Model

```python
@dataclass
class Spell:
    name:               str
    description:        str
    primary_school:     str                        # always one of SCHOOLS.keys()
    secondary_schools:  List[str]                  # subset of SCHOOLS.keys()
    school_abilities:   Dict[str, Dict[str, int]]  # school → {ability_name → count}
    ring_mods:          Dict[str, Dict[str, int]]  # school → {group → 0-3}
    global_mods:        Dict[str, int]             # mod_name → count
    elements:           Dict[str, object]          # el → True | subtype_str
    element_nodes:      Dict[str, Dict[str, int]]  # el → {node_name → count}
    subelement_nodes:   Dict[str, Dict[str, int]]  # "el1,el2" → {node_name → count}
    circle_sizes:       Dict[str, float]           # school → 0.4–2.2×
    custom_effects:     List[str]
```

`Spell.total_points` is a property that sums all purchased items. `Spell.level_info` returns `(level_index, level_name, colour)` from `LEVEL_TABLE`.

---

## Key Data Tables

### `SCHOOLS` (dict, 10 entries)
Each school has: `color`, `symbol`, `short`, `desc`, `ring_mods` (4 group labels), `abilities` (dict of `{name: {cost, desc}}`).

### `ELEMENTS` (dict, 5 entries)
`Fire Water Earth Wind Celestial`. Each has `color`, `symbol`, `rune`, `modification`, `desc`, `nodes` (list of upgrade dicts), and `Celestial` also has `subtypes`.

### `SUBELEMENT_NODES` (dict, 10 pairs)
Keys are `(el1, el2)` tuples. Each value is a list of 3 node dicts `{name, cost, rune, glyph, desc, effect}`.

### `ELEMENT_CONNECTIONS` (dict, 10 pairs)
Keys are `(el1, el2)` tuples → human-readable effect description string.

### `CAPSTONES` (dict, 10 entries)
One per school: `{name, desc, glyph (1 char), ring (8 chars), color}`.

### `DEFAULT_GLOBAL_MODS` / `GLOBAL_MODS` (dict, 47 entries)
Each mod: `{cat, cost, max, desc}`. Categories: Range Duration Area Power Casting Special.

### `LEVEL_TABLE` (list, 15 entries)
Each: `(lo, hi, name, color)`. Points map to spell level via `pts_to_level(pts)`.

### `SCHOOL_CONNECTIONS` (dict, 45 pairs)
Keys are `(school1, school2)` tuples → synergy name string.

---

## UI Panels

### `SchoolAbilityPanel`
One per school (in left-pane tabs). Contains:
- Module size slider → `spell.circle_sizes[school]`
- Ring modifier +/− rows (Range/Duration/Area/Power, 0-3 each) → `spell.ring_mods[school]`
- Ability +/− rows → `spell.school_abilities[school]`
- Capstone info box with ✎ Edit button → `CapstoneEditor` dialog

### `ElementPanel`
Single tab in left pane. Contains per-element:
- Toggle checkbox → `spell.elements[el]`
- ✎ Edit button → `ElementEditor` dialog
- Subtype radio buttons (Celestial only)
- Constellation Upgrades node buy rows → `spell.element_nodes[el]`

### `GlobalModPanel`
In right pane Modifiers tab. Contains per-category grouped rows with +/− and ✎/✕ buttons. "New" button opens `ModEditor`.

### `CalculatorPanel`
Shows total points, level progress bar, full cost breakdown, level threshold table.

---

## Dialog Classes

| Class | Purpose |
|-------|---------|
| `ModEditor` | Add/edit a global modifier (name, cat, cost, max, desc) |
| `ElementEditor` | Edit element modification text + description |
| `CapstoneEditor` | Edit capstone name, desc, sigil glyph, orbital rune string |

---

## Common Extension Tasks

### Add a new school
1. Add entry to `SCHOOLS` dict (color, symbol, short, desc, ring_mods, abilities)
2. Add entry to `SCHOOL_GLYPHS` (8 glyphs)
3. Add entry to `CAPSTONES` (glyph, ring, name, desc, color)
4. Add synergy entries to `SCHOOL_CONNECTIONS` as needed

### Add a new element
1. Add entry to `ELEMENTS` dict with nodes list
2. Add `ELEMENT_CONNECTIONS` entries for pairs with existing elements
3. Add `SUBELEMENT_NODES` entries for those pairs

### Change the circle layout
- All radii are fractions of `R = MagicCircleCanvas.OUTER_R = 340` pixels
- Key radii: outer border = R, connector ring = R*0.82, element band = R*0.55, geometry web = R*0.48, center hub = `CENTER_R = 88`
- `NODE_R_PRI = 52` (primary school), `NODE_R_SEC = 38` (secondary)

### Change draw order
Edit `_do_redraw(self, W, H)` — the calls there define layer order.

### Fix a zoom bug
- Geometry that should scale: use `rs = r * self._zoom` in oval/arc creation
- Text that should NOT scale: use `_text_w()` with a fixed font size
- New arc methods should use `_arc_ring_w()` not raw `create_arc()`

---

## Known Issues / Future Work

- PNG export requires `Pillow` (`pip install Pillow`); degrades gracefully without it
- `_arc_text_w` font size is fixed (not zoom-scaled); intentional
- School label text can overlap at small zoom on dense configurations
- Sub-element node UI (buy buttons) not yet wired in the ElementPanel — data model supports it via `spell.subelement_nodes`, just needs panel rows analogous to `element_nodes` rows

---

## Testing Without a Display

The draw pipeline can be exercised headlessly by stubbing tkinter:

```python
import sys, types
class FW:
    def __init__(self,*a,**k): pass
    def create_oval(self,*a,**k): pass
    def create_line(self,*a,**k): pass
    def create_text(self,*a,**k): pass
    def create_arc(self,*a,**k): pass
    def create_polygon(self,*a,**k): pass
    def create_rectangle(self,*a,**k): pass
    def delete(self,*a,**k): pass
    def winfo_width(self): return 900
    def winfo_height(self): return 700
    # ... (add other needed stubs)

tk = types.ModuleType('tkinter')
tk.Canvas = FW
# ... etc.
sys.modules['tkinter'] = tk

# Then exec the file, create MCC.__new__(MCC), wire stub methods, call _do_redraw(900,700)
```

See the conversation history for complete working stub implementations.

---

## Git Workflow

### Branching strategy
- **`master`** — stable, always buildable. All completed features land here.
- **`feature/<short-name>`** — one branch per logical feature (e.g. `feature/right-click-drawbacks`). Branch from master, merge back when done.
- **`fix/<short-name>`** — for targeted bug fixes.

Claude Code manages branch creation, commits, and merges. When starting a new chunk of work it will:
1. Create an appropriately named branch off master.
2. Commit incrementally with structured messages as work progresses.
3. Merge (fast-forward where possible, or a merge commit with a summary) back to master when the feature is complete and building clean.

### Commit message format
```
<Scope>: <imperative summary under 72 chars>

- Bullet details explaining *what* changed and *why*
- One bullet per logical sub-change
- Reference any rules or constraints that drove the decision
```

**Scopes in use:** `Canvas`, `Models`, `ViewModels`, `Views`, `GameData`, `Spell`, `Build`

### Rules
- Every commit must build with 0 errors before it is recorded.
- The `master` branch is **never force-pushed**.
- Squash is used only to clean up noisy WIP commits before merging a feature branch.

### Git executable
```
C:\Users\anaki\Documents\Git\Git\cmd\git.exe
```
