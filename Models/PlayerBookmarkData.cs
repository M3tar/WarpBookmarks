namespace WarpBookmarks.Models;

/// <summary>Data stored on the current Farmer, so each multiplayer player has an independent list.</summary>
internal sealed class PlayerBookmarkData
{
    public int DataVersion { get; set; } = 1;

    public List<BookmarkRecord> Bookmarks { get; set; } = new();

    public HashSet<string> HiddenDefaultLocationIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> FavoriteDefaultLocationIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
