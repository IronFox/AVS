using AVS.Interfaces;
using AVS.Log;
using AVS.Util;
using FMOD;
using Nautilus.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AVS.Audio;

/// <summary>
/// FMOD sound creator.
/// </summary>
public static class FModSoundCreator
{
    private static float Sqr(float value) => value * value;

    /// <summary>
    /// Attempts to instantiate a new FMOD sound source, based on the provided configuration.
    /// </summary>
    /// <param name="cfg">The sound configuration to instantiate</param>
    /// <returns>Created sound source. Null if creation failed</returns>
    public static ISoundSource? Play(SoundSetup cfg)
    {
        using var log = SmartLog.ForAVS(cfg.RMC);
        try
        {
            cfg.Validate();

            var mode = MODE.DEFAULT | MODE.ACCURATETIME;
            if (cfg.Is3D)
                mode |= MODE._3D | MODE._3D_CUSTOMROLLOFF;
            else
                mode |= MODE._2D;

            if (cfg.Loop)
                mode |= MODE.LOOP_NORMAL;
            else
                mode |= MODE.LOOP_OFF;
            var sound = AudioUtils.CreateSound(cfg.AudioClip, mode);

            VECTOR[]? rolloffArray = null;
            if (cfg.Is3D)
            {
                var halfDistance = Mathf.Min(cfg.MinDistance + Mathf.Max(cfg.HalfDistance, cfg.MinDistance * 2),
                    cfg.MaxDistance);

                var rolloff = new List<VECTOR>(10);
                var range = cfg.MaxDistance - cfg.MinDistance;
                for (var ix = 0; ix <= 10; ix++)
                {
                    var distance = Sqr((float)ix / 10) * range + cfg.MinDistance;
                    var worldDistance = distance;

                    distance /= halfDistance / Mathf.Sqrt(2);
                    //Log.Write($"Distance modified by halfDistance({halfDistance}): {distance}");

                    var volume = Mathf.Clamp01(1f / (distance * distance) - 1f / (cfg.MaxDistance * cfg.MaxDistance));
                    rolloff.Add(new VECTOR
                    {
                        x = worldDistance,
                        y = volume
                    });
                    //Log.Write($"Rolloff added: {worldDistance},{volume}");
                }

                rolloffArray = rolloff.ToArray();
                Check($"sound.set3DCustomRolloff(ref rolloffArray[0], {rolloffArray.Length})",
                    sound.set3DCustomRolloff(ref rolloffArray[0], rolloffArray.Length));
                Check($"sound.set3DMinMaxDistance({cfg.MinDistance}, {cfg.MaxDistance})",
                    sound.set3DMinMaxDistance(cfg.MinDistance, cfg.MaxDistance));
            }


            if (!AudioUtils.TryPlaySound(sound, "bus:/master", out var channel))
                throw new InvalidOperationException(
                    $"AudioUtils.TryPlaySound(sound, \"bus:/master\", out var channel) failed");

            Check($"Channel.setVolume({cfg.Settings.Volume})", channel.setVolume(0));
            Check($"Channel.setPitch({cfg.Settings.Pitch})", channel.setPitch(0.01f));
            if (cfg.Is3D)
            {
                Check($"Channel.set3DMinMaxDistance({cfg.MinDistance}, {cfg.MaxDistance})",
                    channel.set3DMinMaxDistance(cfg.MinDistance, cfg.MaxDistance));
                var pos = new VECTOR
                {
                    x = cfg.Owner.transform.position.x,
                    y = cfg.Owner.transform.position.y,
                    z = cfg.Owner.transform.position.z
                };

                var vel = new VECTOR
                {
                    x = 0,
                    y = 0,
                    z = 0
                };

                Check($"channel.set3DAttributes(ref pos, ref vel)", channel.set3DAttributes(ref pos, ref vel));
            }

            var component = cfg.Owner.AddComponent<FModComponent>();
            component.RMC = cfg.RMC;

            channel.isPlaying(out var isPlaying);
            //                 channel.set3DDistanceFilter()
            log.Write(
                $"Sound ({channel.handle}) created @{cfg.Owner.transform.position} for {cfg.AudioClip.NiceName()} (mode={mode}, is3d={cfg.Is3D}, min={cfg.MinDistance.ToStr()}, max={cfg.MaxDistance.ToStr()}, loop={cfg.Loop}, isPlaying={isPlaying})");
            var result = new FModSound(channel, sound, component, cfg.AudioClip.name, cfg.RMC, rolloffArray)
            {
                Settings = cfg.Settings,
                Is3D = cfg.Is3D
            };
            component.sound = result;
            return result;
        }
        catch (Exception ex)
        {
            log.Error(
                $"Failed to create sound @{cfg.Owner.transform.position} for {cfg.AudioClip.NiceName()} (is3d={cfg.Is3D}, loop={cfg.Loop}): ",
                ex);
            return null;
        }
    }

    internal static void Check(string action, RESULT result)
    {
        if (result != RESULT.OK)
            throw new FModException($"{action} failed with {result}", result);
    }
}

