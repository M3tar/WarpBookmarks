using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using WarpBookmarks.Models;

namespace WarpBookmarks.Framework;

internal sealed class WarpService
{
    private readonly BookmarkRepository repository;
    private readonly IMonitor monitor;
    private readonly Func<string, string> translate;

    public WarpService(BookmarkRepository repository, IMonitor monitor, Func<string, string> translate)
    {
        this.repository = repository;
        this.monitor = monitor;
        this.translate = translate;
    }

    public LocationReference? PreviousLocation { get; private set; }

    public bool TryWarp(WarpDestination destination)
    {
        if (!LocationPolicy.CanUseTeleportNow(out string reasonKey))
            return this.Fail(reasonKey);

        GameLocation? targetLocation = Game1.getLocationFromName(destination.Location.LocationName);
        if (targetLocation is null)
            return this.Fail("error.location-missing");
        if (destination.Kind == WarpDestinationKind.Coordinate
            && targetLocation != Game1.currentLocation
            && !Game1.player.locationsVisited.Contains(targetLocation.Name))
        {
            return this.Fail("error.location-unvisited");
        }
        if (LocationPolicy.IsRestricted(targetLocation))
            return this.Fail("error.location-restricted");
        if (!SafeTileFinder.TryFind(targetLocation, new Point(destination.Location.TileX, destination.Location.TileY), out Point safeTile))
            return this.Fail("error.no-safe-tile");

        LocationReference origin = new()
        {
            LocationName = Game1.currentLocation.NameOrUniqueName,
            DisplayName = Game1.currentLocation.DisplayName,
            TileX = Game1.player.TilePoint.X,
            TileY = Game1.player.TilePoint.Y,
            FacingDirection = Game1.player.FacingDirection
        };

        try
        {
            Game1.exitActiveMenu();
            Game1.warpFarmer(targetLocation.NameOrUniqueName, safeTile.X, safeTile.Y, destination.Location.FacingDirection);
            this.PreviousLocation = origin;
            if (destination.Kind == WarpDestinationKind.Bookmark)
                this.repository.MarkUsed(destination.Id);

            string message = this.translate("hud.warp-success")
                .Replace("{{name}}", destination.Name)
                .Replace("{{x}}", safeTile.X.ToString())
                .Replace("{{y}}", safeTile.Y.ToString());
            Game1.addHUDMessage(new HUDMessage(message, HUDMessage.newQuest_type));
            Game1.playSound("wand");
            return true;
        }
        catch (Exception ex)
        {
            this.monitor.Log($"Warp failed for '{destination.Name}' ({destination.Location.LocationName}). {ex}", LogLevel.Error);
            return this.Fail("error.warp-failed");
        }
    }

    public void ClearPrevious() => this.PreviousLocation = null;

    private bool Fail(string translationKey)
    {
        Game1.addHUDMessage(new HUDMessage(this.translate(translationKey), HUDMessage.error_type));
        return false;
    }
}
