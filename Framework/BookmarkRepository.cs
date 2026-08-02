using System.Text.Json;
using StardewModdingAPI;
using StardewValley;
using WarpBookmarks.Models;

namespace WarpBookmarks.Framework;

/// <summary>Reads and writes the current player's independent bookmark data.</summary>
internal sealed class BookmarkRepository
{
    private const int MaxBookmarks = 200;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string dataKey;
    private readonly IMonitor monitor;
    private PlayerBookmarkData? cachedData;

    public BookmarkRepository(IManifest manifest, IMonitor monitor)
    {
        this.dataKey = $"{manifest.UniqueID}/player-data";
        this.monitor = monitor;
    }

    public PlayerBookmarkData GetData()
    {
        if (this.cachedData is not null)
            return this.cachedData;

        if (!Game1.player.modData.TryGetValue(this.dataKey, out string? json) || string.IsNullOrWhiteSpace(json))
            return this.cachedData = new PlayerBookmarkData();

        try
        {
            PlayerBookmarkData? data = JsonSerializer.Deserialize<PlayerBookmarkData>(json, JsonOptions);
            this.cachedData = data ?? new PlayerBookmarkData();
            this.cachedData.Bookmarks ??= new List<BookmarkRecord>();
            this.cachedData.HiddenDefaultLocationIds ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            this.cachedData.FavoriteDefaultLocationIds ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            this.cachedData.HiddenDefaultLocationIds = new HashSet<string>(this.cachedData.HiddenDefaultLocationIds, StringComparer.OrdinalIgnoreCase);
            this.cachedData.FavoriteDefaultLocationIds = new HashSet<string>(this.cachedData.FavoriteDefaultLocationIds, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            this.monitor.Log($"Couldn't read this player's bookmark data. The original value was left untouched. {ex}", LogLevel.Error);
            this.cachedData = new PlayerBookmarkData();
        }

        return this.cachedData;
    }

    public BookmarkRecord? AddCurrentLocation(string suggestedName, out string? error)
    {
        string locationName = Game1.currentLocation.NameOrUniqueName;
        if (string.IsNullOrWhiteSpace(locationName))
        {
            error = "location";
            return null;
        }

        return this.AddLocation(new LocationReference
        {
            LocationName = locationName,
            DisplayName = Game1.currentLocation.DisplayName,
            TileX = Game1.player.TilePoint.X,
            TileY = Game1.player.TilePoint.Y,
            FacingDirection = Game1.player.FacingDirection
        }, suggestedName, out error);
    }

    public BookmarkRecord? AddLocation(LocationReference location, string suggestedName, out string? error)
    {
        error = null;
        PlayerBookmarkData data = this.GetData();
        if (data.Bookmarks.Count >= MaxBookmarks)
        {
            error = "limit";
            return null;
        }
        if (string.IsNullOrWhiteSpace(location.LocationName))
        {
            error = "location";
            return null;
        }

        string name = MakeUniqueName(data.Bookmarks, suggestedName);
        BookmarkRecord bookmark = new()
        {
            Name = name,
            Location = location
        };
        data.Bookmarks.Add(bookmark);
        this.Save();
        return bookmark;
    }

    public void DeleteBookmark(string id)
    {
        PlayerBookmarkData data = this.GetData();
        if (data.Bookmarks.RemoveAll(bookmark => bookmark.Id == id) > 0)
            this.Save();
    }

    public void RenameBookmark(string id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        PlayerBookmarkData data = this.GetData();
        BookmarkRecord? bookmark = data.Bookmarks.FirstOrDefault(item => item.Id == id);
        if (bookmark is null)
            return;

        bookmark.Name = MakeUniqueName(data.Bookmarks.Where(item => item.Id != id), name.Trim());
        this.Save();
    }

    public void SetFavorite(WarpDestination destination, bool favorite)
    {
        PlayerBookmarkData data = this.GetData();
        if (destination.Kind == WarpDestinationKind.Bookmark)
        {
            BookmarkRecord? bookmark = data.Bookmarks.FirstOrDefault(item => item.Id == destination.Id);
            if (bookmark is not null)
                bookmark.IsFavorite = favorite;
        }
        else if (destination.Kind == WarpDestinationKind.Default)
        {
            if (favorite)
                data.FavoriteDefaultLocationIds.Add(destination.Id);
            else
                data.FavoriteDefaultLocationIds.Remove(destination.Id);
        }
        this.Save();
    }

    public void HideDefault(string id)
    {
        PlayerBookmarkData data = this.GetData();
        data.HiddenDefaultLocationIds.Add(id);
        data.FavoriteDefaultLocationIds.Remove(id);
        this.Save();
    }

    public void RestoreDefaultLocations()
    {
        PlayerBookmarkData data = this.GetData();
        if (data.HiddenDefaultLocationIds.Count == 0)
            return;
        data.HiddenDefaultLocationIds.Clear();
        this.Save();
    }

    public void RestoreDefaultLocation(string id)
    {
        PlayerBookmarkData data = this.GetData();
        if (data.HiddenDefaultLocationIds.Remove(id))
            this.Save();
    }

    public void MarkUsed(string bookmarkId)
    {
        BookmarkRecord? bookmark = this.GetData().Bookmarks.FirstOrDefault(item => item.Id == bookmarkId);
        if (bookmark is null)
            return;

        bookmark.LastUsedAtUtcTicks = DateTime.UtcNow.Ticks;
        this.Save();
    }

    public void ResetCache() => this.cachedData = null;

    private void Save()
    {
        if (this.cachedData is null)
            return;

        Game1.player.modData[this.dataKey] = JsonSerializer.Serialize(this.cachedData, JsonOptions);
    }

    private static string MakeUniqueName(IEnumerable<BookmarkRecord> bookmarks, string suggestedName)
    {
        string baseName = string.IsNullOrWhiteSpace(suggestedName) ? "Bookmark" : suggestedName.Trim();
        HashSet<string> names = bookmarks.Select(bookmark => bookmark.Name).ToHashSet(StringComparer.CurrentCultureIgnoreCase);
        if (!names.Contains(baseName))
            return baseName;

        for (int suffix = 2; suffix < 1000; suffix++)
        {
            string candidate = $"{baseName} {suffix}";
            if (!names.Contains(candidate))
                return candidate;
        }
        return $"{baseName} {Guid.NewGuid():N}";
    }
}
