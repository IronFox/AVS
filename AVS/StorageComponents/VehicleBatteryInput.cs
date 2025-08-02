//using AVS.Localization;
using AVS.BaseVehicle;
using AVS.Localization;
using UnityEngine;

namespace AVS
{
    public class VehicleBatteryInput : HandTarget, IHandTarget
    {
        public EnergyMixin? mixin;

        [SerializeField]
        internal TranslationKey translationKey = TranslationKey.HandOver_BatterySlot;

        public void OnHandHover(GUIHand hand)
        {
            HandReticle.main.SetTextRaw(HandReticle.TextType.Hand, Translator.Get(translationKey));
            HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
        }

        public void OnHandClick(GUIHand hand)
        {
            gameObject.GetComponentInParent<AvsVehicle>().OnAIBatteryReload();
            if (mixin != null)
                mixin.InitiateReload(); // this brings up the battery-changing gui
        }
    }
}
