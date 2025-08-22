using AVS.Assets;
using AVS.BaseVehicle;
using AVS.Log;
using AVS.Util;
using AVS.VehicleTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AVS.VehicleBuilding;
using UnityEngine;

//using AVS.Localization;

namespace AVS;

internal struct VehicleEntry
{
    public VehicleEntry(AvsVehicle inputMv, int id, PingType pt_in, Sprite? sprite, TechType tt = (TechType)0)
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
    public Sprite? ping_sprite;
    public TechType techType;
}

internal static class AvsVehicleBuilder
{
    public static GameObject? UpgradeConsole { get; internal set; }

    private static int numVehicleTypes = 0;
    public static List<AvsVehicle> prefabs = new();


    public const string ControlScheme = "AvsScheme";
    public const string ModuleTypeName = "AvsModuleType";
    public const string ArmTypeName = "AvsArmType";
    public const string InnateStorageName = "AvsInnateStorage";

    public static EquipmentType ModuleType { get; }
    public static TechType InnateStorage { get; }


    static AvsVehicleBuilder()
    {
        Nautilus.Handlers.EnumHandler.AddEntry<Vehicle.ControlSheme>(ControlScheme);
        ModuleType = Nautilus.Handlers.EnumHandler.AddEntry<EquipmentType>(ModuleTypeName).Value;
        //Nautilus.Handlers.EnumHandler.AddEntry<EquipmentType>(ArmTypeName);
        InnateStorage = Nautilus.Handlers.EnumHandler.AddEntry<TechType>(InnateStorageName).Value;
    }

    public static bool IsKnownItemType(EquipmentType itemType) => itemType == ModuleType;

    public static IEnumerator Prefabricate(AvsVehicle mv, PingType pingType, bool verbose)
    {
        var log = mv.Log.Tag(nameof(Prefabricate));
        mv.OnAwakeOrPrefabricate();
        VehicleRegistrar.VerboseLog(log, VehicleRegistrar.LogType.Log, verbose,
            "Prefabricating the " + mv.gameObject.name);
        yield return SeamothHelper.WaitUntilLoaded();
        var seamoth = SeamothHelper.RequireSeamoth;
        if (!Instrument(mv, pingType, seamoth))
        {
            LogWriter.Default.Error("Failed to instrument the vehicle: " + mv.gameObject.name);
            Logger.LoopMainMenuError($"AVS: Failed prefabrication of {mv.GetType().Name}. Not registered. See log.",
                mv.gameObject.name);
            yield break;
        }

        prefabs.Add(mv);
        var ve = new VehicleEntry(mv, numVehicleTypes, pingType, mv.Config.PingSprite);
        numVehicleTypes++;
        var naiveVE = new VehicleEntry(ve.mv, ve.unique_id, ve.pt, ve.ping_sprite, TechType.None);
        AvsVehicleManager.VehicleTypes
            .Add(naiveVE); // must add/remove this vehicle entry so that we can call VFConfig.Setup.
        VehicleNautilusInterface.PatchCraftable(ref ve, verbose);
        AvsVehicleManager.VehicleTypes
            .Remove(naiveVE); // must remove this vehicle entry bc PatchCraftable adds a more complete one (with tech type)
        mv.gameObject.SetActive(true);
    }

    #region setup_funcs

