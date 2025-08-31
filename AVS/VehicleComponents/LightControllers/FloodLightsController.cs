using AVS.Util;
using AVS.VehicleTypes;
using System.Linq;
using UnityEngine;

namespace AVS.VehicleComponents.LightControllers;

/// <summary>
/// Controller for the floodlights on a submarine.
/// </summary>
public class FloodlightsController : BaseLightController
{
    private Submarine Sub => (AV as Submarine).OrThrow($"Vehicle assigned to FloodlightsController is not a submarine");

    /// <inheritdoc/>
    protected override void HandleLighting(bool active)
    {
        Sub.Com.Floodlights.ForEach(x => x.Light.SetActive(active));
        if (active)
            Sub.Com.Floodlights
                .Select(x => x.Light.GetComponent<MeshRenderer>())
                .Where(x => x.IsNotNull())
                .SelectMany(x => x.materials)
                .ForEach(x => Shaders.EnableSimpleEmission(x, 10, 10));
        else
            Sub.Com.Floodlights
                .Select(x => x.Light.GetComponent<MeshRenderer>())
                .Where(x => x.IsNotNull())
                .SelectMany(x => x.materials)
                .ForEach(x => Shaders.EnableSimpleEmission(x, 0, 0));
        foreach (var component in GetComponentsInChildren<ILightsStatusListener>())
            if (active)
                component.OnFloodlightsOn();
            else
                component.OnFloodlightsOff();
    }

    /// <inheritdoc/>
    protected override void HandleSound(bool playSound)
    {
        if (playSound)
        {
            Sub.LightsOnSound.Stop();
            Sub.LightsOnSound.Play();
        }
        else
        {
            Sub.LightsOffSound.Stop();
            Sub.LightsOffSound.Play();
        }
    }

    /// <inheritdoc/>
    protected virtual void Awake()
    {
        if (Sub.Com.Floodlights.Count == 0)
            DestroyImmediate(this);
    }
}