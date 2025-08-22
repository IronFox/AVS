using AVS.BaseVehicle;
using AVS.Log;
using AVS.VehicleTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AVS.Assets;
using AVS.Util;
using AVS.VehicleBuilding;
using UnityEngine;

namespace AVS;

/// <summary>
/// Handles registration, validation, and queuing of mod vehicles for the AVS system.
/// Provides methods for registering vehicles, validating their configuration, and logging registration events.
/// </summary>
public static class VehicleRegistrar
{
    /// <summary>
    /// The number of vehicles successfully registered.
    /// </summary>
    public static int VehiclesRegistered { get; private set; } = 0;

    /// <summary>
    /// The number of vehicles prefabricated.
    /// </summary>
    public static int VehiclesPrefabricated { get; private set; } = 0;

    private static Queue<Action> RegistrationQueue { get; } = new();
    private static bool RegistrySemaphore { get; set; } = false;

    /// <summary>
    /// Specifies the type of log message for verbose logging.
    /// </summary>
    public enum LogType
    {
        /// <summary>
        /// Standard log message.
        /// </summary>
        Log,

        /// <summary>
        /// Warning log message.
        /// </summary>
        Warn
    }

    /// <summary>
    /// Logs a message if verbose logging is enabled, using the specified log type.
    /// </summary>
    /// <param name="type">The type of the log message.</param>
    /// <param name="verbose">Whether verbose logging is enabled.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="log">Out logger</param>
    public static void VerboseLog(LogWriter log, LogType type, bool verbose, string message)
    {
        if (verbose)
            switch (type)
            {
                case LogType.Log:
                    log.Write(message);
                    break;
                case LogType.Warn:
                    log.Warn(message);
                    break;
                default:
                    break;
            }
    }

    /// <summary>
    /// Registers a vehicle asynchronously by starting a coroutine.
    /// </summary>
    /// <remarks>Calls <see cref="RegisterVehicle(AvsVehicle, bool)"/></remarks>
    /// <param name="mv">The mod vehicle to register.</param>
    /// <param name="verbose">Whether to enable verbose logging.</param>
    public static void RegisterVehicleLater(AvsVehicle mv, bool verbose = false)
    {
        MainPatcher.Instance.StartCoroutine(RegisterVehicle(mv, verbose));
    }

    /// <summary>
    /// Coroutine for registering a mod vehicle, including validation and queuing if necessary.
    /// </summary>
    /// <param name="mv">The mod vehicle to register.</param>
    /// <param name="verbose">Whether to enable verbose logging.</param>
    /// <returns>IEnumerator for coroutine execution.</returns>
    public static IEnumerator RegisterVehicle(AvsVehicle mv, bool verbose = false)
    {
        var log = mv.Log.Tag(nameof(RegisterVehicle));
        mv.OnAwakeOrPrefabricate();
        yield return SeamothHelper.WaitUntilLoaded();
        if (AvsVehicleManager.VehicleTypes.Any(x => x.name == mv.gameObject.name))
        {
            VerboseLog(log, LogType.Warn, verbose, $"{mv.gameObject.name} was already registered.");
            yield break;
        }

        VerboseLog(log, LogType.Log, verbose, $"The {mv.gameObject.name} is beginning validation.");
        if (!ValidateAll(mv, verbose))
        {
            log.Error($"{mv.gameObject.name} failed validation. Not registered.");
            Logger.LoopMainMenuError($"Failed validation. Not registered. See log.", mv.gameObject.name);
            yield break;
        }

        if (RegistrySemaphore)
        {
            VerboseLog(log, LogType.Log, verbose, $"Enqueueing the {mv.gameObject.name} for Registration.");
            RegistrationQueue.Enqueue(() => MainPatcher.Instance.StartCoroutine(InternalRegisterVehicle(mv, verbose)));
            yield return new WaitUntil(() => AvsVehicleManager.VehicleTypes.Select(x => x.mv).Contains(mv));
        }
        else
        {
            yield return MainPatcher.Instance.StartCoroutine(InternalRegisterVehicle(mv, verbose));
        }
    }