    private static bool SetupObjects(AvsVehicle mv)
    {
        // Wow, look at this:
        // This Nautilus line might be super nice if it works for us
        // allow it to be opened as a storage container:
        //PrefabUtils.AddStorageContainer(obj, "StorageRoot", "TallLocker", 3, 8, true);

        if (!mv.ReSetupInnateStorages())
            return false;

        if (!mv.ReSetupModularStorages())
            return false;

        if (!mv.ReSetupWaterParks())
            return false;

        try
        {
            foreach (var vu in mv.Com.Upgrades)
            {
                LogWriter.Default.Write("Setting up upgrade in " + vu.Interface.NiceName());
                var vuci = vu.Interface.EnsureComponent<VehicleUpgradeConsoleInput>();
                vuci.flap = vu.Flap.transform;
                vuci.anglesOpened = vu.AnglesOpened;
                vuci.anglesClosed = vu.AnglesClosed;
                vuci.collider = vuci.GetComponentInChildren<Collider>();
                mv.upgradesInput = vuci;
                var up = vu.Interface.EnsureComponent<UpgradeProxy>();
                if (vu.ModuleProxies.IsNotNull())
                {
                    mv.Log.Write(
                        $"Setting up UpgradeProxy in {vu.Interface.NiceName()} with {vu.ModuleProxies.Count} proxy/ies");
                    up.proxies = vu.ModuleProxies.ToArray();
                }
                else
                {
                    mv.Log.Warn($"No module proxies defined for UpgradeProxy in {vu.Interface.NiceName()}");
                }

                SaveLoad.SaveLoadUtils.EnsureUniqueNameAmongSiblings(vu.Interface.transform);
                vu.Interface.EnsureComponent<SaveLoad.AvsUpgradesIdentifier>();
            }

            if (mv.Com.Upgrades.Count == 0)
            {
                var vuci = mv.VehicleRoot.EnsureComponent<VehicleUpgradeConsoleInput>();
                vuci.enabled = false;
                vuci.collider = mv.VehicleRoot.AddComponent<BoxCollider>();
                ((BoxCollider)vuci.collider).size = Vector3.zero;
                mv.upgradesInput = vuci;
            }
        }
        catch (Exception e)
        {
            LogWriter.Default.Error(
                "There was a problem setting up the Upgrades Interface. Check VehicleUpgrades.Interface and .Flap", e);
            return false;
        }

        if (mv.Com.BoundingBoxCollider.IsNotNull())
            mv.Com.BoundingBoxCollider.enabled = false;
        return true;
    }

