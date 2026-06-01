using System.Buffers.Binary;
using System.IO.Hashing;
using System.Windows.Forms;
using FlatSharp;
using GBFRSigilEditor.FlatBuffers;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
            return 0;
        }

        return RunCli(args);
    }

    static int RunCli(string[] args)
    {
        try
        {
            if (args.Contains("--remove-all-sigils", StringComparer.OrdinalIgnoreCase))
                return RunRemoveAllSigils(args);

            Cli cli = Cli.Parse(args);
            DataCatalog catalog = DataCatalog.LoadDefault();
            SigilData sigil = catalog.RequireSigilByName(cli.Primary);
            TraitData secondary = catalog.RequireTraitByName(cli.Secondary);

            int primaryTraitLevel = cli.PrimaryTraitLevel ?? PickSingleLevel(catalog.RequirePrimaryTraitLevels(sigil), "primary trait");
            int secondaryTraitLevel = cli.SecondaryTraitLevel ?? PickSingleLevel(catalog.RequireSecondaryTraitLevels(sigil, secondary), "secondary trait");

            var request = new EditBatchRequest(
                cli.Input,
                cli.Output ?? throw new ToolError("Missing --output."),
                [
                    new EditBatchEntry(
                        sigil.InternalId,
                        cli.Level,
                        primaryTraitLevel,
                        secondary.InternalId,
                        secondaryTraitLevel,
                        cli.Quantity)
                ]);

            if (cli.DryRun)
            {
                SaveEditorService.ValidateBatch(request, catalog);
                Console.WriteLine("Dry run: request validates against local data. No file written.");
                return 0;
            }

            EditBatchResult result = SaveEditorService.ApplyBatch(request, catalog);
            Console.WriteLine($"Added {result.CreatedSigils.Count} sigil(s):");
            foreach (CreatedSigilResult created in result.CreatedSigils)
            {
                Console.WriteLine($"  {created.Sigil.DisplayName}, level {created.SigilLevel}");
                Console.WriteLine($"    Primary trait: {created.PrimaryTrait.DisplayName}, level {created.PrimaryTraitLevel}");
                Console.WriteLine($"    Secondary trait: {created.SecondaryTrait?.DisplayName}, level {created.SecondaryTraitLevel}");
            }
            Console.WriteLine($"Output written: {result.OutputPath}");
            Console.WriteLine($"Verified {result.VerifiedSigils} created sigil(s) in the output save.");
            return 0;
        }
        catch (ToolError ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unexpected error: {ex.Message}");
            return 1;
        }
    }

    static int RunRemoveAllSigils(string[] args)
    {
        string? input = null;
        string? output = null;
        bool dryRun = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--remove-all-sigils":
                    break;
                case "--input":
                    input = Cli.ReadValue(args, ref i, "--input");
                    break;
                case "--output":
                    output = Cli.ReadValue(args, ref i, "--output");
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
                default:
                    throw new ToolError($"Unknown argument for --remove-all-sigils: {args[i]}");
            }
        }

        if (string.IsNullOrWhiteSpace(input))
            throw new ToolError("Missing --input.");
        if (string.IsNullOrWhiteSpace(output))
            throw new ToolError("Missing --output.");

        if (dryRun)
        {
            int count = SaveEditorService.CountOccupiedSigils(input);
            Console.WriteLine($"Dry run: found {count} occupied sigil slot(s). No file written.");
            return 0;
        }

        RemoveAllSigilsResult result = SaveEditorService.RemoveAllSigils(input, output);
        Console.WriteLine($"Removed {result.RemovedSigils} sigil(s).");
        Console.WriteLine($"Output written: {result.OutputPath}");
        Console.WriteLine($"Verified {result.RemainingSigils} occupied sigil slot(s) remain.");
        return 0;
    }

    static int PickSingleLevel(IReadOnlyList<int> levels, string label)
    {
        if (levels.Count == 1)
            return levels[0];
        throw new ToolError($"Specify a {label} level. The local data has {levels.Count} possible values.");
    }
}

sealed class Cli
{
    public required string Input { get; init; }
    public string? Output { get; init; }
    public required string Primary { get; init; }
    public required string Secondary { get; init; }
    public required int Level { get; init; }
    public int? PrimaryTraitLevel { get; init; }
    public int? SecondaryTraitLevel { get; init; }
    public int Quantity { get; init; } = 1;
    public bool DryRun { get; init; }

