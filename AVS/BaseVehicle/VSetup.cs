using AVS.Assets;
using AVS.Configuration;
using AVS.Localization;
using AVS.Log;
using AVS.StorageComponents;
using AVS.Util;
using AVS.VehicleComponents;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using MobileWaterPark = AVS.StorageComponents.MobileWaterPark;

namespace AVS.BaseVehicle;

public abstract partial class AvsVehicle
{
    [SerializeField]
    internal int mainPatcherInstanceId;
    private RootModController? owner;

    internal SmartLog NewLazyAvsLog(IReadOnlyList<string>? tags = null, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string memberName = "")
        => new SmartLog(Owner, "AVS", frameDelta: 1, tags: [$"V{Id}", .. (tags ?? [])], forceLazy: true, nameOverride: SmartLog.DeriveCallerName(callerFilePath, memberName));
    internal SmartLog NewAvsLog(params string[] tags) => new SmartLog(Owner, "AVS", frameDelta: 1, tags: [$"V{Id}", .. tags]);
    /// <summary>
    /// Creates a new instance of <see cref="SmartLog"/> preconfigured with module-specific tags.
    /// </summary>
    /// <param name="tags">An optional array of additional tags to include in the log. These tags are appended to the default module tags.</param>
    /// <returns>A new <see cref="SmartLog"/> instance associated with the module and including the specified tags.</returns>
    public SmartLog NewModLog(params string[] tags) => new SmartLog(Owner, "Mod", frameDelta: 1, tags: [$"V{Id}", .. tags]);
    /// <summary>
    /// Creates a new lazy instance of <see cref="SmartLog"/> preconfigured with module-specific tags.
    /// Lazy logs defer the output of the log context until it is actually needed, which can improve performance.
    /// </summary>
    /// <remarks>
    /// When using this method, ensure that the caller type name matches the caller file name.
    /// </remarks>
    /// <param name="callerFilePath">The file path of the caller. This is automatically populated by the compiler.</param>
    /// <param name="memberName">The member name of the caller. This is automatically populated by the compiler.</param>
    /// <param name="tags">An optional array of additional tags to include in the log. These tags are appended to the default module tags.</param>
    /// <returns>A new <see cref="SmartLog"/> instance associated with the module and including the specified tags.</returns>
    public SmartLog NewLazyModLog(IReadOnlyList<string>? tags = null, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string memberName = "")
        => new SmartLog(Owner, "Mod", frameDelta: 1, tags: [$"V{Id}", .. (tags ?? [])], forceLazy: true, nameOverride: SmartLog.DeriveCallerName(callerFilePath, memberName));

    /// <summary>
    /// The root mod controller instance that owns this vehicle.
    /// </summary>
    public RootModController Owner
    {
        get
        {
            if (owner.IsNull())
                owner = RootModController.GetInstance(mainPatcherInstanceId);
            return owner;
        }
    }


