using System;
using System.Collections.Generic;
using UnityEngine;

namespace AVS.Composition
{
    public class SkimmerComposition : VehicleComposition
    {

        public IReadOnlyList<VehicleParts.VehiclePilotSeat> PilotSeats { get; } = Array.Empty<VehicleParts.VehiclePilotSeat>();

        public SkimmerComposition(
            GameObject storageRootObject,
            GameObject modulesRootObject,
            IReadOnlyList<VehicleParts.VehicleHatchStruct> hatches,
            IReadOnlyList<VehicleParts.VehiclePilotSeat> pilotSeats,
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
