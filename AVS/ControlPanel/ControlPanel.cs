using AVS.Util;
using AVS.VehicleTypes;
using UnityEngine;
//using AVS.Localization;

namespace AVS
{
    /// <summary>
    /// Represents the control panel for a submarine, handling button initialization, lighting, and user interactions.
    /// Implements listeners for vehicle status, power, lights, and autopilot events.
    /// </summary>
    public class ControlPanel : MonoBehaviour, IVehicleStatusListener, IPowerListener, ILightsStatusListener, IAutoPilotListener
    {
        /// <summary>
        /// The submarine instance this control panel is associated with.
        /// </summary>
        public Submarine mv;

        private GameObject buttonHeadLights;
        private GameObject buttonNavLights;
        private GameObject buttonAutoPilot;
        private GameObject buttonInteriorLights;
        private GameObject button5;
        private GameObject buttonDefaultColor;
        private GameObject buttonFloodLights;
        private GameObject button8;
        private GameObject buttonPower;

        /// <summary>
        /// Initializes the control panel by finding and configuring all button GameObjects and their logic.
        /// </summary>
        public void Init()
        {
            // find buttons
            buttonHeadLights = transform.Find("1").gameObject;
            buttonNavLights = transform.Find("2").gameObject;
            buttonAutoPilot = transform.Find("3").gameObject;
            buttonInteriorLights = transform.Find("4").gameObject;
            button5 = transform.Find("5").gameObject;
            buttonDefaultColor = transform.Find("6").gameObject;
            buttonFloodLights = transform.Find("7").gameObject;
            button8 = transform.Find("8").gameObject;
            buttonPower = transform.Find("9").gameObject;

            // give buttons their colliders, for touching
            buttonHeadLights.EnsureComponent<BoxCollider>();
            buttonNavLights.EnsureComponent<BoxCollider>();
            buttonAutoPilot.EnsureComponent<BoxCollider>();
            buttonInteriorLights.EnsureComponent<BoxCollider>();
            button5.EnsureComponent<BoxCollider>();
            buttonDefaultColor.EnsureComponent<BoxCollider>();
            buttonFloodLights.EnsureComponent<BoxCollider>();
            button8.EnsureComponent<BoxCollider>();
            buttonPower.EnsureComponent<BoxCollider>();

            // give buttons their logic, for executing
            buttonHeadLights.EnsureComponent<ControlPanelButton>().Init(HeadlightsClick, HeadLightsHover);
            buttonNavLights.EnsureComponent<ControlPanelButton>().Init(NavLightsClick, NavLightsHover);
            buttonAutoPilot.EnsureComponent<ControlPanelButton>().Init(AutoPilotClick, AutoPilotHover);
            buttonInteriorLights.EnsureComponent<ControlPanelButton>().Init(InteriorLightsClick, InteriorLightsHover);
            button5.EnsureComponent<ControlPanelButton>().Init(EmptyClick, EmptyHover);
            buttonDefaultColor.EnsureComponent<ControlPanelButton>().Init(DefaultColorClick, DefaultColorHover);
            buttonFloodLights.EnsureComponent<ControlPanelButton>().Init(FloodLightsClick, FloodLightsHover);
            button8.EnsureComponent<ControlPanelButton>().Init(EmptyClick, EmptyHover);
            buttonPower.EnsureComponent<ControlPanelButton>().Init(PowerClick, PowerHover);

            ResetAllButtonLighting();
        }

        /// <summary>
        /// Resets all button lighting to their default states.
        /// </summary>
        private void ResetAllButtonLighting()
        {
            SetButtonLightingActive(buttonHeadLights, false);
            SetButtonLightingActive(buttonNavLights, false);
            SetButtonLightingActive(buttonAutoPilot, false);
            SetButtonLightingActive(buttonInteriorLights, true);
            SetButtonLightingActive(button5, false);
            SetButtonLightingActive(buttonDefaultColor, false);
            SetButtonLightingActive(buttonFloodLights, false);
            SetButtonLightingActive(button8, false);
            SetButtonLightingActive(buttonPower, true);
        }

