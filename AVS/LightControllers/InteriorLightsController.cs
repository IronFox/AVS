using AVS.VehicleTypes;
using UnityEngine;

namespace AVS
{
    public class InteriorLightsController : BaseLightController, IPlayerListener
    {
        private Submarine MV => GetComponent<Submarine>();
        protected override void HandleLighting(bool active)
        {
            MV.Com.InteriorLights.ForEach(x => x.enabled = active);
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

        protected override void HandleSound(bool playSound)
        {
            return;
        }
        protected virtual void Awake()
        {
            if (MV.Com.InteriorLights.Count == 0)
            {
                Component.DestroyImmediate(this);
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
