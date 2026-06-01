# Safety Notes

This project is for personal, offline save editing only.

## Save Handling

- Never edit the only copy of a save.
- Keep `--input` read-only.
- Write only to an explicit output path that differs from the input save.
- Keep the input save as the backup copy; the tool should not create extra backup files.
- Test with copied saves before using any output in game.

## Steam Cloud

- Avoid automatic Steam Cloud interactions.
- Do not write directly into the live save folder while the game or Steam Cloud sync may be active.
- Prefer writing to a workspace/output folder, then manually decide how to use the file.

## Scope Limits

Do not add:

- online or multiplayer behavior
- runtime memory editing
- game executable patching
- anti-cheat bypasses
- DRM bypasses
- server validation bypasses
- anything intended for online cheating

## Validation

The GUI may allow impossible in-game sigil combinations for offline testing. Backend validation should still reject missing IDs, missing hashes, invalid levels, same input/output paths, or unsupported save structure. Unknown data should be treated as unsupported, not guessed.
