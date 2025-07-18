using AVS.Saving;
using AVS.VehicleParts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AVS.BaseVehicle
{
    public abstract partial class AvsVehicle
    {

        /// <summary>
        /// Allocates a new save data container for saving or loading
        /// </summary>
        /// <returns>Container</returns>
        public virtual VehicleSaveData AllocateSaveData()
            => new VehicleSaveData();
        /// <summary>
        /// Writes all simple key value data to the save data container.
        /// </summary>
        /// <param name="saveData">Container previously allocated via <see cref="AllocateSaveData"/> </param>
        public virtual void WriteSaveData(VehicleSaveData saveData)
        {
            saveData.IsControlling = IsPlayerControlling();
            saveData.EnteredThroughHatch = IsUnderCommand ? enteredThroughHatch : -1;
            saveData.VehicleName = subName.hullName.text;
            saveData.BaseColor = SavedColor.From(baseColor);
            saveData.InteriorColor = SavedColor.From(interiorColor);
            saveData.StripeColor = SavedColor.From(stripeColor);
            saveData.NameColor = SavedColor.From(nameColor);
        }

        /// <summary>
        /// Gets the helm restored from the save data.
        /// If the derived vehicle does not have multiple helms,
        /// the default helm is returned.
        /// </summary>
        /// <returns></returns>
        protected abstract Helm GetLoadedHelm();

        /// <summary>
        /// Loads data previously saved via <see cref="WriteSaveData(VehicleSaveData)"/>.
        /// </summary>
        /// <param name="saveData">Data to load</param>
        public virtual void LoadData(VehicleSaveData? saveData)
        {
            if (saveData == null)
                return;
            if (saveData.EnteredThroughHatch >= 0)
            {
                PlayerEntry(
                    Com.Hatches[
                        Math.Min(saveData.EnteredThroughHatch, Com.Hatches.Count - 1)
                        ]);
            }
            if (saveData.IsControlling)
            {
                BeginHelmControl(GetLoadedHelm());
            }
            SetName(saveData.VehicleName);
            saveData.BaseColor?.WriteTo(ref baseColor);
            saveData.NameColor?.WriteTo(ref nameColor);
            saveData.StripeColor?.WriteTo(ref stripeColor);
            saveData.InteriorColor?.WriteTo(ref interiorColor);
            subName.SetColor(0, baseColor.HSB, baseColor.RGB);
            subName.SetColor(1, nameColor.HSB, nameColor.RGB);
            subName.SetColor(2, interiorColor.HSB, interiorColor.RGB);
            subName.SetColor(3, stripeColor.HSB, stripeColor.RGB);

        }

        private const string SimpleDataSaveFileName = "SimpleData";
        private void SaveSimpleData()
        {
            var c = AllocateSaveData();
            WriteSaveData(c);
            //Dictionary<string, string> simpleData = new Dictionary<string, string>
            //{
            //    { isControlling, IsPlayerControlling() ? bool.TrueString : bool.FalseString },
            //    { isInside, IsUnderCommand ? bool.TrueString : bool.FalseString },
            //    { mySubName, subName.hullName.text },
            //    { baseColorName, $"#{ColorUtility.ToHtmlStringRGB(baseColor.RGB)}" },
            //    { interiorColorName, $"#{ColorUtility.ToHtmlStringRGB(interiorColor.RGB)}" },
            //    { stripeColorName, $"#{ColorUtility.ToHtmlStringRGB(stripeColor.RGB)}" },
            //    { nameColorName, $"#{ColorUtility.ToHtmlStringRGB(nameColor.RGB)}" },
            //    { defaultColorName, (this is Submarine sub) && sub.IsDefaultTexture ? bool.TrueString : bool.FalseString }
            //};
            SaveLoad.JsonInterface.Write(this, SimpleDataSaveFileName, c);
        }
        private IEnumerator LoadSimpleData()
        {
            // Need to handle some things specially here for Submarines
            // Because Submarines had color changing before I knew how to integrate with the Moonpool
            // The new color changing methods are much simpler, but Odyssey and Beluga use the old methods,
            // So I'll still support them.
            yield return new WaitUntil(() => Admin.GameStateWatcher.IsWorldLoaded);
            yield return new WaitUntil(() => isInitialized);
            var c = AllocateSaveData();

            var simpleData = SaveLoad.JsonInterface.Read(c.GetType(), this, SimpleDataSaveFileName) as VehicleSaveData;
            LoadData(simpleData);

        }
        void IProtoTreeEventListener.OnProtoSerializeObjectTree(ProtobufSerializer serializer)
        {
            try
            {
                SaveSimpleData();
                SaveLoad.VFModularStorageSaveLoad.SerializeAllModularStorage(this);
            }
            catch (Exception e)
            {
                Logger.LogException($"Failed to save simple data for ModVehicle {name}", e);
            }
            OnGameSaved();
        }
        void IProtoTreeEventListener.OnProtoDeserializeObjectTree(ProtobufSerializer serializer)
        {
            UWE.CoroutineHost.StartCoroutine(LoadSimpleData());
            UWE.CoroutineHost.StartCoroutine(SaveLoad.VFModularStorageSaveLoad.DeserializeAllModularStorage(this));
            OnGameLoaded();
        }
        /// <summary>
        /// Executed when the local vehicle has finished saving.
        /// </summary>
        protected virtual void OnGameSaved() { }
        /// <summary>
        /// Executed when the local vehicle has finished loading.
        /// </summary>
        protected virtual void OnGameLoaded() { }

        private const string StorageSaveName = "Storage";
        private Dictionary<string, List<Tuple<TechType, float, TechType>>>? loadedStorageData = null;
        private readonly Dictionary<string, List<Tuple<TechType, float, TechType>>> innateStorageSaveData = new Dictionary<string, List<Tuple<TechType, float, TechType>>>();
        internal void SaveInnateStorage(string path, List<Tuple<TechType, float, TechType>> storageData)
        {
            innateStorageSaveData.Add(path, storageData);
            if (innateStorageSaveData.Count() == Com.InnateStorages.Count())
            {
                // write it out
                SaveLoad.JsonInterface.Write(this, StorageSaveName, innateStorageSaveData);
                innateStorageSaveData.Clear();
            }
        }
        internal List<Tuple<TechType, float, TechType>>? ReadInnateStorage(string path)
        {
            if (loadedStorageData == null)
            {
                loadedStorageData = SaveLoad.JsonInterface.Read<Dictionary<string, List<Tuple<TechType, float, TechType>>>>(this, StorageSaveName);
            }
            if (loadedStorageData == null)
            {
                return default;
            }
            if (loadedStorageData.ContainsKey(path))
            {
                return loadedStorageData[path];
            }
            else
            {
                return default;
            }
        }

        private const string BatterySaveName = "Batteries";
        private Dictionary<string, Tuple<TechType, float>>? loadedBatteryData = null;
        private readonly Dictionary<string, Tuple<TechType, float>> batterySaveData = new Dictionary<string, Tuple<TechType, float>>();
        internal void SaveBatteryData(string path, Tuple<TechType, float> batteryData)
        {
            int batteryCount = 0;
            batteryCount += Com.Batteries.Count;
            batteryCount += Com.BackupBatteries.Count;

            batterySaveData.Add(path, batteryData);
            if (batterySaveData.Count() == batteryCount)
            {
                // write it out
                SaveLoad.JsonInterface.Write(this, BatterySaveName, batterySaveData);
                batterySaveData.Clear();
            }
        }
        internal Tuple<TechType, float>? ReadBatteryData(string path)
        {
            if (loadedBatteryData == null)
            {
                loadedBatteryData = SaveLoad.JsonInterface.Read<Dictionary<string, Tuple<TechType, float>>>(this, BatterySaveName);
            }
            if (loadedBatteryData == null)
            {
                return default;
            }
            if (loadedBatteryData.ContainsKey(path))
            {
                return loadedBatteryData[path];
            }
            else
            {
                return default;
            }
        }


        /// <summary>
        /// Executed when loading has finished in <see cref="VehicleManager"/>.
        /// </summary>
        public virtual void OnFinishedLoading()
        { }

    }
}
