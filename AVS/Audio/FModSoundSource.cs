using FMOD;
using Nautilus.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AVS.Audio
{
    /// <summary>
    /// FMOD sound creator.
    /// </summary>
    internal static class FModSoundCreator
    {


        private static float Sqr(float value) => value * value;

        /// <summary>
        /// Attempts to instantiate a new FMOD sound based on the provided configuration.
        /// </summary>
        /// <param name="cfg">The sound configuration to instantiate</param>
        /// <param name="startingPosition">The position where the sound should start playing</param>
        /// <returns></returns>
        internal static FModSound Instantiate(SoundSetup cfg, Vector3 startingPosition)
        {

            try
            {
                if (!cfg.AudioClip)
                {
                    Logger.Error($"FModSoundCreator.Instantiate(): cfg.AudioClip is null");
                    return null;
                }
                var mode = MODE.DEFAULT | MODE._3D | MODE.ACCURATETIME
                    //| MODE._3D_INVERSEROLLOFF
                    | MODE._3D_CUSTOMROLLOFF
                    ;
                if (cfg.Loop)
                    mode |= MODE.LOOP_NORMAL;
                else
                    mode |= MODE.LOOP_OFF;
                var sound = AudioUtils.CreateSound(cfg.AudioClip, mode
                    );



                List<VECTOR> rolloff = new List<VECTOR>();
                float range = (cfg.MaxDistance - cfg.MinDistance);
                for (int ix = 0; ix <= 10; ix++)
                {
                    float distance = Sqr((float)ix / 10) * range + cfg.MinDistance;
                    float worldDistance = distance;

                    distance /= cfg.HalfDistance / Mathf.Sqrt(2);
                    //Log.Write($"Distance modified by halfDistance({halfDistance}): {distance}");

                    float volume = Mathf.Clamp01(1f / (distance * distance) - (1f / (cfg.MaxDistance * cfg.MaxDistance)));
                    rolloff.Add(new VECTOR
                    {
                        x = worldDistance,
                        y = volume
                    });
                    //Log.Write($"Rolloff added: {worldDistance},{volume}");
                }
                var rolloffArray = rolloff.ToArray();



                FModSoundCreator.Check($"sound.set3DCustomRolloff(ref rolloffArray[0], {rolloffArray.Length})", sound.set3DCustomRolloff(ref rolloffArray[0], rolloffArray.Length));
                FModSoundCreator.Check($"sound.set3DMinMaxDistance({cfg.MinDistance}, {cfg.MaxDistance})", sound.set3DMinMaxDistance(cfg.MinDistance, cfg.MaxDistance));




                if (!AudioUtils.TryPlaySound(sound, "bus:/master", out var channel))
                    throw new InvalidOperationException($"AudioUtils.TryPlaySound(sound, \"bus:/master\", out var channel) failed");

                FModSoundCreator.Check($"Channel.setVolume({cfg.Settings.Volume})", channel.setVolume(0));
                FModSoundCreator.Check($"Channel.setPitch({cfg.Settings.Pitch})", channel.setPitch(0.01f));
                FModSoundCreator.Check($"Channel.set3DMinMaxDistance({cfg.MinDistance}, {cfg.MaxDistance})", channel.set3DMinMaxDistance(cfg.MinDistance, cfg.MaxDistance));




                var pos = new VECTOR
                {
                    x = startingPosition.x,
                    y = startingPosition.y,
                    z = startingPosition.z
                };

                var vel = new VECTOR
                {
                    x = 0,
                    y = 0,
                    z = 0
                };



                Check($"channel.set3DAttributes(ref pos, ref vel)", channel.set3DAttributes(ref pos, ref vel));

                //var component = cfg.Owner.AddComponent<FModSoundSource>();

                channel.isPlaying(out var isPlaying);
                Logger.Log($"Sound ({channel.handle}) created (isPlaying={isPlaying})");
                return new FModSound(cfg, channel, sound, rolloffArray);
            }
            catch (Exception ex)
            {
                Logger.Exception($"FModSoundCreator.Instantiate(): ", ex);
                return null;
            }
        }

        internal static void Check(string action, RESULT result)
        {
            if (result != RESULT.OK)
                throw new FModException($"{action} failed with {result}", result);
        }
    }

    /// <summary>
    /// FMOD sound source, potentially spatial. Attachable as component to any GameObject.
    /// </summary>
    public class FModSoundSource : Component
    {
        private FModSound sound;
        private SoundSetup config;
        private SoundSettings settings;
        private bool reset = false;
        /// <summary>
        /// The sound configuration for this sound source.
        /// Until property set, the local sound will not be created.
        /// </summary>
        public SoundSetup Setup
        {
            get => config;
            set
            {
                config = value;
                settings = value.Settings;
                reset = true;
                //sound?.ApplyLiveChanges(config);
            }
        }

        /// <summary>
        /// Volume and pitch settings for this sound source.
        /// </summary>
        public SoundSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                sound?.ApplyLiveChanges(value);
            }
        }

        /// <inheritDoc />
        public void OnDestroy()
        {
            Logger.Log($"Disposing FModComponent ({sound?.Channel.handle})");
            sound?.Dispose();
        }

        /// <inheritDoc />
        public void Update()
        {
            if (sound is null || reset)
            {
                if (config.AudioClip)
                {
                    sound = FModSoundCreator.Instantiate(config, transform.position);
                    if (sound is null)
                        return; //try again next frame
                    reset = false;
                }
                else
                    return; //no sound to play
            }


            if (!sound.Update(this))
            {
                sound = null;//there is something going on in this case. better just unset and don't touch it
                Logger.Error($"FModComponent.sound({sound.Channel.handle}).Update() returned false. Self-destructing");
                Destroy(this);
            }
        }
    }


    internal class FModSound
    {
        public SoundSetup Config { get; private set; }
        public SoundSettings Settings { get; private set; }
        public FMOD.Channel Channel { get; }
        public VECTOR[] RolloffArray { get; }
        public Sound Sound { get; }
        private float Age { get; set; }
        private bool Recovered { get; set; }

        private Vector3 lastPosition;
        private FModSoundSource lastOwner;

        public FModSound(SoundSetup config, FMOD.Channel channel, Sound sound, VECTOR[] rolloffArray)
        {
            Config = config;
            Settings = config.Settings;
            Channel = channel;
            RolloffArray = rolloffArray;
            Sound = sound;
        }

        internal bool Update(FModSoundSource component)
        {
            lastOwner = component;
            if (Time.deltaTime <= 0)
                return true;
            Vector3 vpos = Vector3.zero, velocity = Vector3.zero;
            try
            {
                var position = component.transform.position;
                velocity = (position - lastPosition) / Time.deltaTime;
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



                FModSoundCreator.Check($"Channel({Channel.handle}).set3DAttributes({position},{velocity})", Channel.set3DAttributes(ref pos, ref vel));

                Age += Time.deltaTime;
                if (Age > 0.1f && !Recovered)
                {
                    Recovered = true;
                    FModSoundCreator.Check($"Channel({Channel.handle}).setVolume({Settings.Volume})", Channel.setVolume(Settings.Volume));
                    FModSoundCreator.Check($"Channel({Channel.handle}).setPitch({Settings.Pitch})", Channel.setPitch(Settings.Pitch));
                }
                return true;
            }
            catch (FModException ex)
            {
                Logger.Exception($"FModSound.Update({Time.deltaTime}) [{vpos},{velocity}] ", ex);
                return ex.Result != RESULT.ERR_INVALID_HANDLE;
            }
            catch (Exception ex)
            {
                Logger.Exception($"FModSound.Update({Time.deltaTime}) [{vpos},{velocity}] ", ex);
                return true;
            }
        }

        public void ApplyLiveChanges(SoundSettings settings)
        {
            try
            {
                if (Recovered)
                {
                    FModSoundCreator.Check($"Channel.setVolume({settings.Volume})", Channel.setVolume(settings.Volume));
                    FModSoundCreator.Check($"Channel.setPitch({settings.Pitch})", Channel.setPitch(settings.Pitch));
                }
                FModSoundCreator.Check($"Channel.set3DMinMaxDistance({Config.MinDistance}, {Config.MaxDistance})", Channel.set3DMinMaxDistance(Config.MinDistance, Config.MaxDistance));
            }
            catch (Exception ex)
            {
                Logger.Exception($"FModSound.ApplyLiveChanges()", ex);
            }


            Settings = settings;
        }

        public void Dispose()
        {
            try
            {
                Channel.stop();
                Sound.release();
                GameObject.Destroy(lastOwner);
            }
            catch (Exception ex)
            {
                Logger.Exception($"FModSound.Dispose()", ex);
            }

        }
    }
}
