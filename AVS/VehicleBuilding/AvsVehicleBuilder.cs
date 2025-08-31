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

namespace AVS;

internal readonly record struct VehicleEntry(
    RootModController RMC,
    AvsVehicle AV,
    int UniqueId,
    PingType PingType,
    Sprite? PingSprite,
    TechType TechType = (TechType)0
    )
{
    public string Name => AV.name;
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

    public static IEnumerator Prefabricate(SmartLog log, RootModController rmc, AvsVehicle av, PingType pingType, bool verbose)
    {
        av.OnAwakeOrPrefabricate();
        VehicleRegistrar.VerboseLog(log, VehicleRegistrar.LogType.Log, verbose,
            "Prefabricating the " + av.gameObject.name);
        yield return SeamothHelper.WaitUntilLoaded();
        var seamoth = SeamothHelper.RequireSeamoth;
        if (!Instrument(rmc, av, pingType, seamoth))
        {
            log.Error("Failed to instrument the vehicle: " + av.gameObject.name);
            Logger.LoopMainMenuError($"AVS: Failed prefabrication of {av.GetType().Name}. Not registered. See log.",
                av.gameObject.name);
            yield break;
        }

        prefabs.Add(av);
        var ve = new VehicleEntry(rmc, av, numVehicleTypes, pingType, av.Config.PingSprite);
        numVehicleTypes++;
        var naiveVE = new VehicleEntry(rmc, ve.AV, ve.UniqueId, ve.PingType, ve.PingSprite, TechType.None);
        AvsVehicleManager.VehicleTypes
            .Add(naiveVE); // must add/remove this vehicle entry so that we can call VFConfig.Setup.
        VehicleNautilusInterface.PatchCraftable(ref ve, verbose);
        AvsVehicleManager.VehicleTypes
            .Remove(naiveVE); // must remove this vehicle entry bc PatchCraftable adds a more complete one (with tech type)
        av.gameObject.SetActive(true);
    }

    #region setup_funcs

    private static bool SetupObjects(AvsVehicle av)
    {
        // Wow, look at this:
        // This Nautilus line might be super nice if it works for us
        // allow it to be opened as a storage container:
        //PrefabUtils.AddStorageContainer(obj, "StorageRoot", "TallLocker", 3, 8, true);

        if (!av.ReSetupInnateStorages())
            return false;

        if (!av.ReSetupModularStorages())
            return false;

        if (!av.ReSetupWaterParks())
            return false;

        using var log = av.NewAvsLog();
        try
        {
            foreach (var vu in av.Com.Upgrades)
            {
                log.Write("Setting up upgrade in " + vu.Interface.NiceName());
                var vuci = vu.Interface.EnsureComponent<VehicleUpgradeConsoleInput>();
                vuci.flap = vu.Flap.transform;
                vuci.anglesOpened = vu.AnglesOpened;
                vuci.anglesClosed = vu.AnglesClosed;
                vuci.collider = vuci.GetComponentInChildren<Collider>();
                av.upgradesInput = vuci;
                var up = vu.Interface.EnsureComponent<UpgradeProxy>();
                up.av = av;
                if (vu.ModuleProxies.IsNotNull())
                {
                    log.Write(
                        $"Setting up UpgradeProxy in {vu.Interface.NiceName()} with {vu.ModuleProxies.Count} proxy/ies");
                    up.proxies = vu.ModuleProxies.ToArray();
                }
                else
                {
                    log.Warn($"No module proxies defined for UpgradeProxy in {vu.Interface.NiceName()}");
                }

                SaveLoad.SaveLoadUtils.EnsureUniqueNameAmongSiblings(vu.Interface.transform);
                var aui = vu.Interface.EnsureComponent<SaveLoad.AvsUpgradesIdentifier>();
                aui.av = av;

            }

            if (av.Com.Upgrades.Count == 0)
            {
                var vuci = av.VehicleRoot.EnsureComponent<VehicleUpgradeConsoleInput>();
                vuci.enabled = false;
                vuci.collider = av.VehicleRoot.AddComponent<BoxCollider>();
                ((BoxCollider)vuci.collider).size = Vector3.zero;
                av.upgradesInput = vuci;
            }
        }
        catch (Exception e)
        {
            log.Error(
                "There was a problem setting up the Upgrades Interface. Check VehicleUpgrades.Interface and .Flap", e);
            return false;
        }

        if (av.Com.BoundingBoxCollider.IsNotNull())
            av.Com.BoundingBoxCollider.enabled = false;
        return true;
    }

    private static bool SetupObjects(Submarine av)
    {
        using var log = av.NewLazyAvsLog();
        try
        {
            for (var i = 0; i < av.Com.Helms.Count; i++)
            {
                var ps = av.Com.Helms[i];
                var pt = ps.Root.EnsureComponent<PilotingTrigger>();
                pt.av = av;
                pt.helmIndex = i;
            }
        }
        catch (Exception e)
        {
            log.Error("There was a problem setting up the PilotSeats. Check VehiclePilotSeat.Seat", e);
            return false;
        }

        try
        {
            for (var i = 0; i < av.Com.Hatches.Count; i++)
            {
                var hatch = av.Com.Hatches[i].Hatch.EnsureComponent<VehicleComponents.VehicleHatch>();
                hatch.av = av;
                hatch.hatchIndex = i;
            }
        }
        catch (Exception e)
        {
            log.Error("There was a problem setting up the Hatches. Check VehicleHatchStruct.Hatch", e);
            return false;
        }

        // Configure the Control Panel
        try
        {
            if (av.Com.ControlPanel.IsNotNull())
            {
                av.controlPanelLogic = av.Com.ControlPanel.EnsureComponent<ControlPanel>();
                av.controlPanelLogic.av = av;
                if (av.transform.Find("Control-Panel-Location").IsNotNull())
                {
                    av.Com.ControlPanel.transform.localPosition =
                        av.transform.Find("Control-Panel-Location").localPosition;
                    av.Com.ControlPanel.transform.localRotation =
                        av.transform.Find("Control-Panel-Location").localRotation;
                    GameObject.Destroy(av.transform.Find("Control-Panel-Location").gameObject);
                }
            }
        }
        catch (Exception e)
        {
            log.Error(
                "There was a problem setting up the Control Panel. Check AvsVehicle.ControlPanel and ensure \"Control-Panel-Location\" exists at the top level of your model. While you're at it, check that \"Fabricator-Location\" is at the top level of your model too.",
                e);
            return false;
        }

        return true;
    }

    private static bool SetupObjects(Submersible av)
    {
        using var log = av.NewLazyAvsLog();
        try
        {
            av.playerPosition = av.Com.PilotSeat.PlayerControlLocation;
            var pt = av.Com.PilotSeat.Root.EnsureComponent<PilotingTrigger>();
            pt.av = av;
        }
        catch (Exception e)
        {
            log.Error("There was a problem setting up the PilotSeats. Check VehiclePilotSeat.Seat", e);
            return false;
        }

        try
        {
            for (var i = 0; i < av.Com.Hatches.Count; i++)
            {
                var hatch = av.Com.Hatches[i].Hatch.EnsureComponent<VehicleComponents.VehicleHatch>();
                hatch.av = av;
                hatch.hatchIndex = i;
            }
        }
        catch (Exception e)
        {
            log.Error("There was a problem setting up the Hatches. Check VehicleHatchStruct.Hatch", e);
            return false;
        }

        // Configure the Control Panel
        return true;
    }

    private static void SetupEnergyInterface(AvsVehicle av)
    {
        av.chargingSound = av.gameObject.AddComponent<FMOD_CustomLoopingEmitter>();
        av.chargingSound.asset = SeamothHelper.RequireSeamoth.GetComponent<SeaMoth>().chargingSound.asset;

        av.SetupPowerCells();
    }

    private static void SetupAIEnergyInterface(AvsVehicle av, GameObject seamoth)
    {
        av.SetupAIEnergyInterface(seamoth);
    }

    private static void SetupLightSounds(AvsVehicle av)
    {
        using var log = av.NewAvsLog();
        log.Debug("Setting up light sounds for " + av.name);
        var fmods = SeamothHelper.RequireSeamoth.GetComponents<FMOD_StudioEventEmitter>();
        av.SetupLightSounds(fmods);
    }

    private static void SetupHeadlights(AvsVehicle av)
    {
        var seamothHeadLight = SeamothHelper.RequireSeamoth.transform.Find("lights_parent/light_left").gameObject;
        if (av.Com.Headlights.IsNotNull())
            foreach (var pc in av.Com.Headlights)
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

                var RLS = av.gameObject.AddComponent<RegistredLightSource>();
                RLS.hostLight = thisLight;
            }
    }

    private static void SetupFloodlights(Submarine av, GameObject seamoth)
    {
        var seamothHeadLight = seamoth.transform.Find("lights_parent/light_left").gameObject;
        if (av.Com.Floodlights.IsNotNull())
            foreach (var pc in av.Com.Floodlights)
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

                var RLS = av.gameObject.AddComponent<RegistredLightSource>();
                RLS.hostLight = thisLight;
            }
    }

    private static void SetupLiveMixin(AvsVehicle av)
    {
        var liveMixin = av.gameObject.EnsureComponent<LiveMixin>();
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
        lmData.maxHealth = av.Config.MaxHealth;
        liveMixin.health = av.Config.MaxHealth;
        liveMixin.data = lmData;
        av.liveMixin = liveMixin;
    }

    private static void SetupRigidbody(AvsVehicle av)
    {
        var rb = av.gameObject.EnsureComponent<Rigidbody>();
        /*
         * For reference,
         * Cyclop: 12000
         * Abyss: 5000
         * Atrama: 4250
         * Odyssey: 3500
         * Prawn: 1250
         * Seamoth: 800
         */
        rb.mass = av.Config.Mass;
        rb.drag = 10f;
        rb.angularDrag = 10f;
        rb.useGravity = false;
        av.useRigidbody = rb;
    }

    private static void SetupWorldForces(AvsVehicle av, GameObject seamoth)
    {
        using var log = av.NewAvsLog();
        log.Write("Setting up world forces for " + av.name);
        av.worldForces = seamoth
            .GetComponent<SeaMoth>()
            .worldForces
            .CopyComponentWithFieldsTo(av.gameObject);
        av.worldForces!.useRigidbody = av.useRigidbody;
        av.worldForces.underwaterGravity = 0f;
        av.worldForces.aboveWaterGravity = 9.8f;
        av.worldForces.waterDepth = 0f;
    }

    private static void SetupHudPing(AvsVehicle av, PingType pingType)
    {
        av.PrefabSetupHudPing(pingType);
        AvsVehicleManager.MvPings.Add(av.HudPingInstance);
    }

    private static void SetupVehicleConfig(AvsVehicle av, GameObject seamoth)
    {
        // add various vehicle things
        av.stabilizeRoll = true;
        av.controlSheme = (Vehicle.ControlSheme)12;
        av.mainAnimator = av.gameObject.EnsureComponent<Animator>();
        av.SetupAmbienceSound(seamoth.GetComponent<SeaMoth>().ambienceSound);
        av.splashSound = seamoth.GetComponent<SeaMoth>().splashSound;
        // TODO
        //atrama.vehicle.bubbles = CopyComponent<ParticleSystem>(seamoth.GetComponent<SeaMoth>().bubbles, atrama.vehicle.gameObject);
    }

    private static void SetupCrushDamage(AvsVehicle av, GameObject seamoth)
    {
        using var log = av.NewAvsLog();
        var container = new GameObject("CrushDamageContainer");
        container.transform.SetParent(av.transform);
        var ce = container.AddComponent<FMOD_CustomEmitter>();
        ce.restartOnPlay = true;
        foreach (var thisCE in seamoth.GetComponentsInChildren<FMOD_CustomEmitter>())
            if (thisCE.name == "crushDamageSound")
            {
                ce.asset = thisCE.asset;
                //LogWriter.Default.Write("Found crush damage sound for " + av.name);
            }

        if (ce.asset.IsNull())
            log.Error("Failed to find crush damage sound for " + av.name);
        /* For reference,
         * Prawn dies from max health in 3:00 minutes.
         * Seamoth in 0:30
         * Cyclops in 3:45
         * So AvsVehicles can die in 3:00 as well
         */
        av.crushDamageEmitter = container;
        av.crushDamage = av.gameObject.EnsureComponent<CrushDamage>();
        av.crushDamage.soundOnDamage = ce;
        av.crushDamage.kBaseCrushDepth = av.Config.BaseCrushDepth;
        av.crushDamage.damagePerCrush = av.Config.CrushDamage;
        av.crushDamage.crushPeriod = av.Config.CrushDamageFrequency;
        av.crushDamage.vehicle = av;
        av.crushDamage.liveMixin = av.liveMixin;
        // TODO: this is of type VoiceNotification
        av.crushDamage.crushDepthUpdate = null;

        log.Write("Crush sound registered: " + av.crushDamage.soundOnDamage.NiceName());
    }

    private static void SetupWaterClipping(AvsVehicle av, GameObject seamoth)
    {
        if (av.Com.WaterClipProxies.IsNotNull())
        {
            // Enable water clipping for proper interaction with the surface of the ocean
            var seamothWCP = seamoth.GetComponentInChildren<WaterClipProxy>();
            foreach (var proxy in av.Com.WaterClipProxies)
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

    private static void SetupSubName(AvsVehicle av)
    {
        var subname = av.gameObject.EnsureComponent<SubName>();
        subname.pingInstance = av.HudPingInstance;
        subname.colorsInitialized = 0;
        subname.hullName =
            av.Com.StorageRootObject
                .AddComponent<
                    TMPro.TextMeshProUGUI>(); // DO NOT push a TMPro.TextMeshProUGUI on the root vehicle object!!!
        av.subName = subname;
        av.SetName(av.vehicleDefaultName);
    }

    private static void SetupCollisionSound(AvsVehicle av, GameObject seamoth)
    {
        var colsound = av.gameObject.EnsureComponent<CollisionSound>();
        var seamothColSound = seamoth.GetComponent<CollisionSound>();
        colsound.hitSoundSmall = seamothColSound.hitSoundSmall;
        colsound.hitSoundSlow = seamothColSound.hitSoundSlow;
        colsound.hitSoundMedium = seamothColSound.hitSoundMedium;
        colsound.hitSoundFast = seamothColSound.hitSoundFast;
    }

    private static void SetupOutOfBoundsWarp(AvsVehicle av)
    {
        av.gameObject.EnsureComponent<OutOfBoundsWarp>();
    }

    private static void SetupConstructionObstacle(AvsVehicle av)
    {
        var co = av.gameObject.EnsureComponent<ConstructionObstacle>();
        co.reason = av.name + " is in the way.";
    }

    private static void SetupSoundOnDamage(AvsVehicle av, GameObject seamoth)
    {
        // TODO: we could have unique sounds for each damage type
        // TODO: this might not work, might need to put it in a VehicleStatusListener
        var sod = av.gameObject.EnsureComponent<SoundOnDamage>();
        sod.damageType = DamageType.Normal;
        sod.sound = seamoth.GetComponent<SoundOnDamage>().sound;
    }

    private static void SetupDealDamageOnImpact(AvsVehicle av)
    {
        var ddoi = av.gameObject.EnsureComponent<DealDamageOnImpact>();
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

    private static void SetupDamageComponents(AvsVehicle av, GameObject seamoth)
    {
        // add vfxvehicledamages... or not

        // add temperaturedamage
        var tempdamg = av.gameObject.EnsureComponent<TemperatureDamage>();
        tempdamg.lavaDatabase = seamoth.GetComponent<TemperatureDamage>().lavaDatabase;
        tempdamg.liveMixin = av.liveMixin;
        tempdamg.baseDamagePerSecond = 2.0f; // 10 times what the seamoth takes, since the Atrama 
        // the following configurations are the same values the seamoth takes
        tempdamg.minDamageTemperature = 70f;
        tempdamg.onlyLavaDamage = false;
        tempdamg.timeDamageStarted = -1000;
        tempdamg.timeLastDamage = 0;
        tempdamg.player = null;

        // add ecotarget
        var et = av.gameObject.EnsureComponent<EcoTarget>();
        et.type = EcoTargetType.Shark; // same as seamoth (lol)
        et.nextUpdateTime = 0f;

        // add creatureutils
        var cr = av.gameObject.EnsureComponent<CreatureUtils>();
        cr.setupEcoTarget = true;
        cr.setupEcoBehaviours = false;
        cr.addedComponents = new Component[1];
        cr.addedComponents.Append(et as Component);
    }

    private static void SetupLavaLarvaAttachPoints(AvsVehicle av)
    {
        if (av.Com.LavaLarvaAttachPoints.Count > 0)
        {
            var attachParent = new GameObject("AttachedLavaLarvae");
            attachParent.transform.SetParent(av.transform);
            attachParent.AddComponent<EcoTarget>().SetTargetType(EcoTargetType.HeatSource);
            var lavaLarvaTarget = attachParent.AddComponent<LavaLarvaTarget>();
            lavaLarvaTarget.energyInterface = av.energyInterface;
            lavaLarvaTarget.larvaePrefabRoot = attachParent.transform;
            lavaLarvaTarget.liveMixin = av.liveMixin;
            lavaLarvaTarget.primiryPointsCount = av.Com.LavaLarvaAttachPoints.Count;
            lavaLarvaTarget.vehicle = av;
            lavaLarvaTarget.subControl = null;
            var llapList = new List<LavaLarvaAttachPoint>();
            foreach (var llap in av.Com.LavaLarvaAttachPoints)
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

    private static void SetupSubRoot(Submarine av, PowerRelay powerRelay)
    {
        var subroot = av.gameObject.EnsureComponent<SubRoot>();
        subroot.rb = av.useRigidbody;
        subroot.worldForces = av.worldForces;
        subroot.modulesRoot = av.modulesRoot.transform;
        subroot.powerRelay = powerRelay;
        if (av.Com.RespawnPoint.IsNull())
            av.gameObject.EnsureComponent<RespawnPoint>();
        else
            av.Com.RespawnPoint.EnsureComponent<RespawnPoint>();
    }

    private static void SetupDenyBuildingTags(AvsVehicle av)
    {
        av.Com.DenyBuildingColliders
            .ForEach(x => x.tag = Builder.denyBuildingTag);
    }

    #endregion

    private static bool Instrument(RootModController rmc, AvsVehicle av, PingType pingType, GameObject seamoth)
    {
        using var log = av.NewAvsLog();
        log.Write("Instrumenting " + av.name + $" {av.Id}");
        av.Com.StorageRootObject.EnsureComponent<ChildObjectIdentifier>();
        av.modulesRoot = av.Com.ModulesRootObject.EnsureComponent<ChildObjectIdentifier>();

        if (!SetupObjects(av as AvsVehicle))
        {
            log.Error("Failed to SetupObjects for AvsVehicle.");
            return false;
        }

        if (av is Submarine sub && !SetupObjects(sub))
        {
            log.Error("Failed to SetupObjects for Submarine.");
            return false;
        }

        if (av is Submersible sub2 && !SetupObjects(sub2))
        {
            log.Error("Failed to SetupObjects for Submersible.");
            return false;
        }

        av.enabled = false;
        SetupEnergyInterface(av);
        SetupAIEnergyInterface(av, seamoth);
        av.enabled = true;
        SetupHeadlights(av);
        SetupLightSounds(av);
        SetupLiveMixin(av);
        SetupRigidbody(av);
        SetupWorldForces(av, seamoth);
        SetupHudPing(av, pingType);
        SetupVehicleConfig(av, seamoth);
        SetupCrushDamage(av, seamoth);
        SetupWaterClipping(av, seamoth);
        SetupSubName(av);
        SetupCollisionSound(av, seamoth);
        SetupOutOfBoundsWarp(av);
        SetupConstructionObstacle(av);
        SetupSoundOnDamage(av, seamoth);
        SetupDealDamageOnImpact(av);
        SetupDamageComponents(av, seamoth);
        SetupLavaLarvaAttachPoints(av);
        SetupDenyBuildingTags(av);
        av.collisionModel = av.Com.CollisionModel[0];

        if (av is Submarine sub3)
        {
            SetupFloodlights(sub3, seamoth);
            var powerRelay =
                av.gameObject
                    .AddComponent<PowerRelay>(); // See PowerRelayPatcher. Allows Submarines to recharge batteries.
            SetupSubRoot(sub3, powerRelay); // depends on SetupWorldForces
        }


        // ApplyShaders should happen last
        //Shader shader = Shader.Find(Admin.Utils.marmosetUberName);
        //ApplyShaders(av, shader);

        return true;
    }

    private static void ApplyGlassMaterial(AvsVehicle av, GameObject seamoth)
    {
        // Add the [marmoset] shader to all renderers
        foreach (var renderer in av.gameObject.GetComponentsInChildren<Renderer>(true))
            if (av.Com.CanopyWindows.IsNotNull() && av.Com.CanopyWindows.Contains(renderer.gameObject))
            {
                var seamothGlassMaterial = seamoth.transform
                    .Find("Model/Submersible_SeaMoth/Submersible_seaMoth_geo/Submersible_SeaMoth_glass_interior_geo")
                    .GetComponent<SkinnedMeshRenderer>().material;
                renderer.material = seamothGlassMaterial;
                renderer.material = seamothGlassMaterial; // this is the right line
                continue;
            }
    }

    internal static void SetIcon(uGUI_Ping ping, PingType inputType)
    {
        using var log = SmartLog.For(RootModController.AnyInstance);
        foreach (var ve in AvsVehicleManager.VehicleTypes)
            if (ve.PingType == inputType)
            {
                log.Debug(
                    $"Setting icon for ping {ping.name} of type {inputType} to sprite {ve.PingSprite?.texture.NiceName()}");
                ping.SetIcon(new Atlas.Sprite(ve.PingSprite));
                return;
            }

        foreach (var pair in SpriteHelper.PingSprites)
            if (pair.Type == inputType)
            {
                log.Debug(
                    $"Setting icon for ping {ping.name} of type {inputType} to sprite {pair.Sprite?.texture.NiceName()}");
                ping.SetIcon(new Atlas.Sprite(pair.Sprite));
                return;
            }

        log.Debug($"No custom icons found for type {inputType}. Skipping");
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