using AVS.Util;
using System;
using UnityEngine;

namespace AVS.Audio;

/// <summary>
/// Settings that can be changed during the lifetime of a sound source
/// </summary>
/// <param name="Volume">The volume value for the sound. Default is 1.0f.</param>
/// <param name="Pitch">The pitch value for the sound. Default is 1.0f.</param>
public readonly record struct SoundSettings(
    float Volume = 1f,
    float Pitch = 1f
)
{
    /// <summary>
    /// Determines whether the current settings are significantly different from another <see cref="SoundSettings"/> instance.
    /// </summary>
    /// <param name="other">The other <see cref="SoundSettings"/> to compare with.</param>
    /// <returns>True if the settings differ by more than a small threshold; otherwise, false.</returns>
    public bool IsSignificantlyDifferent(SoundSettings other)
    {
        if (!SigDif(Pitch, other.Pitch)
            && !SigDif(Volume, other.Volume)
           )
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
        if (a == 0)
            return b != 0;
        if (b == 0)
            return true;
        var mn = Mathf.Min(a, b);
        var mx = Mathf.Max(a, b);
        return mx / mn > 1.01f;
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
/// <summary>
/// Represents the configuration for a sound source, including its audio clip, playback settings, and spatial properties.
/// </summary>
/// <param name="RMC">The root mod controller associated with this sound setup</param>
/// <param name="Owner">The owning game object</param>
/// <param name="AudioClip">The audio clip associated with this instance</param>
/// <param name="Settings">The initial volume and pitch settings</param>
/// <param name="MinDistance">The minimum distance for the listener. If the listener is closer than this distance, the sound will be played at full volume.</param>
/// <param name="MaxDistance">The maximum distance for the listener. If the listener is farther than this distance, the sound will not be heard.</param>
/// <param name="HalfDistance">The listener distance at which the sound volume is exactly half the maximum volume. Must be in the range (MinDistance*2, MaxDistance-MinDistance), otherwise clamped.</param>
/// <param name="Loop">Whether the playback is set to loop</param>
/// <param name="Is3D">Whether the object is represented in 3D</param>
/// <exception cref="System.ArgumentNullException">Thrown if AudioClip is null.</exception>
/// <exception cref="System.ArgumentOutOfRangeException">Thrown if distance parameters are invalid.</exception>
public readonly record struct SoundSetup(
    RootModController RMC,
    GameObject Owner,
    AudioClip AudioClip,
    SoundSettings Settings = default,
    float MinDistance = 1f,
    float MaxDistance = 500f,
    float HalfDistance = 20f,
    bool Loop = false,
    bool Is3D = true)
{
    /// <summary>
    /// Validates the current <see cref="SoundSetup"/> instance to ensure that all required parameters are correctly set.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when either <see cref="AudioClip"/> or <see cref="Owner"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when parameter values do not meet the expected range in a 3D sound configuration.</exception>
    public void Validate()
    {
        if (AudioClip.IsNull())
            throw new ArgumentNullException(nameof(AudioClip), $"SoundSetup: AudioClip must not be null");
        if (Is3D)
        {
            if (float.IsNaN(MinDistance) || float.IsInfinity(MinDistance))
                throw new ArgumentOutOfRangeException(nameof(MinDistance), $"SoundSetup: MinDistance must be a valid, finite number");

            if (float.IsNaN(MaxDistance) || float.IsInfinity(MaxDistance))
                throw new ArgumentOutOfRangeException(nameof(MaxDistance), $"SoundSetup: MaxDistance must be a valid, finite number");

            if (float.IsNaN(HalfDistance) || float.IsInfinity(HalfDistance))
                throw new ArgumentOutOfRangeException(nameof(HalfDistance), $"SoundSetup: HalfDistance must be a valid, finite number");

            if (MinDistance >= MaxDistance)
                throw new ArgumentOutOfRangeException(nameof(MinDistance), $"SoundSetup: MinDistance must be < MaxDistance");

            if (MinDistance <= 0)
                throw new ArgumentOutOfRangeException(nameof(MinDistance), $"SoundSetup: MinDistance must be > 0");
        }

        if (Owner.IsNull())
            throw new ArgumentNullException(nameof(Owner), $"SoundSetup: Sound owner GameObject must exist");
    }

    /// <summary>
    /// Determines if this <see cref="SoundSetup"/> is compatible with another for live playback (e.g., can be swapped without stopping playback).
    /// </summary>
    /// <param name="other">The other <see cref="SoundSetup"/> to compare with.</param>
    /// <returns>True if both setups use the same AudioClip and looping setting; otherwise, false.</returns>
    public bool IsLiveCompatibleTo(SoundSetup other)
    {
        if (AudioClip == other.AudioClip) return Loop == other.Loop;

        return false;
    }
}