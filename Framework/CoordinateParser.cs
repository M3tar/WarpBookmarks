using WarpBookmarks.Models;

namespace WarpBookmarks.Framework;

/// <summary>Parses a location name followed by tile X/Y, accepting commas and parentheses.</summary>
internal static class CoordinateParser
{
    public static bool TryParse(string input, string currentLocationName, out LocationReference? location)
    {
        location = null;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        string normalized = input
            .Replace(',', ' ')
            .Replace('(', ' ')
            .Replace(')', ' ');
        string[] parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2
            || !int.TryParse(parts[^2], out int x)
            || !int.TryParse(parts[^1], out int y))
        {
            return false;
        }

        string locationName = parts.Length == 2
            ? currentLocationName
            : string.Join(" ", parts[..^2]);
        if (string.IsNullOrWhiteSpace(locationName))
            return false;

        location = new LocationReference
        {
            LocationName = locationName,
            DisplayName = locationName,
            TileX = x,
            TileY = y,
            FacingDirection = 2
        };
        return true;
    }
}
