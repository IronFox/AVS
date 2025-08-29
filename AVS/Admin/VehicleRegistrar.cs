using AVS.Assets;
using AVS.BaseVehicle;
using AVS.Log;
using AVS.Util;
using AVS.VehicleTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public static void VerboseLog(SmartLog log, LogType type, bool verbose, string message)
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
    /// <remarks>Calls <see cref="RegisterVehicle"/> as a new coroutine</remarks>
    /// <param name="rmc">The owning root mod controller instance.</param>
    /// <param name="av">The mod vehicle to register.</param>
    /// <param name="verbose">Whether to enable verbose logging.</param>
    public static void RegisterVehicleLater(RootModController rmc, AvsVehicle av, bool verbose = false)
    {
        rmc.StartAvsCoroutine(nameof(VehicleRegistrar) + '.' + nameof(RegisterVehicle), log => RegisterVehicle(log, rmc, av, verbose));
    }

    /// <summary>
    /// Coroutine for registering a mod vehicle, including validation and queuing if necessary.
    /// </summary>
    /// <param name="rmc">The owning root mod controller instance.</param>
    /// <param name="av">The mod vehicle to register.</param>
    /// <param name="verbose">Whether to enable verbose logging.</param>
    /// <param name="log">Out logger</param>
    /// <returns>IEnumerator for coroutine execution.</returns>
    public static IEnumerator RegisterVehicle(SmartLog log, RootModController rmc, AvsVehicle av, bool verbose = false)
    {
        av.mainPatcherInstanceId = rmc.GetInstanceID();
        av.OnAwakeOrPrefabricate();
        yield return SeamothHelper.WaitUntilLoaded();
        if (AvsVehicleManager.VehicleTypes.Any(x => x.Name == av.gameObject.name))
        {
            VerboseLog(log, LogType.Warn, verbose, $"{av.gameObject.name} was already registered.");
            yield break;
        }

        VerboseLog(log, LogType.Log, verbose, $"The {av.gameObject.name} is beginning validation.");
        if (!ValidateAll(rmc, av, verbose))
        {
            log.Error($"{av.gameObject.name} failed validation. Not registered.");
            Logger.LoopMainMenuError($"Failed validation. Not registered. See log.", av.gameObject.name);
            yield break;
        }

        if (RegistrySemaphore)
        {
            VerboseLog(log, LogType.Log, verbose, $"Enqueueing the {av.gameObject.name} for Registration.");
            RegistrationQueue.Enqueue(() => rmc.StartAvsCoroutine(nameof(VehicleRegistrar) + '.' + nameof(InternalRegisterVehicle), log => InternalRegisterVehicle(log, rmc, av, verbose)));
            yield return new WaitUntil(() => AvsVehicleManager.VehicleTypes.Select(x => x.AV).Contains(av));
        }
        else
        {
            yield return rmc.StartAvsCoroutine(nameof(VehicleRegistrar) + '.' + nameof(InternalRegisterVehicle), log => InternalRegisterVehicle(log, rmc, av, verbose));
        }
    }


    /// <summary>
    /// Internal coroutine for registering a mod vehicle, including prefab creation and queue management.
    /// </summary>
    /// <param name="log">Out logger</param>
    /// <param name="rmc">The owning root mod controller instance.</param>
    /// <param name="av">The mod vehicle to register.</param>
    /// <param name="verbose">Whether to enable verbose logging.</param>
    /// <returns>IEnumerator for coroutine execution.</returns>
    private static IEnumerator InternalRegisterVehicle(SmartLog log, RootModController rmc, AvsVehicle av, bool verbose)
    {
        RegistrySemaphore = true;
        VerboseLog(log, LogType.Log, verbose, $"The {av.gameObject.name} is beginning Registration.");
        var registeredPingType = AvsVehicleManager.RegisterPingType(av,
            (PingType)(0xFF + (rmc.ModName.GetHashCode() & 0x7FFFFF)), verbose);
        yield return AvsVehicleBuilder.Prefabricate(log, rmc, av, registeredPingType, verbose);
        RegistrySemaphore = false;
        log.Write($"Finished {av.gameObject.name} registration for ping type {registeredPingType}.");
        VehiclesRegistered++;
        if (RegistrationQueue.Count > 0)
            RegistrationQueue.Dequeue().Invoke();
    }

    /// <summary>
    /// Validates a mod vehicle and its specific type (Submarine, Submersible, Skimmer).
    /// </summary>
    /// <param name="av">The mod vehicle to validate.</param>
    /// <param name="verbose">Whether to enable verbose logging.</param>
    /// <param name="rmc">The root mod controller instance used for logging.</param>
    /// <returns>True if the vehicle is valid; otherwise, false.</returns>
    public static bool ValidateAll(RootModController rmc, AvsVehicle av, bool verbose)
    {
        using var log = av.NewAvsLog();
        if (av is Submarine sub1)
            if (!ValidateRegistration(rmc, sub1, verbose))
            {
                log.Error("Invalid Submarine Registration for the " + av.gameObject.name + ". Next.");
                return false;
            }

        if (av is Submersible sub2)
            if (!ValidateRegistration(rmc, sub2, verbose))
            {
                log.Error("Invalid Submersible Registration for the " + av.gameObject.name + ". Next.");
                return false;
            }

        if (av is Skimmer sk)
            if (!ValidateRegistration(rmc, sk, verbose))
            {
                log.Error("Invalid Submersible Registration for the " + av.gameObject.name + ". Next.");
                return false;
            }

        return true;
    }

    /// <summary>
    /// Validates the registration of a mod vehicle, checking required fields and configuration.
    /// </summary>
    /// <param name="av">The mod vehicle to validate.</param>
    /// <param name="rmc">The root mod controller instance used for logging.</param>
    /// <param name="verbose">Whether to enable verbose logging.</param>
    /// <returns>True if the vehicle is valid; otherwise, false.</returns>
    public static bool ValidateRegistration(RootModController rmc, AvsVehicle av, bool verbose)
    {
        var thisName = "";
        using var log = av.NewAvsLog();
        try
        {
            if (av.IsNull())
            {
                log.Error("An null mod vehicle was passed for registration.");
                return false;
            }

            if (av.name == "")
            {
                log.Error(thisName + "An empty name was provided for this vehicle.");
                return false;
            }

            if (av.Com.CollisionModel.IsNull())
            {
                log.Error(thisName + "A null VehicleComposition.CollisionModel was passed for registration." +
                          " This would lead to null reference exceptions in the Subnautica vehicle system");
                return false;
            }

            if (av.Com.CollisionModel.Contains(av.gameObject))
            {
                log.Error(thisName + "Collision model must not be same as the vehicle root." +
                          " Subnautica would disable the entire vehicle on dock.");
                return false;
            }

            VerboseLog(log, LogType.Log, verbose, "Validating the Registration of the " + av.name);
            thisName = av.name + ": ";
            if (!av.VehicleRoot)
            {
                log.Error(thisName +
                          $"A null {nameof(AvsVehicle)}.{nameof(AvsVehicle.VehicleRoot)} was passed for registration.");
                return false;
            }

            if (av.Config.BaseCrushDepth < 0)
            {
                log.Error(thisName +
                          "A negative crush depth was passed for registration. This vehicle would take crush damage even out of water.");
                return false;
            }

            if (av.Config.MaxHealth <= 0)
            {
                log.Error(thisName +
                          "A non-positive max health was passed for registration. This vehicle would be destroyed as soon as it awakens.");
                return false;
            }

            if (av.Config.Mass <= 0)
            {
                log.Error(thisName + "A non-positive mass was passed for registration. Don't do that.");
                return false;
            }

            if (av.Com.InnateStorages.Count == 0)
                VerboseLog(log, LogType.Log, verbose,
                    thisName +
                    $"No {nameof(AvsVehicle)}.InnateStorages were provided. These are lockers the vehicle always has.");
            if (av.Com.ModularStorages.Count == 0)
                VerboseLog(log, LogType.Log, verbose,
                    thisName +
                    $"No {nameof(AvsVehicle)}.ModularStorages were provided. These are lockers that can be unlocked with upgrades.");
            if (av.Com.Upgrades.Count == 0)
                log.Warn(thisName +
                         $"No {nameof(AvsVehicle)}.Upgrades were provided. These specify interfaces the player can click to insert and remove upgrades.");
            if (av.Com.Batteries.Count == 0)
                log.Warn(thisName +
                         $"No {nameof(AvsVehicle)}.Batteries were provided. These are necessary to power the engines. This vehicle will be always powered.");
            if (av.Com.BackupBatteries.Count == 0)
                VerboseLog(log, LogType.Log, verbose,
                    thisName +
                    $"No {nameof(AvsVehicle)}.BackupBatteries were provided. This collection of batteries belong to the AI and will be used exclusively for life support, auto-leveling, and other AI tasks. The AI will use the main batteries instead.");
            if (av.Com.Headlights.Count == 0)
                VerboseLog(log, LogType.Log, verbose,
                    thisName +
                    $"No {nameof(AvsVehicle)}.Headlights were provided. These lights would be activated when the player right clicks while piloting.");
            if (av.Com.WaterClipProxies.Count == 0)
                VerboseLog(log, LogType.Warn, verbose,
                    thisName +
                    $"No {nameof(AvsVehicle)}.WaterClipProxies were provided. These are necessary to keep the ocean surface out of the vehicle.");
            if (av.Com.CanopyWindows.Count == 0)
                VerboseLog(log, LogType.Log, verbose,
                    thisName +
                    $"No {nameof(AvsVehicle)}.CanopyWindows were provided. These must be specified to handle window transparencies.");
            if (!av.Com.BoundingBoxCollider)
                VerboseLog(log, LogType.Warn, verbose,
                    thisName +
                    "No BoundingBox BoxCollider was provided. If a BoundingBox GameObject was provided, it did not have a BoxCollider. Tether range is 10 meters. This vehicle will not be able to dock in the Moonpool. The build bots will assume this vehicle is 6m x 8m x 12m.");
            if (av.Com.CollisionModel.IsNull() || av.Com.CollisionModel.Length == 0)
            {
                VerboseLog(log, LogType.Log, verbose,
                    thisName +
                    $"A null {nameof(AvsVehicle)}.CollisionModel was provided. This is necessary for leviathans to grab the vehicle.");
                return false;
            }

            foreach (var vs in av.Com.InnateStorages.Concat(av.Com.ModularStorages))
                if (!vs.CheckValidity(rmc, thisName))
                    return false;

            foreach (var vu in av.Com.Upgrades)
                if (!vu.CheckValidity(rmc, thisName, verbose))
                    return false;

            foreach (var vb in av.Com.Batteries.Concat(av.Com.BackupBatteries))
                if (!vb.CheckValidity(rmc, thisName, verbose))
                    return false;

            foreach (var vfl in av.Com.Headlights)
                if (!vfl.CheckValidity(rmc, thisName))
                    return false;

            if (av.Com.StorageRootObject.IsNull())
            {
                log.Error(thisName +
                          $"A null {nameof(AvsVehicle)}.StorageRootObject was provided. There would be no way to store things in this vehicle.");
                return false;
            }

            if (av.Com.ModulesRootObject.IsNull())
            {
                log.Error(thisName +
                          $"A null {nameof(AvsVehicle)}.ModulesRootObject was provided. There would be no way to upgrade this vehicle.");
                return false;
            }

            if (av.Com.StorageRootObject == av.gameObject)
            {
                log.Error(thisName +
                          "The StorageRootObject was the same as the Vehicle itself. These must be uniquely identifiable objects!");
                return false;
            }

            if (av.Com.ModulesRootObject == av.gameObject)
            {
                log.Error(thisName +
                          "The ModulesRootObject was the same as the Vehicle itself. These must be uniquely identifiable objects!");
                return false;
            }

            if (!av.Com.LeviathanGrabPoint)
                VerboseLog(log, LogType.Log, verbose,
                    thisName +
                    $"A null {nameof(AvsVehicle)}.LeviathanGrabPoint was provided. This is where leviathans attach to the vehicle. The root object will be used instead.");
            if (!av.Com.Engine)
                VerboseLog(log, LogType.Warn, verbose,
                    thisName +
                    $"A null {nameof(AvsVehicle)}.Com.Engine was passed for registration. A default engine will be chosen.");
            if (!av.Config.Recipe.CheckValidity(av.name))
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
            $"The Registration of the '{av.name}' as an {nameof(AvsVehicle)} has been validated successfully.");
        return true;
    }

    /// <summary>
    /// Validates the registration of a Submarine, including submarine-specific requirements.
    /// </summary>
    /// <param name="av">The submarine to validate.</param>
    /// <param name="rmc">The root mod controller instance used for logging.</param>
    /// <param name="verbose">Whether to enable verbose logging.</param>
    /// <returns>True if the submarine is valid; otherwise, false.</returns>
    public static bool ValidateRegistration(RootModController rmc, Submarine av, bool verbose)
    {
        if (!ValidateRegistration(rmc, av as AvsVehicle, verbose))
            return false;
        using var log = av.NewAvsLog();
        var thisName = "";
        try
        {
            thisName = av.name + ": ";
            if (av.Com.Helms.Count == 0)
            {
                log.Error(thisName +
                          $"No {nameof(Submarine)}.Com.Helms were provided. These specify what the player will click on to begin piloting the vehicle.");
                return false;
            }

            if (av.Com.Hatches.Count == 0)
            {
                log.Error(thisName +
                          $"No {nameof(AvsVehicle)}.Com.Hatches were provided. These specify how the player will enter and exit the vehicle.");
                return false;
            }

            if (av.Com.Floodlights.Count == 0)
                VerboseLog(log, LogType.Log, verbose,
                    thisName +
                    $"No {nameof(AvsVehicle)}.Com.Floodlights were provided. These lights would be activated on the control panel.");
            if (av.Com.NavigationPortLights.Count == 0)
                VerboseLog(log, LogType.Log, verbose, thisName + "Navigation port lights were not provided.");
            if (av.Com.NavigationStarboardLights.Count == 0)
                VerboseLog(log, LogType.Log, verbose, thisName + "Navigation starboard lights were not provided.");
            if (av.Com.NavigationPositionLights.Count == 0)
                VerboseLog(log, LogType.Log, verbose, thisName + "Navigation position lights were not provided.");
            if (av.Com.NavigationWhiteStrobeLights.Count == 0)
                VerboseLog(log, LogType.Log, verbose, thisName + "White strobe navigation lights were not provided.");
            if (av.Com.NavigationRedStrobeLights.Count == 0)
                VerboseLog(log, LogType.Log, verbose, thisName + "Red strobe navigation lights were not provided.");
            if (av.Com.TetherSources.Count == 0)
            {
                log.Error(thisName +
                          $"No {nameof(Submarine)}.Com.TetherSources were provided. These are necessary to keep the player 'grounded' within the vehicle.");
                return false;
            }

            if (av.Com.ColorPicker.IsNull())
                VerboseLog(log, LogType.Log, verbose,
                    thisName +
                    $"A null {nameof(Submarine)}.Com.ColorPicker was provided. You only need this if you implement the necessary painting functions.");
            if (av.Com.Fabricator.IsNull())
                VerboseLog(log, LogType.Log, verbose,
                    thisName +
                    $"A null {nameof(Submarine)}.Com.Fabricator was provided. The Submarine will not come with a fabricator at construction-time.");
            if (av.Com.ControlPanel.IsNull())
                VerboseLog(log, LogType.Log, verbose,
                    thisName +
                    $"A null {nameof(Submarine)}.Com.ControlPanel was provided. This is necessary to toggle floodlights.");
            foreach (var ps in av.Com.Helms)
                if (!ps.CheckValidity(rmc, thisName, verbose))
                    return false;
            foreach (var vhs in av.Com.Hatches)
                if (!vhs.CheckValidity(rmc, thisName, verbose))
                    return false;
            foreach (var vs in av.Com.ModularStorages)
                if (!vs.CheckValidity(rmc, thisName))
                    return false;
            foreach (var vfl in av.Com.Floodlights)
                if (!vfl.CheckValidity(rmc, thisName))
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
            "The Registration of the " + av.name + " as a Submarine has been validated successfully.");
        return true;
    }

    /// <summary>
    /// Validates the registration of a Submersible, including submersible-specific requirements.
    /// </summary>
    /// <param name="av">The submersible to validate.</param>
    /// <param name="verbose">Whether to enable verbose logging.</param>
    /// <param name="rmc">The root mod controller instance used for logging.</param>
    /// <returns>True if the submersible is valid; otherwise, false.</returns>
    public static bool ValidateRegistration(RootModController rmc, Submersible av, bool verbose)
    {
        if (!ValidateRegistration(rmc, av as AvsVehicle, verbose))
            return false;
        using var log = av.NewAvsLog();
        var thisName = "";
        try
        {
            thisName = av.name + ": ";
            if (av.Com.Hatches.Count == 0)
            {
                log.Error(thisName +
                          $"No {nameof(AvsVehicle)}.Com.Hatches were provided. These specify how the player will enter and exit the vehicle.");
                return false;
            }

            if (!av.Com.PilotSeat.CheckValidity(rmc, thisName, verbose))
                return false;
            foreach (var vhs in av.Com.Hatches)
                if (!vhs.CheckValidity(rmc, thisName, verbose))
                    return false;
            foreach (var vs in av.Com.ModularStorages)
                if (!vs.CheckValidity(rmc, thisName))
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
            "The Registration of the " + av.name + " as a Submersible has been validated successfully.");
        return true;
    }
}