# Future GUI Data Usage

The GUI should treat local JSON data as the source of selectable options.

## Startup

1. Load `data/sigils/sigils.json`.
2. Load `data/traits/traits.json`.
3. Load `data/rules/secondary-trait-rules.json`.
4. Validate that referenced trait IDs and rule IDs exist.
5. Hide or disable entries with missing required data unless the GUI is in a dedicated experimental mode.

## Save Selection

The user should choose a save file manually. The program should write only to a separate output file. The input save remains unchanged and serves as the backup.

Avoid automatic Steam Cloud behavior. Do not edit the only copy of a save.

## Sigil Selection

When a sigil is selected:

- show only `allowedSigilLevels`
- if `allowedSigilLevels` is `null`, disable apply and show that data verification is missing
- set the primary trait from `primaryTraitId`
- show the first-trait level selector using that trait's real `maxLevel` or allowed level list

## Secondary Trait Selection

When choosing a secondary trait:

- if the sigil is not a + sigil or `supportsSecondaryTrait` is false, hide/disable the secondary selector
- for + sigils, show traits in the selected sigil's `allowedSecondaryTraitIds`
- hide traits listed in `disallowedSecondaryTraitIds`
- the current tool intentionally uses a broad secondary pool so offline impossible sigils can be created

## Trait Level Selection

Each trait has its own max level. The GUI must use the selected trait's data:

- primary trait level uses the primary trait's `maxLevel` or `allowedLevels`
- secondary trait level uses the selected secondary trait's `maxLevel`, `allowedLevels`, or a sigil-specific override
- never assume all traits use the same max level

## Final Validation Before Write

Before writing a save, validate:

- input path exists
- output path differs from input path
- selected sigil exists
- selected sigil level is allowed
- primary trait exists
- primary trait level is allowed
- secondary trait exists when required
- secondary trait is allowed for the selected + sigil
- secondary trait level is allowed for that specific trait
- all required save units are present

The GUI filter is convenience. Backend validation is the safety boundary.