        /// <summary>
        /// Adjusts all button lighting for a power down state (all off).
        /// </summary>
        private void AdjustButtonLightingForPowerDown()
        {
            SetButtonLightingActive(buttonHeadLights, false);
            SetButtonLightingActive(buttonNavLights, false);
            SetButtonLightingActive(buttonAutoPilot, false);
            SetButtonLightingActive(buttonInteriorLights, false);
            SetButtonLightingActive(button5, false);
            SetButtonLightingActive(buttonDefaultColor, false);
            SetButtonLightingActive(buttonFloodLights, false);
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
            HandReticle.main.SetTextRaw(HandReticle.TextType.Hand, Language.main.Get("VFEmptyHover"));
            HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
        }

        /// <summary>
        /// Click handler for the headlights button, toggles the headlights.
        /// </summary>
        public void HeadlightsClick()
        {
            if (mv.headlights != null)
            {
                mv.headlights.Toggle();
            }
        }

        /// <summary>
        /// Hover handler for the headlights button, sets the hand reticle text and icon.
        /// </summary>
        public void HeadLightsHover()
        {
            HandReticle.main.SetTextRaw(HandReticle.TextType.Hand, Language.main.Get("VFHeadLightsHover"));
            HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
        }

        /// <summary>
        /// Click handler for the floodlights button, toggles the floodlights.
        /// </summary>
        public void FloodLightsClick()
        {
            if (mv.floodlights != null)
            {
                mv.floodlights.Toggle();
            }
        }

        /// <summary>
        /// Hover handler for the floodlights button, sets the hand reticle text and icon.
        /// </summary>
        public void FloodLightsHover()
        {
            HandReticle.main.SetTextRaw(HandReticle.TextType.Hand, Language.main.Get("VFFloodLightsHover"));
            HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
        }

        /// <summary>
        /// Click handler for the navigation lights button, toggles the navigation lights.
        /// </summary>
        public void NavLightsClick()
        {
            if (mv.navlights != null)
            {
                mv.navlights.Toggle();
            }
        }

        /// <summary>
        /// Hover handler for the navigation lights button, sets the hand reticle text and icon.
        /// </summary>
        public void NavLightsHover()
        {
            HandReticle.main.SetTextRaw(HandReticle.TextType.Hand, Language.main.Get("VFNavLightsHover"));
            HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
        }

        /// <summary>
        /// Click handler for the interior lights button, toggles the interior lights.
        /// </summary>
        public void InteriorLightsClick()
        {
            if (mv.interiorlights != null)
            {
                mv.interiorlights.Toggle();
            }
        }

        /// <summary>
        /// Hover handler for the interior lights button, sets the hand reticle text and icon.
        /// </summary>
        public void InteriorLightsHover()
        {
            HandReticle.main.SetTextRaw(HandReticle.TextType.Hand, Language.main.Get("VFInteriorLightsHover"));
            HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
        }

        /// <summary>
        /// Click handler for the default color button, paints the vehicle with its default style.
        /// </summary>
        public void DefaultColorClick()
        {
            mv.PaintVehicleDefaultStyle(mv.GetName());
        }

        /// <summary>
        /// Hover handler for the default color button, sets the hand reticle text and icon.
        /// </summary>
        public void DefaultColorHover()
        {
            HandReticle.main.SetTextRaw(HandReticle.TextType.Hand, Language.main.Get("VFDefaultColorHover"));
            HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
        }

        /// <summary>
        /// Click handler for the power button, toggles vehicle power if there is charge.
        /// </summary>
        public void PowerClick()
        {
            mv.energyInterface.GetValues(out float charge, out _);
            if (0 < charge)
            {
                mv.TogglePower();
            }
        }

