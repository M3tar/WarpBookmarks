using System.Collections;
using System.Reflection;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace WarpBookmarks.Framework;

/// <summary>Phase-0 runtime probes for location identity, home resolution, tile APIs, and per-player persistence.</summary>
internal static class ApiValidationService
{
    private static readonly BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static void LogCurrentLocation(IMonitor monitor)
    {
        if (!EnsureWorldReady(monitor))
            return;

        GameLocation location = Game1.currentLocation;
        Point tile = Game1.player.TilePoint;
        monitor.Log(
            $"Player={Game1.player.Name}; MultiplayerID={Game1.player.UniqueMultiplayerID}; "
            + $"LocationType={location.GetType().FullName}; Tile=({tile.X},{tile.Y}).",
            LogLevel.Info
        );

        LogMatchingMembers(
            monitor,
            "Location",
            location,
            "name",
            "unique",
            "display",
            "map",
            "parent",
            "building",
            "root",
            "outdoor",
            "structure"
        );
        LogMatchingMembers(
            monitor,
            "Farmer",
            Game1.player,
            "home",
            "house",
            "cabin",
            "unique",
            "multiplayer",
            "location"
        );

        object? parentBuilding = GetMemberValue(location, "ParentBuilding");
        if (parentBuilding is not null)
        {
            LogMatchingMembers(
                monitor,
                "ParentBuilding",
                parentBuilding,
                "id",
                "guid",
                "type",
                "tile",
                "indoors",
                "name",
                "location"
            );
        }
        else
        {
            monitor.Log("ParentBuilding: null or unavailable on this location.", LogLevel.Info);
        }
    }

    public static void LogTileProbe(IMonitor monitor, int x, int y)
    {
        if (!EnsureWorldReady(monitor))
            return;

        GameLocation location = Game1.currentLocation;
        object? map = GetMemberValue(location, "Map");
        monitor.Log(
            $"Tile probe: location={GetFriendlyLocationName(location)}, tile=({x},{y}), mapType={map?.GetType().FullName ?? "null"}.",
            LogLevel.Info
        );

        if (map is not null)
            LogMatchingMembers(monitor, "Map", map, "layer", "width", "height", "display");

        IEnumerable<MethodInfo> candidates = location.GetType()
            .GetMethods(InstanceFlags)
            .Where(method =>
                method.Name.Contains("tile", StringComparison.OrdinalIgnoreCase)
                || method.Name.Contains("pass", StringComparison.OrdinalIgnoreCase)
                || method.Name.Contains("collision", StringComparison.OrdinalIgnoreCase)
                || method.Name.Contains("clear", StringComparison.OrdinalIgnoreCase)
            )
            .OrderBy(method => method.Name, StringComparer.Ordinal)
            .Take(80);

        foreach (MethodInfo method in candidates)
        {
            string parameters = string.Join(
                ", ",
                method.GetParameters().Select(parameter => $"{parameter.ParameterType.Name} {parameter.Name}")
            );
            monitor.Log($"Tile API candidate: {method.ReturnType.Name} {method.Name}({parameters})", LogLevel.Trace);
        }

        Vector2 tileVector = new(x, y);
        LogCollectionKeyProbe(monitor, location, "objects", tileVector);
        LogCollectionKeyProbe(monitor, location, "terrainFeatures", tileVector);

        int characterCount = Game1.currentLocation.characters.Count(character => character.TilePoint == new Point(x, y));
        int farmerCount = Game1.currentLocation.farmers.Count(farmer => farmer.TilePoint == new Point(x, y));
        monitor.Log(
            $"Known occupants at ({x},{y}): characters={characterCount}, farmers={farmerCount}.",
            LogLevel.Info
        );
    }

