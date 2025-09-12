using Assets.Behavior.Util.Math;
using AVS.BaseVehicle;
using AVS.Interfaces;
using AVS.Localization;
using AVS.Log;
using AVS.Util;
using AVS.Util.Math;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AVS.StorageComponents.WaterPark;

/// <summary>
/// Helper structure for fishtanks that work like an aquapark but can
/// be mobile.
/// </summary>
internal class MobileWaterPark : MonoBehaviour, ICraftTarget, IProtoTreeEventListener
{
    private ItemsContainer? _container = null;

    private Dictionary<int, WaterParkInhabitant> Inhabitants { get; } = [];
    private Queue<WaterParkInhabitant> InhabitantAddQueue { get; } = [];

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
    private float topMargin = 0;
    [SerializeField]
    private float bottomMargin = 0;
    [SerializeField]
    private float horizontalMargin = 0;


    [SerializeField]
    internal bool hatchEggs = true;
    [SerializeField]
    internal bool breedCreatures;
    [SerializeField]
    private AvsVehicle? vehicle;

    private Func<bool> ColliderAreLive { get; set; } = () => true;

    [SerializeField]
    internal int wallLayerMask = Physics.AllLayers;
    [SerializeField]
    private Transform? waterPark;

    private Vector3? onAddLocation;

    internal AvsVehicle AV => vehicle.OrThrow(() => new InvalidOperationException($"Trying to access MobileWaterPark.av before it has been initialized"));


    IEnumerable<WaterParkInhabitant> GetAllInhabitants()
    {
        List<int>? remove = null;
        foreach (var inhab in Inhabitants)
        {
            if (inhab.Value.GameObject.IsNotNull())
                yield return inhab.Value;
            else
                (remove ??= []).Add(inhab.Key);
        }
        if (remove.IsNotNull())
        {
            using var log = AV.NewLazyAvsLog();
            foreach (var id in remove)
            {
                log.Warn($"Removing missing inhabitant with id {id} from water park {index}");
                Inhabitants.Remove(id);
            }
        }

    }


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
        using var log = AV.NewLazyAvsLog(parameters: Params.Of(item.item));
        var iid = item.item.gameObject.GetInstanceID();
        if (Inhabitants.TryGetValue(iid, out var inhab))
        {
            Inhabitants.Remove(iid);
            inhab.OnDeinstantiate();
        }
        else
            log.Warn($"Item {item.item.NiceName()}/{iid} was not found in inhabitants list of water park {DisplayName.Rendered}, cannot remove.");


