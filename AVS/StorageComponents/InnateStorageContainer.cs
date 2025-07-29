using AVS.Localization;
using AVS.Log;
using AVS.Util;
using System;
using System.Collections;
using UnityEngine;

namespace AVS
{
    /// <summary>
    /// Innate storage container for vehicles.
    /// Component attached to gameobjects declared as innate storage containers in the vehicle's composition.
    /// </summary>
    public class InnateStorageContainer : MonoBehaviour, ICraftTarget//, IProtoEventListener, IProtoTreeEventListener
    {

        private ItemsContainer? _container = null;
        /// <summary>
        /// Accessor for the storage container.
        /// Throws an exception if called before Awake() or OnCraftEnd() were called.
        /// </summary>
        public ItemsContainer Container =>
            _container ?? throw new InvalidOperationException(
                    "Trying to access InnateStorageContainer.Container before either Awake() or OnCraftEnd() were called");

        public void Awake()
        {
            this.Init();
        }

        private void Init()
        {
            if (_container != null)
            {
                return;
            }
            LogWriter.Default.Debug($"Initializing {this.NiceName()} for {this.DisplayName.Rendered} ({this.DisplayName.Localize}) with width {this.width} and height {this.height}");
            _container = new ItemsContainer(this.width, this.height,
                storageRoot!.transform, this.DisplayName.Rendered, null);
            _container.SetAllowedTechTypes(this.allowedTech);
            _container.isAllowedToRemove = null;
        }

        public void OnCraftEnd(TechType techType)
        {
            // NEWNEW
            IEnumerator GetAndSetTorpedoSlots()
            {
                if (techType == TechType.SeamothTorpedoModule || techType == TechType.ExosuitTorpedoArmModule)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        TaskResult<GameObject> result = new TaskResult<GameObject>();
                        yield return CraftData.InstantiateFromPrefabAsync(techType, result, false);
                        GameObject gameObject = result.Get();
                        if (gameObject != null)
                        {
                            Pickupable pickupable = gameObject.GetComponent<Pickupable>();
                            if (pickupable != null)
                            {
                                // NEWNEW
                                // Why did we use to have this line?
                                //pickupable = pickupable.Pickup(false);
                                if (Container.AddItem(pickupable) == null)
                                {
                                    UnityEngine.Object.Destroy(pickupable.gameObject);
                                }
                            }
                        }
                    }
                }
                yield break;
            }
            this.Init();
            StartCoroutine(GetAndSetTorpedoSlots());
        }

        /// <summary>
        /// The display name of the storage container.
        /// Must be reapplied on vehicle awake or it will reset to the default value.
        /// </summary>
        public MaybeTranslate DisplayName { get; set; } = default;

        /// <summary>
        /// Storage container width.
        /// </summary>
        public int width = 6;
        /// <summary>
        /// Storage container height.
        /// </summary>
        public int height = 8;

        /// <summary>
        /// Tech that may be stored in this container.
        /// If empty, all tech is allowed.
        /// </summary>
        public TechType[] allowedTech = Array.Empty<TechType>();

        [AssertNotNull]
        public ChildObjectIdentifier? storageRoot;

        public int version = 3;

        [NonSerialized]
        public byte[]? serializedStorage;
    }
}
