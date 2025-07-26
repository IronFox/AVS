using AVS.BaseVehicle;
using AVS.Composition;
using AVS.Configuration;
using AVS.Localization;
using AVS.SaveLoad;
using AVS.Util;
using AVS.VehicleComponents;
using AVS.VehicleParts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
//using AVS.Localization;

namespace AVS.VehicleTypes
{
    /*
     * Submarine is the class of self-leveling, walkable submarines
     */
    public abstract class Submarine : AvsVehicle
    {

        /// <summary>
        /// Tether checks are suspended until the next time the player enters or exits helm/the vehicle.
        /// </summary>
        public bool ThetherChecksSuspended { get; internal set; }

        /// <summary>
        /// Constructor for Submarine.
        /// </summary>
        /// <param name="config">Configuration to use</param>
        public Submarine(VehicleConfiguration config) : base(config)
        { }
        /// <summary>
        /// Retrieves the composition for this submarine.
        /// Executed once during Awake.
        /// </summary>
        public abstract SubmarineComposition GetSubmarineComposition();
        /// <inheritdoc />
        public sealed override VehicleComposition GetVehicleComposition()
        {
            _subComposition = GetSubmarineComposition();
            return _subComposition;
        }

        private int currentHelmIndex = 0;
        private SubmarineComposition? _subComposition;

        /// <summary>
        /// The composition of this submarine.
        /// </summary>
        public new SubmarineComposition Com =>
            _subComposition
            ?? throw new InvalidOperationException("This vehicle's composition has not yet been initialized. Please wait until Submarine.Awake() has been called");


        public ControlPanel? controlPanelLogic; //must remain public field

        private bool isAtHelm = false;
        private bool isPlayerInside = false; // You can be inside a scuttled submarine yet not dry.

        /// <summary>
        /// Flood light controller created during Awake.
        /// </summary>
        public FloodLightsController? Floodlights { get; private set; }
        /// <summary>
        /// Interior light controller created during Awake.
        /// </summary>
        public InteriorLightsController? Interiorlights { get; private set; }
        /// <summary>
        /// Nav light controller created during Awake.
        /// </summary>
        public NavigationLightsController? Navlights { get; private set; }

        public GameObject? fabricator = null; //fabricator. Must remain public field

        /// <inheritdoc />
        protected override void CreateDataBlocks(Action<DataBlock> addBlock)
        {
            addBlock(new DataBlock("Submarine")
            {
                Persistable.Property("DefaultColorName", () => IsDefaultTexture, v => IsDefaultTexture = v),
                Persistable.Property("CurrentHelmIndex", () => isAtHelm ? currentHelmIndex : -1, v => currentHelmIndex = Math.Max(0, v))
            });
            base.CreateDataBlocks(addBlock);
        }



        /// <inheritdoc />
        protected override void OnDataLoaded()
        {
            base.OnDataLoaded();

            PaintVehicleDefaultStyle(VehicleName);
            if (IsDefaultTexture)
                return;
            PaintVehicleName(VehicleName, NameColor.RGB, BaseColor.RGB);
        }
        /// <inheritdoc />
        protected override Helm GetLoadedHelm()
        {
            return Com.Helms.Count > currentHelmIndex
                ? Com.Helms[currentHelmIndex]
                : Com.Helms[0];
        }

        public override bool CanPilot()
        {
            return !FPSInputModule.current.lockMovement && IsPowered();
        }

