using System.Text.Json;
using System.Text.Json.Serialization;

sealed class DataCatalog
{
    public required IReadOnlyList<SigilData> Sigils { get; init; }
    public required IReadOnlyList<TraitData> Traits { get; init; }
    public required IReadOnlyList<CompatibilityRule> Rules { get; init; }

    readonly Dictionary<string, SigilData> _sigilsById = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, TraitData> _traitsById = new(StringComparer.OrdinalIgnoreCase);

    public static DataCatalog LoadDefault()
    {
        string baseDir = AppContext.BaseDirectory;
        string dataDir = Directory.Exists(Path.Combine(baseDir, "data"))
            ? Path.Combine(baseDir, "data")
            : Path.Combine(Directory.GetCurrentDirectory(), "data");

        return Load(dataDir);
    }

    public static DataCatalog Load(string dataDir)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        string sigilsPath = PickDataPath(dataDir, "sigils.json", Path.Combine("sigils", "sigils.json"));
        string traitsPath = PickDataPath(dataDir, "traits.json", Path.Combine("traits", "traits.json"));
        string rulesPath = PickDataPath(dataDir, "secondary-trait-rules.json", Path.Combine("rules", "secondary-trait-rules.json"));

        var sigils = ReadJson<SigilFile>(sigilsPath, options).Sigils ?? [];
        var traits = ReadJson<TraitFile>(traitsPath, options).Traits ?? [];
        var rules = ReadJson<RuleFile>(rulesPath, options).Rules ?? [];

        var catalog = new DataCatalog { Sigils = sigils, Traits = traits, Rules = rules };
        foreach (var sigil in sigils)
            catalog._sigilsById[sigil.InternalId] = sigil;
        foreach (var trait in traits)
            catalog._traitsById[trait.InternalId] = trait;

