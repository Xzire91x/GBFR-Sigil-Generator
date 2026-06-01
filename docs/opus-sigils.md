# Opus Sigils

`Alpha+`, `Beta+`, and `Gamma+` are not present in the local `GBFRDataTools-master/GBFRQuestClearChecker/csv_data/item_id.csv`, but they are present in the current Nenkai Sigil/Gem ID list.

Added sigils:

- `GEEN_160_04` / `0x921D90D8` / `Alpha+`
- `GEEN_161_04` / `0xEE337FE3` / `Beta+`
- `GEEN_162_04` / `0x4438676E` / `Gamma+`

Added primary traits:

- `SKILL_160_00` / `0xDBE1D775` / `Alpha`
- `SKILL_161_00` / `0x8D2ADB6E` / `Beta`
- `SKILL_162_00` / `0x5C862E13` / `Gamma`

Temporary behavior:

- Sigil level is fixed to `15`, matching the current temporary sigil-level rule.
- Primary trait max level is set to `30` from public Opus sigil references.
- Secondary trait defaults to `DMG Cap` (`SKILL_020_00`), matching the normal Opus sigil behavior.
- The editor intentionally allows overriding that secondary trait with any known trait for offline/local impossible-sigil testing.
- The selected secondary trait level remains limited by that trait's max-level data.

The local Reloaded mod `gbfr.op.sigils.and.same.stat.for.dlc.weapon` was checked. It contains `skill_status.tbl`, `weapon_status.tbl`, and `weapon_status_awake.tbl`, but no `gem.tbl` or plain-text `Alpha+` / `Beta+` / `Gamma+` ID entries.
