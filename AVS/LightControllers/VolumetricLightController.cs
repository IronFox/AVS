using AVS.BaseVehicle;
using UnityEngine;

namespace AVS
{
    public class VolumetricLightController : MonoBehaviour, IPlayerListener, ILightsStatusListener
    {
        private AvsVehicle MV => GetComponent<AvsVehicle>();
        protected virtual void Awake()
        {
            if (MV.VolumetricLights.Count == 0)
            {
                Component.DestroyImmediate(this);
            }
        }

        private void SetVolumetricLights(bool active)
        {
            MV.VolumetricLights.ForEach(x => x.SetActive(active));
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

        void ILightsStatusListener.OnHeadLightsOn()
        {
            if (MV.IsBoarded)
            {
                SetVolumetricLights(false);
            }
        }

        void ILightsStatusListener.OnHeadLightsOff()
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

        void ILightsStatusListener.OnFloodLightsOn()
        {
            if (MV.IsBoarded)
            {
                SetVolumetricLights(false);
            }
        }

        void ILightsStatusListener.OnFloodLightsOff()
        {
        }
    }
}
