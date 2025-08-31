namespace AVS.VehicleComponents.LightControllers
{
    internal class VolumetricLightController : AvAttached, IPlayerListener, ILightsStatusListener
    {


        protected virtual void Awake()
        {
            if (AV.volumetricLights.Count == 0)
            {
                DestroyImmediate(this);
            }
        }

        private void SetVolumetricLights(bool active)
        {
            AV.volumetricLights.ForEach(x => x.SetActive(active));
        }

        void IPlayerListener.OnPilotBegin()
        {
            return;
        }

        void IPlayerListener.OnPilotEnd()
        {
            return;
        }

        void IPlayerListener.OnPlayerEntry()
        {
            SetVolumetricLights(false);
        }

        void IPlayerListener.OnPlayerExit()
        {
            SetVolumetricLights(true);
        }

        void ILightsStatusListener.OnHeadlightsOn()
        {
            if (AV.IsBoarded)
            {
                SetVolumetricLights(false);
            }
        }

        void ILightsStatusListener.OnHeadlightsOff()
        {
        }

        void ILightsStatusListener.OnInteriorLightsOn()
        {
        }

        void ILightsStatusListener.OnInteriorLightsOff()
        {
        }

        void ILightsStatusListener.OnNavLightsOn()
        {
        }

        void ILightsStatusListener.OnNavLightsOff()
        {
        }

        void ILightsStatusListener.OnFloodlightsOn()
        {
            if (AV.IsBoarded)
            {
                SetVolumetricLights(false);
            }
        }

        void ILightsStatusListener.OnFloodlightsOff()
        {
        }
    }
}
