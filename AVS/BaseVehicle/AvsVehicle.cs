using AVS.Composition;
using AVS.Configuration;
using AVS.Localization;
using AVS.Log;
using AVS.MaterialAdapt;
using AVS.Util;
using AVS.VehicleComponents;
using System;
using System.Linq;
using UnityEngine;

namespace AVS.BaseVehicle
{
    /// <summary>
    /// ModVehicle is the primary abstract class provided by Vehicle Framework. 
    /// All VF vehicles inherit from ModVehicle.
    /// </summary>
    public abstract partial class AvsVehicle : Vehicle, ICraftTarget, IProtoTreeEventListener, ILogFilter
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
        /// The root game object of this vehicle. Usually the same as the vehicle game object.
        /// </summary>
        public virtual GameObject VehicleRoot => gameObject;

        /// <summary>
        /// Primary logging facility for this vehicle.
        /// </summary>
        internal LogWriter Log { get; }

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
        protected abstract VehicleComposition GetVehicleComposition();

        /// <summary>
        /// Resolved vehicle composition.
        /// If accessed before <see cref="Awake()"/> (or vehicle registration), InvalidOperationException will be thrown.
        /// </summary>
        public VehicleComposition Com => _composition
            ?? throw new InvalidOperationException($"This vehicle's composition has not yet been initialized. Please wait until {nameof(AvsVehicle)}.Awake() has been called");
        private VehicleComposition? _composition = null;

        /// <summary>
        /// Constructs the vehicle with the given configuration.
        /// </summary>
        /// <param name="config">Vehicle configuration. Must not be null</param>
        /// <exception cref="ArgumentNullException"></exception>
        protected AvsVehicle(VehicleConfiguration config)
        {
            Log = new LogWriter(prefix: $"V{Id}", tags: LogWriter.DefaultTags);


            Config = config ?? throw new ArgumentNullException(nameof(config), "VehicleConfiguration cannot be null");
            MaterialFixer = new MaterialFixer(this, new AvsMaterialResolver(config.MaterialAdaptConfig, this), config.MaterialAdaptConfig.LogConfig);
            baseColor = config.InitialBaseColor;
            stripeColor = config.InitialStripeColor;
            nameColor = config.InitialNameColor;
            interiorColor = config.InitialInteriorColor;

            GetOrCreateData();
        }

        /// <summary>
        /// Constructs a map mini-world hologram on the given object.
        /// </summary>
        /// <param name="worldObject">The object that will hold the map. May be scaled and rotated by the minimap</param>
        /// <param name="material">Material to use for the hologram</param>
        /// <param name="mapWorldRadius">Effective map radius in meters</param>
        /// <param name="withShadows">True if the map material should cast and receive shadows,
        /// false to use the default behavior (off)</param>
        /// <returns>Created mini-world</returns>
        public MiniWorld SpawnMiniWorld(Transform worldObject, Material material, int mapWorldRadius = 200, bool withShadows = true)
        {
            var world = worldObject.gameObject.EnsureComponent<MiniWorld>();
            world.hologramHolder = worldObject;
            world.hologramMaterial = material;
            world.hologramRadius = mapWorldRadius * 3.5f;
            world.mapWorldRadius = mapWorldRadius;
            world.fadeRadius = mapWorldRadius / 5;
            if (withShadows)
            {
                worldObject.gameObject.EnsureComponent<MiniWorldShadowCastMonitor>();
            }
            return world;
        }

