using AVS.Composition;
using AVS.Configuration;
using AVS.MaterialAdapt;
using AVS.Util;
using AVS.VehicleComponents;
using AVS.VehicleTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AVS
{
    /// <summary>
    /// ModVehicle is the primary abstract class provided by Vehicle Framework. 
    /// All VF vehicles inherit from ModVehicle.
    /// </summary>
    public abstract class ModVehicle : Vehicle, ICraftTarget, IProtoTreeEventListener, ILogFilter
    {

        /// <summary>
        /// The piloting style of the vehicle.
        /// </summary>
        public enum PilotingStyle
        {
            /// <summary>
            /// Arms expected to grab a wheel
            /// </summary>
            Cyclops,
            /// <summary>
            /// Arms expected to grab joysticks
            /// </summary>
            Seamoth,
            /// <summary>
            /// Arms expected to grab joysticks
            /// </summary>
            Prawn,
            /// <summary>
            /// Arm animations controled via <see cref="HandleOtherPilotingAnimations(bool)"/>
            /// </summary>
            Other
        }


        /// <summary>
        /// The material fixer instance used for this vehicle.
        /// Ineffective if <see cref="VehicleConfiguration.AutoFixMaterials"/> is false.
        /// </summary>
        public MaterialFixer MaterialFixer { get; }
        /// <summary>
        /// The root game object of this vehicle. Usually the same as the vehicle game object.
        /// </summary>
        public virtual GameObject VehicleRoot => gameObject;

        //public virtual GameObject BoundingBox => null; // Prefer to use BoundingBoxCollider directly (don't use this)

        /// <summary>
        /// Invariant vehicle configuration. Initialized during construction.
        /// Never null.
        /// </summary>
        public VehicleConfiguration Config { get; }

        /// <summary>
        /// True to log high-verbosity debug messages (as non-debug)
        /// </summary>
        public virtual bool LogDebug { get; } = false;

        /// <summary>
        /// Retrieves the composition of the vehicle.
        /// Executed once either during <see cref="Awake()"/> or vehicle registration, whichever comes first.
        /// </summary>
        public abstract VehicleComposition GetVehicleComposition();

        /// <summary>
        /// Resolved vehicle composition.
        /// If accessed before <see cref="Awake()"/> (or vehicle registration), InvalidOperationException will be thrown.
        /// </summary>
        public VehicleComposition Com => _composition
            ?? throw new InvalidOperationException("This vehicle's composition has not yet been initialized. Please wait until ModVehicle.Awake() has been called");
        private VehicleComposition? _composition = null;

        /// <summary>
        /// Constructs the vehicle with the given configuration.
        /// </summary>
        /// <param name="config">Vehicle configuration. Must not be null</param>
        /// <exception cref="ArgumentNullException"></exception>
        protected ModVehicle(VehicleConfiguration config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config), "VehicleConfiguration cannot be null");
            MaterialFixer = new MaterialFixer(this, config.MaterialAdaptConfig.LogConfig, () => this.ResolveMaterial(config.MaterialAdaptConfig));
            baseColor = config.InitialBaseColor;
            stripeColor = config.InitialStripeColor;
            nameColor = config.InitialNameColor;
            interiorColor = config.InitialInteriorColor;
        }


        private IEnumerable<SurfaceShaderData> ResolveMaterial(IMaterialAdaptConfig config)
        {
            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (config.IsExcludedFromMaterialFixing(renderer.gameObject, Com))
                {
                    config.LogConfig.LogExtraStep($"Skipping renderer {renderer.NiceName()} because it is excluded from material fixing");
                    continue;
                }

                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    if (config.IsExcludedFromMaterialFixing(renderer, i, renderer.materials[i]))
                    {
                        config.LogConfig.LogExtraStep($"Skipping material {i} of {renderer.NiceName()} ({renderer.materials[i].NiceName()}) because it is excluded from material fixing");
                        continue;
                    }

                    var material = SurfaceShaderData.From(renderer, i, config.LogConfig, Config.IgnoreShaderNameWhenFixingMaterial);
                    if (material != null)
                        yield return material;
                }
            }
        }


        internal void OnAwakeOrPrefabricate()
        {
            RequireComposition();
        }
        /// <summary>
        /// Initialized <see cref="Com"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        private void RequireComposition()
        {
            if (_composition == null)
            {
                _composition = GetVehicleComposition();
                if (_composition == null)
                {
                    throw new InvalidOperationException("Vehicle composition cannot be null. Please ensure Get*Composition() is implemented correctly.");
                }
            }
        }


        private static int idCounter = 0;
        /// <summary>
        /// A unique ID for this vehicle instance.
        /// </summary>
        public int Id { get; } = idCounter++;

        private VoiceQueue? voiceQueue;
        /// <summary>
        /// The voice queue for this vehicle.
        /// Set by <see cref="Awake()"/>.
        /// </summary>
        public VoiceQueue VoiceQueue => voiceQueue.OrThrow(
            () => new InvalidOperationException($"Trying to access VoiceQueue before Awake() was called")
            );

        private Autopilot? autopilot;

        /// <summary>
        /// Gets the AutoPilot system associated with the current instance.
        /// Set by <see cref="Awake()"/>.
        /// </summary>
        public Autopilot Autopilot => autopilot.OrThrow(
            () => new InvalidOperationException($"Trying to access Autopilot before Awake() was called")
            );
        ///<inheritdoc />
        public override void Awake()
        {
            OnAwakeOrPrefabricate();
            hudPingInstance = gameObject.GetComponent<PingInstance>();//created during prefab. Cannot properly create here if missing
            voiceQueue = gameObject.EnsureComponent<VoiceQueue>();

            energyInterface = GetComponent<EnergyInterface>();

            powerManager = gameObject.EnsureComponent<PowerManager>();

            base.Awake();
            VehicleManager.EnrollVehicle(this); // Register our new vehicle with Vehicle Framework
            UpgradeOnAddedActions.Add(StorageModuleAction);
            UpgradeOnAddedActions.Add(ArmorPlatingModuleAction);
            UpgradeOnAddedActions.Add(PowerUpgradeModuleAction);

            SetupVolumetricLights();
            HeadlightsController = gameObject.EnsureComponent<HeadLightsController>();
            gameObject.AddComponent<VolumetricLightController>();

            autopilot = gameObject.EnsureComponent<Autopilot>();

            base.LazyInitialize();
            Com.Upgrades.ForEach(x => x.Interface.GetComponent<VehicleUpgradeConsoleInput>().equipment = modules);
            var warpChipThing = GetComponent("TelePingVehicleInstance");
            if (warpChipThing != null)
            {
                Component.DestroyImmediate(warpChipThing);
            }
            vfxConstructing = GetComponent<VFXConstructing>();
        }

        internal void SetupVolumetricLights()
        {
            if (SeamothHelper.Seamoth == null)
            {
                Logger.Error("SeamothHelper.Seamoth is null. Cannot setup volumetric lights.");
                return;
            }
            GameObject seamothHeadLight = SeamothHelper.Seamoth.transform.Find("lights_parent/light_left").gameObject;
            Transform seamothVL = SeamothHelper.Seamoth.transform.Find("lights_parent/light_left/x_FakeVolumletricLight"); // sic
            MeshFilter seamothVLMF = seamothVL.GetComponent<MeshFilter>();
            MeshRenderer seamothVLMR = seamothVL.GetComponent<MeshRenderer>();
            List<VehicleParts.VehicleFloodLight> theseLights = Com.HeadLights.ToList();
            if (this is VehicleTypes.Submarine subma)
            {
                theseLights.AddRange(subma.Com.FloodLights);
            }
            foreach (VehicleParts.VehicleFloodLight pc in theseLights)
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

        ///<inheritdoc />
        public override void Start()
        {
            base.Start();

            upgradesInput.equipment = modules;
            modules.isAllowedToRemove = new IsAllowedToRemove(IsAllowedToRemove);

            // lost this in the update to Nautilus. We're no longer tracking our own tech type IDs or anything,
            // so I'm not able to provide the value easily here. Not even sure what a GameInfoIcon is :shrug:
            gameObject.EnsureComponent<GameInfoIcon>().techType = TechType;
            GameInfoIcon.Add(TechType);
            //hasStarted = true;
        }
        ///<inheritdoc />
        public override void Update()
        {
            if (Config.AutoFixMaterials)
                MaterialFixer.OnUpdate();
            if (isScuttled)
            {
                if (IsVehicleDocked)
                {
                    this.Undock();
                }
                return;
            }
            base.Update();
            HandleExtraQuickSlotInputs();
        }
        /// <inheritdoc />
        public override void FixedUpdate()
        {
            ManagePhysics();
        }
        /// <summary>
        /// To be executed when the vehicle is killed.
        /// </summary>
        /// <remarks>Calls <see cref="DestroyMV"/> </remarks>
        public new virtual void OnKill()
        {
            liveMixin.health = 0;
            if (IsUnderCommand)
            {
                Player.main.playerController.SetEnabled(true);
                Player.main.mode = Player.Mode.Normal;
                Player.main.playerModeChanged.Trigger(Player.main.mode);
                Player.main.sitting = false;
                Player.main.playerController.ForceControllerSize();
                Player.main.transform.parent = null;
                StopPiloting();
                PlayerExit();
            }
            DestroyMV();
        }
        /// <summary>
        /// Executed if a toggleable upgrade module is toggled on or off.
        /// </summary>
        /// <param name="slotID">Upgrade module slot</param>
        /// <param name="active">True if has been toggled on, false if off</param>
        public override void OnUpgradeModuleToggle(int slotID, bool active)
        {
            TechType techType = modules.GetTechTypeInSlot(slotIDs[slotID]);
            UpgradeTypes.ToggleActionParams param = new UpgradeTypes.ToggleActionParams
            {
                active = active,
                vehicle = this,
                slotID = slotID,
                techType = techType
            };
            Admin.UpgradeRegistrar.OnToggleActions.ForEach(x => x(param));
            base.OnUpgradeModuleToggle(slotID, active);
        }
        /// <summary>
        /// Executed when a usable upgrade module is used.
        /// </summary>
        /// <param name="techType">The tech type of the upgrade being used</param>
        /// <param name="slotID">Upgrade module slot</param>
        public override void OnUpgradeModuleUse(TechType techType, int slotID)
        {
            UpgradeTypes.SelectableActionParams param = new UpgradeTypes.SelectableActionParams
            (
                vehicle: this,
                slotID: slotID,
                techType: techType
            );
            Admin.UpgradeRegistrar.OnSelectActions.ForEach(x => x(param));

            UpgradeTypes.SelectableChargeableActionParams param2 = new UpgradeTypes.SelectableChargeableActionParams
            (
                vehicle: this,
                slotID: slotID,
                techType: techType,
                charge: param.Vehicle.quickSlotCharge[param.SlotID],
                slotCharge: param.Vehicle.GetSlotCharge(param.SlotID)
            );
            Admin.UpgradeRegistrar.OnSelectChargeActions.ForEach(x => x(param2));

            AVS.Patches.CompatibilityPatches.BetterVehicleStoragePatcher.TryUseBetterVehicleStorage(this, slotID, techType);
            base.OnUpgradeModuleUse(techType, slotID);
        }
        /// <summary>
        /// Executed when an upgrade module is added or removed.
        /// </summary>
        /// <param name="slotID">Slot index the module is added to or removed from</param>
        /// <param name="techType">Tech type of the module being added or removed</param>
        /// <param name="added">True if the module is added, false if removed</param>
        public override void OnUpgradeModuleChange(int slotID, TechType techType, bool added)
        {
            UpgradeOnAddedActions.ForEach(x => x(slotID, techType, added));
            UpgradeTypes.AddActionParams addedParams = new UpgradeTypes.AddActionParams
            {
                vehicle = this,
                slotID = slotID,
                techType = techType,
                isAdded = added
            };
            Admin.UpgradeRegistrar.OnAddActions.ForEach(x => x(addedParams));
        }
        /// <summary>
        /// Gets the quick slot type of the given slot ID.
        /// </summary>
        /// <param name="slotID">Slot index with 0 being the first</param>
        /// <returns>Slotted inventory item or null</returns>
        public override InventoryItem? GetSlotItem(int slotID)
        {
            if (slotID < 0 || slotID >= this.slotIDs.Length)
            {
                return null;
            }
            string slot = this.slotIDs[slotID];
            if (upgradesInput.equipment.equipment.TryGetValue(slot, out InventoryItem result))
            {
                return result;
            }
            return null;
        }
        /// <summary>
        /// Gets the current vehicle depth and crush depth.
        /// </summary>
        /// <param name="depth">Vehicle depth</param>
        /// <param name="crushDepth">Crush depth</param>
        public override void GetDepth(out int depth, out int crushDepth)
        {
            depth = Mathf.FloorToInt(GetComponent<CrushDamage>().GetDepth());
            crushDepth = Mathf.FloorToInt(GetComponent<CrushDamage>().crushDepth);
        }

        /// <summary>
        /// Invoked via reflection by patches to notify the vehicle of a sub construction completion.
        /// </summary>
        public override void SubConstructionComplete()
        {
            Logger.DebugLog(this, "ModVehicle SubConstructionComplete");
            HudPingInstance.enabled = true;
            worldForces.handleGravity = true;
            BuildBotManager.ResetGhostMaterial();
        }
        public override void DeselectSlots() // This happens when you press the Exit button while having a "currentMountedVehicle."
        {
            if (ignoreInput)
            {
                return;
            }
            int i = 0;
            int num = slotIDs.Length;
            while (i < num)
            {
                QuickSlotType quickSlotType = GetQuickSlotType(i, out _);
                if (quickSlotType == QuickSlotType.Toggleable || quickSlotType == QuickSlotType.Selectable || quickSlotType == QuickSlotType.SelectableChargeable)
                {
                    ToggleSlot(i, false);
                }
                quickSlotCharge[i] = 0f;
                i++;
            }
            activeSlot = -1;
            NotifySelectSlot(activeSlot);
            DoExitRoutines();
        }

        /// <summary>
        /// The slotIds of the vehicle.
        /// </summary>
        public override string[] slotIDs
        {
            get
            {
                if (_slotIDs == null)
                {
                    _slotIDs = GenerateSlotIDs(Config.NumModules);
                }
                return _slotIDs;
            }
        }
        /// <summary>
        /// Vehicle default name
        /// </summary>
        public override string vehicleDefaultName
        {
            get
            {
                Language main = Language.main;
                if (main == null)
                {
                    return Language.main.Get("VFVehicle");
                }
                return main.Get("ModVehicle");
            }
        }

        /// <summary>
        /// Enters the player into the sub, updates the quickbar and notifies the player of the piloting status.
        /// </summary>
        public virtual void BeginPiloting()
        {
            // BeginPiloting is the VF trigger to start controlling a vehicle.
            EnterVehicle(Player.main, true);
            uGUI.main.quickSlots.SetTarget(this);
            NotifyStatus(PlayerStatus.OnPilotBegin);
        }
        /// <summary>
        /// Stops the piloting of the current vehicle and resets the control state.
        /// </summary>
        /// <remarks>This method disengages the player from controlling a vehicle and resets any
        /// associated UI elements.  It also triggers a notification to update the player's status to reflect the end of
        /// piloting.</remarks>
        public virtual void StopPiloting()
        {
            // StopPiloting is the VF trigger to discontinue controlling a vehicle.
            uGUI.main.quickSlots.SetTarget(null);
            NotifyStatus(PlayerStatus.OnPilotEnd);
        }
        /// <summary>
        /// Called when the player enters the vehicle.
        /// </summary>
        public virtual void PlayerEntry()
        {
            Logger.DebugLog(this, "start modvehicle player entry");
            if (!isScuttled && !IsUnderCommand)
            {
                IsUnderCommand = true;
                Player.main.SetScubaMaskActive(false);
                try
                {
                    foreach (GameObject window in Com.CanopyWindows)
                    {
                        window.SafeSetActive(false);
                    }
                }
                catch (Exception)
                {
                    //It's okay if the vehicle doesn't have a canopy
                }
                Player.main.lastValidSub = GetComponent<SubRoot>();
                Player.main.SetCurrentSub(GetComponent<SubRoot>(), true);
                NotifyStatus(PlayerStatus.OnPlayerEntry);
                HudPingInstance.enabled = false;
            }
        }
        /// <summary>
        /// Called when the player exits the vehicle.
        /// </summary>
        public virtual void PlayerExit()
        {
            Logger.DebugLog(this, "start modvehicle player exit");
            if (IsUnderCommand)
            {
                try
                {
                    foreach (GameObject window in Com.CanopyWindows)
                    {
                        window.SafeSetActive(true);
                    }
                }
                catch (Exception)
                {
                    //It's okay if the vehicle doesn't have a canopy
                }
            }
            IsUnderCommand = false;
            if (Player.main.GetCurrentSub() == GetComponent<SubRoot>())
            {
                Player.main.SetCurrentSub(null);
            }
            if (Player.main.GetVehicle() == this)
            {
                Player.main.currentMountedVehicle = null;
            }
            NotifyStatus(PlayerStatus.OnPlayerExit);
            Player.main.transform.SetParent(null);
            Player.main.TryEject(); // for DeathRun Remade Compat. See its patch in PlayerPatcher.cs
            HudPingInstance.enabled = true;
        }

        /// <summary>
        /// Invoked via reflection by patches to notify the vehicle of a sub construction beginning.
        /// </summary>
        public virtual void SubConstructionBeginning()
        {
            Logger.DebugLog(this, $"ModVehicle#{this.Id} SubConstructionBeginning");
            if (HudPingInstance)
                HudPingInstance.enabled = false;
            else
                Logger.Error($"HudPingInstance is null in SubConstructionBeginning #{this.Id}");
            if (worldForces)
                worldForces.handleGravity = false;
            else
                Logger.Error($"worldForces is null in SubConstructionBeginning #{this.Id}");
        }

        /// <summary>
        /// Supposed to be called when the AI battery is reloaded.
        /// The way it's implement now, this appears to be called when any battery is reloaded.
        /// </summary>
        public virtual void OnAIBatteryReload()
        {
        }
        /// <summary>
        /// Executed when the PDA storage is opened or closed.
        /// </summary>
        /// <param name="name">Name of the storage being opened or closed</param>
        /// <param name="open">True if the storage was opened, otherwise false</param>
        /// <returns>
        /// The number of seconds to wait before opening the PDF, to show off the cool animations
        /// </returns>
        public virtual float OnStorageOpen(string name, bool open)
        {
            return 0;
        }

        /// <summary>
        /// Detects if the vehicle is currently underwater.
        /// </summary>
        /// <returns>true if underwater</returns>
        public virtual bool GetIsUnderwater()
        {
            bool isBeneathSurface = !worldForces.IsAboveWater();
            return isBeneathSurface && !precursorOutOfWater;
        }

        public virtual void OnCraftEnd(TechType techType)
        {
            Logger.Log($"OnCraftEnd called for {techType}");
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
        /// Executed when the vehicle docks in a docking bay (e.g. a moonpool).
        /// </summary>
        /// <remarks>Calls <see cref="OnPlayerDocked(Vector3)" /> if the vessel is currently being controlled</remarks>
        /// <param name="exitLocation">
        /// The location the player should exit to after docking</param>
        public virtual void OnVehicleDocked(Vector3 exitLocation)
        {
            if (Config.AutoFixMaterials)
                MaterialFixer.OnVehicleDocked();
            // The Moonpool invokes this once upon vehicle entry into the dock
            IsVehicleDocked = true;
            if (IsUnderCommand)
                OnPlayerDocked(exitLocation);
            useRigidbody.detectCollisions = false;
            foreach (var component in GetComponentsInChildren<IDockListener>())
                component.OnDock();
        }

        /// <summary>
        /// Executed when the player should evict the vehicle after being docked in a docking bay (e.g. a moonpool).
        /// </summary>
        /// <remarks>Calls <see cref="PlayerExit()" /></remarks>
        /// <param name="exitLocation">If non-zero, the player should relocate to this location</param>
        public virtual void OnPlayerDocked(Vector3 exitLocation)
        {
            PlayerExit();
            if (exitLocation != Vector3.zero)
            {
                Player.main.transform.position = exitLocation;
                Player.main.transform.LookAt(this.transform);
            }
        }

        /// <summary>
        /// Executed when the vehicle undocks from a docking bay (e.g. a moonpool).
        /// </summary>
        /// <remarks>Calls <see cref="OnPlayerUndocked()" /></remarks>
        public virtual void OnVehicleUndocked()
        {
            if (Config.AutoFixMaterials)
                MaterialFixer.OnVehicleUndocked();
            // The Moonpool invokes this once upon vehicle exit from the dock
            if (!isScuttled && !Admin.ConsoleCommands.isUndockConsoleCommand)
            {
                OnPlayerUndocked();
            }
            IsVehicleDocked = false;
            foreach (var component in GetComponentsInChildren<IDockListener>())
            {
                component.OnUndock();
            }
            IEnumerator EnsureCollisionsEnabledEventually()
            {
                yield return new WaitForSeconds(5f);
                useRigidbody.detectCollisions = true;
            }
            UWE.CoroutineHost.StartCoroutine(EnsureCollisionsEnabledEventually());
        }

        /// <summary>
        /// Executed when the player should reenter a newly undocked local vehicle.
        /// </summary>
        /// <remarks>Calls <see cref="PlayerEntry()"/></remarks>
        public virtual void OnPlayerUndocked()
        {
            PlayerEntry();
        }

        /// <summary>
        /// Loosely computes the bounding dimensions of the vehicle.
        /// </summary>
        public virtual Vector3 GetBoundingDimensions()
        {
            var box = Com.BoundingBoxCollider;
            if (box == null)
            {
                return Vector3.zero;
            }
            Vector3 boxDimensions = box.size;
            Vector3 worldScale = box.transform.lossyScale;
            return Vector3.Scale(boxDimensions, worldScale);
        }

        /// <summary>
        /// Gets the difference between the vehicle's position and the center of its bounding box.
        /// </summary>
        public virtual Vector3 GetDifferenceFromCenter()
        {
            var box = Com.BoundingBoxCollider;
            if (box != null)
            {
                Vector3 colliderCenterWorld = box.transform.TransformPoint(box.center);
                Vector3 difference = colliderCenterWorld - transform.position;
                return difference;
            }
            return Vector3.zero;
        }
        /// <summary>
        /// Animation routine to execute when the vehicle is (un)docked in a moonpool.
        /// </summary>
        /// <param name="moonpool"></param>
        public virtual void AnimateMoonPoolArms(VehicleDockingBay moonpool)
        {
            // AnimateMoonPoolArms is called in VehicleDockingBay.LateUpdate when a ModVehicle is docked in a moonpool.
            // This line sets the arms of the moonpool to do exactly as they do for the seamoth
            // There is also "exosuit_docked"
            SafeAnimator.SetBool(moonpool.animator, "seamoth_docked", moonpool.vehicle_docked_param && moonpool.dockedVehicle != null);
        }

        /// <summary>
        /// Destroys the vehicle and executes the death action.
        /// </summary>
        /// <remarks>Calls <see cref="DeathAction" /> and <see cref="ScuttleVehicle" /></remarks>
        public virtual void DestroyMV()
        {
            DeathAction();
            ScuttleVehicle();
        }
        /// <summary>
        /// Executed when the vehicle is destroyed.
        /// This default behavior lets the vehicle slowly sink to the bottom of the ocean.
        /// </summary>
        public virtual void DeathAction()
        {
            worldForces.enabled = true;
            worldForces.handleGravity = true;
            worldForces.underwaterGravity = 1.5f;
        }

        /// <summary>
        /// Executed when the vehicle is destroyed.
        /// Sets this vehicle as ready to be salvaged.
        /// </summary>
        public virtual void ScuttleVehicle()
        {
            if (isScuttled)
            {
                return;
            }
            HudPingInstance.enabled = false;
            void OnCutOpen(Sealed sealedComp)
            {
                OnSalvage();
            }
            isScuttled = true;
            foreach (var component in GetComponentsInChildren<IScuttleListener>())
            {
                (component as IScuttleListener).OnScuttle();
            }
            Com.WaterClipProxies.ForEach(x => x.SetActive(false));
            IsPoweredOn = false;
            gameObject.EnsureComponent<Scuttler>().Scuttle();
            var sealedThing = gameObject.EnsureComponent<Sealed>();
            sealedThing.openedAmount = 0;
            sealedThing.maxOpenedAmount = liveMixin.maxHealth / 5f;
            sealedThing.openedEvent.AddHandler(gameObject, new UWE.Event<Sealed>.HandleFunction(OnCutOpen));
        }
        /// <summary>
        /// Returns the vehicle to a non-scuttled state.
        /// </summary>
        public virtual void UnscuttleVehicle()
        {
            isScuttled = false;
            foreach (var component in GetComponentsInChildren<IScuttleListener>())
            {
                component.OnUnscuttle();
            }
            Com.WaterClipProxies.ForEach(x => x.SetActive(true));
            IsPoweredOn = true;
            gameObject.EnsureComponent<Scuttler>().Unscuttle();
        }
        /// <summary>
        /// Executed when the vehicle is salvaged.
        /// </summary>
        public virtual void OnSalvage()
        {
            IEnumerator DropLoot(Vector3 place, GameObject root)
            {
                TaskResult<GameObject> result = new TaskResult<GameObject>();
                foreach (var item in Config.Recipe)
                {
                    for (int i = 0; i < item.Amount; i++)
                    {
                        yield return null;
                        if (UnityEngine.Random.value < 0.6f)
                        {
                            continue;
                        }
                        yield return CraftData.InstantiateFromPrefabAsync(item.Type, result, false);
                        GameObject go = result.Get();
                        Vector3 loc = place + 1.2f * UnityEngine.Random.onUnitSphere;
                        Vector3 rot = 360 * UnityEngine.Random.onUnitSphere;
                        go.transform.position = loc;
                        go.transform.eulerAngles = rot;
                        var rb = go.EnsureComponent<Rigidbody>();
                        rb.isKinematic = false;
                    }
                }
                while (root != null)
                {
                    Destroy(root);
                    yield return null;
                }
            }
            UWE.CoroutineHost.StartCoroutine(DropLoot(transform.position, gameObject));
        }
        /// <summary>
        /// Executed has started being piloted by a player and <see cref="VehicleConfiguration.PilotingStyle" /> is set to <see cref="PilotingStyle.Other" />.
        /// </summary>
        /// <param name="isPiloting">True if the player is actually piloting</param>
        public virtual void HandleOtherPilotingAnimations(bool isPiloting) { }

        /// <summary>
        /// Checks if the player is currently piloting this vehicle.
        /// </summary>
        public virtual bool IsPlayerControlling()
        {
            if (this is VehicleTypes.Submarine sub)
            {
                return sub.IsPlayerPiloting();
            }
            else if (this is ModVehicle sub2)
            {
                return sub2.IsUnderCommand;
            }
            else // this is just a ModVehicle
            {
                return false;
            }
        }
        /// <summary>
        /// Executed when loading has finished
        /// </summary>
        public virtual void OnFinishedLoading()
        {

        }
        /// <summary>
        /// Updates the base color of the vehicle.
        /// </summary>
        public virtual void SetBaseColor(VehicleColor color)
        {
            baseColor = color;
        }

        /// <summary>
        /// Updates the interior color of the vehicle.
        /// </summary>
        public virtual void SetInteriorColor(VehicleColor color)
        {
            interiorColor = color;
        }
        /// <summary>
        /// Updates the stripe color of the vehicle.
        /// </summary>
        public virtual void SetStripeColor(VehicleColor color)
        {
            stripeColor = color;
        }
        /// <summary>
        /// Updates the name color of the vehicle.
        /// </summary>
        public virtual void SetNameColor(VehicleColor color)
        {
            nameColor = color;
        }

        /// <summary>
        /// The current base color.
        /// </summary>
        public VehicleColor BaseColor => baseColor;
        /// <summary>
        /// The current interior color.
        /// </summary>
        public VehicleColor InteriorColor => interiorColor;
        /// <summary>
        /// The current stripe color.
        /// </summary>
        public VehicleColor StripeColor => stripeColor;
        /// <summary>
        /// The current name color.
        /// </summary>
        public VehicleColor NameColor => nameColor;

        /// <summary>
        /// Checks if the vehicle is currently under the command of the player.
        /// </summary>
        public bool IsUnderCommand
        {// true when inside a vehicle (or piloting a drone)
            get
            {
                return _IsUnderCommand;
            }
            protected set
            {
                _IsUnderCommand = value;
                //IsPlayerDry = value;
            }
        }

        /// <summary>
        /// Sound to play when the vehicle lights are turned on.
        /// Set during prefabrication.
        /// </summary>
        /// <remarks> Prefabrication fields must remain open fields or
        /// Unity instantiation will not preserve them.
        /// Since the vehicle has multiple custom emitters, we cannot
        /// fetch it during Awake()</remarks>
        internal FMOD_CustomEmitter? lightsOnSound;

        public FMOD_CustomEmitter LightsOffSound
            => lightsOffSound.OrThrow(
                () => new InvalidOperationException(
                    $"Trying to access LightsOffSound but the prefabrication did not assign this field"));

        /// <summary>
        /// Sound to play when the vehicle lights are turned off.
        /// Set during prefabrication.
        /// </summary>
        /// <remarks> Prefabrication fields must remain open fields or
        /// Unity instantiation will not preserve them.
        /// Since the vehicle has multiple custom emitters, we cannot
        /// fetch it during Awake()</remarks>
        internal FMOD_CustomEmitter? lightsOffSound;

        public FMOD_CustomEmitter LightsOnSound
            => lightsOnSound.OrThrow(
                () => new InvalidOperationException(
                    $"Trying to access LightsOnSound but the prefabrication did not assign this field"));

        /// <summary>
        /// Populated during prefabrication/Awake().
        /// </summary>
        internal List<GameObject> VolumetricLights { get; } = new List<GameObject>();

        private PingInstance? hudPingInstance;
        /// <summary>
        /// Marker on the HUD.
        /// Can be used to enable or disable the marker.
        /// </summary>
        public PingInstance HudPingInstance => hudPingInstance.OrThrow(
            () => new InvalidOperationException(
            $"Trying toa ccess HugPingInstance before Awake() was called"));

        /// <summary>
        /// The headlights controller for this vehicle.
        /// Set during Awake().
        /// </summary>
        public HeadLightsController? HeadlightsController { get; private set; }  //set during awake()

        /// <summary>
        /// Energy interface used by the AI.
        /// At present, this is only used by the <see cref="Autopilot" /> to refill oxygen.
        /// </summary>
        /// <remarks> Prefabrication fields must remain open fields or
        /// Unity instantiation will not preserve them. We cannot fetch it during awake because
        /// the vehicle may have multiple energy interfaces.</remarks>
        public EnergyInterface? aiEnergyInterface;

        //private VoiceQueue voice;
        //private bool hasStarted = false;

        // if the player toggles the power off,
        // the vehicle is called "powered off,"
        // because it is unusable yet the batteries are not empty
        public bool IsPoweredOn { get; private set; } = true;


        /// <summary>
        /// Ambient sound emitter, copied from the seamoth prefab.
        /// </summary>
        /// <remarks>
        /// Copied during prefab setup.
        /// </remarks>
        internal FMOD_StudioEventEmitter? ambienceSound;

        /// <summary>
        /// The number of installed power efficiency modules.
        /// Automatically updated when a power efficiency module is added or removed.
        /// </summary>
        public int NumEfficiencyModules { get; private set; } = 0;


        private PowerManager? powerManager;
        /// <summary>
        /// The vehicle's power manager.
        /// </summary>
        public PowerManager PowerManager => powerManager.OrThrow(
            () => new InvalidOperationException(
                $"Trying to access PowerManager before Awake() was called"));

        //public bool IsPlayerDry { get; private set; } = false;
        /// <summary>
        /// True if the vehicle is scuttled (destroyed and ready to be salvaged).
        /// </summary>
        public bool isScuttled { get; private set; } = false;
        /// <summary>
        /// Gets a value indicating whether the undocking animation is currently in progress.
        /// </summary>
        public bool IsUndockingAnimating { get; internal set; } = false;



        /// <summary>
        /// Actions to execute when an upgrade module is added or removed.
        /// The first argument is the slot ID,
        /// then the tech type of the module,
        /// finally a boolean indicating if the module is being added (true) or removed (false).
        /// </summary>
        internal List<Action<int, TechType, bool>> UpgradeOnAddedActions { get; }
            = new List<Action<int, TechType, bool>>();



        /// <summary>
        /// Fetches the tech type of the vehicle by searching for its <see cref="TechTag" /> component.
        /// </summary>
        public TechType TechType => GetComponent<TechTag>().type;

        /// <summary>
        /// True if the vehicle is constructed and ready to be piloted.
        /// </summary>
        public bool IsConstructed => vfxConstructing == null || vfxConstructing.IsConstructed();

        /// <summary>
        /// True if the vehicle is currently docked in a docking bay (e.g. a moonpool).
        /// </summary>
        protected bool IsVehicleDocked { get; private set; } = false;

        #region internal_fields
        private bool _IsUnderCommand = false;
        private int numArmorModules = 0;
        private string[]? _slotIDs = null;
        private VehicleColor baseColor = VehicleColor.Default;
        private VehicleColor interiorColor = VehicleColor.Default;
        private VehicleColor stripeColor = VehicleColor.Default;
        private VehicleColor nameColor = VehicleColor.Default;
        #endregion



        /// <summary>
        /// Constructs the vehicle's ping instance as part of the prefab setup.
        /// </summary>
        /// <param name="pingType"></param>
        internal void PrefabSetupHudPing(PingType pingType)
        {
            Logger.Log($"Setting up HudPingInstance for ModVehicle #{Id}");
            hudPingInstance = gameObject.EnsureComponent<PingInstance>();
            hudPingInstance.origin = transform;
            hudPingInstance.pingType = pingType;
            hudPingInstance.SetLabel("Vehicle");
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
            foreach (VehicleParts.VehicleBattery vb in Com.BackupBatteries)
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
                vb.BatterySlot.EnsureComponent<SaveLoad.VFBatteryIdentifier>();
            }
            // Configure energy interface
            aiEnergyInterface = Com.BackupBatteries.First().BatterySlot.EnsureComponent<EnergyInterface>();
            aiEnergyInterface.sources = energyMixins.ToArray();
        }

        internal void SetupAmbienceSound(FMOD_StudioEventEmitter reference)
        {
            ambienceSound = reference.CopyComponentWithFieldsTo(gameObject);
        }

        protected virtual bool PlayerCanExitHelmControl(float roll, float pitch, float velocity) => true;

        #region internal_methods
        internal List<string> VehicleModuleSlots => GenerateModuleSlots(Config.NumModules).ToList();
        internal Dictionary<EquipmentType, List<string>> VehicleTypeToSlots => new Dictionary<EquipmentType, List<string>>
                {
                    { VehicleBuilder.ModuleType, VehicleModuleSlots }
                };



        private void StorageModuleAction(int slotID, TechType techType, bool added)
        {
            if (techType == TechType.VehicleStorageModule)
            {
                SetStorageModule(slotID, added);
            }
        }
        private void ArmorPlatingModuleAction(int slotID, TechType techType, bool added)
        {
            if (techType == TechType.VehicleArmorPlating)
            {
                _ = added ? numArmorModules++ : numArmorModules--;
                GetComponent<DealDamageOnImpact>().mirroredSelfDamageFraction = 0.5f * Mathf.Pow(0.5f, (float)numArmorModules);
            }
        }
        private void PowerUpgradeModuleAction(int slotID, TechType techType, bool added)
        {
            if (techType == TechType.VehiclePowerUpgradeModule)
            {
                _ = added ? NumEfficiencyModules++ : NumEfficiencyModules--;
            }
        }
        private bool IsAllowedToRemove(Pickupable pickupable, bool verbose)
        {
            if (pickupable.GetTechType() == TechType.VehicleStorageModule)
            {
                // check the appropriate storage module for emptiness
                SeamothStorageContainer component = pickupable.GetComponent<SeamothStorageContainer>();
                if (component != null)
                {
                    bool flag = component.container.count == 0;
                    if (verbose && !flag)
                    {
                        ErrorMessage.AddDebug(Language.main.Get("SeamothStorageNotEmpty"));
                    }
                    return flag;
                }
                Debug.LogError("No VehicleStorageContainer found on VehicleStorageModule item");
            }
            return true;
        }
        private void HandleExtraQuickSlotInputs()
        {
            if (IsPlayerControlling())
            {
                if (Input.GetKeyDown(KeyCode.Alpha6))
                {
                    SlotKeyDown(5);
                }
                if (Input.GetKeyDown(KeyCode.Alpha7))
                {
                    SlotKeyDown(6);
                }
                if (Input.GetKeyDown(KeyCode.Alpha8))
                {
                    SlotKeyDown(7);
                }
                if (Input.GetKeyDown(KeyCode.Alpha9))
                {
                    SlotKeyDown(8);
                }
                if (Input.GetKeyDown(KeyCode.Alpha0))
                {
                    SlotKeyDown(9);
                }
            }
        }
        private void SetStorageModule(int slotID, bool activated)
        {
            foreach (var sto in Com.InnateStorages)
            {
                sto.Container.SetActive(true);
            }
            if (Com.ModularStorages.Count <= slotID)
            {
                ErrorMessage.AddWarning("There is no storage expansion for slot ID: " + slotID.ToString());
                return;
            }
            var modSto = Com.ModularStorages[slotID];
            modSto.Container.SetActive(activated);
            if (activated)
            {
                var modularContainer = GetSeamothStorageContainer(slotID);
                if (modularContainer == null)
                {
                    Logger.Warn("Warning: failed to get modular storage container for slotID: " + slotID.ToString());
                    return;
                }
                modularContainer.height = modSto.Height;
                modularContainer.width = modSto.Width;
                ModGetStorageInSlot(slotID, TechType.VehicleStorageModule)?.Resize(modSto.Width, modSto.Height);
            }
        }
        internal SeamothStorageContainer? GetSeamothStorageContainer(int slotID)
        {
            var slotItem = this.GetSlotItem(slotID);
            if (slotItem == null)
            {
                Logger.Warn("Warning: failed to get item for that slotID: " + slotID.ToString());
                return null;
            }
            Pickupable item = slotItem.item;
            if (item.GetTechType() != TechType.VehicleStorageModule)
            {
                Logger.Warn("Warning: failed to get pickupable for that slotID: " + slotID.ToString());
                return null;
            }
            SeamothStorageContainer component = item.GetComponent<SeamothStorageContainer>();
            return component;
        }
        internal ItemsContainer? ModGetStorageInSlot(int slotID, TechType techType)
        {
            switch (techType)
            {
                case VehicleBuilder.InnateStorage:
                    {
                        InnateStorageContainer vsc;
                        if (0 <= slotID && slotID < Com.InnateStorages.Count)
                        {
                            vsc = Com.InnateStorages[slotID].Container.GetComponent<InnateStorageContainer>();
                        }
                        else
                        {
                            Logger.Error("Error: ModGetStorageInSlot called on invalid innate storage slotID");
                            return null;
                        }
                        return vsc.Container;
                    }
                case TechType.VehicleStorageModule:
                    {
                        var component = GetSeamothStorageContainer(slotID);
                        if (component == null)
                        {
                            Logger.Warn("Warning: failed to get storage-container for that slotID: " + slotID.ToString());
                            return null;
                        }
                        return component.container;
                    }
                default:
                    {
                        Logger.Error("Error: tried to get storage for unsupported TechType");
                        return null;
                    }
            }
        }

        //private void SetDockedLighting(bool docked)
        //{
        //    foreach (var renderer in GetComponentsInChildren<Renderer>())
        //    {
        //        foreach (Material mat in renderer.materials)
        //        {
        //            if (renderer.gameObject.name.ToLower().Contains("light"))
        //            {
        //                continue;
        //            }
        //            if (Com.CanopyWindows != null && Com.CanopyWindows.Contains(renderer.gameObject))
        //            {
        //                continue;
        //            }
        //            mat.EnableKeyword(Shaders.EmissionKeyword);
        //            mat.SetFloat(Shaders.EmissionNightField, docked ? 0.4f : 0f);
        //            mat.SetFloat(Shaders.EmissionField, 0);
        //            mat.SetFloat(Shaders.GlowField, 0);
        //            mat.SetFloat(Shaders.GlowNightField, 0);
        //            mat.SetFloat(Shaders.SpecIntField, 0f);
        //            if (docked)
        //            {
        //                mat.EnableKeyword(Admin.Utils.specmapKeyword);
        //            }
        //            else
        //            {
        //                mat.DisableKeyword(Admin.Utils.specmapKeyword);
        //            }
        //        }
        //    }
        //}
        internal void TogglePower()
        {
            IsPoweredOn = !IsPoweredOn;
        }
        private void ManagePhysics()
        {
            if (worldForces.IsAboveWater() != wasAboveWater)
            {
                PlaySplashSound();
                wasAboveWater = worldForces.IsAboveWater();
            }
            if (stabilizeRoll)
            {
                StabilizeRoll();
            }
            prevVelocity = useRigidbody.velocity;
            bool shouldSetKinematic = teleporting || (!constructionFallOverride && !GetPilotingMode() && (!Admin.GameStateWatcher.IsWorldSettled || docked || !vfxConstructing.IsConstructed()));
            UWE.Utils.SetIsKinematicAndUpdateInterpolation(useRigidbody, shouldSetKinematic, true);
        }
        /// <summary>
        /// Executed when the player has started piloting a vehicle. The exact animations depend on <see cref="VehicleConfiguration.PilotingStyle" />.
        /// </summary>
        internal void HandlePilotingAnimations()
        {
            switch (Config.PilotingStyle)
            {
                case PilotingStyle.Cyclops:
                    SafeAnimator.SetBool(Player.main.armsController.animator, "cyclops_steering", IsPlayerControlling());
                    break;
                case PilotingStyle.Seamoth:
                    SafeAnimator.SetBool(Player.main.armsController.animator, "in_seamoth", IsPlayerControlling());
                    break;
                case PilotingStyle.Prawn:
                    SafeAnimator.SetBool(Player.main.armsController.animator, "in_exosuit", IsPlayerControlling());
                    break;
                default:
                    HandleOtherPilotingAnimations(IsPlayerControlling());
                    break;
            }
        }
        private void MyExitLockedMode()
        {
            GameInput.ClearInput();
            Player.main.transform.parent = null;
            Player.main.transform.localScale = Vector3.one;
            Player.main.currentMountedVehicle = null;
            Player.main.playerController.SetEnabled(true);
            Player.main.mode = Player.Mode.Normal;
            Player.main.playerModeChanged.Trigger(Player.main.mode);
            Player.main.sitting = false;
            Player.main.playerController.ForceControllerSize();
        }
        private void DoExitRoutines()
        {
            Player myPlayer = Player.main;
            Player.Mode myMode = myPlayer.mode;
            void DoExitActions(ref Player.Mode mode)
            {
                GameInput.ClearInput();
                myPlayer.playerController.SetEnabled(true);
                mode = Player.Mode.Normal;
                myPlayer.playerModeChanged.Trigger(mode);
                myPlayer.sitting = false;
                myPlayer.playerController.ForceControllerSize();
                myPlayer.transform.parent = null;
            }
            switch (this)
            {
                case Submersible mvSubmersible:
                    // exit locked mode
                    DoExitActions(ref myMode);
                    myPlayer.mode = myMode;
                    mvSubmersible.StopPiloting();
                    break;
                case Skimmer mvSkimmer:
                    DoExitActions(ref myMode);
                    myPlayer.mode = myMode;
                    mvSkimmer.StopPiloting();
                    break;
                case Submarine mvSubmarine:
                    // check if we're level by comparing pitch and roll
                    float roll = mvSubmarine.transform.rotation.eulerAngles.z;
                    float rollDelta = roll >= 180 ? 360 - roll : roll;
                    float pitch = mvSubmarine.transform.rotation.eulerAngles.x;
                    float pitchDelta = pitch >= 180 ? 360 - pitch : pitch;
                    if (!PlayerCanExitHelmControl(rollDelta, pitchDelta, mvSubmarine.useRigidbody.velocity.magnitude))
                    {
                        Logger.PDANote($"{Language.main.Get("AvsExitNotAllowed")} ({GameInput.Button.Exit})");
                        return;
                    }


                    mvSubmarine.Com.Engine.KillMomentum();
                    if (mvSubmarine.Com.PilotSeats.Count == 0)
                    {
                        Logger.Error("Error: tried to exit a submarine without pilot seats");
                        return;
                    }


                    DoExitActions(ref myMode);
                    myPlayer.mode = myMode;
                    mvSubmarine.StopPiloting();

                    var seat = mvSubmarine.Com.PilotSeats[0];
                    var exitLocation = seat.ExitLocation;
                    Vector3 exit;
                    if (exitLocation != null)
                    {
                        Logger.DebugLog($"Exit location defined. Deriving from seat status {seat.Seat.transform.localPosition} / {seat.Seat.transform.localRotation}");
                        exit = exitLocation.position;
                    }
                    else
                    {
                        Logger.DebugLog($"Exit location not declared in seat definition. Calculating location");
                        // if the exit location is not set, use the calculated exit location
                        exit = seat.CalculatedExitLocation;
                    }
                    Logger.DebugLog($"Exiting submarine at {exit} (local {transform.InverseTransformPoint(exit)})");
                    Player.main.transform.position = exit;

                    break;
                default:
                    MyExitLockedMode();
                    break;
            }
        }
        #endregion

        #region public_methods
        public void NotifyStatus(PlayerStatus vs)
        {
            foreach (var component in GetComponentsInChildren<IPlayerListener>())
            {
                switch (vs)
                {
                    case PlayerStatus.OnPlayerEntry:
                        component.OnPlayerEntry();
                        break;
                    case PlayerStatus.OnPlayerExit:
                        component.OnPlayerExit();
                        break;
                    case PlayerStatus.OnPilotBegin:
                        component.OnPilotBegin();
                        break;
                    case PlayerStatus.OnPilotEnd:
                        component.OnPilotEnd();
                        break;
                    default:
                        Logger.Error("Error: tried to notify using an invalid status");
                        break;
                }
            }
        }
        public void GetHUDValues(out float health, out float power)
        {
            health = this.liveMixin.GetHealthFraction();
            base.GetEnergyValues(out float num, out float num2);
            power = ((num > 0f && num2 > 0f) ? (num / num2) : 0f);
        }
        public bool HasRoomFor(Pickupable pickup)
        {
            foreach (var container in Com.InnateStorages.Select(x => x.Container.GetComponent<InnateStorageContainer>().Container))
            {
                if (container != null && container.HasRoomFor(pickup))
                {
                    return true;
                }
            }
            foreach (var container in ModularStorageInput.GetAllModularStorageContainers(this))
            {
                if (container.HasRoomFor(pickup))
                {
                    return true;
                }
            }
            return false;
        }
        public bool HasInStorage(TechType techType, int count = 1)
        {
            foreach (var container in Com.InnateStorages.Select(x => x.Container.GetComponent<InnateStorageContainer>().Container))
            {
                if (container != null && container.Contains(techType))
                {
                    if (container.GetCount(techType) >= count)
                    {
                        return true;
                    }
                }
            }
            foreach (var container in ModularStorageInput.GetAllModularStorageContainers(this))
            {
                if (container.Contains(techType))
                {
                    if (container.GetCount(techType) >= count)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public bool AddToStorage(Pickupable pickup)
        {
            if (!HasRoomFor(pickup))
            {
                if (Player.main.GetVehicle() == this)
                {
                    ErrorMessage.AddMessage(Language.main.Get("ContainerCantFit"));
                }
                return false;
            }
            foreach (var container in Com.InnateStorages.Select(x => x.Container.GetComponent<InnateStorageContainer>().Container))
            {
                if (container != null && container.HasRoomFor(pickup))
                {
                    string arg = Language.main.Get(pickup.GetTechName());
                    ErrorMessage.AddMessage(Language.main.GetFormat<string>("VehicleAddedToStorage", arg));
                    uGUI_IconNotifier.main.Play(pickup.GetTechType(), uGUI_IconNotifier.AnimationType.From, null);
                    pickup.Initialize();
                    InventoryItem item = new InventoryItem(pickup);
                    container.UnsafeAdd(item);
                    pickup.PlayPickupSound();
                    return true;
                }
            }
            foreach (var container in ModularStorageInput.GetAllModularStorageContainers(this))
            {
                if (container.HasRoomFor(pickup))
                {
                    string arg = Language.main.Get(pickup.GetTechName());
                    ErrorMessage.AddMessage(Language.main.GetFormat<string>("VehicleAddedToStorage", arg));
                    uGUI_IconNotifier.main.Play(pickup.GetTechType(), uGUI_IconNotifier.AnimationType.From, null);
                    pickup.Initialize();
                    InventoryItem item = new InventoryItem(pickup);
                    container.UnsafeAdd(item);
                    pickup.PlayPickupSound();
                    return true;
                }
            }
            return false;
        }
        public void GetStorageValues(out int stored, out int capacity)
        {
            int retStored = 0;
            int retCapacity = 0;

            int GetModularCapacity()
            {
                int ret = 0;
                var marty = ModularStorageInput.GetAllModularStorageContainers(this);
                marty.ForEach(x => ret += x.sizeX * x.sizeY);
                return ret;
            }
            int GetModularStored()
            {
                int ret = 0;
                var marty = ModularStorageInput.GetAllModularStorageContainers(this);
                marty.ForEach(x => x.ForEach(y => ret += y.width * y.height));
                return ret;
            }
            int GetInnateCapacity(VehicleParts.VehicleStorage sto)
            {
                var container = sto.Container.GetComponent<InnateStorageContainer>();
                return container.Container.sizeX * container.Container.sizeY;
            }
            int GetInnateStored(VehicleParts.VehicleStorage sto)
            {
                int ret = 0;
                var marty = (IEnumerable<InventoryItem>)sto.Container.GetComponent<InnateStorageContainer>().Container;
                marty.ForEach(x => ret += x.width * x.height);
                return ret;
            }

            Com.InnateStorages.ForEach(x => retCapacity += GetInnateCapacity(x));
            Com.InnateStorages.ForEach(x => retStored += GetInnateStored(x));

            if (Com.ModularStorages != null)
            {
                retCapacity += GetModularCapacity();
                retStored += GetModularStored();
            }
            stored = retStored;
            capacity = retCapacity;
        }
        public void SetName(string name)
        {
            vehicleName = name;
            subName.SetName(name);
        }
        /// <summary>
        /// Gets the applied local vehicle name
        /// </summary>
        public string VehicleName => subName != null ? subName.GetName() : vehicleName;


        #endregion

        #region static_methods
        private static string[] GenerateModuleSlots(int modules)
        {
            string[] retIDs;
            retIDs = new string[modules];
            for (int i = 0; i < modules; i++)
            {
                retIDs[i] = ModuleBuilder.ModuleName(i);
            }
            return retIDs;
        }
        private static string[] GenerateSlotIDs(int modules)
        {
            string[] retIDs;
            int numUpgradesTotal = modules;
            retIDs = new string[numUpgradesTotal];
            for (int i = 0; i < modules; i++)
            {
                retIDs[i] = ModuleBuilder.ModuleName(i);
            }
            return retIDs;
        }
        internal static void MaybeControlRotation(Vehicle veh)
        {
            if (veh is ModVehicle mv)
            {
                if (!mv.GetPilotingMode()
                    || !mv.IsUnderCommand
                    || !mv.Com.Engine.enabled
                    || Player.main.GetPDA().isOpen
                    || (AvatarInputHandler.main && !AvatarInputHandler.main.IsEnabled())
                    || !mv.energyInterface.hasCharge)
                {
                    return;
                }
                mv.Com.Engine.ControlRotation();
            }
        }
        public static EnergyMixin GetEnergyMixinFromVehicle(Vehicle veh)
        {
            if (!(veh is ModVehicle mod))
            {
                return veh.GetComponent<EnergyMixin>();
            }
            else
            {
                return mod.energyInterface.sources.First();
            }
        }
        public static void TeleportPlayer(Vector3 destination)
        {
            var mv = Player.main.GetModVehicle();
            UWE.Utils.EnterPhysicsSyncSection();
            Player.main.SetCurrentSub(null, true);
            Player.main.playerController.SetEnabled(false);
            IEnumerator waitForTeleport()
            {
                yield return null;
                Player.main.SetPosition(destination);
                Player.main.SetCurrentSub(mv.SafeGetComponent<SubRoot>(), true);
                Player.main.playerController.SetEnabled(true);
                yield return null;
                UWE.Utils.ExitPhysicsSyncSection();
            }
            UWE.CoroutineHost.StartCoroutine(waitForTeleport());
        }
        #endregion

        #region saveload
        private const string isControlling = "isControlling";
        private const string isInside = "isInside";
        private const string mySubName = "SubName";
        private const string baseColorName = "BaseColor";
        private const string interiorColorName = "InteriorColor";
        private const string stripeColorName = "StripeColor";
        private const string nameColorName = "NameColor";
        private const string defaultColorName = "DefaultColor";
        private const string SimpleDataSaveFileName = "SimpleData";
        private void SaveSimpleData()
        {
            Dictionary<string, string> simpleData = new Dictionary<string, string>
            {
                { isControlling, IsPlayerControlling() ? bool.TrueString : bool.FalseString },
                { isInside, IsUnderCommand ? bool.TrueString : bool.FalseString },
                { mySubName, subName.hullName.text },
                { baseColorName, $"#{ColorUtility.ToHtmlStringRGB(baseColor.RGB)}" },
                { interiorColorName, $"#{ColorUtility.ToHtmlStringRGB(interiorColor.RGB)}" },
                { stripeColorName, $"#{ColorUtility.ToHtmlStringRGB(stripeColor.RGB)}" },
                { nameColorName, $"#{ColorUtility.ToHtmlStringRGB(nameColor.RGB)}" },
                { defaultColorName, (this is Submarine sub) && sub.IsDefaultTexture ? bool.TrueString : bool.FalseString }
            };
            SaveLoad.JsonInterface.Write(this, SimpleDataSaveFileName, simpleData);
        }
        private IEnumerator LoadSimpleData()
        {
            // Need to handle some things specially here for Submarines
            // Because Submarines had color changing before I knew how to integrate with the Moonpool
            // The new color changing methods are much simpler, but Odyssey and Beluga use the old methods,
            // So I'll still support them.
            yield return new WaitUntil(() => Admin.GameStateWatcher.IsWorldLoaded);
            yield return new WaitUntil(() => isInitialized);
            var simpleData = SaveLoad.JsonInterface.Read<Dictionary<string, string>>(this, SimpleDataSaveFileName);
            if (simpleData == null || simpleData.Count == 0)
            {
                yield break;
            }
            if (bool.Parse(simpleData[isInside]))
            {
                PlayerEntry();
            }
            if (bool.Parse(simpleData[isControlling]))
            {
                BeginPiloting();
            }
            SetName(simpleData[mySubName]);
            var sub = this as Submarine;
            if (sub != null)
                sub.PaintVehicleDefaultStyle(simpleData[mySubName]);
            if (Boolean.Parse(simpleData[defaultColorName]))
            {
                yield break;
            }
            if (ColorUtility.TryParseHtmlString(simpleData[baseColorName], out var rgb))
            {
                baseColor = new VehicleColor(rgb);
                subName.SetColor(0, Vector3.zero, baseColor.RGB);
                if (sub != null)
                    sub.PaintVehicleName(simpleData[mySubName], Color.black, baseColor.RGB);
            }
            if (ColorUtility.TryParseHtmlString(simpleData[nameColorName], out rgb))
            {
                nameColor = new VehicleColor(rgb);
                subName.SetColor(1, Vector3.zero, nameColor.RGB);
                if (sub != null)
                    sub.PaintVehicleName(simpleData[mySubName], nameColor.RGB, baseColor.RGB);
            }
            if (ColorUtility.TryParseHtmlString(simpleData[interiorColorName], out rgb))
            {
                interiorColor = new VehicleColor(rgb);
                subName.SetColor(2, Vector3.zero, interiorColor.RGB);
            }
            if (ColorUtility.TryParseHtmlString(simpleData[stripeColorName], out rgb))
            {
                stripeColor = new VehicleColor(rgb);
                subName.SetColor(3, Vector3.zero, stripeColor.RGB);
            }
        }
        void IProtoTreeEventListener.OnProtoSerializeObjectTree(ProtobufSerializer serializer)
        {
            try
            {
                SaveSimpleData();
                SaveLoad.VFModularStorageSaveLoad.SerializeAllModularStorage(this);
            }
            catch (Exception e)
            {
                Logger.LogException($"Failed to save simple data for ModVehicle {name}", e);
            }
            OnGameSaved();
        }
        void IProtoTreeEventListener.OnProtoDeserializeObjectTree(ProtobufSerializer serializer)
        {
            UWE.CoroutineHost.StartCoroutine(LoadSimpleData());
            UWE.CoroutineHost.StartCoroutine(SaveLoad.VFModularStorageSaveLoad.DeserializeAllModularStorage(this));
            OnGameLoaded();
        }
        protected virtual void OnGameSaved() { }
        protected virtual void OnGameLoaded() { }

        private const string StorageSaveName = "Storage";
        private Dictionary<string, List<Tuple<TechType, float, TechType>>>? loadedStorageData = null;
        private readonly Dictionary<string, List<Tuple<TechType, float, TechType>>> innateStorageSaveData = new Dictionary<string, List<Tuple<TechType, float, TechType>>>();
        internal void SaveInnateStorage(string path, List<Tuple<TechType, float, TechType>> storageData)
        {
            innateStorageSaveData.Add(path, storageData);
            if (innateStorageSaveData.Count() == Com.InnateStorages.Count())
            {
                // write it out
                SaveLoad.JsonInterface.Write(this, StorageSaveName, innateStorageSaveData);
                innateStorageSaveData.Clear();
            }
        }
        internal List<Tuple<TechType, float, TechType>>? ReadInnateStorage(string path)
        {
            if (loadedStorageData == null)
            {
                loadedStorageData = SaveLoad.JsonInterface.Read<Dictionary<string, List<Tuple<TechType, float, TechType>>>>(this, StorageSaveName);
            }
            if (loadedStorageData == null)
            {
                return default;
            }
            if (loadedStorageData.ContainsKey(path))
            {
                return loadedStorageData[path];
            }
            else
            {
                return default;
            }
        }

        private const string BatterySaveName = "Batteries";
        private Dictionary<string, Tuple<TechType, float>>? loadedBatteryData = null;
        private readonly Dictionary<string, Tuple<TechType, float>> batterySaveData = new Dictionary<string, Tuple<TechType, float>>();
        internal void SaveBatteryData(string path, Tuple<TechType, float> batteryData)
        {
            int batteryCount = 0;
            batteryCount += Com.Batteries.Count;
            batteryCount += Com.BackupBatteries.Count;

            batterySaveData.Add(path, batteryData);
            if (batterySaveData.Count() == batteryCount)
            {
                // write it out
                SaveLoad.JsonInterface.Write(this, BatterySaveName, batterySaveData);
                batterySaveData.Clear();
            }
        }
        internal Tuple<TechType, float>? ReadBatteryData(string path)
        {
            if (loadedBatteryData == null)
            {
                loadedBatteryData = SaveLoad.JsonInterface.Read<Dictionary<string, Tuple<TechType, float>>>(this, BatterySaveName);
            }
            if (loadedBatteryData == null)
            {
                return default;
            }
            if (loadedBatteryData.ContainsKey(path))
            {
                return loadedBatteryData[path];
            }
            else
            {
                return default;
            }
        }

        #endregion

        /// <summary>
        /// Overridable import method called when an imported recipe is loaded.
        /// </summary>
        /// <param name="recipe">Recipe restored from file</param>
        /// <returns>Recipe to use</returns>
        public virtual Recipe OnRecipeOverride(Recipe recipe)
        {
            return recipe;
        }


        private GameObject GetOrCreateChild(string childName)
        {
            var child = transform.Find(childName).SafeGetGameObject();
            if (child == null)
            {
                child = new GameObject(childName);
                child.transform.SetParent(transform);
            }
            return child;
        }

        public GameObject GetOrCreateDefaultStorageRootObject()
            => GetOrCreateChild("StorageRootObject");
        public GameObject GetOrCreateDefaultModulesRootObject()
            => GetOrCreateChild("ModulesRootObject");
    }
}