internal class FModComponent : MonoBehaviour
{
    public FModSound? sound;

    internal RootModController? RMC { get; set; }



    public void OnDestroy()
    {
        using var log = SmartLog.ForAVS(RMC.OrRequired(RootModController.AnyInstance));
        log.Write($"Disposing FModComponent ({sound?.Channel.handle})");
        sound?.Dispose();
    }

    public void Update()
    {
        if (sound is null)
        {
            using var log = SmartLog.ForAVS(RMC.OrRequired(RootModController.AnyInstance));
            log.Error($"sound is null. Self-destructing");
            Destroy(this);
            return;
        }

        if (!sound.Update(Time.deltaTime))
        {
            using var log = SmartLog.ForAVS(RMC.OrRequired(RootModController.AnyInstance));
            log.Error($"FModComponent.sound({sound?.Channel.handle}).Update() returned false. Self-destructing");
            sound = null; //there is something going on in this case. better just unset and don't touch it
            Destroy(this);
        }
    }
}

internal record FModSound(
    Channel Channel,
    Sound Sound,
    FModComponent Component,
    string SoundName,
    RootModController RMC,
    // ReSharper disable once NotAccessedPositionalProperty.Global
    VECTOR[]? RolloffArray //IT IS ABSOLUTELY MANDATORY THAT THIS PROPERTY IS NEVER EVER REMOVED, NO MATTER HOW USELESS IT MAY SEEM!
                           //IF THE GARBAGE COLLECTOR CLEANS THIS UP BEFORE THE SOUND SOURCE IS DONE, THE FMOD ARRAY REFERENCE IS LOST
                           //AND THE SOUND IS SILENCED
    )
    : ISoundSource
{
    private float Age { get; set; }
    private bool Recovered { get; set; }


    public SoundSettings Settings { get; internal set; }
    public bool Is3D { get; init; }

    public bool Died => !Component;

    private Vector3 lastPosition;

    internal bool Update(float timeDelta)
    {
        if (timeDelta <= 0)
            return true;
        Vector3 vpos = Vector3.zero, velocity = Vector3.zero;
        try
        {
            if (Is3D)
            {
                var position = Component.transform.position;
                velocity = (position - lastPosition) / timeDelta;
                lastPosition = position;
                vpos = position;

                var pos = new VECTOR
                {
                    x = position.x,
                    y = position.y,
                    z = position.z
                };

                var vel = new VECTOR
                {
                    x = velocity.x,
                    y = velocity.y,
                    z = velocity.z
                };


                //Log.Write($"Updating FModSound '{SoundName}' ({Channel.handle}) @{position} vel={velocity}");


                FModSoundCreator.Check($"Channel({Channel.handle}).set3DAttributes({position},{velocity})",
                    Channel.set3DAttributes(ref pos, ref vel));
            }

            Age += timeDelta;
            if (Age > 0.1f)
            {
                if (!Recovered)
                {
                    Recovered = true;
                    SetVolumePitch();
                }
                else if (SettingChanged > 0)
                {
                    SettingChanged--;
                    SetVolumePitch();
                }
            }

            return true;
        }
        catch (FModException ex)
        {
            using var log = SmartLog.ForAVS(RMC);
            log.Error($"FModSound.Update({timeDelta}) [{vpos},{velocity}] ", ex);
            return ex.Result != RESULT.ERR_INVALID_HANDLE && ex.Result != RESULT.ERR_CHANNEL_STOLEN;
        }
        catch (Exception ex)
        {
            using var log = SmartLog.ForAVS(RMC);
            log.Error($"FModSound.Update({timeDelta}) [{vpos},{velocity}] ", ex);
            return true;
        }
    }

    private void SetVolumePitch()
    {
        //Log.Write($"Updating FModSound '{SoundName}' {Settings.Volume} / {Settings.Pitch}");
        FModSoundCreator.Check($"Channel({Channel.handle}).setVolume({Settings.Volume})",
            Channel.setVolume(Settings.Volume));
        FModSoundCreator.Check($"Channel({Channel.handle}).setPitch({Settings.Pitch})",
            Channel.setPitch(Settings.Pitch));
    }

    public void ApplyLiveChanges(SoundSettings cfg)
    {
        if (!cfg.IsSignificantlyDifferent(Settings))
            return;
        if (Died)
            return;
        Settings = cfg;
        SettingChanged = 2;
    }

    private int SettingChanged { get; set; }

    public bool IsPlaying
    {
        get
        {
            if (Died)
                return false;
            Channel.isPlaying(out var isPlaying);

            return isPlaying;
        }
    }

    public bool IsPaused
    {
        get
        {
            if (Died)
                return false;
            Channel.getPaused(out var isPaused);
            return isPaused;
        }
        set
        {
            if (Died)
                return;
            Channel.setPaused(value);
        }
    }

    public void Dispose()
    {
        try
        {
            Channel.stop();
            Sound.release();
            Object.Destroy(Component);
        }
        catch (Exception ex)
        {
            using var log = SmartLog.ForAVS(RMC);
            log.Error($"FModSound.Dispose()", ex);
        }
    }
}