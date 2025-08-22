using AVS.SaveLoad;
using AVS.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using AVS.VehicleBuilding;
using UnityEngine;

namespace AVS.BaseVehicle;

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
            return data;
        var blocks = new List<DataBlock>();
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


    private IEnumerable<IPersistable> MakeLocalProperties()
    {
        yield return Persistable.Property(
            "VehicleName",
            () => VehicleName,
            (value) => SetName(value)
        );
        yield return Persistable.Property(
            "BoardedAt",
            () => IsBoarded ? (Vector3?)Player.mainObject.transform.position : null,
            (value) => { lateBoardAt = value; }
        );
        yield return Persistable.Property(
            "IsControlling",
            () => IsPlayerControlling(),
            (value) => { lateControl = value; }
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
            (value) => { Constructed = value; }
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
    {
    }

    private const string BasicSaveFileNamePrefix = "Basic";

    /// <summary>
    /// Fetches the prefab identifier of this vehicle.
    /// </summary>
    public PrefabIdentifier? PrefabID => GetComponent<PrefabIdentifier>();

    /// <summary>
    /// True if this vehicle has completed loading
    /// </summary>
    public bool VehicleIsReady { get; private set; }

    private void SaveSimpleData()
    {
        PrefabID.WriteData(BasicSaveFileNamePrefix, GetOrCreateData(), Log);
    }

    void IProtoTreeEventListener.OnProtoSerializeObjectTree(ProtobufSerializer serializer)
    {
        try
        {
            SaveSimpleData();
            AvsModularStorageSaveLoad.SerializeAllModularStorage(this);
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

        StartCoroutine(AvsModularStorageSaveLoad.DeserializeAllModularStorage(this));
    }

    /// <summary>
    /// Executed when the local vehicle has finished saving.
    /// </summary>
    protected virtual void OnGameSaved()
    {
    }

    private const string StorageSaveName = "Storage";
    private Dictionary<string, List<Tuple<TechType, float, TechType>>>? loadedStorageData = null;
    private readonly Dictionary<string, List<Tuple<TechType, float, TechType>>> innateStorageSaveData = new();

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
            PrefabID.ReadReflected(
                StorageSaveName,
                out loadedStorageData,
                Log);
        if (loadedStorageData == null)
            return default;
        if (loadedStorageData.ContainsKey(path))
            return loadedStorageData[path];
        else
            return default;
    }

    private record BatterySaveData(TechType techType, float amount);

    private const string BatterySaveName = "Batteries";


    private Dictionary<string, Tuple<TechType, float, string?>>? loadedBatteryData = null;

    private readonly Dictionary<string, Tuple<TechType, float, string>> batterySaveData =
        new();

    internal void SaveBatteryData(string path, Tuple<TechType, float> batteryData)
    {
        var batteryCount = 0;
        batteryCount += Com.Batteries.Count;
        batteryCount += Com.BackupBatteries.Count;

        batterySaveData.Add(path, Tuple.Create(batteryData.Item1, batteryData.Item2, batteryData.Item1.AsString()));
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
            PrefabID.ReadReflected(BatterySaveName, out loadedBatteryData, Log);
        var log = Log.Tag(nameof(ReadBatteryData));
        if (loadedBatteryData == null)
        {
            log.Error(
                $"Failed to load battery data for {path}: Unable to deserialize loaded battery data from save file");
            return default;
        }

        if (loadedBatteryData.TryGetValue(path, out var rs))
        {
            if (!string.IsNullOrEmpty(rs.Item3))
            {
                if (TechTypeExtensions.FromString(rs.Item3, out var tt, true))
                {
                    log.Debug($"Decoded tech type {tt} from '{rs.Item3}' for '{path}'");

                    return Tuple.Create(tt, rs.Item2);
                }
                else
                {
                    log.Error($"Failed to decode given tech type string '{rs.Item3}' for '{path}'");
                    return default;
                }
            }

            log.Warn(
                $"Save data did not provide a parsable tech type string for {rs.Item1} for '{path}'. Using numeric value instead.");
            return Tuple.Create(rs.Item1, rs.Item2);
        }

        log.Error(
            $"Vehicle is requesting battery path '{path}' but this path is not present in the loaded battery data.");
        return default;
    }


    /// <summary>
    /// Executed last when everything has been loaded successfully and the
    /// scene was completely initialized.
    /// Everything loaded by the savegame now exists at its final location and state.
    /// </summary>
    public virtual void OnFinishedLoading()
    {
        ReSetupWaterParks();
        foreach (var wp in Com.WaterParks)
        {
            var waterPark = wp.Container.GetComponent<StorageComponents.MobileWaterPark>();
            if (waterPark == null)
            {
                Log.Error($"WaterPark {wp.Container.name} has no MobileWaterPark component!");
                continue;
            }

            waterPark.OnVehicleLoaded();
        }

        if (lateBoardAt != null)
            RegisterPlayerEntry(() => { Player.mainObject.transform.position = lateBoardAt.Value; });

        if (lateControl)
            BeginHelmControl(GetLoadedHelm());

        VehicleIsReady = true;
    }
}