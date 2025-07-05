using UnityEngine;

namespace AVS.Audio
{

    /// <summary>
    /// Represents the volume and pitch settings for a sound.
    /// </summary>
    public readonly struct SoundSettings
    {
        /// <summary>
        /// Gets the pitch value for the sound.
        /// </summary>
        public float Pitch { get; }

        /// <summary>
        /// Gets the volume value for the sound.
        /// </summary>
        public float Volume { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SoundSettings"/> struct.
        /// </summary>
        /// <param name="volume">The volume of the sound. Default is 1.0f.</param>
        /// <param name="pitch">The pitch of the sound. Default is 1.0f.</param>
        public SoundSettings(
            float volume = 1f,
            float pitch = 1f)
        {
            Pitch = pitch;
            Volume = volume;
        }

        /// <summary>
        /// Determines whether the current settings are significantly different from another <see cref="SoundSettings"/> instance.
        /// </summary>
        /// <param name="other">The other <see cref="SoundSettings"/> to compare with.</param>
        /// <returns>True if the settings differ by more than a small threshold; otherwise, false.</returns>
        public bool IsSignificantlyDifferent(SoundSettings other)
        {
            if (!SigDif(Pitch, other.Pitch) && !SigDif(Volume, other.Volume))
                return false;

            return true;
        }

        /// <summary>
        /// Determines if two float values are significantly different based on a fixed threshold.
        /// </summary>
        /// <param name="a">First value.</param>
        /// <param name="b">Second value.</param>
        /// <returns>True if the absolute difference is greater than 0.005f; otherwise, false.</returns>
        private static bool SigDif(float a, float b)
        {
            return Mathf.Abs(a - b) > 0.005f;
        }
    }
    /// <summary>
    /// Represents the configuration for a sound source, including its audio clip, playback settings, and spatial properties.
    /// </summary>
    /// <remarks>
    /// This struct encapsulates the settings required to configure and play a sound, such as the
    /// associated audio clip, volume, pitch, spatial distances, and looping behavior. It is immutable and can be used
    /// to define sound properties for playback in both 2D and 3D contexts.
    /// </remarks>
    public readonly struct SoundSetup
    {
        /// <summary>
        /// Gets the audio clip associated with this instance.
        /// </summary>
        public AudioClip AudioClip { get; }

        /// <summary>
        /// Gets a value indicating whether the object is represented in 3D.
        /// </summary>
        public bool Is3D { get; }

        /// <summary>
        /// Gets a value indicating whether the playback is set to loop.
        /// </summary>
        public bool Loop { get; }

        /// <summary>
        /// Gets the minimum distance for the listener.
        /// If the listener is closer than this distance, the sound will be played at full volume.
        /// </summary>
        public float MinDistance { get; }

        /// <summary>
        /// Gets the maximum distance for the listener.
        /// If the listener is farther than this distance, the sound will not be heard.
        /// </summary>
        public float MaxDistance { get; }

        /// <summary>
        /// Gets the listener distance at which the sound volume is exactly half the maximum volume.
        /// Must be in the range (MinDistance, MaxDistance).
        /// </summary>
        public float HalfDistance { get; }

        /// <summary>
        /// The initial volume and pitch settings.
        /// </summary>
        public SoundSettings Settings { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SoundSetup"/> struct.
        /// </summary>
        /// <param name="audioClip">The audio clip to play. Cannot be null.</param>
        /// <param name="volume">The initial volume. Default is 1.0f.</param>
        /// <param name="pitch">The initial pitch. Default is 1.0f.</param>
        /// <param name="minDistance">The minimum distance for full volume. Default is 1.0f.</param>
        /// <param name="maxDistance">The maximum distance for audibility. Default is 500.0f.</param>
        /// <param name="halfDistance">The distance at which the volume is half. Must be between minDistance and maxDistance. Default is 20.0f.</param>
        /// <param name="loop">Whether the sound should loop. Default is false.</param>
        /// <param name="is3D">Whether the sound is 3D. Default is true.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="audioClip"/> is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if distance parameters are invalid.</exception>
        public SoundSetup(
            AudioClip audioClip,
            float volume = 1f,
            float pitch = 1f,
            float minDistance = 1f,
            float maxDistance = 500f,
            float halfDistance = 20f,
            bool loop = false,
            bool is3D = true
            )
        {
            if (!audioClip)
            {
                throw new System.ArgumentNullException(nameof(audioClip), "AudioClip cannot be null.");
            }
            if (halfDistance <= minDistance || halfDistance >= maxDistance)
            {
                throw new System.ArgumentOutOfRangeException(nameof(halfDistance), "HalfDistance must be between MinDistance and MaxDistance.");
            }
            if (minDistance <= 0f || maxDistance <= 0f || minDistance >= maxDistance)
            {
                throw new System.ArgumentOutOfRangeException(nameof(minDistance), "MinDistance and MaxDistance must be positive and MinDistance must be less than MaxDistance.");
            }
            AudioClip = audioClip;
            HalfDistance = halfDistance;
            MinDistance = minDistance;
            MaxDistance = maxDistance;
            Is3D = is3D;
            Loop = loop;
            Settings = new SoundSettings(volume, pitch);
        }

        /// <summary>
        /// Determines if this <see cref="SoundSetup"/> is compatible with another for live playback (e.g., can be swapped without stopping playback).
        /// </summary>
        /// <param name="other">The other <see cref="SoundSetup"/> to compare with.</param>
        /// <returns>True if both setups use the same AudioClip and looping setting; otherwise, false.</returns>
        public bool IsLiveCompatibleTo(SoundSetup other)
        {
            if (AudioClip == other.AudioClip)
            {
                return Loop == other.Loop;
            }

            return false;
        }
    }
}
