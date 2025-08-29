using AVS.BaseVehicle;
using AVS.Localization;
using AVS.Log;
using AVS.Util;
using System;
using System.Collections;
using UnityEngine;

namespace AVS;

/// <summary>
/// Innate storage container for vehicles.
/// Component attached to gameobjects declared as innate storage containers in the vehicle's composition.
/// </summary>
public class InnateStorageContainer : MonoBehaviour, ICraftTarget //, IProtoEventListener, IProtoTreeEventListener
{
    /// <summary>
    /// Custom delegate to check if an item is allowed to be added to the storage container.
    /// </summary>
    public IsAllowedToAdd? isAllowedToAdd = null;

    /// <summary>
    /// Indicates whether the current entity is allowed to be removed.
    /// </summary>
    public IsAllowedToRemove? isAllowedToRemove = null;


    private ItemsContainer? _container = null;

    /// <summary>
    /// Accessor for the storage container.
    /// Throws an exception if called before Awake() or OnCraftEnd() were called.
    /// </summary>
    public ItemsContainer Container =>
        _container ?? throw new InvalidOperationException(
            "Trying to access InnateStorageContainer.Container before either Awake() or OnCraftEnd() were called");

    /// <inheritdoc />
    public void Awake()
    {
        Init();
    }

    private void Init()
    {
        if (_container.IsNotNull()) return;
        using var log = SmartLog.ForAVS(av!.Owner, nameof(InnateStorageContainer));
        log.Debug(
            $"Initializing {this.NiceName()} for {DisplayName.Rendered} ({DisplayName.Localize}) with width {width} and height {height}");
        _container = new ItemsContainer(width, height,
            storageRoot!.transform, DisplayName.Rendered, null);
        _container.SetAllowedTechTypes(allowedTech);
        _container.isAllowedToAdd = isAllowedToAdd;
        _container.isAllowedToRemove = isAllowedToRemove;
    }

    /// <summary>
    /// Called via reflection when the vehicle is crafted.
    /// </summary>
    /// <param name="techType"></param>
    public void OnCraftEnd(TechType techType)
    {
        // NEWNEW
        IEnumerator GetAndSetTorpedoSlots(SmartLog log)
        {
            if (techType == TechType.SeamothTorpedoModule || techType == TechType.ExosuitTorpedoArmModule)
                for (var i = 0; i < 2; i++)
                {
                    var result = new InstanceContainer();
                    yield return AvsCraftData.InstantiateFromPrefabAsync(
                        log,
                        techType, result);
                    var gameObject = result.Instance;
                    if (gameObject.IsNotNull())
                    {
                        var pickupable = gameObject.GetComponent<Pickupable>();
                        if (pickupable.IsNotNull())
                            // NEWNEW
                            // Why did we use to have this line?
                            //pickupable = pickupable.Pickup(false);
                            if (Container.AddItem(pickupable) is null)
                                Destroy(pickupable.gameObject);
                    }
                }

            yield break;
        }

        Init();
        av!.Owner.StartAvsCoroutine(
            nameof(InnateStorageContainer) + '.' + nameof(GetAndSetTorpedoSlots),
            GetAndSetTorpedoSlots);
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
    public TechType[] allowedTech = [];

    [AssertNotNull][SerializeField] internal ChildObjectIdentifier? storageRoot;

    [SerializeField]
    internal int version = 3;
    [SerializeField]
    internal AvsVehicle? av;
}