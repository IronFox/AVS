using AVS.Assets;
using AVS.BaseVehicle;
using AVS.Log;
using AVS.Util;
using AVS.VehicleTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
//using AVS.Localization;

namespace AVS
{
    public struct VehicleEntry
    {
        public VehicleEntry(AvsVehicle inputMv, int id, PingType pt_in, Atlas.Sprite? sprite, TechType tt = (TechType)0)
        {
            mv = inputMv ?? throw new ArgumentException("Vehicle Entry cannot take a null mod vehicle");
            unique_id = id;
            pt = pt_in;
            name = mv.name;
            techType = tt;
            ping_sprite = sprite;
        }
        public AvsVehicle mv;
        public string name;
        public int unique_id;
        public PingType pt;
        public Atlas.Sprite? ping_sprite;
        public TechType techType;
    }

    public static class AvsVehicleBuilder
    {
        public static GameObject? UpgradeConsole { get; internal set; }

        private static int numVehicleTypes = 0;
        public static List<AvsVehicle> prefabs = new List<AvsVehicle>();

        public const EquipmentType ModuleType = (EquipmentType)625;
        public const EquipmentType ArmType = (EquipmentType)626;
        public const TechType InnateStorage = (TechType)0x4100;

        public static IEnumerator Prefabricate(AvsVehicle mv, PingType pingType, bool verbose)
        {
            mv.OnAwakeOrPrefabricate();
            VehicleRegistrar.VerboseLog(VehicleRegistrar.LogType.Log, verbose, "Prefabricating the " + mv.gameObject.name);
            yield return SeamothHelper.Coroutine;
            var seamoth = SeamothHelper.RequireSeamoth;
            if (!Instrument(mv, pingType, seamoth))
            {
                LogWriter.Default.Error("Failed to instrument the vehicle: " + mv.gameObject.name);
                Logger.LoopMainMenuError($"AVS: Failed prefabrication of {mv.GetType().Name}. Not registered. See log.", mv.gameObject.name);
                yield break;
            }
            prefabs.Add(mv);
            VehicleEntry ve = new VehicleEntry(mv, numVehicleTypes, pingType, mv.Config.PingSprite);
            numVehicleTypes++;
            VehicleEntry naiveVE = new VehicleEntry(ve.mv, ve.unique_id, ve.pt, ve.ping_sprite, TechType.None);
            VehicleManager.VehicleTypes.Add(naiveVE); // must add/remove this vehicle entry so that we can call VFConfig.Setup.
            VehicleNautilusInterface.PatchCraftable(ref ve, verbose);
            VehicleManager.VehicleTypes.Remove(naiveVE); // must remove this vehicle entry bc PatchCraftable adds a more complete one (with tech type)
            mv.gameObject.SetActive(true);
        }