    internal void SetupVolumetricLights()
    {
        using var log = NewAvsLog();
        if (SeamothHelper.Seamoth.IsNull())
        {
            log.Error("SeamothHelper.Seamoth is null. Cannot setup volumetric lights.");
            return;
        }

        var seamothHeadLight = SeamothHelper.Seamoth.transform.Find("lights_parent/light_left").gameObject;
        var seamothVL = SeamothHelper.Seamoth.transform.Find("lights_parent/light_left/x_FakeVolumletricLight"); // sic
        var seamothVLMF = seamothVL.GetComponent<MeshFilter>();
        var seamothVLMR = seamothVL.GetComponent<MeshRenderer>();
        var theseLights = Com.Headlights.ToList();
        if (this is VehicleTypes.Submarine subma) theseLights.AddRange(subma.Com.Floodlights);
        foreach (var pc in theseLights)
        {
            log.Debug($"Setting up volumetric light for {pc.Light.NiceName()} on {this.NiceName()}");
            var volumetricLight = new GameObject("VolumetricLight");
            volumetricLight.transform.SetParent(pc.Light.transform);
            volumetricLight.transform.localPosition = Vector3.zero;
            volumetricLight.transform.localEulerAngles = Vector3.zero;
            volumetricLight.transform.localScale = seamothVL.localScale;

            var lvlMeshFilter = volumetricLight.AddComponent<MeshFilter>();
            lvlMeshFilter.mesh = seamothVLMF.mesh;
            lvlMeshFilter.sharedMesh = seamothVLMF.sharedMesh;

            var lvlMeshRenderer = volumetricLight.AddComponent<MeshRenderer>();
            lvlMeshRenderer.material = seamothVLMR.material;
            lvlMeshRenderer.sharedMaterial = seamothVLMR.sharedMaterial;
            lvlMeshRenderer.shadowCastingMode = seamothVLMR.shadowCastingMode;
            lvlMeshRenderer.renderingLayerMask = seamothVLMR.renderingLayerMask;

            var leftVFX = seamothHeadLight
                .GetComponent<VFXVolumetricLight>()
                .CopyComponentWithFieldsTo(pc.Light);
            leftVFX.lightSource = pc.Light.GetComponent<Light>();
            leftVFX.color = pc.Color;
            leftVFX.volumGO = volumetricLight;
            leftVFX.volumRenderer = lvlMeshRenderer;
            leftVFX.volumMeshFilter = lvlMeshFilter;
            leftVFX.angle = Mathf.RoundToInt(Mathf.Clamp(pc.Angle, 5f, 175f));
            leftVFX.range = pc.Range;
            volumetricLights.Add(volumetricLight);
        }
    }


    /// <summary>
    /// Invoked via reflection by patches to notify the vehicle of a sub construction completion.
    /// </summary>
    public override void SubConstructionComplete()
    {
        HudPingInstance.enabled = true;
        worldForces.handleGravity = true;
        BuildBotManager.ResetGhostMaterial();
    }


    /// <summary>
    /// Invoked via reflection by patches to notify the vehicle of a sub construction beginning.
    /// </summary>
    public virtual void SubConstructionBeginning()
    {
        using var log = NewAvsLog();
        log.Debug($"{nameof(AvsVehicle)}[#{Id}].{nameof(SubConstructionBeginning)}");
        if (HudPingInstance)
            HudPingInstance.enabled = false;
        else
            log.Error($"HudPingInstance is null in {nameof(SubConstructionBeginning)} #{Id}");
        if (worldForces)
            worldForces.handleGravity = false;
        else
            log.Error($"worldForces is null in {nameof(SubConstructionBeginning)} #{Id}");
    }

    /// <summary>
    /// Called via reflection once vehicle crafting is completed.
    /// </summary>
    /// <param name="techType">This vehicle's tech type</param>
    public virtual void OnCraftEnd(TechType techType)
    {
        using var log = NewAvsLog();
        log.Write($"OnCraftEnd called for {techType}");

        IEnumerator GiveUsABatteryOrGiveUsDeath(SmartLog log)
        {
            yield return new WaitForSeconds(2.5f);

            // give us an AI battery please
            var result = new InstanceContainer();
            yield return AvsCraftData.InstantiateFromPrefabAsync(log, TechType.PowerCell,
                result);
            var newAIBattery = result.Instance;
            if (newAIBattery.IsNull())
            {
                log.Error($"Could not find PowerCell prefab for {techType}");
                yield break;
            }

            newAIBattery.GetComponent<Battery>().charge = 200;
            newAIBattery.transform.SetParent(Com.StorageRootObject.transform);
            if (aiEnergyInterface.IsNotNull())
            {
                aiEnergyInterface.sources.First().battery = newAIBattery.GetComponent<Battery>();
                aiEnergyInterface.sources.First().batterySlot.AddItem(newAIBattery.GetComponent<Pickupable>());
                newAIBattery.SetActive(false);
            }

            if (!energyInterface.hasCharge)
            {
                yield return AvsCraftData.InstantiateFromPrefabAsync(log, TechType.PowerCell,
                    result);
                var newPowerCell = result.Instance;
                if (newPowerCell.IsNotNull())
                {
                    newPowerCell.GetComponent<Battery>().charge = 200;
                    newPowerCell.transform.SetParent(Com.StorageRootObject.transform);
                    var mixin = Com.Batteries[0].Root.gameObject.GetComponent<EnergyMixin>();
                    mixin.battery = newPowerCell.GetComponent<Battery>();
                    mixin.batterySlot.AddItem(newPowerCell.GetComponent<Pickupable>());
                    newPowerCell.SetActive(false);
                }
                else
                {
                    log.Error($"Could not find PowerCell prefab for {techType}");
                }
            }
        }

        if (Com.Batteries.Count > 0)
            Owner.StartAvsCoroutine(
                nameof(MaterialReactor) + '.' + nameof(GiveUsABatteryOrGiveUsDeath),
                GiveUsABatteryOrGiveUsDeath);
    }

