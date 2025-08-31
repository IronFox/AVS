using AVS.Localization;
using AVS.Util;
using AVS.VehicleTypes;
using UnityEngine;

//using AVS.Localization;

namespace AVS;

/// <summary>
/// Represents the control panel for a submarine, handling button initialization, lighting, and user interactions.
/// Implements listeners for vehicle status, power, lights, and autopilot events.
/// </summary>
public class ControlPanel : MonoBehaviour, IVehicleStatusListener, IPowerListener, ILightsStatusListener
{
    /// <summary>
    /// The submarine instance this control panel is associated with.
    /// </summary>
    public Submarine? av;

    private GameObject? buttonHeadlights;
    private GameObject? buttonNavLights;
    private GameObject? buttonAutopilot;
    private GameObject? buttonInteriorLights;
    private GameObject? button5;
    private GameObject? buttonDefaultColor;
    private GameObject? buttonFloodlights;
    private GameObject? button8;
    private GameObject? buttonPower;

    /// <summary>
    /// Initializes the control panel by finding and configuring all button GameObjects and their logic.
    /// </summary>
    public void Init()
    {
        // find buttons
        buttonHeadlights = transform.Find("1").gameObject;
        buttonNavLights = transform.Find("2").gameObject;
        buttonAutopilot = transform.Find("3").gameObject;
        buttonInteriorLights = transform.Find("4").gameObject;
        button5 = transform.Find("5").gameObject;
        buttonDefaultColor = transform.Find("6").gameObject;
        buttonFloodlights = transform.Find("7").gameObject;
        button8 = transform.Find("8").gameObject;
        buttonPower = transform.Find("9").gameObject;

        // give buttons their colliders, for touching
        buttonHeadlights.EnsureComponent<BoxCollider>();
        buttonNavLights.EnsureComponent<BoxCollider>();
        buttonAutopilot.EnsureComponent<BoxCollider>();
        buttonInteriorLights.EnsureComponent<BoxCollider>();
        button5.EnsureComponent<BoxCollider>();
        buttonDefaultColor.EnsureComponent<BoxCollider>();
        buttonFloodlights.EnsureComponent<BoxCollider>();
        button8.EnsureComponent<BoxCollider>();
        buttonPower.EnsureComponent<BoxCollider>();

        // give buttons their logic, for executing
        buttonHeadlights.EnsureComponent<ControlPanelButton>().Init(HeadlightsClick, HeadlightsHover);
        buttonNavLights.EnsureComponent<ControlPanelButton>().Init(NavLightsClick, NavLightsHover);
        buttonAutopilot.EnsureComponent<ControlPanelButton>().Init(AutopilotClick, AutopilotHover);
        buttonInteriorLights.EnsureComponent<ControlPanelButton>().Init(InteriorLightsClick, InteriorLightsHover);
        button5.EnsureComponent<ControlPanelButton>().Init(EmptyClick, EmptyHover);
        buttonDefaultColor.EnsureComponent<ControlPanelButton>().Init(DefaultColorClick, DefaultColorHover);
        buttonFloodlights.EnsureComponent<ControlPanelButton>().Init(FloodlightsClick, FloodlightsHover);
        button8.EnsureComponent<ControlPanelButton>().Init(EmptyClick, EmptyHover);
        buttonPower.EnsureComponent<ControlPanelButton>().Init(PowerClick, PowerHover);

        ResetAllButtonLighting();
    }

    /// <summary>
    /// Resets all button lighting to their default states.
    /// </summary>
    private void ResetAllButtonLighting()
    {
        SetButtonLightingActive(buttonHeadlights, false);
        SetButtonLightingActive(buttonNavLights, false);
        SetButtonLightingActive(buttonAutopilot, false);
        SetButtonLightingActive(buttonInteriorLights, true);
        SetButtonLightingActive(button5, false);
        SetButtonLightingActive(buttonDefaultColor, false);
        SetButtonLightingActive(buttonFloodlights, false);
        SetButtonLightingActive(button8, false);
        SetButtonLightingActive(buttonPower, true);
    }

    /// <summary>
    /// Adjusts all button lighting for a power down state (all off).
    /// </summary>
    private void AdjustButtonLightingForPowerDown()
    {
        SetButtonLightingActive(buttonHeadlights, false);
        SetButtonLightingActive(buttonNavLights, false);
        SetButtonLightingActive(buttonAutopilot, false);
        SetButtonLightingActive(buttonInteriorLights, false);
        SetButtonLightingActive(button5, false);
        SetButtonLightingActive(buttonDefaultColor, false);
        SetButtonLightingActive(buttonFloodlights, false);
        SetButtonLightingActive(button8, false);
        SetButtonLightingActive(buttonPower, false);
    }

