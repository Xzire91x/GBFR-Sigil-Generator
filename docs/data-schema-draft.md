# Draft Data Schema

The future GUI should load local JSON data rather than relying on hardcoded switch logic.

## Sigil Entry

Each sigil should support:

- `internalId`: game ID such as `GEEN_146_24`
- `hash`: save hash such as `0x00612B10`
- `displayName`: user-facing name
- `category`: sigil category/type when verified
- `isPlusSigil`: whether this is a + sigil
- `supportsSecondaryTrait`: whether this sigil can have a second trait
- `allowedSigilLevels`: exact allowed sigil levels, or `null` until verified
- `defaultSigilLevel`: default selected level
- `maxSigilLevel`: highest valid sigil level
- `primaryTraitId`: first trait ID
- `primaryTraitName`: first trait display name
- `firstTraitMaxLevel`: max level for the first trait
- `allowedFirstTraitLevels`: exact allowed first-trait levels, or range data later
- `defaultSecondaryTraitId`: normal paired second trait for combined character sigils, without locking the dropdown
- `defaultSecondaryTraitName`: display name for `defaultSecondaryTraitId`
- `allowedSecondaryTraitIds`: allow-list for known valid secondary traits
- `disallowedSecondaryTraitIds`: explicit deny-list for known invalid secondary traits
- `secondaryTraitLevelOverrides`: per-secondary-trait level limits
- `compatibilityRuleIds`: linked rule IDs from `data/rules/`
- `notes`, `source`, `confidence`: verification metadata

## Trait Entry

Each trait should support:

- `internalId`: game ID such as `SKILL_166_00`
- `hash`: save hash such as `0x48A95B8D`
- `displayName`: user-facing name
- `category`: trait category/type when verified
- `maxLevel`: true max level for that specific trait, or `null` until verified
- `allowedLevels`: exact allowed levels or range data
- `observedLevels`: levels seen in saves or generated tests
- `canAppearAsPrimary`: `true`, `false`, or `null`
- `canAppearAsSecondary`: `true`, `false`, or `null`
- `bannedAsSecondaryOnPlusSigils`: `true`, `false`, or `null`
- `notes`, `source`, `confidence`: verification metadata

Important: max level is per trait. The future GUI must not assume a global max level.

## Compatibility Rule Entry

Each rule should support:

- `id`: stable rule identifier
- `type`: for example `allow-secondary-trait` or `ban-secondary-trait`
- `sigilId`: affected sigil
- `primaryTraitId`: primary trait context
- `secondaryTraitId`: affected secondary trait
- `allowedSecondaryLevels`: level list/range when verified
- `notes`, `source`, `confidence`: verification metadata

## Unknown Values

Use these conventions until data is verified:

- `null`: unknown scalar value
- `[]`: known empty list or no verified entries yet
- `confidence: "low"`: written/tested but not verified as valid in game
- `confidence: "medium"`: observed in a save and supported by current tool behavior
- `confidence: "high"`: verified from game tables or multiple independent sources

Do not infer unknown max levels or compatibility from names alone.
