using WarpBookmarks.Config;
using WarpBookmarks.Framework;
using WarpBookmarks.Integrations;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using WarpBookmarks.Models;
using WarpBookmarks.UI;

namespace WarpBookmarks;

/// <summary>The mod entry point.</summary>
public sealed class ModEntry : Mod
{
    private const int CurrentConfigVersion = 4;

    private ModConfig? config;
    private BookmarkRepository? repository;
    private DestinationCatalog? catalog;
    private WarpService? warpService;
    private DestinationCategory lastMenuCategory = DestinationCategory.Common;

    public override void Entry(IModHelper helper)
    {
        this.config = helper.ReadConfig<ModConfig>();
        this.MigrateConfig();
        this.repository = new BookmarkRepository(this.ModManifest, this.Monitor);
        HomeLocationResolver homeResolver = new(this.Monitor);
        this.catalog = new DestinationCatalog(this.repository, homeResolver, this.Translate);
        this.warpService = new WarpService(this.repository, this.Monitor, this.Translate);
        this.Monitor.Log(
            $"Active shortcuts: open={this.config.OpenMenuKey}; create={this.config.CreateBookmarkKey}.",
            LogLevel.Info
        );

        helper.Events.Input.ButtonsChanged += this.OnButtonsChanged;
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;

        helper.ConsoleCommands.Add(
            "wb_where",
            "Log phase-0 location, building, home, and player identity fields for the current tile.",
            this.OnWhereCommand
        );
        helper.ConsoleCommands.Add(
            "wb_validate_tile",
            "Log phase-0 tile API and occupancy probes. Usage: wb_validate_tile <x> <y>",
            this.OnValidateTileCommand
        );
        helper.ConsoleCommands.Add(
            "wb_probe_data",
            "Read/write the current player's phase-0 persistence probe. Usage: wb_probe_data [value|clear]",
            this.OnProbeDataCommand
        );
        helper.ConsoleCommands.Add(
            "wb_warp",
            "Safely warp to an exact tile. Usage: wb_warp <location> <x> <y>",
            this.OnWarpCommand
        );
        helper.ConsoleCommands.Add(
            "wb_dump_bookmarks",
            "List the current player's bookmark names and coordinates.",
            this.OnDumpBookmarksCommand
        );

        this.Monitor.Log(
            $"Warp Bookmarks {this.ModManifest.Version} vertical slice loaded.",
            LogLevel.Info
        );
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        IGenericModConfigMenuApi? api = this.Helper.ModRegistry
            .GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (api is null || this.config is null)
            return;

        api.Register(this.ModManifest, this.ResetConfig, () => this.Helper.WriteConfig(this.config));
        api.AddSectionTitle(this.ModManifest, () => this.Helper.Translation.Get("config.section.shortcuts"));
        api.AddKeybindList(
            this.ModManifest,
            () => this.config.OpenMenuKey,
            value => this.config.OpenMenuKey = value,
            () => this.Helper.Translation.Get("config.open-menu.name"),
            () => this.Helper.Translation.Get("config.open-menu.tooltip"),
            fieldId: nameof(ModConfig.OpenMenuKey)
        );
        api.AddKeybindList(
            this.ModManifest,
            () => this.config.CreateBookmarkKey,
            value => this.config.CreateBookmarkKey = value,
            () => this.Helper.Translation.Get("config.create-bookmark.name"),
            () => this.Helper.Translation.Get("config.create-bookmark.tooltip"),
            fieldId: nameof(ModConfig.CreateBookmarkKey)
        );
    }

    private void ResetConfig()
    {
        if (this.config is null)
            return;

        ModConfig defaults = new();
        this.config.ConfigVersion = CurrentConfigVersion;
        this.config.OpenMenuKey = defaults.OpenMenuKey;
        this.config.CreateBookmarkKey = defaults.CreateBookmarkKey;
    }