        item.item.gameObject.SetActive(false); //disable the item so it doesn't cause issues
        log.Debug($"Remaining inhabitants: {Inhabitants.Count}");
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
                InfectedMixin component = gameObject.GetComponent<InfectedMixin>();
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
                InhabitantAddQueue.Enqueue(new WaterParkCreatureInhabitant(
                    this,
                    embed.gameObject,
                    embed.gameObject.GetComponentInChildren<Rigidbody>(),
                    live,
                    infect,
                    embed,
                    wpCreature,
                    creature,
                    onAddLocation
                    ));
                onAddLocation = null;
            }
            else
            {
                var egg = embed.GetComponent<CreatureEgg>();
                if (egg.IsNotNull())
                {
                    log.Debug($"Item {embed.NiceName()} is an egg, adding as such.");
                    InhabitantAddQueue.Enqueue(new WaterParkEggInhabitant(this, embed.gameObject, live, infect, egg, embed, onAddLocation));
                    onAddLocation = null;
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

    private readonly record struct CreaturePosition(Degrees HAngle, Degrees VAngle, Vector3 Position, float Radius)
    {
        public CreaturePosition(Transform t, float radius)
            : this(Degrees.FromEulerY(t.rotation), Degrees.Zero, t.position, radius)
        { }
    }

    private readonly record struct Restricted(CreaturePosition Creature, Degrees MaxHAngleDelta, Degrees MaxVAngleDelta, Degrees MaxVAngle)
    {
        private static Degrees Random(Degrees max) => Degrees.RandomIn(-max, max);
        private static Degrees Random(Degrees min, Degrees max) => Degrees.RandomIn(min, max);

        public Vector3 GetRandomTarget(float distance = 2.5f)
        {
            using var log = SmartLog.ForAVS(RootModController.AnyInstance);

            var a = Creature.HAngle + Random(MaxHAngleDelta);
            var x = a.Rad.Sin;
            var z = a.Rad.Cos;
            var maxV = MaxVAngle;
            var upMax = (Creature.VAngle + maxV).WrapToPlusMinus180().ClampTo(-maxV, maxV);
            var upMin = (Creature.VAngle - maxV).WrapToPlusMinus180().ClampTo(-maxV, maxV);

            var up = Random(upMin, upMax);

            float y = up.Rad.Sin;
            float r = up.Rad.Cos;
            log.Debug($"a={a}, x={x.ToStr()}, z={z.ToStr()}, up={up}, y={y.ToStr()}, r={r.ToStr()}, av={Creature.VAngle}, up={up}, min={upMin}, max={upMax}, maxhr={MaxHAngleDelta}, maxvr={MaxVAngleDelta}, maxva={maxV}");


            var dir = new Vector3(x * r, y, z * r);
            return Creature.Position + dir * distance;
        }
    }

    private bool GetAlignedRandomSwimTarget(Restricted creature, out Vector3 target)
    {
        target = creature.GetRandomTarget();

        if (EnforceEnclosure(ref target, null, creature.Creature.Radius))
            return false;
        return true;
    }

    internal Vector3 GetRandomSwimTarget(Transform transform, float radius)
    {
        var creature = new CreaturePosition(transform, radius);
        var restricted = new Restricted(creature, Degrees.Thirty, Degrees.Thirty / 2, Degrees.Thirty / 2);
        var unrestricted = new Restricted(creature, Degrees.OneEighty, Degrees.Ninety, Degrees.Thirty);
        for (int i = 0; i < 100; i++)
        {

            if (GetAlignedRandomSwimTarget(restricted, out var target))
                return target;
            if (GetAlignedRandomSwimTarget(restricted, out target))
                return target;
            if (GetAlignedRandomSwimTarget(restricted, out target))
                return target; //three times restricted for one unrestricted attempt => usually restricted
            if (GetAlignedRandomSwimTarget(unrestricted, out target))
                return target;
        }
        throw new InvalidOperationException($"Failed to find a swim target for {transform.NiceName()} in water park {DisplayName.Rendered}");
    }

    internal Vector3 GetRandomLocation(bool dropToFloor, float worldItemRadius)
    {
        var center = waterPark!.position;
        var random = UnityEngine.Random.insideUnitSphere;
        var ray = random.normalized;
        var haveLocation = false;
        RaycastHit hit = default;
        for (var i = 0; i < 100; i++)
        {
            if (haveLocation = FirstWallHit(new Ray(waterPark!.position, ray), out hit))
                break;
            random = UnityEngine.Random.insideUnitSphere;
            ray = random.normalized;
        }
        //LogWriter.Default.Debug($"haveLocation: {haveLocation} => {ray}");

        Vector3 rs;
        if (haveLocation)
        {
            var dir = hit.point - center;
            var dist = dir.magnitude;
            dir /= dist;
            float maxDist = dist - worldItemRadius - horizontalMargin;
            var randomDist = random.magnitude * maxDist;
            rs = center + dir * randomDist;
        }
        else
            rs = center + random;


        if (dropToFloor)
        {
            rs = DropToFloor(rs, worldItemRadius);
        }
        return rs;
        //return EnsureInside(rs, null, worldItemRadius);
    }

    private Vector3 DropToFloor(Vector3 rs, float worldItemRadius)
    {
        if (FirstWallHit(new Ray(rs, Vector3.down), out var hit))
            return hit.point + Vector3.up * (bottomMargin + worldItemRadius);
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
        topMargin = vp.TopMargin;
        bottomMargin = vp.BottomMargin;
        horizontalMargin = vp.HorizontalMargin;
        hatchEggs = vp.HatchEggs;
        breedCreatures = vp.AllowReproduction;
        this.vehicle = vehicle;
        ColliderAreLive = vp.CollidersAreLive;
        wallLayerMask = vp.WallLayerMask;
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
                CanHatchEggs: hatchEggs,
                AllowReproduction: breedCreatures
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
            hatchEggs = data.CanHatchEggs;
            breedCreatures = data.AllowReproduction;
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
                infectedMixin.SetInfectedAmount(item.Inhabitant.InfectedAmount);
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
        if (_container.IsNotNull())
        {
            if (ColliderAreLive() != collidersLive)
            {
                collidersLive = ColliderAreLive();

                foreach (var inhab in GetAllInhabitants())
                    inhab.SignalCollidersChanged(collidersLive);
            }
            if (collidersLive)
            {
                while (InhabitantAddQueue.Count > 0)
                {
                    var thing = InhabitantAddQueue.Dequeue();

                    Inhabitants[thing.InstanceId] = thing;
                    thing.OnInstantiate();
                }
                if (Time.deltaTime > 0)
                    foreach (var inhab in GetAllInhabitants())
                    {
                        inhab.OnUpdate();
                    }
            }
        }
        else
            log.Error($"MobileWaterPark.Udpate called without a valid container.");

    }

    public void LateUpdate()
    {
        if (_container.IsNotNull() && collidersLive)
        {
            foreach (var inhab in GetAllInhabitants())
                inhab.OnLateUpdate();
        }
    }

    public void FixedUpdate()
    {
        if (_container.IsNotNull() && collidersLive)
        {
            foreach (var inhab in GetAllInhabitants())
                inhab.OnFixedUpdate();
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



    internal WaterParkCreatureInhabitant? GetBreedingPartner(WaterParkCreatureInhabitant creature)
    {
        if (!Container.HasRoomFor(creature.Pickupable))
        {
            return null;
        }

        WaterParkCreatureInhabitant? result = null;
        float num = float.MaxValue;
        TechType techType = creature.WpCreature.GetTechType();
        foreach (var inhab in GetAllInhabitants())
        {
            if (inhab is WaterParkCreatureInhabitant wpc && wpc.GameObject != creature.GameObject)
            {
                var waterParkCreature = wpc.WpCreature;
                if (waterParkCreature.GetCanBreed()
                    && waterParkCreature.timeNextBreed < num
                    && waterParkCreature.GetTechType() == techType)
                {
                    num = waterParkCreature.timeNextBreed;
                    result = wpc;
                }
            }
        }

        return result;
    }

    private IEnumerator BornAsync(SmartLog log, AssetReferenceGameObject creaturePrefabReference, Vector3 position, float? infectedAmount, float? enzymeAmount)
    {
        log.Write($"Spawning new creature from prefab {creaturePrefabReference.RuntimeKey} at {position}");
        CoroutineTask<GameObject> task = AddressablesUtility.InstantiateAsync(creaturePrefabReference.RuntimeKey as string, null, position, Quaternion.identity, awake: false);
        yield return task;
        GameObject result = task.GetResult();
        WaterParkCreature creature = result.GetComponent<WaterParkCreature>();
        onAddLocation = position;
        if (creature.IsNotNull())
        {
            creature.age = 0f;
            creature.bornInside = true;
            creature.InitializeCreatureBornInWaterPark();
            result.transform.localScale = creature.data.initialSize * Vector3.one;

            var egg = result.GetComponent<CreatureEgg>();
            if (egg.IsNotNull())
            {
                onAddLocation = DropToFloor(position, GetItemWorldRadius(creature.GetTechType(), result));
            }
        }

        Peeper? peeper = result.GetComponent<Peeper>();
        if (peeper.IsNotNull())
            peeper.enzymeAmount = enzymeAmount ?? 0;

        Pickupable pickupable = result.EnsureComponent<Pickupable>();
        result.SetActive(value: true);
        if (infectedAmount.IsNotNull())
        {
            result.GetComponent<InfectedMixin>().SafeDo(x => x.SetInfectedAmount(infectedAmount.Value));
        }
        _container!.AddItem(pickupable);
        log.Write($"Spawned new creature {result.NiceName()} at {position} and added to water park.");
    }

    internal bool EnforceEnclosure(Transform transform, float radius)
    {
        var p = transform.position;
        if (EnforceEnclosure(ref p, null, radius))
        {
            transform.position = p;
            return true;
        }
        return false;
    }
    internal bool EnforceEnclosure(ref Vector3 p, Rigidbody? rb, float creatureRadius, bool clampVertically = true)
    {
        using var log = AV.NewLazyAvsLog(parameters: Params.Of(p, creatureRadius));
        var center = waterPark!.position;
        var dir = p - center;
        var hDir = dir.Flat();
        var hDist = hDir.magnitude;
        bool rs = false;
        float push = 2f;
        if (hDist > 0.001f)
        {
            hDir /= hDist;
            var origin = new Vector3(center.x, p.y, center.z);
            var d = hDir.UnFlat();
            if (FirstWallHit(new Ray(origin, d), out var hit2))
            {
                float maxDistance = hit2.distance - creatureRadius - horizontalMargin;
                if (hDist > maxDistance)
                {
                    p = (origin.Flat() + hDir * (maxDistance - 0.05f)).UnFlat(p.y);
                    rs = true;
                    rb.SafeDo(x => x.AddForce(-d * push, ForceMode.VelocityChange));
                }
            }
            else
            {
                log.Warn($"No wall hit found horizontally from {origin} towards {d}, cannot enforce horizontal enclosure.");
                rs = true; //don't know where the wall is, so consider it out of bounds
            }
        }
        if (clampVertically)
        {
            if (FirstWallHit(new Ray(p, Vector3.up), out var hit))
            {
                if (hit.distance < creatureRadius + topMargin)
                {
                    rb.SafeDo(x =>
                    {
                        if (x.velocity.y > 0)
                            x.velocity = x.velocity.Flat();
                        x.AddForce(Vector3.down * push, ForceMode.VelocityChange);
                    });
                    p = hit.point - Vector3.up * (creatureRadius + topMargin + 0.05f);
                    rs = true;
                }
            }
            else
            {
                log.Warn($"No wall hit found upwards from {p}, cannot enforce top enclosure.");
                rs = true;
            }

            if (FirstWallHit(new Ray(p, Vector3.down), out hit))
            {
                if (hit.distance < creatureRadius + bottomMargin)
                {
                    rb.SafeDo(x =>
                    {
                        if (x.velocity.y < 0)
                            x.velocity = x.velocity.Flat();
                        x.AddForce(Vector3.up * push, ForceMode.VelocityChange);
                    });
                    p = hit.point + Vector3.up * (creatureRadius + bottomMargin + 0.05f);
                    rs = true;
                }
            }
            else
            {
                log.Warn($"No wall hit found downwards from {p}, cannot enforce bottom enclosure.");
                rs = true;
            }
        }
        return rs;
    }


    private static Dictionary<TechType, float> CreatureRadius { get; } = [];

    internal float GetItemWorldRadius(TechType key, GameObject go)
    {
        if (CreatureRadius.TryGetValue(key, out var radius))
            return radius * go.transform.localScale.x;
        using var log = AV.NewLazyAvsLog();
        radius = (BoundsUtil.ComputeScaledLocalBounds(
            go.transform,
            AV.Owner,
            includeRenderers: false,
            includeColliders: true,
            excludeFrom: null,
            applyLocalScale: false
            ).Size / 2).MaxAxis();
        CreatureRadius[key] = radius;
        log.Debug($"Computed world radius for {go.NiceName()} (tech type {key.AsString()}) as {radius.ToStr()}");
        return radius * go.transform.localScale.x;
    }

    internal void Reincarnate(WaterParkCreatureInhabitant creature, AssetReferenceGameObject adultPrefab, float infectedAmount, float enzymeAmount, Vector3 position)
    {
        using var log = AV.NewLazyAvsLog();
        log.Debug($"Reinstantiating {creature.WpCreature.NiceName()} as an adult.");

        AV.Owner.StartAvsCoroutine(nameof(MobileWaterPark) + '.' + nameof(BornAsync), log => BornAsync(log, adultPrefab, creature.WpCreature.transform.position, infectedAmount, enzymeAmount));
        _container!.RemoveItem(creature.Pickupable, forced: true);
        Destroy(creature.GameObject);
    }

    internal void AddChild(SmartLog log, AssetReferenceGameObject eggOrChildPrefab, Vector3 position)
    {
        if (FirstWallHit(new Ray(position, Vector3.down), out var hit))
            AV.Owner.StartAvsCoroutine(nameof(MobileWaterPark) + '.' + nameof(BornAsync), log => BornAsync(log, eggOrChildPrefab, hit.point + Vector3.up * bottomMargin, 0, 0));
        else
            AV.Owner.StartAvsCoroutine(nameof(MobileWaterPark) + '.' + nameof(BornAsync), log => BornAsync(log, eggOrChildPrefab, position + Vector3.down, 0, 0));
    }

}