    /// <summary>
    /// Internal coroutine for registering a mod vehicle, including prefab creation and queue management.
    /// </summary>
    /// <param name="mv">The mod vehicle to register.</param>
    /// <param name="verbose">Whether to enable verbose logging.</param>
    /// <returns>IEnumerator for coroutine execution.</returns>
    private static IEnumerator InternalRegisterVehicle(AvsVehicle mv, bool verbose)
    {
        var log = mv.Log.Tag(nameof(InternalRegisterVehicle));
        RegistrySemaphore = true;
        VerboseLog(log, LogType.Log, verbose, $"The {mv.gameObject.name} is beginning Registration.");
        var registeredPingType = AvsVehicleManager.RegisterPingType(mv, (PingType)121, verbose);
        yield return MainPatcher.Instance.StartCoroutine(
            AvsVehicleBuilder.Prefabricate(mv, registeredPingType, verbose));
        RegistrySemaphore = false;
        log.Write($"Finished {mv.gameObject.name} registration.");
        VehiclesRegistered++;
        if (RegistrationQueue.Count > 0)
            RegistrationQueue.Dequeue().Invoke();
    }

    /// <summary>
    /// Validates a mod vehicle and its specific type (Submarine, Submersible, Skimmer).
    /// </summary>
    /// <param name="mv">The mod vehicle to validate.</param>
    /// <param name="verbose">Whether to enable verbose logging.</param>
    /// <returns>True if the vehicle is valid; otherwise, false.</returns>
    public static bool ValidateAll(AvsVehicle mv, bool verbose)
    {
        var log = mv.Log.Tag(nameof(ValidateAll));
        if (mv is Submarine sub1)
            if (!ValidateRegistration(sub1, verbose))
            {
                log.Error("Invalid Submarine Registration for the " + mv.gameObject.name + ". Next.");
                return false;
            }

        if (mv is Submersible sub2)
            if (!ValidateRegistration(sub2, verbose))
            {
                log.Error("Invalid Submersible Registration for the " + mv.gameObject.name + ". Next.");
                return false;
            }

        if (mv is Skimmer sk)
            if (!ValidateRegistration(sk, verbose))
            {
                log.Error("Invalid Submersible Registration for the " + mv.gameObject.name + ". Next.");
                return false;
            }

        return true;
    }

