using AVS.Engines;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace AVS
{
    public readonly struct EngineSounds
    {
        public AudioClip Hum { get; }
        public AudioClip Whistle { get; }

        public EngineSounds(AudioClip hum, AudioClip whistle)
        {
            Hum = hum ?? VoiceManager.silence;
            Whistle = whistle ?? VoiceManager.silence;
        }

        public static EngineSounds Silence { get; } = new EngineSounds(VoiceManager.silence, VoiceManager.silence);
    }
    public static class EngineSoundsManager
    {
        internal static List<ModVehicleEngine> engines { get; } = new List<ModVehicleEngine>();
        // EngineSounds names : EngineSounds
        internal static Dictionary<string, EngineSounds> EngineSoundMap { get; } = new Dictionary<string, EngineSounds>();
        // vehicle names : EngineSounds names
        internal static Dictionary<TechType, string> defaultEngineSounds { get; } = new Dictionary<TechType, string>();
        private static EngineSounds SilentVoice = EngineSounds.Silence;
        public static void RegisterEngineSounds(string name, EngineSounds voice)
        {
            try
            {
                EngineSoundMap.Add(name, voice);
            }
            catch (ArgumentException e)
            {
                Logger.WarnException($"Tried to register an engine-sounds using a name that already exists: {name}.", e);
                return;
            }
            catch (Exception e)
            {
                Logger.LogException($"Failed to register a engine-sounds: {name}.", e);
                return;
            }
            Logger.Log($"Successfully registered engine-sounds: {name}.");
        }
        public static IEnumerator RegisterEngineSounds(string name, string voicepath = "")
        {
            yield return LoadEngineSoundClips(name, EngineSounds =>
            {
                // Once the voice is loaded, store it in the dictionary
                RegisterEngineSounds(name, EngineSounds);
            }, voicepath);
        }
        public static EngineSounds GetVoice(string name)
        {
            try
            {
                return EngineSoundMap[name];
            }
            catch (KeyNotFoundException e)
            {
                Logger.WarnException($"That engine-sounds not found: {name}.", e);
            }
            catch (ArgumentNullException e)
            {
                Logger.WarnException($"That engine-sounds was null: {name}. ", e);
            }
            catch (Exception e)
            {
                Logger.LogException($"GetVoice engine-sounds failed: {name}.", e);
            }
            return SilentVoice;
        }
        public static void RegisterDefault(ModVehicle mv, string voice)
        {
            if (mv == null)
            {
                Logger.Error($"Cannot register default engine sounds for null ModVehicle: {voice}.");
                return;
            }
            try
            {
                defaultEngineSounds.Add(mv.TechType, voice);
            }
            catch (ArgumentException e)
            {
                Logger.WarnException($"Tried to register a default engine-sounds for a vehicle {mv.GetName()} that already had a default engine-sounds {voice}.", e);
            }
            catch (Exception e)
            {
                Logger.LogException($"Failed to register a default engine-sounds: {voice} for vehicle {mv.GetName()}.", e);
            }
        }
        internal static void UpdateDefaultVoice(ModVehicle mv, string voice)
        {
            if (defaultEngineSounds.ContainsKey(mv.TechType))
            {
                defaultEngineSounds[mv.TechType] = voice;
            }
            else
            {
                defaultEngineSounds.Add(mv.TechType, voice);
            }
        }
        public static EngineSounds GetDefaultVoice(ModVehicle mv)
        {
            try
            {
                return EngineSoundMap[defaultEngineSounds[mv.TechType]];
            }
            catch (Exception)
            {
                Logger.Warn($"No default engine sounds for vehicle type: {mv.GetName()}. Using Shiruba.");
                return EngineSoundMap.First().Value;
            }
        }
        internal static IEnumerator LoadAllVoices()
        {
            GetSilence();
            yield return RegisterEngineSounds("ShirubaFoxy");
            MainPatcher.Instance.GetEngineSounds = null;
        }
        private static IEnumerator GetSilence()
        {
            yield return new WaitUntil(() => VoiceManager.silence != null);
            SilentVoice = new EngineSounds();
            yield break;
        }
        // Method signature with a callback to return the EngineSounds instance
        private static IEnumerator LoadEngineSoundClips(string voice, Action<EngineSounds> onComplete, string voicepath)
        {
            EngineSounds returnVoice = new EngineSounds();

            string modPath = "";
            if (voicepath == "")
            {
                modPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            }
            else
            {
                modPath = voicepath;
            }
            string engineSoundsFolder = Path.Combine(modPath, "EngineSounds");
            string engineSoundPath = Path.Combine(engineSoundsFolder, voice) + "/";


            AudioClip hum = null,
                    whistle = null;

            yield return LoadAudioClip(engineSoundPath + "hum.ogg", clip =>
            {
                hum = clip;
            },
            () =>
            {
                hum = VoiceManager.silence;
            });

            yield return LoadAudioClip(engineSoundPath + "whistle.ogg", clip =>
            {
                whistle = clip;
            },
            () =>
            {
                whistle = VoiceManager.silence;
            });

            returnVoice = new EngineSounds(hum: hum, whistle: whistle);

            onComplete?.Invoke(returnVoice);
        }
        private static IEnumerator LoadAudioClip(string filePath, Action<AudioClip> onSuccess, Action onError)
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(filePath, AudioType.OGGVORBIS))
            {
                yield return www.SendWebRequest();

                if (www.isHttpError || www.isNetworkError)
                {
                    onError?.Invoke();
                }
                else
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    if (!clip)
                    {
                        Logger.Error("Failed to retrieve AudioClip from file: " + filePath);
                    }
                    else
                    {
                        onSuccess?.Invoke(clip);
                    }
                }
            }
        }
    }
}
