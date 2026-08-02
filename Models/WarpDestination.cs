namespace WarpBookmarks.Models;

internal enum WarpDestinationKind
{
    Home,
    Previous,
    Default,
    Bookmark,
    Coordinate
}

/// <summary>A resolved destination displayed by the menu and accepted by the common warp pipeline.</summary>
internal sealed class WarpDestination
{
    public string Id { get; init; } = "";

    public string Name { get; init; } = "";

    public LocationReference Location { get; init; } = new();

    public WarpDestinationKind Kind { get; init; }

    public bool IsFavorite { get; set; }

    public bool CanRemove => this.Kind is WarpDestinationKind.Bookmark or WarpDestinationKind.Default;
}