    /// <summary>
    /// Validates the registration of a mod vehicle, checking required fields and configuration.
    /// </summary>
    /// <param name="mv">The mod vehicle to validate.</param>
    /// <param name="verbose">Whether to enable verbose logging.</param>
    /// <returns>True if the vehicle is valid; otherwise, false.</returns>
    public static bool ValidateRegistration(AvsVehicle mv, bool verbose)
    {
        var thisName = "";
        var log = mv.Log.Tag(nameof(ValidateRegistration));
        try
        {
            if (mv.IsNull())
            {
                log.Error("An null mod vehicle was passed for registration.");
                return false;
            }

            if (mv.name == "")
            {
                log.Error(thisName + "An empty name was provided for this vehicle.");
                return false;
            }

            if (mv.Com.CollisionModel.IsNull())
            {
                log.Error(thisName + "A null VehicleComposition.CollisionModel was passed for registration." +
                          " This would lead to null reference exceptions in the Subnautica vehicle system");
                return false;
            }

            if (mv.Com.CollisionModel.Contains(mv.gameObject))
            {
                log.Error(thisName + "Collision model must not be same as the vehicle root." +
                          " Subnautica would disable the entire vehicle on dock.");
                return false;
            }

            VerboseLog(log, LogType.Log, verbose, "Validating the Registration of the " + mv.name);
            thisName = mv.name + ": ";
            if (!mv.VehicleRoot)
            {
                log.Error(thisName +
                          $"A null {nameof(AvsVehicle)}.{nameof(AvsVehicle.VehicleRoot)} was passed for registration.");
                return false;
            }

            if (mv.Config.BaseCrushDepth < 0)
            {
                log.Error(thisName +
                          "A negative crush depth was passed for registration. This vehicle would take crush damage even out of water.");
                return false;
            }

            if (mv.Config.MaxHealth <= 0)
            {
                log.Error(thisName +
                          "A non-positive max health was passed for registration. This vehicle would be destroyed as soon as it awakens.");
                return false;
            }

            if (mv.Config.Mass <= 0)
            {
                log.Error(thisName + "A non-positive mass was passed for registration. Don't do that.");
                return false;
            }

            if (mv.Com.InnateStorages.Count == 0)
                VerboseLog(log, LogType.Warn, verbose,
                    thisName +
                    $"No {nameof(AvsVehicle)}.InnateStorages were provided. These are lockers the vehicle always has.");
            if (mv.Com.ModularStorages.Count == 0)
                VerboseLog(log, LogType.Log, verbose,
                    thisName +
                    $"No {nameof(AvsVehicle)}.ModularStorages were provided. These are lockers that can be unlocked with upgrades.");
            if (mv.Com.Upgrades.Count == 0)
                log.Warn(thisName +
                         $"No {nameof(AvsVehicle)}.Upgrades were provided. These specify interfaces the player can click to insert and remove upgrades.");
            if (mv.Com.Batteries.Count == 0)
                log.Warn(thisName +
                         $"No {nameof(AvsVehicle)}.Batteries were provided. These are necessary to power the engines. This vehicle will be always powered.");
            if (mv.Com.BackupBatteries.Count == 0)
                VerboseLog(log, LogType.Log, verbose,
                    thisName +
                    $"No {nameof(AvsVehicle)}.BackupBatteries were provided. This collection of batteries belong to the AI and will be used exclusively for life support, auto-leveling, and other AI tasks. The AI will use the main batteries instead.");
            if (mv.Com.Headlights.Count == 0)
                VerboseLog(log, LogType.Warn, verbose,
                    thisName +
                    $"No {nameof(AvsVehicle)}.Headlights were provided. These lights would be activated when the player right clicks while piloting.");
            if (mv.Com.WaterClipProxies.Count == 0)
                VerboseLog(log, LogType.Warn, verbose,
                    thisName +
                    $"No {nameof(AvsVehicle)}.WaterClipProxies were provided. These are necessary to keep the ocean surface out of the vehicle.");
            if (mv.Com.CanopyWindows.Count == 0)
                VerboseLog(log, LogType.Warn, verbose,
                    thisName +
                    $"No {nameof(AvsVehicle)}.CanopyWindows were provided. These must be specified to handle window transparencies.");
            if (!mv.Com.BoundingBoxCollider)
                VerboseLog(log, LogType.Warn, verbose,
                    thisName +
                    "No BoundingBox BoxCollider was provided. If a BoundingBox GameObject was provided, it did not have a BoxCollider. Tether range is 10 meters. This vehicle will not be able to dock in the Moonpool. The build bots will assume this vehicle is 6m x 8m x 12m.");
            if (mv.Com.CollisionModel.IsNull() || mv.Com.CollisionModel.Length == 0)
            {
                VerboseLog(log, LogType.Warn, verbose,
                    thisName +
                    $"A null {nameof(AvsVehicle)}.CollisionModel was provided. This is necessary for leviathans to grab the vehicle.");
                return false;
            }

            foreach (var vs in mv.Com.InnateStorages.Concat(mv.Com.ModularStorages))
                if (!vs.CheckValidity(log, thisName))
                    return false;

            foreach (var vu in mv.Com.Upgrades)
                if (!vu.CheckValidity(log, thisName, verbose))
                    return false;

            foreach (var vb in mv.Com.Batteries.Concat(mv.Com.BackupBatteries))
                if (!vb.CheckValidity(log, thisName, verbose))
                    return false;

            foreach (var vfl in mv.Com.Headlights)
                if (!vfl.CheckValidity(log, thisName))
                    return false;

            if (mv.Com.StorageRootObject.IsNull())
            {
                log.Error(thisName +
                          $"A null {nameof(AvsVehicle)}.StorageRootObject was provided. There would be no way to store things in this vehicle.");
                return false;
            }

            if (mv.Com.ModulesRootObject.IsNull())
            {
                log.Error(thisName +
                          $"A null {nameof(AvsVehicle)}.ModulesRootObject was provided. There would be no way to upgrade this vehicle.");
                return false;
            }

            if (mv.Com.StorageRootObject == mv.gameObject)
            {
                log.Error(thisName +
                          "The StorageRootObject was the same as the Vehicle itself. These must be uniquely identifiable objects!");
                return false;
            }

            if (mv.Com.ModulesRootObject == mv.gameObject)
            {
                log.Error(thisName +
                          "The ModulesRootObject was the same as the Vehicle itself. These must be uniquely identifiable objects!");
                return false;
            }

            if (!mv.Com.LeviathanGrabPoint)
                VerboseLog(log, LogType.Warn, verbose,
                    thisName +
                    $"A null {nameof(AvsVehicle)}.LeviathanGrabPoint was provided. This is where leviathans attach to the vehicle. The root object will be used instead.");
            if (!mv.Com.Engine)
                VerboseLog(log, LogType.Warn, verbose,
                    thisName +
                    $"A null {nameof(AvsVehicle)}.Com.Engine was passed for registration. A default engine will be chosen.");
            if (!mv.Config.Recipe.CheckValidity(mv.name))
                return false;
        }
        catch (Exception e)
        {
            log.Error(thisName +
                      $"Exception Caught. Likely this {nameof(AvsVehicle)} is not implementing something it must. Check the abstract features of {nameof(AvsVehicle)}",
                e);
            return false;
        }

        VerboseLog(log, LogType.Log, verbose,
            $"The Registration of the '{mv.name}' as a {nameof(AvsVehicle)} has been Validated.");
        return true;
    }

