using AVS.Assets;
using AVS.Configuration;
using AVS.Localization;
using AVS.Log;
using AVS.Util;
using AVS.VehicleParts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
            List<VehicleFloodLight> theseLights = Com.HeadLights.ToList();
            if (this is VehicleTypes.Submarine subma)
            {
                theseLights.AddRange(subma.Com.FloodLights);
            }
            foreach (VehicleFloodLight pc in theseLights)
            {
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
                leftVFX.angle = (int)pc.Angle;
                leftVFX.range = pc.Range;
                VolumetricLights.Add(volumetricLight);
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
                    var mixin = Com.Batteries[0].BatterySlot.gameObject.GetComponent<EnergyMixin>();
                    mixin.battery = newPowerCell.GetComponent<Battery>();
                    mixin.batterySlot.AddItem(newPowerCell.GetComponent<Pickupable>());
                    newPowerCell.SetActive(false);
                }
            }
            if (Com.Batteries != null && Com.Batteries.Count() > 0)
            {
                UWE.CoroutineHost.StartCoroutine(GiveUsABatteryOrGiveUsDeath());
            }
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

        internal bool ReSetupInnateStorages()
        {

            int iter = 0;
            try
            {
                if (Com.InnateStorages != null)
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
                        vs.Container.EnsureComponent<SaveLoad.VFInnateStorageIdentifier>();
                    }
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
            foreach (VehicleBattery vb in Com.BackupBatteries)
            {
                // Configure energy mixin for this battery slot
                var em = vb.BatterySlot.EnsureComponent<EnergyMixin>();
                em.storageRoot = Com.StorageRootObject.GetComponent<ChildObjectIdentifier>();
                em.defaultBattery = seamothEnergyMixin.defaultBattery;
                em.compatibleBatteries = new List<TechType>() { TechType.PowerCell, TechType.PrecursorIonPowerCell };
                em.soundPowerUp = seamothEnergyMixin.soundPowerUp;
                em.soundPowerDown = seamothEnergyMixin.soundPowerDown;
                em.soundBatteryAdd = seamothEnergyMixin.soundBatteryAdd;
                em.soundBatteryRemove = seamothEnergyMixin.soundBatteryRemove;
                em.batteryModels = seamothEnergyMixin.batteryModels;

                energyMixins.Add(em);

                var tmp = vb.BatterySlot.EnsureComponent<VehicleBatteryInput>();
                tmp.mixin = em;
                tmp.tooltip = "VFAutoPilotBattery";

                var model = vb.BatterySlot.gameObject.EnsureComponent<StorageComponents.BatteryProxy>();
                model.proxy = vb.BatteryProxy;
                model.mixin = em;

                SaveLoad.SaveLoadUtils.EnsureUniqueNameAmongSiblings(vb.BatterySlot.transform);
                vb.BatterySlot.EnsureComponent<SaveLoad.AvsBatteryIdentifier>();
            }
            // Configure energy interface
            aiEnergyInterface = Com.BackupBatteries.First().BatterySlot.EnsureComponent<EnergyInterface>();
            aiEnergyInterface.sources = energyMixins.ToArray();
        }

        internal void SetupAmbienceSound(FMOD_StudioEventEmitter reference)
        {
            ambienceSound = reference.CopyComponentWithFieldsTo(gameObject);
        }
    }
}