        internal void OnAwakeOrPrefabricate()
        {
            RequireComposition();
            //playerPosition = GetMainHelm().PlayerControlLocation;
            playerPosition = null;
            if (SubRoot == null)
            {
                Log.Warn("SubRoot not found during OnAwakeOrPrefabricate");
            }
            else
                Log.Write("SubRoot found during OnAwakeOrPrefabricate");
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
        /// Gets the Autopilot system associated with the current instance.
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
            HeadlightsController = gameObject.EnsureComponent<HeadlightsController>();
            gameObject.AddComponent<VolumetricLightController>();

            autopilot = gameObject.EnsureComponent<Autopilot>();

            LazyInitialize();
            Com.Upgrades.ForEach(x =>
            {
                if (x.Interface == null)
                {
                    Log.Error($"Null upgrade interface found.");
                    return;
                }
                var consoleInput = x.Interface.GetComponent<VehicleUpgradeConsoleInput>();
                if (consoleInput == null)
                {
                    Log.Error($"VehicleUpgradeConsoleInput not found on {x.Interface.NiceName()}. This is a required component for vehicle upgrades.");
                }
                else
                    consoleInput.equipment = modules;
            });
            var warpChipThing = GetComponent("TelePingVehicleInstance");
            if (warpChipThing != null)
            {
                DestroyImmediate(warpChipThing);
            }
            vfxConstructing = GetComponent<VFXConstructing>();

            ReSetupInnateStorages();    //preserve labels. Also adapt to changes in language to some extent
            ReSetupModularStorages();    //preserve labels. Also adapt to changes in language to some extent
            ReSetupWaterParks();
            CheckEnergyInterface();
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

        /// <summary>
        /// If set true, the vehicle assumes the player will be manipulated by external processes.
        /// Remains active until the PDA, main menu, or builder menu are opened, or the player
        /// exists the vehicle or enters helm control.
        /// </summary>
        public bool AnticipatePlayerIssues { get; set; } = false;

        /// <summary>
        /// Reference to the custom emitter used for crush damage sounds.
        /// </summary>
        public GameObject? crushDamageEmitter;

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

            if (crushDamageEmitter != null)
            {
                if (IsBoarded)
                    crushDamageEmitter.transform.position = Player.main.camRoot.transform.position;
                else
                    crushDamageEmitter.transform.position = transform.position;
            }

            if (AnticipatePlayerIssues)
            {
                if (!isScuttled
                    && IsBoarded
                    && !IsPlayerControlling()
                    && !Character.IsAnyMenuOpen
                    )
                {
                    SanitizePlayerForWalking(false);
                }
                else
                    AnticipatePlayerIssues = false;
            }

            bool hadNoPlayerPosition = playerPosition == null;
            if (hadNoPlayerPosition)
                playerPosition = gameObject;
            try
            {
                base.Update();
            }
            catch (Exception e)
            {
                Log.Error($"Error during base.Update()", e);
            }
            if (hadNoPlayerPosition)
                playerPosition = null;
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
        /// <remarks>Calls <see cref="DestroyVehicle"/> </remarks>
        public new virtual void OnKill()
        {
            liveMixin.health = 0;
            if (IsBoarded)
            {
                Player.main.playerController.SetEnabled(true);
                Player.main.mode = Player.Mode.Normal;
                Player.main.playerModeChanged.Trigger(Player.main.mode);
                Player.main.sitting = false;
                Player.main.playerController.ForceControllerSize();
                Player.main.transform.parent = null;
                EndHelmControl(0);
                ClosestPlayerExit(true);
            }
            DestroyVehicle();
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
        /// Deselects quick-slots and exits piloting
        /// </summary>
        public void ExitHelmControl()
        {
            DeselectSlots();
        }



        /// <summary>
        /// Vehicle default name
        /// </summary>
        public override string vehicleDefaultName
        {
            get
            {
                return Translator.Get(TranslationKey.VehicleDefaultName);
            }
        }



        /// <summary>
        /// Supposed to be called when the AI battery is reloaded.
        /// The way it's implement now, this appears to be called when any battery is reloaded.
        /// </summary>
        public virtual void OnAIBatteryReload()
        {
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




        private PingInstance? hudPingInstance;

        /// <summary>
        /// Marker on the HUD.
        /// Can be used to enable or disable the marker.
        /// </summary>
        /// <remarks>Set during <see cref="Awake"/></remarks>
        public PingInstance HudPingInstance => hudPingInstance.OrThrow(
            () => new InvalidOperationException(
            $"Trying toa ccess HugPingInstance before Awake() was called"));

        /// <summary>
        /// Energy interface used by the AI.
        /// At present, this is only used by the <see cref="Autopilot" /> to refill oxygen.
        /// </summary>
        /// <remarks> Prefabrication fields must remain open fields or
        /// Unity instantiation will not preserve them. We cannot fetch it during awake because
        /// the vehicle may have multiple energy interfaces.</remarks>
        [SerializeField]
        internal EnergyInterface? aiEnergyInterface;


        /// <summary>
        /// if the player toggles the power off, the vehicle is called "powered off,"
        /// because it is unusable yet the batteries are not empty
        /// </summary>
        public bool IsPoweredOn { get; private set; } = true;


        /// <summary>
        /// Ambient sound emitter, copied from the seamoth prefab.
        /// </summary>
        /// <remarks>
        /// Copied during prefab setup.
        /// </remarks>
        [SerializeField]
        internal FMOD_StudioEventEmitter? ambienceSound;


        private PowerManager? powerManager;
        /// <summary>
        /// The vehicle's power manager.
        /// </summary>
        public PowerManager PowerManager => powerManager.OrThrow(
            () => new InvalidOperationException(
                $"Trying to access PowerManager before Awake() was called"));

        //public bool IsPlayerDry { get; private set; } = false;






        /// <summary>
        /// Fetches the tech type of the vehicle by searching for its <see cref="TechTag" /> component.
        /// </summary>
        public TechType TechType => GetComponent<TechTag>().type;




        #region internal_fields
        private bool isBoarded = false;

        #endregion





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
            bool shouldSetKinematic = teleporting || !constructionFallOverride && !GetPilotingMode() && (!Admin.GameStateWatcher.IsWorldSettled || docked || !vfxConstructing.IsConstructed());
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


        /// <summary>
        /// Broadcasts a new player status to all components that implement <see cref="IPlayerListener" />.
        /// </summary>
        /// <param name="newStatus">New status to broadcast</param>
        public void NotifyStatus(PlayerStatus newStatus)
        {
            foreach (var component in GetComponentsInChildren<IPlayerListener>())
            {
                switch (newStatus)
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
                        Log.Error("Error: tried to notify using an invalid status");
                        break;
                }
            }
        }
        /// <summary>
        /// Retrieves the current health and power values of the vehicle.
        /// Returned values are in the range of 0 to 1, where 1 is full health/power.
        /// </summary>
        /// <param name="health">Relative out health</param>
        /// <param name="power">Relative out power</param>
        public void GetHUDValues(out float health, out float power)
        {
            health = liveMixin.GetHealthFraction();
            GetEnergyValues(out float num, out float num2);
            power = num > 0f && num2 > 0f ? num / num2 : 0f;
        }

        /// <summary>
        /// Updates the vehicle name.
        /// </summary>
        /// <param name="name">New vehicle name</param>
        public void SetName(string name)
        {
            vehicleName = name;
            subName.SetName(name);
        }

        /// <summary>
        /// Gets the applied local vehicle name
        /// </summary>
        public string VehicleName => this.GetVehicleName();

        internal SubRoot? _subRoot;
        /// <summary>
        /// Retrieves and/or caches the SubRoot instance attached to this vehicle
        /// </summary>
        public SubRoot? SubRoot
        {
            get
            {
                if (_subRoot == null)
                    _subRoot = gameObject.GetComponent<SubRoot>();
                return _subRoot;
            }
        }


        /// <summary>
        /// Checks if the vehicle is in locked piloting mode.
        /// That is, the player is in locked piloting mode and is currently
        /// located in this vehicle's helm location.
        /// </summary>
        public new bool GetPilotingMode()
        {
            if (playerPosition == null)
                return false;
            return base.GetPilotingMode();
        }

        internal static void MaybeControlRotation(Vehicle veh)
        {
            if (veh is AvsVehicle mv)
            {
                if (!mv.GetPilotingMode()
                    || !mv.IsBoarded
                    || !mv.Com.Engine.enabled
                    || Player.main.GetPDA().isOpen
                    || AvatarInputHandler.main && !AvatarInputHandler.main.IsEnabled()
                    || !mv.energyInterface.hasCharge)
                {
                    return;
                }
                mv.Com.Engine.ControlRotation();
            }
        }

        /// <summary>
        /// Retrieves the <see cref="EnergyMixin"/> from the given vehicle.
        /// </summary>
        /// <remarks>Called via reflection</remarks>
        /// <param name="veh">Vehicle to retrieve the energy mixin from</param>
        /// <returns>Energy mixin. Every vehicle should have one</returns>
        public static EnergyMixin GetEnergyMixinFromVehicle(Vehicle veh)
        {
            if (!(veh is AvsVehicle mod))
            {
                var em = veh.GetComponent<EnergyMixin>();
                if (em == null)
                {
                    throw new InvalidOperationException($"Vehicle {veh.name} does not have an EnergyMixin component. This is expected of any vehicle.");
                }
                return em;
            }
            else
            {
                return mod.energyInterface.sources.First();
            }
        }



    }
}
