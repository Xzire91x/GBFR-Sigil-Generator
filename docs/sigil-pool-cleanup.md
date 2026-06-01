# Sigil Pool Cleanup

The GUI sigil pool is intentionally smaller than the full public `GEEN_*` ID list.

Cleanup rules applied to `data/sigils/sigils.json`:

- Remove every sigil whose display name starts with `Dummy`.
- For numbered sigil families, keep only the final `V+` version.
- For unnumbered sigil families, keep the `+` version when it exists; otherwise keep the base name.
- If multiple remaining entries have the same display name, keep one entry only.
- Duplicate display names are collapsed by keeping the highest/latest `GEEN_xxx_yy` internal ID variant, normally the `_24` version over `_14`.
- The GUI orders sigils by the first three digits in `GEEN_xxx_yy`; the final two digits do not affect dropdown order.

This rule is a UI/data cleanup rule, not proof that lower ID variants are invalid game data. The full source list contains multiple internal records with the same visible name and different hashes. The GUI keeps one visible choice to avoid showing duplicate names to the user.