    internal void CheckEnergyInterface()
    {
        using var log = NewAvsLog();
        if (energyInterface.sources.Length < Com.Batteries.Count)
        {
            log.Error($"EnergyInterface for {this.NiceName()} has less sources than batteries. " +
                      $"Expected {Com.Batteries.Count}, got {energyInterface.sources.Length}. " +
                      $"This is a bug, please report it.");
            var energyMixins = new List<EnergyMixin>();
            foreach (var vb in Com.Batteries) energyMixins.Add(vb.Root.GetComponent<EnergyMixin>());
            energyInterface.sources = energyMixins.ToArray();
        }

        if (!IsPowered())
            GetComponentsInChildren<IPowerListener>(true).ForEach(x => x.OnBatteryDead());

        log.Debug($"EnergyInterface for {energyInterface.NiceName()} has {energyInterface.sources.Length} sources.");
    }

    internal void SetupPowerCells()
    {
        using var log = NewAvsLog();
        var seamothEnergyMixin = SeamothHelper.RequireSeamoth.GetComponent<EnergyMixin>();
        var energyMixins = new List<EnergyMixin>();
        if (Com.Batteries.Count == 0)
        {
            // Configure energy mixin for this battery slot
            var energyMixin = gameObject.EnsureComponent<ForeverBattery>();
            energyMixin.storageRoot = Com.StorageRootObject.GetComponent<ChildObjectIdentifier>();
            energyMixin.defaultBattery = seamothEnergyMixin.defaultBattery;
            energyMixin.compatibleBatteries = seamothEnergyMixin.compatibleBatteries;
            energyMixin.soundPowerUp = seamothEnergyMixin.soundPowerUp;
            energyMixin.soundPowerDown = seamothEnergyMixin.soundPowerDown;
            energyMixin.soundBatteryAdd = seamothEnergyMixin.soundBatteryAdd;
            energyMixin.soundBatteryRemove = seamothEnergyMixin.soundBatteryRemove;
            energyMixin.batteryModels = seamothEnergyMixin.batteryModels;
            energyMixin.controlledObjects = [];
            energyMixins.Add(energyMixin);
        }

        foreach (var vb in Com.Batteries)
        {
            log.Debug($"Setting up vehicle battery '{vb.DisplayName?.Text ?? vb.Root.name}' for {this.NiceName()}");
            // Configure energy mixin for this battery slot
            //vb.Root.GetComponents<EnergyMixin>().ForEach(em => GameObject.Destroy(em)); // remove old energy mixins
            var energyMixin = vb.Root.EnsureComponent<EnergyMixin>();
            //energyMixin.originalProxy = vb.BatteryProxy;
            energyMixin.storageRoot = Com.StorageRootObject.GetComponent<ChildObjectIdentifier>();
            energyMixin.defaultBattery = seamothEnergyMixin.defaultBattery;
            energyMixin.compatibleBatteries = seamothEnergyMixin.compatibleBatteries;
            energyMixin.soundPowerUp = seamothEnergyMixin.soundPowerUp;
            energyMixin.soundPowerDown = seamothEnergyMixin.soundPowerDown;
            energyMixin.soundBatteryAdd = seamothEnergyMixin.soundBatteryAdd;
            energyMixin.soundBatteryRemove = seamothEnergyMixin.soundBatteryRemove;
            energyMixin.batteryModels = seamothEnergyMixin.batteryModels;
            energyMixins.Add(energyMixin);
            var tmp = vb.Root.EnsureComponent<VehicleBatteryInput>();
            tmp.mixin = energyMixin;
            tmp.vehicle = this;
            tmp.powerCellObject = vb.Root;
            tmp.displayName = vb.DisplayName?.Text;
            tmp.displayNameLocalized = vb.DisplayName?.Localize ?? false;


            var model = AvAttached.Ensure<BatteryProxy>(vb.Root, this, log);
            model.proxy = vb.BatteryProxy;
            model.mixin = energyMixin;

            SaveLoad.SaveLoadUtils.EnsureUniqueNameAmongSiblings(vb.Root.transform);
            var abi = AvAttached.Ensure<SaveLoad.AvsBatteryIdentifier>(vb.Root, this, log);
        }

        // Configure energy interface
        var eInterf = gameObject.EnsureComponent<EnergyInterface>();
        eInterf.sources = energyMixins.ToArray();
        energyInterface = eInterf;
    }


