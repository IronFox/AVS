using AVS.BaseVehicle;
using AVS.Log;
using AVS.SaveLoad;
using Nautilus.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AVS
{
    /// <summary>
    /// Provides management functions for AVS vehicles, including registration, enrollment, and loading.
    /// </summary>
    public static class AvsVehicleManager
    {
        /// <summary>
        /// List of all AVS vehicles currently in play.
        /// </summary>
        public static List<AvsVehicle> VehiclesInPlay { get; } = new List<AvsVehicle>();

        /// <summary>
        /// List of all registered ping instances for vehicles.
        /// </summary>
        public static List<PingInstance> PingInstances { get; } = new List<PingInstance>();

        /// <summary>
        /// List of all registered vehicle types.
        /// </summary>
        public static List<VehicleEntry> VehicleTypes { get; } = new List<VehicleEntry>();

        /// <summary>
        /// Registers a new <see cref="PingType"/> for a vehicle, ensuring it is unique and above the minimum value.
        /// </summary>
        /// <param name="pt">The initial ping type to register.</param>
        /// <returns>The registered, unique ping type.</returns>
        public static PingType RegisterPingType(PingType pt)
        {
            return RegisterPingType(pt, false);
        }

        /// <summary>
        /// Registers a new <see cref="PingType"/> for a vehicle, ensuring it is unique and above the minimum value.
        /// Optionally logs the registration process.
        /// </summary>
        /// <param name="pt">The initial ping type to register.</param>
        /// <param name="verbose">If true, logs detailed registration steps.</param>
        /// <returns>The registered, unique ping type.</returns>
        public static PingType RegisterPingType(PingType pt, bool verbose)
        {
            PingType ret = pt;
            if ((int)ret < 121)
            {
                VehicleRegistrar.VerboseLog(VehicleRegistrar.LogType.Log, verbose, "PingType " + pt.ToString() + " was too small. Trying 121.");
                ret = (PingType)121;
            }
            while (PingInstances.Where(x => x.pingType == ret).Count() > 0)
            {
                VehicleRegistrar.VerboseLog(VehicleRegistrar.LogType.Log, verbose, "PingType " + ret.ToString() + " was taken.");
                ret++;
            }
            VehicleRegistrar.VerboseLog(VehicleRegistrar.LogType.Log, verbose, "Registering PingType " + ret.ToString() + ".");
            return ret;
        }

        /// <summary>
        /// Enrolls a vehicle into the <see cref="VehiclesInPlay"/> list and starts loading it if constructed.
        /// </summary>
        /// <param name="mv">The vehicle to enroll.</param>
        public static void EnrollVehicle(AvsVehicle mv)
        {
            if (mv.name.Contains("Clone") && !VehiclesInPlay.Contains(mv))
            {
                VehiclesInPlay.Add(mv);
                Logger.Log("Enrolled the " + mv.name + " : " + mv.GetName() + " : " + mv.subName);
                if (!mv.GetComponent<VFXConstructing>() || mv.GetComponent<VFXConstructing>().constructed > 3f)
                {
                    UWE.CoroutineHost.StartCoroutine(LoadVehicle(mv)); // I wish I knew a good way to optionally NOT do this if this sub is being constructed rn
                }
            }
        }

        /// <summary>
        /// Removes a vehicle from the <see cref="VehiclesInPlay"/> list.
        /// </summary>
        /// <param name="mv">The vehicle to deregister.</param>
        public static void DeregisterVehicle(AvsVehicle mv)
        {
            VehiclesInPlay.Remove(mv);
        }

        /// <summary>
        /// Coroutine that waits for the world to be ready, then calls <see cref="AvsVehicle.OnFinishedLoading"/> on the vehicle.
        /// </summary>
        /// <param name="mv">The vehicle to load.</param>
        /// <returns>Coroutine enumerator.</returns>
        private static IEnumerator LoadVehicle(AvsVehicle mv)
        {
            // See SaveData.cs
            yield return new WaitUntil(() => LargeWorldStreamer.main != null);
            yield return new WaitUntil(() => LargeWorldStreamer.main.IsReady());
            yield return new WaitUntil(() => LargeWorldStreamer.main.IsWorldSettled());
            yield return new WaitUntil(() => !WaitScreen.IsWaiting);
            Logger.Log($"Loading: {mv.GetName()}");
            //if (mv.liveMixin.health == 0)
            //{
            //    mv.OnKill();
            //}
            mv.OnFinishedLoading();
        }

        internal static void CreateSpritesFile(object sender, JsonFileEventArgs e)
        {
            SaveFiles.Current.WriteReflected(
                Patches.SaveLoadManagerPatcher.SaveFileSpritesFileName,
                AvsVehicleManager.VehicleTypes.Select(x => x.techType).Where(x => GameInfoIcon.Has(x)).Select(x => x.AsString()).ToList(),
                LogWriter.Default
            );
        }
    }
}
