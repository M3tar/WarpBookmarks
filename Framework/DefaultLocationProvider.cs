using StardewValley;
using WarpBookmarks.Models;

namespace WarpBookmarks.Framework;

/// <summary>Builds the standard warp-totem destinations without mixing them into player bookmarks.</summary>
internal static class DefaultLocationProvider
{
    public static IEnumerable<WarpDestination> GetVisible(PlayerBookmarkData data, Func<string, string> translate)
    {
        foreach ((string id, string nameKey, string locationName, int x, int y) in GetDefinitions())
        {
            if (data.HiddenDefaultLocationIds.Contains(id) || !HasVisited(locationName))
                continue;

            LocationReference location = id == "FarmTotem"
                ? ResolveFarmTotemLocation()
                : new LocationReference
                {
                    LocationName = locationName,
                    DisplayName = translate(nameKey),
                    TileX = x,
                    TileY = y,
                    FacingDirection = 2
                };

            yield return new WarpDestination
            {
                Id = id,
                Name = translate(nameKey),
                Location = location,
                Kind = WarpDestinationKind.Default,
                IsFavorite = data.FavoriteDefaultLocationIds.Contains(id)
            };
        }
    }

    public static LocationReference ResolveFarmTotemLocation()
    {
        GameLocation farm = Game1.getFarm();
        int x = 48;
        int y = 7;
        if (HomeLocationResolver.TryReadMapPoint(farm, "WarpTotemEntry", out int mapX, out int mapY))
        {
            x = mapX;
            y = mapY;
        }
        return new LocationReference
        {
            LocationName = farm.NameOrUniqueName,
            DisplayName = farm.DisplayName,
            TileX = x,
            TileY = y,
            FacingDirection = 2
        };
    }

    private static bool HasVisited(string locationName)
    {
        if (locationName == "Farm")
            return true;
        return Game1.player.locationsVisited.Contains(locationName);
    }

    private static IEnumerable<(string Id, string NameKey, string LocationName, int X, int Y)> GetDefinitions()
    {
        yield return ("FarmTotem", "destination.farm", "Farm", 48, 7);
        yield return ("MountainTotem", "destination.mountain", "Mountain", 31, 20);
        yield return ("BeachTotem", "destination.beach", "Beach", 20, 4);
        yield return ("DesertTotem", "destination.desert", "Desert", 35, 43);
        yield return ("IslandTotem", "destination.island", "IslandSouth", 11, 11);
    }
}
