using AVS.BaseVehicle;
using AVS.Localization;
using AVS.Log;
using AVS.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AVS.StorageComponents;

/// <summary>
/// Helper structure for fishtanks that work like an aquapark but can
/// be mobile.
/// </summary>
internal class MobileWaterPark : MonoBehaviour, ICraftTarget, IProtoTreeEventListener
{
    private ItemsContainer? _container = null;

    /// <summary>
    /// Accessor for the storage container.
    /// Throws an exception if called before Awake() or OnCraftEnd() were called.
    /// </summary>
    public ItemsContainer Container =>
        _container ?? throw new InvalidOperationException(
            "Trying to access InnateStorageContainer.Container before either Awake() or OnCraftEnd() were called");

    /// <summary>
    /// The display name of the storage container.
    /// Must be reapplied on vehicle awake or it will reset to the default value.
    /// </summary>
    public MaybeTranslate DisplayName { get; private set; } = default;

    [SerializeField]
    private int index;

    /// <summary>
    /// Storage container width.
    /// </summary>
    [SerializeField]
    private int width = 6;

    /// <summary>
    /// Storage container height.
    /// </summary>
    [SerializeField]
    private int height = 8;

    [SerializeField]
    private bool canHatchEggs = true;
    [SerializeField]
    private AvsVehicle? vehicle;
    [SerializeField]
    private Transform? waterPark;

    private AvsVehicle AV => vehicle.OrThrow(() => new InvalidOperationException($"Trying to access MobileWaterPark.av before it has been initialized"));



    public void Awake()
    {
        Init();
    }

    private Dictionary<int, float> LastFishTankWarning { get; } = new();

    private bool CanWarnAbout(Pickupable pickupable)
    {
        if (!LastFishTankWarning.TryGetValue(pickupable.GetInstanceID(), out var lastWarning) ||
            Time.time - lastWarning > 30f)
        {
            LastFishTankWarning[pickupable.GetInstanceID()] = Time.time;
            return true;
        }

        return false;
    }

    private bool IsLivingFishOrEgg(Pickupable pickupable, bool verbose)
    {
        if (pickupable.IsNull() || pickupable.gameObject.IsNull())
            return false;
        var creature = pickupable.GetComponent<WaterParkCreature>();
        var live = pickupable.GetComponent<LiveMixin>();
        var localizedName = Language.main.Get(pickupable.GetTechName());
        if (creature.IsNotNull())
        {
            if (live.IsNotNull() && live.IsAlive())
                return true;
            if (CanWarnAbout(pickupable))
                ErrorMessage.AddMessage(Translator.GetFormatted(
                    TranslationKey.Error_MobileWaterPark_CannotAdd_FishIsDead, localizedName, DisplayName.Rendered));
            return false;
        }

        var egg = pickupable.GetComponent<CreatureEgg>();
        if (egg.IsNotNull())
        {
            if (live.IsNotNull() && live.IsAlive())
                return true;
            //as it turns out, eggs die when hatched
            if (CanWarnAbout(pickupable))
                ErrorMessage.AddMessage(Translator.GetFormatted(
                    TranslationKey.Error_MobileWaterPark_CannotAdd_EggIsHatched, localizedName, DisplayName.Rendered));
            return false;
        }

        if (CanWarnAbout(pickupable))
            ErrorMessage.AddMessage(Translator.GetFormatted(TranslationKey.Error_MobileWaterPark_CannotAdd_Incompatible,
                localizedName, DisplayName.Rendered));
        return false;
    }


