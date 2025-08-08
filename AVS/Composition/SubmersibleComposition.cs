using AVS.Engines;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AVS.Composition
{
    /// <summary>
    /// The vehicle composition for submersible vehicles.
    /// </summary>
    public class SubmersibleComposition : VehicleComposition
    {
        /// <summary>
        /// The pilot seat of the vehicle.
        /// Must not be null.
        /// </summary>
        public VehicleParts.Helm PilotSeat { get; }


        /// <summary>
        /// Represents the composition of a submersible vehicle, including all required and optional parts.
        /// Inherits from <see cref="VehicleComposition"/> and adds a required pilot seat.
        /// </summary>
        /// <param name="pilotSeat">
        /// The pilot seat of the vehicle. Must not be null and must have a non-null <c>Seat</c> property.
        /// </param>
        /// <param name="storageRootObject">
        /// The parent object for all storage objects. Must not be null and not the same as the vehicle object.
        /// </param>
        /// <param name="modulesRootObject">
        /// The parent object for all modules. Must not be null and not the same as the vehicle object.
        /// </param>
        /// <param name="hatches">
        /// Entry/exit hatches for the submarine. Must not be null or empty.
        /// </param>
        /// <param name="collisionModel">
        /// Object containing all colliders. Must not be null. Should not be the same as the vehicle object.
        /// </param>
        /// <param name="batteries">
        /// Power cell definitions. Optional. Can be null or empty.
        /// </param>
        /// <param name="upgrades">
        /// Upgrade module definitions. Optional. Can be null or empty.
        /// </param>
        /// <param name="boundingBoxCollider">
        /// Single box collider for the vehicle. Can be null.
        /// </param>
        /// <param name="waterClipProxies">
        /// Water clip proxies. Optional. Can be null or empty.
        /// </param>
        /// <param name="innateStorages">
        /// Storages that the vehicle always has. Optional. Can be null or empty.
        /// </param>
        /// <param name="modularStorages">
        /// Storages that can be added to the vehicle by the player. Optional. Can be null or empty.
        /// </param>
        /// <param name="headlights">
        /// Collection and configuration of headlights. Optional. Can be null or empty.
        /// </param>
        /// <param name="canopyWindows">
        /// Window objects automatically hidden when the vehicle is being piloted. Optional. Can be null or empty.
        /// </param>
        /// <param name="backupBatteries">
        /// Batteries exclusively used for the AI. Optional. Can be null or empty.
        /// </param>
        /// <param name="denyBuildingColliders">
        /// Colliders within which building is not permitted. Optional. Can be null or empty.
        /// </param>
        /// <param name="subNameDecals">
        /// Text output objects containing the vehicle name decals. Optional. Can be null or empty.
        /// </param>
        /// <param name="lavaLarvaAttachPoints">
        /// Attach points for Lava Larvae. Optional. Can be null or empty.
        /// </param>
        /// <param name="leviathanGrabPoint">
        /// Leviathan grab point. Optional.
        /// </param>
        /// <param name="engine">The engine that powers the vehicle. Must not be null.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="pilotSeat"/> is null or if <c>PilotSeat.Seat</c> is null.
        /// </exception>
        public SubmersibleComposition(
            VehicleParts.Helm pilotSeat,
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
            GameObject? leviathanGrabPoint = null
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
            headlights: headlights,
            canopyWindows: canopyWindows,
            backupBatteries: backupBatteries,
            denyBuildingColliders: denyBuildingColliders,
            subNameDecals: subNameDecals,
            lavaLarvaAttachPoints: lavaLarvaAttachPoints,
            leviathanGrabPoint: leviathanGrabPoint
        )
        {
            if (pilotSeat.Root == null)
                throw new ArgumentNullException(nameof(pilotSeat), "PilotSeat.Seat must not be null.");
            PilotSeat = pilotSeat;
        }

    }
}
