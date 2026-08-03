using StardewModdingAPI.Utilities;

namespace WarpBookmarks.Config;

internal sealed class ModConfig
{
    public int ConfigVersion { get; set; }

    public KeybindList OpenMenuKey { get; set; } = KeybindList.Parse("K");

    public KeybindList CreateBookmarkKey { get; set; } = KeybindList.Parse("LeftShift + K");
}
