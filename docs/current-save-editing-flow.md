# Current Save Editing Flow

This document describes the current command-line flow in `Program.cs`. It is not a GUI design.

## Input Save

The program receives the save path from `--input`. It reads the entire file into memory and never writes back to that path. The output path must be different from the input path.

The save header offsets currently used are:

- `0x1C`: slot-data offset
- `0x2C`: slot-data size

These offsets match the save parsing logic from GBFRDataTools.

## Save Parsing

`SaveDataBinary.fbs` defines the FlatBuffer tables used by the save:

- `IntTable`
- `UIntTable`
- other scalar tables that are currently parsed but not edited

The current editor uses `FlatSharp` to parse slot data into `SaveDataBinary`.

## Sigil Slot Selection

The editor finds the first empty sigil inventory unit where:

- ID type `2703`
- unit ID is `>= 30000`
- value is `0x887AE0B0`

`0x887AE0B0` is currently treated as the empty hash.

For a sigil unit, the paired trait units are derived with:

```text
gemIndex = gemUnitId - 30000
primaryTraitUnit = 120000000 + (gemIndex * 100)
secondaryTraitUnit = primaryTraitUnit + 1
```

## Values Written

The current writer updates these save units:

- `2701`: max sigil slot ID, incremented by one
- `2702`: sigil slot ID for the chosen empty unit
- `2703`: sigil item hash
- `2704`: sigil level
- `2706`: equipped character hash, currently written as empty/unequipped
- `2707`: sigil flags, currently written as `2`
- `1701`: primary and secondary trait hashes
- `1702`: primary and secondary trait levels

The important fix from the previous test is that displayed trait levels are stored in `1702`, not only in `2704`.

## Hash Repair

After patching raw save bytes, the editor recalculates the active save hash section and writes the corrected XXHash64 value. The hash seed unit is `1003`.

## Current Hardcoded Data

Current IDs and target levels are defined in `Program.cs`:

- `Known` static class: save unit IDs, empty hash, sigil hashes, trait hashes, normal flags, hash seed
- `ResolveSpec(...)`: supported primary/secondary combinations and target levels

Currently supported combinations:

- `Dark Huntress's Warpath+` + `Autorevive`
- `War Elemental+` + `Greater Aegis`

## Trait Max Levels

Authoritative trait max levels are not defined yet. Current code only stores target levels for specific tests.

For the future GUI, do not treat current target levels as verified max levels unless they are confirmed from `skill_status.tbl`, an exported database, or reliable in-game observation.

## Data the Future GUI Needs

The GUI should not keep expanding `Program.cs` hardcoded values. It needs local structured data for:

- all known sigils and hashes
- all known traits and hashes
- real max level per trait
- valid sigil levels per sigil
- valid primary trait levels
- valid secondary trait levels
- which traits can appear as secondary traits
- which secondary traits are invalid on each + sigil
- source and confidence for every rule

The draft data files under `data/` are the starting point for this.
