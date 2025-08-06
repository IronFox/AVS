using AVS.BaseVehicle;
using AVS.Localization;
using AVS.Log;
using AVS.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UWE;

namespace AVS.StorageComponents
{
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

        public void Awake()
        {
            Init();
        }

        private Dictionary<int, float> LastFishTankWarning { get; } = new Dictionary<int, float>();
        private bool CanWarnAbout(Pickupable pickupable)
        {
            if (!LastFishTankWarning.TryGetValue(pickupable.GetInstanceID(), out var lastWarning) || Time.time - lastWarning > 30f)
            {
                LastFishTankWarning[pickupable.GetInstanceID()] = Time.time;
                return true;
            }
            return false;
        }

        private bool IsLivingFishOrEgg(Pickupable pickupable, bool verbose)
        {
            if (pickupable == null || pickupable.gameObject == null)
            {
                return false;
            }
            var creature = pickupable.GetComponent<WaterParkCreature>();
            var live = pickupable.GetComponent<LiveMixin>();
            if (creature != null)
            {
                if (live != null && live.IsAlive())
                    return true;
                if (verbose && CanWarnAbout(pickupable))
                    ErrorMessage.AddMessage(Translator.GetFormatted(TranslationKey.Error_MobileWaterPark_CannotAdd_FishIsDead, pickupable.GetTechName()));
                return false;
            }
            var egg = pickupable.GetComponent<CreatureEgg>();
            if (egg != null)
            {
                if (live != null && live.IsAlive())
                    return true;
                //as it turns out, eggs die when hatched
                if (verbose && CanWarnAbout(pickupable))
                    ErrorMessage.AddMessage(Translator.GetFormatted(TranslationKey.Error_MobileWaterPark_CannotAdd_EggIsHatched, pickupable.GetTechName()));
                return false;
            }
            if (verbose && CanWarnAbout(pickupable))
                ErrorMessage.AddMessage(Translator.GetFormatted(TranslationKey.Error_MobileWaterPark_CannotAdd_Incompatible, pickupable.GetTechName()));
            return false;
        }


        private void Init()
        {
            if (_container != null)
            {
                return;
            }
            LogWriter.Default.Debug($"Initializing {this.NiceName()} for {DisplayName.Rendered} ({DisplayName.Localize}) with width {width} and height {height}");
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
                //    if (prefabId == null)
                //    {
                //        LogWriter.Default.Error($"Item {item.item.NiceName()} does not have a valid PrefabIdentifier, skipping.");
                //        return;
                //    }
                //    var live = item.item.GetComponent<LiveMixin>();
                //    var infect = item.item.GetComponent<InfectedMixin>();
                //    if (live == null || !live.IsAlive())
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
                //    //if (newInfect != null && infect != null)
                //    //{
                //    //    newInfect.infectedAmount = infect.infectedAmount;
                //    //}

                //    LogWriter.Default.Debug($"Adding item {embed.NiceName()} from {embed.transform.parent.NiceName()} to water park {waterPark.NiceName()}");
                //    embed.transform.SetParent(waterPark, false);
                //    var creature = embed.GetComponent<WaterParkCreature>();
                //    if (creature != null)
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
                //        if (egg != null)
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
        }

