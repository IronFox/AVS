using System;
using System.Collections;
using UnityEngine;

namespace AVS
{
    public class InnateStorageContainer : MonoBehaviour, ICraftTarget//, IProtoEventListener, IProtoTreeEventListener
    {

        private ItemsContainer? _container = null;
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
            _container = new ItemsContainer(this.width, this.height,
                storageRoot!.transform, this.storageLabel, null);
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

        public string storageLabel = "StorageLabel";

        public int width = 6;
        public int height = 8;

        public TechType[] allowedTech = new TechType[0];

        [AssertNotNull]
        public ChildObjectIdentifier? storageRoot;

        public int version = 3;

        [NonSerialized]
        public byte[]? serializedStorage;
    }
}
