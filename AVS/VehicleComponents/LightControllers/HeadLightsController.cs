using AVS.Util;

namespace AVS.VehicleComponents.LightControllers
{
    /// <summary>
    /// Controller for the headlights of a vehicle.
    /// </summary>
    public class HeadlightsController : BaseLightController
    {

        /// <inheritdoc/>
        protected override void HandleLighting(bool active)
        {
            AV.Com.Headlights.ForEach(x => x.Light.SetActive(active));
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
                AV.LightsOnSound.Stop();
                AV.LightsOnSound.Play();
            }
            else
            {
                AV.LightsOffSound.Stop();
                AV.LightsOffSound.Play();
            }
        }

        /// <inheritdoc/>
        protected virtual void Awake()
        {
            using var log = AV.NewAvsLog();
            log.Debug($"Awake {this.NiceName()} for {AV.NiceName()}: {AV.Com.Headlights.Count} head light(s)");
            if (AV.Com.Headlights.Count == 0)
            {
                log.Write($"Destroying headlights controller because there are no lights to control");
                DestroyImmediate(this);
            }
        }

        /// <inheritdoc/>
        protected virtual void Update()
        {
            var isHeadlightsButtonPressed = GameInput.GetButtonDown(GameInput.Button.RightHand);
            if (AV.IsPlayerControlling() && isHeadlightsButtonPressed && !Player.main.GetPDA().isInUse)
            {
                using var log = AV.NewAvsLog();
                log.Debug($"Toggling headlights for {AV.NiceName()}");
                Toggle();
            }
        }
    }
}