        return catalog;
    }

    static string PickDataPath(string dataDir, string flatName, string nestedName)
    {
        string flatPath = Path.Combine(dataDir, flatName);
        if (File.Exists(flatPath))
            return flatPath;

        return Path.Combine(dataDir, nestedName);
    }

    static T ReadJson<T>(string path, JsonSerializerOptions options)
    {
        if (!File.Exists(path))
            throw new ToolError($"Missing data file: {path}");

        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), options)
            ?? throw new ToolError($"Could not parse data file: {path}");
    }

    public SigilData RequireSigil(string id)
    {
        return _sigilsById.TryGetValue(id, out var sigil)
            ? sigil
            : throw new ToolError($"Unknown sigil ID in local data: {id}");
    }

    public TraitData RequireTrait(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ToolError("Missing verified trait ID in local data.");

        return _traitsById.TryGetValue(id, out var trait)
            ? trait
            : throw new ToolError($"Unknown trait ID in local data: {id}");
    }

    public SigilData RequireSigilByName(string name)
    {
        return Sigils.FirstOrDefault(x => x.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? throw new ToolError($"Unknown sigil name in local data: {name}");
    }

    public TraitData RequireTraitByName(string name)
    {
        return Traits.FirstOrDefault(x => x.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? throw new ToolError($"Unknown trait name in local data: {name}");
    }

    public IReadOnlyList<TraitData> GetAllowedSecondaryTraits(SigilData sigil)
    {
        if (sigil.SupportsSecondaryTrait != true)
            return [];

        var disallowed = new HashSet<string>(sigil.DisallowedSecondaryTraitIds ?? [], StringComparer.OrdinalIgnoreCase);
        return (sigil.AllowedSecondaryTraitIds ?? [])
            .Where(id => !disallowed.Contains(id))
            .Select(RequireTrait)
            .Where(t => t.CanAppearAsSecondary != false && t.BannedAsSecondaryOnPlusSigils != true)
            .ToArray();
    }

    public IReadOnlyList<int> RequireSigilLevels(SigilData sigil)
    {
        if (sigil.AllowedSigilLevels is { Count: > 0 })
            return sigil.AllowedSigilLevels;
        if (sigil.MaxSigilLevel is int maxLevel)
            return Enumerable.Range(1, maxLevel).ToArray();
        throw new ToolError($"Missing verified sigil levels for {sigil.DisplayName}.");
    }

    public IReadOnlyList<int> RequirePrimaryTraitLevels(SigilData sigil)
    {
        if (sigil.AllowedFirstTraitLevels is { Count: > 0 })
            return sigil.AllowedFirstTraitLevels;
        if (sigil.FirstTraitMaxLevel is int sigilTraitMax)
            return Enumerable.Range(1, sigilTraitMax).ToArray();
        if (string.IsNullOrWhiteSpace(sigil.PrimaryTraitId))
            throw new ToolError($"Missing verified primary trait data for {sigil.DisplayName}.");

        var trait = RequireTrait(sigil.PrimaryTraitId);
        return RequireTraitLevels(trait, $"primary trait {trait.DisplayName}");
    }

    public IReadOnlyList<int> RequireSecondaryTraitLevels(SigilData sigil, TraitData trait)
    {
        if (sigil.SecondaryTraitLevelOverrides is not null &&
            sigil.SecondaryTraitLevelOverrides.TryGetValue(trait.InternalId, out var levelOverride))
        {
            if (levelOverride.AllowedLevels is { Count: > 0 })
                return levelOverride.AllowedLevels;
            if (levelOverride.MaxLevel is int overrideMax)
                return Enumerable.Range(1, overrideMax).ToArray();
        }

        return RequireTraitLevels(trait, $"secondary trait {trait.DisplayName}");
    }

    static IReadOnlyList<int> RequireTraitLevels(TraitData trait, string label)
    {
        if (trait.AllowedLevels is { Count: > 0 })
            return trait.AllowedLevels;
        if (trait.MaxLevel is int maxLevel)
            return Enumerable.Range(1, maxLevel).ToArray();
        throw new ToolError($"Missing verified max level or allowed levels for {label}. Update data/traits/traits.json before applying.");
    }
}

sealed class SigilFile
{
    public List<SigilData>? Sigils { get; init; }
}

sealed class TraitFile
{
    public List<TraitData>? Traits { get; init; }
}

sealed class RuleFile
{
    public List<CompatibilityRule>? Rules { get; init; }
}

sealed class SigilData
{
    public required string InternalId { get; init; }
    public string? Hash { get; init; }
    public required string DisplayName { get; init; }
    public string? Category { get; init; }
    public bool? IsPlusSigil { get; init; }
    public bool? SupportsSecondaryTrait { get; init; }
    public List<int>? AllowedSigilLevels { get; init; }
    public int? DefaultSigilLevel { get; init; }
    public int? MaxSigilLevel { get; init; }
    public string? PrimaryTraitId { get; init; }
    public string? PrimaryTraitName { get; init; }
    public int? FirstTraitMaxLevel { get; init; }
    public List<int>? AllowedFirstTraitLevels { get; init; }
    public List<string>? AllowedSecondaryTraitIds { get; init; }
    public List<string>? DisallowedSecondaryTraitIds { get; init; }
    public string? DefaultSecondaryTraitId { get; init; }
    public string? DefaultSecondaryTraitName { get; init; }
    public Dictionary<string, TraitLevelOverride>? SecondaryTraitLevelOverrides { get; init; }

    public override string ToString() => $"{DisplayName} ({InternalId})";
}

sealed class TraitData
{
    public required string InternalId { get; init; }
    public required string Hash { get; init; }
    public required string DisplayName { get; init; }
    public string? Category { get; init; }
    public int? MaxLevel { get; init; }
    public List<int>? AllowedLevels { get; init; }
    public List<int>? ObservedLevels { get; init; }
    public bool? CanAppearAsPrimary { get; init; }
    public bool? CanAppearAsSecondary { get; init; }
    public bool? BannedAsSecondaryOnPlusSigils { get; init; }

    public override string ToString() => DisplayName;
}

sealed class TraitLevelOverride
{
    public int? MaxLevel { get; init; }
    public List<int>? AllowedLevels { get; init; }
    public List<int>? ObservedLevels { get; init; }
}

sealed class CompatibilityRule
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public string? SigilId { get; init; }
    public string? PrimaryTraitId { get; init; }
    public string? SecondaryTraitId { get; init; }
    public List<int>? AllowedSecondaryLevels { get; init; }
}
