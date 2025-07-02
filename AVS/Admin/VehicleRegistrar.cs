using AVS.VehicleTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AVS
{
    public static class VehicleRegistrar
    {
        public static int VehiclesRegistered = 0;
        public static int VehiclesPrefabricated = 0;
        private static Queue<Action> RegistrationQueue = new Queue<Action>();
        private static bool RegistrySemaphore = false;
        public enum LogType
        {
            Log,
            Warn
        }
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
        public static void RegisterVehicleLater(ModVehicle mv, bool verbose = false)
        {
            UWE.CoroutineHost.StartCoroutine(RegisterVehicle(mv, verbose));
        }
        public static IEnumerator RegisterVehicle(ModVehicle mv, bool verbose = false)
        {
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
        public static IEnumerator RegisterVehicle(ModVehicle mv)
        {
            yield return RegisterVehicle(mv, false);
        }
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
        public static bool ValidateAll(ModVehicle mv, bool verbose)
        {
            if (mv as Submarine != null)
            {
                if (!ValidateRegistration(mv as Submarine, verbose))
                {
                    Logger.Error("Invalid Submarine Registration for the " + mv.gameObject.name + ". Next.");
                    return false;
                }
            }
            if (mv as Submersible != null)
            {
                if (!ValidateRegistration(mv as Submersible, verbose))
                {
                    Logger.Error("Invalid Submersible Registration for the " + mv.gameObject.name + ". Next.");
                    return false;
                }
            }
            if (mv as Walker != null)
            {
                if (!ValidateRegistration(mv as Walker, verbose))
                {
                    Logger.Error("Invalid Submersible Registration for the " + mv.gameObject.name + ". Next.");
                    return false;
                }
            }
            if (mv as Skimmer != null)
            {
                if (!ValidateRegistration(mv as Skimmer, verbose))
                {
                    Logger.Error("Invalid Submersible Registration for the " + mv.gameObject.name + ". Next.");
                    return false;
                }
            }
            return true;
        }
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
                if (mv.Config.VehicleModel == null)
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
                if (mv.Config.InnateStorages.Count == 0)
                {
                    VerboseLog(LogType.Warn, verbose, thisName + " No ModVehicle.InnateStorages were provided. These are lockers the vehicle always has.");
                }
                if (mv.Config.ModularStorages.Count == 0)
                {
                    VerboseLog(LogType.Log, verbose, thisName + " No ModVehicle.ModularStorages were provided. These are lockers that can be unlocked with upgrades.");
                }
                if (mv.Config.Upgrades.Count == 0)
                {
                    Logger.Warn(thisName + " No ModVehicle.Upgrades were provided. These specify interfaces the player can click to insert and remove upgrades.");
                }
                if (mv.Config.Batteries.Count == 0)
                {
                    Logger.Warn(thisName + " No ModVehicle.Batteries were provided. These are necessary to power the engines. This vehicle will be always powered.");
                }
                if (mv.Config.BackupBatteries.Count == 0)
                {
                    VerboseLog(LogType.Log, verbose, thisName + " No ModVehicle.BackupBatteries were provided. This collection of batteries belong to the AI and will be used exclusively for life support, auto-leveling, and other AI tasks. The AI will use the main batteries instead.");
                }
                if (mv.Config.HeadLights.Count == 0)
                {
                    VerboseLog(LogType.Warn, verbose, thisName + " No ModVehicle.HeadLights were provided. These lights would be activated when the player right clicks while piloting.");
                }
                if (mv.Config.WaterClipProxies.Count == 0)
                {
                    VerboseLog(LogType.Warn, verbose, thisName + " No ModVehicle.WaterClipProxies were provided. These are necessary to keep the ocean surface out of the vehicle.");
                }
                if (mv.Config.CanopyWindows.Count == 0)
                {
                    VerboseLog(LogType.Warn, verbose, thisName + " No ModVehicle.CanopyWindows were provided. These must be specified to handle window transparencies.");
                }
                if (mv.Config.BoundingBoxCollider == null)
                {
                    VerboseLog(LogType.Warn, verbose, thisName + " No BoundingBox BoxCollider was provided. If a BoundingBox GameObject was provided, it did not have a BoxCollider. Tether range is 10 meters. This vehicle will not be able to dock in the Moonpool. The build bots will assume this vehicle is 6m x 8m x 12m.");
                }
                if (mv.Config.CollisionModel == null)
                {
                    VerboseLog(LogType.Warn, verbose, thisName + " A null ModVehicle.CollisionModel was provided. This is necessary for leviathans to grab the vehicle.");
                }
                foreach (VehicleParts.VehicleStorage vs in mv.Config.InnateStorages.Concat(mv.Config.ModularStorages))
                {
                    if (vs.Container == null)
                    {
                        Logger.Error(thisName + " A null VehicleStorage.Container was provided. There would be no way to access this storage.");
                        return false;
                    }
                    if (vs.Height < 0)
                    {
                        Logger.Error(thisName + " A negative VehicleStorage.Height was provided. This storage would have no space.");
                        return false;
                    }
                    if (vs.Width < 0)
                    {
                        Logger.Error(thisName + " A negative VehicleStorage.Width was provided. This storage would have no space.");
                        return false;
                    }
                }
                foreach (VehicleParts.VehicleUpgrades vu in mv.Config.Upgrades)
                {
                    if (vu.Interface == null)
                    {
                        Logger.Error(thisName + " A null VehicleUpgrades.Interface was provided. There would be no way to upgrade this vehicle.");
                        return false;
                    }
                    if (vu.Flap is null)
                    {
                        Logger.Error(thisName + " A null VehicleUpgrades.Flap was provided. The upgrades interface requires this. It will be rotated by the angles in this struct when activated. You can set the rotation angle to zero to take no action.");
                        return false;
                    }
                    if (vu.ModuleProxies is null)
                    {
                        VerboseLog(LogType.Log, verbose, thisName + " A null VehicleUpgrades.ModuleProxies was provided. AVS will not provide a model for this upgrade slot.");
                    }
                }
                foreach (VehicleParts.VehicleBattery vb in mv.Config.Batteries.Concat(mv.Config.BackupBatteries))
                {
                    if (vb.BatterySlot == null)
                    {
                        Logger.Error(thisName + " A null VehicleBattery.BatterySlot was provided. There would be no way to access this battery.");
                        return false;
                    }
                    if (vb.BatteryProxy == null)
                    {
                        VerboseLog(LogType.Log, verbose, thisName + " A null VehicleBattery.BatteryProxy was provided. AVS will not provide a model for this battery slot.");
                    }
                }
                foreach (VehicleParts.VehicleFloodLight vfl in mv.Config.HeadLights)
                {
                    if (vfl.Light == null)
                    {
                        Logger.Error(thisName + " A null VehicleFloodLight.Light was provided. There would be nothing from which to emit light.");
                        return false;
                    }
                    if (vfl.Intensity < 0)
                    {
                        Logger.Error(thisName + " A negative VehicleFloodLight.Intensity was provided. The light would be totally dark.");
                        return false;
                    }
                    if (vfl.Range < 0)
                    {
                        Logger.Error(thisName + " A negative VehicleFloodLight.Range was provided. The light would be totally dark.");
                        return false;
                    }
                }
                if (mv.Config.StorageRootObject == null)
                {
                    Logger.Error(thisName + " A null ModVehicle.StorageRootObject was provided. There would be no way to store things in this vehicle.");
                    return false;
                }
                if (mv.Config.ModulesRootObject == null)
                {
                    Logger.Error(thisName + " A null ModVehicle.ModulesRootObject was provided. There would be no way to upgrade this vehicle.");
                    return false;
                }
                if (mv.Config.StorageRootObject == mv.gameObject)
                {
                    Logger.Error(thisName + " The StorageRootObject was the same as the Vehicle itself. These must be uniquely identifiable objects!");
                    return false;
                }
                if (mv.Config.ModulesRootObject == mv.gameObject)
                {
                    Logger.Error(thisName + " The ModulesRootObject was the same as the Vehicle itself. These must be uniquely identifiable objects!");
                    return false;
                }
                if (mv.LeviathanGrabPoint == null)
                {
                    VerboseLog(LogType.Warn, verbose, thisName + " A null ModVehicle.LeviathanGrabPoint was provided. This is where leviathans attach to the vehicle. The root object will be used instead.");
                }
                if (mv.VFEngine == null && mv.Engine == null)
                {
                    VerboseLog(LogType.Warn, verbose, thisName + " A null ModVehicle.ModVehicleEngine was passed for registration. A default engine will be chosen.");
                }
                if (mv.Config.Recipe != null)
                {
                    bool badRecipeFlag = false;
                    foreach (var ingredient in mv.Config.Recipe)
                    {
                        try
                        {
                            string result = ingredient.Key.EncodeKey();
                        }
                        catch (System.Exception e)
                        {
                            Logger.LogException($"Vehicle Recipe Error: {mv.name}'s recipe had an invalid tech type: {ingredient.Key}. Probably you are referencing an unregistered/non-existent techtype!", e);
                            badRecipeFlag = true;
                        }
                    }
                    if (badRecipeFlag)
                    {
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(thisName + " Exception Caught. Likely this ModVehicle is not implementing something it must. Check the abstract features of ModVehicle.\n\n" + e.ToString() + "\n\n");
                return false;
            }

            VerboseLog(LogType.Log, verbose, "The Registration of the " + mv.name + " as a ModVehicle has been Validated.");
            return true;
        }
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
                if (mv.SubConfig.PilotSeats.Count == 0)
                {
                    Logger.Error(thisName + " No ModVehicle.PilotSeats were provided. These specify what the player will click on to begin piloting the vehicle.");
                    return false;
                }
                if (mv.SubConfig.Hatches.Count == 0)
                {
                    Logger.Error(thisName + " No ModVehicle.Hatches were provided. These specify how the player will enter and exit the vehicle.");
                    return false;
                }
                if (mv.SubConfig.FloodLights.Count == 0)
                {
                    VerboseLog(LogType.Warn, verbose, thisName + " No ModVehicle.FloodLights were provided. These lights would be activated on the control panel.");
                }
                if (mv.SubConfig.NavigationPortLights.Count == 0)
                {
                    VerboseLog(LogType.Log, verbose, thisName + " Some navigation lights were missing.");
                }
                if (mv.SubConfig.NavigationStarboardLights.Count == 0)
                {
                    VerboseLog(LogType.Log, verbose, thisName + " Some navigation lights were missing.");
                }
                if (mv.SubConfig.NavigationPositionLights.Count == 0)
                {
                    VerboseLog(LogType.Log, verbose, thisName + " Some navigation lights were missing.");
                }
                if (mv.SubConfig.NavigationWhiteStrobeLights.Count == 0)
                {
                    VerboseLog(LogType.Log, verbose, thisName + " Some navigation lights were missing.");
                }
                if (mv.SubConfig.NavigationRedStrobeLights.Count == 0)
                {
                    VerboseLog(LogType.Log, verbose, thisName + " Some navigation lights were missing.");
                }
                if (mv.SubConfig.TetherSources.Count == 0)
                {
                    Logger.Error(thisName + " No ModVehicle.TetherSources were provided. These are necessary to keep the player 'grounded' within the vehicle.");
                    return false;
                }
                if (mv.SubConfig.ColorPicker == null)
                {
                    VerboseLog(LogType.Log, verbose, thisName + " A null ModVehicle.ColorPicker was provided. You only need this if you implement the necessary painting functions.");
                }
                if (mv.SubConfig.Fabricator == null)
                {
                    VerboseLog(LogType.Warn, verbose, thisName + " A null ModVehicle.Fabricator was provided. The Submarine will not come with a fabricator at construction-time.");
                }
                if (mv.SubConfig.ControlPanel == null)
                {
                    VerboseLog(LogType.Warn, verbose, thisName + " A null ModVehicle.ControlPanel was provided. This is necessary to toggle floodlights.");
                }
                if (mv.SubConfig.SteeringWheelLeftHandTarget == null)
                {
                    VerboseLog(LogType.Log, verbose, thisName + " A null ModVehicle.SteeringWheelLeftHandTarget was provided. This is what the player's left hand will 'grab' while you pilot.");
                }
                if (mv.SubConfig.SteeringWheelRightHandTarget == null)
                {
                    VerboseLog(LogType.Log, verbose, thisName + " A null ModVehicle.SteeringWheelRightHandTarget was provided.  This is what the player's right hand will 'grab' while you pilot.");
                }
                foreach (VehicleParts.VehiclePilotSeat ps in mv.SubConfig.PilotSeats)
                {
                    if (!ps.CheckValidity(thisName, verbose))
                        return false;
                }
                foreach (VehicleParts.VehicleHatchStruct vhs in mv.SubConfig.Hatches)
                {
                    if (!vhs.CheckValidity(thisName, verbose))
                        return false;

                }
                foreach (VehicleParts.VehicleStorage vs in mv.SubConfig.ModularStorages)
                {
                    if (!vs.CheckValidity(thisName))
                        return false;
                }
                foreach (VehicleParts.VehicleFloodLight vfl in mv.SubConfig.FloodLights)
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
                if (mv.SubConfig.Hatches.Count == 0)
                {
                    Logger.Error(thisName + " No ModVehicle.Hatches were provided. These specify how the player will enter and exit the vehicle.");
                    return false;
                }
                if (mv.SubConfig.SteeringWheelLeftHandTarget == null)
                {
                    VerboseLog(LogType.Log, verbose, thisName + " A null ModVehicle.SteeringWheelLeftHandTarget was provided. This is what the player's left hand will 'grab' while you pilot.");
                }
                if (mv.SubConfig.SteeringWheelRightHandTarget == null)
                {
                    VerboseLog(LogType.Log, verbose, thisName + " A null ModVehicle.SteeringWheelRightHandTarget was provided.  This is what the player's right hand will 'grab' while you pilot.");
                }
                if (!mv.SubConfig.PilotSeat.CheckValidity(thisName, verbose))
                    return false;
                foreach (VehicleParts.VehicleHatchStruct vhs in mv.SubConfig.Hatches)
                {
                    if (!vhs.CheckValidity(thisName, verbose))
                        return false;
                }
                foreach (VehicleParts.VehicleStorage vs in mv.SubConfig.ModularStorages)
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