        public override void Awake()
        {
            base.Awake();
            Floodlights = gameObject.AddComponent<FloodLightsController>();
            Interiorlights = gameObject.AddComponent<InteriorLightsController>();
            Navlights = gameObject.AddComponent<NavigationLightsController>();
            gameObject.EnsureComponent<TetherSource>().mv = this;
            controlPanelLogic.SafeDo(x => x.Init());
        }
        public override void Start()
        {
            base.Start();

            // now that we're in-game, load the color picker
            // we can't do this before we're in-game because not all assets are ready before the game is started
            if (Com.ColorPicker != null)
            {
                if (Com.ColorPicker.transform.Find("EditScreen") == null)
                {
                    UWE.CoroutineHost.StartCoroutine(SetupColorPicker(Com.ColorPicker));
                }
                else
                {
                    EnsureColorPickerEnabled();
                }
            }
        }
        private void EnsureColorPickerEnabled()
        {
            if (Com.ColorPicker != null)
            {
                var edit = Com.ColorPicker.transform.Find("EditScreen");
                if (edit != null)
                    ActualEditScreen = edit.gameObject;
            }
            if (ActualEditScreen == null)
            {
                return;
            }
            // why is canvas sometimes disabled, and Active is sometimes inactive?
            // Don't know!
            ActualEditScreen.GetComponent<Canvas>().enabled = true;
            ActualEditScreen.transform.Find("Active").gameObject.SetActive(true);
        }
        public bool IsPlayerInside()
        {
            // this one is correct ?
            return isPlayerInside;
        }
        public bool IsPlayerPiloting()
        {
            return isAtHelm;
        }
        //protected IEnumerator SitDownInChair()
        //{
        //    Player.main.playerAnimator.SetBool("chair_sit", true);
        //    yield return null;
        //    Player.main.playerAnimator.SetBool("chair_sit", false);
        //}
        //protected IEnumerator StandUpFromChair()
        //{
        //    Player.main.playerAnimator.SetBool("chair_stand_up", true);
        //    yield return null;
        //    Player.main.playerAnimator.SetBool("chair_stand_up", false);
        //}
        //protected IEnumerator TryStandUpFromChair()
        //{
        //    yield return new WaitUntil(() => !IsPlayerControlling());
        //    yield return new WaitForSeconds(2);
        //    Player.main.playerAnimator.SetBool("chair_stand_up", true);
        //    yield return null;
        //    Player.main.playerAnimator.SetBool("chair_stand_up", false);
        //}

        /// <inheritdoc/>
        public override Helm GetMainHelm()
            => Com.Helms[0];


        /// <inheritdoc/>
        protected override void OnBeginHelmControl(Helm helm)
        {
            base.OnBeginHelmControl(helm);
            ThetherChecksSuspended = false;
            isAtHelm = true;
            currentHelmIndex = Com.Helms.FindIndexOf(x => x.Root == helm.Root);
            if (currentHelmIndex < 0)
            {
                Logger.Error($"Error: helm {helm.Root.name} not found in submarine {VehicleName}. Defaulting to first helm.");
                currentHelmIndex = 0;
            }

            Player.main.SetCurrentSub(GetComponent<SubRoot>());
        }

        /// <inheritdoc/>
        protected override void OnEndHelmControl()
        {
            base.OnEndHelmControl();
            ThetherChecksSuspended = false;
            isAtHelm = false;
            Player.main.SetScubaMaskActive(false);
            Player.main.armsController.ikToggleTime = 0.5f;
            Player.main.armsController.SetWorldIKTarget(null, null);
            if (!IsVehicleDocked && IsPlayerControlling())
            {
                Player.main.transform.SetParent(transform);
                var exit = currentHelmIndex < Com.Helms.Count
                    ? Com.Helms[currentHelmIndex].ExitLocation
                    : null;
                if (exit == null)
                {
                    var tetherTarget = Com.TetherSources.FirstOrDefault(x => x != null);
                    if (tetherTarget != null)
                    {
                        Logger.Warn("Warning: pilot exit location is null. Defaulting to first tether.");
                        Player.main.transform.position = tetherTarget.transform.position;
                    }
                    else
                        Logger.Error("Error: pilot exit location is null. Tether source is empty.");
                }
                else
                {
                    Player.main.transform.position = exit.position;
                }
            }
            if (isScuttled)
            {
                UWE.CoroutineHost.StartCoroutine(GrantPlayerInvincibility(3f));
            }
            Player.main.SetCurrentSub(GetComponent<SubRoot>());
        }
        public static IEnumerator GrantPlayerInvincibility(float time)
        {
            Player.main.liveMixin.invincible = true;
            yield return new WaitForSeconds(time);
            Player.main.liveMixin.invincible = false;
        }
        /// <inheritdoc/>
        protected override void OnPlayerEntry()
        {
            Log.Debug(this, nameof(Submarine) + '.' + nameof(OnPlayerEntry));
            isPlayerInside = true;
            ThetherChecksSuspended = false;
            //if (!isScuttled)
            //{
            //    if (!IsVehicleDocked)
            //    {
            //        Player.main.transform.SetParent(transform);
            //        Player.main.playerController.activeController.SetUnderWater(false);
            //        Player.main.isUnderwater.Update(false);
            //        Player.main.isUnderwaterForSwimming.Update(false);
            //        Player.main.playerController.SetMotorMode(Player.MotorMode.Walk);
            //        Player.main.motorMode = Player.MotorMode.Walk;
            //        Player.main.playerMotorModeChanged.Trigger(Player.MotorMode.Walk);
            //    }
            //}
            EnsureColorPickerEnabled();

            Player.main.CancelInvoke("ValidateCurrentSub");

            Log.Debug(this, nameof(Submarine) + '.' + nameof(OnPlayerEntry) + " done");
        }

