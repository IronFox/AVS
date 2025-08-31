using AVS.Util;
using AVS.VehicleTypes;

namespace AVS.VehicleComponents.LightControllers
{
    /// <summary>
    /// The controller for the interior lights of a submarine.
    /// </summary>
    public class InteriorLightsController : BaseLightController, IPlayerListener
    {
        private Submarine Sub => (AV as Submarine).OrThrow($"Vehicle assigned to FloodlightsController is not a submarine");

        /// <inheritdoc/>
        protected override void HandleLighting(bool active)
        {
            Sub.Com.InteriorLights.ForEach(x => x.enabled = active);
            foreach (var component in GetComponentsInChildren<ILightsStatusListener>())
            {
                if (active)
                {
                    component.OnInteriorLightsOn();
                }
                else
                {
                    component.OnInteriorLightsOff();
                }
            }
        }

        /// <inheritdoc/>
        protected override void HandleSound(bool playSound)
        {
            return;
        }
        /// <inheritdoc/>
        protected virtual void Awake()
        {
            if (Sub.Com.InteriorLights.Count == 0)
            {
                DestroyImmediate(this);
            }
        }

        void IPlayerListener.OnPilotBegin()
        {
        }

        void IPlayerListener.OnPilotEnd()
        {
        }

        void IPlayerListener.OnPlayerEntry()
        {
            if (!IsLightsOn)
            {
                Toggle();
            }
        }

        void IPlayerListener.OnPlayerExit()
        {
            if (IsLightsOn)
            {
                Toggle();
            }
        }
    }
}
