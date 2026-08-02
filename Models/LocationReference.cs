namespace WarpBookmarks.Models;

/// <summary>A serializable reference to a tile in a game location.</summary>
internal sealed class LocationReference
{
    public string LocationName { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public int TileX { get; set; }

    public int TileY { get; set; }

    public int FacingDirection { get; set; } = 2;
}