        ///// <summary>
        ///// Attempts to recover from being unable to build anything anymore.
        ///// Observation appears to indicate this happens if the player walks beyond +-35 meters from the origin
        ///// and normally does not recover until the player exits the craft.
        ///// </summary>
        //public void TryFixLostBuildFocus()
        //{
        //    Log.Write(nameof(TryFixLostBuildFocus));
        //    if (isScuttled || IsVehicleDocked)
        //        return;

        //    //IsUnderCommand = false;



        //    Player.main.lastValidSub = GetComponent<SubRoot>();
        //    Player.main.SetCurrentSub(GetComponent<SubRoot>(), true);
        //    NotifyStatus(PlayerStatus.OnPlayerEntry);



        //    Player.main.currentMountedVehicle = this;
        //    Player.main.transform.SetParent(transform);
        //    Player.main.playerController.activeController.SetUnderWater(false);
        //    Player.main.isUnderwater.Update(false);
        //    Player.main.isUnderwaterForSwimming.Update(false);
        //    Player.main.playerController.SetMotorMode(Player.MotorMode.Walk);
        //    Player.main.motorMode = Player.MotorMode.Walk;
        //    Player.main.playerMotorModeChanged.Trigger(Player.MotorMode.Walk);

        //}

        /// <inheritdoc/>
        protected override void OnPlayerExit()
        {
            Log.Debug(this, nameof(Submarine) + '.' + nameof(OnPlayerExit));
            isPlayerInside = false;
            ThetherChecksSuspended = false;
            Log.Debug(this, nameof(Submarine) + '.' + nameof(OnPlayerExit) + " done");
        }


        /// <inheritdoc/>
        public override void SubConstructionBeginning()
        {
            base.SubConstructionBeginning();
            PaintVehicleDefaultStyle(GetName());
        }

        /// <inheritdoc/>
        public override void SubConstructionComplete()
        {
            if (!HudPingInstance.enabled)
            {
                // Setup the color picker with the submarine's name
                var active = transform.Find("ColorPicker/EditScreen/Active");
                if (active)
                {
                    active.transform.Find("InputField").GetComponent<uGUI_InputField>().text = GetName();
                    active.transform.Find("InputField/Text").GetComponent<TMPro.TextMeshProUGUI>().text = GetName();
                }
                UWE.CoroutineHost.StartCoroutine(TrySpawnFabricator());
            }
            base.SubConstructionComplete();
            PaintNameDefaultStyle(GetName());
        }

        /// <inheritdoc/>
        public override void OnKill()
        {
            bool isplayerinthissub = IsPlayerInside();
            base.OnKill();
            if (isplayerinthissub)
            {
                ClosestPlayerEntry();
            }
        }

        IEnumerator TrySpawnFabricator()
        {
            if (Com.Fabricator == null)
            {
                yield break;
            }
            foreach (var fab in GetComponentsInChildren<Fabricator>())
            {
                if (fab.gameObject.transform.localPosition == Com.Fabricator.transform.localPosition)
                {
                    // This fabricator blueprint has already been fulfilled.
                    yield break;
                }
            }
            yield return SpawnFabricator(Com.Fabricator.transform);
        }

