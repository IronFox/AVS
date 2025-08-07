using AVS.Assets;
using AVS.Configuration;
using AVS.Localization;
using AVS.Log;
using AVS.StorageComponents;
using AVS.Util;
using AVS.VehicleComponents;
using AVS.VehicleParts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MobileWaterPark = AVS.StorageComponents.MobileWaterPark;

namespace AVS.BaseVehicle
{
    public abstract partial class AvsVehicle
    {


        internal void SetupVolumetricLights()
        {
            if (SeamothHelper.Seamoth == null)
            {
                Log.Error("SeamothHelper.Seamoth is null. Cannot setup volumetric lights.");
                return;
            }
            GameObject seamothHeadLight = SeamothHelper.Seamoth.transform.Find("lights_parent/light_left").gameObject;
            Transform seamothVL = SeamothHelper.Seamoth.transform.Find("lights_parent/light_left/x_FakeVolumletricLight"); // sic
            MeshFilter seamothVLMF = seamothVL.GetComponent<MeshFilter>();
            MeshRenderer seamothVLMR = seamothVL.GetComponent<MeshRenderer>();
            List<VehicleSpotLightDefinition> theseLights = Com.HeadLights.ToList();
            if (this is VehicleTypes.Submarine subma)
            {
                theseLights.AddRange(subma.Com.FloodLights);
            }
            foreach (VehicleSpotLightDefinition pc in theseLights)
            {
                LogWriter.Default.Debug($"Setting up volumetric light for {pc.Light.NiceName()} on {this.NiceName()}");
                GameObject volumetricLight = new GameObject("VolumetricLight");
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
            Log.Debug(this, $"{nameof(AvsVehicle)}.{nameof(SubConstructionComplete)}");
            HudPingInstance.enabled = true;
            worldForces.handleGravity = true;
            BuildBotManager.ResetGhostMaterial();
        }


        /// <summary>
        /// Invoked via reflection by patches to notify the vehicle of a sub construction beginning.
        /// </summary>
        public virtual void SubConstructionBeginning()
        {
            Log.Debug(this, $"{nameof(AvsVehicle)}[#{Id}].{nameof(SubConstructionBeginning)}");
            if (HudPingInstance)
                HudPingInstance.enabled = false;
            else
                Log.Error($"HudPingInstance is null in {nameof(SubConstructionBeginning)} #{Id}");
            if (worldForces)
                worldForces.handleGravity = false;
            else
                Log.Error($"worldForces is null in {nameof(SubConstructionBeginning)} #{Id}");
        }

        /// <summary>
        /// Called via reflection once vehicle crafting is completed.
        /// </summary>
        /// <param name="techType">This vehicle's tech type</param>
        public virtual void OnCraftEnd(TechType techType)
        {
            Log.Write($"OnCraftEnd called for {techType}");
            IEnumerator GiveUsABatteryOrGiveUsDeath()
            {
                yield return new WaitForSeconds(2.5f);

                // give us an AI battery please
                TaskResult<GameObject> result = new TaskResult<GameObject>();
                yield return CraftData.InstantiateFromPrefabAsync(TechType.PowerCell, result, false);
                GameObject newAIBattery = result.Get();
                newAIBattery.GetComponent<Battery>().charge = 200;
                newAIBattery.transform.SetParent(Com.StorageRootObject.transform);
                if (aiEnergyInterface != null)
                {
                    aiEnergyInterface.sources.First().battery = newAIBattery.GetComponent<Battery>();
                    aiEnergyInterface.sources.First().batterySlot.AddItem(newAIBattery.GetComponent<Pickupable>());
                    newAIBattery.SetActive(false);
                }
                if (!energyInterface.hasCharge)
                {
                    yield return CraftData.InstantiateFromPrefabAsync(TechType.PowerCell, result, false);
                    GameObject newPowerCell = result.Get();
                    newPowerCell.GetComponent<Battery>().charge = 200;
                    newPowerCell.transform.SetParent(Com.StorageRootObject.transform);
                    var mixin = Com.PowerCells[0].Root.gameObject.GetComponent<EnergyMixin>();
                    mixin.battery = newPowerCell.GetComponent<Battery>();
                    mixin.batterySlot.AddItem(newPowerCell.GetComponent<Pickupable>());
                    newPowerCell.SetActive(false);
                }
            }
            if (Com.PowerCells != null && Com.PowerCells.Count() > 0)
            {
                UWE.CoroutineHost.StartCoroutine(GiveUsABatteryOrGiveUsDeath());
            }
        }

        internal void CheckEnergyInterface()
        {

            Log.Debug(this, $"{nameof(AvsVehicle)}.{nameof(CheckEnergyInterface)}");
            if (energyInterface.sources.Length < Com.PowerCells.Count)
            {
                Log.Error($"EnergyInterface for {this.NiceName()} has less sources than batteries. " +
                          $"Expected {Com.PowerCells.Count}, got {energyInterface.sources.Length}. " +
                          $"This is a bug, please report it.");
                List<EnergyMixin> energyMixins = new List<EnergyMixin>();
                foreach (VehicleParts.VehiclePowerCellDefinition vb in Com.PowerCells)
                {
                    energyMixins.Add(vb.Root.GetComponent<EnergyMixin>());

                }
                energyInterface.sources = energyMixins.ToArray();
            }
            Log.Debug(this, $"EnergyInterface for {energyInterface.NiceName()} has {energyInterface.sources.Length} sources.");
        }

        internal void SetupPowerCells()
        {
            Log.Debug(this, $"{nameof(AvsVehicle)}.{nameof(SetupPowerCells)}");
            var seamothEnergyMixin = SeamothHelper.RequireSeamoth.GetComponent<EnergyMixin>();
            List<EnergyMixin> energyMixins = new List<EnergyMixin>();
            if (Com.PowerCells.Count == 0)
            {
                // Configure energy mixin for this battery slot
                var energyMixin = gameObject.EnsureComponent<VehicleComponents.ForeverBattery>();
                energyMixin.storageRoot = Com.StorageRootObject.GetComponent<ChildObjectIdentifier>();
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
            foreach (VehicleParts.VehiclePowerCellDefinition vb in Com.PowerCells)
            {
                Log.Debug(this, $"Setting up Vehicle Power Cell {vb.DisplayName?.Text ?? vb.Root.name} for {this.NiceName()}");
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

                var model = vb.Root.gameObject.EnsureComponent<StorageComponents.BatteryProxy>();
                model.proxy = vb.BatteryProxy;
                model.mixin = energyMixin;

                SaveLoad.SaveLoadUtils.EnsureUniqueNameAmongSiblings(vb.Root.transform);
                vb.Root.EnsureComponent<SaveLoad.AvsBatteryIdentifier>();
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
        public virtual Recipe OnRecipeOverride(Recipe recipe)
        {
            return recipe;
        }


        /// <summary>
        /// True if the vehicle is constructed and ready to be piloted.
        /// </summary>
        public bool IsConstructed => vfxConstructing == null || vfxConstructing.IsConstructed();

        /// <summary>
        /// Constructs the vehicle's ping instance as part of the prefab setup.
        /// </summary>
        /// <param name="pingType"></param>
        internal void PrefabSetupHudPing(PingType pingType)
        {
            Log.Write($"Setting up HudPingInstance for {nameof(AvsVehicle)} #{Id}");
            hudPingInstance = gameObject.EnsureComponent<PingInstance>();
            hudPingInstance.origin = transform;
            hudPingInstance.pingType = pingType;
            hudPingInstance.SetLabel("Vehicle");
        }


        internal bool ReSetupModularStorages()
        {
            int iter = 0;
            try
            {
                foreach (VehicleParts.VehicleStorage vs in Com.ModularStorages)
                {
                    LogWriter.Default.Debug("Setting up Modular Storage " + vs.Container.NiceName() + " for " + this.NiceName());
                    vs.Container.SetActive(false);

                    LogWriter.Default.Debug("Scanning seamoth");
                    var sm = SeamothHelper.RequireSeamoth;
                    LogWriter.Default.Debug("Found seamoth: " + sm.NiceName());
                    var storage = sm.transform.Find("Storage/Storage1");
                    if (storage == null)
                    {
                        LogWriter.Default.Error("Could not find Storage/Storage1 in the Seamoth prefab");
                        return false;
                    }
                    FMODAsset storageCloseSound = storage.GetComponent<SeamothStorageInput>().closeSound;
                    FMODAsset storageOpenSound = storage.GetComponent<SeamothStorageInput>().openSound;
                    LogWriter.Default.Debug("Setting up");
                    var inp = vs.Container.EnsureComponent<ModularStorageInput>();
                    var name = vs.DisplayName ?? Text.Untranslated("Modular Vehicle Storage " + iter);
                    inp.displayName = name;
                    inp.mv = this;
                    inp.slotID = iter;
                    iter++;
                    inp.model = vs.Container;
                    if (vs.Container.GetComponentInChildren<Collider>() is null)
                    {
                        inp.collider = vs.Container.EnsureComponent<BoxCollider>();
                    }
                    inp.openSound = storageOpenSound;
                    inp.closeSound = storageCloseSound;
                }
                return true;
            }
            catch (Exception e)
            {
                LogWriter.Default.Error("There was a problem setting up the Modular Storage. Check VehicleStorage.Container and ModVehicle.StorageRootObject", e);
                return false;
            }
        }

        internal bool ReSetupWaterParks()
        {
            int iter = 0;
            try
            {
                LogWriter.Default.Debug($"Setting up {Com.WaterParks.Count} Mobile Water Parks");
                foreach (var vp in Com.WaterParks)
                {
                    vp.Container.SetActive(false);

                    var cont = vp.Container.EnsureComponent<MobileWaterPark>();
                    LogWriter.Default.Debug("Setting up Mobile Water Park " + cont.NiceName() + $" '{cont.DisplayName}'");
                    var name = vp.DisplayName ?? Text.Untranslated("Innate Vehicle Storage " + iter);
                    cont.Setup(this, name, vp, iter + 1);
                    FMODAsset storageCloseSound = SeamothHelper.RequireSeamoth.transform.Find("Storage/Storage1").GetComponent<SeamothStorageInput>().closeSound;
                    FMODAsset storageOpenSound = SeamothHelper.RequireSeamoth.transform.Find("Storage/Storage1").GetComponent<SeamothStorageInput>().openSound;
                    var inp = vp.Container.EnsureComponent<WaterParkStorageInput>();
                    inp.displayName = name;
                    inp.mv = this;
                    inp.slotID = iter;
                    iter++;
                    inp.model = vp.Container;
                    if (vp.Container.GetComponentInChildren<Collider>() is null)
                    {
                        inp.collider = vp.Container.EnsureComponent<BoxCollider>();
                    }
                    inp.openSound = storageOpenSound;
                    inp.closeSound = storageCloseSound;
                    vp.Container.SetActive(true);

                    SaveLoad.SaveLoadUtils.EnsureUniqueNameAmongSiblings(vp.Container.transform);
                }
                return true;
            }
            catch (Exception e)
            {
                LogWriter.Default.Error("There was a problem setting up the Innate Storage. Check VehicleStorage.Container and ModVehicle.StorageRootObject", e);
                return false;
            }
        }

        internal bool ReSetupInnateStorages()
        {

            int iter = 0;
            try
            {
                foreach (VehicleParts.VehicleStorage vs in Com.InnateStorages)
                {
                    vs.Container.SetActive(false);

                    var cont = vs.Container.EnsureComponent<InnateStorageContainer>();
                    var name = vs.DisplayName ?? Text.Untranslated("Innate Vehicle Storage " + iter);
                    cont.storageRoot = Com.StorageRootObject.GetComponent<ChildObjectIdentifier>();
                    cont.DisplayName = name;
                    cont.height = vs.Height;
                    cont.width = vs.Width;
                    cont.isAllowedToAdd = vs.InnateIsAllowedToAdd;
                    cont.isAllowedToRemove = vs.InnateIsAllowedToRemove;
                    //cont.name = "Innate Vehicle Storage " + iter;

                    LogWriter.Default.Debug("Setting up Innate Storage " + cont.NiceName() + $" '{cont.DisplayName}'");

                    FMODAsset storageCloseSound = SeamothHelper.RequireSeamoth.transform.Find("Storage/Storage1").GetComponent<SeamothStorageInput>().closeSound;
                    FMODAsset storageOpenSound = SeamothHelper.RequireSeamoth.transform.Find("Storage/Storage1").GetComponent<SeamothStorageInput>().openSound;
                    var inp = vs.Container.EnsureComponent<InnateStorageInput>();
                    inp.displayName = name;
                    inp.mv = this;
                    inp.slotID = iter;
                    iter++;
                    inp.model = vs.Container;
                    if (vs.Container.GetComponentInChildren<Collider>() is null)
                    {
                        inp.collider = vs.Container.EnsureComponent<BoxCollider>();
                    }
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
                LogWriter.Default.Error("There was a problem setting up the Innate Storage. Check VehicleStorage.Container and ModVehicle.StorageRootObject", e);
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
            List<EnergyMixin> energyMixins = new List<EnergyMixin>();
            foreach (VehiclePowerCellDefinition vb in Com.BackupBatteries)
            {
                // Configure energy mixin for this battery slot
                vb.Root.GetComponents<EnergyMixin>().ForEach(em => GameObject.Destroy(em)); // remove old energy mixins
                var em = vb.Root.AddComponent<DebugBatteryEnergyMixin>();
                em.originalProxy = vb.BatteryProxy;
                em.storageRoot = Com.StorageRootObject.GetComponent<ChildObjectIdentifier>();
                em.defaultBattery = seamothEnergyMixin.defaultBattery;
                em.compatibleBatteries = new List<TechType>() { TechType.PowerCell, TechType.PrecursorIonPowerCell };
                em.soundPowerUp = seamothEnergyMixin.soundPowerUp;
                em.soundPowerDown = seamothEnergyMixin.soundPowerDown;
                em.soundBatteryAdd = seamothEnergyMixin.soundBatteryAdd;
                em.soundBatteryRemove = seamothEnergyMixin.soundBatteryRemove;
                em.batteryModels = seamothEnergyMixin.batteryModels;

                energyMixins.Add(em);

                var tmp = vb.Root.EnsureComponent<VehicleBatteryInput>();
                tmp.powerCellObject = vb.Root;
                tmp.mixin = em;
                tmp.translationKey = TranslationKey.HandOver_AutoPilotBatterySlot;
                tmp.displayName = vb.DisplayName?.Text;
                tmp.displayNameLocalized = vb.DisplayName?.Localize ?? false;
                tmp.vehicle = this;

                var model = vb.Root.gameObject.EnsureComponent<StorageComponents.BatteryProxy>();
                model.proxy = vb.BatteryProxy;
                model.mixin = em;

                SaveLoad.SaveLoadUtils.EnsureUniqueNameAmongSiblings(vb.Root.transform);
                vb.Root.EnsureComponent<SaveLoad.AvsBatteryIdentifier>();
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
            foreach (FMOD_StudioEventEmitter fmod in fmods)
            {
                if (fmod.asset.name == "seamoth_light_on")
                {
                    Log.Debug("Found light on sound for " + name);
                    var ce = gameObject.AddComponent<FMOD_CustomEmitter>();
                    ce.asset = fmod.asset;
                    lightsOnSound = ce;
                }
                else if (fmod.asset.name == "seamoth_light_off")
                {
                    Log.Debug("Found light off sound for " + name);
                    var ce = gameObject.AddComponent<FMOD_CustomEmitter>();
                    ce.asset = fmod.asset;
                    lightsOffSound = ce;
                }
            }
            if (lightsOnSound == null || lightsOffSound == null)
            {
                Log.Error("Failed to find light sounds for " + name);
            }

        }
    }
}