    /// <summary>
    /// Validates the registration of a Submarine, including submarine-specific requirements.
    /// </summary>
    /// <param name="mv">The submarine to validate.</param>
    /// <param name="verbose">Whether to enable verbose logging.</param>
    /// <returns>True if the submarine is valid; otherwise, false.</returns>
    public static bool ValidateRegistration(Submarine mv, bool verbose)
    {
        if (!ValidateRegistration(mv as AvsVehicle, verbose))
            return false;
        var log = mv.Log.Tag(nameof(ValidateRegistration));
        var thisName = "";
        try
        {
            thisName = mv.name + ": ";
            if (mv.Com.Helms.Count == 0)
            {
                log.Error(thisName +
                          $"No {nameof(Submarine)}.Com.Helms were provided. These specify what the player will click on to begin piloting the vehicle.");
                return false;
            }

            if (mv.Com.Hatches.Count == 0)
            {
                log.Error(thisName +
                          $"No {nameof(AvsVehicle)}.Com.Hatches were provided. These specify how the player will enter and exit the vehicle.");
                return false;
            }

            if (mv.Com.Floodlights.Count == 0)
                VerboseLog(log, LogType.Warn, verbose,
                    thisName +
                    $"No {nameof(AvsVehicle)}.Com.Floodlights were provided. These lights would be activated on the control panel.");
            if (mv.Com.NavigationPortLights.Count == 0)
                VerboseLog(log, LogType.Log, verbose, thisName + "Some navigation lights were missing.");
            if (mv.Com.NavigationStarboardLights.Count == 0)
                VerboseLog(log, LogType.Log, verbose, thisName + "Some navigation lights were missing.");
            if (mv.Com.NavigationPositionLights.Count == 0)
                VerboseLog(log, LogType.Log, verbose, thisName + "Some navigation lights were missing.");
            if (mv.Com.NavigationWhiteStrobeLights.Count == 0)
                VerboseLog(log, LogType.Log, verbose, thisName + "Some navigation lights were missing.");
            if (mv.Com.NavigationRedStrobeLights.Count == 0)
                VerboseLog(log, LogType.Log, verbose, thisName + "Some navigation lights were missing.");
            if (mv.Com.TetherSources.Count == 0)
            {
                log.Error(thisName +
                          $"No {nameof(Submarine)}.Com.TetherSources were provided. These are necessary to keep the player 'grounded' within the vehicle.");
                return false;
            }

            if (mv.Com.ColorPicker.IsNull())
                VerboseLog(log, LogType.Log, verbose,
                    thisName +
                    $"A null {nameof(Submarine)}.Com.ColorPicker was provided. You only need this if you implement the necessary painting functions.");
            if (mv.Com.Fabricator.IsNull())
                VerboseLog(log, LogType.Warn, verbose,
                    thisName +
                    $"A null {nameof(Submarine)}.Com.Fabricator was provided. The Submarine will not come with a fabricator at construction-time.");
            if (mv.Com.ControlPanel.IsNull())
                VerboseLog(log, LogType.Warn, verbose,
                    thisName +
                    $"A null {nameof(Submarine)}.Com.ControlPanel was provided. This is necessary to toggle floodlights.");
            foreach (var ps in mv.Com.Helms)
                if (!ps.CheckValidity(log, thisName, verbose))
                    return false;
            foreach (var vhs in mv.Com.Hatches)
                if (!vhs.CheckValidity(log, thisName, verbose))
                    return false;
            foreach (var vs in mv.Com.ModularStorages)
                if (!vs.CheckValidity(log, thisName))
                    return false;
            foreach (var vfl in mv.Com.Floodlights)
                if (!vfl.CheckValidity(log, thisName))
                    return false;
        }
        catch (Exception e)
        {
            log.Error(thisName +
                      "Exception Caught. Likely this Submarine is not implementing something it must. Check the abstract features of Submarine",
                e);
            return false;
        }

        VerboseLog(log, LogType.Log, verbose,
            "The Registration of the " + mv.name + " as a Submarine has been Validated.");
        return true;
    }

