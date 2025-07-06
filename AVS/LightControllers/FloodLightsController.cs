using AVS.Util;
using AVS.VehicleTypes;
using System.Linq;
using UnityEngine;

namespace AVS
{
    public class FloodLightsController : BaseLightController
    {
        private Submarine MV => GetComponent<Submarine>();
        protected override void HandleLighting(bool active)
        {
            MV.Com.FloodLights.ForEach(x => x.Light.SetActive(active));
            if (active)
            {
                MV.Com.FloodLights
                    .Select(x => x.Light.GetComponent<MeshRenderer>())
                    .Where(x => x != null)
                    .SelectMany(x => x.materials)
                    .ForEach(x => Shaders.EnableSimpleEmission(x, 10, 10));
            }
            else
            {
                MV.Com.FloodLights
                    .Select(x => x.Light.GetComponent<MeshRenderer>())
                    .Where(x => x != null)
                    .SelectMany(x => x.materials)
                    .ForEach(x => Shaders.EnableSimpleEmission(x, 0, 0));
            }
            foreach (var component in GetComponentsInChildren<ILightsStatusListener>())
            {
                if (active)
                {
                    component.OnFloodLightsOn();
                }
                else
                {
                    component.OnFloodLightsOff();
                }
            }
        }
        protected override void HandleSound(bool playSound)
        {
            if (playSound)
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
            if (MV.Com.FloodLights.Count == 0)
            {
                Component.DestroyImmediate(this);
            }
        }
    }
}
