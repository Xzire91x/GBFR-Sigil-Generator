# Base Trait Verification

This pass maps the current cleaned sigil pool to primary/base traits where the available sources support it.

Sources used:

- `GBFRDataTools-master/GBFRQuestClearChecker/csv_data/item_id.csv` for local `GEEN_*` sigil IDs and item hashes.
- Nenkai Trait/Skill IDs page and local `ids.txt` for `SKILL_*` trait IDs, display names, and hashes.
- Local `skill_status.tbl` for final trait max-level verification.
- `GBFRDataTools.Database/Headers/gem.headers` confirms the real game table has `SkillId1` and `SkillId2` columns, but the exported `gem.tbl` rows are not present in this workspace.

Current status:

- Total sigils in pool: 157.
- Sigils with mapped primary/base trait ID: 157.
- Sigils with mapped primary/base trait ID and verified max level: 157.
- Unresolved sigils: 0.
- Trait max-level mismatches against local `skill_status.tbl`: 0.

Support sigil update:

- Added missing final-tier support sigils: `Quick Cooldown V+`, `Cascade V+`, and `Uplift V+`.
- Their IDs and hashes come from local GBFRDataTools ID data.
- Their trait max levels are `45`, `20`, and `45`.
- Added missing defensive support sigils: `Guts V+` and `Autorevive V+`.
- Their trait max levels are both `20`.
- Added missing final-tier support sigil: `Nimble Onslaught V+`.
- `Nimble Onslaught` uses `SKILL_106_00` and max level `30`.
- Added missing upgraded support sigil: `Improved Dodge+`.
- The game data names the final upgraded entry `Improved Dodge+`, not `Improved Dodge V+`; it uses `SKILL_063_00` and max level `15`.
- Added missing upgraded support sigil: `Potion Hoarder+`.
- Verified `Potion Hoarder` and `Untouchable` max levels as `15` from local `skill_status.tbl`.
- Added missing upgraded support sigil: `Drain+`.
- `Drain` uses `SKILL_067_00` and max level `45`.
- Removed the four `SBA/Chain Burst Effect+` variants from the selectable pool because they remained unresolved/TODO and do not appear as normal in-game sigils.
- Removed `Enhanced Damage V+` and `SKILL_168_00` from the selectable pool after in-game testing showed a blank/nonfunctional trait.
- Removed `Finders Keepers` from the selectable pool because it had no verified primary trait and was the last unresolved/TODO entry.

Character sigil update:

- Character sigils now have verified primary trait IDs and max level 15.
- `Awakening+` / `Soul+` combined sigils now record their normal paired secondary trait in `defaultSecondaryTraitId`, but their selectable secondary pool remains broad for offline/local testing.
- Added the missing character entries found in the public ID lists: `Seven-Star Boundary+`, `Two-Crown Boundary+`, and Sandalphon's `Supreme Primarch*` / `Ain+` sigils.
- `SKILL_172_02` is used for `Supreme Primarch's Warpath` and `SKILL_172_03` is used for `Ain`, based on the local GBFRDataTools `ids.txt` table.

Conservative rule:

- Exact or documented alias name matches were written to `data/sigils/sigils.json`.
- Trait IDs/hashes were written to `data/traits/traits.json`.
- Max levels were written only when present in public data, confirmed by the user in-game, or verified from local `skill_status.tbl`.
- Ambiguous non-character entries are removed from the selectable pool until their primary trait data is verified.
- `Awakening+` / `Soul+` sigils are represented as primary trait plus `defaultSecondaryTraitId`; this records the normal game pairing without locking the offline test secondary dropdown.

The full per-sigil result is saved in `data/sigils/base-trait-mapping-report.json`.

Remaining data needed for exact completion:

- Exported `system/table/gem.tbl`, or a SQLite export created by GBFRDataTools, for exact `SkillId1` and `SkillId2` per sigil.
- Exported/verified data for any removed hidden or unresolved entries before re-adding them.