    /// <summary>
    /// Validates the registration of a Submersible, including submersible-specific requirements.
    /// </summary>
    /// <param name="mv">The submersible to validate.</param>
    /// <param name="verbose">Whether to enable verbose logging.</param>
    /// <returns>True if the submersible is valid; otherwise, false.</returns>
    public static bool ValidateRegistration(Submersible mv, bool verbose)
    {
        if (!ValidateRegistration(mv as AvsVehicle, verbose))
            return false;
        var log = mv.Log.Tag(nameof(ValidateRegistration));
        var thisName = "";
        try
        {
            thisName = mv.name + ": ";
            if (mv.Com.Hatches.Count == 0)
            {
                log.Error(thisName +
                          $"No {nameof(AvsVehicle)}.Com.Hatches were provided. These specify how the player will enter and exit the vehicle.");
                return false;
            }

            if (!mv.Com.PilotSeat.CheckValidity(log, thisName, verbose))
                return false;
            foreach (var vhs in mv.Com.Hatches)
                if (!vhs.CheckValidity(log, thisName, verbose))
                    return false;
            foreach (var vs in mv.Com.ModularStorages)
                if (!vs.CheckValidity(log, thisName))
                    return false;
        }
        catch (Exception e)
        {
            log.Error(thisName +
                      "Exception Caught. Likely this Submersible is not implementing something it must. Check the abstract features of Submersible",
                e);
            return false;
        }

        VerboseLog(log, LogType.Log, verbose,
            "The Registration of the " + mv.name + " as a Submersible has been Validated.");
        return true;
    }
}