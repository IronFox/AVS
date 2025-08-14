using AVS.BaseVehicle;
using UnityEngine;

namespace AVS
{
    internal class VolumetricLightController : MonoBehaviour, IPlayerListener, ILightsStatusListener
    {
        private AvsVehicle MV => GetComponent<AvsVehicle>();
        protected virtual void Awake()
        {
            if (MV.volumetricLights.Count == 0)
            {
                Component.DestroyImmediate(this);
            }
        }

        private void SetVolumetricLights(bool active)
        {
            MV.volumetricLights.ForEach(x => x.SetActive(active));
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
            if (MV.IsBoarded)
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
            if (MV.IsBoarded)
            {
                SetVolumetricLights(false);
            }
        }

        void ILightsStatusListener.OnFloodlightsOff()
        {
        }
    }
}
