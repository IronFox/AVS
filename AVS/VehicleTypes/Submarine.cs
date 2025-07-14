using AVS.Composition;
using AVS.Configuration;
using AVS.Saving;
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
    public abstract class Submarine : ModVehicle
    {

        public Submarine(VehicleConfiguration config) : base(config)
        { }
        public abstract SubmarineComposition GetSubmarineComposition();
        public sealed override VehicleComposition GetVehicleComposition()
        {
            _subComposition = GetSubmarineComposition();
            return _subComposition;
        }

        private int currentPilotSeatIndex = 0;
        private SubmarineComposition? _subComposition;
        public new SubmarineComposition Com =>
            _subComposition
            ?? throw new InvalidOperationException("This vehicle's composition has not yet been initialized. Please wait until Submarine.Awake() has been called");


        public ControlPanel? controlPanelLogic; //must remain public field

        private bool isPilotSeated = false;
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


        public override VehicleSaveData AllocateSaveData()
            => new SubmarineSaveData();
        public override void WriteSaveData(VehicleSaveData saveData)
        {
            var sub = (SubmarineSaveData)saveData;
            sub.DefaultColorName = IsDefaultTexture;
            sub.CurrentPilotSeatIndex = currentPilotSeatIndex;
            base.WriteSaveData(saveData);
        }

        public override void LoadSaveData(VehicleSaveData? saveData)
        {
            var sub = saveData as SubmarineSaveData;
            base.LoadSaveData(saveData);

            if (sub != null)
            {
                IsDefaultTexture = sub.DefaultColorName;
                currentPilotSeatIndex = sub.CurrentPilotSeatIndex;


                PaintVehicleDefaultStyle(sub.VehicleName);
                if (IsDefaultTexture)
                    return;
                PaintVehicleName(sub.VehicleName, NameColor.RGB, BaseColor.RGB);
            }
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
            return isPilotSeated;
        }
        protected IEnumerator SitDownInChair()
        {
            Player.main.playerAnimator.SetBool("chair_sit", true);
            yield return null;
            Player.main.playerAnimator.SetBool("chair_sit", false);
        }
        protected IEnumerator StandUpFromChair()
        {
            Player.main.playerAnimator.SetBool("chair_stand_up", true);
            yield return null;
            Player.main.playerAnimator.SetBool("chair_stand_up", false);
        }
        protected IEnumerator TryStandUpFromChair()
        {
            yield return new WaitUntil(() => !IsPlayerControlling());
            yield return new WaitForSeconds(2);
            Player.main.playerAnimator.SetBool("chair_stand_up", true);
            yield return null;
            Player.main.playerAnimator.SetBool("chair_stand_up", false);
        }
        public override void BeginPiloting()
        {
            base.BeginPiloting();
            isPilotSeated = true;
            Player.main.armsController.ikToggleTime = 0;
            Player.main.armsController.SetWorldIKTarget(
                Com.SteeringWheelLeftHandTarget.GetTransform(),
                Com.SteeringWheelRightHandTarget.GetTransform());
            Player.main.SetCurrentSub(GetComponent<SubRoot>());
        }
        /// <inheritdoc/>
        public override void StopPiloting()
        {
            if (Player.main.currentSub != null && Player.main.currentSub.name.ToLower().Contains("cyclops"))
            {
                //Unfortunately, this method shares a name with some Cyclops components.
                // PilotingChair.ReleaseBy broadcasts a message for "StopPiloting"
                // So because a docked vehicle is part of the Cyclops heirarchy,
                // it tries to respond, which causes a game crash.
                // So we'll return if the player is within a Cyclops.
                return;
            }
            base.StopPiloting();
            isPilotSeated = false;
            Player.main.SetScubaMaskActive(false);
            Player.main.armsController.ikToggleTime = 0.5f;
            Player.main.armsController.SetWorldIKTarget(null, null);
            if (!IsVehicleDocked && IsPlayerControlling())
            {
                Player.main.transform.SetParent(transform);
                var exit = currentPilotSeatIndex < Com.PilotSeats.Count
                    ? Com.PilotSeats[currentPilotSeatIndex].ExitLocation
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
        public override void PlayerEntry()
        {
            Log.Debug(this, nameof(Submarine) + '.' + nameof(PlayerEntry));
            isPlayerInside = true;
            base.PlayerEntry();
            if (!isScuttled)
            {
                Player.main.currentMountedVehicle = this;
                if (!IsVehicleDocked)
                {
                    Player.main.transform.SetParent(transform);
                    Player.main.playerController.activeController.SetUnderWater(false);
                    Player.main.isUnderwater.Update(false);
                    Player.main.isUnderwaterForSwimming.Update(false);
                    Player.main.playerController.SetMotorMode(Player.MotorMode.Walk);
                    Player.main.motorMode = Player.MotorMode.Walk;
                    Player.main.playerMotorModeChanged.Trigger(Player.MotorMode.Walk);
                }
            }
            EnsureColorPickerEnabled();

            Player.main.CancelInvoke("ValidateCurrentSub");
            
            Log.Debug(this, nameof(Submarine) + '.' + nameof(PlayerEntry)+" done");
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


        public override void PlayerExit()
        {
            Log.Debug(this, nameof(Submarine) + '.' + nameof(PlayerExit));
            isPlayerInside = false;
            base.PlayerExit();
            //Player.main.currentSub = null;
            if (!IsVehicleDocked)
            {
                Player.main.transform.SetParent(null);
            }
            Log.Debug(this, nameof(Submarine) + '.' + nameof(PlayerExit) + " done");
        }
        public override void SubConstructionBeginning()
        {
            base.SubConstructionBeginning();
            PaintVehicleDefaultStyle(GetName());
        }
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

        public override void OnKill()
        {
            bool isplayerinthissub = IsPlayerInside();
            base.OnKill();
            if (isplayerinthissub)
            {
                PlayerEntry();
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
        public enum TextureDefinition : int
        {
            twice = 4096,
            full = 2048,
            half = 1024
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
            but.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = Language.main.Get("VFMainExterior");
            but.gameObject.EnsureComponent<Button>().onClick.AddListener(CreateAction("MainExterior"));

            but = ActualEditScreen.transform.Find("Active/NameTab");
            but.name = "PrimaryAccent";
            but.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = Language.main.Get("VFPrimaryAccent");
            but.gameObject.EnsureComponent<Button>().onClick.AddListener(CreateAction("PrimaryAccent"));

            but = ActualEditScreen.transform.Find("Active/InteriorTab");
            but.name = "SecondaryAccent";
            but.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = Language.main.Get("VFSecondaryAccent");
            but.gameObject.EnsureComponent<Button>().onClick.AddListener(CreateAction("SecondaryAccent"));

            but = ActualEditScreen.transform.Find("Active/Stripe1Tab");
            but.name = "NameLabel";
            but.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = Language.main.Get("VFNameLabel");
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
        }

        // this function returns the number of seconds to wait before opening the PDF,
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
            StopPiloting();
            base.OnPlayerDocked(exitLocation);
            //UWE.CoroutineHost.StartCoroutine(TryStandUpFromChair());
        }
        public override void OnPlayerUndocked()
        {
            base.OnPlayerUndocked();
            BeginPiloting();
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

        internal void EnterHelmControl(int seatIndex)
        {
            Log.Write($"Entering helm control for seat index {seatIndex} on submarine {VehicleName}");
            currentPilotSeatIndex = seatIndex;
            BeginPiloting();
        }

        /// <inheritdoc/>
        internal protected override void DoExitRoutines()
        {
            Log.Debug(this, nameof(Submarine)+'.'+nameof(DoExitRoutines));

            // check if we're level by comparing pitch and roll
            float roll = transform.rotation.eulerAngles.z;
            float rollDelta = roll >= 180 ? 360 - roll : roll;
            float pitch = transform.rotation.eulerAngles.x;
            float pitchDelta = pitch >= 180 ? 360 - pitch : pitch;
            if (!PlayerCanExitHelmControl(rollDelta, pitchDelta, useRigidbody.velocity.magnitude))
            {
                Logger.PDANote($"{Language.main.Get("AvsExitNotAllowed")} ({GameInput.Button.Exit})");
                return;
            }


            Com.Engine.KillMomentum();
            if (currentPilotSeatIndex >= Com.PilotSeats.Count)
            {
                Log.Error($"Error: tried to exit a submarine without pilot seats or with an incorrect selection ({currentPilotSeatIndex})");
                return;
            }

            Player myPlayer = Player.main;
            Player.Mode myMode = myPlayer.mode;

            DoCommonExitActions(ref myMode);
            myPlayer.mode = myMode;
            StopPiloting();

            var seat = Com.PilotSeats[currentPilotSeatIndex];
            var exitLocation = seat.ExitLocation;
            Vector3 exit;
            if (exitLocation != null)
            {
                Log.Debug(this, $"Exit location defined. Deriving from seat status {seat.Seat.transform.localPosition} / {seat.Seat.transform.localRotation}");
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
    }
}
