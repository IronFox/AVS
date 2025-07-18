using AVS.BaseVehicle;
using AVS.Util;
using AVS.VehicleTypes;
using UnityEngine;
//using AVS.Localization;

namespace AVS
{
    public class VehicleHatch : HandTarget, IHandTarget, IDockListener
    {
        private bool isLive = true;
        public AvsVehicle? mv;
        public int hatchIndex = 0; // Index of the hatch in the vehicle's list of hatches
        public string EnterHint => mv != null ? mv.VehicleName : Language.main.Get("AvsEnterVehicle");
        public string ExitHint = Language.main.Get("AvsExitVehicle");

        public void OnHandHover(GUIHand hand)
        {
            if (!isLive)
            {
                return;
            }
            HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
            if (mv is Submarine sub)
            {
                if (sub.IsPlayerInside())
                {
                    HandReticle.main.SetTextRaw(HandReticle.TextType.Hand, ExitHint);
                }
                else
                {
                    HandReticle.main.SetTextRaw(HandReticle.TextType.Hand, EnterHint);
                }
            }
            else if ((mv as Submersible != null) || (mv as Skimmer != null))
            {
                HandReticle.main.SetTextRaw(HandReticle.TextType.Hand, EnterHint);
            }
        }

        public void OnHandClick(GUIHand hand)
        {
            Logging.Default.Write($"VehicleHatch.OnHandClick: {mv?.NiceName()}");
            if (!isLive)
            {
                return;
            }
            Player.main.rigidBody.velocity = Vector3.zero;
            Player.main.rigidBody.angularVelocity = Vector3.zero;
            if (mv is Submarine sub)
            {
                if (hatchIndex < 0 || hatchIndex >= sub.Com.Hatches.Count)
                {
                    Logging.Default.Error($"Invalid hatch index {hatchIndex} for submarine {sub.VehicleName}");
                    return;
                }
                if (sub.IsPlayerInside())
                {
                    mv.PlayerExit(mv.Com.Hatches[hatchIndex], true);

                }
                else
                {
                    sub.PlayerEntry(mv.Com.Hatches[hatchIndex]);
                }
            }
            else if (mv is Submersible sub2 && !mv.isScuttled)
            {
                sub2.ClosestPlayerEntry();
            }
            Logging.Default.Write($"VehicleHatch.OnHandClick: end");

            /*
			if (mv as Walker != null)
			{
				Player.main.transform.position = (mv as Walker).PilotSeat.SitLocation.transform.position;
				Player.main.transform.rotation = (mv as Walker).PilotSeat.SitLocation.transform.rotation;
				mv.PlayerEntry();
			}
			if (mv as Skimmer != null)
			{
				Player.main.transform.position = (mv as Skimmer).PilotSeats.First().SitLocation.transform.position;
				Player.main.transform.rotation = (mv as Skimmer).PilotSeats.First().SitLocation.transform.rotation;
				mv.PlayerEntry();
			}
			*/
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