    private void Init()
    {
        if (_container.IsNotNull())
            return;
        using var log = SmartLog.ForAVS(AV.Owner);
        log.Write(
            $"Initializing {this.NiceName()} for {DisplayName.Rendered} ({DisplayName.Localize}) with width {width} and height {height}");
        _container = new ItemsContainer(width, height,
            waterPark, DisplayName.Rendered, null);
        _container.SetAllowedTechTypes(Array.Empty<TechType>());
        _container.isAllowedToAdd = IsLivingFishOrEgg;
        _container.isAllowedToRemove = IsNotHatchingEgg;
        _container.onAddItem += (item) =>
        {
            item.item.gameObject.SetActive(false); //keep it off until I can figure this out


            //if (item.item/* && item.item.transform.parent != waterPark*/)
            //{
            //    var prefabId = item.item.GetComponent<PrefabIdentifier>();
            //    if (prefabId.IsNull())
            //    {
            //        LogWriter.Default.Error($"Item {item.item.NiceName()} does not have a valid PrefabIdentifier, skipping.");
            //        return;
            //    }
            //    var live = item.item.GetComponent<LiveMixin>();
            //    var infect = item.item.GetComponent<InfectedMixin>();
            //    if (live.IsNull() || !live.IsAlive())
            //    {
            //        if (CanWarnAbout(item.item))
            //            ErrorMessage.AddMessage(Translator.GetFormatted(TranslationKey.Error_MobileWaterPark_CannotAdd_FishIsDead, item.item.GetTechName()));
            //        return;
            //    }
            //    item.item.gameObject.SetActive(true); //enable the item so it can be added to the water park
            //    var embed = item.item;
            //    embed.name = prefabId.Id;

            //    //var newLive = embed.GetComponent<LiveMixin>();
            //    //newLive.health = live.health; //copy health from the original item

            //    //var newInfect = embed.GetComponent<InfectedMixin>();
            //    //if (newInfect.IsNotNull() && infect.IsNotNull())
            //    //{
            //    //    newInfect.infectedAmount = infect.infectedAmount;
            //    //}

            //    LogWriter.Default.Debug($"Adding item {embed.NiceName()} from {embed.transform.parent.NiceName()} to water park {waterPark.NiceName()}");
            //    embed.transform.SetParent(waterPark, false);
            //    var creature = embed.GetComponent<WaterParkCreature>();
            //    if (creature.IsNotNull())
            //    {
            //        creature.currentWaterPark = new WaterPark();
            //        //creature.currentWaterPark.internalRadius = 5f;
            //        creature.OnAddToWP();
            //        if (embed.transform.parent != waterPark)
            //        {
            //            LogWriter.Default.Error($"Creature {embed.NiceName()} was not added to the water park instances, but to {embed.transform.parent.NiceName()}");
            //            embed.transform.SetParent(waterPark, false);
            //        }
            //        creature.transform.position = RandomLocation(false);
            //        creature.transform.localRotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
            //        LogWriter.Default.Debug($"Spawned creature {embed.NiceName()} @ {embed.transform.localPosition}");
            //    }
            //    else
            //    {
            //        var egg = embed.GetComponent<CreatureEgg>();
            //        if (egg.IsNotNull())
            //        {

            //            egg.transform.SetParent(waterPark, false);
            //            egg.transform.position = RandomLocation(true);
            //            if (canHatchEggs)
            //            {
            //                egg.OnAddToWaterPark();
            //                if (embed.transform.parent != waterPark)
            //                {
            //                    LogWriter.Default.Error($"Egg {embed.NiceName()} was not added to the water park instances, but to {embed.transform.parent.NiceName()}");
            //                    embed.transform.SetParent(waterPark, false);
            //                }

            //            }
            //            LogWriter.Default.Debug($"Spawned egg {embed.NiceName()} @ {embed.transform.localPosition}");
            //        }
            //    }
            //}
        };

        _container.onRemoveItem += (item) =>
        {
            item.item.gameObject.SetActive(false); //disable the item so it doesn't cause issues
        };
        log.Write($"Initialized");
    }

