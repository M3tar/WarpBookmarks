namespace WarpBookmarks.Models;

/// <summary>A player-owned teleport bookmark.</summary>
internal sealed class BookmarkRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = "";

    public LocationReference Location { get; set; } = new();

    public bool IsFavorite { get; set; }

    public long CreatedAtUtcTicks { get; set; } = DateTime.UtcNow.Ticks;

    public long LastUsedAtUtcTicks { get; set; }
}
