using AVS.Util;
using AVS.VehicleTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AVS.VehicleComponents.LightControllers;

/// <summary>
/// The controller for the navigation lights of a submarine.
/// </summary>
public class NavigationLightsController : BaseLightController
{
    private Submarine Sub => (AV as Submarine).OrThrow($"Vehicle assigned to FloodlightsController is not a submarine");


    /// <inheritdoc/>
    protected override void HandleLighting(bool active)
    {
        if (active)
        {
            EnableLightClass(LightClass.Positions);
            EnableLightClass(LightClass.Ports);
            EnableLightClass(LightClass.Starboards);
        }
        else
        {
            foreach (var lc in Enum.GetValues(typeof(LightClass)).Cast<LightClass>())
                DisableLightClass(lc);
        }

        foreach (var component in GetComponentsInChildren<ILightsStatusListener>())
            if (active)
                component.OnNavLightsOn();
            else
                component.OnNavLightsOff();
    }

    /// <inheritdoc/>
    protected override void HandleSound(bool playSound)
    {
        return;
    }

    /// <inheritdoc/>
    protected virtual void Awake()
    {
        var noPort = Sub.Com.NavigationPortLights.Count == 0;
        var noStar = Sub.Com.NavigationStarboardLights.Count == 0;
        var noPosi = Sub.Com.NavigationPositionLights.Count == 0;
        var noReds = Sub.Com.NavigationRedStrobeLights.Count == 0;
        var noWhit = Sub.Com.NavigationWhiteStrobeLights.Count == 0;
        if (noPort && noStar && noPosi && noReds && noWhit)
            DestroyImmediate(this);
    }

    /// <inheritdoc/>
    protected void Start()
    {
        rb = GetComponent<Rigidbody>();
        foreach (var lightObj in Sub.Com.NavigationPositionLights)
            positionMats.Add(lightObj.GetComponent<MeshRenderer>().material);
        BlinkOn(positionMats, Color.white);
        foreach (var lightObj in Sub.Com.NavigationRedStrobeLights)
        {
            redStrobeMats.Add(lightObj.GetComponent<MeshRenderer>().material);
            var light = lightObj.EnsureComponent<Light>();
            light.enabled = false;
            light.color = Color.red;
            light.type = LightType.Point;
            light.intensity = 1f;
            light.range = 80f;
            light.shadows = LightShadows.Hard;
            redStrobeLights.Add(light);
        }

        foreach (var lightObj in Sub.Com.NavigationWhiteStrobeLights)
        {
            whiteStrobeMats.Add(lightObj.GetComponent<MeshRenderer>().material);
            var light = lightObj.EnsureComponent<Light>();
            light.enabled = false;
            light.color = Color.white;
            light.type = LightType.Point;
            light.intensity = 0.5f;
            light.range = 80f;
            light.shadows = LightShadows.Hard;
            whiteStrobeLights.Add(light);
        }

        foreach (var lightObj in Sub.Com.NavigationPortLights)
            portMats.Add(lightObj.GetComponent<MeshRenderer>().material);

        foreach (var lightObj in Sub.Com.NavigationStarboardLights)
            starboardMats.Add(lightObj.GetComponent<MeshRenderer>().material);
        Sub.Owner.StartAvsCoroutine(
            nameof(NavigationLightsController) + '.' + nameof(ControlLights),
            _ => ControlLights());
    }

    private Rigidbody? rb = null;
    private bool position = false;
    private Coroutine? white = null;
    private Coroutine? red = null;
    private ICoroutineHandle? port = null;
    private ICoroutineHandle? starboard = null;
    internal const float lightBrightness = 1f;
    internal const float strobeBrightness = 30f;
    private List<Material> positionMats = new();
    private List<Material> portMats = new();
    private List<Material> starboardMats = new();
    private List<Material> whiteStrobeMats = new();
    private List<Material> redStrobeMats = new();
    private List<Light> whiteStrobeLights = new();
    private List<Light> redStrobeLights = new();

    private enum LightClass
    {
        WhiteStrobes,
        RedStrobes,
        Positions,
        Ports,
        Starboards
    }

