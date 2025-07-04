using AVS.VehicleTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AVS
{
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

        private static Queue<Action> RegistrationQueue { get; } = new Queue<Action>();
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
        /// <param name="type">The type of log message.</param>
        /// <param name="verbose">Whether verbose logging is enabled.</param>
        /// <param name="message">The message to log.</param>
        public static void VerboseLog(LogType type, bool verbose, string message)
        {
            if (verbose)
            {
                switch (type)
                {
                    case LogType.Log:
                        Logger.Log(message);
                        break;
                    case LogType.Warn:
                        Logger.Warn(message);
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Registers a vehicle asynchronously by starting a coroutine.
        /// </summary>
        /// <remarks>Calls <see cref="RegisterVehicle(ModVehicle, bool)" /></remarks>
        /// <param name="mv">The mod vehicle to register.</param>
        /// <param name="verbose">Whether to enable verbose logging.</param>
        public static void RegisterVehicleLater(ModVehicle mv, bool verbose = false)
        {
            UWE.CoroutineHost.StartCoroutine(RegisterVehicle(mv, verbose));
        }

        /// <summary>
        /// Coroutine for registering a mod vehicle, including validation and queuing if necessary.
        /// </summary>
        /// <param name="mv">The mod vehicle to register.</param>
        /// <param name="verbose">Whether to enable verbose logging.</param>
        /// <returns>IEnumerator for coroutine execution.</returns>
        public static IEnumerator RegisterVehicle(ModVehicle mv, bool verbose = false)
        {
            mv.RequireComposition();
            if (VehicleManager.vehicleTypes.Where(x => x.name == mv.gameObject.name).Any())
            {
                VerboseLog(LogType.Warn, verbose, $"{mv.gameObject.name} was already registered.");
                yield break;
            }
            VerboseLog(LogType.Log, verbose, $"The {mv.gameObject.name} is beginning validation.");
            if (!ValidateAll(mv, verbose))
            {
                Logger.Error($"{mv.gameObject.name} failed validation. Not registered.");
                Logger.LoopMainMenuError($"Failed validation. Not registered. See log.", mv.gameObject.name);
                yield break;
            }
            yield return new WaitUntil(() => MainPatcher.Instance.GetVoices == null);
            yield return new WaitUntil(() => MainPatcher.Instance.GetEngineSounds == null);
            if (RegistrySemaphore)
            {
                VerboseLog(LogType.Log, verbose, $"Enqueueing the {mv.gameObject.name} for Registration.");
                RegistrationQueue.Enqueue(() => UWE.CoroutineHost.StartCoroutine(InternalRegisterVehicle(mv, verbose)));
                yield return new WaitUntil(() => VehicleManager.vehicleTypes.Select(x => x.mv).Contains(mv));
            }
            else
            {
                yield return UWE.CoroutineHost.StartCoroutine(InternalRegisterVehicle(mv, verbose));
            }
        }


        /// <summary>
        /// Internal coroutine for registering a mod vehicle, including prefab creation and queue management.
        /// </summary>
        /// <param name="mv">The mod vehicle to register.</param>
        /// <param name="verbose">Whether to enable verbose logging.</param>
        /// <returns>IEnumerator for coroutine execution.</returns>
        private static IEnumerator InternalRegisterVehicle(ModVehicle mv, bool verbose)
        {
            RegistrySemaphore = true;
            VerboseLog(LogType.Log, verbose, $"The {mv.gameObject.name} is beginning Registration.");
            PingType registeredPingType = VehicleManager.RegisterPingType((PingType)121, verbose);
            yield return UWE.CoroutineHost.StartCoroutine(VehicleBuilder.Prefabricate(mv, registeredPingType, verbose));
            RegistrySemaphore = false;
            Logger.Log($"Finished {mv.gameObject.name} registration.");
            VehiclesRegistered++;
            if (RegistrationQueue.Count > 0)
            {
                RegistrationQueue.Dequeue().Invoke();
            }
        }

        /// <summary>
        /// Validates a mod vehicle and its specific type (Submarine, Submersible, Skimmer).
        /// </summary>
        /// <param name="mv">The mod vehicle to validate.</param>
        /// <param name="verbose">Whether to enable verbose logging.</param>
        /// <returns>True if the vehicle is valid; otherwise, false.</returns>
        public static bool ValidateAll(ModVehicle mv, bool verbose)
        {
            if (mv is Submarine sub1)
            {
                if (!ValidateRegistration(sub1, verbose))
                {
                    Logger.Error("Invalid Submarine Registration for the " + mv.gameObject.name + ". Next.");
                    return false;
                }
            }
            if (mv is Submersible sub2)
            {
                if (!ValidateRegistration(sub2, verbose))
                {
                    Logger.Error("Invalid Submersible Registration for the " + mv.gameObject.name + ". Next.");
                    return false;
                }
            }
            if (mv is Skimmer sk)
            {
                if (!ValidateRegistration(sk, verbose))
                {
                    Logger.Error("Invalid Submersible Registration for the " + mv.gameObject.name + ". Next.");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Validates the registration of a mod vehicle, checking required fields and configuration.
        /// </summary>
        /// <param name="mv">The mod vehicle to validate.</param>
        /// <param name="verbose">Whether to enable verbose logging.</param>
        /// <returns>True if the vehicle is valid; otherwise, false.</returns>
        public static bool ValidateRegistration(ModVehicle mv, bool verbose)
        {
            string thisName = "";
            try
            {
                if (mv is null)
                {
                    Logger.Error("An null mod vehicle was passed for registration.");
                    return false;
                }
                if (mv.name == "")
                {
                    Logger.Error(thisName + " An empty name was provided for this vehicle.");
                    return false;
                }
                VerboseLog(LogType.Log, verbose, "Validating the Registration of the " + mv.name);
                thisName = mv.name + ": ";
                if (!mv.VehicleRoot)
                {
                    Logger.Error(thisName + " A null ModVehicle.VehicleModel was passed for registration.");
                    return false;
                }
                if (mv.Config.BaseCrushDepth < 0)
                {
                    Logger.Error(thisName + " A negative crush depth was passed for registration. This vehicle would take crush damage even out of water.");
                    return false;
                }
                if (mv.Config.MaxHealth <= 0)
                {
                    Logger.Error(thisName + " A non-positive max health was passed for registration. This vehicle would be destroyed as soon as it awakens.");
                    return false;
                }
                if (mv.Config.Mass <= 0)
                {
                    Logger.Error(thisName + " A non-positive mass was passed for registration. Don't do that.");
                    return false;
                }
                if (mv.Com.InnateStorages.Count == 0)
                {
                    VerboseLog(LogType.Warn, verbose, thisName + " No ModVehicle.InnateStorages were provided. These are lockers the vehicle always has.");
                }
                if (mv.Com.ModularStorages.Count == 0)
                {
                    VerboseLog(LogType.Log, verbose, thisName + " No ModVehicle.ModularStorages were provided. These are lockers that can be unlocked with upgrades.");
                }
                if (mv.Com.Upgrades.Count == 0)
                {
                    Logger.Warn(thisName + " No ModVehicle.Upgrades were provided. These specify interfaces the player can click to insert and remove upgrades.");
                }
                if (mv.Com.Batteries.Count == 0)
                {
                    Logger.Warn(thisName + " No ModVehicle.Batteries were provided. These are necessary to power the engines. This vehicle will be always powered.");
                }
                if (mv.Com.BackupBatteries.Count == 0)
                {
                    VerboseLog(LogType.Log, verbose, thisName + " No ModVehicle.BackupBatteries were provided. This collection of batteries belong to the AI and will be used exclusively for life support, auto-leveling, and other AI tasks. The AI will use the main batteries instead.");
                }
                if (mv.Com.HeadLights.Count == 0)
                {
                    VerboseLog(LogType.Warn, verbose, thisName + " No ModVehicle.HeadLights were provided. These lights would be activated when the player right clicks while piloting.");
                }
                if (mv.Com.WaterClipProxies.Count == 0)
                {
                    VerboseLog(LogType.Warn, verbose, thisName + " No ModVehicle.WaterClipProxies were provided. These are necessary to keep the ocean surface out of the vehicle.");
                }
                if (mv.Com.CanopyWindows.Count == 0)
                {
                    VerboseLog(LogType.Warn, verbose, thisName + " No ModVehicle.CanopyWindows were provided. These must be specified to handle window transparencies.");
                }
                if (!mv.Com.BoundingBoxCollider)
                {
                    VerboseLog(LogType.Warn, verbose, thisName + " No BoundingBox BoxCollider was provided. If a BoundingBox GameObject was provided, it did not have a BoxCollider. Tether range is 10 meters. This vehicle will not be able to dock in the Moonpool. The build bots will assume this vehicle is 6m x 8m x 12m.");
                }
                if (!mv.Com.CollisionModel)
                {
                    VerboseLog(LogType.Warn, verbose, thisName + " A null ModVehicle.CollisionModel was provided. This is necessary for leviathans to grab the vehicle.");
                }
                foreach (VehicleParts.VehicleStorage vs in mv.Com.InnateStorages.Concat(mv.Com.ModularStorages))
                {
                    if (!vs.CheckValidity(thisName))
                    {
                        return false;
                    }
                }
                foreach (VehicleParts.VehicleUpgrades vu in mv.Com.Upgrades)
                {
                    if (!vu.CheckValidity(thisName, verbose))
                    {
                        return false;
                    }
                }
                foreach (VehicleParts.VehicleBattery vb in mv.Com.Batteries.Concat(mv.Com.BackupBatteries))
                {
                    if (!vb.CheckValidity(thisName, verbose))
                    {
                        return false;
                    }
                }
                foreach (VehicleParts.VehicleFloodLight vfl in mv.Com.HeadLights)
                {
                    if (!vfl.CheckValidity(thisName))
                    {
                        return false;
                    }
                }
                if (mv.Com.StorageRootObject == null)
                {
                    Logger.Error(thisName + " A null ModVehicle.StorageRootObject was provided. There would be no way to store things in this vehicle.");
                    return false;
                }
                if (mv.Com.ModulesRootObject == null)
                {
                    Logger.Error(thisName + " A null ModVehicle.ModulesRootObject was provided. There would be no way to upgrade this vehicle.");
                    return false;
                }
                if (mv.Com.StorageRootObject == mv.gameObject)
                {
                    Logger.Error(thisName + " The StorageRootObject was the same as the Vehicle itself. These must be uniquely identifiable objects!");
                    return false;
                }
                if (mv.Com.ModulesRootObject == mv.gameObject)
                {
                    Logger.Error(thisName + " The ModulesRootObject was the same as the Vehicle itself. These must be uniquely identifiable objects!");
                    return false;
                }
                if (!mv.Com.LeviathanGrabPoint)
                {
                    VerboseLog(LogType.Warn, verbose, thisName + " A null ModVehicle.LeviathanGrabPoint was provided. This is where leviathans attach to the vehicle. The root object will be used instead.");
                }
                if (!mv.Engine)
                {
                    VerboseLog(LogType.Warn, verbose, thisName + " A null ModVehicle.ModVehicleEngine was passed for registration. A default engine will be chosen.");
                }
                if (!mv.Config.Recipe.CheckValidity(mv.name))
                    return false;

            }
            catch (Exception e)
            {
                Logger.Error(thisName + " Exception Caught. Likely this ModVehicle is not implementing something it must. Check the abstract features of ModVehicle.\n\n" + e.ToString() + "\n\n");
                return false;
            }

            VerboseLog(LogType.Log, verbose, "The Registration of the " + mv.name + " as a ModVehicle has been Validated.");
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
            if (!ValidateRegistration(mv as ModVehicle, verbose))
            {
                return false;
            }
            string thisName = "";
            try
            {
                thisName = mv.name + ": ";
                if (mv.Com.PilotSeats.Count == 0)
                {
                    Logger.Error(thisName + " No ModVehicle.PilotSeats were provided. These specify what the player will click on to begin piloting the vehicle.");
                    return false;
                }
                if (mv.Com.Hatches.Count == 0)
                {
                    Logger.Error(thisName + " No ModVehicle.Hatches were provided. These specify how the player will enter and exit the vehicle.");
                    return false;
                }
                if (mv.Com.FloodLights.Count == 0)
                {
                    VerboseLog(LogType.Warn, verbose, thisName + " No ModVehicle.FloodLights were provided. These lights would be activated on the control panel.");
                }
                if (mv.Com.NavigationPortLights.Count == 0)
                {
                    VerboseLog(LogType.Log, verbose, thisName + " Some navigation lights were missing.");
                }
                if (mv.Com.NavigationStarboardLights.Count == 0)
                {
                    VerboseLog(LogType.Log, verbose, thisName + " Some navigation lights were missing.");
                }
                if (mv.Com.NavigationPositionLights.Count == 0)
                {
                    VerboseLog(LogType.Log, verbose, thisName + " Some navigation lights were missing.");
                }
                if (mv.Com.NavigationWhiteStrobeLights.Count == 0)
                {
                    VerboseLog(LogType.Log, verbose, thisName + " Some navigation lights were missing.");
                }
                if (mv.Com.NavigationRedStrobeLights.Count == 0)
                {
                    VerboseLog(LogType.Log, verbose, thisName + " Some navigation lights were missing.");
                }
                if (mv.Com.TetherSources.Count == 0)
                {
                    Logger.Error(thisName + " No ModVehicle.TetherSources were provided. These are necessary to keep the player 'grounded' within the vehicle.");
                    return false;
                }
                if (mv.Com.ColorPicker == null)
                {
                    VerboseLog(LogType.Log, verbose, thisName + " A null ModVehicle.ColorPicker was provided. You only need this if you implement the necessary painting functions.");
                }
                if (mv.Com.Fabricator == null)
                {
                    VerboseLog(LogType.Warn, verbose, thisName + " A null ModVehicle.Fabricator was provided. The Submarine will not come with a fabricator at construction-time.");
                }
                if (mv.Com.ControlPanel == null)
                {
                    VerboseLog(LogType.Warn, verbose, thisName + " A null ModVehicle.ControlPanel was provided. This is necessary to toggle floodlights.");
                }
                if (mv.Com.SteeringWheelLeftHandTarget == null)
                {
                    VerboseLog(LogType.Log, verbose, thisName + " A null ModVehicle.SteeringWheelLeftHandTarget was provided. This is what the player's left hand will 'grab' while you pilot.");
                }
                if (mv.Com.SteeringWheelRightHandTarget == null)
                {
                    VerboseLog(LogType.Log, verbose, thisName + " A null ModVehicle.SteeringWheelRightHandTarget was provided.  This is what the player's right hand will 'grab' while you pilot.");
                }
                foreach (VehicleParts.VehiclePilotSeat ps in mv.Com.PilotSeats)
                {
                    if (!ps.CheckValidity(thisName, verbose))
                        return false;
                }
                foreach (VehicleParts.VehicleHatchStruct vhs in mv.Com.Hatches)
                {
                    if (!vhs.CheckValidity(thisName, verbose))
                        return false;

                }
                foreach (VehicleParts.VehicleStorage vs in mv.Com.ModularStorages)
                {
                    if (!vs.CheckValidity(thisName))
                        return false;
                }
                foreach (VehicleParts.VehicleFloodLight vfl in mv.Com.FloodLights)
                {
                    if (!vfl.CheckValidity(thisName))
                        return false;

                }
            }
            catch (Exception e)
            {
                Logger.Error(thisName + " Exception Caught. Likely this Submarine is not implementing something it must. Check the abstract features of Submarine.\n\n" + e.ToString() + "\n\n");
                return false;
            }

            VerboseLog(LogType.Log, verbose, "The Registration of the " + mv.name + " as a Submarine has been Validated.");
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
            if (!ValidateRegistration(mv as ModVehicle, verbose))
            {
                return false;
            }
            string thisName = "";
            try
            {
                thisName = mv.name + ": ";
                if (mv.Com.Hatches.Count == 0)
                {
                    Logger.Error(thisName + " No ModVehicle.Hatches were provided. These specify how the player will enter and exit the vehicle.");
                    return false;
                }
                if (mv.Com.SteeringWheelLeftHandTarget == null)
                {
                    VerboseLog(LogType.Log, verbose, thisName + " A null ModVehicle.SteeringWheelLeftHandTarget was provided. This is what the player's left hand will 'grab' while you pilot.");
                }
                if (mv.Com.SteeringWheelRightHandTarget == null)
                {
                    VerboseLog(LogType.Log, verbose, thisName + " A null ModVehicle.SteeringWheelRightHandTarget was provided.  This is what the player's right hand will 'grab' while you pilot.");
                }
                if (!mv.Com.PilotSeat.CheckValidity(thisName, verbose))
                    return false;
                foreach (VehicleParts.VehicleHatchStruct vhs in mv.Com.Hatches)
                {
                    if (!vhs.CheckValidity(thisName, verbose))
                        return false;
                }
                foreach (VehicleParts.VehicleStorage vs in mv.Com.ModularStorages)
                {
                    if (!vs.CheckValidity(thisName))
                        return false;
                }
            }
            catch (Exception e)
            {
                Logger.Error(thisName + " Exception Caught. Likely this Submersible is not implementing something it must. Check the abstract features of Submersible.\n\n" + e.ToString() + "\n\n");
                return false;
            }

            VerboseLog(LogType.Log, verbose, "The Registration of the " + mv.name + " as a Submersible has been Validated.");
            return true;
        }
    }
}
