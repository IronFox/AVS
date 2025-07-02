using AVS.Assets;
using System;
using System.Collections.Generic;
using UnityEngine;
using static AVS.ModVehicle;

namespace AVS.Config
{
    public class VehicleConfiguration
    {
        public GameObject StorageRootObject { get; } //required, must be different from vehicle go
        public GameObject ModulesRootObject { get; } //required, must be different from vehicle go
        public GameObject VehicleModel { get; } //required
        public GameObject CollisionModel { get; }//should not be null
        public IReadOnlyList<VehicleParts.VehicleBattery> Batteries { get; } = Array.Empty<VehicleParts.VehicleBattery>();
        public IReadOnlyList<VehicleParts.VehicleUpgrades> Upgrades { get; } = Array.Empty<VehicleParts.VehicleUpgrades>();
        public BoxCollider BoundingBoxCollider { get; } //should not be null
        public Atlas.Sprite PingSprite { get; } = Assets.StaticAssets.DefaultPingSprite;
        public Sprite SaveFileSprite { get; } = Assets.StaticAssets.DefaultSaveFileSprite; // I think I can use SpriteHelper.CreateSpriteFromAtlasSprite for this now. But do I want to?
        public IReadOnlyList<GameObject> WaterClipProxies { get; } = Array.Empty<GameObject>();
        public IReadOnlyList<VehicleParts.VehicleStorage> InnateStorages { get; } = Array.Empty<VehicleParts.VehicleStorage>();
        public IReadOnlyList<VehicleParts.VehicleStorage> ModularStorages { get; } = Array.Empty<VehicleParts.VehicleStorage>();
        public IReadOnlyList<VehicleParts.VehicleFloodLight> HeadLights { get; } = Array.Empty<VehicleParts.VehicleFloodLight>();
        public IReadOnlyList<GameObject> CanopyWindows { get; } = Array.Empty<GameObject>();
        public IReadOnlyDictionary<TechType, int> Recipe { get; } = new Dictionary<TechType, int>() { { TechType.Titanium, 1 } };
        public IReadOnlyList<VehicleParts.VehicleBattery> BackupBatteries { get; } = Array.Empty<VehicleParts.VehicleBattery>();
        public Sprite UnlockedSprite { get; } = null;
        public IReadOnlyList<Transform> LavaLarvaAttachPoints { get; }
        public string Description { get; } = "A vehicle";
        public string EncyclopediaEntry { get; } = "";
        public Sprite EncyclopediaImage { get; } = null;
        public Atlas.Sprite CraftingSprite { get; } = StaticAssets.ModVehicleIcon;
        public Sprite ModuleBackgroundImage { get; } = SpriteHelper.GetSpriteRaw("Sprites/VFModuleBackground.png");
        public TechType UnlockedWith { get; } = TechType.Constructor;
        public int BaseCrushDepth { get; } = 250;   //required > 0
        public int MaxHealth { get; } = 100;    //required > 0
        public int CrushDamage { get; } = 10; //= MaxHealth / 15;
        public float GhostAdultBiteDamage { get; } = 150f;
        public float GhostJuvenileBiteDamage { get; } = 100f;
        public float ReaperBiteDamage { get; } = 120f;
        public int Mass { get; } = 1000;    //required > 0
        public int NumModules { get; } = 4;
        public string UnlockedMessage { get; } = "New vehicle blueprint acquired";
        public int CrushDepthUpgrade1 { get; } = 300;
        public int CrushDepthUpgrade2 { get; } = 300;
        public int CrushDepthUpgrade3 { get; } = 300;
        public int CrushPeriod { get; } = 1;
        public PilotingStyle PilotingStyle { get; } = PilotingStyle.Other;
        public IReadOnlyList<Collider> DenyBuildingColliders { get; } = Array.Empty<Collider>();
        public float TimeToConstruct { get; } = 15f; // Seamoth : 10 seconds, Cyclops : 20, Rocket Base : 25
        public Color ConstructionGhostColor { get; } = Color.black;
        public Color ConstructionWireframeColor { get; } = Color.black;
        public bool CanLeviathanGrab { get; set; } = true;
        public bool CanMoonpoolDock { get; set; } = true;
        public IReadOnlyList<TMPro.TextMeshProUGUI> SubNameDecals { get; } = null;
        public Quaternion CyclopsDockRotation { get; } = Quaternion.identity;

        /// <summary>
        /// This is what the player's left hand will 'grab' while you pilot.
        /// Can be null if the vehicle does not have a steering wheel.
        /// </summary>
        public GameObject SteeringWheelLeftHandTarget { get; } = null;
        /// <summary>
        /// This is what the player's right hand will 'grab' while you pilot.
        /// Can be null if the vehicle does not have a steering wheel.
        /// </summary>
        public GameObject SteeringWheelRightHandTarget { get; } = null;

    }
}