        IEnumerator SpawnFabricator(Transform location)
        {
            TaskResult<GameObject> result = new TaskResult<GameObject>();
            yield return UWE.CoroutineHost.StartCoroutine(CraftData.InstantiateFromPrefabAsync(TechType.Fabricator, result, false));
            fabricator = result.Get();
            fabricator.GetComponent<SkyApplier>().enabled = true;
            fabricator.transform.SetParent(transform);
            fabricator.transform.localPosition = location.localPosition;
            fabricator.transform.localRotation = location.localRotation;
            fabricator.transform.localScale = location.localScale;
            if (location.localScale.x == 0 || location.localScale.y == 0 || location.localScale.z == 0)
            {
                fabricator.transform.localScale = Vector3.one;
            }
            yield break;
        }
        public virtual void PaintNameDefaultStyle(string name)
        {
            OnNameChange(name);
        }
        public virtual void PaintVehicleDefaultStyle(string name)
        {
            IsDefaultTexture = true;
            PaintNameDefaultStyle(name);
        }

        public void PaintVehicleSection(string materialName, VehicleColor col)
            => PaintVehicleSection(materialName, col.RGB);
        public virtual void PaintVehicleSection(string materialName, Color col)
        {
        }
        public void PaintVehicleName(string name, VehicleColor nameColor, VehicleColor hullColor)
            => PaintVehicleName(name, nameColor.RGB, hullColor.RGB);
        public virtual void PaintVehicleName(string name, Color nameColor, Color hullColor)
        {
            OnNameChange(name);
        }

        public bool IsDefaultTexture = true;

        public override void SetBaseColor(VehicleColor color)
        {
            base.SetBaseColor(color);
            PaintVehicleSection("ExteriorMainColor", color.RGB);
        }
        public override void SetInteriorColor(VehicleColor color)
        {
            base.SetInteriorColor(color);
            PaintVehicleSection("ExteriorPrimaryAccent", color.RGB);
        }
        public override void SetStripeColor(VehicleColor color)
        {
            base.SetStripeColor(color);
            PaintVehicleSection("ExteriorSecondaryAccent", color.RGB);
        }

        public virtual void SetColorPickerUIColor(string name, Color col)
        {
            if (ActualEditScreen != null)
                ActualEditScreen.transform.Find("Active/" + name + "/SelectedColor").GetComponent<Image>().color = col;
        }
        public virtual void OnColorChange(ColorChangeEventData eventData)
        {
            // determine which tab is selected
            // call the desired function

            if (ActualEditScreen == null)
            {
                Logger.Error("Error: ActualEditScreen is null. Color picker cannot be used.");
                return;
            }

            List<string> tabnames = new List<string>() { "MainExterior", "PrimaryAccent", "SecondaryAccent", "NameLabel" };
            string selectedTab = "";
            foreach (string tab in tabnames)
            {
                if (ActualEditScreen.transform.Find("Active/" + tab + "/Background").gameObject.activeSelf)
                {
                    selectedTab = tab;
                    break;
                }
            }

            SetColorPickerUIColor(selectedTab, eventData.color);
            switch (selectedTab)
            {
                case "MainExterior":
                    IsDefaultTexture = false;
                    base.SetBaseColor(new VehicleColor(eventData.color));
                    break;
                case "PrimaryAccent":
                    IsDefaultTexture = false;
                    base.SetInteriorColor(new VehicleColor(eventData.color));
                    break;
                case "SecondaryAccent":
                    IsDefaultTexture = false;
                    base.SetStripeColor(new VehicleColor(eventData.color));
                    break;
                case "NameLabel":
                    base.SetNameColor(new VehicleColor(eventData.color));
                    break;
                default:
                    break;
            }
            ActualEditScreen.transform.Find("Active/MainExterior/SelectedColor")
                .GetComponent<Image>().color = BaseColor.RGB;
        }
        public virtual void OnNameChange(string e) // why is this independent from OnNameChange?
        {
            if (vehicleName != e)
            {
                SetName(e);
            }
        }
        public virtual void OnColorSubmit() // called by color picker submit button
        {
            SetBaseColor(BaseColor);
            SetInteriorColor(InteriorColor);
            SetStripeColor(StripeColor);
            SetNameColor(NameColor);
            if (IsDefaultTexture)
            {
                PaintVehicleDefaultStyle(GetName());
            }
            else
            {
                PaintVehicleName(GetName(), NameColor.RGB, BaseColor.RGB);
            }
            return;
        }

        public GameObject? ActualEditScreen { get; private set; } = null;

