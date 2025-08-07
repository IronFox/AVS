using AVS.BaseVehicle;
using AVS.Log;
using AVS.Util;
using UnityEngine;

namespace AVS
{
    public class HeadLightsController : BaseLightController
    {
        private bool hasWarned = false;
        public bool isHeadlightsOn // this is just here because the Beluga was using it
        {
            get
            {
                if (!hasWarned)
                {
                    Logger.Warn("Getting HeadLightsController.isHeadlightsOn (deprecated). Please instead Get HeadLightsController.IsLightsOn!");
                    hasWarned = true;
                }
                return IsLightsOn;
            }
        }
        private AvsVehicle MV => GetComponent<AvsVehicle>();
        protected override void HandleLighting(bool active)
        {
            MV.Com.HeadLights.ForEach(x => x.Light.SetActive(active));
            foreach (var component in GetComponentsInChildren<ILightsStatusListener>())
            {
                if (active)
                {
                    component.OnHeadLightsOn();
                }
                else
                {
                    component.OnHeadLightsOff();
                }
            }
        }

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

        protected virtual void Awake()
        {
            LogWriter.Default.Debug($"Awake {this.NiceName()} for {MV.NiceName()}: {MV.Com.HeadLights.Count} head light(s)");
            if (MV.Com.HeadLights.Count == 0)
            {
                LogWriter.Default.Write($"Destroying headlights controller because there are no lights to control");
                Component.DestroyImmediate(this);
            }
        }

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