    public static Cli Parse(string[] args)
    {
        if (args.Length == 0)
            throw new ToolError(Usage);

        string? input = null;
        string? output = null;
        string? primary = null;
        string? secondary = null;
        int? level = null;
        int? primaryTraitLevel = null;
        int? secondaryTraitLevel = null;
        int quantity = 1;
        bool dryRun = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--input":
                    input = ReadValue(args, ref i, "--input");
                    break;
                case "--output":
                    output = ReadValue(args, ref i, "--output");
                    break;
                case "--primary":
                    primary = ReadValue(args, ref i, "--primary");
                    break;
                case "--secondary":
                    secondary = ReadValue(args, ref i, "--secondary");
                    break;
                case "--level":
                    string levelText = ReadValue(args, ref i, "--level");
                    if (!int.TryParse(levelText, out int parsedLevel))
                        throw new ToolError("--level must be a number.");
                    level = parsedLevel;
                    break;
                case "--primary-trait-level":
                    string primaryTraitLevelText = ReadValue(args, ref i, "--primary-trait-level");
                    if (!int.TryParse(primaryTraitLevelText, out int parsedPrimaryTraitLevel))
                        throw new ToolError("--primary-trait-level must be a number.");
                    primaryTraitLevel = parsedPrimaryTraitLevel;
                    break;
                case "--secondary-trait-level":
                    string secondaryTraitLevelText = ReadValue(args, ref i, "--secondary-trait-level");
                    if (!int.TryParse(secondaryTraitLevelText, out int parsedSecondaryTraitLevel))
                        throw new ToolError("--secondary-trait-level must be a number.");
                    secondaryTraitLevel = parsedSecondaryTraitLevel;
                    break;
                case "--quantity":
                    string quantityText = ReadValue(args, ref i, "--quantity");
                    if (!int.TryParse(quantityText, out int parsedQuantity) || parsedQuantity <= 0)
                        throw new ToolError("--quantity must be a positive number.");
                    quantity = parsedQuantity;
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
                default:
                    throw new ToolError($"Unknown argument: {args[i]}{Environment.NewLine}{Usage}");
            }
        }

        if (string.IsNullOrWhiteSpace(input))
            throw new ToolError($"Missing --input.{Environment.NewLine}{Usage}");
        if (string.IsNullOrWhiteSpace(primary))
            throw new ToolError($"Missing --primary.{Environment.NewLine}{Usage}");
        if (string.IsNullOrWhiteSpace(secondary))
            throw new ToolError($"Missing --secondary.{Environment.NewLine}{Usage}");
        if (level is null)
            throw new ToolError($"Missing --level.{Environment.NewLine}{Usage}");

        return new Cli
        {
            Input = input,
            Output = output,
            Primary = primary,
            Secondary = secondary,
            Level = level.Value,
            PrimaryTraitLevel = primaryTraitLevel,
            SecondaryTraitLevel = secondaryTraitLevel,
            Quantity = quantity,
            DryRun = dryRun,
        };
    }

    public static string ReadValue(string[] args, ref int i, string name)
    {
        if (i + 1 >= args.Length)
            throw new ToolError($"Missing value for {name}.");
        return args[++i];
    }

    const string Usage =
        "Usage: \"GBFR sigil generator.exe\" --input save.dat --output save_modified.dat --primary \"War Elemental+\" --secondary \"Greater Aegis\" --level 15 --primary-trait-level 15 --secondary-trait-level 30 [--quantity 3] [--dry-run]";
}

sealed class SaveFile
{
    public required byte[] FileBytes { get; init; }
    public required SaveDataBinary SlotData { get; init; }
    public required int SlotDataOffset { get; init; }
    public required int SlotDataSize { get; init; }

    Span<byte> SlotSpan => FileBytes.AsSpan(SlotDataOffset, SlotDataSize);

    public static SaveFile Read(string path)
    {
        byte[] fileBytes = File.ReadAllBytes(path);
        if (fileBytes.Length < 0x34)
            throw new ToolError("Save file is too small to contain a valid header.");

        long slotDataOffset = BinaryPrimitives.ReadInt64LittleEndian(fileBytes.AsSpan(0x1C));
        long slotDataSize = BinaryPrimitives.ReadInt64LittleEndian(fileBytes.AsSpan(0x2C));
        if (slotDataOffset < 0 || slotDataSize <= 0 || slotDataOffset + slotDataSize > fileBytes.Length)
            throw new ToolError("Save header has invalid slot-data offsets.");

        byte[] slotData = fileBytes.AsSpan(checked((int)slotDataOffset), checked((int)slotDataSize)).ToArray();
        return new SaveFile
        {
            FileBytes = fileBytes,
            SlotData = SaveDataBinary.Serializer.Parse(slotData.AsMemory()),
            SlotDataOffset = checked((int)slotDataOffset),
            SlotDataSize = checked((int)slotDataSize),
        };
    }