    /// <summary>
    /// Overridable import method called when an imported recipe is loaded.
    /// </summary>
    /// <param name="recipe">Recipe restored from file</param>
    /// <returns>Recipe to use</returns>
    public virtual Recipe OnRecipeOverride(Recipe recipe) => recipe;


    /// <summary>
    /// True if the vehicle is constructed and ready to be piloted.
    /// </summary>
    public bool IsConstructed => vfxConstructing.IsNull() || vfxConstructing.IsConstructed();

    /// <summary>
    /// Constructs the vehicle's ping instance as part of the prefab setup.
    /// </summary>
    /// <param name="pingType"></param>
    internal void PrefabSetupHudPing(PingType pingType)
    {
        using var log = NewAvsLog();
        log.Write($"Setting up HudPingInstance for {GetType().Name}");
        hudPingInstance = gameObject.EnsureComponent<PingInstance>();
        hudPingInstance.origin = transform;
        hudPingInstance.pingType = pingType;
        hudPingInstance.SetLabel("Vehicle");
    }


    internal bool ReSetupModularStorages()
    {
        var iter = 0;
        using var log = NewAvsLog();
        try
        {
            foreach (var vs in Com.ModularStorages)
            {
                log.Debug("Setting up Modular Storage " + vs.Container.NiceName() + " for " +
                          this.NiceName());
                vs.Container.SetActive(false);

                //LogWriter.Default.Debug("Scanning seamoth");
                var sm = SeamothHelper.RequireSeamoth;
                //LogWriter.Default.Debug("Found seamoth: " + sm.NiceName());
                var storage = sm.transform.Find("Storage/Storage1");
                if (storage.IsNull())
                {
                    log.Error("Could not find Storage/Storage1 in the Seamoth prefab");
                    return false;
                }

                var storageCloseSound = storage.GetComponent<SeamothStorageInput>().closeSound;
                var storageOpenSound = storage.GetComponent<SeamothStorageInput>().openSound;
                //LogWriter.Default.Debug("Setting up");
                var inp = vs.Container.EnsureComponent<ModularStorageInput>();
                var name = vs.DisplayName ?? Text.Untranslated("Modular Vehicle Storage " + iter);
                inp.displayName = name;
                inp.av = this;
                inp.slotID = iter;
                iter++;
                inp.model = vs.Container;
                if (vs.Container.GetComponentInChildren<Collider>() is null)
                    inp.collider = vs.Container.EnsureComponent<BoxCollider>();
                inp.openSound = storageOpenSound;
                inp.closeSound = storageCloseSound;
            }

            return true;
        }
        catch (Exception e)
        {
            log.Error(
                "There was a problem setting up the Modular Storage. Check VehicleStorage.Container and AvsVehicle.StorageRootObject",
                e);
            return false;
        }
    }

