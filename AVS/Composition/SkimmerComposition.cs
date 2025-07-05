using AVS.Engines;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AVS.Composition
{
    /// <summary>
    /// Vehicle composition for skimmers, which are small, fast vehicles designed for surface travel.
    /// </summary>
    public class SkimmerComposition : VehicleComposition
    {
        /// <summary>
        /// The pilot seats of the vehicle.
        /// </summary>
        public IReadOnlyList<VehicleParts.VehiclePilotSeat> PilotSeats { get; } = Array.Empty<VehicleParts.VehiclePilotSeat>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SkimmerComposition"/> class, representing the composition of a
        /// skimmer vehicle with its associated components and configuration.
        /// </summary>
        /// <remarks>This constructor initializes the skimmer vehicle with its various components,
        /// including storage, modules, hatches, pilot seats, and other optional features. The <paramref
        /// name="pilotSeats"/> parameter must not be null, and an empty collection will be used if no pilot seats are
        /// provided.</remarks>
        /// <param name="storageRootObject">The root <see cref="GameObject"/> that serves as the parent for all storage-related components.</param>
        /// <param name="modulesRootObject">The root <see cref="GameObject"/> that serves as the parent for all module-related components.</param>
        /// <param name="hatches">A collection of hatches (<see cref="VehicleParts.VehicleHatchStruct"/>) that provide entry and exit points
        /// for the vehicle.</param>
        /// <param name="pilotSeats">A collection of pilot seats (<see cref="VehicleParts.VehiclePilotSeat"/>) available in the vehicle. Cannot
        /// be null.</param>
        /// <param name="engine">The engine (<see cref="AbstractEngine"/>) that powers the vehicle. Must not be null</param>
        /// <param name="collisionModel">An optional <see cref="GameObject"/> representing the collision model of the vehicle. Can be null.</param>
        /// <param name="batteries">An optional collection of batteries (<see cref="VehicleParts.VehicleBattery"/>) used to power the vehicle.
        /// Can be null.</param>
        /// <param name="upgrades">An optional collection of upgrades (<see cref="VehicleParts.VehicleUpgrades"/>) installed on the vehicle.
        /// Can be null.</param>
        /// <param name="boundingBoxCollider">An optional <see cref="BoxCollider"/> defining the bounding box of the vehicle. Can be null.</param>
        /// <param name="waterClipProxies">An optional collection of <see cref="GameObject"/> proxies used for water clipping. Can be null.</param>
        /// <param name="innateStorages">An optional collection of innate storage components (<see cref="VehicleParts.VehicleStorage"/>) built into
        /// the vehicle. Can be null.</param>
        /// <param name="modularStorages">An optional collection of modular storage components (<see cref="VehicleParts.VehicleStorage"/>) that can be
        /// added to the vehicle. Can be null.</param>
        /// <param name="headLights">An optional collection of floodlights (<see cref="VehicleParts.VehicleFloodLight"/>) used as headlights for
        /// the vehicle. Can be null.</param>
        /// <param name="canopyWindows">An optional collection of <see cref="GameObject"/> instances representing the canopy windows of the vehicle.
        /// Can be null.</param>
        /// <param name="backupBatteries">An optional collection of backup batteries (<see cref="VehicleParts.VehicleBattery"/>) for the vehicle. Can
        /// be null.</param>
        /// <param name="steeringWheelLeftHandTarget">An optional <see cref="GameObject"/> representing the left-hand target for the steering wheel. Can be null.</param>
        /// <param name="steeringWheelRightHandTarget">An optional <see cref="GameObject"/> representing the right-hand target for the steering wheel. Can be null.</param>
        /// <param name="denyBuildingColliders">An optional collection of <see cref="Collider"/> instances that prevent building in certain areas. Can be
        /// null.</param>
        /// <param name="subNameDecals">An optional collection of <see cref="TMPro.TextMeshProUGUI"/> decals used to display the vehicle's name. Can
        /// be null.</param>
        /// <param name="lavaLarvaAttachPoints">An optional collection of <see cref="Transform"/> points where lava larvae can attach. Can be null.</param>
        /// <param name="leviathanGrabPoint">An optional <see cref="GameObject"/> representing the grab point for leviathans. Can be null.</param>
        public SkimmerComposition(
            GameObject storageRootObject,
            GameObject modulesRootObject,
            IReadOnlyList<VehicleParts.VehicleHatchStruct> hatches,
            IReadOnlyList<VehicleParts.VehiclePilotSeat> pilotSeats,
            AbstractEngine engine,
            GameObject collisionModel = null,
            IReadOnlyList<VehicleParts.VehicleBattery> batteries = null,
            IReadOnlyList<VehicleParts.VehicleUpgrades> upgrades = null,
            BoxCollider boundingBoxCollider = null,
            IReadOnlyList<GameObject> waterClipProxies = null,
            IReadOnlyList<VehicleParts.VehicleStorage> innateStorages = null,
            IReadOnlyList<VehicleParts.VehicleStorage> modularStorages = null,
            IReadOnlyList<VehicleParts.VehicleFloodLight> headLights = null,
            IReadOnlyList<GameObject> canopyWindows = null,
            IReadOnlyList<VehicleParts.VehicleBattery> backupBatteries = null,
            GameObject steeringWheelLeftHandTarget = null,
            GameObject steeringWheelRightHandTarget = null,
            IReadOnlyList<Collider> denyBuildingColliders = null,
            IReadOnlyList<TMPro.TextMeshProUGUI> subNameDecals = null,
            IReadOnlyList<Transform> lavaLarvaAttachPoints = null,
            GameObject leviathanGrabPoint = null
        ) : base(
            engine: engine,
            storageRootObject: storageRootObject,
            modulesRootObject: modulesRootObject,
            hatches: hatches,
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
            PilotSeats = pilotSeats ?? Array.Empty<VehicleParts.VehiclePilotSeat>();
        }


    }
}