        private Vector3 RandomLocation(bool dropToFloor)
        {
            //return waterParkInstances!.position;
            var random = UnityEngine.Random.insideUnitSphere;
            var ray = random.normalized;
            //            LogWriter.Default.Debug($"Random ray for water park location: {random} => {ray}");
            bool haveLocation = false;
            RaycastHit hit = default;
            for (int i = 0; i < 100; i++)
            {
                if (haveLocation = Physics.Raycast(waterPark!.position, ray, out hit, 10f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
                    break;
            }
            //LogWriter.Default.Debug($"haveLocation: {haveLocation} => {ray}");

            var rs = haveLocation
                ? waterPark!.position + (hit.point - waterPark!.position) * random.magnitude * 0.9f
                : waterPark!.position + random;

            if (dropToFloor)
            {
                if (Physics.Raycast(rs, Vector3.down, out hit, 20f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
                    return hit.point + Vector3.up * 0.2f;
            }

            return rs;
        }

        private bool IsNotHatchingEgg(Pickupable pickupable, bool verbose)
        {
            //    if (pickupable == null || pickupable.gameObject == null)
            //    {
            //        return false;
            //    }
            //    var egg = pickupable.GetComponent<CreatureEgg>();
            //    if (egg != null)
            //    {
            //        if (canHatchEggs && egg.creaturePrefab != null && egg.creaturePrefab.RuntimeKeyIsValid())
            //        {
            //            if (CanWarnAbout(pickupable))
            //                ErrorMessage.AddMessage(Translator.GetFormatted(TranslationKey.Error_MobileWaterPark_CannotRemove_HatchingEgg, pickupable.GetTechName()));
            //            return false;
            //        }
            //        return true;
            //    }
            return true; //not an egg, so we can remove it

        }

        public void OnCraftEnd(TechType techType)
        {
            Init();
        }

        private void Reinit()
        {

            if (_container != null)
            {
                var items = _container.ToList();
                _container = null; //reset the container so it can be re-initialized
                Init();
                foreach (var item in items)
                    _container!.UnsafeAdd(item);
            }
            else
                Init();
        }

        private Transform GetOrCreateChild(GameObject parent, string childName)
        {
            var child = parent.transform.Find(childName).SafeGetGameObject();
            if (child == null)
            {
                child = new GameObject(childName);
                child.transform.SetParent(parent.transform);
                child.transform.localPosition = Vector3.zero;
                child.transform.localRotation = Quaternion.identity;
            }
            return child.transform;
        }

        internal void Setup(AvsVehicle vehicle, MaybeTranslate name, VehicleParts.MobileWaterPark vp, int index)
        {
            waterPark = vp.Container.transform;
            DisplayName = name;
            this.index = index;
            height = vp.Height;
            width = vp.Width;
            canHatchEggs = vp.HatchEggs;
            this.vehicle = vehicle;
            Reinit();
        }

        private class Serialized
        {
            public List<string>? items;
            public List<TechType>? techTypes;
            public int width;
            public int height;
            public bool canHatchEggs;
        }

        public void OnProtoSerializeObjectTree(ProtobufSerializer serializer)
        {
            if (_container == null)
            {
                LogWriter.Default.Error($"MobileWaterPark.OnProtoSerializeObjectTree called without a valid container.");
                return;
            }
            if (vehicle == null)
            {
                LogWriter.Default.Error($"MobileWaterPark.OnProtoSerializeObjectTree called without a valid vehicle or storageRoot.");
                return;
            }
            if (index <= 0)
            {
                LogWriter.Default.Error($"MobileWaterPark.OnProtoSerializeObjectTree called with invalid index {index}.");
                return;
            }
            vehicle.Log.Write($"Saving water park to file");
            var result = new List<(PrefabIdentifier, TechType)>();
            foreach (var item in _container.ToList())
            {
                var prefabId = item.item.GetComponent<PrefabIdentifier>();
                var tt = item.item.GetTechType();
                if (prefabId == null)
                {
                    vehicle.Log.Error($"Item {item.item.NiceName()} does not have a valid PrefabIdentifier, skipping.");
                    continue;
                }
                result.Add((prefabId, tt));
            }
            vehicle.PrefabID.WriteReflected(
                $"WP{index}",
                new Serialized
                {
                    items = result.Select(x => x.Item1.Id).ToList(),
                    techTypes = result.Select(x => x.Item2).ToList(),
                    width = width,
                    height = height,
                    canHatchEggs = canHatchEggs
                },
                vehicle.Log);

        }
        private List<(string, TechType, CoroutineTask<GameObject>?)> ReAddWhenDone { get; }
            = new List<(string, TechType, CoroutineTask<GameObject>?)>();
        public void OnProtoDeserializeObjectTree(ProtobufSerializer serializer)
        {
            if (vehicle == null)
            {
                LogWriter.Default.Error($"MobileWaterPark.OnProtoDeserializeObjectTree called without a valid vehicle.");
                return;
            }
            if (index <= 0)
            {
                LogWriter.Default.Error($"MobileWaterPark.OnProtoDeserializeObjectTree called with invalid index {index}.");
                return;
            }
            vehicle.Log.Write($"Loading water park from file");
            if (vehicle.PrefabID.ReadReflected($"WP{index}", out Serialized? data, vehicle.Log))
            {
                height = data.height;
                width = data.width;
                canHatchEggs = data.canHatchEggs;
                Reinit();

                var itemsToAdd = new List<(string, TechType, CoroutineTask<GameObject>?)>();
                if (data.items != null && data.techTypes != null && data.items.Count == data.techTypes.Count)
                {
                    for (int i = 0; i < data.items.Count; i++)
                    {
                        var load = CraftData.GetPrefabForTechTypeAsync(data.techTypes[i]);
                        CoroutineHost.StartCoroutine(load);
                        itemsToAdd.Add((data.items[i], data.techTypes[i], load));
                    }
                }
                else if (data.items != null && data.techTypes == null)
                {
                    foreach (var item in data.items)
                    {
                        itemsToAdd.Add((item, TechType.None, null));
                    }
                }
                else
                {
                    LogWriter.Default.Error($"Water park data for {index} is invalid, items or techTypes are null or have different counts.");
                }

                ReAddWhenDone.AddRange(itemsToAdd);
            }
            else
            {
                LogWriter.Default.Error($"Failed to read water park data for {index}");
            }
        }

        public void OnVehicleLoaded()
        {
            if (_container == null)
            {
                LogWriter.Default.Error($"MobileWaterPark.OnVehicleLoaded called without a valid container.");
                return;
            }

            if (vehicle == null)
            {
                LogWriter.Default.Error($"MobileWaterPark.OnVehicleLoaded called without a valid vehicle.");
                return;
            }

            if (waterPark == null)
            {
                LogWriter.Default.Error($"MobileWaterPark.OnVehicleLoaded called without a valid water park transform.");
                return;
            }

            vehicle.Log.Write($"Re-adding {ReAddWhenDone.Count} items to the water park");
            foreach (var item in ReAddWhenDone)
            {
                var thisItem = waterPark.Find(item.Item1);
                if (thisItem == null)
                    thisItem = waterPark.GetComponentsInChildren<PrefabIdentifier>()
                        .FirstOrDefault(x => x.Id == item.Item1).SafeGetTransform();
                if (thisItem == null)
                {
                    if (item.Item3 != null)
                    {
                        //try to load the item
                        var prefab = item.Item3.GetResult();
                        if (prefab == null)
                        {
                            vehicle.Log.Error($"Failed to load item {item.Item1} for water park {index}, skipping.");
                            continue;
                        }
                        vehicle.Log.Error($"Could not find real instance of {item.Item2}. Instantiating");
                        thisItem = (Utils.SpawnFromPrefab(prefab, null)).transform;
                    }
                    else
                    {
                        vehicle.Log.Error($"Failed to find item with ID {item} in the water park storage, no tech type registered, skipping.");
                        continue;
                    }
                }
                var pickupable = thisItem.GetComponent<Pickupable>();
                if (pickupable == null)
                {
                    vehicle.Log.Error($"Item {thisItem.NiceName()} does not have a Pickupable component, skipping.");
                    continue;
                }

                _container.AddItem(pickupable);
                vehicle.Log.Write($"Added item {thisItem.NiceName()} to the water park.");
            }
            ReAddWhenDone.Clear();
        }
    }
}