        #region setup_funcs
        public static bool SetupObjects(AvsVehicle mv)
        {
            // Wow, look at this:
            // This Nautilus line might be super nice if it works for us
            // allow it to be opened as a storage container:
            //PrefabUtils.AddStorageContainer(obj, "StorageRoot", "TallLocker", 3, 8, true);

            if (!mv.ReSetupInnateStorages())
                return false;

            if (!mv.ReSetupModularStorages())
                return false;

            try
            {
                foreach (VehicleParts.VehicleUpgrades vu in mv.Com.Upgrades)
                {
                    VehicleUpgradeConsoleInput vuci = vu.Interface.EnsureComponent<VehicleUpgradeConsoleInput>();
                    vuci.flap = vu.Flap.transform;
                    vuci.anglesOpened = vu.AnglesOpened;
                    vuci.anglesClosed = vu.AnglesClosed;
                    vuci.collider = vuci.GetComponentInChildren<Collider>();
                    mv.upgradesInput = vuci;
                    var up = vu.Interface.EnsureComponent<UpgradeProxy>();
                    if (vu.ModuleProxies != null)
                        up.proxies = vu.ModuleProxies;

                    SaveLoad.SaveLoadUtils.EnsureUniqueNameAmongSiblings(vu.Interface.transform);
                    vu.Interface.EnsureComponent<SaveLoad.AvsUpgradesIdentifier>();
                }
                if (mv.Com.Upgrades.Count == 0)
                {
                    VehicleUpgradeConsoleInput vuci = mv.VehicleRoot.EnsureComponent<VehicleUpgradeConsoleInput>();
                    vuci.enabled = false;
                    vuci.collider = mv.VehicleRoot.AddComponent<BoxCollider>();
                    ((BoxCollider)vuci.collider).size = Vector3.zero;
                    mv.upgradesInput = vuci;
                }
            }
            catch (Exception e)
            {
                LogWriter.Default.Error("There was a problem setting up the Upgrades Interface. Check VehicleUpgrades.Interface and .Flap", e);
                return false;
            }
            if (mv.Com.BoundingBoxCollider != null)
                mv.Com.BoundingBoxCollider.enabled = false;
            return true;
        }
        public static bool SetupObjects(Submarine mv)
        {
            try
            {
                for (int i = 0; i < mv.Com.Helms.Count; i++)
                {
                    VehicleParts.Helm ps = mv.Com.Helms[i];
                    var pt = ps.Root.EnsureComponent<PilotingTrigger>();
                    pt.mv = mv;
                    pt.helmIndex = i;
                }
            }
            catch (Exception e)
            {
                LogWriter.Default.Error("There was a problem setting up the PilotSeats. Check VehiclePilotSeat.Seat", e);
                return false;
            }
            try
            {
                for (int i = 0; i < mv.Com.Hatches.Count; i++)
                {
                    var hatch = mv.Com.Hatches[i].Hatch.EnsureComponent<VehicleHatch>();
                    hatch.mv = mv;
                    hatch.hatchIndex = i;
                }
            }
            catch (Exception e)
            {
                LogWriter.Default.Error("There was a problem setting up the Hatches. Check VehicleHatchStruct.Hatch", e);
                return false;
            }
            // Configure the Control Panel
            try
            {
                if (mv.Com.ControlPanel != null)
                {
                    mv.controlPanelLogic = mv.Com.ControlPanel.EnsureComponent<ControlPanel>();
                    mv.controlPanelLogic.mv = mv;
                    if (mv.transform.Find("Control-Panel-Location") != null)
                    {
                        mv.Com.ControlPanel.transform.localPosition = mv.transform.Find("Control-Panel-Location").localPosition;
                        mv.Com.ControlPanel.transform.localRotation = mv.transform.Find("Control-Panel-Location").localRotation;
                        GameObject.Destroy(mv.transform.Find("Control-Panel-Location").gameObject);
                    }
                }
            }
            catch (Exception e)
            {
                LogWriter.Default.Error("There was a problem setting up the Control Panel. Check ModVehicle.ControlPanel and ensure \"Control-Panel-Location\" exists at the top level of your model. While you're at it, check that \"Fabricator-Location\" is at the top level of your model too.", e);
                return false;
            }
            return true;
        }
        public static bool SetupObjects(Submersible mv)
        {
            try
            {
                mv.playerPosition = mv.Com.PilotSeat.PlayerControlLocation;
                PilotingTrigger pt = mv.Com.PilotSeat.Root.EnsureComponent<PilotingTrigger>();
                pt.mv = mv;
            }
            catch (Exception e)
            {
                LogWriter.Default.Error("There was a problem setting up the PilotSeats. Check VehiclePilotSeat.Seat", e);
                return false;
            }
            try
            {
                for (int i = 0; i < mv.Com.Hatches.Count; i++)
                {
                    var hatch = mv.Com.Hatches[i].Hatch.EnsureComponent<VehicleHatch>();
                    hatch.mv = mv;
                    hatch.hatchIndex = i;
                }
            }
            catch (Exception e)
            {
                LogWriter.Default.Error("There was a problem setting up the Hatches. Check VehicleHatchStruct.Hatch", e);
                return false;
            }
            // Configure the Control Panel
            return true;
        }
        public static void SetupEnergyInterface(AvsVehicle mv)
        {
            var seamothEnergyMixin = SeamothHelper.RequireSeamoth.GetComponent<EnergyMixin>();
            List<EnergyMixin> energyMixins = new List<EnergyMixin>();
            if (mv.Com.Batteries.Count() == 0)
            {
                // Configure energy mixin for this battery slot
                var energyMixin = mv.gameObject.AddComponent<VehicleComponents.ForeverBattery>();
                energyMixin.storageRoot = mv.Com.StorageRootObject.GetComponent<ChildObjectIdentifier>();
                energyMixin.defaultBattery = seamothEnergyMixin.defaultBattery;
                energyMixin.compatibleBatteries = seamothEnergyMixin.compatibleBatteries;
                energyMixin.soundPowerUp = seamothEnergyMixin.soundPowerUp;
                energyMixin.soundPowerDown = seamothEnergyMixin.soundPowerDown;
                energyMixin.soundBatteryAdd = seamothEnergyMixin.soundBatteryAdd;
                energyMixin.soundBatteryRemove = seamothEnergyMixin.soundBatteryRemove;
                energyMixin.batteryModels = seamothEnergyMixin.batteryModels;
                energyMixin.controlledObjects = new GameObject[] { };
                energyMixins.Add(energyMixin);
            }
            foreach (VehicleParts.VehicleBattery vb in mv.Com.Batteries)
            {
                // Configure energy mixin for this battery slot
                var energyMixin = vb.BatterySlot.EnsureComponent<EnergyMixin>();
                energyMixin.storageRoot = mv.Com.StorageRootObject.GetComponent<ChildObjectIdentifier>();
                energyMixin.defaultBattery = seamothEnergyMixin.defaultBattery;
                energyMixin.compatibleBatteries = seamothEnergyMixin.compatibleBatteries;
                energyMixin.soundPowerUp = seamothEnergyMixin.soundPowerUp;
                energyMixin.soundPowerDown = seamothEnergyMixin.soundPowerDown;
                energyMixin.soundBatteryAdd = seamothEnergyMixin.soundBatteryAdd;
                energyMixin.soundBatteryRemove = seamothEnergyMixin.soundBatteryRemove;
                energyMixin.batteryModels = seamothEnergyMixin.batteryModels;
                energyMixins.Add(energyMixin);
                var tmp = vb.BatterySlot.EnsureComponent<VehicleBatteryInput>();
                tmp.mixin = energyMixin;
                tmp.tooltip = "VFVehicleBattery";

                var model = vb.BatterySlot.gameObject.EnsureComponent<StorageComponents.BatteryProxy>();
                model.proxy = vb.BatteryProxy;
                model.mixin = energyMixin;

                SaveLoad.SaveLoadUtils.EnsureUniqueNameAmongSiblings(vb.BatterySlot.transform);
                vb.BatterySlot.EnsureComponent<SaveLoad.AvsBatteryIdentifier>();
            }
            // Configure energy interface
            var eInterf = mv.gameObject.EnsureComponent<EnergyInterface>();
            eInterf.sources = energyMixins.ToArray();
            mv.energyInterface = eInterf;

            mv.chargingSound = mv.gameObject.AddComponent<FMOD_CustomLoopingEmitter>();
            mv.chargingSound.asset = SeamothHelper.RequireSeamoth.GetComponent<SeaMoth>().chargingSound.asset;
        }
        public static void SetupAIEnergyInterface(AvsVehicle mv, GameObject seamoth)
        {
            mv.SetupAIEnergyInterface(seamoth);

        }
        public static void SetupLightSounds(AvsVehicle mv)
        {
            mv.Log.Debug("Setting up light sounds for " + mv.name);
            FMOD_StudioEventEmitter[] fmods = SeamothHelper.RequireSeamoth.GetComponents<FMOD_StudioEventEmitter>();
            foreach (FMOD_StudioEventEmitter fmod in fmods)
            {
                if (fmod.asset.name == "seamoth_light_on")
                {
                    mv.Log.Debug("Found light on sound for " + mv.name);
                    var ce = mv.gameObject.AddComponent<FMOD_CustomEmitter>();
                    ce.asset = fmod.asset;
                    mv.lightsOnSound = ce;
                }
                else if (fmod.asset.name == "seamoth_light_off")
                {
                    mv.Log.Debug("Found light off sound for " + mv.name);
                    var ce = mv.gameObject.AddComponent<FMOD_CustomEmitter>();
                    ce.asset = fmod.asset;
                    mv.lightsOffSound = ce;
                }
            }
            if (mv.lightsOnSound == null || mv.lightsOffSound == null)
            {
                mv.Log.Error("Failed to find light sounds for " + mv.name);
            }
        }
        public static void SetupHeadLights(AvsVehicle mv)
        {
            GameObject seamothHeadLight = SeamothHelper.RequireSeamoth.transform.Find("lights_parent/light_left").gameObject;
            if (mv.Com.HeadLights != null)
            {
                foreach (VehicleParts.VehicleFloodLight pc in mv.Com.HeadLights)
                {
                    seamothHeadLight.GetComponent<LightShadowQuality>().CopyComponentWithFieldsTo(pc.Light);
                    var thisLight = pc.Light.EnsureComponent<Light>();
                    thisLight.type = LightType.Spot;
                    thisLight.spotAngle = pc.Angle;
                    thisLight.innerSpotAngle = pc.Angle * .75f;
                    thisLight.color = pc.Color;
                    thisLight.intensity = pc.Intensity;
                    thisLight.range = pc.Range;
                    thisLight.shadows = LightShadows.Hard;
                    thisLight.gameObject.SetActive(false);

                    var RLS = mv.gameObject.AddComponent<RegistredLightSource>();
                    RLS.hostLight = thisLight;
                }
            }
        }
        public static void SetupFloodLights(Submarine mv, GameObject seamoth)
        {
            GameObject seamothHeadLight = seamoth.transform.Find("lights_parent/light_left").gameObject;
            if (mv.Com.FloodLights != null)
            {
                foreach (VehicleParts.VehicleFloodLight pc in mv.Com.FloodLights)
                {
                    seamothHeadLight
                        .GetComponent<LightShadowQuality>()
                        .CopyComponentWithFieldsTo(pc.Light);
                    var thisLight = pc.Light.EnsureComponent<Light>();
                    thisLight.type = LightType.Spot;
                    thisLight.spotAngle = pc.Angle;
                    thisLight.innerSpotAngle = pc.Angle * .75f;
                    thisLight.color = pc.Color;
                    thisLight.intensity = pc.Intensity;
                    thisLight.range = pc.Range;
                    thisLight.shadows = LightShadows.Hard;
                    pc.Light.SetActive(false);

                    var RLS = mv.gameObject.AddComponent<RegistredLightSource>();
                    RLS.hostLight = thisLight;
                }
            }
        }
        public static void SetupVolumetricLights(AvsVehicle mv)
        {
            mv.SetupVolumetricLights();

        }
        public static void SetupLiveMixin(AvsVehicle mv)
        {
            var liveMixin = mv.gameObject.EnsureComponent<LiveMixin>();
            var lmData = ScriptableObject.CreateInstance<LiveMixinData>();
            lmData.canResurrect = true;
            lmData.broadcastKillOnDeath = true;
            lmData.destroyOnDeath = false;
            // NEWNEW
            // What's going to happen when a vdehicle dies now?
            //lmData.explodeOnDestroy = true;
            lmData.invincibleInCreative = true;
            lmData.weldable = true;
            lmData.minDamageForSound = 20f;
            /*
             * Other Max Health Values
             * Seamoth: 200
             * Prawn: 600
             * Odyssey: 667
             * Atrama: 1000
             * Abyss: 1250
             * Cyclops: 1500
             */
            lmData.maxHealth = mv.Config.MaxHealth;
            liveMixin.health = mv.Config.MaxHealth;
            liveMixin.data = lmData;
            mv.liveMixin = liveMixin;
        }
        public static void SetupRigidbody(AvsVehicle mv)
        {
            var rb = mv.gameObject.EnsureComponent<Rigidbody>();
            /* 
             * For reference,
             * Cyclop: 12000
             * Abyss: 5000
             * Atrama: 4250
             * Odyssey: 3500
             * Prawn: 1250
             * Seamoth: 800
             */
            rb.mass = mv.Config.Mass;
            rb.drag = 10f;
            rb.angularDrag = 10f;
            rb.useGravity = false;
            mv.useRigidbody = rb;
        }

