using AVS.Composition;
using AVS.Configuration;
using AVS.Engines;
using AVS.MaterialAdapt;
using AVS.Util;
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

        public virtual ModVehicleEngine Engine { get; set; }

        //public virtual GameObject BoundingBox => null; // Prefer to use BoundingBoxCollider directly (don't use this)

        /// <summary>
        /// Invariant vehicle configuration. Initialized during construction.
        /// Never null.
        /// </summary>
        public VehicleConfiguration Config { get; }

        /// <summary>
        /// True to log high-verbosity debug messages (as non-debug)
        /// </summary>
        public virtual bool LogDebug => false;

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
        private VehicleComposition _composition = null;

        /// <summary>
        /// Constructs the vehicle with the given configuration.
        /// </summary>
        /// <param name="config">Vehicle configuration. Must not be null</param>
        /// <exception cref="ArgumentNullException"></exception>
        protected ModVehicle(VehicleConfiguration config)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config), "VehicleConfiguration cannot be null");
            MaterialFixer = new MaterialFixer(this, config.MaterialFixLogging, () => this.ResolveMaterial(config.MaterialFixLogging));

        }

        /// <summary>
        /// If this method returns true,
        /// all materials of the given game object will be excluded
        /// from material fixing.
        /// </summary>
        /// <remarks>Child objects will still be processed</remarks>
        /// <param name="go">Game object to test</param>
        /// <returns>True if this object should not be fixed</returns>
        protected virtual bool ExcludeFromMaterialFixing(GameObject go)
            => go.GetComponent<Skybox>()
            || go.name.ToLower().Contains("light")
            || Com.CanopyWindows.Contains(go);

        /// <summary>
        /// If this method returns true,
        /// the specific material of the given renderer will be excluded
        /// from material fixing.
        /// </summary>
        /// <param name="renderer">Owning renderer</param>
        /// <param name="materialIndex">Index of the material being processed with 0 being the first material</param>
        /// <param name="material">Material being processed</param>
        /// <returns>True if this material should not be fixed</returns>
        protected virtual bool ExcludeFromMaterialFixing(Renderer renderer, int materialIndex, Material material)
            => false;


        private IEnumerable<SurfaceShaderData> ResolveMaterial(Logging logConfig)
        {
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (ExcludeFromMaterialFixing(renderer.gameObject))
                {
                    // skip this renderer
                    continue;
                }

                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    if (ExcludeFromMaterialFixing(renderer, i, renderer.materials[i]))
                    {
                        continue;
                    }

                    var material = SurfaceShaderData.From(renderer, i, logConfig, Config.IgnoreShaderNameWhenFixingMaterial);
                    if (material != null)
                        yield return material;
                }
            }
        }

        internal void RequireComposition()
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

        ///<inheritdoc />
        public override void Awake()
        {
            RequireComposition();

            energyInterface = GetComponent<EnergyInterface>();
            base.Awake();
            VehicleManager.EnrollVehicle(this); // Register our new vehicle with Vehicle Framework
            upgradeOnAddedActions.Add(StorageModuleAction);
            upgradeOnAddedActions.Add(ArmorPlatingModuleAction);
            upgradeOnAddedActions.Add(PowerUpgradeModuleAction);

            VehicleBuilder.SetupVolumetricLights(this);
            headlights = gameObject.AddComponent<HeadLightsController>();
            gameObject.AddComponent<VolumetricLightController>();

            gameObject.EnsureComponent<AutoPilot>();

            if (!Engine)
            {
                Engine = GetComponent<ModVehicleEngine>();
            }
            base.LazyInitialize();
            Com.Upgrades.ForEach(x => x.Interface.GetComponent<VehicleUpgradeConsoleInput>().equipment = modules);
            var warpChipThing = GetComponent("TelePingVehicleInstance");
            if (warpChipThing != null)
            {
                Component.DestroyImmediate(warpChipThing);
            }
            vfxConstructing = GetComponent<VFXConstructing>();
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
            powerMan = gameObject.EnsureComponent<PowerManager>();
            isInited = true;
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
        public override void OnUpgradeModuleUse(TechType techType, int slotID)
        {
            UpgradeTypes.SelectableActionParams param = new UpgradeTypes.SelectableActionParams
            {
                vehicle = this,
                slotID = slotID,
                techType = techType
            };
            Admin.UpgradeRegistrar.OnSelectActions.ForEach(x => x(param));

            UpgradeTypes.SelectableChargeableActionParams param2 = new UpgradeTypes.SelectableChargeableActionParams
            {
                vehicle = this,
                slotID = slotID,
                techType = techType,
                charge = param.vehicle.quickSlotCharge[param.slotID],
                slotCharge = param.vehicle.GetSlotCharge(param.slotID)
            };
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
            upgradeOnAddedActions.ForEach(x => x(slotID, techType, added));
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
        public override InventoryItem GetSlotItem(int slotID)
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
        public override void SubConstructionComplete()
        {
            Logger.DebugLog(this, "ModVehicle SubConstructionComplete");
            pingInstance.enabled = true;
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
                        window?.SetActive(false);
                    }
                }
                catch (Exception)
                {
                    //It's okay if the vehicle doesn't have a canopy
                }
                Player.main.lastValidSub = GetComponent<SubRoot>();
                Player.main.SetCurrentSub(GetComponent<SubRoot>(), true);
                NotifyStatus(PlayerStatus.OnPlayerEntry);
                pingInstance.enabled = false;
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
                        window?.SetActive(true);
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
            pingInstance.enabled = true;
        }
        public virtual void SubConstructionBeginning()
        {
            Logger.DebugLog(this, "ModVehicle SubConstructionBeginning");
            pingInstance.enabled = false;
            worldForces.handleGravity = false;
        }
        public virtual void OnAIBatteryReload()
        {
        }
        public virtual float OnStorageOpen(string name, bool open)
        {
            // this function returns the number of seconds to wait before opening the PDA,
            // to show off the cool animations~
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
            IEnumerator GiveUsABatteryOrGiveUsDeath()
            {
                yield return new WaitForSeconds(2.5f);

                // give us an AI battery please
                TaskResult<GameObject> result = new TaskResult<GameObject>();
                yield return CraftData.InstantiateFromPrefabAsync(TechType.PowerCell, result, false);
                GameObject newAIBattery = result.Get();
                newAIBattery.GetComponent<Battery>().charge = 200;
                newAIBattery.transform.SetParent(Com.StorageRootObject.transform);
                if (AIEnergyInterface)
                {
                    AIEnergyInterface.sources.First().battery = newAIBattery.GetComponent<Battery>();
                    AIEnergyInterface.sources.First().batterySlot.AddItem(newAIBattery.GetComponent<Pickupable>());
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
        /// <param name="exitLocation">If non-0, the player should relocate to this location</param>
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
            BoxCollider box = Com.BoundingBoxCollider;
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
            BoxCollider box = Com.BoundingBoxCollider;
            if (box)
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
            pingInstance.enabled = false;
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
            isPoweredOn = false;
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
            isPoweredOn = true;
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
            if (this as VehicleTypes.Submarine != null)
            {
                return (this as VehicleTypes.Submarine).IsPlayerPiloting();
            }
            else if (this as VehicleTypes.Submersible != null)
            {
                return (this as VehicleTypes.Submersible).IsUnderCommand;
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
        public virtual void SetBaseColor(Vector3 hsb, Color color)
        {
            baseColor = color;
        }
        /// <summary>
        /// Updates the interior color of the vehicle.
        /// </summary>
        public virtual void SetInteriorColor(Vector3 hsb, Color color)
        {
            interiorColor = color;
        }
        /// <summary>
        /// Updates the stripe color of the vehicle.
        /// </summary>
        public virtual void SetStripeColor(Vector3 hsb, Color color)
        {
            stripeColor = color;
        }

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
                IsPlayerDry = value;
            }
        }

        public FMOD_CustomEmitter lightsOnSound = null;
        public FMOD_CustomEmitter lightsOffSound = null;
        public List<GameObject> lights = new List<GameObject>();
        public List<GameObject> volumetricLights = new List<GameObject>();
        public PingInstance pingInstance = null;
        public HeadLightsController headlights;
        public EnergyInterface AIEnergyInterface;
        public AutoPilotVoice voice;
        public bool isInited = false;
        // if the player toggles the power off,
        // the vehicle is called "powered off,"
        // because it is unusable yet the batteries are not empty
        public bool isPoweredOn = true;
        public FMOD_StudioEventEmitter ambienceSound;
        public int numEfficiencyModules = 0;
        public PowerManager powerMan = null;
        public bool IsPlayerDry = false;
        public bool isScuttled = false;
        public bool IsUndockingAnimating = false;



        public List<Action<int, TechType, bool>> upgradeOnAddedActions = new List<Action<int, TechType, bool>>();



        /// <summary>
        /// Tech type of the vehicle.
        /// </summary>
        public TechType TechType => GetComponent<TechTag>().type;

        /// <summary>
        /// True if the vehicle is constructed and ready to be piloted.
        /// </summary>
        public bool IsConstructed => vfxConstructing == null || vfxConstructing.IsConstructed();


        #region internal_fields
        private bool _IsUnderCommand = false;
        private int numArmorModules = 0;
        protected bool IsVehicleDocked = false;
        private string[] _slotIDs = null;
        protected internal Color baseColor = Color.white;
        protected internal Color interiorColor = Color.white;
        protected internal Color stripeColor = Color.white;
        protected internal Color nameColor = Color.black;
        #endregion


        protected virtual bool PlayerCanExitHelmControl(float roll, float pitch, float velocity) => true;

        #region internal_methods
        internal List<string> VehicleModuleSlots => GenerateModuleSlots(Config.NumModules).ToList();
        internal Dictionary<EquipmentType, List<string>> VehicleTypeToSlots => new Dictionary<EquipmentType, List<string>>
                {
                    { VehicleBuilder.ModuleType, VehicleModuleSlots }
                };

        public float EngineSoundVolume => 1;
        public float AutopilotSoundVolume => 1;
        public bool ShowAutopilotSubtitles => false;

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
                _ = added ? numEfficiencyModules++ : numEfficiencyModules--;
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
                modularContainer.height = modSto.Height;
                modularContainer.width = modSto.Width;
                ModGetStorageInSlot(slotID, TechType.VehicleStorageModule).Resize(modSto.Width, modSto.Height);
            }
        }
        internal SeamothStorageContainer GetSeamothStorageContainer(int slotID)
        {
            InventoryItem slotItem = this.GetSlotItem(slotID);
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
        internal ItemsContainer ModGetStorageInSlot(int slotID, TechType techType)
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
                        return vsc.container;
                    }
                case TechType.VehicleStorageModule:
                    {
                        SeamothStorageContainer component = GetSeamothStorageContainer(slotID);
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
            isPoweredOn = !isPoweredOn;
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
            Submersible mvSubmersible = this as Submersible;
            Skimmer mvSkimmer = this as Skimmer;
            Submarine mvSubmarine = this as Submarine;
            if (mvSubmersible != null)
            {
                // exit locked mode
                DoExitActions(ref myMode);
                myPlayer.mode = myMode;
                mvSubmersible.StopPiloting();
                return;
            }
            else if (mvSkimmer != null)
            {
                DoExitActions(ref myMode);
                myPlayer.mode = myMode;
                mvSkimmer.StopPiloting();
                return;
            }
            else if (mvSubmarine != null)
            {
                // check if we're level by comparing pitch and roll
                float roll = mvSubmarine.transform.rotation.eulerAngles.z;
                float rollDelta = roll >= 180 ? 360 - roll : roll;
                float pitch = mvSubmarine.transform.rotation.eulerAngles.x;
                float pitchDelta = pitch >= 180 ? 360 - pitch : pitch;
                if (!PlayerCanExitHelmControl(rollDelta, pitchDelta, mvSubmarine.useRigidbody.velocity.magnitude))
                {
                    if (HUDBuilder.IsVR)
                    {
                        Logger.PDANote($"{Language.main.Get("VFExitNotAllowed")} ({GameInput.Button.Exit})");
                    }
                    else
                    {
                        Logger.PDANote($"{Language.main.Get("VFExitNotAllowed")} ({GameInput.Button.Exit})");
                    }
                    return;
                }


                mvSubmarine.Engine.KillMomentum();
                if (mvSubmarine.Com.PilotSeats.Count == 0)
                {
                    Logger.Error("Error: tried to exit a submarine without pilot seats");
                    return;
                }
                // teleport the player to a walking position, just behind the chair
                Player.main.transform.position =
                    mvSubmarine.Com.PilotSeats[0].CalculatedExitLocation;

                DoExitActions(ref myMode);
                myPlayer.mode = myMode;
                mvSubmarine.StopPiloting();
                return;
            }
            MyExitLockedMode();
            return;
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
            foreach (var container in Com.InnateStorages.Select(x => x.Container.GetComponent<InnateStorageContainer>().container))
            {
                if (container.HasRoomFor(pickup))
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
            foreach (var container in Com.InnateStorages.Select(x => x.Container.GetComponent<InnateStorageContainer>().container))
            {
                if (container.Contains(techType))
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
            foreach (var container in Com.InnateStorages.Select(x => x.Container.GetComponent<InnateStorageContainer>().container))
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
                return container.container.sizeX * container.container.sizeY;
            }
            int GetInnateStored(VehicleParts.VehicleStorage sto)
            {
                int ret = 0;
                var marty = (IEnumerable<InventoryItem>)sto.Container.GetComponent<InnateStorageContainer>().container;
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
                    || !mv.Engine
                    || !mv.Engine.enabled
                    || Player.main.GetPDA().isOpen
                    || (AvatarInputHandler.main && !AvatarInputHandler.main.IsEnabled())
                    || !mv.energyInterface.hasCharge)
                {
                    return;
                }
                mv.GetComponent<ModVehicleEngine>().ControlRotation();
            }
        }
        public static EnergyMixin GetEnergyMixinFromVehicle(Vehicle veh)
        {
            if ((veh as ModVehicle) == null)
            {
                return veh.GetComponent<EnergyMixin>();
            }
            else
            {
                return (veh as ModVehicle).energyInterface.sources.First();
            }
        }
        public static void TeleportPlayer(Vector3 destination)
        {
            ModVehicle mv = Player.main.GetModVehicle();
            UWE.Utils.EnterPhysicsSyncSection();
            Player.main.SetCurrentSub(null, true);
            Player.main.playerController.SetEnabled(false);
            IEnumerator waitForTeleport()
            {
                yield return null;
                Player.main.SetPosition(destination);
                Player.main.SetCurrentSub(mv?.GetComponent<SubRoot>(), true);
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
                { baseColorName, $"#{ColorUtility.ToHtmlStringRGB(baseColor)}" },
                { interiorColorName, $"#{ColorUtility.ToHtmlStringRGB(interiorColor)}" },
                { stripeColorName, $"#{ColorUtility.ToHtmlStringRGB(stripeColor)}" },
                { nameColorName, $"#{ColorUtility.ToHtmlStringRGB(nameColor)}" },
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
            Submarine sub = this as Submarine;
            sub?.PaintVehicleDefaultStyle(simpleData[mySubName]);
            if (Boolean.Parse(simpleData[defaultColorName]))
            {
                yield break;
            }
            if (ColorUtility.TryParseHtmlString(simpleData[baseColorName], out baseColor))
            {
                subName.SetColor(0, Vector3.zero, baseColor);
                sub?.PaintVehicleName(simpleData[mySubName], Color.black, baseColor);
            }
            if (ColorUtility.TryParseHtmlString(simpleData[nameColorName], out nameColor))
            {
                subName.SetColor(1, Vector3.zero, nameColor);
                sub?.PaintVehicleName(simpleData[mySubName], nameColor, baseColor);
            }
            if (ColorUtility.TryParseHtmlString(simpleData[interiorColorName], out interiorColor))
            {
                subName.SetColor(2, Vector3.zero, interiorColor);
            }
            if (ColorUtility.TryParseHtmlString(simpleData[stripeColorName], out stripeColor))
            {
                subName.SetColor(3, Vector3.zero, stripeColor);
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
        private Dictionary<string, List<Tuple<TechType, float, TechType>>> loadedStorageData = null;
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
        internal List<Tuple<TechType, float, TechType>> ReadInnateStorage(string path)
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
        private Dictionary<string, Tuple<TechType, float>> loadedBatteryData = null;
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
        internal Tuple<TechType, float> ReadBatteryData(string path)
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
            var child = transform.Find(childName)?.gameObject;
            if (!child)
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
