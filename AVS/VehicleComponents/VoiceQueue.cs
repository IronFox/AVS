using AVS.BaseVehicle;
using AVS.Configuration;
using AVS.Util;
using AVS.VehicleTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AVS
{
    /// <summary>
    /// Represents a voice line consisting of one or more audio clips, along with a text translation key and priority
    /// level.
    /// </summary>
    /// <remarks>A <see cref="VoiceLine"/> can either contain a single audio clip or a collection of audio
    /// clips to be played in sequence (unless interrupted).
    /// It is typically used to represent a line of dialogue or sound effect in an application, with an
    /// associated translation key for localization and an optional priority to determine playback order or
    /// importance.</remarks>
    public class VoiceLine
    {
        /// <summary>
        /// The single audio clip of this voice line.
        /// If multiple clips are provided, this will be null.
        /// </summary>
        public AudioClip? Clip { get; }
        /// <summary>
        /// Multiple audio clips of this voice line.
        /// If a single clip is provided, this will be null.
        /// </summary>
        public IReadOnlyList<AudioClip>? Clips { get; }
        /// <summary>
        /// Time in seconds between each two clips in <see cref="Clips"/>.
        /// If null or with less elements than <see cref="Clips"/>(-1), trailing gaps will be 0.
        /// </summary>
        public IReadOnlyList<float>? Gaps { get; }
        /// <summary>
        /// The translation key for the text associated with this voice line.
        /// Null if no subtitle should be displayed (even if configured).
        /// Effective only if <see cref="VehicleConfiguration.GetVoiceSubtitlesEnabled"/> is true.
        /// </summary>
        public string? TextTranslationKey { get; }
        /// <summary>
        /// The queue priority of this voice line.
        /// Voice lines of higher priority will interrupt queued clips of lower priority.
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// Individual volume of this voice line, further modified by <see cref="VehicleConfiguration.GetVoiceSoundVolume"/>,
        /// </summary>
        public float Volume { get; set; } = 1f;
        /// <summary>
        /// Checks if this voice line has any audio clips assigned.
        /// </summary>
        public bool HasAnyClips => Clip != null || (Clips != null && Clips.Count > 0);

        /// <summary>
        /// Constructs a new <see cref="VoiceLine"/> with a single audio clip, a text translation key, and an optional priority.
        /// </summary>
        /// <param name="clip">Clip to play. May be null</param>
        /// <param name="textTranslationKey">Text translation key of this line. Null if no subtitle of this line should ever be shown</param>
        /// <param name="priority">Interruption priority</param>
        public VoiceLine(AudioClip? clip, string? textTranslationKey, int priority = 0)
        {
            Clip = clip;
            TextTranslationKey = textTranslationKey;
            Priority = priority;
        }

        /// <summary>
        /// Constructs a new <see cref="VoiceLine"/> with multiple audio clips, a text translation key, and an optional priority.
        /// </summary>
        /// <param name="clips">Clips to play in sequence. While a single playing clip cannot be interrupted, subsequent clips can</param>
        /// <param name="textTranslationKey">Text translation key of this line</param>
        /// <param name="priority">Interruption priority</param>
        /// <param name="gaps">Time in seconds between each two clips in <paramref name="clips"/>. Should have one less element than <paramref name="clips"/> or be null. </param>
        public VoiceLine(IReadOnlyList<AudioClip>? clips, IReadOnlyList<float>? gaps, string? textTranslationKey, int priority = 0)
        {
            Clips = clips;
            Gaps = gaps;
            TextTranslationKey = textTranslationKey;
            Priority = priority;
        }

        internal void ReplaceQueue(Queue<Queued> partQueue)
        {
            partQueue.Clear();
            if (Clips != null && Clips.Count > 0)
            {
                for (int i = 0; i < Clips.Count; i++)
                {
                    float gap = i > 0 && Gaps != null && i <= Gaps.Count
                        ? Gaps[i - 1]
                        : 0f;
                    Logger.DebugLog($"Fetched gap at index {i} out of {string.Join(", ", Gaps?.Select(x => x.ToStr()) ?? Array.Empty<string>())}: {gap.ToStr()} ");
                    partQueue.Enqueue(new Queued(this, Clips[i], i == 0, gap));
                }
            }
            else if (Clip != null)
            {
                partQueue.Enqueue(new Queued(this, Clip, true, 0));
            }
            else
            {
                partQueue.Enqueue(new Queued(this, null, true, 0));
            }
        }
    }

    internal readonly struct Queued
    {
        public VoiceLine Line { get; }
        public AudioClip? Clip { get; }
        public bool IsFirst { get; }
        public float Volume => Line.Volume;
        public float DelayInSeconds { get; }
        public bool HasClips { get; }

        public Queued(VoiceLine line, AudioClip? clip, bool isFirst, float delayInSeconds)
        {
            Line = line;
            Clip = clip;
            IsFirst = isFirst;
            DelayInSeconds = delayInSeconds;
            HasClips = line.HasAnyClips;
            Logger.DebugLog($"Queued voice line: {line.TextTranslationKey}, clip: {clip.NiceName()}, isFirst: {isFirst}, delay: {delayInSeconds:F2}s, volume: {Volume:F2}");
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
        private AvsVehicle? mv;
        private EnergyInterface? aiEI;
        private List<AudioSource> speakers = new List<AudioSource>();

        private VoiceLine? Playing { get; set; } = null;
        private Queue<Queued> PartQueue { get; } = new Queue<Queued>();
        private bool isReadyToSpeak = false;


        /// <summary>
        /// Pauses or unpauses all speakers in this voice queue.
        /// </summary>
        /// <param name="pause">If true, pause all speakers, otherwise unpause them</param>
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

        /// <inheritdoc/>
        public void Awake()
        {
            isReadyToSpeak = false;
            mv = GetComponent<AvsVehicle>();
            if (mv.Com.BackupBatteries.Count > 0)
            {
                aiEI = mv.Com.BackupBatteries[0].Root.GetComponent<EnergyInterface>();
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
                foreach (var ps in sub.Com.Helms)
                {
                    speakers.Add(ps.Root.EnsureComponent<AudioSource>().Register());
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
                speakers.Add(sub2.Com.PilotSeat.Root.EnsureComponent<AudioSource>().Register());
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

        /// <inheritdoc/>
        public void Start()
        {
            SetupSpeakers();
        }


        /// <inheritdoc/>
        public void Update()
        {
            if (mv == null || aiEI == null)
                return;
            foreach (var speaker in speakers)
            {
                if (mv.IsBoarded)
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
                if (Playing != null)
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
                    else
                        timeSinceLastStart = -1;
                }
            }
        }
        private float timeSinceLastStart = -1f;
        private void TryPlayNextClipInQueue()
        {

            if (timeSinceLastStart < 0)
                timeSinceLastStart = 0;
            else
                timeSinceLastStart += Time.deltaTime;
            if (PartQueue.Count > 0)
            {
                var part = PartQueue.Peek();
                if (timeSinceLastStart < part.DelayInSeconds)
                {
                    //Logger.DebugLog($"Waiting for {part.DelayInSeconds - timeSinceLastStart:F2}s before playing next clip.");
                    return; // not enough time has passed since the last clip started
                }
                part = PartQueue.Dequeue();
                if (part.HasClips)
                {
                    foreach (var speaker in speakers)
                    {
                        if (!speaker.enabled)
                            continue;
                        speaker.volume = part.Volume * mv!.Config.GetVoiceSoundVolume() * SoundSystem.GetVoiceVolume() * SoundSystem.GetMasterVolume();
                        speaker.clip = part.Clip;
                        speaker.Play();
                    }
                }
                if ((!part.HasClips || mv!.Config.GetVoiceSubtitlesEnabled())
                    && part.IsFirst && part.Line.TextTranslationKey != null)
                {
                    CreateSubtitle(part.Line.TextTranslationKey);
                }
            }
            else
                Playing = null;
        }
        /// <summary>
        /// Schedules the given voice line to be player if none is currently playing or if the queued line has a higher priority than the current one.
        /// Otherwise, the line is not played and discarded.
        /// </summary>
        /// <param name="line">Line to play</param>
        public void Play(VoiceLine line)
        {
            if (mv && aiEI != null && aiEI.hasCharge)
            {
                if (Playing is null || Playing.Priority < line.Priority)
                {
                    Playing = line;
                    line.ReplaceQueue(PartQueue);
                }
            }
        }
        private void NotifyReadyToSpeak()
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
            Logger.PDANote($"{mv!.subName.hullName.text}: {Language.main.Get(textTranslationKey)}");
        }

    }
}