    private static bool SetupObjects(Submarine mv)
    {
        try
        {
            for (var i = 0; i < mv.Com.Helms.Count; i++)
            {
                var ps = mv.Com.Helms[i];
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
            for (var i = 0; i < mv.Com.Hatches.Count; i++)
            {
                var hatch = mv.Com.Hatches[i].Hatch.EnsureComponent<VehicleComponents.VehicleHatch>();
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
            if (mv.Com.ControlPanel.IsNotNull())
            {
                mv.controlPanelLogic = mv.Com.ControlPanel.EnsureComponent<ControlPanel>();
                mv.controlPanelLogic.mv = mv;
                if (mv.transform.Find("Control-Panel-Location").IsNotNull())
                {
                    mv.Com.ControlPanel.transform.localPosition =
                        mv.transform.Find("Control-Panel-Location").localPosition;
                    mv.Com.ControlPanel.transform.localRotation =
                        mv.transform.Find("Control-Panel-Location").localRotation;
                    GameObject.Destroy(mv.transform.Find("Control-Panel-Location").gameObject);
                }
            }
        }
        catch (Exception e)
        {
            LogWriter.Default.Error(
                "There was a problem setting up the Control Panel. Check AvsVehicle.ControlPanel and ensure \"Control-Panel-Location\" exists at the top level of your model. While you're at it, check that \"Fabricator-Location\" is at the top level of your model too.",
                e);
            return false;
        }

        return true;
    }

    private static bool SetupObjects(Submersible mv)
    {
        try
        {
            mv.playerPosition = mv.Com.PilotSeat.PlayerControlLocation;
            var pt = mv.Com.PilotSeat.Root.EnsureComponent<PilotingTrigger>();
            pt.mv = mv;
        }
        catch (Exception e)
        {
            LogWriter.Default.Error("There was a problem setting up the PilotSeats. Check VehiclePilotSeat.Seat", e);
            return false;
        }

        try
        {
            for (var i = 0; i < mv.Com.Hatches.Count; i++)
            {
                var hatch = mv.Com.Hatches[i].Hatch.EnsureComponent<VehicleComponents.VehicleHatch>();
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

    private static void SetupEnergyInterface(AvsVehicle mv)
    {
        mv.chargingSound = mv.gameObject.AddComponent<FMOD_CustomLoopingEmitter>();
        mv.chargingSound.asset = SeamothHelper.RequireSeamoth.GetComponent<SeaMoth>().chargingSound.asset;

        mv.SetupPowerCells();
    }

    private static void SetupAIEnergyInterface(AvsVehicle mv, GameObject seamoth)
    {
        mv.SetupAIEnergyInterface(seamoth);
    }

    private static void SetupLightSounds(AvsVehicle mv)
    {
        mv.Log.Debug("Setting up light sounds for " + mv.name);
        var fmods = SeamothHelper.RequireSeamoth.GetComponents<FMOD_StudioEventEmitter>();
        mv.SetupLightSounds(fmods);
    }

    private static void SetupHeadlights(AvsVehicle mv)
    {
        var seamothHeadLight = SeamothHelper.RequireSeamoth.transform.Find("lights_parent/light_left").gameObject;
        if (mv.Com.Headlights.IsNotNull())
            foreach (var pc in mv.Com.Headlights)
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

    private static void SetupFloodlights(Submarine mv, GameObject seamoth)
    {
        var seamothHeadLight = seamoth.transform.Find("lights_parent/light_left").gameObject;
        if (mv.Com.Floodlights.IsNotNull())
            foreach (var pc in mv.Com.Floodlights)
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

    private static void SetupLiveMixin(AvsVehicle mv)
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

    private static void SetupRigidbody(AvsVehicle mv)
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

    private static void SetupWorldForces(AvsVehicle mv, GameObject seamoth)
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

    private static void SetupHudPing(AvsVehicle mv, PingType pingType)
    {
        mv.PrefabSetupHudPing(pingType);
        AvsVehicleManager.MvPings.Add(mv.HudPingInstance);
    }

    private static void SetupVehicleConfig(AvsVehicle mv, GameObject seamoth)
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

    private static void SetupCrushDamage(AvsVehicle mv, GameObject seamoth)
    {
        var container = new GameObject("CrushDamageContainer");
        container.transform.SetParent(mv.transform);
        var ce = container.AddComponent<FMOD_CustomEmitter>();
        ce.restartOnPlay = true;
        foreach (var thisCE in seamoth.GetComponentsInChildren<FMOD_CustomEmitter>())
            if (thisCE.name == "crushDamageSound")
            {
                ce.asset = thisCE.asset;
                LogWriter.Default.Write("Found crush damage sound for " + mv.name);
            }

        if (ce.asset.IsNull()) LogWriter.Default.Error("Failed to find crush damage sound for " + mv.name);
        /* For reference,
         * Prawn dies from max health in 3:00 minutes.
         * Seamoth in 0:30
         * Cyclops in 3:45
         * So AvsVehicles can die in 3:00 as well
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

    private static void SetupWaterClipping(AvsVehicle mv, GameObject seamoth)
    {
        if (mv.Com.WaterClipProxies.IsNotNull())
        {
            // Enable water clipping for proper interaction with the surface of the ocean
            var seamothWCP = seamoth.GetComponentInChildren<WaterClipProxy>();
            foreach (var proxy in mv.Com.WaterClipProxies)
            {
                var waterClip = proxy.AddComponent<WaterClipProxy>();
                waterClip.shape = WaterClipProxy.Shape.Box;
                //"""Apply the seamoth's clip material. No idea what shader it uses or what settings it actually has, so this is an easier option. Reuse the game's assets.""" -Lee23
                waterClip.clipMaterial = seamothWCP.clipMaterial;
                //"""You need to do this. By default the layer is 0. This makes it displace everything in the default rendering layer. We only want to displace water.""" -Lee23
                waterClip.gameObject.layer = seamothWCP.gameObject.layer;
            }
        }
    }

    private static void SetupSubName(AvsVehicle mv)
    {
        var subname = mv.gameObject.EnsureComponent<SubName>();
        subname.pingInstance = mv.HudPingInstance;
        subname.colorsInitialized = 0;
        subname.hullName =
            mv.Com.StorageRootObject
                .AddComponent<
                    TMPro.TextMeshProUGUI>(); // DO NOT push a TMPro.TextMeshProUGUI on the root vehicle object!!!
        mv.subName = subname;
        mv.SetName(mv.vehicleDefaultName);
    }

    private static void SetupCollisionSound(AvsVehicle mv, GameObject seamoth)
    {
        var colsound = mv.gameObject.EnsureComponent<CollisionSound>();
        var seamothColSound = seamoth.GetComponent<CollisionSound>();
        colsound.hitSoundSmall = seamothColSound.hitSoundSmall;
        colsound.hitSoundSlow = seamothColSound.hitSoundSlow;
        colsound.hitSoundMedium = seamothColSound.hitSoundMedium;
        colsound.hitSoundFast = seamothColSound.hitSoundFast;
    }

    private static void SetupOutOfBoundsWarp(AvsVehicle mv)
    {
        mv.gameObject.EnsureComponent<OutOfBoundsWarp>();
    }

    private static void SetupConstructionObstacle(AvsVehicle mv)
    {
        var co = mv.gameObject.EnsureComponent<ConstructionObstacle>();
        co.reason = mv.name + " is in the way.";
    }

    private static void SetupSoundOnDamage(AvsVehicle mv, GameObject seamoth)
    {
        // TODO: we could have unique sounds for each damage type
        // TODO: this might not work, might need to put it in a VehicleStatusListener
        var sod = mv.gameObject.EnsureComponent<SoundOnDamage>();
        sod.damageType = DamageType.Normal;
        sod.sound = seamoth.GetComponent<SoundOnDamage>().sound;
    }

    private static void SetupDealDamageOnImpact(AvsVehicle mv)
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

    private static void SetupDamageComponents(AvsVehicle mv, GameObject seamoth)
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

    private static void SetupLavaLarvaAttachPoints(AvsVehicle mv)
    {
        if (mv.Com.LavaLarvaAttachPoints.Count > 0)
        {
            var attachParent = new GameObject("AttachedLavaLarvae");
            attachParent.transform.SetParent(mv.transform);
            attachParent.AddComponent<EcoTarget>().SetTargetType(EcoTargetType.HeatSource);
            var lavaLarvaTarget = attachParent.AddComponent<LavaLarvaTarget>();
            lavaLarvaTarget.energyInterface = mv.energyInterface;
            lavaLarvaTarget.larvaePrefabRoot = attachParent.transform;
            lavaLarvaTarget.liveMixin = mv.liveMixin;
            lavaLarvaTarget.primiryPointsCount = mv.Com.LavaLarvaAttachPoints.Count;
            lavaLarvaTarget.vehicle = mv;
            lavaLarvaTarget.subControl = null;
            var llapList = new List<LavaLarvaAttachPoint>();
            foreach (var llap in mv.Com.LavaLarvaAttachPoints)
            {
                var llapGO = new GameObject();
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

    private static void SetupSubRoot(Submarine mv, PowerRelay powerRelay)
    {
        var subroot = mv.gameObject.EnsureComponent<SubRoot>();
        subroot.rb = mv.useRigidbody;
        subroot.worldForces = mv.worldForces;
        subroot.modulesRoot = mv.modulesRoot.transform;
        subroot.powerRelay = powerRelay;
        if (mv.Com.RespawnPoint.IsNull())
            mv.gameObject.EnsureComponent<RespawnPoint>();
        else
            mv.Com.RespawnPoint.EnsureComponent<RespawnPoint>();
    }

    private static void SetupDenyBuildingTags(AvsVehicle mv)
    {
        mv.Com.DenyBuildingColliders
            .ForEach(x => x.tag = Builder.denyBuildingTag);
    }

    #endregion

    private static bool Instrument(AvsVehicle mv, PingType pingType, GameObject seamoth)
    {
        LogWriter.Default.Write("Instrumenting " + mv.name + $" {mv.Id}");
        mv.Com.StorageRootObject.EnsureComponent<ChildObjectIdentifier>();
        mv.modulesRoot = mv.Com.ModulesRootObject.EnsureComponent<ChildObjectIdentifier>();

        if (!SetupObjects(mv as AvsVehicle))
        {
            LogWriter.Default.Error("Failed to SetupObjects for AvsVehicle.");
            return false;
        }

        if (mv is Submarine sub && !SetupObjects(sub))
        {
            LogWriter.Default.Error("Failed to SetupObjects for Submarine.");
            return false;
        }

        if (mv is Submersible sub2 && !SetupObjects(sub2))
        {
            LogWriter.Default.Error("Failed to SetupObjects for Submersible.");
            return false;
        }

        mv.enabled = false;
        SetupEnergyInterface(mv);
        SetupAIEnergyInterface(mv, seamoth);
        mv.enabled = true;
        SetupHeadlights(mv);
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
            SetupFloodlights(sub3, seamoth);
            var powerRelay =
                mv.gameObject
                    .AddComponent<PowerRelay>(); // See PowerRelayPatcher. Allows Submarines to recharge batteries.
            SetupSubRoot(sub3, powerRelay); // depends on SetupWorldForces
        }


        // ApplyShaders should happen last
        //Shader shader = Shader.Find(Admin.Utils.marmosetUberName);
        //ApplyShaders(mv, shader);

        return true;
    }

    private static void ApplyGlassMaterial(AvsVehicle mv, GameObject seamoth)
    {
        // Add the [marmoset] shader to all renderers
        foreach (var renderer in mv.gameObject.GetComponentsInChildren<Renderer>(true))
            if (mv.Com.CanopyWindows.IsNotNull() && mv.Com.CanopyWindows.Contains(renderer.gameObject))
            {
                var seamothGlassMaterial = seamoth.transform
                    .Find("Model/Submersible_SeaMoth/Submersible_seaMoth_geo/Submersible_SeaMoth_glass_interior_geo")
                    .GetComponent<SkinnedMeshRenderer>().material;
                renderer.material = seamothGlassMaterial;
                renderer.material = seamothGlassMaterial; // this is the right line
                continue;
            }
    }

    //public static void ApplyShaders(AvsVehicle mv, Shader shader)
    //{
    //    if (mv.Config.AutoFixMaterials)
    //    {
    //        ForceApplyShaders(mv, shader);
    //        ApplyGlassMaterial(mv);
    //    }
    //}
    private static void ForceApplyShaders(AvsVehicle mv, Shader shader)
    {
        if (shader.IsNull())
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

            if (renderer.gameObject.name.ToLower().Contains("light")) continue;
            if (mv.Com.CanopyWindows.Contains(renderer.gameObject)) continue;
            foreach (var mat in renderer.materials)
                // give it the marmo shader, no matter what
                mat.shader = shader;
        }
    }

    internal static void SetIcon(uGUI_Ping ping, PingType inputType)
    {
        foreach (var ve in AvsVehicleManager.VehicleTypes)
            if (ve.pt == inputType)
            {
                LogWriter.Default.Debug(
                    $"[AvsVehicleBuilder] Setting icon for ping {ping.name} of type {inputType} to sprite {ve.ping_sprite?.texture.NiceName()}");
                ping.SetIcon(ve.ping_sprite);
                return;
            }

        foreach (var pair in SpriteHelper.PingSprites)
            if (pair.Type == inputType)
            {
                LogWriter.Default.Debug(
                    $"[AvsVehicleBuilder] Setting icon for ping {ping.name} of type {inputType} to sprite {pair.Sprite?.texture.NiceName()}");
                ping.SetIcon(pair.Sprite);
                return;
            }

        LogWriter.Default.Debug($"[AvsVehicleBuilder] No custom icons found for type {inputType}. Skipping");
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