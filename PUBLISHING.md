# Publishing

Do not commit built executables or release zips to the repository.

Keep the repository source-only:

- C# source files
- project files
- local JSON data files
- documentation
- GitHub workflow files

Do not commit:

- `bin/`
- `obj/`
- `publish/`
- `.exe`
- `.zip`
- save files such as `SaveData1.dat`

## Create a local release package

```powershell
dotnet publish .\GBFRSigilEditor.csproj -c Release -r win-x64 --self-contained true -o ".\publish\GBFR sigil generator"
Compress-Archive -Path ".\publish\GBFR sigil generator\*" -DestinationPath ".\GBFR-sigil-generator-v0.1.0-win-x64.zip" -Force
```

Upload the zip to a GitHub Release instead of committing it to Git.

## Create an automatic GitHub Release

After the source has been pushed, create and push a version tag:

```powershell
git tag v0.1.0
git push origin v0.1.0
```

The included GitHub Actions workflow builds the Windows package and attaches it to a GitHub Release.

## Why releases instead of committing the exe?

GitHub web uploads reject files larger than 25 MiB. Git itself allows larger files, but committing large build outputs makes the repository slower and harder to maintain. Release assets are the correct place for built downloadable programs.
