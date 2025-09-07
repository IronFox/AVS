using AVS.BaseVehicle;
using AVS.Interfaces;
using AVS.Localization;
using AVS.Log;
using AVS.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

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
    private bool allowReproduction;
    [SerializeField]
    private AvsVehicle? vehicle;

    private Func<bool> ColliderAreLive { get; set; } = () => true;

    [SerializeField]
    private int wallLayerMask = Physics.AllLayers;
    [SerializeField]
    private Transform? waterPark;

    private AvsVehicle AV => vehicle.OrThrow(() => new InvalidOperationException($"Trying to access MobileWaterPark.av before it has been initialized"));



    public void Awake()
    {
        using var log = SmartLog.ForAVS(RootModController.AnyInstance);
        log.Write($"Awake called for {this.NiceName()} (enabled={enabled}, active={gameObject.activeInHierarchy})");
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

    private record LivingThing(
        GameObject GameObject,
        LiveMixin Live,
        InfectedMixin? Infected,
        Pickupable Pickupable
        ) : INullTestableType;

    private record LivingCreature(
        GameObject GameObject,
        LiveMixin Live,
        InfectedMixin? Infected,
        Creature Creature,
        WaterParkCreature WpCreature,
        Pickupable Pickupable
        ) : LivingThing(GameObject, Live, Infected, Pickupable);

    private record LivingEgg(
        GameObject GameObject,
        LiveMixin Live,
        InfectedMixin? Infected,
        CreatureEgg Egg,
        Pickupable Pickupable
        ) : LivingThing(GameObject, Live, Infected, Pickupable);

    private Queue<LivingThing> LivingThingsToAdd { get; } = new();


    private void Init()
    {
        if (_container.IsNotNull())
            return;
        using var log = SmartLog.ForAVS(AV.Owner);
        log.Write(
            $"(Re)Initializing {this.NiceName()} for {DisplayName.Rendered} ({DisplayName.Localize}) with width {width} and height {height}");
        if (_container.IsNull() || _container.sizeX != width || _container.sizeY != height)
        {
            var oldItems = _container?.ToList() ?? [];
            _container = new ItemsContainer(width, height,
                waterPark, DisplayName.Rendered, null);
            _container.SetAllowedTechTypes([]);
            _container.isAllowedToAdd = IsLivingFishOrEgg;
            _container.isAllowedToRemove = IsNotHatchingEgg;
            _container.onAddItem += MyOnAddItem;
            _container.onRemoveItem += MyOnRemoveItem;
            foreach (var item in oldItems)
                _container.UnsafeAdd(item);
        }
        log.Write($"Initialized (enabled={enabled}, active={gameObject.activeInHierarchy})");
    }

    private void MyOnRemoveItem(InventoryItem item)
    {
        item.item.GetComponent<LiveMixin>().SafeDo(x =>
        {
            x.invincible = false;
            x.shielded = false;
        });
        item.item.GetComponent<WaterParkCreature>().SafeDo(x =>
        {
            x.SetOutsideState();
        });
        item.item.gameObject.SetActive(false); //disable the item so it doesn't cause issues
    }

    private void MyOnAddItem(InventoryItem item)
    {
        using var log = AV.NewLazyAvsLog(parameters: Params.Of(item));
        item.item.gameObject.SetActive(false); //keep it off until I can figure this out


        if (item.item/* && item.item.transform.parent != waterPark*/)
        {
            var prefabId = item.item.GetComponent<PrefabIdentifier>();
            if (prefabId.IsNull())
            {
                log.Error($"Item {item.item.NiceName()} does not have a valid PrefabIdentifier, skipping.");
                return;
            }
            var embed = item.item;
            //embed.name = prefabId.Id;

            var live = item.item.GetComponent<LiveMixin>();
            var infect = item.item.GetComponent<InfectedMixin>();
            if (live.IsNull() || !live.IsAlive())
            {
                log.Error($"Item {item.item.NiceName()} is not alive, cannot add to water park.");
                if (CanWarnAbout(item.item))
                    ErrorMessage.AddMessage(Translator.GetFormatted(TranslationKey.Error_MobileWaterPark_CannotAdd_FishIsDead, item.item.GetTechName()));
                return;
            }

            var peeper = item.item.GetComponent<Peeper>();
            if (peeper.IsNotNull() && peeper.enzymeAmount > 0)
            {
                log.Debug($"Activating enzyme visualization for {item.item.NiceName()}");

                peeper.UpdateEnzymeFX();
                peeper.enzymeParticles.Play();
                peeper.enzymeTrail.enabled = true;
                peeper.healingTrigger.SetActive(value: true);
                InfectedMixin component = base.gameObject.GetComponent<InfectedMixin>();
                if ((bool)component)
                {
                    component.SetInfectedAmount(0f);
                }
            }


            //item.item.gameObject.SetActive(true); //enable the item so it can be added to the water park

            //var newLive = embed.GetComponent<LiveMixin>();
            //newLive.health = live.health; //copy health from the original item

            //var newInfect = embed.GetComponent<InfectedMixin>();
            //if (newInfect.IsNotNull() && infect.IsNotNull())
            //{
            //    newInfect.infectedAmount = infect.infectedAmount;
            //}

            log.Debug($"Adding item {embed.NiceName()} from {embed.transform.parent.NiceName()} to water park {waterPark.NiceName()}");
            //embed.transform.SetParent(waterPark, false);
            var wpCreature = embed.GetComponent<WaterParkCreature>();
            var creature = embed.GetComponent<Creature>();
            if (creature.IsNotNull() && wpCreature.IsNotNull())
            {
                //creature.friendlyToPlayer = true;
                creature.cyclopsSonarDetectable = false;


                log.Debug($"Item {embed.NiceName()} is a creature, adding as such.");
                LivingThingsToAdd.Enqueue(new LivingCreature(embed.gameObject, live, infect, creature, wpCreature, embed));
                //creature.currentWaterPark = new WaterPark();
                ////creature.currentWaterPark.internalRadius = 5f;
                //creature.OnAddToWP();
                //if (embed.transform.parent != waterPark)
                //{
                //    LogWriter.Default.Error($"Creature {embed.NiceName()} was not added to the water park instances, but to {embed.transform.parent.NiceName()}");
                //    embed.transform.SetParent(waterPark, false);
                //}
                //creature.transform.position = RandomLocation(false);
                //creature.transform.localRotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
                //LogWriter.Default.Debug($"Spawned creature {embed.NiceName()} @ {embed.transform.localPosition}");
            }
            else
            {
                var egg = embed.GetComponent<CreatureEgg>();
                if (egg.IsNotNull())
                {
                    log.Debug($"Item {embed.NiceName()} is an egg, adding as such.");
                    LivingThingsToAdd.Enqueue(new LivingEgg(embed.gameObject, live, infect, egg, embed));

                    //egg.transform.SetParent(waterPark, false);
                    //egg.transform.position = RandomLocation(true);
                    //if (canHatchEggs)
                    //{
                    //    egg.OnAddToWaterPark();
                    //    if (embed.transform.parent != waterPark)
                    //    {
                    //        LogWriter.Default.Error($"Egg {embed.NiceName()} was not added to the water park instances, but to {embed.transform.parent.NiceName()}");
                    //        embed.transform.SetParent(waterPark, false);
                    //    }

                    //}
                    //LogWriter.Default.Debug($"Spawned egg {embed.NiceName()} @ {embed.transform.localPosition}");
                }
                else
                    log.Error($"Item {embed.NiceName()} is neither a creature nor an egg, cannot add to water park.");
            }
        }
    }

    private RayCaster Caster { get; } = new();

    private bool FirstWallHit(Ray ray, out RaycastHit hit)
    {
        //RaycastHit hit;
        var rb = AV.useRigidbody;
        float d = float.MaxValue;
        bool rs = false;
        hit = default;

        Caster.RayCastAll(ray, 20f, wallLayerMask);
        foreach (var h in Caster)
            if (h.collider.attachedRigidbody == rb && !h.collider.isTrigger)
            {
                if (h.distance < d)
                {
                    d = h.distance;
                    rs = true;
                    hit = h;
                }
            }
        return rs;
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
            if (haveLocation = FirstWallHit(new Ray(waterPark!.position, ray), out hit))
                break;
        //LogWriter.Default.Debug($"haveLocation: {haveLocation} => {ray}");

        var rs = haveLocation
            ? waterPark!.position + (hit.point - waterPark!.position) * random.magnitude * 0.9f
            : waterPark!.position + random;

        if (dropToFloor)
        {
            if (FirstWallHit(new Ray(rs, Vector3.down), out hit))
                return hit.point + Vector3.up * 0.2f;
        }

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
        allowReproduction = vp.AllowReproduction;
        this.vehicle = vehicle;
        this.ColliderAreLive = vp.CollidersAreLive;
        this.wallLayerMask = vp.WallLayerMask;
        Reinit();
    }

    private record Inhabitant(
        string? TechTypeAsString,
        float EnzymeAmount,
        float InfectedAmount,
        float IncubationProgress,
        float HatchDuration,
        float Health,
        bool IsFriendlyToPlayer,
        bool IsBornInside,
        float Age,
        bool? IsMature
    );

    private record LoadingInhabitant(
        Inhabitant Inhabitant,
        InstanceContainer Result,
        Coroutine LoadTask);

    private record Serialized
    (
        List<Inhabitant>? Inhabitants,
        int Width,
        int Height,
        bool CanHatchEggs,
        bool AllowReproduction
    );

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
            Creature? creature = item.item.GetComponent<Creature>();
            CreatureEgg? egg = item.item.GetComponent<CreatureEgg>();
            WaterParkCreature? wpCreature = item.item.GetComponent<WaterParkCreature>();
            egg.SafeDo(x => x.UpdateHatchingTime());
            var inhab = new Inhabitant
            (
                InfectedAmount: item.item.GetComponent<InfectedMixin>().SafeGet(x => x.infectedAmount, 0),
                EnzymeAmount: item.item.GetComponent<Peeper>().SafeGet(x => x.enzymeAmount, 0),
                Health: item.item.GetComponent<LiveMixin>().SafeGet(x => x.health, 0),
                IncubationProgress: egg.SafeGet(x => x.progress, 0),
                HatchDuration: egg.SafeGet(x => x.GetHatchDuration(), 1),
                TechTypeAsString: tt.AsString(),
                IsFriendlyToPlayer: creature.SafeGet(x => x._friendlyToPlayer, false),
                IsBornInside: wpCreature.SafeGet(x => x.bornInside, false),
                Age: wpCreature.SafeGet(x => x.age, 0),
                IsMature: wpCreature.SafeGet(x => x.isMature, true)
            );

            result.Add((prefabId, inhab));
        }

        vehicle.PrefabID.WriteReflected(
            $"WP{index}",
            new Serialized
            (
                Inhabitants: result.Select(x => x.Item2).ToList(),
                Width: width,
                Height: height,
                CanHatchEggs: canHatchEggs,
                AllowReproduction: allowReproduction
            ),
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
            height = data.Height;
            width = data.Width;
            canHatchEggs = data.CanHatchEggs;
            allowReproduction = data.AllowReproduction;
            Reinit();

            var itemsToAdd = new List<LoadingInhabitant>();
            if (data.Inhabitants.IsNotNull())
            {
                log.Write($"Found {data.Inhabitants.Count} inhabitants in water park {index}");
                foreach (var inhabitant in data.Inhabitants)
                {
                    if (TechTypeExtensions.FromString(inhabitant.TechTypeAsString, out var tt, true))
                    {
                        log.Write($"Loading inhabitant {tt} for water park {index}");

                        InstanceContainer result = new();
                        var co = vehicle.Owner.StartAvsCoroutine(
                            nameof(CraftData) + '.' + nameof(CraftData.GetPrefabForTechTypeAsync),
                            log => AvsCraftData.InstantiateFromPrefabAsync(log, tt, result));

                        var itm = new LoadingInhabitant
                        (
                            inhabitant,
                            result,
                            co
                        );
                        itemsToAdd.Add(itm);
                        log.Debug($"Started loading item {itm} for water park {index}");
                    }
                    else
                    {
                        log.Error(
                            $"Failed to parse tech type {inhabitant.TechTypeAsString} for water park {index}, skipping.");
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
        log.Write($"OnVehicleLoaded called for water park {index} with vehicle {vehicle.NiceName()} (enabled={enabled}, active={gameObject.activeInHierarchy})");
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
            var thisItem = item.Result.Instance;
            if (thisItem.IsNull())
            {
                log.Error($"Failed to load item {item.Inhabitant.TechTypeAsString} for water park {index}, skipping.");
                continue;
            }

            var pickupable = thisItem.GetComponent<Pickupable>();
            if (pickupable.IsNull())
            {
                log.Error($"Item {thisItem.NiceName()} does not have a Pickupable component, skipping.");
                continue;
            }

            var liveMixin = thisItem.GetComponent<LiveMixin>();
            if (liveMixin.IsNull())
            {
                log.Error($"Item {thisItem.NiceName()} does not have a LiveMixin component, skipping.");
                continue;
            }
            liveMixin.health = item.Inhabitant.Health;
            var infectedMixin = thisItem.GetComponent<InfectedMixin>();
            if (infectedMixin.IsNotNull())
            {
                log.Debug($"Setting infected amount for {thisItem.NiceName()} to {item.Inhabitant.InfectedAmount.ToStr()}");
                infectedMixin.infectedAmount = item.Inhabitant.InfectedAmount;
            }
            else
                log.Warn($"Item {thisItem.NiceName()} does not have an InfectedMixin component, skipping infected amount.");
            var peeper = thisItem.GetComponent<Peeper>();
            if (peeper.IsNotNull())
            {
                log.Debug($"Setting enzyme amount for peeper {thisItem.NiceName()} to {item.Inhabitant.EnzymeAmount.ToStr()}");
                peeper.enzymeAmount = item.Inhabitant.EnzymeAmount;
            }
            else
                log.Debug($"Item {thisItem.NiceName()} is not a peeper, skipping enzyme amount.");
            var egg = thisItem.GetComponent<CreatureEgg>();
            if (egg.IsNotNull())
            {
                egg.progress = item.Inhabitant.IncubationProgress;
                egg.UpdateHatchingTime();
                //LivingThingsToAdd.Enqueue(new LivingEgg(thisItem, liveMixin, infectedMixin, egg, pickupable));
            }
            else
            {
                var wpCreature = thisItem.GetComponent<WaterParkCreature>();
                var creature = thisItem.GetComponent<Creature>();
                if (creature.IsNotNull() && wpCreature.IsNotNull())
                {
                    creature.friendlyToPlayer = item.Inhabitant.IsFriendlyToPlayer;
                    if (item.Inhabitant.IsBornInside)
                        wpCreature.InitializeCreatureBornInWaterPark();
                    wpCreature.bornInside = item.Inhabitant.IsBornInside;
                    if (!(item.Inhabitant.IsMature ?? true))
                    {
                        wpCreature.age = item.Inhabitant.Age;
                        wpCreature.SetMatureTime();
                    }
                    else
                    {
                        wpCreature.isMature = true;
                    }

                    //LivingThingsToAdd.Enqueue(new LivingCreature(thisItem, liveMixin, infectedMixin, creature, wpCreature, pickupable));
                }
                else
                {
                    log.Error($"Item {thisItem.NiceName()} is neither a creature nor an egg, cannot add to water park.");
                    continue;
                }
            }

            _container!.AddItem(pickupable);
            log.Write($"Added item {thisItem.NiceName()} to the water park.");
        }

        ReAddWhenDone.Clear();
    }

    private bool collidersLive = false;
    public void Update()
    {
        using var log = vehicle.IsNotNull() ? vehicle.NewLazyAvsLog() : SmartLog.LazyForAVS(RootModController.AnyInstance);
        //log.Debug($"MobileWaterPark.Udpate called for water park {index} with vehicle {vehicle?.NiceName()}");
        if (_container != null)
        {
            if (ColliderAreLive() != collidersLive)
            {
                collidersLive = ColliderAreLive();

                if (!collidersLive)
                {
                    foreach (var item in _container.ToList())
                    {
                        if (item.item.GetComponent<WaterParkCreature>().IsNotNull())
                            item.item.gameObject.SetActive(false); //disable the fish so it doesn't cause issues
                    }
                }
                else
                    foreach (var item in _container.ToList())
                    {
                        if (item.item.GetComponent<WaterParkCreature>().IsNotNull())
                            item.item.gameObject.SetActive(true);
                    }

            }
            if (collidersLive)
            {
                while (LivingThingsToAdd.Count > 0)
                {
                    var thing = LivingThingsToAdd.Dequeue();
                    thing.Live.invincible = true;
                    thing.Live.shielded = true;
                    if (thing is LivingCreature creature)
                    {
                        SetInsideState(creature);
                        creature.WpCreature.swimBehaviour = creature.GameObject.GetComponent<SwimBehaviour>();
                        creature.WpCreature.breedInterval = creature.WpCreature.data.growingPeriod * 0.5f;
                        creature.WpCreature.ResetBreedTime();
                        creature.Creature.transform.position = RandomLocation(false);
                        creature.Creature.transform.localRotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
                        log.Debug($"Spawning creature {creature.GameObject.NiceName()} @ {creature.GameObject.transform.localPosition} with breed interval {creature.WpCreature.breedInterval}");
                        creature.GameObject.SetActive(true); //enable the item so it can be added to the water park
                    }
                    else if (thing is LivingEgg egg)
                    {
                        egg.Egg.transform.position = RandomLocation(true);
                        log.Debug($"Spawning egg {egg.GameObject.NiceName()} @ {egg.GameObject.transform.localPosition}");
                        if (canHatchEggs)
                        {
                            egg.Egg.OnAddToWaterPark();
                        }
                        egg.GameObject.GetComponent<Rigidbody>().SafeDo(x => x.isKinematic = true);
                        egg.GameObject.SetActive(true); //enable the item so it can be added to the water park
                    }
                    else
                        log.Error($"Unknown living thing {thing?.GetType().Name} cannot be added to water park.");
                }
                if (Time.deltaTime > 0)
                    foreach (var item in _container.ToList())
                    {
                        var living = LivingFrom(item.item);
                        if (living is LivingCreature creature)
                        {
                            Update(creature);
                            //creature.Aggression.Value = 0;
                            //creature.Hunger.Value = 0;
                            //creature.Scared.Value = 0;
                            //creature.Happy.Value = 1;
                            //creature.Friendliness.Value = 1;


                            //var dir = (wpCreature.transform.position - waterPark!.position);
                            //var dist = dir.magnitude;
                            //dir /= dist;
                            //if (FirstWallHit(new Ray(waterPark.position, dir), out var hit))
                            //{
                            //    var threshold = hit.distance * 0.7f;

                            //    var distanceFromGlass = Math.Max(0, hit.distance - dist);

                            //    if (distanceFromGlass < 0.5f)
                            //    {
                            //        var force = -dir / (2f * distanceFromGlass + 0.1f) * 0.1f;
                            //        force *= Time.fixedDeltaTime / Time.deltaTime;
                            //        wpCreature.GetComponent<Rigidbody>().SafeDo(x => x.AddForce(force, ForceMode.Acceleration));
                            //    }


                            //    if (dist > hit.distance * 0.9f)
                            //    {
                            //        //var force = -dir * (dist - threshold) / (20f * distanceFromGlass + 0.1f);

                            //        //force *= Time.fixedDeltaTime / Time.deltaTime;

                            //        //creature.GetComponent<Rigidbody>().SafeDo(x => x.AddForce(force, ForceMode.Acceleration));
                            //        wpCreature.transform.position = waterPark.position + dir * hit.distance * 0.9f;
                            //        //creature.transform.localEulerAngles = new Vector3(creature.transform.localEulerAngles.x, creature.transform.localEulerAngles.y + 180, creature.transform.localEulerAngles.z);
                            //        //log.Debug($"Corrected creature {creature.NiceName()} position to {creature.transform.localPosition}");
                            //    }
                            //}
                            //                        creature.bornInside
                        }
                    }
            }
        }
        else
            log.Error($"MobileWaterPark.Udpate called without a valid container.");

    }

    private void SetInsideState(LivingCreature creature)
    {
        //creature.WpCreature.SetInsideState();
        using var log = AV.NewLazyAvsLog();
        if (creature.WpCreature.isInside)
        {
            return;
        }
        log.Debug($"Setting inside state for {creature.WpCreature.NiceName()}");

        creature.WpCreature.isInside = true;

        Animator animator = creature.WpCreature.gameObject.GetComponent<Creature>().GetAnimator();
        if (animator != null)
        {
            AnimateByVelocity component = animator.GetComponent<AnimateByVelocity>();
            if (component != null)
            {
                creature.WpCreature.outsideMoveMaxSpeed = component.animationMoveMaxSpeed;
                component.animationMoveMaxSpeed = creature.WpCreature.swimMaxVelocity;
            }
        }

        Locomotion component2 = creature.WpCreature.gameObject.GetComponent<Locomotion>();
        component2.canMoveAboveWater = true;
        creature.WpCreature.locomotionParametersOverrode = true;
        creature.WpCreature.locomotionDriftFactor = component2.driftFactor;
        component2.driftFactor = 0.1f;
        component2.forwardRotationSpeed = 0.6f;
        if (creature.WpCreature.swimBehaviour != null)
        {
            creature.WpCreature.outsideTurnSpeed = creature.WpCreature.swimBehaviour.turnSpeed;
            creature.WpCreature.swimBehaviour.turnSpeed = 1f;
        }

        creature.WpCreature.disabledBehaviours = new List<Behaviour>();
        Behaviour[] componentsInChildren = creature.WpCreature.GetComponentsInChildren<Behaviour>(includeInactive: true);
        foreach (Behaviour behaviour in componentsInChildren)
        {
            if (behaviour == null)
            {
                log.Warn($"Discarded missing behaviour on a WaterParkCreature gameObject {creature.WpCreature.NiceName()}");
            }
            else
            {
                if (!behaviour.enabled)
                {
                    continue;
                }

                Type type = behaviour.GetType();

                for (int j = 0; j < WaterParkCreature.behavioursToDisableInside.Length; j++)
                {
                    if (type.Equals(WaterParkCreature.behavioursToDisableInside[j]) || type.IsSubclassOf(WaterParkCreature.behavioursToDisableInside[j]))
                    {
                        log.Debug($"Disabling behaviour {behaviour.GetType().Name} {j + 1}/{WaterParkCreature.behavioursToDisableInside.Length} on {creature.WpCreature.NiceName()}");
                        behaviour.enabled = false;
                        creature.WpCreature.disabledBehaviours.Add(behaviour);
                        break;
                    }
                }
            }
        }
    }

    private LivingThing LivingFrom(Pickupable item)
    {
        var live = item.GetComponent<LiveMixin>();
        var infect = item.GetComponent<InfectedMixin>();
        var wpCreature = item.GetComponent<WaterParkCreature>();
        var creature = item.GetComponent<Creature>();
        var egg = item.GetComponent<CreatureEgg>();

        if (creature.IsNotNull() && wpCreature.IsNotNull())
            return new LivingCreature(item.gameObject, live, infect, creature, wpCreature, item);
        else if (egg.IsNotNull())
            return new LivingEgg(item.gameObject, live, infect, egg, item);
        else
            return new LivingThing(item.gameObject, live, infect, item);
    }

    private void UpdateMovement(LivingCreature creature)
    {
        if (Time.time > creature.WpCreature.timeNextSwim)
        {
            creature.WpCreature.swimTarget = RandomLocation(false);
            creature.WpCreature.swimBehaviour.SwimTo(creature.WpCreature.swimTarget, Mathf.Lerp(creature.WpCreature.swimMinVelocity, creature.WpCreature.swimMaxVelocity, creature.WpCreature.age));
            creature.WpCreature.timeNextSwim = Time.time + creature.WpCreature.swimInterval * UnityEngine.Random.Range(1f, 2f);
        }

        creature.WpCreature.transform.position = EnsureInside(creature.WpCreature.transform.position);
        creature.WpCreature.swimTarget = EnsureInside(creature.WpCreature.swimTarget);

    }

    private void Update(LivingCreature creature)
    {
        UpdateMovement(creature);
        double timePassed = DayNightCycle.main.timePassed;
        if (!creature.WpCreature.isMature)
        {
            float a = (float)(creature.WpCreature.matureTime - (double)creature.WpCreature.data.growingPeriod);
            creature.WpCreature.age = Mathf.InverseLerp(a, (float)creature.WpCreature.matureTime, (float)timePassed);
            creature.WpCreature.transform.localScale = Mathf.Lerp(creature.WpCreature.data.initialSize, creature.WpCreature.data.maxSize, creature.WpCreature.age) * Vector3.one;
            if (creature.WpCreature.age == 1f)
            {
                creature.WpCreature.isMature = true;
                if (creature.WpCreature.data.canBreed)
                {
                    creature.WpCreature.breedInterval = creature.WpCreature.data.growingPeriod * 0.5f;
                    if (creature.WpCreature.timeNextBreed < 0f)
                    {
                        creature.WpCreature.ResetBreedTime();
                    }
                }
                else
                {

                    //this is some strange behavior. It seems to reinstantiate itself as an adult...
                    AssetReferenceGameObject adultPrefab = creature.WpCreature.data.adultPrefab;
                    if (adultPrefab != null && adultPrefab.RuntimeKeyIsValid())
                    {
                        using var log = AV.NewLazyAvsLog();
                        log.Debug($"Reinstantiating {creature.WpCreature.NiceName()} as an adult.");

                        AV.Owner.StartAvsCoroutine(nameof(MobileWaterPark) + '.' + nameof(BornAsync), log => BornAsync(log, adultPrefab, creature.WpCreature.transform.position));
                        _container!.RemoveItem(creature.WpCreature.GetComponent<Pickupable>());
                        Destroy(creature.WpCreature.gameObject);
                        return;
                    }
                }
            }
        }

        if (creature.WpCreature.GetCanBreed() && timePassed > (double)creature.WpCreature.timeNextBreed)
        {
            using var log = AV.NewLazyAvsLog();
            log.Debug($"Creature {creature.WpCreature.NiceName()} is ready to breed.");
            creature.WpCreature.ResetBreedTime();
            var breedingPartner = GetBreedingPartner(creature);
            if (breedingPartner.IsNotNull())
            {
                breedingPartner.ResetBreedTime();
                log.Debug($"Breeding {creature.WpCreature.NiceName()} with {breedingPartner.NiceName()}");
                if (FirstWallHit(new Ray(creature.WpCreature.transform.position, Vector3.down), out var hit))
                    AV.Owner.StartAvsCoroutine(nameof(MobileWaterPark) + '.' + nameof(BornAsync), log => BornAsync(log, creature.WpCreature.data.eggOrChildPrefab, hit.point + Vector3.up * 0.2f));
                else
                    AV.Owner.StartAvsCoroutine(nameof(MobileWaterPark) + '.' + nameof(BornAsync), log => BornAsync(log, creature.WpCreature.data.eggOrChildPrefab, creature.WpCreature.transform.position + Vector3.down));
            }
            else
                log.Debug($"Creature {creature.WpCreature.NiceName()} could not find a breeding partner. Retrying in {creature.WpCreature.breedInterval}");
        }
    }

    private WaterParkCreature? GetBreedingPartner(LivingCreature creature)
    {
        if (!Container.HasRoomFor(creature.Pickupable))
        {
            return null;
        }

        WaterParkCreature? result = null;
        float num = float.MaxValue;
        TechType techType = creature.WpCreature.GetTechType();
        foreach (var item in Container)
        {
            if (!(item.item == creature.Pickupable))
            {
                WaterParkCreature? waterParkCreature = item.item.GetComponent<WaterParkCreature>();
                if (waterParkCreature.IsNotNull()
                    && waterParkCreature.GetCanBreed()
                    && waterParkCreature.timeNextBreed < num
                    && waterParkCreature.GetTechType() == techType)
                {
                    num = waterParkCreature.timeNextBreed;
                    result = waterParkCreature;
                }
            }
        }

        return result;
    }

    private IEnumerator BornAsync(SmartLog log, AssetReferenceGameObject creaturePrefabReference, Vector3 position)
    {
        log.Write($"Spawning new creature from prefab {creaturePrefabReference.RuntimeKey} at {position}");
        CoroutineTask<GameObject> task = AddressablesUtility.InstantiateAsync(creaturePrefabReference.RuntimeKey as string, null, position, Quaternion.identity, awake: false);
        yield return task;
        GameObject result = task.GetResult();
        WaterParkCreature component = result.GetComponent<WaterParkCreature>();
        if (component != null)
        {
            component.age = 0f;
            component.bornInside = true;
            component.InitializeCreatureBornInWaterPark();
            result.transform.localScale = component.data.initialSize * Vector3.one;
        }

        Pickupable pickupable = result.EnsureComponent<Pickupable>();
        result.SetActive(value: true);
        _container!.AddItem(pickupable);
        log.Write($"Spawned new creature {result.NiceName()} at {position} and added to water park.");
    }

    private Vector3 EnsureInside(Vector3 p)
    {
        var dir = (p - waterPark!.position);
        var dist = dir.magnitude;
        dir /= dist;
        if (FirstWallHit(new Ray(waterPark.position, dir), out var hit))
        {
            if (dist > hit.distance * 0.9f)
            {
                return waterPark.position + dir * hit.distance * 0.9f;
            }
        }
        return p;
    }
}