    private void DisableLightClass(LightClass lc)
    {
        switch (lc)
        {
            case LightClass.WhiteStrobes:
                if (white.IsNotNull())
                {
                    StopCoroutine(white);
                    white = null;
                }

                KillStrobes(LightClass.WhiteStrobes);
                break;
            case LightClass.RedStrobes:
                if (red.IsNotNull())
                {
                    StopCoroutine(red);
                    red = null;
                }

                KillStrobes(LightClass.RedStrobes);
                break;
            case LightClass.Positions:
                if (position)
                {
                    BlinkOff(positionMats);
                    position = false;
                }

                break;
            case LightClass.Ports:
                if (port.IsNotNull())
                {
                    port.Stop();
                    port = null;
                }

                BlinkOff(portMats);
                break;
            case LightClass.Starboards:
                if (starboard.IsNotNull())
                {
                    starboard.Stop();
                    starboard = null;
                }

                BlinkOff(starboardMats);
                break;
        }
    }

    private void EnableLightClass(LightClass lc)
    {
        switch (lc)
        {
            case LightClass.WhiteStrobes:
                //if (MainPatcher.NautilusConfig.IsFlashingLights && white.IsNull())
                //{
                //    white = MainPatcher.Instance.StartCoroutine(Strobe(LightClass.WhiteStrobes));
                //}
                break;
            case LightClass.RedStrobes:
                //if (MainPatcher.NautilusConfig.IsFlashingLights && red.IsNull())
                //{
                //    red = MainPatcher.Instance.StartCoroutine(Strobe(LightClass.RedStrobes));
                //}
                break;
            case LightClass.Positions:
                if (!position)
                {
                    BlinkOn(positionMats, Color.white);
                    position = true;
                }

                break;
            case LightClass.Ports:
                if (port.IsNull())
                    port = Sub.Owner.StartAvsCoroutine(
                        nameof(NavigationLightsController) + '.' + nameof(BlinkNarySequence),
                        _ => BlinkNarySequence(2, true));
                break;
            case LightClass.Starboards:
                if (starboard.IsNull())
                    starboard = Sub.Owner.StartAvsCoroutine(
                        nameof(NavigationLightsController) + '.' + nameof(BlinkNarySequence),
                        _ => BlinkNarySequence(2, false));
                break;
        }
    }

    private void BlinkThisLightOn(Material mat, Color col)
    {
        mat.EnableKeyword(Shaders.EmissionKeyword);
        mat.EnableKeyword(Shaders.SpecmapKeyword);
        mat.SetFloat(Shaders.GlowField, lightBrightness);
        mat.SetFloat(Shaders.GlowNightField, lightBrightness);
        mat.SetColor(Shaders.ColorField, col);
        mat.SetColor(Shaders.GlowColorField, col);
    }

    private void BlinkThisStrobeOn(Material mat, Color col)
    {
        mat.EnableKeyword(Shaders.EmissionKeyword);
        mat.EnableKeyword(Shaders.SpecmapKeyword);
        mat.SetFloat(Shaders.GlowField, strobeBrightness);
        mat.SetFloat(Shaders.GlowNightField, strobeBrightness);
        mat.SetColor(Shaders.ColorField, col);
        mat.SetColor(Shaders.GlowColorField, col);
    }

    private void BlinkThisLightOff(Material mat)
    {
        mat.DisableKeyword(Shaders.EmissionKeyword);
    }

    private void BlinkOn(List<Material> mats, Color col)
    {
        foreach (var mat in mats)
            BlinkThisLightOn(mat, col);
    }

    private void BlinkOff(List<Material> mats)
    {
        foreach (var mat in mats)
            BlinkThisLightOff(mat);
    }

    private void KillStrobes(LightClass lc)
    {
        switch (lc)
        {
            case LightClass.RedStrobes:
                foreach (var tmp in redStrobeLights)
                    tmp.enabled = false;
                break;
            case LightClass.WhiteStrobes:
                foreach (var tmp in whiteStrobeLights)
                    tmp.enabled = false;
                break;
            default:
                Logger.Warn("Warning: passed bad arg to KillStrobe");
                break;
        }
    }