    /// <summary>
    /// No-op click handler for unused buttons.
    /// </summary>
    public void EmptyClick()
    {
    }

    /// <summary>
    /// Hover handler for unused buttons, sets the hand reticle text and icon.
    /// </summary>
    public void EmptyHover()
    {
        HandReticle.main.SetTextRaw(HandReticle.TextType.Hand,
            Translator.Get(TranslationKey.HandHover_ControlPanel_Empty));
        HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
    }

    /// <summary>
    /// Click handler for the headlights button, toggles the headlights.
    /// </summary>
    public void HeadlightsClick()
    {
        if (av.IsNotNull() && av.HeadlightsController.IsNotNull())
            av.HeadlightsController.Toggle();
    }

    /// <summary>
    /// Hover handler for the headlights button, sets the hand reticle text and icon.
    /// </summary>
    public void HeadlightsHover()
    {
        HandReticle.main.SetTextRaw(HandReticle.TextType.Hand,
            Translator.Get(TranslationKey.HandHover_ControlPanel_Headlights));
        HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
    }

    /// <summary>
    /// Click handler for the floodlights button, toggles the floodlights.
    /// </summary>
    public void FloodlightsClick()
    {
        if (av.IsNotNull() && av.Floodlights.IsNotNull())
            av.Floodlights.Toggle();
    }

    /// <summary>
    /// Hover handler for the floodlights button, sets the hand reticle text and icon.
    /// </summary>
    public void FloodlightsHover()
    {
        HandReticle.main.SetTextRaw(HandReticle.TextType.Hand,
            Translator.Get(TranslationKey.HandHover_ControlPanel_Floodlights));
        HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
    }

    /// <summary>
    /// Click handler for the navigation lights button, toggles the navigation lights.
    /// </summary>
    public void NavLightsClick()
    {
        if (av.IsNotNull() && av.NavLights.IsNotNull())
            av.NavLights.Toggle();
    }

    /// <summary>
    /// Hover handler for the navigation lights button, sets the hand reticle text and icon.
    /// </summary>
    public void NavLightsHover()
    {
        HandReticle.main.SetTextRaw(HandReticle.TextType.Hand,
            Translator.Get(TranslationKey.HandHover_ControlPanel_NavLights));
        HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
    }

    /// <summary>
    /// Click handler for the interior lights button, toggles the interior lights.
    /// </summary>
    public void InteriorLightsClick()
    {
        if (av.IsNotNull() && av.Interiorlights.IsNotNull())
            av.Interiorlights.Toggle();
    }

    /// <summary>
    /// Hover handler for the interior lights button, sets the hand reticle text and icon.
    /// </summary>
    public void InteriorLightsHover()
    {
        HandReticle.main.SetTextRaw(HandReticle.TextType.Hand,
            Translator.Get(TranslationKey.HandHover_ControlPanel_InteriorLights));
        HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
    }

    /// <summary>
    /// Click handler for the default color button, paints the vehicle with its default style.
    /// </summary>
    public void DefaultColorClick()
    {
        if (av.IsNotNull())
            av.PaintVehicleDefaultStyle(av.GetName());
    }

    /// <summary>
    /// Hover handler for the default color button, sets the hand reticle text and icon.
    /// </summary>
    public void DefaultColorHover()
    {
        HandReticle.main.SetTextRaw(HandReticle.TextType.Hand,
            Translator.Get(TranslationKey.HandHover_ControlPanel_DefaultColor));
        HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
    }

    /// <summary>
    /// Click handler for the power button, toggles vehicle power if there is charge.
    /// </summary>
    public void PowerClick()
    {
        if (av.IsNotNull())
        {
            av.energyInterface.GetValues(out var charge, out _);
            if (0 < charge)
                av.TogglePower();
        }
    }

    /// <summary>
    /// Hover handler for the power button, sets the hand reticle text and icon.
    /// </summary>
    public void PowerHover()
    {
        HandReticle.main.SetTextRaw(HandReticle.TextType.Hand,
            Translator.Get(TranslationKey.HandHover_ControlPanel_Power));
        HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
    }

    /// <summary>
    /// Click handler for the autopilot button. (Not implemented.)
    /// </summary>
    public void AutopilotClick()
    {
        // TODO
    }

