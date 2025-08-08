using AVS.Util;
using AVS.VehicleTypes;
using System.Linq;
using UnityEngine;

namespace AVS
{
    /// <summary>
    /// Controller for the floodlights on a submarine.
    /// </summary>
    public class FloodlightsController : BaseLightController
    {
        private Submarine MV => GetComponent<Submarine>();
        /// <inheritdoc/>
        protected override void HandleLighting(bool active)
        {
            MV.Com.Floodlights.ForEach(x => x.Light.SetActive(active));
            if (active)
            {
                MV.Com.Floodlights
                    .Select(x => x.Light.GetComponent<MeshRenderer>())
                    .Where(x => x != null)
                    .SelectMany(x => x.materials)
                    .ForEach(x => Shaders.EnableSimpleEmission(x, 10, 10));
            }
            else
            {
                MV.Com.Floodlights
                    .Select(x => x.Light.GetComponent<MeshRenderer>())
                    .Where(x => x != null)
                    .SelectMany(x => x.materials)
                    .ForEach(x => Shaders.EnableSimpleEmission(x, 0, 0));
            }
            foreach (var component in GetComponentsInChildren<ILightsStatusListener>())
            {
                if (active)
                {
                    component.OnFloodlightsOn();
                }
                else
                {
                    component.OnFloodlightsOff();
                }
            }
        }
        /// <inheritdoc/>
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
        /// <inheritdoc/>
        protected virtual void Awake()
        {
            if (MV.Com.Floodlights.Count == 0)
            {
                Component.DestroyImmediate(this);
            }
        }
    }
}
