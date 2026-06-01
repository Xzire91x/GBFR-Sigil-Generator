# Temporary Secondary Pool

This is a temporary working mode for testing the GUI and save writer.

Current behavior:

- Every sigil in `data/sigils/sigils.json` has `allowedSigilLevels` set to `[15]`.
- Every sigil has `supportsSecondaryTrait` set to `true`.
- Every sigil uses the same broad secondary pool: every trait currently listed in `data/traits/traits.json`.
- Trait level selection still comes from each trait's own `maxLevel` or `allowedLevels`.
- Traits with unknown max levels are intentionally not assigned guessed levels.

Important limitation:

This broad secondary pool is not final legality data. It exists so combinations can be tested while the missing `gem.tbl`, `skill_status.tbl`, and secondary-lot rule data are still being gathered. The final version should replace this with verified compatibility rules.
