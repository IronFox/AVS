using AVS.VehicleTypes;
using UnityEngine;
//using AVS.Localization;

namespace AVS
{
    /// <summary>
    /// Hand target added to the pilot seat
    /// </summary>
    public class PilotingTrigger : HandTarget, IHandTarget, IScuttleListener, IDockListener
    {
        /// <summary>
        /// The owning vehicle. Assigned during instantiation
        /// </summary>
        public ModVehicle? mv;
        /// <summary>
        /// The index of the seat this trigger was attached to
        /// </summary>
        public int seatIndex;

        private bool isLive = true;
        void IHandTarget.OnHandClick(GUIHand hand)
        {
            if (mv != null
                && !mv.GetPilotingMode()
                && mv.IsPowered()
                && isLive)
            {
                if (mv is Submarine submarine)
                    submarine.EnterHelmControl(seatIndex);
                else
                    mv.BeginPiloting();
            }
        }
        void IHandTarget.OnHandHover(GUIHand hand)
        {
            if (mv != null
                && !mv.GetPilotingMode()
                && mv.IsPowered()
                && isLive)
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
