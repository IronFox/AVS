using System;
using System.Collections.Generic;
using UnityEngine;

namespace AVS.Config
{
    public class SubmarineConfiguration : VehicleConfiguration
    {
        //set CanLeviathanGrab false
        /// <summary>
        /// The list of active tethers in the submarine.
        /// Each tether is an object with attached SphereCollider,
        /// no renderers or rigidbodies,
        /// and scale set to 1, 1, 1.
        /// As long as a player is within the radius of at least one tether,
        /// they will be considered to be inside the submarine.
        /// Must not be empty.
        /// </summary>
        public IReadOnlyList<GameObject> TetherSources { get; } = Array.Empty<GameObject>();
        /// <summary>
        /// Required not empty
        /// </summary>
        public IReadOnlyList<VehicleParts.VehiclePilotSeat> PilotSeats { get; } = Array.Empty<VehicleParts.VehiclePilotSeat>();
        /// <summary>
        /// Required not empty
        /// </summary>
        public IReadOnlyList<VehicleParts.VehicleHatchStruct> Hatches { get; } = Array.Empty<VehicleParts.VehicleHatchStruct>();
        public IReadOnlyList<VehicleParts.VehicleFloodLight> FloodLights { get; } = Array.Empty<VehicleParts.VehicleFloodLight>();
        public IReadOnlyList<Light> InteriorLights { get; } = null;
        public IReadOnlyList<GameObject> NavigationPortLights { get; } = Array.Empty<GameObject>();
        public IReadOnlyList<GameObject> NavigationStarboardLights { get; } = Array.Empty<GameObject>();
        public IReadOnlyList<GameObject> NavigationPositionLights { get; } = Array.Empty<GameObject>();
        public IReadOnlyList<GameObject> NavigationWhiteStrobeLights { get; } = Array.Empty<GameObject>();
        public IReadOnlyList<GameObject> NavigationRedStrobeLights { get; } = Array.Empty<GameObject>();

        /// <summary>
        /// Panel that controls floodlights
        /// </summary>
        public GameObject ControlPanel { get; } = null;
        public GameObject Fabricator { get; } = null;
        /// <summary>
        /// Optional color picker console game object.
        /// The child transform hierarchy must be as follows:
        /// EditScreen:
        ///     Inactive
        ///     Active:
        ///         BaseTab
        ///         NameTab
        ///         InteriorTab
        ///         Stripe1Tab
        ///         ColorPicker
        ///         Button
        ///         InputField:
        ///             Text
        /// </summary>
        public GameObject ColorPicker { get; } = null;
        public GameObject RespawnPoint { get; } = null;

    }
}
