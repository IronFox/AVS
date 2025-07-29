using AVS.SaveLoad;
using AVS.Util;
using AVS.VehicleParts;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AVS.BaseVehicle
{
    public abstract partial class AvsVehicle
    {
        private Data? data = null;

        /// <summary>
        /// Creates the save data container for this vehicle.
        /// </summary>
        /// <remarks>
        /// For consistency, the base method should be called LAST
        /// by any inherited class
        /// </remarks>
        /// <param name="addBlock">Action to register new blocks with</param>
        protected virtual void CreateDataBlocks(Action<DataBlock> addBlock)
        {
            addBlock(new DataBlock($"AvsVehicle", MakeLocalProperties()));
        }

        private Data GetOrCreateData()
        {
            if (data != null)
            {
                return data;
            }
            List<DataBlock> blocks = new List<DataBlock>();
            CreateDataBlocks(blocks.Add);
            data = new Data(
                "AvsVehicle",
                blocks.ToArray()
                );
            return data;
        }


        /// <summary>
        /// The construction date of this vehicle.
        /// </summary>
        public DateTimeOffset Constructed { get; private set; } = DateTimeOffset.Now;


        IEnumerable<IPersistable> MakeLocalProperties()
        {
            yield return Persistable.Property(
                "VehicleName",
                () => VehicleName,
                (value) => SetName(value)
                );
            yield return Persistable.Property(
                "BoardedAt",
                () => IsBoarded ? (Vector3?)Player.mainObject.transform.position : null,
                (value) =>
                {
                    lateBoardAt = value;
                }
                );
            yield return Persistable.Property(
                "IsControlling",
                () => IsPlayerControlling(),
                (value) =>
                {
                    lateControl = value;
                }
                );
            yield return Persistable.Property(
                "BaseColor",
                () => SavedColor.From(baseColor),
                (value) =>
                {
                    value.WriteTo(ref baseColor);
                    subName.SetColor(0, baseColor.HSB, baseColor.RGB);
                }
                );
            yield return Persistable.Property(
                "InteriorColor",
                () => SavedColor.From(interiorColor),
                (value) =>
                {
                    value.WriteTo(ref interiorColor);
                    subName.SetColor(2, interiorColor.HSB, interiorColor.RGB);
                }
                );
            yield return Persistable.Property(
                "StripeColor",
                () => SavedColor.From(stripeColor),
                (value) =>
                {
                    value.WriteTo(ref stripeColor);
                    subName.SetColor(3, stripeColor.HSB, stripeColor.RGB);
                }
                );
            yield return Persistable.Property(
                "NameColor",
                () => SavedColor.From(nameColor),
                (value) =>
                {
                    value.WriteTo(ref nameColor);
                    subName.SetColor(1, nameColor.HSB, nameColor.RGB);
                }
                );
            yield return Persistable.Property(
                "Constructed",
                () => Constructed,
                (value) =>
                {
                    Constructed = value;
                }
                );

        }


        /// <summary>
        /// Gets the helm restored from the save data.
        /// If the derived vehicle does not have multiple helms,
        /// the default helm is returned.
        /// </summary>
        /// <returns></returns>
        protected abstract Helm GetLoadedHelm();

        private bool lateControl = false;
        private Vector3? lateBoardAt = null;

        /// <summary>
        /// Executed when the data for this vehicle has been loaded.
        /// </summary>
        protected virtual void OnDataLoaded()
        { }

        private const string BasicSaveFileNamePrefix = "Basic";

        /// <summary>
        /// Fetches the prefab identifier of this vehicle.
        /// </summary>
        public PrefabIdentifier? PrefabID => GetComponent<PrefabIdentifier>();

        private void SaveSimpleData()
        {
            PrefabID.WriteData(BasicSaveFileNamePrefix, GetOrCreateData(), this.Log);
        }

        void IProtoTreeEventListener.OnProtoSerializeObjectTree(ProtobufSerializer serializer)
        {
            try
            {
                SaveSimpleData();
                SaveLoad.AvsModularStorageSaveLoad.SerializeAllModularStorage(this);
            }
            catch (Exception e)
            {
                Logger.LogException($"Failed to save simple data for {nameof(AvsVehicle)} {name}", e);
            }
            OnGameSaved();
        }
        void IProtoTreeEventListener.OnProtoDeserializeObjectTree(ProtobufSerializer serializer)
        {
            Log.Write($"OnProtoDeserializeObjectTree {name} {GetType().Name}");
            if (PrefabID.ReadData(BasicSaveFileNamePrefix, GetOrCreateData(), Log))
                OnDataLoaded();

            UWE.CoroutineHost.StartCoroutine(SaveLoad.AvsModularStorageSaveLoad.DeserializeAllModularStorage(this));
        }

        /// <summary>
        /// Executed when the local vehicle has finished saving.
        /// </summary>
        protected virtual void OnGameSaved() { }

        private const string StorageSaveName = "Storage";
        private Dictionary<string, List<Tuple<TechType, float, TechType>>>? loadedStorageData = null;
        private readonly Dictionary<string, List<Tuple<TechType, float, TechType>>> innateStorageSaveData = new Dictionary<string, List<Tuple<TechType, float, TechType>>>();
        internal void SaveInnateStorage(string path, List<Tuple<TechType, float, TechType>> storageData)
        {
            innateStorageSaveData.Add(path, storageData);
            if (innateStorageSaveData.Count() == Com.InnateStorages.Count())
            {
                // write it out
                PrefabID.WriteReflected(StorageSaveName, innateStorageSaveData, Log);
                innateStorageSaveData.Clear();
            }
        }
        internal List<Tuple<TechType, float, TechType>>? ReadInnateStorage(string path)
        {
            if (loadedStorageData == null)
            {
                PrefabID.ReadReflected(
                    StorageSaveName,
                    out loadedStorageData,
                    Log);
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
                PrefabID.WriteReflected(BatterySaveName, batterySaveData, Log);
                batterySaveData.Clear();
            }
        }
        internal Tuple<TechType, float>? ReadBatteryData(string path)
        {
            if (loadedBatteryData == null)
            {
                PrefabID.ReadReflected(BatterySaveName, out loadedBatteryData, Log);
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
        /// Executed last when everything has been loaded successfully and the
        /// scene was completely initialized.
        /// Everything loaded by the savegame now exists at its final location and state.
        /// </summary>
        public virtual void OnFinishedLoading()
        {
            if (lateBoardAt != null)
            {
                RegisterPlayerEntry(() =>
                {
                    Player.mainObject.transform.position = lateBoardAt.Value;

                });
            }

            if (lateControl)
                BeginHelmControl(GetLoadedHelm());




        }

    }
}
