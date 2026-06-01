# Project Map

This maps the current code locations relevant to a future GUI.

## Current Source Files

| Area | Current Location | Notes |
| --- | --- | --- |
| CLI argument parsing | `Program.cs`, `Cli.Parse(...)` | Reads `--input`, `--output`, `--primary`, `--secondary`, `--level`, and `--dry-run`. |
| Supported sigil requests | `Program.cs`, `ResolveSpec(...)` | Hardcoded supported combinations and target trait levels. |
| Sigil and trait constants | `Program.cs`, `Known` | Save unit IDs, hashes, flags, and hash seed. |
| Save header offsets | `Program.cs`, `SaveFile.Read(...)` | Reads slot-data offset at `0x1C` and slot-data size at `0x2C`. |
| Save FlatBuffer schema | `SaveDataBinary.fbs` | Defines `SaveDataBinary`, `IntSaveDataUnit`, `UIntSaveDataUnit`, and other table types. |
| Save parsing | `Program.cs`, `SaveFile.Read(...)` | Uses `SaveDataBinary.Serializer.Parse(...)`. |
| Empty sigil search | `Program.cs`, `FindEmptyGemUnit(...)` | Finds first `2703` unit with empty hash `0x887AE0B0`. |
| Raw value patching | `Program.cs`, `PatchUInt(...)`, `PatchInt(...)`, `FindScalarVectorValueOffset(...)` | Finds and patches raw FlatBuffer scalar vector values. |
| Save hash repair | `Program.cs`, `FixCurrentHash(...)` | Recalculates active XXHash64 save hash. |
| Current README | `README.md` | Basic command-line usage and known IDs. |

## Save Unit IDs Currently Used

| ID Type | Meaning in Current Tool |
| --- | --- |
| `1003` | save hash seed |
| `1701` | trait hash storage |
| `1702` | trait level storage |
| `2701` | max sigil slot ID |
| `2702` | sigil slot IDs |
| `2703` | sigil item hashes |
| `2704` | sigil level |
| `2706` | equipped character hash |
| `2707` | sigil flags |

## Local Reference Files

| Reference | Location | Useful Fields |
| --- | --- | --- |
| GBFRDataTools save parser | `../GBFRDataTools-master/GBFRDataTools.SaveFile/SaveFile.cs` | Header offsets and save hash logic. |
| Save ID enum | `../GBFRDataTools-master/GBFRDataTools.SaveFile/SaveIDType.cs` | Known save unit IDs. |
| Gem table header | `../GBFRDataTools-master/GBFRDataTools.Database/Headers/gem.headers` | `SkillId1`, `SkillId2`, `SkillTypeLotIdForRandom2ndSkill`, rarity/category fields. |
| Skill status header | `../GBFRDataTools-master/GBFRDataTools.Database/Headers/skill_status.headers` | Level values and level keys. |
| Public item ID CSV | `../GBFRDataTools-master/GBFRQuestClearChecker/csv_data/item_id.csv` | Known `GEEN_*` item/sigil names and hashes. |

## Future Data Locations

| Data | Location |
| --- | --- |
| Draft schema | `data/schema.draft.json` |
| Sigil catalog | `data/sigils/sigils.json` |
| Trait catalog | `data/traits/traits.json` |
| Compatibility rules | `data/rules/secondary-trait-rules.json` |
