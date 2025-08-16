using System;
using AVS.Audio;

namespace AVS.Interfaces;

/// <summary>
/// Instantiated sound source
/// </summary>
public interface ISoundSource : IDisposable
{
    /// <summary>
    /// The sound source has been destroyed. It is no longer playing
    /// </summary>
    public bool Died { get; }

    /// <summary>
    /// Updates the live settings of this sound source.
    /// </summary>
    /// <param name="cfg">New configuration</param>
    public void ApplyLiveChanges(SoundSettings cfg);
    
    /// <summary>
    /// Check if the sound source is playing
    /// </summary>
    public bool IsPlaying { get; }
    
    /// <summary>
    /// Gets/sets whether the sound source is currently paused
    /// </summary>
    public bool IsPaused { get; set; }

}