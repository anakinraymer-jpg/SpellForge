# SpellForge — RPG Ground Rules

These rules define the boundaries of a legal SpellForge spell.  
Hard caps are enforced in code; this document explains the *why* behind each.

---

## 1. Point Budget

| Level bracket | Points required | Notes |
|---|---|---|
| Cantrip | 0 – 2 | Trivial effect, no training needed |
| 1st – 3rd Level | 3 – 14 | Apprentice spells |
| 4th – 6th Level | 15 – 32 | Journeyman spells |
| 7th – 9th Level | 33 – 60 | Master spells |
| Legendary – Divine | 61 – 98 | Rare; requires great study or innate power |
| Cosmic – Omnipotent | 99 – ∞ | World-shaping; reserved for legendary casters |

**High-budget spells (40 – 100+ pts)** are the intended sweet spot.  
There is no hard upper cap on total points. Spend freely — limits come from *composition* rules, not spend.

---

## 2. School Limit — Maximum 3

A spell may draw from **at most 3 schools of magic** simultaneously.

A school becomes "active" the moment any ability or ring modifier in that school has a non-zero count.

**Rationale:** Magical traditions require intense study. Weaving more than three together produces chaotic, unstable effects. Synergies between school pairs reward focused builds, not unfocused ones.

**Enforcement:** The `+` buttons for abilities and ring mods in a 4th school are disabled once 3 schools are already active. Existing data in a 4th school (e.g. from a loaded save with an older rule set) triggers a validation warning in the Calculator panel.

---

## 3. Element Limit — Maximum 3

A spell may have **at most 3 elemental affinities** active simultaneously.

**Rationale:** Each element fundamentally changes the spell's nature. Three is already a complex interaction; four or more overlap destructively and blur the caster's intent.

**Enforcement:** The element toggle in the Elements tab refuses a 4th selection with a dialog prompt. The validation system also flags this if data is loaded that exceeds the cap.

---

## 4. Ring Modifier Cap — Tied to Spell Level

Ring modifiers (Range, Duration, Area, Power) are powerful force-multipliers. Their total pip count is capped based on the spell's **base level** — the level computed from *all points except ring mods themselves*.

### Formula

```
BasePoints  = TotalPoints − TotalRingMods
LevelIndex  = index into the 15-entry level table for BasePoints
              (Cantrip = 0 … Omnipotent = 14)
RingModCap  = clamp(LevelIndex × 3,  min = 2,  max = 36)
```

### Why BasePoints (not TotalPoints)?

Using `TotalPoints` would create a bootstrapping loop: buying ring mods raises the level, which raises the cap, which allows more ring mods, which raises the level further.  
`BasePoints` breaks the loop: ring mods can never inflate their own cap.

### Ring mod cap table

| Base level | LevelIndex | Ring mod cap |
|---|---|---|
| Cantrip | 0 | 2 |
| 1st Level | 1 | 3 |
| 2nd Level | 2 | 6 |
| 3rd Level | 3 | 9 |
| 4th Level | 4 | 12 |
| 5th Level | 5 | 15 |
| 6th Level | 6 | 18 |
| 7th Level | 7 | 21 |
| 8th Level | 8 | 24 |
| 9th Level | 9 | 27 |
| Legendary | 10 | 30 |
| Mythic | 11 | 33 |
| Divine | 12 | 36 |
| Cosmic | 13 | 36 (capped) |
| Omnipotent | 14 | 36 (capped) |

Each ring mod group maxes at 3 pips (hardcoded), so 12 ring groups × 3 pips = 36 theoretical maximum — hence the cap of 36.

**Enforcement:** The `+` button for each ring mod pip is disabled when `TotalRingMods >= RingModCap`. The Calculator tab shows a live ring-mod budget bar and a warning when the cap is exceeded (possible when loading older saves).

---

## 5. Combat Time — Phases and Rounds

### Structure

| Unit | Length |
|---|---|
| **Phase** | The smallest unit of in-combat time — one character's turn |
| **Round** | 5 Phases |

Every participant in an encounter acts on their Phase. Once all 5 Phases of a Round have resolved, a new Round begins.

### What you may do on your Phase

Choose **one** of the following:

| Option | What you do | Side effect |
|---|---|---|
| **Move** | Travel up to your movement speed | None |
| **Action** | Perform one action (see below) | None |
| **Hurry** | Move *and* take one action | All enemies gain **+5 to attack** for 1 round |

> **Hurry** represents reckless haste — you expose openings while rushing. The +5 attack bonus applies to *all* enemies currently in the encounter for the remainder of the current round (i.e., until the next Round begins).

### Actions

An Action is a deliberate, focused act. Exactly one may be taken per Phase (without Hurrying):