    public UIntSaveDataUnit FindEmptyGemUnit()
    {
        return FindEmptyGemUnits(1)[0];
    }

    public IReadOnlyList<UIntSaveDataUnit> FindEmptyGemUnits(int count)
    {
        var emptyGems = SlotData.UIntTable?
            .Where(x => x.IDType == Known.GemIdType && x.UnitID >= 30000 && x.ValueData is { Count: > 0 } && x.ValueData[0] == Known.EmptyHash)
            .OrderBy(x => x.UnitID)
            .Take(count)
            .ToArray()
            ?? [];

        if (emptyGems.Length < count)
            throw new ToolError($"Not enough empty sigil inventory slots. Needed {count}, found {emptyGems.Length}.");

        return emptyGems;
    }

    public IReadOnlyList<UIntSaveDataUnit> GetOccupiedGemUnits()
    {
        return SlotData.UIntTable?
            .Where(x => x.IDType == Known.GemIdType && x.UnitID >= 30000 && x.ValueData is { Count: > 0 } && x.ValueData[0] != Known.EmptyHash)
            .OrderBy(x => x.UnitID)
            .ToArray()
            ?? [];
    }

    public UIntSaveDataUnit RequireFirstUInt(uint idType, string label)
    {
        return SlotData.UIntTable?.FirstOrDefault(x => x.IDType == idType && x.ValueData is { Count: > 0 })
            ?? throw new ToolError($"Missing required save unit: {label} ({idType}).");
    }

    public UIntSaveDataUnit RequireUInt(uint idType, uint unitId, string label)
    {
        return SlotData.UIntTable?.FirstOrDefault(x => x.IDType == idType && x.UnitID == unitId && x.ValueData is { Count: > 0 })
            ?? throw new ToolError($"Missing required save unit: {label} ({idType}, unit {unitId}).");
    }

    public IntSaveDataUnit RequireInt(uint idType, uint unitId, string label)
    {
        return SlotData.IntTable?.FirstOrDefault(x => x.IDType == idType && x.UnitID == unitId && x.ValueData is { Count: > 0 })
            ?? throw new ToolError($"Missing required save unit: {label} ({idType}, unit {unitId}).");
    }

    public void PatchUInt(uint idType, uint unitId, uint value)
    {
        int offset = FindScalarVectorValueOffset(idType, unitId);
        BinaryPrimitives.WriteUInt32LittleEndian(SlotSpan[offset..], value);
    }

    public void PatchInt(uint idType, uint unitId, int value)
    {
        int offset = FindScalarVectorValueOffset(idType, unitId);
        BinaryPrimitives.WriteInt32LittleEndian(SlotSpan[offset..], value);
    }

    public void FixCurrentHash(IReadOnlyList<HashSectionInfo> sections)
    {
        UIntSaveDataUnit hashSeed = RequireFirstUInt(Known.HashSeedIdType, "SAVEDATA_HASHSEED");
        int idx = checked((int)(hashSeed.ValueData![0] % sections.Count));

        uint hashesOffset = BinaryPrimitives.ReadUInt32LittleEndian(SlotSpan[(SlotDataSize - 0x14)..]);
        if (hashesOffset + (sections.Count * 8) > SlotDataSize)
            throw new ToolError("Save hash table offset is outside slot data.");

        HashSectionInfo section = sections[idx];
        int hashStart = section.StartOffset;
        int hashLength = checked((int)hashesOffset - (section.StartOffset + section.SubSize));
        if (hashLength <= 0 || hashStart + hashLength > SlotDataSize)
            throw new ToolError("Save hash section is outside slot data.");

        ulong hash = XxHash64.HashToUInt64(SlotSpan.Slice(hashStart, hashLength), Known.SaveHashSeed);
        BinaryPrimitives.WriteUInt64LittleEndian(SlotSpan.Slice(checked((int)hashesOffset) + (idx * 8)), hash);
    }

    public void Write(string output)
    {
        string? directory = Path.GetDirectoryName(Path.GetFullPath(output));
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllBytes(output, FileBytes);
    }

    int FindScalarVectorValueOffset(uint idType, uint unitId)
    {
        Span<byte> slot = SlotSpan;
        for (int tableOffset = 4; tableOffset < slot.Length - 16; tableOffset += 4)
        {
            int? found = TryFindScalarVectorValueOffset(slot, tableOffset, idType, unitId);
            if (found is not null)
                return found.Value;
        }

        for (int tableOffset = 4; tableOffset < slot.Length - 16; tableOffset++)
        {
            int? found = TryFindScalarVectorValueOffset(slot, tableOffset, idType, unitId);
            if (found is not null)
                return found.Value;
        }

        throw new ToolError($"Could not locate raw save bytes for unit ({idType}, unit {unitId}).");
    }

