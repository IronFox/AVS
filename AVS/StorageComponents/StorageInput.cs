using AVS.BaseVehicle;
using AVS.Localization;
using UnityEngine;
//using AVS.Localization;

namespace AVS
{
    public abstract class StorageInput : HandTarget, IHandTarget
    {
        public AvsVehicle? mv;
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

        public MaybeTranslate displayName = Text.Untranslated("Storage");

        public abstract void OpenFromExternal();
        protected abstract void OpenPDA();



        public override void Awake()
        {
            base.Awake();
            this.tr = GetComponent<Transform>();
            this.UpdateColliderState();

            // go up in the transform heirarchy until we find the AvsVehicle
            Transform modVe = transform;
            while (modVe.gameObject.GetComponent<AvsVehicle>() == null)
            {
                modVe = modVe.parent;
            }
            mv = modVe.gameObject.GetComponent<AvsVehicle>();
            SetEnabled(true);
        }
        protected void OnDisable()
        {

        }
        protected void ChangeFlapState()
        {
            //Utils.PlayFMODAsset(open ? this.openSound : this.closeSound, base.transform, 1f);
            Utils.PlayFMODAsset(this.openSound, base.transform, 1f);
            OpenPDA();
        }
        protected void OnClosePDA(PDA? pda)
        {
            seq.Set(0, false, null);
            gameObject.GetComponentInParent<AvsVehicle>().OnStorageOpen(transform.name, false);
            Utils.PlayFMODAsset(this.closeSound, base.transform, 1f);
        }
        protected void UpdateColliderState()
        {
            if (this.collider != null)
            {
                this.collider.enabled = (this.state && this.dockType != Vehicle.DockType.Cyclops);
            }
        }
        public void SetEnabled(bool state)
        {
            if (this.state == state)
            {
                return;
            }
            this.state = state;
            this.UpdateColliderState();
            if (this.model != null)
            {
                this.model.SetActive(state);
            }
        }
        public void SetDocked(Vehicle.DockType dockType)
        {
            this.dockType = dockType;
            this.UpdateColliderState();
        }
        public void OnHandHover(GUIHand hand)
        {
            string nameDisplayed;
            var displayName = this.displayName.Rendered;

            nameDisplayed = Translator.GetFormatted(TranslationKey.HandHover_OpenStorage, displayName);

            HandReticle.main.SetTextRaw(HandReticle.TextType.Hand, nameDisplayed);
            HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
        }

        public Sequence seq = new Sequence();
        public void Update()
        {
            seq.Update();
        }
        public void OnHandClick(GUIHand hand)
        {
            float timeToWait = gameObject.GetComponentInParent<AvsVehicle>().OnStorageOpen(transform.name, true);
            seq.Set(timeToWait, true, new SequenceCallback(ChangeFlapState));
        }
    }
}
