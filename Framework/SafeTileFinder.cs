using Microsoft.Xna.Framework;
using StardewValley;
using xTile.Dimensions;

namespace WarpBookmarks.Framework;

/// <summary>Finds a nearby passable, unoccupied player tile.</summary>
internal static class SafeTileFinder
{
    public static bool TryFind(GameLocation location, Point requested, out Point safeTile)
    {
        for (int radius = 0; radius <= 3; radius++)
        {
            foreach (Point tile in GetRing(requested, radius))
            {
                if (IsSafe(location, tile))
                {
                    safeTile = tile;
                    return true;
                }
            }
        }

        safeTile = Point.Zero;
        return false;
    }

    private static bool IsSafe(GameLocation location, Point tile)
    {
        if (tile.X < 0 || tile.Y < 0 || location.Map is null || location.Map.Layers.Count == 0)
            return false;

        int width = location.Map.Layers[0].LayerWidth;
        int height = location.Map.Layers[0].LayerHeight;
        if (tile.X >= width || tile.Y >= height)
            return false;

        Vector2 vector = new(tile.X, tile.Y);
        if (!location.isTilePassable(new Location(tile.X, tile.Y), Game1.viewport))
            return false;
        if (location.doesTileHaveProperty(tile.X, tile.Y, "Water", "Back") is not null)
            return false;
        if (location.objects.ContainsKey(vector))
            return false;
        if (location.characters.Any(character => character.TilePoint == tile))
            return false;
        if (location.farmers.Any(farmer => farmer.TilePoint == tile))
            return false;

        return true;
    }

    private static IEnumerable<Point> GetRing(Point center, int radius)
    {
        if (radius == 0)
        {
            yield return center;
            yield break;
        }

        for (int x = -radius; x <= radius; x++)
        {
            yield return new Point(center.X + x, center.Y - radius);
            yield return new Point(center.X + x, center.Y + radius);
        }
        for (int y = -radius + 1; y < radius; y++)
        {
            yield return new Point(center.X - radius, center.Y + y);
            yield return new Point(center.X + radius, center.Y + y);
        }
    }
}
