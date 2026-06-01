using System.Globalization;
using GBFRSigilEditor.FlatBuffers;

sealed record EditRequest(
    string InputPath,
    string OutputPath,
    string SigilId,
    int SigilLevel,
    int PrimaryTraitLevel,
    string? SecondaryTraitId,
    int? SecondaryTraitLevel);

sealed record EditBatchEntry(
    string SigilId,
    int SigilLevel,
    int PrimaryTraitLevel,
    string? SecondaryTraitId,
    int? SecondaryTraitLevel,
    int Quantity);

sealed record EditBatchRequest(
    string InputPath,
    string OutputPath,
    IReadOnlyList<EditBatchEntry> Entries);

sealed record EditResult(
    string OutputPath,
    uint SaveUnit,
    uint SlotId,
    SigilData Sigil,
    TraitData PrimaryTrait,
    TraitData? SecondaryTrait,
    int PrimaryTraitLevel,
    int? SecondaryTraitLevel,
    int VerifiedSigils);

sealed record CreatedSigilResult(
    uint SaveUnit,
    uint SlotId,
    SigilData Sigil,
    TraitData PrimaryTrait,
    TraitData? SecondaryTrait,
    int SigilLevel,
    int PrimaryTraitLevel,
    int? SecondaryTraitLevel);

sealed record EditBatchResult(
    string OutputPath,
    IReadOnlyList<CreatedSigilResult> CreatedSigils,
    int VerifiedSigils);

sealed record RemoveAllSigilsResult(
    string OutputPath,
    int RemovedSigils,
    int RemainingSigils);

static class SaveEditorService
{
    static readonly HashSectionInfo[] HashSections =
    [
        new(0x58, 0x80),
        new(0x30, 0xA0),
        new(0x28, 0x30),
        new(0x38, 0xC0),
        new(0x40, 0xB0),
        new(0x68, 0x50),
        new(0x48, 0x60),
        new(0x70, 0x90),
        new(0x50, 0x40),
        new(0x60, 0x70),
    ];

    public static void Validate(EditRequest request, DataCatalog catalog)
    {
        ValidateBatch(ToBatchRequest(request), catalog);
    }

    public static void ValidateBatch(EditBatchRequest request, DataCatalog catalog)
    {
        if (!File.Exists(request.InputPath))
            throw new ToolError($"Input save not found: {request.InputPath}");

        if (string.IsNullOrWhiteSpace(request.OutputPath))
            throw new ToolError("Choose an output save path.");

        string inputFullPath = Path.GetFullPath(request.InputPath);
        string outputFullPath = Path.GetFullPath(request.OutputPath);
        if (string.Equals(inputFullPath, outputFullPath, StringComparison.OrdinalIgnoreCase))
            throw new ToolError("Output path must be different from the input save path.");

        if (request.Entries.Count == 0)
            throw new ToolError("Add at least one sigil to create.");

        foreach (EditBatchEntry entry in request.Entries)
            ValidateEntry(entry, catalog);
    }

    public static void ValidateEntry(EditBatchEntry entry, DataCatalog catalog)
    {
        if (entry.Quantity <= 0)
            throw new ToolError("Quantity must be at least 1.");

        SigilData sigil = catalog.RequireSigil(entry.SigilId);
        TraitData primaryTrait = catalog.RequireTrait(sigil.PrimaryTraitId);

        if (string.IsNullOrWhiteSpace(sigil.Hash))
            throw new ToolError($"Missing verified item hash for {sigil.DisplayName}.");

        if (sigil.SupportsSecondaryTrait is null)
            throw new ToolError($"Missing verified secondary-trait support rules for {sigil.DisplayName}.");

        if (!catalog.RequireSigilLevels(sigil).Contains(entry.SigilLevel))
            throw new ToolError($"{sigil.DisplayName} does not allow sigil level {entry.SigilLevel}.");

        if (!catalog.RequirePrimaryTraitLevels(sigil).Contains(entry.PrimaryTraitLevel))
            throw new ToolError($"{primaryTrait.DisplayName} does not allow level {entry.PrimaryTraitLevel}.");

        if (sigil.SupportsSecondaryTrait == true)
        {
            if (string.IsNullOrWhiteSpace(entry.SecondaryTraitId))
                throw new ToolError($"{sigil.DisplayName} requires a secondary trait selection.");

            var allowedSecondary = catalog.GetAllowedSecondaryTraits(sigil);
            TraitData secondaryTrait = allowedSecondary.FirstOrDefault(t => t.InternalId.Equals(entry.SecondaryTraitId, StringComparison.OrdinalIgnoreCase))
                ?? throw new ToolError($"{entry.SecondaryTraitId} is not a verified secondary trait for {sigil.DisplayName}.");

            if (entry.SecondaryTraitLevel is null)
                throw new ToolError("Choose a secondary trait level.");

            if (!catalog.RequireSecondaryTraitLevels(sigil, secondaryTrait).Contains(entry.SecondaryTraitLevel.Value))
                throw new ToolError($"{secondaryTrait.DisplayName} does not allow level {entry.SecondaryTraitLevel.Value}.");
        }
        else if (!string.IsNullOrWhiteSpace(entry.SecondaryTraitId))
        {
            throw new ToolError($"{sigil.DisplayName} does not support a secondary trait.");
        }
    }

