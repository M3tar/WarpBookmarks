# Warp Bookmarks

[Download Warp Bookmarks 1.0.0 from GitHub Releases](https://github.com/M3tar/WarpBookmarks/releases/tag/v1.0.0) or [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/50310).

Warp Bookmarks is a lightweight SMAPI mod for Stardew Valley 1.6. Save personal locations, search them in a parchment-style travel book, return home or to your previous departure point, and teleport using map tile coordinates.

## Features

- Open the travel book with `K` and quickly save your current position with `Shift + K`.
- Create, name, search, favorite, rename, and delete personal bookmarks.
- Return to the outside of your own farmhouse or multiplayer cabin.
- Travel to the five standard vanilla Warp Totem destinations.
- Return to the departure point of your previous successful Warp Bookmarks teleport.
- Select a visited map, enter X/Y tile coordinates, preview the destination, teleport, or save it as a bookmark.
- Automatically search within three tiles when the requested tile is blocked.
- Keep custom bookmarks separate for each save and multiplayer player.
- Configure shortcuts through Generic Mod Config Menu when installed.

## Requirements

- Stardew Valley 1.6.15 or later in the 1.6 line;
- SMAPI 4.5.2 or later;
- Generic Mod Config Menu is optional.

## Installation

Download [`WarpBookmarks-1.0.0.zip` directly from GitHub](https://github.com/M3tar/WarpBookmarks/releases/download/v1.0.0/WarpBookmarks-1.0.0.zip), or get the mod from [Nexus Mods (Mod ID 50310)](https://www.nexusmods.com/stardewvalley/mods/50310).

1. Install [SMAPI](https://smapi.io/).
2. Download `WarpBookmarks-1.0.0.zip`, then extract the `WarpBookmarks` folder inside it into the game's `Mods` folder.
3. Confirm that the resulting path is `Mods/WarpBookmarks/WarpBookmarks.dll`.
4. Start the game through SMAPI and load a save.

If the DLL ends up at `Mods/WarpBookmarks/WarpBookmarks/WarpBookmarks.dll`, the archive was extracted with an extra folder level. Move the inner `WarpBookmarks` folder directly into `Mods`.

## First use

1. Press `K` after loading a save to open the travel book.
2. Select Home, Previous Location, a vanilla destination, or a personal bookmark on the left. Check its details, then use the main warp button in the lower-right corner.
3. To save your current position, select **Record current location** and enter a name. Press `Shift + K` instead to skip the naming window and create a bookmark with an automatic name.
4. Use the search field and categories to find destinations. Personal bookmarks can be favorited, renamed, or deleted.
5. For an exact destination, select **Enter coordinates…**, choose a map you have already visited, enter its X/Y tile coordinates, preview the result, then warp or save it as a bookmark.

To update, replace the existing `Mods/WarpBookmarks` folder with the new release. Personal bookmark data is stored in the player save data and is preserved when the mod folder is replaced.

To uninstall, remove `Mods/WarpBookmarks`. Saves remain playable; the unused bookmark data may remain in the save unless removed by a dedicated cleanup tool.

## Known limitation

The Left and Right arrow keys don't move the caret in the bookmark rename field. Backspace and retyping still work, and this doesn't affect saving or teleporting.

## Compatibility scope

The mod is designed around stable game location names and should work with many conventional custom maps. The first release has been tested on Windows with Stardew Valley 1.6.15 and SMAPI 4.5.2 in single-player and online multiplayer. Universal compatibility with every expansion mod is not guaranteed.