    private void PowerStrobes(LightClass lc)
    {
        switch (lc)
        {
            case LightClass.RedStrobes:
                foreach (var tmp in redStrobeLights)
                    tmp.enabled = true;
                break;
            case LightClass.WhiteStrobes:
                foreach (var tmp in whiteStrobeLights)
                    tmp.enabled = true;
                break;
            default:
                Logger.Warn("Warning: passed bad arg to Strobe");
                break;
        }
    }

    private void BlinkOnStrobe(LightClass lc)
    {
        switch (lc)
        {
            case LightClass.RedStrobes:
                foreach (var mat in redStrobeMats)
                    BlinkThisStrobeOn(mat, Color.red);
                break;
            case LightClass.WhiteStrobes:
                foreach (var mat in whiteStrobeMats)
                    BlinkThisStrobeOn(mat, Color.white);
                break;
            default:
                Logger.Warn("Warning: passed bad arg to BlinkOnStrobe");
                break;
        }

        PowerStrobes(lc);
    }

    private void BlinkOffStrobe(LightClass lc)
    {
        KillStrobes(lc);
        switch (lc)
        {
            case LightClass.RedStrobes:
                BlinkOff(redStrobeMats);
                break;
            case LightClass.WhiteStrobes:
                BlinkOff(whiteStrobeMats);
                break;
            default:
                Logger.Warn("Warning: passed bad arg to BlinkOffStrobe");
                break;
        }
    }

    private IEnumerator Strobe(LightClass lc)
    {
        while (true)
        {
            BlinkOnStrobe(lc);
            yield return new WaitForSeconds(0.01f);
            BlinkOffStrobe(lc);
            yield return new WaitForSeconds(2.99f);
        }
    }

    private IEnumerator BlinkNarySequence(int n, bool isPortSide)
    {
        int m;
        if (isPortSide)
            m = portMats.Count;
        else
            m = starboardMats.Count;
        if (m == 0)
            yield break;
        var sequenceLength = m == 0 ? 0 : m + 2 * n;
        while (true)
        {
            for (var i = 0; i < sequenceLength - n; i++)
            {
                if (0 <= i && i < m)
                {
                    if (isPortSide)
                        BlinkThisLightOn(portMats[i], Color.red);
                    else
                        BlinkThisLightOn(starboardMats[i], Color.green);
                }

                if (0 <= i - n && i - n < m)
                {
                    if (isPortSide)
                        BlinkThisLightOff(portMats[i - n]);
                    else
                        BlinkThisLightOff(starboardMats[i - n]);
                }

                yield return new WaitForSeconds(0.25f / (sequenceLength - n));
            }

            if (isPortSide)
                BlinkThisLightOff(portMats[m - 1]);
            else
                BlinkThisLightOff(starboardMats[m - 1]);
            yield return new WaitForSeconds(0.75f);
        }
    }

    private IEnumerator ControlLights()
    {
        while (true)
        {
            if (IsLightsOn && rb.IsNotNull())
            {
                EnableLightClass(LightClass.Positions);
                EnableLightClass(LightClass.Ports);
                EnableLightClass(LightClass.Starboards);

                if (white.IsNull() && 10f <= rb.velocity.magnitude)
                {
                    EnableLightClass(LightClass.WhiteStrobes);
                }
                else if (3f < rb.velocity.magnitude && rb.velocity.magnitude < 10f)
                {
                    DisableLightClass(LightClass.WhiteStrobes);
                    DisableLightClass(LightClass.RedStrobes);
                }
                else if (red.IsNull() && 0.001f < rb.velocity.magnitude && rb.velocity.magnitude <= 3f)
                {
                    EnableLightClass(LightClass.RedStrobes);
                }
                else
                {
                    DisableLightClass(LightClass.WhiteStrobes);
                    DisableLightClass(LightClass.RedStrobes);
                }
            }
            else
            {
                DisableLightClass(LightClass.Ports);
                DisableLightClass(LightClass.Starboards);
                DisableLightClass(LightClass.WhiteStrobes);
                DisableLightClass(LightClass.RedStrobes);
                DisableLightClass(LightClass.WhiteStrobes);
                DisableLightClass(LightClass.Positions);
            }

            yield return new WaitForSeconds(1f);
        }
    }
}