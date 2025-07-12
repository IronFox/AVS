using AVS.VehicleTypes;
using System.Collections;
using UnityEngine;
//using AVS.Localization;

namespace AVS
{
    public class VehicleHatch : HandTarget, IHandTarget, IDockListener
    {
        private bool isLive = true;
        public ModVehicle? mv;
        public Transform? entryLocation;
        public Transform? exitLocation;
        public Transform? surfaceExitLocation;
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
            if (!isLive)
            {
                return;
            }
            Player.main.rigidBody.velocity = Vector3.zero;
            Player.main.rigidBody.angularVelocity = Vector3.zero;
            if (mv is Submarine sub)
            {
                if (sub.IsPlayerInside())
                {
                    mv.PlayerExit();
                    if (mv.transform.position.y < -3f)
                    {
                        if (exitLocation == null)
                        {
                            Logger.Error("Error: exitLocation is null. Cannot exit vehicle.");
                            return;
                        }
                        Player.main.transform.position = exitLocation.position;
                    }
                    else
                    {
                        StartCoroutine(ExitToSurface());
                    }
                }
                else
                {
                    if (entryLocation == null)
                    {
                        Logger.Error("Error: entryLocation is null. Cannot enter vehicle.");
                        return;
                    }


                    Player.main.transform.position = entryLocation.position;
                    sub.PlayerEntry();
                }
            }
            else if (mv is Submersible sub2 && !mv.isScuttled)
            {
                Player.main.transform.position = sub2.Com.PilotSeat.SitLocation.transform.position;
                Player.main.transform.rotation = sub2.Com.PilotSeat.SitLocation.transform.rotation;
                sub2.PlayerEntry();
            }
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

        public IEnumerator ExitToSurface()
        {
            int tryCount = 0;
            float playerHeightBefore = Player.main.transform.position.y;
            while (Player.main.transform.position.y < 2 + playerHeightBefore)
            {
                if (100 < tryCount)
                {
                    Logger.Error("Error: Failed to exit vehicle too many times. Stopping.");
                    yield break;
                }
                if (surfaceExitLocation == null)
                {
                    Logger.Error("Error: surfaceExitLocation is null. Cannot exit vehicle to surface.");
                    yield break;
                }
                Player.main.transform.position = surfaceExitLocation.position;
                tryCount++;
                yield return null;
            }
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
