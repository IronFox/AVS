using AVS.Engines;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AVS.Composition
{
    /// <summary>
    /// Represents the composition of a submarine vehicle, including all relevant objects, lights, seats, and optional components.
    /// Inherits from <see cref="VehicleComposition"/> and adds submarine-specific parts such as tethers, pilot seats, navigation lights, and more.
    /// </summary>
    public class SubmarineComposition : VehicleComposition
    {
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
        /// The list of pilot seats in the submarine.
        /// Each seat allows a player to pilot the submarine.
        /// Must not be empty.
        /// </summary>
        public IReadOnlyList<VehicleParts.VehiclePilotSeat> PilotSeats { get; } = Array.Empty<VehicleParts.VehiclePilotSeat>();

        /// <summary>
        /// Optional flood light definitions.
        /// If non-empty, these lights will be controlled using the control panel, if installed.
        /// </summary>
        public IReadOnlyList<VehicleParts.VehicleFloodLight> FloodLights { get; } = Array.Empty<VehicleParts.VehicleFloodLight>();

        /// <summary>
        /// Optional interior light definitions.
        /// If non-empty, these lights will be controlled using the control panel, if installed.
        /// </summary>
        public IReadOnlyList<Light> InteriorLights { get; } = Array.Empty<Light>();

        /// <summary>
        /// External navigation lights located on the port side of the submarine.
        /// </summary>
        public IReadOnlyList<GameObject> NavigationPortLights { get; } = Array.Empty<GameObject>();

        /// <summary>
        /// External navigation lights located on the starboard side of the submarine.
        /// </summary>
        public IReadOnlyList<GameObject> NavigationStarboardLights { get; } = Array.Empty<GameObject>();

        /// <summary>
        /// External position navigation lights.
        /// </summary>
        public IReadOnlyList<GameObject> NavigationPositionLights { get; } = Array.Empty<GameObject>();

        /// <summary>
        /// White strobe lights that also emit light.
        /// </summary>
        public IReadOnlyList<GameObject> NavigationWhiteStrobeLights { get; } = Array.Empty<GameObject>();

        /// <summary>
        /// Red strobe lights that also emit light.
        /// </summary>
        public IReadOnlyList<GameObject> NavigationRedStrobeLights { get; } = Array.Empty<GameObject>();

        /// <summary>
        /// Optional parent game object for the flood light control panel.
        /// </summary>
        public GameObject? ControlPanel { get; } = null;

        /// <summary>
        /// Optional pre-install fabricator parent game object.
        /// If not null, a fabricator will be automatically created as child of this game object.
        /// </summary>
        public GameObject? Fabricator { get; } = null;

        /// <summary>
        /// Optional color picker console game object to construct necessary components in.
        /// </summary>
        public GameObject? ColorPicker { get; } = null;

        /// <summary>
        /// Optional respawn point in case the character dies.
        /// If null, a respawn point will automatically be created in the vehicle's root object.
        /// </summary>
        public GameObject? RespawnPoint { get; } = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubmarineComposition"/> class with the specified components and configuration.
        /// </summary>
        /// <param name="storageRootObject">The parent object for all storage objects. Must not be null and not the same as vehicle object.</param>
        /// <param name="modulesRootObject">The parent object for all modules. Must not be null and not the same as vehicle object.</param>
        /// <param name="tetherSources">The list of active tethers in the submarine. Must not be null or empty.</param>
        /// <param name="pilotSeats">The list of pilot seats in the submarine. Must not be null or empty.</param>
        /// <param name="hatches">Entry/exit hatches for the submarine. Must not be null or empty.</param>
        /// <param name="floodLights">Optional flood light definitions. If non-empty, these lights will be controlled using the control panel, if installed.</param>
        /// <param name="interiorLights">Optional interior light definitions. If non-empty, these lights will be controlled using the control panel, if installed.</param>
        /// <param name="navigationPortLights">External navigation lights located on the port side of the submarine.</param>
        /// <param name="navigationStarboardLights">External navigation lights located on the starboard side of the submarine.</param>
        /// <param name="navigationPositionLights">External position navigation lights.</param>
        /// <param name="navigationWhiteStrobeLights">White strobe lights that also emit light.</param>
        /// <param name="navigationRedStrobeLights">Red strobe lights that also emit light.</param>
        /// <param name="controlPanel">Optional parent game object for the flood light control panel.</param>
        /// <param name="fabricator">Optional pre-install fabricator parent game object. If not null, a fabricator will be automatically created as child of this game object.</param>
        /// <param name="colorPicker">Optional color picker console game object to construct necessary components in.</param>
        /// <param name="respawnPoint">Optional respawn point in case the character dies. If null, a respawn point will automatically be created in the vehicle's root object.</param>
        /// <param name="collisionModel">Object containing all colliders. Must not be null. Should not be the same as the vehicle object.</param>
        /// <param name="batteries">Battery definitions. Optional.</param>
        /// <param name="upgrades">Upgrade module definitions. Optional.</param>
        /// <param name="boundingBoxCollider">Single box collider for the vehicle. Can be null.</param>
        /// <param name="waterClipProxies">Water clip proxies. Optional.</param>
        /// <param name="innateStorages">Innate storages. Optional.</param>
        /// <param name="modularStorages">Modular storages. Optional.</param>
        /// <param name="headLights">Headlights. Optional.</param>
        /// <param name="canopyWindows">Canopy windows. Optional.</param>
        /// <param name="backupBatteries">Backup batteries. Optional.</param>
        /// <param name="steeringWheelLeftHandTarget">Steering wheel left hand target. Optional.</param>
        /// <param name="steeringWheelRightHandTarget">Steering wheel right hand target. Optional.</param>
        /// <param name="denyBuildingColliders">Deny building colliders. Optional.</param>
        /// <param name="subNameDecals">Sub name decals. Optional.</param>
        /// <param name="lavaLarvaAttachPoints">Lava larva attach points. Optional.</param>
        /// <param name="leviathanGrabPoint">Leviathan grab point. Optional.</param>
        /// <param name="engine">The engine that powers the vehicle. Must not be null.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="tetherSources"/> or <paramref name="pilotSeats"/> is null or empty.</exception>
        public SubmarineComposition(
            GameObject storageRootObject,
            GameObject modulesRootObject,
            IReadOnlyList<GameObject> tetherSources,
            IReadOnlyList<VehicleParts.VehiclePilotSeat> pilotSeats,
            IReadOnlyList<VehicleParts.VehicleHatchDefinition> hatches,
            AbstractEngine engine,
            GameObject collisionModel,
            IReadOnlyList<VehicleParts.VehicleFloodLight>? floodLights = null,
            IReadOnlyList<Light>? interiorLights = null,
            IReadOnlyList<GameObject>? navigationPortLights = null,
            IReadOnlyList<GameObject>? navigationStarboardLights = null,
            IReadOnlyList<GameObject>? navigationPositionLights = null,
            IReadOnlyList<GameObject>? navigationWhiteStrobeLights = null,
            IReadOnlyList<GameObject>? navigationRedStrobeLights = null,
            GameObject? controlPanel = null,
            GameObject? fabricator = null,
            GameObject? colorPicker = null,
            GameObject? respawnPoint = null,
            // VehicleComposition base parameters
            IReadOnlyList<VehicleParts.VehicleBattery>? batteries = null,
            IReadOnlyList<VehicleParts.VehicleUpgrades>? upgrades = null,
            BoxCollider? boundingBoxCollider = null,
            IReadOnlyList<GameObject>? waterClipProxies = null,
            IReadOnlyList<VehicleParts.VehicleStorage>? innateStorages = null,
            IReadOnlyList<VehicleParts.VehicleStorage>? modularStorages = null,
            IReadOnlyList<VehicleParts.VehicleFloodLight>? headLights = null,
            IReadOnlyList<GameObject>? canopyWindows = null,
            IReadOnlyList<VehicleParts.VehicleBattery>? backupBatteries = null,
            GameObject? steeringWheelLeftHandTarget = null,
            GameObject? steeringWheelRightHandTarget = null,
            IReadOnlyList<Collider>? denyBuildingColliders = null,
            IReadOnlyList<TMPro.TextMeshProUGUI>? subNameDecals = null,
            IReadOnlyList<Transform>? lavaLarvaAttachPoints = null,
            GameObject? leviathanGrabPoint = null
        )
        : base(
              engine: engine,
            hatches: hatches,
            storageRootObject: storageRootObject,
            modulesRootObject: modulesRootObject,
            collisionModel: collisionModel,
            batteries: batteries,
            upgrades: upgrades,
            boundingBoxCollider: boundingBoxCollider,
            waterClipProxies: waterClipProxies,
            innateStorages: innateStorages,
            modularStorages: modularStorages,
            headLights: headLights,
            canopyWindows: canopyWindows,
            backupBatteries: backupBatteries,
            steeringWheelLeftHandTarget: steeringWheelLeftHandTarget,
            steeringWheelRightHandTarget: steeringWheelRightHandTarget,
            denyBuildingColliders: denyBuildingColliders,
            subNameDecals: subNameDecals,
            lavaLarvaAttachPoints: lavaLarvaAttachPoints,
            leviathanGrabPoint: leviathanGrabPoint
        )
        {
            if (tetherSources == null || tetherSources.Count == 0)
                throw new ArgumentException("TetherSources must not be null or empty.", nameof(tetherSources));
            if (pilotSeats == null || pilotSeats.Count == 0)
                throw new ArgumentException("PilotSeats must not be null or empty.", nameof(pilotSeats));

            TetherSources = tetherSources;
            PilotSeats = pilotSeats;
            FloodLights = floodLights ?? Array.Empty<VehicleParts.VehicleFloodLight>();
            InteriorLights = interiorLights ?? Array.Empty<Light>();
            NavigationPortLights = navigationPortLights ?? Array.Empty<GameObject>();
            NavigationStarboardLights = navigationStarboardLights ?? Array.Empty<GameObject>();
            NavigationPositionLights = navigationPositionLights ?? Array.Empty<GameObject>();
            NavigationWhiteStrobeLights = navigationWhiteStrobeLights ?? Array.Empty<GameObject>();
            NavigationRedStrobeLights = navigationRedStrobeLights ?? Array.Empty<GameObject>();
            ControlPanel = controlPanel;
            Fabricator = fabricator;
            ColorPicker = colorPicker;
            RespawnPoint = respawnPoint;
        }
    }
}
