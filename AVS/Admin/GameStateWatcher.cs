using System;
using System.Collections.Generic;
using AVS.Util;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AVS.Admin;

/// <summary>
/// Provides utilities for monitoring and managing the state of the game, including player and world state.
/// </summary>
/// <remarks>This class contains static members to track key game state flags, such as whether the player
/// has awakened or started, and whether the world is loaded or settled. It also provides mechanisms for resetting
/// the game state and invoking cleanup actions when a scene is reset.</remarks>
public static class GameStateWatcher
{
    /// <summary>
    /// Gets or sets a value indicating whether the player has been awakened.
    /// </summary>
    public static bool IsPlayerAwakened { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the player has started.
    /// </summary>
    public static bool IsPlayerStarted { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the world is currently loaded.
    /// </summary>
    public static bool IsWorldLoaded { get; set; } = false;

    /// <summary>
    /// Gets a value indicating whether the world is fully loaded and the player's immediate surroundings are active
    /// and built.
    /// </summary>
    public static bool IsWorldSettled => LargeWorldStreamer.main.IsNotNull() && Player.main.IsNotNull() &&
                                         LargeWorldStreamer.main.IsRangeActiveAndBuilt(
                                             new Bounds(Player.main.transform.position, new Vector3(5f, 5f, 5f)));

    /// <summary>
    /// Actions called when <see cref="SignalSceneUnloaded(Scene)"/> is invoked.
    /// </summary>
    public static List<Action> OnSceneUnloaded { get; } = new();

    /// <summary>
    /// Signals that the scene has been unloaded, clearing the vehicle manager and invoking all registered actions.
    /// </summary>
    /// <param name="scene">Scene that has been unloaded</param>
    public static void SignalSceneUnloaded(Scene scene)
    {
        AvsVehicleManager.VehiclesInPlay.Clear();
        OnSceneUnloaded.ForEach(x => x.Invoke());
        IsPlayerAwakened = false;
        IsPlayerStarted = false;
    }
}