        public IEnumerator SetupColorPicker(GameObject colorPickerParent)
        {
            UnityAction CreateAction(string name)
            {
                void Action()
                {
                    List<string> tabnames = new List<string>() { "MainExterior", "PrimaryAccent", "SecondaryAccent", "NameLabel" };
                    foreach (string tab in tabnames.FindAll(x => x != name))
                    {
                        ActualEditScreen.transform.Find("Active/" + tab + "/Background").gameObject.SetActive(false);
                    }
                    ActualEditScreen.transform.Find("Active/" + name + "/Background").gameObject.SetActive(true);
                }
                return Action;
            }

            GameObject? console = Resources.FindObjectsOfTypeAll<BaseUpgradeConsoleGeometry>()
                ?.ToList().Find(x => x.gameObject.name.Contains("Short")).SafeGetGameObject();

            if (console == null)
            {
                yield return UWE.CoroutineHost.StartCoroutine(Builder.BeginAsync(TechType.BaseUpgradeConsole));
                Builder.ghostModel.GetComponentInChildren<BaseGhost>().OnPlace();
                console = Resources.FindObjectsOfTypeAll<BaseUpgradeConsoleGeometry>().ToList().Find(x => x.gameObject.name.Contains("Short")).gameObject;
                Builder.End();
            }
            ActualEditScreen = GameObject.Instantiate(console.transform.Find("EditScreen").gameObject);
            ActualEditScreen.GetComponentInChildren<SubNameInput>().enabled = false;
            ActualEditScreen.name = "EditScreen";
            ActualEditScreen.SetActive(true);
            ActualEditScreen.transform.Find("Inactive").gameObject.SetActive(false);
            Vector3 originalLocalScale = ActualEditScreen.transform.localScale;


            var frame = colorPickerParent;
            ActualEditScreen.transform.SetParent(frame.transform);
            ActualEditScreen.transform.localPosition = new Vector3(.15f, .28f, 0.01f);
            ActualEditScreen.transform.localEulerAngles = new Vector3(0, 180, 0);
            ActualEditScreen.transform.localScale = originalLocalScale;

            var but = ActualEditScreen.transform.Find("Active/BaseTab");
            but.name = "MainExterior";
            but.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = Translator.Get(TranslationKey.ColorPicker_Tab_Exterior);
            but.gameObject.EnsureComponent<Button>().onClick.AddListener(CreateAction("MainExterior"));

            but = ActualEditScreen.transform.Find("Active/NameTab");
            but.name = "PrimaryAccent";
            but.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = Translator.Get(TranslationKey.ColorPicker_Tab_PrimaryAccent);
            but.gameObject.EnsureComponent<Button>().onClick.AddListener(CreateAction("PrimaryAccent"));

            but = ActualEditScreen.transform.Find("Active/InteriorTab");
            but.name = "SecondaryAccent";
            but.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = Translator.Get(TranslationKey.ColorPicker_Tab_SecondaryAccent);
            but.gameObject.EnsureComponent<Button>().onClick.AddListener(CreateAction("SecondaryAccent"));

            but = ActualEditScreen.transform.Find("Active/Stripe1Tab");
            but.name = "NameLabel";
            but.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = Translator.Get(TranslationKey.ColorPicker_Tab_Name);
            but.gameObject.EnsureComponent<Button>().onClick.AddListener(CreateAction("NameLabel"));

            GameObject colorPicker = ActualEditScreen.transform.Find("Active/ColorPicker").gameObject;
            colorPicker.GetComponentInChildren<uGUI_ColorPicker>().onColorChange.RemoveAllListeners();
            colorPicker.GetComponentInChildren<uGUI_ColorPicker>().onColorChange.AddListener(new UnityAction<ColorChangeEventData>(OnColorChange));
            ActualEditScreen.transform.Find("Active/Button").GetComponent<Button>().onClick.RemoveAllListeners();
            ActualEditScreen.transform.Find("Active/Button").GetComponent<Button>().onClick.AddListener(new UnityAction(OnColorSubmit));
            ActualEditScreen.transform.Find("Active/InputField").GetComponent<uGUI_InputField>().onEndEdit.RemoveAllListeners();
            ActualEditScreen.transform.Find("Active/InputField").GetComponent<uGUI_InputField>().onEndEdit.AddListener(new UnityAction<string>(OnNameChange));

            EnsureColorPickerEnabled();
            yield break;
        }

        public override void OnAIBatteryReload()
        {
            base.OnAIBatteryReload();
        }

