namespace SporeMaster.Core.Presentation;

/// <summary>
/// Searches resources by their <b>contents</b> (text or hex), as opposed to the name filter. Content
/// search reads resource bytes, so it is bounded by <c>maxScan</c> (how many resources to read) and
/// <c>maxResults</c> (how many matches to collect) to stay responsive.
/// </summary>
public static class ResourceContentSearch
{
    private const int DefaultMaxScan = 4000;
    private const int DefaultMaxResults = 500;

    /// <summary>The outcome of a content search.</summary>
    public sealed class Result
    {
        public List<ResourceItem> Matches { get; } = new();

        /// <summary>Number of resources actually read/checked.</summary>
        public int Scanned { get; set; }

        /// <summary>True when the scan stopped early because a limit was reached.</summary>
        public bool Truncated { get; set; }
    }

    public static Result Search(
        IEnumerable<ResourceItem> items,
        Func<ResourceItem, byte[]?> read,
        NameFilter nameFilter,
        ContentQuery content,
        int maxScan = DefaultMaxScan,
        int maxResults = DefaultMaxResults)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(read);
        ArgumentNullException.ThrowIfNull(nameFilter);
        ArgumentNullException.ThrowIfNull(content);

        var result = new Result();
        if (!content.HasContent)
        {
            return result;
        }

        foreach (ResourceItem item in items)
        {
            if (!nameFilter.IsEmpty && !item.Matches(nameFilter))
            {
                continue;
            }

            if (result.Scanned >= maxScan)
            {
                result.Truncated = true;
                break;
            }

            byte[]? data;
            try
            {
                data = read(item);
            }
            catch
            {
                continue;
            }

            result.Scanned++;
            if (data is not null && content.Matches(data))
            {
                result.Matches.Add(item);
                if (result.Matches.Count >= maxResults)
                {
                    result.Truncated = true;
                    break;
                }
            }
        }

        return result;
    }
}