    internal bool ReSetupWaterParks()
    {
        var iter = 0;
        using var log = NewAvsLog();
        try
        {
            log.Debug($"Setting up {Com.WaterParks.Count} Mobile Water Parks");
            foreach (var vp in Com.WaterParks)
            {
                vp.Root.SetActive(false);

                var cont = vp.ContentContainer.gameObject.EnsureComponent<MobileWaterPark>();
                log.Debug("Setting up Mobile Water Park " + cont.NiceName() + $" '{cont.DisplayName}'");
                var name = vp.DisplayName ?? Text.Untranslated("Innate Vehicle Storage " + iter);
                cont.Setup(this, name, vp, iter + 1);
                var storageCloseSound = SeamothHelper.RequireSeamoth.transform.Find("Storage/Storage1")
                    .GetComponent<SeamothStorageInput>().closeSound;
                var storageOpenSound = SeamothHelper.RequireSeamoth.transform.Find("Storage/Storage1")
                    .GetComponent<SeamothStorageInput>().openSound;
                var inp = vp.Root.EnsureComponent<WaterParkStorageInput>();
                inp.displayName = name;
                inp.av = this;
                inp.slotID = iter;
                iter++;
                inp.model = vp.Root;
                if (vp.Root.GetComponentInChildren<Collider>() is null)
                    inp.collider = vp.Root.EnsureComponent<BoxCollider>();
                inp.openSound = storageOpenSound;
                inp.closeSound = storageCloseSound;
                vp.Root.SetActive(true);

                SaveLoad.SaveLoadUtils.EnsureUniqueNameAmongSiblings(vp.Root.transform);
            }

            return true;
        }
        catch (Exception e)
        {
            log.Error(
                "There was a problem setting up the Innate Storage. Check VehicleStorage.Container and AvsVehicle.StorageRootObject",
                e);
            return false;
        }
    }

    internal bool ReSetupInnateStorages()
    {
        using var log = NewAvsLog();
        var iter = 0;
        try
        {
            foreach (var vs in Com.InnateStorages)
            {
                vs.Container.SetActive(false);

                var cont = vs.Container.EnsureComponent<InnateStorageContainer>();
                var name = vs.DisplayName ?? Text.Untranslated("Innate Vehicle Storage " + iter);
                cont.av = this;
                cont.storageRoot = Com.StorageRootObject.GetComponent<ChildObjectIdentifier>();
                cont.DisplayName = name;
                cont.height = vs.Height;
                cont.width = vs.Width;
                cont.isAllowedToAdd = vs.InnateIsAllowedToAdd;
                cont.isAllowedToRemove = vs.InnateIsAllowedToRemove;
                //cont.name = "Innate Vehicle Storage " + iter;

                log.Debug("Setting up Innate Storage " + cont.NiceName() + $" '{cont.DisplayName}'");

                var storageCloseSound = SeamothHelper.RequireSeamoth.transform.Find("Storage/Storage1")
                    .GetComponent<SeamothStorageInput>().closeSound;
                var storageOpenSound = SeamothHelper.RequireSeamoth.transform.Find("Storage/Storage1")
                    .GetComponent<SeamothStorageInput>().openSound;
                var inp = vs.Container.EnsureComponent<InnateStorageInput>();
                inp.displayName = name;
                inp.av = this;
                inp.slotID = iter;
                iter++;
                inp.model = vs.Container;
                if (vs.Container.GetComponentInChildren<Collider>() is null)
                    inp.collider = vs.Container.EnsureComponent<BoxCollider>();
                inp.openSound = storageOpenSound;
                inp.closeSound = storageCloseSound;
                vs.Container.SetActive(true);

                SaveLoad.SaveLoadUtils.EnsureUniqueNameAmongSiblings(vs.Container.transform);
                vs.Container.EnsureComponent<SaveLoad.AvsInnateStorageIdentifier>();
            }

            return true;
        }
        catch (Exception e)
        {
            log.Error(
                "There was a problem setting up the Innate Storage. Check VehicleStorage.Container and AvsVehicle.StorageRootObject",
                e);
            return false;
        }
    }