    static int? TryFindScalarVectorValueOffset(Span<byte> slot, int tableOffset, uint idType, uint unitId)
    {
        int vtableDistance = BinaryPrimitives.ReadInt32LittleEndian(slot[tableOffset..]);
        if (vtableDistance == 0)
            return null;

        int[] candidates = [tableOffset - vtableDistance, tableOffset + vtableDistance];
        foreach (int vtableOffset in candidates)
        {
            if (vtableOffset < 0 || vtableOffset > slot.Length - 10)
                continue;

            ushort vtableSize = BinaryPrimitives.ReadUInt16LittleEndian(slot[vtableOffset..]);
            ushort objectSize = BinaryPrimitives.ReadUInt16LittleEndian(slot[(vtableOffset + 2)..]);
            if (vtableSize < 10 || objectSize < 4 || vtableSize > 64 || objectSize > slot.Length - tableOffset)
                continue;

            ushort idField = BinaryPrimitives.ReadUInt16LittleEndian(slot[(vtableOffset + 4)..]);
            ushort unitField = BinaryPrimitives.ReadUInt16LittleEndian(slot[(vtableOffset + 6)..]);
            ushort dataField = BinaryPrimitives.ReadUInt16LittleEndian(slot[(vtableOffset + 8)..]);
            if (idField == 0 || dataField == 0)
                continue;

            if (idField > slot.Length - tableOffset - 4 ||
                (unitField != 0 && unitField > slot.Length - tableOffset - 4) ||
                dataField > slot.Length - tableOffset - 4)
                continue;

            uint foundIdType = BinaryPrimitives.ReadUInt32LittleEndian(slot[(tableOffset + idField)..]);
            uint foundUnitId = unitField == 0 ? 0 : BinaryPrimitives.ReadUInt32LittleEndian(slot[(tableOffset + unitField)..]);
            if (foundIdType != idType || foundUnitId != unitId)
                continue;

            int vectorOffsetField = tableOffset + dataField;
            uint relativeVectorOffset = BinaryPrimitives.ReadUInt32LittleEndian(slot[vectorOffsetField..]);
            int vectorOffset = checked(vectorOffsetField + (int)relativeVectorOffset);
            if (vectorOffset < 0 || vectorOffset > slot.Length - 8)
                continue;

            int count = BinaryPrimitives.ReadInt32LittleEndian(slot[vectorOffset..]);
            if (count <= 0)
                continue;

            return vectorOffset + 4;
        }

        return null;
    }
}

sealed record HashSectionInfo(int StartOffset, int SubSize);

sealed record SigilSpec(
    string PrimaryName,
    string PrimaryGemCode,
    uint PrimaryGemHash,
    string PrimaryTraitName,
    string PrimaryTraitCode,
    uint PrimaryTraitHash,
    int PrimaryTraitLevel,
    string SecondaryName,
    string SecondaryTraitCode,
    uint SecondaryTraitHash,
    int SecondaryTraitLevel,
    int SigilLevel);

static class Known
{
    public const string PrimaryName = "Dark Huntress's Warpath+";
    public const string SecondaryName = "Autorevive";
    public const string WarElementalPlusName = "War Elemental+";
    public const string GreaterAegisName = "Greater Aegis";

    public const uint HashSeedIdType = 1003;
    public const uint TraitHashIdType = 1701;
    public const uint TraitLevelIdType = 1702;
    public const uint GemMaxSlotIdType = 2701;
    public const uint GemSlotIdType = 2702;
    public const uint GemIdType = 2703;
    public const uint GemLevelIdType = 2704;
    public const uint GemWornByIdType = 2706;
    public const uint GemFlagsIdType = 2707;

    public const uint EmptyHash = 0x887AE0B0;
    public const uint PrimaryGemHash = 0xAD8CAEFB;
    public const uint PrimaryTraitHash = 0x81B293D9;
    public const uint SecondaryTraitHash = 0x95F3FA86;
    public const uint WarElementalPlusGemHash = 0x00612B10;
    public const uint WarElementalTraitHash = 0x4C588C27;
    public const uint GreaterAegisTraitHash = 0x48A95B8D;
    public const uint NormalSigilFlags = 2;
    public const long SaveHashSeed = 0x2F1A43EBCD;
}

sealed class ToolError(string message) : Exception(message);
