# Final Data Audit

Date: 2026-06-05

## Sources Checked

- `GBFRDataTools-master/GBFRDataTools.Database/Data/ids.txt`
- `GBFRDataTools-master/GBFRQuestClearChecker/csv_data/item_id.csv`
- `C:/Reloaded/Mods/gbfr.op.sigils.and.same.stat.for.dlc.weapon/GBFR/data/system/table/skill_status.tbl`

## Result

- Sigils in current pool: `159`
- Traits in current pool: `137`
- Missing sigil hashes: `0`
- Missing trait max levels: `0`
- Duplicate sigil IDs: `0`
- Duplicate sigil display names: `0`
- Duplicate trait IDs: `0`
- Dangling primary trait references: `0`
- Dangling secondary trait references: `0`
- Trait max-level mismatches against local `skill_status.tbl`: `0`
- Sigil hash mismatches against local `ids.txt`: `0`
- Trait hash mismatches against local `ids.txt`: `0`

## Corrections Made

- Filled missing sigil hashes for `Crabmiration` and `Auto Potion`.
- Filled missing trait max levels for:
  - `Fortifying Vigor`: `50`
  - `Instilling Vigor`: `50`
  - `Gilding Vigor`: `50`
  - `Seven Net`: `6`
  - `Stronghold`: `30`
  - `Berserker`: `30`
  - `Flight over Fight`: `15`
  - `Crabmiration`: `45`
  - `Auto Potion`: `15`
- Corrected `Crabvestment Returns` from `SKILL_141_04` to the actual skill row `SKILL_141_00`, with max level `15`.
- Added `Improved Healing V+` / `SKILL_065_00` with max level `30`.
- Added `Regen V+` / `SKILL_066_00` with max level `45`.

## Notes

The current secondary trait pool is intentionally broad so the tool can create offline impossible sigil combinations. This audit verifies IDs, hashes, references, and trait level caps. It does not restore strict in-game drop-generation legality.

