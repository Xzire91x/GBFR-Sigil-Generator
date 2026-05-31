# GBFR sigil generator

Minimal offline save-copy tool for adding sigils to a Granblue Fantasy: Relink save.

The tool never writes to the input save. It writes only to the selected output path, so keep the original save as your backup.

## Build

```powershell
dotnet build .\GBFRSigilEditor.csproj -c Release
```

The normal build executable is:

```text
bin\Release\net9.0-windows\GBFR sigil generator.exe
```

## Publish

For a clean folder to share:

```powershell
dotnet publish .\GBFRSigilEditor.csproj -c Release -r win-x64 --self-contained true -o ".\publish\GBFR sigil generator"
```

Distribute the whole publish folder, including the `data` folder. The program loads sigil and trait data from those JSON files.

## GUI

Run the executable with no arguments:

```powershell
& ".\bin\Release\net9.0-windows\GBFR sigil generator.exe"
```

Normal flow:

1. Select an input save.
2. Choose an output save path.
3. Select sigil, trait levels, secondary trait, secondary level, and quantity.
4. Click `Add` to queue more requests, or click `Apply` to write the output save.

If the queue is empty, `Apply` uses the current selection and quantity.

The `Remove all` button writes an output save with all existing sigils cleared. It shows an extra warning before running.

## CLI Examples

```powershell
& ".\bin\Release\net9.0-windows\GBFR sigil generator.exe" --input "SaveData1.dat" --output "SaveData1_modified.dat" --primary "Dark Huntress's Warpath+" --secondary "Autorevive" --level 15 --primary-trait-level 15 --secondary-trait-level 15
```

Quantity:

```powershell
& ".\bin\Release\net9.0-windows\GBFR sigil generator.exe" --input "SaveData1.dat" --output "SaveData1_modified.dat" --primary "Attack Power V+" --secondary "ATK" --level 15 --primary-trait-level 50 --secondary-trait-level 50 --quantity 3
```

Remove all sigils from the output copy:

```powershell
& ".\bin\Release\net9.0-windows\GBFR sigil generator.exe" --remove-all-sigils --input "SaveData1.dat" --output "SaveData1_no_sigils.dat"
```

Dry run:

```powershell
& ".\bin\Release\net9.0-windows\GBFR sigil generator.exe" --input "SaveData1.dat" --output "SaveData1_modified.dat" --primary "Dark Huntress's Warpath+" --secondary "Autorevive" --level 15 --primary-trait-level 15 --secondary-trait-level 15 --dry-run
```

## Data

Local data lives in:

- `data/sigils/sigils.json`
- `data/traits/traits.json`
- `data/rules/secondary-trait-rules.json`

Sigil and trait hashes are verified against local GBFRDataTools IDs. Trait max levels are verified against local `skill_status.tbl`. Secondary trait selection is intentionally broad for offline impossible-sigil creation.

## Save Fields

- `2701`: max sigil slot ID
- `2702`: sigil slot IDs
- `2703`: sigil item hashes
- `2704`: sigil level
- `2706`: equipped character hash
- `2707`: sigil flags
- `1701`: stored trait hashes
- `1702`: stored trait levels

After writing, the tool reopens the output save and verifies the sigil hashes, levels, trait hashes, trait levels, flags, and remove-all result.

## GitHub Release Downloads

The repository is intended to stay source-only. Built Windows packages should be attached to GitHub Releases rather than committed to Git.

To publish a tagged release after pushing the source:

```powershell
git tag v0.1.0
git push origin v0.1.0
```

The included GitHub Actions workflow will publish a Windows x64 zip for tags named `v*`.
