using System.Collections;
using System.Reflection;
using StardewModdingAPI;
using StardewValley;
using WarpBookmarks.Models;

namespace WarpBookmarks.Framework;

/// <summary>Resolves the current player's own exterior home door without assuming a farm layout.</summary>
internal sealed class HomeLocationResolver
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
    private readonly IMonitor monitor;

    public HomeLocationResolver(IMonitor monitor)
    {
        this.monitor = monitor;
    }

    public LocationReference Resolve()
    {
        string homeName = ReadString(Game1.player, "homeLocation") ?? "FarmHouse";
        GameLocation? home = Game1.getLocationFromName(homeName);
        if (home is not null && TryReadExteriorWarp(home, out LocationReference? exterior) && exterior is not null)
            return exterior;

        GameLocation farm = Game1.getFarm();
        if (TryReadMapPoint(farm, "FarmHouseEntry", out int x, out int y))
        {
            return new LocationReference
            {
                LocationName = farm.NameOrUniqueName,
                DisplayName = farm.DisplayName,
                TileX = x,
                TileY = y,
                FacingDirection = 2
            };
        }

        this.monitor.Log("Couldn't resolve the player's exterior home warp; using the farm totem entry as a fallback.", LogLevel.Warn);
        return DefaultLocationProvider.ResolveFarmTotemLocation();
    }

    private static bool TryReadExteriorWarp(GameLocation home, out LocationReference? result)
    {
        object? warps = GetMemberValue(home, "warps");
        if (warps is IEnumerable enumerable)
        {
            foreach (object? warp in enumerable)
            {
                if (warp is null)
                    continue;

                string? targetName = ReadString(warp, "TargetName", "targetName");
                int? targetX = ReadInt(warp, "TargetX", "targetX");
                int? targetY = ReadInt(warp, "TargetY", "targetY");
                if (string.IsNullOrWhiteSpace(targetName) || targetX is null || targetY is null)
                    continue;

                GameLocation? target = Game1.getLocationFromName(targetName);
                if (target is null || ReadBool(target, "IsOutdoors", "isOutdoors") == false)
                    continue;

                result = new LocationReference
                {
                    LocationName = target.NameOrUniqueName,
                    DisplayName = target.DisplayName,
                    TileX = targetX.Value,
                    TileY = targetY.Value,
                    FacingDirection = 2
                };
                return true;
            }
        }

        result = null;
        return false;
    }

    internal static bool TryReadMapPoint(GameLocation location, string propertyName, out int x, out int y)
    {
        x = 0;
        y = 0;
        if (location.Map is null || !location.Map.Properties.ContainsKey(propertyName))
            return false;

        xTile.ObjectModel.PropertyValue value = location.Map.Properties[propertyName];
        string[] parts = value.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 && int.TryParse(parts[0], out x) && int.TryParse(parts[1], out y);
    }

    private static string? ReadString(object target, params string[] names)
    {
        object? value = GetAnyMemberValue(target, names);
        value = UnwrapValue(value);
        return value?.ToString();
    }

    private static int? ReadInt(object target, params string[] names)
    {
        object? value = UnwrapValue(GetAnyMemberValue(target, names));
        if (value is int number)
            return number;
        return int.TryParse(value?.ToString(), out number) ? number : null;
    }

    private static bool? ReadBool(object target, params string[] names)
    {
        object? value = UnwrapValue(GetAnyMemberValue(target, names));
        if (value is bool boolean)
            return boolean;
        return bool.TryParse(value?.ToString(), out boolean) ? boolean : null;
    }

    private static object? GetAnyMemberValue(object target, params string[] names)
    {
        foreach (string name in names)
        {
            object? value = GetMemberValue(target, name);
            if (value is not null)
                return value;
        }
        return null;
    }

    private static object? GetMemberValue(object target, string name)
    {
        Type type = target.GetType();
        PropertyInfo? property = type.GetProperty(name, Flags);
        if (property is not null && property.GetIndexParameters().Length == 0)
            return property.GetValue(target);
        return type.GetField(name, Flags)?.GetValue(target);
    }

    private static object? UnwrapValue(object? value)
    {
        if (value is null || value is string || value.GetType().IsPrimitive)
            return value;
        PropertyInfo? property = value.GetType().GetProperty("Value", Flags);
        return property?.GetIndexParameters().Length == 0 ? property.GetValue(value) : value;
    }
}
