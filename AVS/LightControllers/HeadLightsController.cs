using AVS.BaseVehicle;
using AVS.Log;
using AVS.Util;
using UnityEngine;

namespace AVS
{
    /// <summary>
    /// Controller for the headlights of a vehicle.
    /// </summary>
    public class HeadlightsController : BaseLightController
    {

        private AvsVehicle MV => GetComponent<AvsVehicle>();

        /// <inheritdoc/>
        protected override void HandleLighting(bool active)
        {
            MV.Com.Headlights.ForEach(x => x.Light.SetActive(active));
            foreach (var component in GetComponentsInChildren<ILightsStatusListener>())
            {
                if (active)
                {
                    component.OnHeadlightsOn();
                }
                else
                {
                    component.OnHeadlightsOff();
                }
            }
        }

        /// <inheritdoc/>
        protected override void HandleSound(bool turnOn)
        {
            if (turnOn)
            {
                MV.LightsOnSound.Stop();
                MV.LightsOnSound.Play();
            }
            else
            {
                MV.LightsOffSound.Stop();
                MV.LightsOffSound.Play();
            }
        }

        /// <inheritdoc/>
        protected virtual void Awake()
        {
            LogWriter.Default.Debug($"Awake {this.NiceName()} for {MV.NiceName()}: {MV.Com.Headlights.Count} head light(s)");
            if (MV.Com.Headlights.Count == 0)
            {
                LogWriter.Default.Write($"Destroying headlights controller because there are no lights to control");
                Component.DestroyImmediate(this);
            }
        }

        /// <inheritdoc/>
        protected virtual void Update()
        {
            var isHeadlightsButtonPressed = GameInput.GetButtonDown(GameInput.Button.RightHand);
            if (MV.IsPlayerControlling() && isHeadlightsButtonPressed && !Player.main.GetPDA().isInUse)
            {
                LogWriter.Default.Debug($"Toggling headlights for {MV.NiceName()}");
                Toggle();
            }
        }
    }
}