        public static void SetupWorldForces(AvsVehicle mv, GameObject seamoth)
        {
            LogWriter.Default.Write("Setting up world forces for " + mv.name);
            mv.worldForces = seamoth
                        .GetComponent<SeaMoth>()
                        .worldForces
                        .CopyComponentWithFieldsTo(mv.gameObject);
            mv.worldForces!.useRigidbody = mv.useRigidbody;
            mv.worldForces.underwaterGravity = 0f;
            mv.worldForces.aboveWaterGravity = 9.8f;
            mv.worldForces.waterDepth = 0f;
        }
        public static void SetupHudPing(AvsVehicle mv, PingType pingType)
        {
            mv.PrefabSetupHudPing(pingType);
            VehicleManager.MvPings.Add(mv.HudPingInstance);
        }
        public static void SetupVehicleConfig(AvsVehicle mv, GameObject seamoth)
        {
            // add various vehicle things
            mv.stabilizeRoll = true;
            mv.controlSheme = (Vehicle.ControlSheme)12;
            mv.mainAnimator = mv.gameObject.EnsureComponent<Animator>();
            mv.SetupAmbienceSound(seamoth.GetComponent<SeaMoth>().ambienceSound);
            mv.splashSound = seamoth.GetComponent<SeaMoth>().splashSound;
            // TODO
            //atrama.vehicle.bubbles = CopyComponent<ParticleSystem>(seamoth.GetComponent<SeaMoth>().bubbles, atrama.vehicle.gameObject);
        }
        public static void SetupCrushDamage(AvsVehicle mv, GameObject seamoth)
        {
            var container = new GameObject("CrushDamageContainer");
            container.transform.SetParent(mv.transform);
            var ce = container.AddComponent<FMOD_CustomEmitter>();
            ce.restartOnPlay = true;
            foreach (var thisCE in seamoth.GetComponentsInChildren<FMOD_CustomEmitter>())
            {
                if (thisCE.name == "crushDamageSound")
                {
                    ce.asset = thisCE.asset;
                    LogWriter.Default.Write("Found crush damage sound for " + mv.name);
                }
            }
            if (ce.asset == null)
            {
                LogWriter.Default.Error("Failed to find crush damage sound for " + mv.name);
            }
            /* For reference,
             * Prawn dies from max health in 3:00 minutes.
             * Seamoth in 0:30
             * Cyclops in 3:45
             * So ModVehicles can die in 3:00 as well
             */
            mv.crushDamageEmitter = container;
            mv.crushDamage = mv.gameObject.EnsureComponent<CrushDamage>();
            mv.crushDamage.soundOnDamage = ce;
            mv.crushDamage.kBaseCrushDepth = mv.Config.BaseCrushDepth;
            mv.crushDamage.damagePerCrush = mv.Config.CrushDamage;
            mv.crushDamage.crushPeriod = mv.Config.CrushDamageFrequency;
            mv.crushDamage.vehicle = mv;
            mv.crushDamage.liveMixin = mv.liveMixin;
            // TODO: this is of type VoiceNotification
            mv.crushDamage.crushDepthUpdate = null;

            LogWriter.Default.Write("Crush sound registered: " + mv.crushDamage.soundOnDamage.NiceName());

        }
        public static void SetupWaterClipping(AvsVehicle mv, GameObject seamoth)
        {
            if (mv.Com.WaterClipProxies != null)
            {
                // Enable water clipping for proper interaction with the surface of the ocean
                WaterClipProxy seamothWCP = seamoth.GetComponentInChildren<WaterClipProxy>();
                foreach (GameObject proxy in mv.Com.WaterClipProxies)
                {
                    WaterClipProxy waterClip = proxy.AddComponent<WaterClipProxy>();
                    waterClip.shape = WaterClipProxy.Shape.Box;
                    //"""Apply the seamoth's clip material. No idea what shader it uses or what settings it actually has, so this is an easier option. Reuse the game's assets.""" -Lee23
                    waterClip.clipMaterial = seamothWCP.clipMaterial;
                    //"""You need to do this. By default the layer is 0. This makes it displace everything in the default rendering layer. We only want to displace water.""" -Lee23
                    waterClip.gameObject.layer = seamothWCP.gameObject.layer;
                }
            }
        }
        public static void SetupSubName(AvsVehicle mv)
        {
            var subname = mv.gameObject.EnsureComponent<SubName>();
            subname.pingInstance = mv.HudPingInstance;
            subname.colorsInitialized = 0;
            subname.hullName = mv.Com.StorageRootObject.AddComponent<TMPro.TextMeshProUGUI>(); // DO NOT push a TMPro.TextMeshProUGUI on the root vehicle object!!!
            mv.subName = subname;
            mv.SetName(mv.vehicleDefaultName);
        }
        public static void SetupCollisionSound(AvsVehicle mv, GameObject seamoth)
        {
            var colsound = mv.gameObject.EnsureComponent<CollisionSound>();
            var seamothColSound = seamoth.GetComponent<CollisionSound>();
            colsound.hitSoundSmall = seamothColSound.hitSoundSmall;
            colsound.hitSoundSlow = seamothColSound.hitSoundSlow;
            colsound.hitSoundMedium = seamothColSound.hitSoundMedium;
            colsound.hitSoundFast = seamothColSound.hitSoundFast;
        }
        public static void SetupOutOfBoundsWarp(AvsVehicle mv)
        {
            mv.gameObject.EnsureComponent<OutOfBoundsWarp>();
        }
        public static void SetupConstructionObstacle(AvsVehicle mv)
        {
            var co = mv.gameObject.EnsureComponent<ConstructionObstacle>();
            co.reason = mv.name + " is in the way.";
        }
        public static void SetupSoundOnDamage(AvsVehicle mv, GameObject seamoth)
        {
            // TODO: we could have unique sounds for each damage type
            // TODO: this might not work, might need to put it in a VehicleStatusListener
            var sod = mv.gameObject.EnsureComponent<SoundOnDamage>();
            sod.damageType = DamageType.Normal;
            sod.sound = seamoth.GetComponent<SoundOnDamage>().sound;
        }
        public static void SetupDealDamageOnImpact(AvsVehicle mv)
        {
            var ddoi = mv.gameObject.EnsureComponent<DealDamageOnImpact>();
            // NEWNEW
            // ddoi.damageTerrain = true;
            ddoi.speedMinimumForSelfDamage = 4;
            ddoi.speedMinimumForDamage = 2;
            ddoi.affectsEcosystem = true;
            ddoi.minimumMassForDamage = 5;
            ddoi.mirroredSelfDamage = true;
            ddoi.mirroredSelfDamageFraction = 0.5f;
            ddoi.capMirrorDamage = -1;
            ddoi.minDamageInterval = 0;
            ddoi.timeLastDamage = 0;
            ddoi.timeLastDamagedSelf = 0;
            ddoi.prevPosition = Vector3.zero;
            ddoi.prevPosition = Vector3.zero;
            ddoi.allowDamageToPlayer = false;
        }
        public static void SetupDamageComponents(AvsVehicle mv, GameObject seamoth)
        {
            // add vfxvehicledamages... or not

            // add temperaturedamage
            var tempdamg = mv.gameObject.EnsureComponent<TemperatureDamage>();
            tempdamg.lavaDatabase = seamoth.GetComponent<TemperatureDamage>().lavaDatabase;
            tempdamg.liveMixin = mv.liveMixin;
            tempdamg.baseDamagePerSecond = 2.0f; // 10 times what the seamoth takes, since the Atrama 
            // the following configurations are the same values the seamoth takes
            tempdamg.minDamageTemperature = 70f;
            tempdamg.onlyLavaDamage = false;
            tempdamg.timeDamageStarted = -1000;
            tempdamg.timeLastDamage = 0;
            tempdamg.player = null;

            // add ecotarget
            var et = mv.gameObject.EnsureComponent<EcoTarget>();
            et.type = EcoTargetType.Shark; // same as seamoth (lol)
            et.nextUpdateTime = 0f;

            // add creatureutils
            var cr = mv.gameObject.EnsureComponent<CreatureUtils>();
            cr.setupEcoTarget = true;
            cr.setupEcoBehaviours = false;
            cr.addedComponents = new Component[1];
            cr.addedComponents.Append(et as Component);

        }
        public static void SetupLavaLarvaAttachPoints(AvsVehicle mv)
        {
            if (mv.Com.LavaLarvaAttachPoints.Count > 0)
            {
                GameObject attachParent = new GameObject("AttachedLavaLarvae");
                attachParent.transform.SetParent(mv.transform);
                attachParent.AddComponent<EcoTarget>().SetTargetType(EcoTargetType.HeatSource);
                var lavaLarvaTarget = attachParent.AddComponent<LavaLarvaTarget>();
                lavaLarvaTarget.energyInterface = mv.energyInterface;
                lavaLarvaTarget.larvaePrefabRoot = attachParent.transform;
                lavaLarvaTarget.liveMixin = mv.liveMixin;
                lavaLarvaTarget.primiryPointsCount = mv.Com.LavaLarvaAttachPoints.Count;
                lavaLarvaTarget.vehicle = mv;
                lavaLarvaTarget.subControl = null;
                List<LavaLarvaAttachPoint> llapList = new List<LavaLarvaAttachPoint>();
                foreach (var llap in mv.Com.LavaLarvaAttachPoints)
                {
                    GameObject llapGO = new GameObject();
                    llapGO.transform.SetParent(attachParent.transform);
                    var thisLlap = llapGO.AddComponent<LavaLarvaAttachPoint>();
                    thisLlap.Clear();
                    llapList.Add(thisLlap);
                    llapGO.transform.localPosition = attachParent.transform.InverseTransformPoint(llap.position);
                    llapGO.transform.localEulerAngles = attachParent.transform.InverseTransformDirection(llap.eulerAngles);
                }
                lavaLarvaTarget.attachPoints = llapList.ToArray();
            }
        }
        public static void SetupSubRoot(Submarine mv, PowerRelay powerRelay)
        {
            var subroot = mv.gameObject.EnsureComponent<SubRoot>();
            subroot.rb = mv.useRigidbody;
            subroot.worldForces = mv.worldForces;
            subroot.modulesRoot = mv.modulesRoot.transform;
            subroot.powerRelay = powerRelay;
            if (mv.Com.RespawnPoint == null)
            {
                mv.gameObject.EnsureComponent<RespawnPoint>();
            }
            else
            {
                mv.Com.RespawnPoint.EnsureComponent<RespawnPoint>();
            }
        }
        public static void SetupDenyBuildingTags(AvsVehicle mv)
        {
            mv.Com.DenyBuildingColliders
                .ForEach(x => x.tag = Builder.denyBuildingTag);
        }

