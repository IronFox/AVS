using AVS.VehicleTypes;
using UnityEngine;
//using AVS.Localization;

namespace AVS
{
    public class PilotingTrigger : HandTarget, IHandTarget, IScuttleListener, IDockListener
    {
        public ModVehicle? mv;
        public Transform? exit;
        private bool isLive = true;
        void IHandTarget.OnHandClick(GUIHand hand)
        {
            if (mv != null && !mv.GetPilotingMode() && mv.IsPowered() && isLive)
            {
                // TODO multiplayer?
                if (mv is Submarine sub)
                {
                    sub.thisStopPilotingLocation = exit;
                }
                mv.BeginPiloting();
            }
        }
        void IHandTarget.OnHandHover(GUIHand hand)
        {
            if (mv != null && !mv.GetPilotingMode() && mv.IsPowered() && isLive)
            {
                HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
                HandReticle.main.SetTextRaw(HandReticle.TextType.Hand, Language.main.Get("VFStartPiloting"));
            }
        }

        void IScuttleListener.OnScuttle()
        {
            isLive = false;
        }

        void IScuttleListener.OnUnscuttle()
        {
            isLive = true;
        }

        void IDockListener.OnDock()
        {
            isLive = false;
        }

        void IDockListener.OnUndock()
        {
            isLive = true;
        }
    }
}
