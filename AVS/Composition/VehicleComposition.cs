using AVS.Engines;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AVS.Composition
{
    /// <summary>
    /// Objects, transforms, and components as identified by the derived mod vehicle.
    /// GameObjects, Transforms, and Components are expected to be contained by the vehicle
    /// or its children.
    /// </summary>
    public class VehicleComposition
    {
        /// <summary>
        /// The parent object for all storage objects.
        /// Must not be null. Must be different from the vehicle game object.
        /// </summary>
        public GameObject StorageRootObject { get; }

        /// <summary>
        /// The parent object for all modules.
        /// Must not be null. Must be different from the vehicle game object.
        /// </summary>
        public GameObject ModulesRootObject { get; }

        /// <summary>
        /// Base object containing colliders (and nothing else).
        /// AVS can do without but the Subnautica system uses this object to switch off colliders while docked.
        /// Therefore, this value must be set, even if the referenced game object contains nothing.
        /// Should not be the vehicle root as this would disable everything.
        /// </summary>
        public GameObject CollisionModel { get; }

        /// <summary>
        /// Entry/exit hatches for the submarine.
        /// Required not empty
        /// </summary>
        public IReadOnlyList<VehicleParts.VehicleHatchDefinition> Hatches { get; } = Array.Empty<VehicleParts.VehicleHatchDefinition>();

        /// <summary>
        /// Power cell definitions. Can be empty which disallows any batteries.
        /// </summary>
        public IReadOnlyList<VehicleParts.VehicleBatteryDefinition> Batteries { get; } = Array.Empty<VehicleParts.VehicleBatteryDefinition>();

        /// <summary>
        /// Upgrade module definitions. Can be empty which disallows any upgrades.
        /// </summary>
        public IReadOnlyList<VehicleParts.VehicleUpgrades> Upgrades { get; } = Array.Empty<VehicleParts.VehicleUpgrades>();

        /// <summary>
        /// Single box collider that contains the entire vehicle.
        /// While the code can handle this not being set, it really should be set.
        /// </summary>
        public BoxCollider? BoundingBoxCollider { get; }

        /// <summary>
        /// Empty game objects that each define a box that clips the water surface.
        /// These objects must not contain any components (renderers or otherwise).
        /// The position identifies their center, the size their extents.
        /// </summary>
        public IReadOnlyList<GameObject> WaterClipProxies { get; } = Array.Empty<GameObject>();


        /// <summary>
        /// Mobile water parks in the vehicle.
        /// </summary>
        public IReadOnlyList<VehicleParts.MobileWaterPark> WaterParks { get; }

        /// <summary>
        /// Storages that the vehicle always has.
        /// </summary>
        public IReadOnlyList<VehicleParts.VehicleStorage> InnateStorages { get; }
        /// <summary>
        /// Storages that can be added to the vehicle by the player.
        /// </summary>
        public IReadOnlyList<VehicleParts.VehicleStorage> ModularStorages { get; }

        /// <summary>
        /// Collection and configuration of headlights, to be rendered volumetrically while the player is outside the vehicle.
        /// </summary>
        public IReadOnlyList<VehicleParts.VehicleSpotLightDefinition> Headlights { get; } = Array.Empty<VehicleParts.VehicleSpotLightDefinition>();

        /// <summary>
        /// Window objects automatically hidden when the vehicle is being piloted as to avoid reflections.
        /// </summary>
        public IReadOnlyList<GameObject> CanopyWindows { get; } = Array.Empty<GameObject>();

        /// <summary>
        /// Batteries exclusively used for the AI. Not sure anyone uses these.
        /// </summary>
        public IReadOnlyList<VehicleParts.VehicleBatteryDefinition> BackupBatteries { get; } = Array.Empty<VehicleParts.VehicleBatteryDefinition>();


        /// <summary>
        /// Base object building is not permitted within these colliders.
        /// </summary>
        public IReadOnlyList<Collider> DenyBuildingColliders { get; } = Array.Empty<Collider>();

        /// <summary>
        /// Text output objects containing the vehicle name decals.
        /// </summary>
        public IReadOnlyList<TMPro.TextMeshProUGUI> SubNameDecals { get; } = Array.Empty<TMPro.TextMeshProUGUI>();

        /// <summary>
        /// Contains the attach points for the Lava Larvae (to suck away energy).
        /// If empty, the vehicle will not be attacked by Lava Larvae.
        /// </summary>
        public IReadOnlyList<Transform> LavaLarvaAttachPoints { get; } = Array.Empty<Transform>();
        /// <summary>
        /// Leviathan grab point, used by the Leviathan to grab the vehicle.
        /// If empty, the vehicle's own object is used as the grab point.
        /// </summary>
        public GameObject? LeviathanGrabPoint { get; }

        /// <summary>
        /// The engine that powers the vehicle. Must not be null.
        /// </summary>
        /// <remarks>
        /// The engine must be instantiated and attached during or before querying the vehicle's
        /// composition.
        /// As such, it is the only part that is not just derived from the model but rather newly
        /// created on demand.
        /// It is contained here because the mod vehicle requires it and it must be custom defined
        /// by the client vehicle.
        /// </remarks>
        public AbstractEngine Engine { get; }

        /// <summary>
        /// Constructs a VehicleComposition with required and optional parts.
        /// </summary>
        /// <param name="storageRootObject">The parent object for all storage objects. Must not be null and not the same as vehicle object.</param>
        /// <param name="modulesRootObject">The parent object for all modules. Must not be null and not the same as vehicle object.</param>
        /// <param name="collisionModel">Base object containing all colliders. Can be null.</param>
        /// <param name="hatches">Entry/exit hatches. Must not be null or empty.</param>
        /// <param name="batteries">Power cell definitions. Optional.</param>
        /// <param name="upgrades">Upgrade module definitions. Optional.</param>
        /// <param name="boundingBoxCollider">Single box collider for the vehicle. Can be null.</param>
        /// <param name="waterClipProxies">Water clip proxies. Optional.</param>
        /// <param name="innateStorages">Innate storages. Optional.</param>
        /// <param name="modularStorages">Modular storages. Optional.</param>
        /// <param name="headlights">Headlights. Optional.</param>
        /// <param name="canopyWindows">Canopy windows. Optional.</param>
        /// <param name="backupBatteries">Backup batteries. Optional.</param>
        /// <param name="denyBuildingColliders">Deny building colliders. Optional.</param>
        /// <param name="subNameDecals">Sub name decals. Optional.</param>
        /// <param name="lavaLarvaAttachPoints">Lava larva attach points. Optional.</param>
        /// <param name="leviathanGrabPoint">Leviathan grab point. Optional.</param>
        /// <param name="waterParks">Mobile water parks. Optional.</param>
        /// <param name="engine">The engine that powers the vehicle. Must not be null.</param>
        public VehicleComposition(
            GameObject storageRootObject,
            GameObject modulesRootObject,
            IReadOnlyList<VehicleParts.VehicleHatchDefinition> hatches,
            AbstractEngine engine,
            GameObject collisionModel,
            IReadOnlyList<VehicleParts.VehicleBatteryDefinition>? batteries = null,
            IReadOnlyList<VehicleParts.VehicleUpgrades>? upgrades = null,
            BoxCollider? boundingBoxCollider = null,
            IReadOnlyList<GameObject>? waterClipProxies = null,
            IReadOnlyList<VehicleParts.VehicleStorage>? innateStorages = null,
            IReadOnlyList<VehicleParts.VehicleStorage>? modularStorages = null,
            IReadOnlyList<VehicleParts.VehicleSpotLightDefinition>? headlights = null,
            IReadOnlyList<GameObject>? canopyWindows = null,
            IReadOnlyList<VehicleParts.VehicleBatteryDefinition>? backupBatteries = null,
            IReadOnlyList<Collider>? denyBuildingColliders = null,
            IReadOnlyList<TMPro.TextMeshProUGUI>? subNameDecals = null,
            IReadOnlyList<Transform>? lavaLarvaAttachPoints = null,
            GameObject? leviathanGrabPoint = null,
            IReadOnlyList<VehicleParts.MobileWaterPark>? waterParks = null
        )
        {
            if (!storageRootObject)
                throw new ArgumentNullException(nameof(storageRootObject));
            if (!modulesRootObject)
                throw new ArgumentNullException(nameof(modulesRootObject));
            if (hatches == null || hatches.Count == 0)
                throw new ArgumentException("Hatches must not be null or empty.", nameof(hatches));
            if (!engine)
                throw new ArgumentNullException(nameof(engine));
            Engine = engine;
            LeviathanGrabPoint = leviathanGrabPoint;
            StorageRootObject = storageRootObject;
            ModulesRootObject = modulesRootObject;
            CollisionModel = collisionModel;
            Hatches = hatches;
            Batteries = batteries ?? Array.Empty<VehicleParts.VehicleBatteryDefinition>();
            Upgrades = upgrades ?? Array.Empty<VehicleParts.VehicleUpgrades>();
            BoundingBoxCollider = boundingBoxCollider;
            WaterClipProxies = waterClipProxies ?? Array.Empty<GameObject>();
            InnateStorages = innateStorages ?? Array.Empty<VehicleParts.VehicleStorage>();
            ModularStorages = modularStorages ?? Array.Empty<VehicleParts.VehicleStorage>();
            Headlights = headlights ?? Array.Empty<VehicleParts.VehicleSpotLightDefinition>();
            CanopyWindows = canopyWindows ?? Array.Empty<GameObject>();
            BackupBatteries = backupBatteries ?? Array.Empty<VehicleParts.VehicleBatteryDefinition>();
            DenyBuildingColliders = denyBuildingColliders ?? Array.Empty<Collider>();
            SubNameDecals = subNameDecals ?? Array.Empty<TMPro.TextMeshProUGUI>();
            LavaLarvaAttachPoints = lavaLarvaAttachPoints ?? Array.Empty<Transform>();
            WaterParks = waterParks ?? Array.Empty<VehicleParts.MobileWaterPark>();

            if (Upgrades == null)
                throw new InvalidOperationException($"Something went wrong. Upgrades should not be null at this point. {upgrades}");
        }






    }
}