    /// <summary>
    /// Hover handler for the autopilot button, sets the hand reticle text and icon.
    /// </summary>
    public void AutopilotHover()
    {
        HandReticle.main.SetTextRaw(HandReticle.TextType.Hand,
            Translator.Get(TranslationKey.HandHover_ControlPanel_Autopilot));
        HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
    }

    /// <summary>
    /// Sets the lighting state of a button, enabling or disabling emission and color.
    /// </summary>
    /// <param name="button">The button GameObject.</param>
    /// <param name="active">True to enable lighting, false to disable.</param>
    public void SetButtonLightingActive(GameObject? button, bool active)
    {
        if (button.IsNull())
        {
            Logger.Warn("Tried to set control-panel button active, but it was NULL");
            return;
        }

        if (active)
            foreach (var renderer in button.GetComponentsInChildren<Renderer>())
            foreach (var mat in renderer.materials)
            {
                mat.EnableKeyword(Shaders.EmissionKeyword);
                mat.SetFloat(Shaders.GlowField, 0.1f);
                mat.SetFloat(Shaders.GlowNightField, 0.1f);
                mat.SetFloat(Shaders.EmissionField, 0.01f);
                mat.SetFloat(Shaders.EmissionNightField, 0.01f);
                mat.SetColor(Shaders.GlowColorField, Color.red);
                mat.SetColor(Shaders.ColorField, Color.red);
            }
        else
            foreach (var renderer in button.GetComponentsInChildren<Renderer>())
            foreach (var mat in renderer.materials)
            {
                mat.DisableKeyword(Shaders.EmissionKeyword);
                mat.SetColor(Shaders.ColorField, Color.white);
            }
    }

    // ILightsStatusListener implementation

    /// <inheritdoc />
    void ILightsStatusListener.OnHeadlightsOn()
    {
        SetButtonLightingActive(buttonHeadlights, true);
    }

    /// <inheritdoc />
    void ILightsStatusListener.OnHeadlightsOff()
    {
        SetButtonLightingActive(buttonHeadlights, false);
    }

    /// <inheritdoc />
    void ILightsStatusListener.OnInteriorLightsOn()
    {
        SetButtonLightingActive(buttonInteriorLights, true);
    }

    /// <inheritdoc />
    void ILightsStatusListener.OnInteriorLightsOff()
    {
        SetButtonLightingActive(buttonInteriorLights, false);
    }

    // IVehicleStatusListener implementation

    /// <inheritdoc />
    void IVehicleStatusListener.OnTakeDamage()
    {
    }

    // IAutopilotListener implementation


    // ILightsStatusListener continued

    /// <inheritdoc />
    void ILightsStatusListener.OnFloodlightsOn()
    {
        SetButtonLightingActive(buttonFloodlights, true);
    }

    /// <inheritdoc />
    void ILightsStatusListener.OnFloodlightsOff()
    {
        SetButtonLightingActive(buttonFloodlights, false);
    }

    /// <inheritdoc />
    void ILightsStatusListener.OnNavLightsOn()
    {
        SetButtonLightingActive(buttonNavLights, true);
    }

    /// <inheritdoc />
    void ILightsStatusListener.OnNavLightsOff()
    {
        SetButtonLightingActive(buttonNavLights, false);
    }

    // IPowerListener implementation

    /// <inheritdoc />
    void IPowerListener.OnBatterySafe()
    {
    }

    /// <inheritdoc />
    void IPowerListener.OnBatteryLow()
    {
    }

    /// <inheritdoc />
    void IPowerListener.OnBatteryNearlyEmpty()
    {
    }

    /// <inheritdoc />
    void IPowerListener.OnBatteryDepleted()
    {
    }

    /// <inheritdoc />
    void IPowerListener.OnPowerUp()
    {
        ResetAllButtonLighting();
    }

    /// <inheritdoc />
    void IPowerListener.OnPowerDown()
    {
        AdjustButtonLightingForPowerDown();
    }

    /// <inheritdoc />
    void IPowerListener.OnBatteryDead()
    {
        AdjustButtonLightingForPowerDown();
        SetButtonLightingActive(buttonPower, false);
    }

    /// <inheritdoc />
    void IPowerListener.OnBatteryRevive()
    {
        ResetAllButtonLighting();
    }

    // IVehicleStatusListener continued

    /// <inheritdoc />
    void IVehicleStatusListener.OnNearbyLeviathan()
    {
        SetButtonLightingActive(buttonHeadlights, false);
        SetButtonLightingActive(buttonFloodlights, false);
        SetButtonLightingActive(buttonInteriorLights, false);
        SetButtonLightingActive(buttonNavLights, false);
    }
}