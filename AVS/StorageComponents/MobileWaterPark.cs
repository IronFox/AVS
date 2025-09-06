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
        );

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
            embed.name = prefabId.Id;

            var live = item.item.GetComponent<LiveMixin>();
            var infect = item.item.GetComponent<InfectedMixin>();
            if (live.IsNull() || !live.IsAlive())
            {
                log.Error($"Item {item.item.NiceName()} is not alive, cannot add to water park.");
                if (CanWarnAbout(item.item))
                    ErrorMessage.AddMessage(Translator.GetFormatted(TranslationKey.Error_MobileWaterPark_CannotAdd_FishIsDead, item.item.GetTechName()));
                return;
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
                creature.friendlyToPlayer = true;
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
        bool IsMature
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
                infectedMixin.infectedAmount = item.Inhabitant.InfectedAmount;
            var peeper = thisItem.GetComponent<Peeper>();
            if (peeper.IsNotNull())
                peeper.enzymeAmount = item.Inhabitant.EnzymeAmount;
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
                    creature.friendlyToPlayer = true;// item.Inhabitant.IsFriendlyToPlayer;
                    if (item.Inhabitant.IsBornInside)
                        wpCreature.InitializeCreatureBornInWaterPark();
                    wpCreature.bornInside = item.Inhabitant.IsBornInside;
                    if (!item.Inhabitant.IsMature)
                    {
                        wpCreature.age = item.Inhabitant.Age;
                        wpCreature.SetMatureTime();
                    }
                    else
                    {
                        wpCreature.isMature = true;
                        wpCreature.ResetBreedTime();
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
                        creature.Creature.transform.position = RandomLocation(false);
                        creature.Creature.transform.localRotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
                        log.Debug($"Spawning creature {creature.GameObject.NiceName()} @ {creature.GameObject.transform.localPosition}");
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
                        var wpCreature = item.item.GetComponent<WaterParkCreature>();
                        var creature = item.item.GetComponent<Creature>();
                        if (wpCreature.IsNotNull() && creature.IsNotNull())
                        {
                            creature.Aggression.Value = 0;
                            creature.Hunger.Value = 0;
                            creature.Scared.Value = 0;
                            creature.Happy.Value = 1;
                            creature.Friendliness.Value = 1;



                            var dir = (wpCreature.transform.position - waterPark!.position);
                            var dist = dir.magnitude;
                            dir /= dist;
                            if (FirstWallHit(new Ray(waterPark.position, dir), out var hit))
                            {
                                var threshold = hit.distance * 0.7f;

                                var distanceFromGlass = Math.Max(0, hit.distance - dist);

                                if (distanceFromGlass < 0.5f)
                                {
                                    var force = -dir / (2f * distanceFromGlass + 0.1f) * 0.1f;
                                    force *= Time.fixedDeltaTime / Time.deltaTime;
                                    wpCreature.GetComponent<Rigidbody>().SafeDo(x => x.AddForce(force, ForceMode.Acceleration));
                                }


                                if (dist > hit.distance * 0.9f)
                                {
                                    //var force = -dir * (dist - threshold) / (20f * distanceFromGlass + 0.1f);

                                    //force *= Time.fixedDeltaTime / Time.deltaTime;

                                    //creature.GetComponent<Rigidbody>().SafeDo(x => x.AddForce(force, ForceMode.Acceleration));
                                    wpCreature.transform.position = waterPark.position + dir * hit.distance * 0.9f;
                                    //creature.transform.localEulerAngles = new Vector3(creature.transform.localEulerAngles.x, creature.transform.localEulerAngles.y + 180, creature.transform.localEulerAngles.z);
                                    //log.Debug($"Corrected creature {creature.NiceName()} position to {creature.transform.localPosition}");
                                }
                            }
                            //                        creature.bornInside
                        }
                    }
            }
        }
        else
            log.Error($"MobileWaterPark.Udpate called without a valid container.");

    }
}