| Action | Description |
|---|---|
| **Make an attack** | Attempt to strike a target with a weapon or unarmed blow |
| **Use a skill** | Apply a trained ability (Stealth, Persuasion, Athletics, etc.) |
| **Cast a spell** | Invoke a prepared SpellForge spell |
| **Discreet action** | Any focused activity that requires full attention but is not inherently aggressive (pick a lock, tend a wound, examine an object, etc.) |

### Casting spells in combat

Casting a spell uses your entire Action for that Phase. A Hurried cast is legal — the caster moves and casts in one Phase — but grants all enemies +5 to attack for the round, reflecting the loss of focus.  
Higher-cost or more complex spells may impose additional casting conditions defined in the spell's **Conditions** field.

---

## 6. Player Health — Equipment, Skills, and Attrition

### The Health Track

A player character's health is not a number — it is a list of **what they still have**.

| Slot type | Count | Description |
|---|---|---|
| **Equipment** | up to 10 | Weapons, armour, tools, and carried items |
| **Skills** | up to 10 | Trained abilities, techniques, and expertise |
| **Total** | up to 20 | All markable slots combined |

Each slot is either **intact** (available, functional) or **crossed off** (lost or broken for this encounter / session).  
**Destroyed** equipment is a special case — it is permanently removed from the character sheet entirely, not just crossed off.

A character with every slot crossed off is fully incapacitated. The GM determines whether this means unconscious, captured, or dead based on context.

---

### Attrition

Damage is dealt as **Attrition** — a category of harm that removes or degrades the character's resources rather than reducing a hit-point total.  
There are five grades, from least to most severe:

---

#### ① Basic Attrition
> *"You take a hit, but you choose how."*

**The player** crosses off any **1** slot of their choice — either one equipment or one skill.

This is the most controlled form of attrition: the player decides what they sacrifice to absorb the blow.

---

#### ② Flesh Attrition
> *"The wound is personal — your training falters."*

**Randomly select** 1 **skill** and cross it off.

Roll a die (or draw randomly) among all intact skill slots. That skill is lost.

---

#### ③ Equipment Attrition
> *"Your gear takes the punishment."*

**Randomly select** 1 **equipment** and cross it off.

Roll a die (or draw randomly) among all intact equipment slots. That item is broken or lost.

---

#### ④ Destroy Attrition
> *"Something is gone forever, and you're bleeding."*

Two things happen simultaneously:

1. **Randomly select** 1 **equipment** — it is **permanently destroyed** (remove it from the character sheet entirely; it cannot be recovered).
2. **Randomly select** 1 **skill** — cross it off.

This is the harshest single-hit attrition type. The destroyed equipment loss is absolute.

---

#### ⑤ Brutal Attrition
> *"Everything goes wrong at once."*

Roll **1d6**. Cross off that many slots, chosen **randomly** from *all* intact equipment and skills combined.

Distribute rolls across the combined pool (e.g. 10 intact equipment + 7 intact skills = 17-slot pool; roll 1d17 repeatedly without replacement until the 1d6 count is reached).

---

### Attrition Summary

| Type | Slots lost | How selected | Permanent? |
|---|---|---|---|
| **Basic** | 1 equipment *or* 1 skill | Player chooses | No |
| **Flesh** | 1 skill | Random | No |
| **Equipment** | 1 equipment | Random | No |
| **Destroy** | 1 equipment + 1 skill | Both random | Equipment yes; skill no |
| **Brutal** | 1d6 total (equipment + skills) | Random | No |

---

## 7. Drawback Refunds — 50 % Cap

Drawbacks refund points, making a spell cheaper but more restricted. Refunds are capped at **50 % of the spell's gross cost** (cost before any drawback credit).

This cap is enforced inside `Spell.TotalPoints` so it applies automatically everywhere.

---

## 8. Validation Summary

The **✦ Calculator** panel in the right pane shows:

- A live **Ring Mods** progress bar (`used / cap`)
- A live **Schools** count (`active / 3`)
- **✓ All limits OK** when no rules are violated
- **⚠ Warning** entries (in red) for each active violation

Warnings are informational — the app does not prevent saving or using a spell that breaks rules (important for loading legacy saves). The game master makes the final call.

---

## 9. Design Philosophy

These rules exist to keep SpellForge builds *interesting*, not to strangle creativity:

- **High point budgets** mean you can invest heavily in effects — the canvas should look *busy* and impressive at 40+ pts.
- **School and element limits** force choices and make synergies meaningful.
- **Ring mod cap** scales with investment, rewarding dedicated builds while preventing trivial spam.
- **No hard point ceiling** means Omnipotent-tier spells are possible — they just require extreme investment in actual spell content first.
