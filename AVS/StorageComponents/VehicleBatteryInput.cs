﻿//using AVS.Localization;
using AVS.BaseVehicle;
using UnityEngine;

namespace AVS
{
    public class VehicleBatteryInput : HandTarget, IHandTarget
    {
        public EnergyMixin? mixin;

        // need this SerializeField attribute or else assignment in
        // VehicleBuilder is not propogated to instances
        [SerializeField]
        internal string? tooltip;

        public void OnHandHover(GUIHand hand)
        {
            HandReticle.main.SetTextRaw(HandReticle.TextType.Hand, Language.main.Get(tooltip));
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
