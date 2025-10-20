//using AVS.Localization;

using AVS.BaseVehicle;
using AVS.Localization;
using AVS.Util;
using UnityEngine;

namespace AVS;

internal class VehicleBatteryInput : HandTarget, IHandTarget
{
    public EnergyMixin? mixin;
    public AvsVehicle? vehicle;

    [SerializeField] internal TranslationKey translationKey = TranslationKey.HandOver_BatterySlot;

    [SerializeField] internal bool displayNameLocalized;
    [SerializeField] internal string? displayName;
    [SerializeField] internal GameObject? powerCellObject;

    public void OnHandHover(GUIHand hand)
    {
        string text;
        if (!string.IsNullOrEmpty(displayName))
        {
            if (displayNameLocalized)
                text = DefaultTranslator.IntlTranslate(displayName);
            else
                text = displayName!;
        }
        else
        {
            text = Translator.Get(translationKey);
        }

        HandReticle.main.SetTextRaw(HandReticle.TextType.Hand, text);
        HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
    }


    public void OnHandClick(GUIHand hand)
    {
        gameObject.GetComponentInParent<AvsVehicle>().OnAIBatteryReload();
        if (mixin.IsNotNull())
            mixin.InitiateReload();
    }
}