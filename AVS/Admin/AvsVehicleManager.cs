using AVS.BaseVehicle;
using AVS.Log;
using AVS.SaveLoad;
using AVS.Util;
using Nautilus.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AVS;

/// <summary>
/// Provides management functions for AVS vehicles, including registration, enrollment, and loading.
/// </summary>
internal static class AvsVehicleManager
{
    /// <summary>
    /// List of all AVS vehicles currently in play.
    /// </summary>
    public static List<AvsVehicle> VehiclesInPlay { get; } = new();

    /// <summary>
    /// List of all registered ping instances for vehicles.
    /// </summary>
    public static List<PingInstance> MvPings { get; } = new();

    /// <summary>
    /// List of all registered vehicle types.
    /// </summary>
    public static List<VehicleEntry> VehicleTypes { get; } = new();

    /// <summary>
    /// Registers a new <see cref="PingType"/> for a vehicle, ensuring it is unique and above the minimum value.
    /// Optionally logs the registration process.
    /// </summary>
    /// <param name="v">The vehicle for which the ping type is being registered</param>
    /// <param name="pt">The initial ping type to register.</param>
    /// <param name="verbose">If true, logs detailed registration steps.</param>
    /// <returns>The registered, unique ping type.</returns>
    public static PingType RegisterPingType(AvsVehicle v, PingType pt, bool verbose)
    {

        using var log = v.NewAvsLog();
        var ret = pt;
        if ((int)ret < 1000)
        {
            VehicleRegistrar.VerboseLog(log, VehicleRegistrar.LogType.Log, verbose,
                "PingType " + pt + " was too small. Trying 1000.");
            ret = (PingType)1000;
        }

        while (MvPings.Any(x => x.pingType == ret))
        {
            VehicleRegistrar.VerboseLog(log, VehicleRegistrar.LogType.Log, verbose,
                "PingType " + ret + " was taken.");
            ret++;
        }

        VehicleRegistrar.VerboseLog(log, VehicleRegistrar.LogType.Log, verbose,
            "Registering PingType " + ret + ".");
        return ret;
    }

    /// <summary>
    /// Enrolls a vehicle into the <see cref="VehiclesInPlay"/> list and starts loading it if constructed.
    /// </summary>
    /// <param name="av">The vehicle to enroll.</param>
    /// <param name="rmc">The root mod controller instance used to start coroutines.</param>
    public static void EnrollVehicle(RootModController rmc, AvsVehicle av)
    {
        if (av.name.Contains("Clone") && !VehiclesInPlay.Contains(av))
        {
            using var log = av.NewAvsLog();
            VehiclesInPlay.Add(av);
            log.Write("Enrolled the " + av.name + " : " + av.GetName() + " : " + av.subName);
            if (!av.GetComponent<VFXConstructing>() || av.GetComponent<VFXConstructing>().constructed > 3f)
                rmc
                    .StartAvsCoroutine(
                        nameof(AvsVehicleManager) + '.' + nameof(LoadVehicle),
                        log => LoadVehicle(log,
                            av)); // I wish I knew a good way to optionally NOT do this if this sub is being constructed rn
        }
    }

    /// <summary>
    /// Removes a vehicle from the <see cref="VehiclesInPlay"/> list.
    /// </summary>
    /// <param name="av">The vehicle to deregister.</param>
    public static void DeregisterVehicle(AvsVehicle av)
    {
        VehiclesInPlay.Remove(av);
    }

    /// <summary>
    /// Coroutine that waits for the world to be ready, then calls <see cref="AvsVehicle.OnFinishedLoading"/> on the vehicle.
    /// </summary>
    /// <param name="log">The log to write to.</param>
    /// <param name="av">The vehicle to load.</param>
    /// <returns>Coroutine enumerator.</returns>
    private static IEnumerator LoadVehicle(SmartLog log, AvsVehicle av)
    {
        // See SaveData.cs
        yield return new WaitUntil(() => LargeWorldStreamer.main.IsNotNull());
        yield return new WaitUntil(() => LargeWorldStreamer.main.IsReady());
        yield return new WaitUntil(() => LargeWorldStreamer.main.IsWorldSettled());
        yield return new WaitUntil(() => !WaitScreen.IsWaiting);
        log.Write($"Loading: {av.GetName()}");
        //if (mv.liveMixin.health == 0)
        //{
        //    mv.OnKill();
        //}
        av.OnFinishedLoading();
    }

    internal static void CreateSpritesFile(RootModController rmc, JsonFileEventArgs e)
    {
        SaveFiles.Current.WriteReflected(
            Patches.SaveLoadManagerPatcher.GetSaveFileSpritesFileName(rmc),
            VehicleTypes.Select(x => x.TechType).Where(GameInfoIcon.Has).Select(x => x.AsString()).ToList(),
            rmc
        );
    }

    internal static void Add(VehicleEntry newVE)
    {
        VehicleTypes.Add(newVE);
        //if (PingManager.sCachedPingTypeStrings.IsNull())
        //{
        //    LogWriter.Default.Error("PingManager.sCachedPingTypeStrings was null. Cannot add to cache");
        //}
        //else
        //    PingManager.sCachedPingTypeStrings.valueToString.Add(newVE.pt, newVE.name);

        //var group = SpriteManager.Group.Pings;
        //var name = newVE.name;
        //if (SpriteManager.mapping.IsNull())
        //    LogWriter.Default.Error("[PingSprite] SpriteManager.mapping was null. Cannot register ping sprite.");
        //else
        //{
        //    if (SpriteManager.mapping.TryGetValue(group, out var atlasName))
        //    {
        //        var atlas = Atlas.GetAtlas(atlasName);
        //        if (atlas.IsNotNull())
        //        {
        //            atlas.nameToSprite.Add(name, newVE.ping_sprite);
        //            LogWriter.Default.Write($"[PingSprite] Ping sprite {newVE.ping_sprite?.texture.NiceName()} registered in atlas");
        //        }
        //        else
        //            LogWriter.Default.Error($"[PingSprite] SpriteManager.mapping contained group {group} but Atlas.GetAtlas returned null. Cannot register ping sprite.");


        //    }
        //    else
        //    {
        //        LogWriter.Default.Error($"[PingSprite] SpriteManager.mapping did not contain group {group}. Cannot register ping sprite.");
        //    }


        //}
        //if (SpriteManager.groups.IsNull())
        //    LogWriter.Default.Error("[PingSprite] SpriteManager.groups was null. Cannot register ping sprite.");
        //else
        //{
        //    if (SpriteManager.groups.TryGetValue(group, out var resourceGroup))
        //    {
        //        resourceGroup.Add(name, newVE.ping_sprite);
        //        LogWriter.Default.Write($"[PingSprite] Ping sprite {newVE.ping_sprite?.texture.NiceName()} registered in group");
        //    }
        //    else
        //        LogWriter.Default.Error($"[PingSprite] SpriteManager.groups did not contain group Pings. Cannot register ping sprite.");
        //}
    }
}