    internal void SetupAIEnergyInterface(GameObject seamoth)
    {
        if (Com.BackupBatteries.Count == 0)
        {
            aiEnergyInterface = energyInterface;
            return;
        }

        var seamothEnergyMixin = seamoth.GetComponent<EnergyMixin>();
        var energyMixins = new List<EnergyMixin>();
        foreach (var vb in Com.BackupBatteries)
        {
            // Configure energy mixin for this battery slot
            vb.Root.GetComponents<EnergyMixin>().ForEach(Destroy); // remove old energy mixins
            var em = vb.Root.AddComponent<DebugBatteryEnergyMixin>();
            em.originalProxy = vb.BatteryProxy;
            em.storageRoot = Com.StorageRootObject.GetComponent<ChildObjectIdentifier>();
            em.defaultBattery = seamothEnergyMixin.defaultBattery;
            em.compatibleBatteries = [TechType.PowerCell, TechType.PrecursorIonPowerCell];
            em.soundPowerUp = seamothEnergyMixin.soundPowerUp;
            em.soundPowerDown = seamothEnergyMixin.soundPowerDown;
            em.soundBatteryAdd = seamothEnergyMixin.soundBatteryAdd;
            em.soundBatteryRemove = seamothEnergyMixin.soundBatteryRemove;
            em.batteryModels = seamothEnergyMixin.batteryModels;

            energyMixins.Add(em);

            var tmp = vb.Root.EnsureComponent<VehicleBatteryInput>();
            tmp.powerCellObject = vb.Root;
            tmp.mixin = em;
            tmp.translationKey = TranslationKey.HandOver_AutopilotBatterySlot;
            tmp.displayName = vb.DisplayName?.Text;
            tmp.displayNameLocalized = vb.DisplayName?.Localize ?? false;
            tmp.vehicle = this;

            var model = vb.Root.gameObject.EnsureComponent<BatteryProxy>();
            model.proxy = vb.BatteryProxy;
            model.mixin = em;

            SaveLoad.SaveLoadUtils.EnsureUniqueNameAmongSiblings(vb.Root.transform);
            var abi = vb.Root.EnsureComponent<SaveLoad.AvsBatteryIdentifier>();
            abi.av = this;
        }

        // Configure energy interface
        aiEnergyInterface = Com.BackupBatteries.First().Root.EnsureComponent<EnergyInterface>();
        aiEnergyInterface.sources = energyMixins.ToArray();
    }

    internal void SetupAmbienceSound(FMOD_StudioEventEmitter reference)
    {
        ambienceSound = reference.CopyComponentWithFieldsTo(gameObject);
    }

    internal void SetupLightSounds(FMOD_StudioEventEmitter[] fmods)
    {
        foreach (var fmod in fmods)
            if (fmod.asset.name == "seamoth_light_on")
            {
                //Log.Debug("Found light on sound for " + name);
                var ce = gameObject.AddComponent<FMOD_CustomEmitter>();
                ce.asset = fmod.asset;
                lightsOnSound = ce;
            }
            else if (fmod.asset.name == "seamoth_light_off")
            {
                //Log.Debug("Found light off sound for " + name);
                var ce = gameObject.AddComponent<FMOD_CustomEmitter>();
                ce.asset = fmod.asset;
                lightsOffSound = ce;
            }

        if (lightsOnSound.IsNull() || lightsOffSound.IsNull())
        {
            using var log = NewAvsLog();
            log.Error("Failed to find light sounds for " + name);
        }
    }
}