        // this function returns the number of seconds to wait before opening the PDA,
        // to show off the cool animations~
        public override float OnStorageOpen(string name, bool open)
        {
            return 0;
        }

        public void EnableFabricator(bool enabled)
        {
            foreach (Transform tran in transform)
            {
                if (tran.gameObject.name == "Fabricator(Clone)")
                {
                    fabricator = tran.gameObject;
                    fabricator.GetComponentInChildren<Fabricator>().enabled = enabled;
                    fabricator.GetComponentInChildren<Collider>().enabled = enabled;
                    //fabricator.SetActive(enabled);
                }
            }
        }
        public override void OnVehicleDocked(Vector3 exitLocation)
        {
            base.OnVehicleDocked(exitLocation);
            EnableFabricator(false);
        }
        public override void OnVehicleUndocked()
        {
            base.OnVehicleUndocked();
            EnableFabricator(true);
        }
        public override void OnPlayerDocked(Vector3 exitLocation)
        {
            EndHelmControl(0.5f);
            base.OnPlayerDocked(exitLocation);
            //UWE.CoroutineHost.StartCoroutine(TryStandUpFromChair());
        }
        public override void OnPlayerUndocked()
        {
            base.OnPlayerUndocked();
            var helm = Com.Helms[0];
            BeginHelmControl(helm);
        }
        public override void ScuttleVehicle()
        {
            base.ScuttleVehicle();
            EnableFabricator(false);
        }
        public override void UnscuttleVehicle()
        {
            base.UnscuttleVehicle();
            EnableFabricator(true);
        }

        internal void EnterHelmControl(int helmIndex)
        {
            Log.Write($"Entering helm control for seat index {helmIndex} on submarine {VehicleName}");
            BeginHelmControl(Com.Helms[helmIndex]);
        }



        /// <inheritdoc/>
        internal protected override void DoExitRoutines()
        {
            Log.Debug(this, nameof(Submarine) + '.' + nameof(DoExitRoutines));

            // check if we're level by comparing pitch and roll
            float roll = transform.rotation.eulerAngles.z;
            float rollDelta = roll >= 180 ? 360 - roll : roll;
            float pitch = transform.rotation.eulerAngles.x;
            float pitchDelta = pitch >= 180 ? 360 - pitch : pitch;
            if (!PlayerCanExitHelmControl(rollDelta, pitchDelta, useRigidbody.velocity.magnitude))
            {
                Logger.PDANote($"{Translator.Get(TranslationKey.Error_CannotExitVehicle)} ({GameInput.Button.Exit})");
                return;
            }


            Com.Engine.KillMomentum();
            if (currentHelmIndex >= Com.Helms.Count)
            {
                Log.Error($"Error: tried to exit a submarine without pilot seats or with an incorrect selection ({currentHelmIndex})");
                return;
            }

            Player myPlayer = Player.main;
            Player.Mode myMode = myPlayer.mode;

            DoCommonExitActions(ref myMode);
            myPlayer.mode = myMode;
            EndHelmControl(0f);

            var seat = Com.Helms[currentHelmIndex];
            var exitLocation = seat.ExitLocation;
            Vector3 exit;
            if (exitLocation != null)
            {
                Log.Debug(this, $"Exit location defined. Deriving from seat status {seat.Root.transform.localPosition} / {seat.Root.transform.localRotation}");
                exit = exitLocation.position;
            }
            else
            {
                Log.Debug(this, $"Exit location not declared in seat definition. Calculating location");
                // if the exit location is not set, use the calculated exit location
                exit = seat.CalculatedExitLocation;
            }
            Log.Debug(this, $"Exiting submarine at {exit} (local {transform.InverseTransformPoint(exit)})");
            Player.main.transform.position = exit;
        }

        /// <summary>
        /// Registers that the player was close enough to a tether source to be considered inside the sub.
        /// </summary>
        /// <param name="tetherSource">Tether source that triggered the event</param>
        internal void RegisterTetherEntry(TetherSource tetherSource)
        {
            if (ThetherChecksSuspended)
                return;
            RegisterPlayerEntry();
        }

        /// <summary>
        /// Suspends tether checks until the character next enters or exits helm/the vehicle
        /// </summary>
        public void SuspendTetherChecks()
        {
            ThetherChecksSuspended = true;
        }

    }
}
