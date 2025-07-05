using AVS.VehicleTypes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AVS
{
    public class VoiceLine
    {
        public AudioClip Clip { get; }
        public string TextTranslationKey { get; }
        public int Priority { get; }

        public VoiceLine(AudioClip clip, string textTranslationKey, int priority = 0)
        {
            Clip = clip;
            TextTranslationKey = textTranslationKey;
            Priority = priority;
        }
    }
    /// <summary>
    /// Manages the playback of voice lines for a vehicle, including queuing, speaker setup, and audio playback control.
    /// </summary>
    /// <remarks>The <see cref="VoiceQueue"/> class is responsible for handling voice line playback for
    /// vehicles, such as submarines or submersibles. It manages a one-element-queue of voice lines, controls audio sources attached
    /// to the vehicle, and ensures proper playback behavior based on the vehicle's state (e.g., whether it is under
    /// command or has sufficient energy). This class also integrates with the game's subtitle system to display
    /// subtitles for voice lines when enabled.</remarks>
    public class VoiceQueue : MonoBehaviour, IScuttleListener
    {
        private ModVehicle mv;
        private EnergyInterface aiEI;
        private List<AudioSource> speakers = new List<AudioSource>();
        private VoiceLine Queued { get; set; } = null;
        private bool isReadyToSpeak = false;
        private float m_balance = 1f;
        public float Balance
        {
            get
            {
                return m_balance;
            }
            set
            {
                if (value < 0)
                {
                    m_balance = 0;
                }
                else if (1 < value)
                {
                    m_balance = 1;
                }
                else
                {
                    m_balance = value;
                }
            }
        }

        public void PauseSpeakers(bool pause)
        {
            foreach (var sp in speakers)
            {
                if (sp != null)
                {
                    if (pause)
                    {
                        sp.Pause();
                    }
                    else
                    {
                        sp.UnPause();
                    }
                }
            }
        }
        public void Awake()
        {
            isReadyToSpeak = false;
            mv = GetComponent<ModVehicle>();
            if (mv.Com.BackupBatteries.Count > 0)
            {
                aiEI = mv.Com.BackupBatteries[0].BatterySlot.GetComponent<EnergyInterface>();
            }
            else
            {
                aiEI = mv.energyInterface;
            }

            // register self with mainpatcher, for on-the-fly voice selection updating
            //VoiceManager.voices.Add(this);
            IEnumerator WaitUntilReadyToSpeak()
            {
                yield return new WaitUntil(() => Admin.GameStateWatcher.IsWorldSettled);
                NotifyReadyToSpeak();
                yield break;
            }
            UWE.CoroutineHost.StartCoroutine(WaitUntilReadyToSpeak());

        }
        private void SetupSpeakers()
        {
            //speakers.Add(mv.VehicleModel.EnsureComponent<AudioSource>());
            if (mv is Submarine sub)
            {
                foreach (var ps in sub.Com.PilotSeats)
                {
                    speakers.Add(ps.Seat.EnsureComponent<AudioSource>().Register());
                }
                foreach (var ps in sub.Com.Hatches)
                {
                    speakers.Add(ps.Hatch.EnsureComponent<AudioSource>().Register());
                }
                foreach (var ps in sub.Com.TetherSources)
                {
                    speakers.Add(ps.EnsureComponent<AudioSource>().Register());
                }
            }
            if (mv is Submersible sub2)
            {
                speakers.Add(sub2.Com.PilotSeat.Seat.EnsureComponent<AudioSource>().Register());
                foreach (var ps in sub2.Com.Hatches)
                {
                    speakers.Add(ps.Hatch.EnsureComponent<AudioSource>().Register());
                }
            }
            foreach (var sp in speakers)
            {
                sp.gameObject.EnsureComponent<AudioLowPassFilter>().cutoffFrequency = 1500;
                sp.priority = 1;
                sp.playOnAwake = false;
                //sp.clip = VoiceManager.silence;
                sp.spatialBlend = 0.92f;
                sp.spatialize = true;
                sp.rolloffMode = AudioRolloffMode.Linear;
                sp.minDistance = 0f;
                sp.maxDistance = 100f;
                sp.spread = 180;
                sp.Stop();
            }
        }
        public void Start()
        {
            SetupSpeakers();
        }

        public void Update()
        {
            foreach (var speaker in speakers)
            {
                if (mv.IsUnderCommand)
                {
                    speaker.GetComponent<AudioLowPassFilter>().enabled = false;
                }
                else
                {
                    speaker.GetComponent<AudioLowPassFilter>().enabled = true;
                }
            }
            if (aiEI.hasCharge && isReadyToSpeak)
            {
                if (Queued != null)
                {
                    bool anyPlaying = false;
                    foreach (var but in speakers)
                    {
                        if (but.isPlaying)
                        {
                            anyPlaying = true;
                            break;
                        }
                    }
                    if (!anyPlaying)
                    {
                        TryPlayNextClipInQueue();
                    }
                }
            }
        }
        public void TryPlayNextClipInQueue()
        {
            if (Queued != null)
            {
                var line = Queued;
                Queued = null;
                foreach (var speaker in speakers)
                {
                    speaker.volume = Balance * mv.AutopilotSoundVolume * SoundSystem.GetVoiceVolume() * SoundSystem.GetMasterVolume();
                    speaker.clip = line.Clip;
                    speaker.Play();
                    if (mv.ShowAutopilotSubtitles)
                    {
                        CreateSubtitle(line.TextTranslationKey);
                    }
                }
            }
        }
        public void EnqueueClip(VoiceLine line)
        {
            if (mv && aiEI.hasCharge)
            {
                if (Queued is null || Queued.Priority < line.Priority)
                    Queued = line;
            }
        }
        public void NotifyReadyToSpeak()
        {
            isReadyToSpeak = true;
        }
        void IScuttleListener.OnScuttle()
        {
            enabled = false;
        }
        void IScuttleListener.OnUnscuttle()
        {
            enabled = true;
        }
        private void CreateSubtitle(string textTranslationKey)
        {
            Logger.PDANote($"{mv.subName.hullName.text}: {Language.main.Get(textTranslationKey)}");
        }

    }
}
