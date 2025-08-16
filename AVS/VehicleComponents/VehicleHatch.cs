using AVS.BaseVehicle;
using AVS.Localization;
using AVS.Log;
using AVS.Util;
using AVS.VehicleTypes;
using UnityEngine;

//using AVS.Localization;

namespace AVS.VehicleComponents;

internal class VehicleHatch : HandTarget, IHandTarget, IDockListener
{
    public AvsVehicle? mv;
    public int hatchIndex; // Index of the hatch in the vehicle's list of hatches
    private bool isLive = true;
    public string EnterHint => Translator.GetFormatted(TranslationKey.HandHover_Vehicle_Enter, mv.GetVehicleName());
    public string ExitHint => Translator.GetFormatted(TranslationKey.HandHover_Vehicle_Exit, mv.GetVehicleName());


    void IDockListener.OnDock()
    {
        isLive = false;
    }

    void IDockListener.OnUndock()
    {
        isLive = true;
    }


    public void OnHandHover(GUIHand hand)
    {
        if (!isLive) return;
        HandReticle.main.SetIcon(HandReticle.IconType.Hand);
        if (mv is Submarine sub)
        {
            if (sub.IsPlayerInside())
                HandReticle.main.SetTextRaw(HandReticle.TextType.Hand, ExitHint);
            else
                HandReticle.main.SetTextRaw(HandReticle.TextType.Hand, EnterHint);
        }
        else if (mv as Submersible != null || mv as Skimmer != null)
        {
            HandReticle.main.SetTextRaw(HandReticle.TextType.Hand, EnterHint);
        }
    }

    public void OnHandClick(GUIHand hand)
    {
        LogWriter.Default.Write($"VehicleHatch.OnHandClick: {mv?.NiceName()}");
        if (!isLive) return;
        Player.main.rigidBody.velocity = Vector3.zero;
        Player.main.rigidBody.angularVelocity = Vector3.zero;
        if (mv is Submarine sub)
        {
            if (hatchIndex < 0 || hatchIndex >= sub.Com.Hatches.Count)
            {
                LogWriter.Default.Error($"Invalid hatch index {hatchIndex} for submarine {sub.VehicleName}");
                return;
            }

            if (sub.IsPlayerInside())
                mv.PlayerExit(mv.Com.Hatches[hatchIndex], true);
            else
                sub.PlayerEntry(mv.Com.Hatches[hatchIndex]);
        }
        else if (mv is Submersible sub2 && !mv.isScuttled)
        {
            sub2.ClosestPlayerEntry();
        }

        LogWriter.Default.Write("VehicleHatch.OnHandClick: end");

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
}