    public static void ProbePlayerData(IMonitor monitor, IManifest manifest, string[] args)
    {
        if (!EnsureWorldReady(monitor))
            return;

        string key = $"{manifest.UniqueID}/phase-zero-probe";
        if (args.Length == 0)
        {
            bool found = Game1.player.modData.TryGetValue(key, out string? value);
            monitor.Log(
                found ? $"Player modData probe value: {value}" : "Player modData probe value is not set.",
                LogLevel.Info
            );
            return;
        }

        if (string.Equals(args[0], "clear", StringComparison.OrdinalIgnoreCase))
        {
            bool removed = Game1.player.modData.Remove(key);
            monitor.Log(removed ? "Player modData probe value cleared." : "No probe value existed.", LogLevel.Info);
            return;
        }

        string newValue = string.Join(" ", args);
        Game1.player.modData[key] = newValue;
        monitor.Log(
            $"Stored phase-0 probe value for player ID {Game1.player.UniqueMultiplayerID}. Save, reload, and run wb_probe_data to verify persistence.",
            LogLevel.Info
        );
    }

    private static bool EnsureWorldReady(IMonitor monitor)
    {
        if (Context.IsWorldReady && Game1.currentLocation is not null && Game1.player is not null)
            return true;

        monitor.Log("Load a save before running this command.", LogLevel.Warn);
        return false;
    }

    private static string GetFriendlyLocationName(GameLocation location)
    {
        return GetMemberValue(location, "NameOrUniqueName")?.ToString()
            ?? GetMemberValue(location, "Name")?.ToString()
            ?? location.GetType().Name;
    }

    private static void LogMatchingMembers(
        IMonitor monitor,
        string label,
        object target,
        params string[] fragments
    )
    {
        Type type = target.GetType();
        IEnumerable<MemberInfo> members = type
            .GetMembers(InstanceFlags)
            .Where(member => member.MemberType is MemberTypes.Field or MemberTypes.Property)
            .Where(member => fragments.Any(fragment => member.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(member => member.Name, StringComparer.Ordinal)
            .Take(100);

        foreach (MemberInfo member in members)
        {
            object? value;
            try
            {
                value = member switch
                {
                    PropertyInfo property when property.GetIndexParameters().Length == 0 => property.GetValue(target),
                    FieldInfo field => field.GetValue(target),
                    _ => null
                };
            }
            catch (Exception ex)
            {
                monitor.Log($"{label}.{member.Name}=<getter failed: {ex.GetType().Name}>", LogLevel.Trace);
                continue;
            }

            monitor.Log($"{label}.{member.Name}={FormatValue(value)}", LogLevel.Info);
        }
    }

    private static object? GetMemberValue(object target, string name)
    {
        Type type = target.GetType();
        PropertyInfo? property = type.GetProperty(name, InstanceFlags);
        if (property is not null && property.GetIndexParameters().Length == 0)
        {
            try
            {
                return property.GetValue(target);
            }
            catch
            {
                return null;
            }
        }

        FieldInfo? field = type.GetField(name, InstanceFlags);
        if (field is null)
            return null;

        try
        {
            return field.GetValue(target);
        }
        catch
        {
            return null;
        }
    }

    private static void LogCollectionKeyProbe(IMonitor monitor, object target, string memberName, Vector2 key)
    {
        object? collection = GetMemberValue(target, memberName);
        if (collection is null)
        {
            monitor.Log($"{memberName}: unavailable.", LogLevel.Trace);
            return;
        }

        MethodInfo? containsKey = collection.GetType().GetMethod("ContainsKey", new[] { typeof(Vector2) });
        if (containsKey is null)
        {
            monitor.Log($"{memberName}: type={collection.GetType().FullName}; no Vector2 ContainsKey method.", LogLevel.Trace);
            return;
        }

        try
        {
            bool contains = containsKey.Invoke(collection, new object[] { key }) as bool? == true;
            monitor.Log($"{memberName}.ContainsKey({key.X},{key.Y})={contains}.", LogLevel.Info);
        }
        catch (Exception ex)
        {
            monitor.Log($"{memberName} key probe failed: {ex.GetType().Name}.", LogLevel.Trace);
        }
    }

    private static string FormatValue(object? value)
    {
        if (value is null)
            return "null";
        if (value is string text)
            return text.Length <= 180 ? text : text[..180] + "…";
        if (value is IEnumerable and not string)
            return $"<{value.GetType().FullName}>";
        return value.ToString() ?? $"<{value.GetType().FullName}>";
    }
}