    public static EditResult Apply(EditRequest request, DataCatalog catalog)
    {
        EditBatchResult batchResult = ApplyBatch(ToBatchRequest(request), catalog);
        CreatedSigilResult first = batchResult.CreatedSigils[0];

        return new EditResult(
            batchResult.OutputPath,
            first.SaveUnit,
            first.SlotId,
            first.Sigil,
            first.PrimaryTrait,
            first.SecondaryTrait,
            first.PrimaryTraitLevel,
            first.SecondaryTraitLevel,
            batchResult.VerifiedSigils);
    }

    public static EditBatchResult ApplyBatch(EditBatchRequest request, DataCatalog catalog)
    {
        ValidateBatch(request, catalog);

        var expandedEntries = ExpandEntries(request.Entries).ToArray();
        SaveFile save = SaveFile.Read(request.InputPath);
        var emptyGems = save.FindEmptyGemUnits(expandedEntries.Length);
        var maxSlot = save.RequireFirstUInt(Known.GemMaxSlotIdType, "GEMDATA_MAX_SLOT_ID");
        uint firstNewSlotId = checked(maxSlot.ValueData![0] + 1);

        for (int i = 0; i < expandedEntries.Length; i++)
            RequireWritableSigilUnits(save, emptyGems[i].UnitID, expandedEntries[i], catalog);

        var created = new List<CreatedSigilResult>(expandedEntries.Length);

        save.PatchUInt(maxSlot.IDType, maxSlot.UnitID, checked(firstNewSlotId + (uint)expandedEntries.Length - 1));

        for (int i = 0; i < expandedEntries.Length; i++)
        {
            EditBatchEntry entry = expandedEntries[i];
            SigilData sigil = catalog.RequireSigil(entry.SigilId);
            TraitData primaryTrait = catalog.RequireTrait(sigil.PrimaryTraitId);
            TraitData? secondaryTrait = string.IsNullOrWhiteSpace(entry.SecondaryTraitId)
                ? null
                : catalog.RequireTrait(entry.SecondaryTraitId);

            uint gemUnitId = emptyGems[i].UnitID;
            uint newSlotId = checked(firstNewSlotId + (uint)i);
            PatchSigil(save, gemUnitId, newSlotId, entry, sigil, primaryTrait, secondaryTrait);
            created.Add(new CreatedSigilResult(
                gemUnitId,
                newSlotId,
                sigil,
                primaryTrait,
                secondaryTrait,
                entry.SigilLevel,
                entry.PrimaryTraitLevel,
                entry.SecondaryTraitLevel));
        }

        save.FixCurrentHash(HashSections);
        save.Write(request.OutputPath);
        int verifiedSigils = VerifyCreatedSigils(request.OutputPath, created);

        return new EditBatchResult(
            Path.GetFullPath(request.OutputPath),
            created,
            verifiedSigils);
    }

    static EditBatchRequest ToBatchRequest(EditRequest request)
    {
        return new EditBatchRequest(
            request.InputPath,
            request.OutputPath,
            [
                new EditBatchEntry(
                    request.SigilId,
                    request.SigilLevel,
                    request.PrimaryTraitLevel,
                    request.SecondaryTraitId,
                    request.SecondaryTraitLevel,
                    1)
            ]);
    }

    static IEnumerable<EditBatchEntry> ExpandEntries(IReadOnlyList<EditBatchEntry> entries)
    {
        foreach (EditBatchEntry entry in entries)
        {
            for (int i = 0; i < entry.Quantity; i++)
                yield return entry with { Quantity = 1 };
        }
    }

