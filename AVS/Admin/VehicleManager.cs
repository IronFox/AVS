using AVS.SaveLoad;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AVS
{
    public static class VehicleManager
    {
        public static readonly List<ModVehicle> VehiclesInPlay = new List<ModVehicle>();
        public static readonly List<PingInstance> mvPings = new List<PingInstance>();
        public static readonly List<VehicleEntry> vehicleTypes = new List<VehicleEntry>();
        public static PingType RegisterPingType(PingType pt)
        {
            return RegisterPingType(pt, false);
        }
        public static PingType RegisterPingType(PingType pt, bool verbose)
        {
            PingType ret = pt;
            if ((int)ret < 121)
            {
                VehicleRegistrar.VerboseLog(VehicleRegistrar.LogType.Log, verbose, "PingType " + pt.ToString() + " was too small. Trying 121.");
                ret = (PingType)121;
            }
            while (mvPings.Where(x => x.pingType == ret).Count() > 0)
            {
                VehicleRegistrar.VerboseLog(VehicleRegistrar.LogType.Log, verbose, "PingType " + ret.ToString() + " was taken.");
                ret++;
            }
            VehicleRegistrar.VerboseLog(VehicleRegistrar.LogType.Log, verbose, "Registering PingType " + ret.ToString() + ".");
            return ret;
        }
        public static void EnrollVehicle(ModVehicle mv)
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
        public static void DeregisterVehicle(ModVehicle mv)
        {
            VehiclesInPlay.Remove(mv);
        }
        internal static void CreateSaveFileData(object sender, Nautilus.Json.JsonFileEventArgs e)
        {
            // See SaveData.cs
            var data = e.Instance as SaveData;
            if (data == null)
            {
                Logger.Error($"SaveData instance is null in CreateSaveFileData. Cannot save {Patches.SaveLoadManagerPatcher.SaveFileSpritesFileName}");
                return;
            }
            data.UpgradeLists = SaveManager.SerializeUpgrades();
            data.InnateStorages = SaveManager.SerializeInnateStorage();
            data.ModularStorages = SaveManager.SerializeModularStorage();
            data.Batteries = SaveManager.SerializeBatteries();
            data.BackupBatteries = SaveManager.SerializeBackupBatteries();
            data.IsPlayerInside = SaveManager.SerializePlayerInside();
            data.AllVehiclesAesthetics = SaveManager.SerializeAesthetics();
            data.IsPlayerControlling = SaveManager.SerializePlayerControlling();
            data.SubNames = SaveManager.SerializeSubName();
            JsonInterface.Write(
                Patches.SaveLoadManagerPatcher.SaveFileSpritesFileName,
                vehicleTypes
                    .Select(x => x.techType)
                    .Where(GameInfoIcon.Has)
                    .Select(x => x.AsString())
                    .ToList()
                    );
        }
        private static IEnumerator LoadVehicle(ModVehicle mv)
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
    }
}
