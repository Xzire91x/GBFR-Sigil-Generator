# Invalid Secondary Traits on + Sigils

This is the rule area that needs the most verification before a GUI should expose broad selections.

## Current Status

There is no complete verified local list of invalid secondary traits yet.

The current command-line tool does not discover compatibility at runtime. It only supports a couple of explicitly allowed test combinations. That is safer than showing every trait as selectable.

## Future Rule Source

GBFRDataTools shows that `gem.headers` includes these relevant fields:

- `SkillId1`
- `SkillId2`
- `SkillTypeLotIdForRandom2ndSkill`

The likely next step is to use the real `gem` table plus related skill lot tables to determine which secondary traits can roll on each + sigil.

Do not infer compatibility from display names alone.

## GUI Filtering Rule

Until a complete rule table is verified:

1. If a selected sigil has an explicit `allowedSecondaryTraitIds` list, show only those traits.
2. If a selected sigil has no verified allowed list, do not offer a broad secondary trait dropdown.
3. If a trait appears in `disallowedSecondaryTraitIds`, hide it even if it appears elsewhere.
4. The backend must run the same validation before writing.

## Data File

The machine-readable draft rule file is:

```text
data/rules/secondary-trait-rules.json
```

The `invalidSecondaryTraitsOnPlusSigils` array is currently empty because no complete invalid list has been verified.

## TODO

- Extract real secondary trait pools from game data.
- Confirm whether the rules are allow-list based, ban-list based, or both.
- Add explicit invalid secondary traits only after verification.
- Keep source and confidence metadata for each rule.