    private void MigrateConfig()
    {
        if (this.config is null || this.config.ConfigVersion >= CurrentConfigVersion)
            return;

        bool migratedShortcut = false;
        if (this.config.ConfigVersion < 1 && KeybindsMatch(this.config.OpenMenuKey, "F4"))
        {
            this.config.OpenMenuKey = KeybindList.Parse("F6");
            migratedShortcut = true;
        }
        if (this.config.ConfigVersion < 1 && KeybindsMatch(this.config.CreateBookmarkKey, "LeftShift + F4"))
        {
            this.config.CreateBookmarkKey = KeybindList.Parse("LeftShift + F6");
            migratedShortcut = true;
        }
        if (this.config.ConfigVersion < 2 && KeybindsMatch(this.config.OpenMenuKey, "F6"))
        {
            this.config.OpenMenuKey = KeybindList.Parse("T");
            migratedShortcut = true;
        }
        if (this.config.ConfigVersion < 2 && KeybindsMatch(this.config.CreateBookmarkKey, "LeftShift + F6"))
        {
            this.config.CreateBookmarkKey = KeybindList.Parse("LeftShift + T");
            migratedShortcut = true;
        }
        if (this.config.ConfigVersion < 3
            && (KeybindsMatch(this.config.OpenMenuKey, "T") || KeybindsMatch(this.config.OpenMenuKey, "F5")))
        {
            this.config.OpenMenuKey = KeybindList.Parse("K");
            migratedShortcut = true;
        }
        if (this.config.ConfigVersion < 3
            && (KeybindsMatch(this.config.CreateBookmarkKey, "LeftShift + T")
                || KeybindsMatch(this.config.CreateBookmarkKey, "LeftShift + F5")))
        {
            this.config.CreateBookmarkKey = KeybindList.Parse("LeftShift + K");
            migratedShortcut = true;
        }

        this.config.ConfigVersion = CurrentConfigVersion;
        this.Helper.WriteConfig(this.config);
        if (migratedShortcut)
        {
            this.Monitor.Log(
                "Migrated the legacy test shortcuts to K and LeftShift + K.",
                LogLevel.Info
            );
        }

    }

    private static bool KeybindsMatch(KeybindList actual, string expected)
    {
        return string.Equals(
            actual.ToString(),
            KeybindList.Parse(expected).ToString(),
            StringComparison.OrdinalIgnoreCase
        );
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.lastMenuCategory = DestinationCategory.Common;
        this.repository?.ResetCache();
        this.warpService?.ClearPrevious();
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        this.lastMenuCategory = DestinationCategory.Common;
        this.repository?.ResetCache();
        this.warpService?.ClearPrevious();
    }

    private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
    {
        if (!Context.IsWorldReady || this.config is null)
            return;

        if (Game1.activeClickableMenu is WarpBookmarksMenu)
        {
            if (this.config.OpenMenuKey.JustPressed())
            {
                this.Helper.Input.SuppressActiveKeybinds(this.config.OpenMenuKey);
                Game1.exitActiveMenu();
            }
            return;
        }
        if (Game1.activeClickableMenu is CoordinateWarpDialog)
        {
            if (this.config.OpenMenuKey.JustPressed())
            {
                this.Helper.Input.SuppressActiveKeybinds(this.config.OpenMenuKey);
                Game1.exitActiveMenu();
                this.OpenMenu();
            }
            return;
        }
        if (Game1.activeClickableMenu is not null)
            return;

        if (this.config.CreateBookmarkKey.JustPressed())
        {
            this.Helper.Input.SuppressActiveKeybinds(this.config.CreateBookmarkKey);
            this.CreateBookmarkAtCurrentLocation();
            return;
        }

        if (this.config.OpenMenuKey.JustPressed())
        {
            this.Helper.Input.SuppressActiveKeybinds(this.config.OpenMenuKey);
            this.OpenMenu();
        }
    }

    private void OpenMenu()
    {
        if (this.config is null || this.catalog is null || this.repository is null || this.warpService is null)
            return;
        if (!LocationPolicy.CanUseTeleportNow(out string reasonKey))
        {
            this.ShowError(reasonKey);
            return;
        }

        Game1.activeClickableMenu = new WarpBookmarksMenu(
            this.catalog.Build(this.warpService.PreviousLocation),
            destination => this.warpService.TryWarp(destination),
            (destination, favorite) => this.repository.SetFavorite(destination, favorite),
            this.RemoveDestination,
            this.RestoreHiddenDestination,
            this.OpenRenameDialog,
            this.RestoreDefaultLocations,
            this.OpenCoordinateDialog,
            this.OpenCreateCurrentBookmarkDialog,
            this.lastMenuCategory,
            category => this.lastMenuCategory = category,
            this.Translate,
            this.Helper.Translation.Get("menu.shortcuts", new
            {
                open = this.config.OpenMenuKey,
                create = this.config.CreateBookmarkKey
            })
        );
    }