        #endregion
        public static bool Instrument(AvsVehicle mv, PingType pingType, GameObject seamoth)
        {
            LogWriter.Default.Write("Instrumenting " + mv.name + $" {mv.Id}");
            mv.Com.StorageRootObject.EnsureComponent<ChildObjectIdentifier>();
            mv.modulesRoot = mv.Com.ModulesRootObject.EnsureComponent<ChildObjectIdentifier>();

            if (!SetupObjects(mv as AvsVehicle))
            {
                LogWriter.Default.Error("Failed to SetupObjects for ModVehicle.");
                return false;
            }
            if ((mv is Submarine sub) && !SetupObjects(sub))
            {
                LogWriter.Default.Error("Failed to SetupObjects for Submarine.");
                return false;
            }
            if ((mv is Submersible sub2) && !SetupObjects(sub2))
            {
                LogWriter.Default.Error("Failed to SetupObjects for Submersible.");
                return false;
            }
            mv.enabled = false;
            SetupEnergyInterface(mv);
            SetupAIEnergyInterface(mv, seamoth);
            mv.enabled = true;
            SetupHeadLights(mv);
            SetupLightSounds(mv);
            SetupLiveMixin(mv);
            SetupRigidbody(mv);
            SetupWorldForces(mv, seamoth);
            SetupHudPing(mv, pingType);
            SetupVehicleConfig(mv, seamoth);
            SetupCrushDamage(mv, seamoth);
            SetupWaterClipping(mv, seamoth);
            SetupSubName(mv);
            SetupCollisionSound(mv, seamoth);
            SetupOutOfBoundsWarp(mv);
            SetupConstructionObstacle(mv);
            SetupSoundOnDamage(mv, seamoth);
            SetupDealDamageOnImpact(mv);
            SetupDamageComponents(mv, seamoth);
            SetupLavaLarvaAttachPoints(mv);
            SetupDenyBuildingTags(mv);
            mv.collisionModel = mv.Com.CollisionModel;

            if (mv is Submarine sub3)
            {
                SetupFloodLights(sub3, seamoth);
                PowerRelay powerRelay = mv.gameObject.AddComponent<PowerRelay>(); // See PowerRelayPatcher. Allows Submarines to recharge batteries.
                SetupSubRoot(sub3, powerRelay); // depends on SetupWorldForces
            }


            // ApplyShaders should happen last
            //Shader shader = Shader.Find(Admin.Utils.marmosetUberName);
            //ApplyShaders(mv, shader);

            return true;
        }
        public static void ApplyGlassMaterial(AvsVehicle mv, GameObject seamoth)
        {
            // Add the [marmoset] shader to all renderers
            foreach (var renderer in mv.gameObject.GetComponentsInChildren<Renderer>(true))
            {
                if (mv.Com.CanopyWindows != null && mv.Com.CanopyWindows.Contains(renderer.gameObject))
                {
                    var seamothGlassMaterial = seamoth.transform.Find("Model/Submersible_SeaMoth/Submersible_seaMoth_geo/Submersible_SeaMoth_glass_interior_geo").GetComponent<SkinnedMeshRenderer>().material;
                    renderer.material = seamothGlassMaterial;
                    renderer.material = seamothGlassMaterial; // this is the right line
                    continue;
                }
            }
        }
        //public static void ApplyShaders(ModVehicle mv, Shader shader)
        //{
        //    if (mv.Config.AutoFixMaterials)
        //    {
        //        ForceApplyShaders(mv, shader);
        //        ApplyGlassMaterial(mv);
        //    }
        //}
        public static void ForceApplyShaders(AvsVehicle mv, Shader shader)
        {
            if (shader == null)
            {
                LogWriter.Default.Error("Tried to apply a null Shader.");
                return;
            }
            // Add the [marmoset] shader to all renderers
            foreach (var renderer in mv.gameObject.GetComponentsInChildren<Renderer>(true))
            {
                // skip some materials
                if (renderer.gameObject.GetComponent<Skybox>())
                {
                    // I feel okay using Skybox as the designated "don't apply marmoset to me" component.
                    // I think there's no reason a vehicle should have a skybox anywhere.
                    // And if there is, I'm sure that developer can work around this.
                    Component.DestroyImmediate(renderer.gameObject.GetComponent<Skybox>());
                    continue;
                }
                if (renderer.gameObject.name.ToLower().Contains("light"))
                {
                    continue;
                }
                if (mv.Com.CanopyWindows.Contains(renderer.gameObject))
                {
                    continue;
                }
                foreach (Material mat in renderer.materials)
                {
                    // give it the marmo shader, no matter what
                    mat.shader = shader;
                }
            }
        }