        /// <summary>
        /// Hover handler for the power button, sets the hand reticle text and icon.
        /// </summary>
        public void PowerHover()
        {
            HandReticle.main.SetTextRaw(HandReticle.TextType.Hand, Language.main.Get("VFPowerHover"));
            HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
        }

        /// <summary>
        /// Click handler for the autopilot button. (Not implemented.)
        /// </summary>
        public void AutoPilotClick()
        {
            // TODO
        }

        /// <summary>
        /// Hover handler for the autopilot button, sets the hand reticle text and icon.
        /// </summary>
        public void AutoPilotHover()
        {
            HandReticle.main.SetTextRaw(HandReticle.TextType.Hand, Language.main.Get("VFAutoPilotHover"));
            HandReticle.main.SetIcon(HandReticle.IconType.Hand, 1f);
        }

        /// <summary>
        /// Sets the lighting state of a button, enabling or disabling emission and color.
        /// </summary>
        /// <param name="button">The button GameObject.</param>
        /// <param name="active">True to enable lighting, false to disable.</param>
        public void SetButtonLightingActive(GameObject button, bool active)
        {
            if (button == null)
            {
                Logger.Warn("Tried to set control-panel button active, but it was NULL");
                return;
            }
            if (active)
            {
                foreach (var renderer in button.GetComponentsInChildren<Renderer>())
                {
                    foreach (Material mat in renderer.materials)
                    {
                        mat.EnableKeyword(Shaders.EmissionKeyword);
                        mat.SetFloat(Shaders.GlowField, 0.1f);
                        mat.SetFloat(Shaders.GlowNightField, 0.1f);
                        mat.SetFloat(Shaders.EmissionField, 0.01f);
                        mat.SetFloat(Shaders.EmissionNightField, 0.01f);
                        mat.SetColor(Shaders.GlowColorField, Color.red);
                        mat.SetColor(Shaders.ColorField, Color.red);
                    }
                }
            }
            else
            {
                foreach (var renderer in button.GetComponentsInChildren<Renderer>())
                {
                    foreach (Material mat in renderer.materials)
                    {
                        mat.DisableKeyword(Shaders.EmissionKeyword);
                        mat.SetColor(Shaders.ColorField, Color.white);
                    }
                }
            }
        }

        // ILightsStatusListener implementation

        /// <inheritdoc />
        void ILightsStatusListener.OnHeadLightsOn()
        {
            SetButtonLightingActive(buttonHeadLights, true);
        }

        /// <inheritdoc />
        void ILightsStatusListener.OnHeadLightsOff()
        {
            SetButtonLightingActive(buttonHeadLights, false);
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

        // IAutoPilotListener implementation

        /// <inheritdoc />
        void IAutoPilotListener.OnAutoLevelBegin()
        {
        }

        /// <inheritdoc />
        void IAutoPilotListener.OnAutoLevelEnd()
        {
        }

        /// <inheritdoc />
        void IAutoPilotListener.OnAutoPilotBegin()
        {
            SetButtonLightingActive(buttonAutoPilot, true);
        }

        /// <inheritdoc />
        void IAutoPilotListener.OnAutoPilotEnd()
        {
            SetButtonLightingActive(buttonAutoPilot, false);
        }

        // ILightsStatusListener continued

        /// <inheritdoc />
        void ILightsStatusListener.OnFloodLightsOn()
        {
            SetButtonLightingActive(buttonFloodLights, true);
        }

        /// <inheritdoc />
        void ILightsStatusListener.OnFloodLightsOff()
        {
            SetButtonLightingActive(buttonFloodLights, false);
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
            SetButtonLightingActive(buttonHeadLights, false);
            SetButtonLightingActive(buttonFloodLights, false);
            SetButtonLightingActive(buttonInteriorLights, false);
            SetButtonLightingActive(buttonNavLights, false);
        }
    }
}
