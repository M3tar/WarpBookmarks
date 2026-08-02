using StardewValley;
using WarpBookmarks.Models;

namespace WarpBookmarks.Framework;

internal sealed class DestinationCatalog
{
    private readonly BookmarkRepository repository;
    private readonly HomeLocationResolver homeResolver;
    private readonly Func<string, string> translate;

    public DestinationCatalog(BookmarkRepository repository, HomeLocationResolver homeResolver, Func<string, string> translate)
    {
        this.repository = repository;
        this.homeResolver = homeResolver;
        this.translate = translate;
    }

    public List<WarpDestination> Build(LocationReference? previous)
    {
        PlayerBookmarkData data = this.repository.GetData();
        List<WarpDestination> destinations = new()
        {
            new WarpDestination
            {
                Id = "Home",
                Name = this.translate("destination.home"),
                Location = this.homeResolver.Resolve(),
                Kind = WarpDestinationKind.Home
            }
        };

        if (previous is not null)
        {
            destinations.Add(new WarpDestination
            {
                Id = "Previous",
                Name = this.translate("destination.previous"),
                Location = previous,
                Kind = WarpDestinationKind.Previous
            });
        }

        destinations.AddRange(DefaultLocationProvider.GetVisible(data, this.translate));
        destinations.AddRange(data.Bookmarks
            .OrderByDescending(bookmark => bookmark.IsFavorite)
            .ThenBy(bookmark => bookmark.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(bookmark => new WarpDestination
            {
                Id = bookmark.Id,
                Name = bookmark.Name,
                Location = bookmark.Location,
                Kind = WarpDestinationKind.Bookmark,
                IsFavorite = bookmark.IsFavorite
            }));
        destinations.AddRange(DefaultLocationProvider.GetHidden(data, this.translate));
        return destinations;
    }
}