    static void RequireWritableSigilUnits(SaveFile save, uint gemUnitId, EditBatchEntry entry, DataCatalog catalog)
    {
        SigilData sigil = catalog.RequireSigil(entry.SigilId);
        TraitData? secondaryTrait = string.IsNullOrWhiteSpace(entry.SecondaryTraitId)
            ? null
            : catalog.RequireTrait(entry.SecondaryTraitId);

        uint gemIndex = checked(gemUnitId - 30000);
        uint primaryTraitUnit = checked(120000000 + (gemIndex * 100));
        uint secondaryTraitUnit = checked(primaryTraitUnit + 1);

        save.RequireUInt(Known.GemSlotIdType, gemUnitId, "GEMDATA_SLOT_IDS");
        save.RequireUInt(Known.GemWornByIdType, gemUnitId, "GEMDATA_WORN_BY");
        save.RequireUInt(Known.GemFlagsIdType, gemUnitId, "GEMDATA_FLAGS");
        save.RequireInt(Known.GemLevelIdType, gemUnitId, "GEMDATA_SKILL_1_LEVEL");
        save.RequireUInt(Known.TraitHashIdType, primaryTraitUnit, "primary trait hash");
        save.RequireInt(Known.TraitLevelIdType, primaryTraitUnit, "primary trait level");
        if (secondaryTrait is not null)
        {
            save.RequireUInt(Known.TraitHashIdType, secondaryTraitUnit, "secondary trait hash");
            save.RequireInt(Known.TraitLevelIdType, secondaryTraitUnit, "secondary trait level");
        }
    }

    static void PatchSigil(
        SaveFile save,
        uint gemUnitId,
        uint newSlotId,
        EditBatchEntry entry,
        SigilData sigil,
        TraitData primaryTrait,
        TraitData? secondaryTrait)
    {
        uint gemIndex = checked(gemUnitId - 30000);
        uint primaryTraitUnit = checked(120000000 + (gemIndex * 100));
        uint secondaryTraitUnit = checked(primaryTraitUnit + 1);

        save.PatchUInt(Known.GemSlotIdType, gemUnitId, newSlotId);
        save.PatchUInt(Known.GemIdType, gemUnitId, ParseHexUInt(sigil.Hash, $"{sigil.DisplayName} item hash"));
        save.PatchInt(Known.GemLevelIdType, gemUnitId, entry.SigilLevel);
        save.PatchUInt(Known.GemWornByIdType, gemUnitId, Known.EmptyHash);
        save.PatchUInt(Known.GemFlagsIdType, gemUnitId, Known.NormalSigilFlags);
        save.PatchUInt(Known.TraitHashIdType, primaryTraitUnit, ParseHexUInt(primaryTrait.Hash, $"{primaryTrait.DisplayName} trait hash"));
        save.PatchInt(Known.TraitLevelIdType, primaryTraitUnit, entry.PrimaryTraitLevel);

        if (secondaryTrait is not null)
        {
            save.PatchUInt(Known.TraitHashIdType, secondaryTraitUnit, ParseHexUInt(secondaryTrait.Hash, $"{secondaryTrait.DisplayName} trait hash"));
            save.PatchInt(Known.TraitLevelIdType, secondaryTraitUnit, entry.SecondaryTraitLevel!.Value);
        }
    }

    public static int CountOccupiedSigils(string inputPath)
    {
        SaveFile save = SaveFile.Read(inputPath);
        return save.GetOccupiedGemUnits().Count;
    }

    public static RemoveAllSigilsResult RemoveAllSigils(string inputPath, string outputPath)
    {
        if (!File.Exists(inputPath))
            throw new ToolError($"Input save not found: {inputPath}");

        string inputFullPath = Path.GetFullPath(inputPath);
        string outputFullPath = Path.GetFullPath(outputPath);
        if (string.Equals(inputFullPath, outputFullPath, StringComparison.OrdinalIgnoreCase))
            throw new ToolError("Output path must be different from the input save path.");

        SaveFile save = SaveFile.Read(inputPath);
        var occupiedGems = save.GetOccupiedGemUnits();

        foreach (UIntSaveDataUnit gem in occupiedGems)
        {
            uint gemUnitId = gem.UnitID;
            uint gemIndex = checked(gemUnitId - 30000);
            uint primaryTraitUnit = checked(120000000 + (gemIndex * 100));
            uint secondaryTraitUnit = checked(primaryTraitUnit + 1);

            save.PatchUInt(Known.GemIdType, gemUnitId, Known.EmptyHash);
            save.PatchInt(Known.GemLevelIdType, gemUnitId, 0);
            save.PatchUInt(Known.GemWornByIdType, gemUnitId, Known.EmptyHash);
            save.PatchUInt(Known.GemFlagsIdType, gemUnitId, 0);
            save.PatchUInt(Known.TraitHashIdType, primaryTraitUnit, Known.EmptyHash);
            save.PatchInt(Known.TraitLevelIdType, primaryTraitUnit, 0);
            save.PatchUInt(Known.TraitHashIdType, secondaryTraitUnit, Known.EmptyHash);
            save.PatchInt(Known.TraitLevelIdType, secondaryTraitUnit, 0);
        }

        save.FixCurrentHash(HashSections);
        save.Write(outputPath);
        int remainingSigils = CountOccupiedSigils(outputPath);
        if (remainingSigils != 0)
            throw new ToolError($"Post-write verification failed: {remainingSigils} occupied sigil slot(s) remain in the output save.");

        return new RemoveAllSigilsResult(
            Path.GetFullPath(outputPath),
            occupiedGems.Count,
            remainingSigils);
    }