    private WarpDestination? CreateBookmarkAtCurrentLocation()
    {
        if (this.repository is null)
            return null;
        if (!LocationPolicy.CanUseTeleportNow(out string reasonKey))
        {
            this.ShowError(reasonKey);
            return null;
        }
        if (LocationPolicy.IsRestricted(Game1.currentLocation))
        {
            this.ShowError("error.record-restricted");
            return null;
        }

        string suggestedName = $"{Game1.currentLocation.DisplayName} ({Game1.player.TilePoint.X}, {Game1.player.TilePoint.Y})";
        BookmarkRecord? bookmark = this.repository.AddCurrentLocation(suggestedName, out string? error);
        if (bookmark is null)
        {
            this.ShowError(error == "limit" ? "error.bookmark-limit" : "error.bookmark-create");
            return null;
        }

        Game1.playSound("newArtifact");
        Game1.addHUDMessage(new HUDMessage(this.Helper.Translation.Get("hud.bookmark-created", new { name = bookmark.Name }), HUDMessage.newQuest_type));
        return new WarpDestination
        {
            Id = bookmark.Id,
            Name = bookmark.Name,
            Location = bookmark.Location,
            Kind = WarpDestinationKind.Bookmark,
            IsFavorite = bookmark.IsFavorite
        };
    }

    private void OpenCreateCurrentBookmarkDialog()
    {
        if (!this.TryCaptureCurrentLocation(out LocationReference location, out string suggestedName))
            return;

        Game1.activeClickableMenu = new BookmarkCreateDialog(
            location,
            suggestedName,
            name => this.SaveNamedBookmark(location, name),
            this.OpenMenu,
            this.Translate
        );
    }

    private bool TryCaptureCurrentLocation(out LocationReference location, out string suggestedName)
    {
        location = new LocationReference();
        suggestedName = "";
        if (!LocationPolicy.CanUseTeleportNow(out string reasonKey))
        {
            this.ShowError(reasonKey);
            return false;
        }
        if (LocationPolicy.IsRestricted(Game1.currentLocation))
        {
            this.ShowError("error.record-restricted");
            return false;
        }

        string locationName = Game1.currentLocation.NameOrUniqueName;
        if (string.IsNullOrWhiteSpace(locationName))
        {
            this.ShowError("error.bookmark-create");
            return false;
        }

        location = new LocationReference
        {
            LocationName = locationName,
            DisplayName = Game1.currentLocation.DisplayName,
            TileX = Game1.player.TilePoint.X,
            TileY = Game1.player.TilePoint.Y,
            FacingDirection = Game1.player.FacingDirection
        };
        suggestedName = $"{location.DisplayName} ({location.TileX}, {location.TileY})";
        return true;
    }

    private void RemoveDestination(WarpDestination destination)
    {
        if (this.repository is null)
            return;
        if (destination.Kind == WarpDestinationKind.Bookmark)
            this.repository.DeleteBookmark(destination.Id);
        else if (destination.Kind == WarpDestinationKind.Default)
        {
            this.repository.HideDefault(destination.Id);
            this.OpenMenu();
        }
    }

    private void OpenRenameDialog(WarpDestination destination)
    {
        if (this.repository is null || destination.Kind != WarpDestinationKind.Bookmark)
            return;

        Game1.activeClickableMenu = new BookmarkRenameDialog(
            destination.Name,
            name =>
            {
                this.repository.RenameBookmark(destination.Id, name);
                this.OpenMenu();
            },
            this.OpenMenu,
            this.Translate
        );
    }

    private void RestoreDefaultLocations()
    {
        this.repository?.RestoreDefaultLocations();
        this.OpenMenu();
    }

    private void RestoreHiddenDestination(WarpDestination destination)
    {
        if (destination.Kind == WarpDestinationKind.Default)
        {
            this.repository?.RestoreDefaultLocation(destination.Id);
            this.OpenMenu();
        }
    }

