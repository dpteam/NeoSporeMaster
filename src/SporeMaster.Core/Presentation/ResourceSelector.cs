using SporeMaster.Core.Hashing;
using SporeMaster.Core.Names;

namespace SporeMaster.Core.Presentation;

/// <summary>
/// A predicate that selects resources for listing or extraction. Supports choosing one or more
/// "folders" (groups), one or more types, and a name substring — the building blocks for selective
/// unpacking (a set of folders, or the whole package when nothing is specified).
/// </summary>
/// <remarks>
/// Group and type hashes are EA constants that are frequently <b>not</b> the FNV of their name, so a
/// token matches either the <b>resolved name</b> (from the registry) or an explicit hash literal
/// (<c>0x...</c> / <c>#...</c>), case-insensitively.
/// </remarks>
public sealed class ResourceSelector
{
    private readonly NameRegistrySet? _names;

    private ResourceSelector(NameRegistrySet? names)
    {
        _names = names;
    }

    /// <summary>Group (folder) tokens to include; empty means "any".</summary>
    public List<string> Groups { get; } = new();

    /// <summary>Type tokens to include; empty means "any".</summary>
    public List<string> Types { get; } = new();

    /// <summary>Case-insensitive substring the resolved path must contain; null means "any".</summary>
    public string? Name { get; set; }

    public bool IsEmpty => Groups.Count == 0 && Types.Count == 0 && string.IsNullOrEmpty(Name);

    /// <summary>Parses comma-separated type/group tokens (names or hash literals) and a name substring.</summary>
    public static ResourceSelector Parse(
        string? types,
        string? groups,
        string? name,
        NameRegistrySet names)
    {
        ArgumentNullException.ThrowIfNull(names);
        var selector = new ResourceSelector(names) { Name = string.IsNullOrWhiteSpace(name) ? null : name };

        foreach (string token in SplitTokens(types))
        {
            selector.Types.Add(token);
        }

        foreach (string token in SplitTokens(groups))
        {
            selector.Groups.Add(NormalizeFolder(token));
        }

        return selector;
    }

    /// <summary>True when a resource matches the selector.</summary>
    public bool Matches(uint typeId, uint groupId, string resolvedPath)
    {
        if (Types.Count > 0 && !MatchesAny(typeId, Types, isType: true))
        {
            return false;
        }

        if (Groups.Count > 0 && !MatchesAny(groupId, Groups, isType: false))
        {
            return false;
        }

        if (Name is not null && !resolvedPath.Contains(Name, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private bool MatchesAny(uint hash, List<string> tokens, bool isType)
    {
        string resolved = isType
            ? _names?.GetTypeName(hash) ?? Fnv.ToHexUpper(hash)
            : _names?.GetFileName(hash) ?? Fnv.ToHexUpper(hash);

        foreach (string token in tokens)
        {
            if (string.Equals(resolved, token, StringComparison.OrdinalIgnoreCase)
                || string.Equals(TrimTilde(resolved), TrimTilde(token), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            bool isLiteral = token.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || token.StartsWith('#');
            if (isLiteral && Fnv.TryParseLiteral(token, out uint literal) && literal == hash)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Group names often carry a trailing <c>~</c> alias marker; ignore it when matching.</summary>
    private static string TrimTilde(string value) => value.EndsWith('~') ? value[..^1] : value;

    private static IEnumerable<string> SplitTokens(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield break;
        }

        foreach (string part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part.Length > 0)
            {
                yield return part;
            }
        }
    }

    /// <summary>A folder token may be given as a path; the group is the first segment.</summary>
    private static string NormalizeFolder(string token)
    {
        string normalized = token.Replace('\\', '/').Trim('/');
        int slash = normalized.IndexOf('/');
        return slash >= 0 ? normalized[..slash] : normalized;
    }
}