    private Vector3 RandomLocation(bool dropToFloor)
    {
        //return waterParkInstances!.position;
        var random = UnityEngine.Random.insideUnitSphere;
        var ray = random.normalized;
        //            LogWriter.Default.Debug($"Random ray for water park location: {random} => {ray}");
        var haveLocation = false;
        RaycastHit hit = default;
        for (var i = 0; i < 100; i++)
            if (haveLocation = Physics.Raycast(waterPark!.position, ray, out hit, 10f, Physics.AllLayers,
                    QueryTriggerInteraction.Ignore))
                break;
        //LogWriter.Default.Debug($"haveLocation: {haveLocation} => {ray}");

        var rs = haveLocation
            ? waterPark!.position + (hit.point - waterPark!.position) * random.magnitude * 0.9f
            : waterPark!.position + random;

        if (dropToFloor)
            if (Physics.Raycast(rs, Vector3.down, out hit, 20f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
                return hit.point + Vector3.up * 0.2f;

        return rs;
    }

    private bool IsNotHatchingEgg(Pickupable pickupable, bool verbose) =>
        //    if (pickupable.IsNull() || pickupable.gameObject.IsNull())
        //    {
        //        return false;
        //    }
        //    var egg = pickupable.GetComponent<CreatureEgg>();
        //    if (egg.IsNotNull())
        //    {
        //        if (canHatchEggs && egg.creaturePrefab.IsNotNull() && egg.creaturePrefab.RuntimeKeyIsValid())
        //        {
        //            if (CanWarnAbout(pickupable))
        //                ErrorMessage.AddMessage(Translator.GetFormatted(TranslationKey.Error_MobileWaterPark_CannotRemove_HatchingEgg, pickupable.GetTechName())); afskdldf add name and localize tech name
        //            return false;
        //        }
        //        return true;
        //    }
        true; //not an egg, so we can remove it

    public void OnCraftEnd(TechType techType)
    {
        Init();
    }

    private void Reinit()
    {
        if (_container.IsNotNull())
        {
            var items = _container.ToList();
            _container = null; //reset the container so it can be re-initialized
            Init();
            foreach (var item in items)
                _container!.UnsafeAdd(item);
        }
        else
        {
            Init();
        }
    }

    private Transform GetOrCreateChild(GameObject parent, string childName)
    {
        var child = parent.transform.Find(childName).SafeGetGameObject();
        if (child.IsNull())
        {
            child = new GameObject(childName);
            child.transform.SetParent(parent.transform);
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
        }

        return child.transform;
    }

    internal void Setup(AvsVehicle vehicle, MaybeTranslate name, VehicleBuilding.MobileWaterPark vp, int index)
    {
        waterPark = vp.ContentContainer.transform;
        DisplayName = name;
        this.index = index;
        height = vp.Height;
        width = vp.Width;
        canHatchEggs = vp.HatchEggs;
        this.vehicle = vehicle;
        Reinit();
    }

    private record Inhabitant(
        string? techTypeAsString,
        float enzymeAmount,
        float infectedAmount,
        float incubationProgress,
        float hatchDuration,
        float health
    );

    private record LoadingInhabitant(
        Inhabitant Inhabitant,
        CoroutineTask<GameObject> LoadTask,
        TechType UndiscoveredTechType);

    private class Serialized
    {
        public List<Inhabitant>? inhabitants;
        public int width;
        public int height;
        public bool canHatchEggs;
    }

    public void OnProtoSerializeObjectTree(ProtobufSerializer serializer)
    {
        using var log = SmartLog.ForAVS(AV.Owner);
        if (_container.IsNull())
        {
            log.Error($"MobileWaterPark.OnProtoSerializeObjectTree called without a valid container.");
            return;
        }

        if (vehicle.IsNull())
        {
            log.Error($"MobileWaterPark.OnProtoSerializeObjectTree called without a valid vehicle or storageRoot.");
            return;
        }

        if (index <= 0)
        {
            log.Error($"MobileWaterPark.OnProtoSerializeObjectTree called with invalid index {index}.");
            return;
        }

        log.Write($"Saving water park to file");
        var result = new List<(PrefabIdentifier, Inhabitant)>();
        foreach (var item in _container.ToList())
        {
            var prefabId = item.item.GetComponent<PrefabIdentifier>();
            var tt = item.item.GetTechType();
            if (prefabId.IsNull())
            {
                log.Error($"Item {item.item.NiceName()} does not have a valid PrefabIdentifier, skipping.");
                continue;
            }

            CreatureEgg? egg = item.item.GetComponent<CreatureEgg>();
            egg.SafeDo(x => x.UpdateHatchingTime());
            var inhab = new Inhabitant
            (
                infectedAmount: item.item.GetComponent<InfectedMixin>().SafeGet(x => x.infectedAmount, 0),
                enzymeAmount: item.item.GetComponent<Peeper>().SafeGet(x => x.enzymeAmount, 0),
                health: item.item.GetComponent<LiveMixin>().SafeGet(x => x.health, 0),
                incubationProgress: egg.SafeGet(x => x.progress, 0),
                hatchDuration: egg.SafeGet(x => x.GetHatchDuration(), 1),
                techTypeAsString: tt.AsString()
            );

            result.Add((prefabId, inhab));
        }

        vehicle.PrefabID.WriteReflected(
            $"WP{index}",
            new Serialized
            {
                inhabitants = result.Select(x => x.Item2).ToList(),
                width = width,
                height = height,
                canHatchEggs = canHatchEggs
            },
            vehicle.Owner);
    }

    private List<LoadingInhabitant> ReAddWhenDone { get; } = new();

    public void OnProtoDeserializeObjectTree(ProtobufSerializer serializer)
    {

        //Log.Write($"OnProtoDeserializeObjectTree called for water park {index} with vehicle {vehicle?.NiceName()}");
        if (vehicle.IsNull())
        {
            using var log1 = SmartLog.ForAVS(RootModController.AnyInstance);
            log1.Error($"MobileWaterPark.OnProtoDeserializeObjectTree called without a valid vehicle.");
            return;
        }

        using var log = vehicle.NewAvsLog();

        if (index <= 0)
        {
            log.Error($"MobileWaterPark.OnProtoDeserializeObjectTree called with invalid index {index}.");
            return;
        }

        log.Write($"Loading water park from file");
        if (vehicle.PrefabID.ReadReflected($"WP{index}", out Serialized? data, vehicle.Owner))
        {
            height = data.height;
            width = data.width;
            canHatchEggs = data.canHatchEggs;
            Reinit();

            var itemsToAdd = new List<LoadingInhabitant>();
            if (data.inhabitants.IsNotNull())
            {
                log.Write($"Found {data.inhabitants.Count} inhabitants in water park {index}");
                foreach (var inhabitant in data.inhabitants)
                {
                    var typeName = inhabitant.techTypeAsString;
                    TechType undiscoveredTechType = TechType.None;
                    if (typeName?.EndsWith("Undiscovered") == true)
                    {
                        TechTypeExtensions.FromString(typeName, out undiscoveredTechType, true);
                        typeName = typeName.Substring(0, typeName.Length - "Undiscovered".Length);
                    }

                    if (TechTypeExtensions.FromString(typeName, out var tt, true))
                    {
                        log.Write($"Loading inhabitant {tt} for water park {index}");

                        var load = CraftData.GetPrefabForTechTypeAsync(tt);
                        vehicle.Owner.StartAvsCoroutine(
                            nameof(CraftData) + '.' + nameof(CraftData.GetPrefabForTechTypeAsync),
                            _ => load);

                        var itm = new LoadingInhabitant
                        (
                            inhabitant,
                            load,
                            UndiscoveredTechType: undiscoveredTechType
                        );
                        itemsToAdd.Add(itm);
                        log.Debug($"Started loading item {itm} for water park {index}");
                    }
                    else
                    {
                        log.Error(
                            $"Failed to parse tech type {inhabitant.techTypeAsString} for water park {index}, skipping.");
                    }
                }
            }

            log.Write($"Loaded {itemsToAdd.Count} items for water park {index}. Adding them when done");

            ReAddWhenDone.AddRange(itemsToAdd);
        }
        else
        {
            log.Error($"Failed to read water park data for {index}");
        }
    }

    public void OnVehicleLoaded(AvsVehicle expectVehicle)
    {
        vehicle = vehicle.OrRequired(expectVehicle);
        using var log = vehicle.NewAvsLog();
        log.Write($"OnVehicleLoaded called for water park {index} with vehicle {vehicle.NiceName()}");
        if (_container.IsNull())
        {
            log.Error($"MobileWaterPark.OnVehicleLoaded called without a valid container.");
            return;
        }

        if (waterPark.IsNull())
        {
            log.Error($"MobileWaterPark.OnVehicleLoaded called without a valid water park transform.");
            return;
        }

        vehicle.Owner.StartAvsCoroutine(
            nameof(MobileWaterPark) + '.' + nameof(LoadInhabitants),
            log => LoadInhabitants(log, vehicle));
    }

    private IEnumerator LoadInhabitants(SmartLog log, AvsVehicle vehicle)
    {
        log.Write($"Re-adding {ReAddWhenDone.Count} items to the water park");
        foreach (var item in ReAddWhenDone)
        {
            //try to load the item
            yield return item.LoadTask;
            var prefab = item.LoadTask.GetResult();
            if (prefab.IsNull())
            {
                log.Error($"Failed to load item {item.Inhabitant.techTypeAsString} for water park {index}, skipping.");
                continue;
            }

            var thisItem = Utils.SpawnFromPrefab(prefab, null).transform;

            var pickupable = thisItem.GetComponent<Pickupable>();
            if (pickupable.IsNull())
            {
                log.Error($"Item {thisItem.NiceName()} does not have a Pickupable component, skipping.");
                continue;
            }

            var liveMixin = thisItem.GetComponent<LiveMixin>();
            if (liveMixin.IsNotNull())
                liveMixin.health = item.Inhabitant.health;
            var infectedMixin = thisItem.GetComponent<InfectedMixin>();
            if (infectedMixin.IsNotNull())
                infectedMixin.infectedAmount = item.Inhabitant.infectedAmount;
            var peeper = thisItem.GetComponent<Peeper>();
            if (peeper.IsNotNull())
                peeper.enzymeAmount = item.Inhabitant.enzymeAmount;
            var egg = thisItem.GetComponent<CreatureEgg>();
            if (egg.IsNotNull())
            {
                if (item.UndiscoveredTechType != TechType.None)
                {
                    egg.overrideEggType = item.UndiscoveredTechType;
                    egg.isKnown = false;
                    egg.Subscribe(true);    //in case Awake() was already called
                }
                egg.progress = item.Inhabitant.incubationProgress;
                egg.UpdateHatchingTime();
            }

            _container!.AddItem(pickupable);
            log.Write($"Added item {thisItem.NiceName()} to the water park.");
        }

        ReAddWhenDone.Clear();
    }
}