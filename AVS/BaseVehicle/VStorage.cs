using AVS.Localization;
using AVS.Util;
using AVS.VehicleBuilding;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AVS.BaseVehicle;

public abstract partial class AvsVehicle
{

    /// <summary>
    /// Executed when the PDA storage is opened or closed.
    /// </summary>
    /// <param name="name">Name of the storage being opened or closed</param>
    /// <param name="open">True if the storage was opened, otherwise false</param>
    /// <returns>
    /// The number of seconds to wait before opening the PDF, to show off the cool animations
    /// </returns>
    public virtual float OnStorageOpen(string name, bool open) => 0;

    private GameObject GetOrCreateChild(string childName)
    {
        var child = transform.Find(childName).SafeGetGameObject();
        if (child.IsNull())
        {
            child = new GameObject(childName);
            child.transform.SetParent(transform);
        }

        return child;
    }

    /// <summary>
    /// Gets or creates a storage root object named "StorageRootObject".
    /// </summary>
    public GameObject GetOrCreateDefaultStorageRootObject()
        => GetOrCreateChild("StorageRootObject");

    /// <summary>
    /// Gets or creates a modules root object named "ModulesRootObject".
    /// </summary>
    public GameObject GetOrCreateDefaultModulesRootObject()
        => GetOrCreateChild("ModulesRootObject");

    /// <summary>
    /// Checks if the vehicle has room for the specified pickupable item in any of its storage containers.
    /// </summary>
    /// <param name="pickup">Pickupable to check</param>
    /// <returns>True if there is enough room, false if not</returns>
    public bool HasRoomFor(Pickupable pickup)
    {
        foreach (var container in Com.InnateStorages.Select(x =>
                     x.Container.GetComponent<InnateStorageContainer>().Container))
            if (container.IsNotNull() && container.HasRoomFor(pickup))
                return true;

        foreach (var container in ModularStorageInput.GetAllModularStorageContainers(this))
            if (container.HasRoomFor(pickup))
                return true;

        return false;
    }

    /// <summary>
    /// Checks if the vehicle has at least the given number of items of
    /// the specified TechType in any of its storage containers.
    /// </summary>
    /// <param name="techType">Tech type to look for</param>
    /// <param name="count">Number of items to require</param>
    /// <returns>True if there are at least the given amount of items of the given
    /// tech type in this vehicle's storages</returns>
    public bool HasInStorage(TechType techType, int count = 1)
    {
        foreach (var container in Com.InnateStorages.Select(x =>
                     x.Container.GetComponent<InnateStorageContainer>().Container))
            if (container.IsNotNull() && container.Contains(techType))
                if (container.GetCount(techType) >= count)
                    return true;

        foreach (var container in ModularStorageInput.GetAllModularStorageContainers(this))
            if (container.Contains(techType))
                if (container.GetCount(techType) >= count)
                    return true;

        return false;
    }

    /// <summary>
    /// Attempts to add a pickupable item to the vehicle's storage.
    /// </summary>
    /// <param name="pickup">The pickupable item to add.</param>
    /// <returns>True if the given pickupable could be placed in this vehicle's storages</returns>
    public bool AddToStorage(Pickupable pickup)
    {
        if (!HasRoomFor(pickup))
        {
            if (Player.main.GetVehicle() == this)
                ErrorMessage.AddMessage(Translator.Get(TranslationKey.Error_CannotAdd_StorageFull));
            return false;
        }

        foreach (var container in Com.InnateStorages.Select(x =>
                     x.Container.GetComponent<InnateStorageContainer>().Container))
            if (container.IsNotNull() && container.HasRoomFor(pickup))
            {
                var arg = Language.main.Get(pickup.GetTechName());
                ErrorMessage.AddMessage(Translator.GetFormatted(TranslationKey.Report_AddedToStorage, arg));
                uGUI_IconNotifier.main.Play(pickup.GetTechType(), uGUI_IconNotifier.AnimationType.From, null);
                pickup.Initialize();
                var item = new InventoryItem(pickup);
                container.UnsafeAdd(item);
                pickup.PlayPickupSound();
                return true;
            }

        foreach (var container in ModularStorageInput.GetAllModularStorageContainers(this))
            if (container.HasRoomFor(pickup))
            {
                var arg = Language.main.Get(pickup.GetTechName());
                ErrorMessage.AddMessage(Translator.GetFormatted(TranslationKey.Report_AddedToStorage, arg));
                uGUI_IconNotifier.main.Play(pickup.GetTechType(), uGUI_IconNotifier.AnimationType.From, null);
                pickup.Initialize();
                var item = new InventoryItem(pickup);
                container.UnsafeAdd(item);
                pickup.PlayPickupSound();
                return true;
            }

        return false;
    }

    /// <summary>
    /// Queries the currently stored and total capacity of the vehicle's storage.
    /// </summary>
    /// <param name="stored">Out storage capacity occupied</param>
    /// <param name="capacity">Out total storage capacity</param>
    public void GetStorageValues(out int stored, out int capacity)
    {
        var retStored = 0;
        var retCapacity = 0;

        int GetModularCapacity()
        {
            var ret = 0;
            var marty = ModularStorageInput.GetAllModularStorageContainers(this);
            marty.ForEach(x => ret += x.sizeX * x.sizeY);
            return ret;
        }

        int GetModularStored()
        {
            var ret = 0;
            var marty = ModularStorageInput.GetAllModularStorageContainers(this);
            marty.ForEach(x => x.ForEach(y => ret += y.width * y.height));
            return ret;
        }

        int GetInnateCapacity(VehicleStorage sto)
        {
            var container = sto.Container.GetComponent<InnateStorageContainer>();
            return container.Container.sizeX * container.Container.sizeY;
        }

        int GetInnateStored(VehicleStorage sto)
        {
            var ret = 0;
            var marty = (IEnumerable<InventoryItem>)sto.Container.GetComponent<InnateStorageContainer>().Container;
            marty.ForEach(x => ret += x.width * x.height);
            return ret;
        }

        Com.InnateStorages.ForEach(x => retCapacity += GetInnateCapacity(x));
        Com.InnateStorages.ForEach(x => retStored += GetInnateStored(x));

        if (Com.ModularStorages.IsNotNull())
        {
            retCapacity += GetModularCapacity();
            retStored += GetModularStored();
        }

        stored = retStored;
        capacity = retCapacity;
    }
}