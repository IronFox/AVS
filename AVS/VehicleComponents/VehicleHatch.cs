using AVS.BaseVehicle;
using AVS.Localization;
using AVS.Util;
using AVS.VehicleTypes;
using UnityEngine;

//using AVS.Localization;

namespace AVS.VehicleComponents;

internal class VehicleHatch : HandTarget, IHandTarget, IDockListener
{
    public AvsVehicle? av;
    public int hatchIndex; // Index of the hatch in the vehicle's list of hatches
    private bool isLive = true;
    public string EnterHint => Translator.GetFormatted(TranslationKey.HandHover_Vehicle_Enter, av.GetVehicleName());
    public string ExitHint => Translator.GetFormatted(TranslationKey.HandHover_Vehicle_Exit, av.GetVehicleName());


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
        if (av is Submarine sub)
        {
            if (sub.IsPlayerInside())
                HandReticle.main.SetTextRaw(HandReticle.TextType.Hand, ExitHint);
            else
                HandReticle.main.SetTextRaw(HandReticle.TextType.Hand, EnterHint);
        }
        else if (av is Submersible || av is Skimmer)
        {
            HandReticle.main.SetTextRaw(HandReticle.TextType.Hand, EnterHint);
        }
    }

    public void OnHandClick(GUIHand hand)
    {
        using var log = av.OrThrow($"VehicleHatch.av is not set at this point").NewAvsLog();
        log.Write($"VehicleHatch.OnHandClick: {av?.NiceName()}");
        if (!isLive)
        {
            log.Write("VehicleHatch is not live, ignoring click");
            return;
        }
        Player.main.rigidBody.velocity = Vector3.zero;
        Player.main.rigidBody.angularVelocity = Vector3.zero;
        if (av is Submarine sub)
        {
            if (hatchIndex < 0 || hatchIndex >= sub.Com.Hatches.Count)
            {
                log.Error($"Invalid hatch index {hatchIndex} for submarine {sub.VehicleName}");
                return;
            }

            if (sub.IsPlayerInside())
                av.PlayerExit(av.Com.Hatches[hatchIndex], true);
            else
                sub.PlayerEntry(av.Com.Hatches[hatchIndex]);
        }
        else if (av is Submersible sub2 && !av.isScuttled)
        {
            sub2.ClosestPlayerEntry();
        }


    }
}