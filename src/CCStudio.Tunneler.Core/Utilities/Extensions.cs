namespace CCStudio.Tunneler.Core.Utilities;

/// <summary>
/// Extension methods for various types
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Check if a string matches a wildcard pattern
    /// </summary>
    public static bool MatchesWildcard(this string text, string pattern)
    {
        if (string.IsNullOrEmpty(pattern) || pattern == "*")
            return true;

        if (string.IsNullOrEmpty(text))
            return false;

        // Convert wildcard pattern to regex
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        return System.Text.RegularExpressions.Regex.IsMatch(text, regexPattern,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Check if a tag matches any filter in a list
    /// </summary>
    public static bool MatchesAnyFilter(this string tagName, IEnumerable<string> filters)
    {
        if (filters == null || !filters.Any())
            return true;

        return filters.Any(filter => tagName.MatchesWildcard(filter));
    }

    /// <summary>
    /// Truncate string to maximum length
    /// </summary>
    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }

    /// <summary>
    /// Get friendly time span string
    /// </summary>
    public static string ToFriendlyString(this TimeSpan timeSpan)
    {
        if (timeSpan.TotalSeconds < 60)
            return $"{timeSpan.Seconds}s";

        if (timeSpan.TotalMinutes < 60)
            return $"{timeSpan.Minutes}m {timeSpan.Seconds}s";

        if (timeSpan.TotalHours < 24)
            return $"{timeSpan.Hours}h {timeSpan.Minutes}m";

        return $"{timeSpan.Days}d {timeSpan.Hours}h {timeSpan.Minutes}m";
    }

    /// <summary>
    /// Convert bytes to human readable format
    /// </summary>
    public static string ToHumanReadableSize(this long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}
