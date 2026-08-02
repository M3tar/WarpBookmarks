using WarpBookmarks.Config;
using WarpBookmarks.Framework;
using WarpBookmarks.Integrations;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace WarpBookmarks;

/// <summary>The mod entry point.</summary>
public sealed class ModEntry : Mod
{
    private const int CurrentConfigVersion = 3;

    private ModConfig? config;
    private bool showedShortcutHint;

    public override void Entry(IModHelper helper)
    {
        this.config = helper.ReadConfig<ModConfig>();
        this.MigrateConfig();
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

        this.Monitor.Log(
            "Warp Bookmarks 0.0.4 phase-0 foundation loaded. Run wb_where after loading a save.",
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
        api.AddBoolOption(
            this.ModManifest,
            () => this.config.ShowShortcutHint,
            value => this.config.ShowShortcutHint = value,
            () => this.Helper.Translation.Get("config.show-hint.name"),
            () => this.Helper.Translation.Get("config.show-hint.tooltip"),
            fieldId: nameof(ModConfig.ShowShortcutHint)
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
        this.config.ShowShortcutHint = defaults.ShowShortcutHint;
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
        this.ShowShortcutHintIfNeeded();
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        this.showedShortcutHint = false;
    }

    private void ShowShortcutHintIfNeeded()
    {
        if (this.showedShortcutHint || this.config is null || !this.config.ShowShortcutHint)
            return;

        this.showedShortcutHint = true;
        string message = this.Helper.Translation.Get("hud.shortcut-hint", new
        {
            open = this.config.OpenMenuKey,
            create = this.config.CreateBookmarkKey
        });
        Game1.addHUDMessage(new HUDMessage(message, HUDMessage.newQuest_type));
    }

    private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
    {
        if (!Context.IsWorldReady || this.config is null || Game1.activeClickableMenu is not null)
            return;

        if (this.config.CreateBookmarkKey.JustPressed())
        {
            this.Helper.Input.SuppressActiveKeybinds(this.config.CreateBookmarkKey);
            this.ShowPhaseZeroNotice("notice.create-bookmark");
            return;
        }

        if (this.config.OpenMenuKey.JustPressed())
        {
            this.Helper.Input.SuppressActiveKeybinds(this.config.OpenMenuKey);
            this.ShowPhaseZeroNotice("notice.open-menu");
        }
    }

    private void ShowPhaseZeroNotice(string key)
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
}
