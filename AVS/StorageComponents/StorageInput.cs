using AVS.BaseVehicle;
using AVS.Localization;
using AVS.Util;
using UnityEngine;

//using AVS.Localization;

namespace AVS.StorageComponents;

internal abstract class StorageInput : HandTarget, IHandTarget
{
    public AvsVehicle? av;
    public int slotID = -1;
    public GameObject? model;
    public Collider? collider;
    public float timeOpen = 0.5f;
    public float timeClose = 0.25f;
    public FMODAsset? openSound;
    public FMODAsset? closeSound;
    protected Transform? tr;
    protected Vehicle.DockType dockType;
    protected bool state;

    protected AvsVehicle AV => av.OrThrow("StorageInput does not have a vehicle attached. This should not happen.");

    public MaybeTranslate displayName = Text.Untranslated("Storage");

    public abstract void OpenFromExternal();
    protected abstract void OpenPDA();


    public override void Awake()
    {
        base.Awake();
        tr = GetComponent<Transform>();
        UpdateColliderState();

        // go up in the transform hierarchy until we find the AvsVehicle
        var modVe = transform;
        while (modVe.gameObject.GetComponent<AvsVehicle>().IsNull())
            modVe = modVe.parent;
        if (av.IsNull())
            av = modVe.gameObject.GetComponent<AvsVehicle>();
        SetEnabled(true);
    }

    protected void OnDisable()
    {
    }

    protected void ChangeFlapState()
    {
        //Utils.PlayFMODAsset(open ? this.openSound : this.closeSound, base.transform, 1f);
        Utils.PlayFMODAsset(openSound, transform, 1f);
        OpenPDA();
    }

    protected void OnClosePDA(PDA? pda)
    {
        seq.Set(0, false, null);
        gameObject.GetComponentInParent<AvsVehicle>().OnStorageOpen(transform.name, false);
        Utils.PlayFMODAsset(closeSound, transform, 1f);
    }

    protected void UpdateColliderState()
    {
        if (collider.IsNotNull())
            collider.enabled = state && dockType != Vehicle.DockType.Cyclops;
    }

    public void SetEnabled(bool state)
    {
        if (this.state == state)
            return;
        this.state = state;
        UpdateColliderState();
        if (model.IsNotNull())
            model.SetActive(state);
    }

    public void SetDocked(Vehicle.DockType dockType)
    {
        this.dockType = dockType;
        UpdateColliderState();
    }

    public void OnHandHover(GUIHand hand)
    {
        var displayName = this.displayName.Rendered;

        var nameDisplayed = Translator.GetFormatted(TranslationKey.HandHover_OpenStorage, displayName);

        HandReticle.main.SetTextRaw(HandReticle.TextType.Hand, nameDisplayed);
        HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
    }

    public Sequence seq = new();

    public void Update()
    {
        seq.Update();
    }

    public void OnHandClick(GUIHand hand)
    {
        var timeToWait = gameObject.GetComponentInParent<AvsVehicle>().OnStorageOpen(transform.name, true);
        seq.Set(timeToWait, true, new SequenceCallback(ChangeFlapState));
    }
}