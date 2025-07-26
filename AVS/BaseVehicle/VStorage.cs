using AVS.Localization;
using AVS.Util;
using AVS.VehicleParts;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AVS.BaseVehicle
{
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
        public virtual float OnStorageOpen(string name, bool open)
        {
            return 0;
        }

        private GameObject GetOrCreateChild(string childName)
        {
            var child = transform.Find(childName).SafeGetGameObject();
            if (child == null)
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

        public bool HasRoomFor(Pickupable pickup)
        {
            foreach (var container in Com.InnateStorages.Select(x => x.Container.GetComponent<InnateStorageContainer>().Container))
            {
                if (container != null && container.HasRoomFor(pickup))
                {
                    return true;
                }
            }
            foreach (var container in ModularStorageInput.GetAllModularStorageContainers(this))
            {
                if (container.HasRoomFor(pickup))
                {
                    return true;
                }
            }
            return false;
        }
        public bool HasInStorage(TechType techType, int count = 1)
        {
            foreach (var container in Com.InnateStorages.Select(x => x.Container.GetComponent<InnateStorageContainer>().Container))
            {
                if (container != null && container.Contains(techType))
                {
                    if (container.GetCount(techType) >= count)
                    {
                        return true;
                    }
                }
            }
            foreach (var container in ModularStorageInput.GetAllModularStorageContainers(this))
            {
                if (container.Contains(techType))
                {
                    if (container.GetCount(techType) >= count)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public bool AddToStorage(Pickupable pickup)
        {
            if (!HasRoomFor(pickup))
            {
                if (Player.main.GetVehicle() == this)
                {
                    ErrorMessage.AddMessage(Translator.Get(TranslationKey.Error_CannotAdd_StorageFull));
                }
                return false;
            }
            foreach (var container in Com.InnateStorages.Select(x => x.Container.GetComponent<InnateStorageContainer>().Container))
            {
                if (container != null && container.HasRoomFor(pickup))
                {
                    string arg = Language.main.Get(pickup.GetTechName());
                    ErrorMessage.AddMessage(Translator.GetFormatted(TranslationKey.Report_AddedToStorage, arg));
                    uGUI_IconNotifier.main.Play(pickup.GetTechType(), uGUI_IconNotifier.AnimationType.From, null);
                    pickup.Initialize();
                    InventoryItem item = new InventoryItem(pickup);
                    container.UnsafeAdd(item);
                    pickup.PlayPickupSound();
                    return true;
                }
            }
            foreach (var container in ModularStorageInput.GetAllModularStorageContainers(this))
            {
                if (container.HasRoomFor(pickup))
                {
                    string arg = Language.main.Get(pickup.GetTechName());
                    ErrorMessage.AddMessage(Translator.GetFormatted(TranslationKey.Report_AddedToStorage, arg));
                    uGUI_IconNotifier.main.Play(pickup.GetTechType(), uGUI_IconNotifier.AnimationType.From, null);
                    pickup.Initialize();
                    InventoryItem item = new InventoryItem(pickup);
                    container.UnsafeAdd(item);
                    pickup.PlayPickupSound();
                    return true;
                }
            }
            return false;
        }
        public void GetStorageValues(out int stored, out int capacity)
        {
            int retStored = 0;
            int retCapacity = 0;

            int GetModularCapacity()
            {
                int ret = 0;
                var marty = ModularStorageInput.GetAllModularStorageContainers(this);
                marty.ForEach(x => ret += x.sizeX * x.sizeY);
                return ret;
            }
            int GetModularStored()
            {
                int ret = 0;
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
                int ret = 0;
                var marty = (IEnumerable<InventoryItem>)sto.Container.GetComponent<InnateStorageContainer>().Container;
                marty.ForEach(x => ret += x.width * x.height);
                return ret;
            }

            Com.InnateStorages.ForEach(x => retCapacity += GetInnateCapacity(x));
            Com.InnateStorages.ForEach(x => retStored += GetInnateStored(x));

            if (Com.ModularStorages != null)
            {
                retCapacity += GetModularCapacity();
                retStored += GetModularStored();
            }
            stored = retStored;
            capacity = retCapacity;
        }
    }
}
