using AVS.BaseVehicle;
using AVS.Localization;
using AVS.Log;
using AVS.Util;
using AVS.VehicleTypes;

//using AVS.Localization;

namespace AVS;

/// <summary>
/// Hand target added to the pilot seat
/// </summary>
public class PilotingTrigger : HandTarget, IHandTarget, IScuttleListener, IDockListener
{
    /// <summary>
    /// The owning vehicle. Assigned during instantiation
    /// </summary>
    public AvsVehicle? av;

    /// <summary>
    /// The index of the seat this trigger was attached to
    /// </summary>
    public int helmIndex;

    private bool isLive = true;

    void IHandTarget.OnHandClick(GUIHand hand)
    {
        LogWriter.Default.Write($"PilotingTrigger.OnHandClick: {av?.NiceName()}");
        if (av.IsNotNull()
            && !av.GetPilotingMode()
            && (av.Config.CanEnterHelmWithoutPower || av.IsPowered())
            && isLive)
        {
            if (av is Submarine submarine)
                submarine.EnterHelmControl(helmIndex);
            else if (av is Submersible sub)
                sub.EnterHelmControl();
            else
                av.Log.Error($"Unsupported helm on vehicle {av.NiceName()} type {av.GetType()}");
        }

        LogWriter.Default.Write($"PilotingTrigger.OnHandClick end");
    }

    void IHandTarget.OnHandHover(GUIHand hand)
    {
        if (av.IsNotNull()
            && !av.GetPilotingMode()
            && (av.Config.CanEnterHelmWithoutPower || av.IsPowered())
            && isLive)
        {
            HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
            HandReticle.main.SetTextRaw(HandReticle.TextType.Hand,
                Translator.Get(TranslationKey.HandHover_Vehicle_StartHelmControl));
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