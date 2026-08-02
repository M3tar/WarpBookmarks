using StardewModdingAPI;
using StardewValley;

namespace WarpBookmarks.Framework;

/// <summary>Conservative checks for locations which should never be recorded or entered freely.</summary>
internal static class LocationPolicy
{
    private static readonly string[] BlockedNameFragments =
    {
        "Mine",
        "VolcanoDungeon",
        "Festival",
        "Temp",
        "MovieTheaterScreeningRoom"
    };

    public static bool CanUseTeleportNow(out string reasonKey)
    {
        Farmer? player = Game1.player;
        GameLocation? currentLocation = Game1.currentLocation;
        if (!Context.IsWorldReady || player is null || currentLocation is null)
        {
            reasonKey = "error.world-not-ready";
            return false;
        }
        if (Game1.eventUp || Game1.currentMinigame is not null)
        {
            reasonKey = "error.event-active";
            return false;
        }
        if (player.UsingTool)
        {
            reasonKey = "error.player-busy";
            return false;
        }

        reasonKey = "";
        return true;
    }

    public static bool IsRestricted(GameLocation location)
    {
        string typeName = location.GetType().FullName ?? location.GetType().Name;
        string name = location.NameOrUniqueName ?? location.Name ?? "";
        return BlockedNameFragments.Any(fragment =>
            typeName.Contains(fragment, StringComparison.OrdinalIgnoreCase)
            || name.Contains(fragment, StringComparison.OrdinalIgnoreCase)
        );
    }
}