    static int VerifyCreatedSigils(string outputPath, IReadOnlyList<CreatedSigilResult> createdSigils)
    {
        SaveFile written = SaveFile.Read(outputPath);

        foreach (CreatedSigilResult created in createdSigils)
        {
            uint gemIndex = checked(created.SaveUnit - 30000);
            uint primaryTraitUnit = checked(120000000 + (gemIndex * 100));
            uint secondaryTraitUnit = checked(primaryTraitUnit + 1);

            Expect(
                written.RequireUInt(Known.GemSlotIdType, created.SaveUnit, "written sigil slot ID").ValueData![0],
                created.SlotId,
                $"{created.Sigil.DisplayName} slot ID");
            Expect(
                written.RequireUInt(Known.GemIdType, created.SaveUnit, "written sigil hash").ValueData![0],
                ParseHexUInt(created.Sigil.Hash, $"{created.Sigil.DisplayName} item hash"),
                $"{created.Sigil.DisplayName} item hash");
            Expect(
                written.RequireInt(Known.GemLevelIdType, created.SaveUnit, "written sigil level").ValueData![0],
                created.SigilLevel,
                $"{created.Sigil.DisplayName} sigil level");
            Expect(
                written.RequireUInt(Known.GemWornByIdType, created.SaveUnit, "written sigil equipped character").ValueData![0],
                Known.EmptyHash,
                $"{created.Sigil.DisplayName} equipped character");
            Expect(
                written.RequireUInt(Known.GemFlagsIdType, created.SaveUnit, "written sigil flags").ValueData![0],
                Known.NormalSigilFlags,
                $"{created.Sigil.DisplayName} flags");
            Expect(
                written.RequireUInt(Known.TraitHashIdType, primaryTraitUnit, "written primary trait hash").ValueData![0],
                ParseHexUInt(created.PrimaryTrait.Hash, $"{created.PrimaryTrait.DisplayName} trait hash"),
                $"{created.PrimaryTrait.DisplayName} primary trait hash");
            Expect(
                written.RequireInt(Known.TraitLevelIdType, primaryTraitUnit, "written primary trait level").ValueData![0],
                created.PrimaryTraitLevel,
                $"{created.PrimaryTrait.DisplayName} primary trait level");

            if (created.SecondaryTrait is not null)
            {
                Expect(
                    written.RequireUInt(Known.TraitHashIdType, secondaryTraitUnit, "written secondary trait hash").ValueData![0],
                    ParseHexUInt(created.SecondaryTrait.Hash, $"{created.SecondaryTrait.DisplayName} trait hash"),
                    $"{created.SecondaryTrait.DisplayName} secondary trait hash");
                Expect(
                    written.RequireInt(Known.TraitLevelIdType, secondaryTraitUnit, "written secondary trait level").ValueData![0],
                    created.SecondaryTraitLevel!.Value,
                    $"{created.SecondaryTrait.DisplayName} secondary trait level");
            }
        }

        return createdSigils.Count;
    }

    static void Expect(uint actual, uint expected, string label)
    {
        if (actual != expected)
            throw new ToolError($"Post-write verification failed for {label}: expected 0x{expected:X8}, found 0x{actual:X8}.");
    }

    static void Expect(int actual, int expected, string label)
    {
        if (actual != expected)
            throw new ToolError($"Post-write verification failed for {label}: expected {expected}, found {actual}.");
    }

    static uint ParseHexUInt(string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ToolError($"Missing verified {label}.");

        string text = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
        return uint.Parse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }
}