    private void OpenCoordinateDialog()
    {
        if (this.warpService is null)
            return;
        Game1.activeClickableMenu = new CoordinateWarpDialog(
            this.warpService,
            this.Translate,
            this.OpenMenu,
            this.OpenCreateCoordinateBookmarkDialog
        );
    }

    private void OpenCreateCoordinateBookmarkDialog(WarpDestination destination)
    {
        LocationReference location = new()
        {
            LocationName = destination.Location.LocationName,
            DisplayName = destination.Location.DisplayName,
            TileX = destination.Location.TileX,
            TileY = destination.Location.TileY,
            FacingDirection = destination.Location.FacingDirection
        };
        Game1.activeClickableMenu = new BookmarkCreateDialog(
            location,
            destination.Name,
            name => this.SaveNamedBookmark(location, name),
            this.OpenCoordinateDialog,
            this.Translate
        );
    }

    private void SaveNamedBookmark(LocationReference location, string name)
    {
        if (this.repository is null)
            return;

        BookmarkRecord? bookmark = this.repository.AddLocation(location, name, out string? error);
        if (bookmark is null)
        {
            this.ShowError(error == "limit" ? "error.bookmark-limit" : "error.bookmark-create");
            this.OpenMenu();
            return;
        }

        this.lastMenuCategory = DestinationCategory.Bookmarks;
        Game1.playSound("newArtifact");
        Game1.addHUDMessage(new HUDMessage(this.Helper.Translation.Get("hud.bookmark-created", new { name = bookmark.Name }), HUDMessage.newQuest_type));
        this.OpenMenu();
    }

    private void ShowError(string key)
    {
        Game1.addHUDMessage(new HUDMessage(this.Helper.Translation.Get(key), HUDMessage.error_type));
    }

    private void OnWhereCommand(string command, string[] args)
    {
        ApiValidationService.LogCurrentLocation(this.Monitor);
    }

    private void OnValidateTileCommand(string command, string[] args)
    {
        if (args.Length < 2 || !int.TryParse(args[0], out int x) || !int.TryParse(args[1], out int y))
        {
            this.Monitor.Log("Usage: wb_validate_tile <x> <y>", LogLevel.Warn);
            return;
        }

        ApiValidationService.LogTileProbe(this.Monitor, x, y);
    }

    private void OnProbeDataCommand(string command, string[] args)
    {
        ApiValidationService.ProbePlayerData(this.Monitor, this.ModManifest, args);
    }

    private void OnWarpCommand(string command, string[] args)
    {
        if (args.Length != 3 || !int.TryParse(args[1], out int x) || !int.TryParse(args[2], out int y))
        {
            this.Monitor.Log("Usage: wb_warp <location> <x> <y>", LogLevel.Warn);
            return;
        }
        if (!Context.IsWorldReady || this.warpService is null)
        {
            this.Monitor.Log("Load a save before using wb_warp.", LogLevel.Warn);
            return;
        }

        GameLocation? location = Game1.getLocationFromName(args[0]);
        string displayName = location?.DisplayName ?? args[0];
        this.warpService.TryWarp(new WarpDestination
        {
            Id = "Coordinate",
            Name = $"{displayName} ({x}, {y})",
            Kind = WarpDestinationKind.Coordinate,
            Location = new LocationReference
            {
                LocationName = args[0],
                DisplayName = displayName,
                TileX = x,
                TileY = y,
                FacingDirection = 2
            }
        });
    }

    private void OnDumpBookmarksCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || this.repository is null)
        {
            this.Monitor.Log("Load a save before using wb_dump_bookmarks.", LogLevel.Warn);
            return;
        }

        IReadOnlyList<BookmarkRecord> bookmarks = this.repository.GetData().Bookmarks;
        this.Monitor.Log($"Current player has {bookmarks.Count} Warp Bookmarks:", LogLevel.Info);
        foreach (BookmarkRecord bookmark in bookmarks)
        {
            this.Monitor.Log(
                $"- {bookmark.Name}: {bookmark.Location.LocationName} ({bookmark.Location.TileX}, {bookmark.Location.TileY}) favorite={bookmark.IsFavorite}",
                LogLevel.Info
            );
        }
    }

    private string Translate(string key) => this.Helper.Translation.Get(key);
}