        internal static void SetIcon(uGUI_Ping ping, Atlas.Sprite sprite, PingType inputType)
        {
            foreach (VehicleEntry ve in VehicleManager.VehicleTypes)
            {
                if (ve.pt == inputType)
                {
                    LogWriter.Default.Debug($"[AvsVehicleBuilder] Setting icon for ping {ping.name} of type {inputType} to sprite {ve.ping_sprite?.texture.NiceName()}");
                    ping.SetIcon(ve.ping_sprite);
                    return;
                }
            }
            foreach (var pair in Assets.SpriteHelper.PingSprites)
            {
                if (pair.Type == inputType)
                {
                    LogWriter.Default.Debug($"[AvsVehicleBuilder] Setting icon for ping {ping.name} of type {inputType} to sprite {pair.Sprite?.texture.NiceName()}");
                    ping.SetIcon(pair.Sprite);
                    return;
                }
            }

            LogWriter.Default.Debug($"[AvsVehicleBuilder] No custom icons found: Setting icon for ping {ping.name} of type {inputType} to sprite {sprite?.texture.NiceName()}");
            ping.SetIcon(sprite);   //no op
        }

        /*
        //https://github.com/Metious/MetiousSubnauticaMods/blob/master/CustomDataboxes/API/Databox.cs
        public static void VehicleDataboxPatch(CustomDataboxes.API.Databox databox)
        {
            string result = "";

            if (string.IsNullOrEmpty(databox.DataboxID))
                result += "Missing required Info 'DataboxID'\n";
            if (string.IsNullOrEmpty(databox.PrimaryDescription))
                result += "Missing required Info 'PrimaryDescription'\n";
            if (!string.IsNullOrEmpty(result))
            {
                string msg = "Unable to patch\n" + result;
                LogWriter.Default.Log(msg);
                throw new InvalidOperationException(msg);
            }

            var dataBox = new CustomDataboxes.Databoxes.CustomDatabox(DataboxID)
            {
                PrimaryDescription = this.PrimaryDescription,
                SecondaryDescription = this.SecondaryDescription,
                TechTypeToUnlock = this.TechTypeToUnlock,
                BiomesToSpawn = BiomesToSpawnIn,
                coordinatedSpawns = CoordinatedSpawns,
                ModifyGameObject = this.ModifyGameObject
            };
            dataBox.Patch();

            TechType = dataBox.TechType;
        }
        public static void ScatterDataBoxes(List<VehicleCraftable> craftables)
        {
            List<Spawnable.SpawnLocation> spawnLocations = new List<Spawnable.SpawnLocation>
            {
                new Spawnable.SpawnLocation(Vector3.zero, Vector3.zero),
                new Spawnable.SpawnLocation(new Vector3(50,0,0), Vector3.zero),
                new Spawnable.SpawnLocation(new Vector3(100,0,0), Vector3.zero),
                new Spawnable.SpawnLocation(new Vector3(200,0,0), Vector3.zero),
                new Spawnable.SpawnLocation(new Vector3(400,0,0), Vector3.zero),
            };

            foreach (var craftable in craftables)
            {
                CustomDataboxes.API.Databox myDatabox = new CustomDataboxes.API.Databox()
                {
                    DataboxID = craftable.ClassID + "_databox",
                    PrimaryDescription = craftable.FriendlyName + "_databox",
                    SecondaryDescription = "wow so cool",
                    CoordinatedSpawns = spawnLocations,
                    TechTypeToUnlock = craftable.TechType
                };
                myDatabox.Patch();
            }
        }
        */
    }
}
