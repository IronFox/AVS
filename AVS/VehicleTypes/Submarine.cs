using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AVS.BaseVehicle;
using AVS.Composition;
using AVS.Configuration;
using AVS.Localization;
using AVS.SaveLoad;
using AVS.Util;
using AVS.VehicleBuilding;
using AVS.VehicleComponents;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

//using AVS.Localization;

namespace AVS.VehicleTypes;

/// <summary>
///     Submarine is the class of self-leveling, walkable vehicle
/// </summary>
public abstract class Submarine : AvsVehicle
{
    [SerializeField] internal ControlPanel? controlPanelLogic; //must remain field

    [SerializeField] internal GameObject? fabricator; //fabricator. must remain field
    private SubmarineComposition? _subComposition;

    private int currentHelmIndex;

    private bool isAtHelm;
    private bool isPlayerInside; // You can be inside a scuttled submarine yet not dry.

    /// <summary>
    ///     Constructor for Submarine.
    /// </summary>
    /// <param name="config">Configuration to use</param>
    public Submarine(VehicleConfiguration config) : base(config)
    {
    }

    /// <summary>
    ///     Indicates whether the submarine is using its default texture or a customized one.
    /// </summary>
    public bool IsDefaultTexture { get; set; } = true;

    /// <summary>
    ///     Tether checks are suspended until the next time the player enters or exits helm/the vehicle.
    /// </summary>
    public bool ThetherChecksSuspended { get; internal set; }

    /// <summary>
    ///     The composition of this submarine.
    /// </summary>
    public new SubmarineComposition Com =>
        _subComposition
        ?? throw new InvalidOperationException(
            "This vehicle's composition has not yet been initialized. Please wait until Submarine.Awake() has been called");

    /// <summary>
    ///     Flood light controller created during Awake.
    /// </summary>
    /// <remarks>
    ///     Auto-destroyed/nulled if no floodlights were declared.
    /// </remarks>
    public FloodlightsController? Floodlights { get; private set; }

    /// <summary>
    ///     Interior light controller created during Awake.
    /// </summary>
    /// <remarks>
    ///     Auto-destroyed/nulled if no interior lights were declared.
    /// </remarks>
    public InteriorLightsController? Interiorlights { get; private set; }

    /// <summary>
    ///     Nav light controller created during Awake.
    /// </summary>
    /// <remarks>
    ///     Auto-destroyed/nulled if no navigation lights were declared.
    /// </remarks>
    public NavigationLightsController? NavLights { get; private set; }

    /// <summary>
    ///     Represents the actual edit screen associated with the submarine's color picker functionality.
    ///     This is used to manage UI elements for editing colors and associated settings.
    /// </summary>
    public GameObject? ActualEditScreen { get; private set; }

    /// <inheritdoc />
    public override void Awake()
    {
        base.Awake();
        Floodlights = gameObject.AddComponent<FloodlightsController>();
        Interiorlights = gameObject.AddComponent<InteriorLightsController>();
        NavLights = gameObject.AddComponent<NavigationLightsController>();
        gameObject.EnsureComponent<TetherSource>().mv = this;
        controlPanelLogic.SafeDo(x => x.Init());
    }


    /// <inheritdoc />
    public override void Start()
    {
        base.Start();

        // now that we're in-game, load the color picker
        // we can't do this before we're in-game because not all assets are ready before the game is started
        if (Com.ColorPicker != null)
        {
            if (Com.ColorPicker.transform.Find("EditScreen") == null)
                MainPatcher.Instance.StartCoroutine(SetupColorPicker(Com.ColorPicker));
            else
                EnsureColorPickerEnabled();
        }
    }

    /// <summary>
    ///     Retrieves the composition for this submarine.
    ///     Executed once during Awake.
    /// </summary>
    protected abstract SubmarineComposition GetSubmarineComposition();

    /// <inheritdoc />
    protected sealed override VehicleComposition GetVehicleComposition()
    {
        _subComposition = GetSubmarineComposition();
        return _subComposition;
    }

    /// <inheritdoc />
    protected override void CreateDataBlocks(Action<DataBlock> addBlock)
    {
        addBlock(new DataBlock("Submarine")
        {
            Persistable.Property("DefaultColorName", () => IsDefaultTexture, v => IsDefaultTexture = v),
            Persistable.Property("CurrentHelmIndex", () => isAtHelm ? currentHelmIndex : -1,
                v => currentHelmIndex = Math.Max(0, v))
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

    private void EnsureColorPickerEnabled()
    {
        if (Com.ColorPicker != null)
        {
            var edit = Com.ColorPicker.transform.Find("EditScreen");
            if (edit != null)
                ActualEditScreen = edit.gameObject;
        }

        if (ActualEditScreen == null) return;
        // why is canvas sometimes disabled, and Active is sometimes inactive?
        // Don't know!
        ActualEditScreen.GetComponent<Canvas>().enabled = true;
        ActualEditScreen.transform.Find("Active").gameObject.SetActive(true);
    }

    /// <summary>
    ///     True if the player is inside the submarine, false otherwise.
    /// </summary>
    public bool IsPlayerInside()
    {
        return isPlayerInside;
    }

    /// <summary>
    ///     Gets a value indicating whether the player is currently piloting the vehicle.
    /// </summary>
    public bool IsPlayerPiloting()
    {
        return isAtHelm;
    }


    /// <inheritdoc />
    public override Helm GetMainHelm()
    {
        return Com.Helms[0];
    }


    /// <inheritdoc />
    protected override void OnBeginHelmControl(Helm helm)
    {
        base.OnBeginHelmControl(helm);
        ThetherChecksSuspended = false;
        isAtHelm = true;
        currentHelmIndex = Com.Helms.FindIndexOf(x => x.Root == helm.Root);
        if (currentHelmIndex < 0)
        {
            Logger.Error(
                $"Error: helm {helm.Root.name} not found in submarine {VehicleName}. Defaulting to first helm.");
            currentHelmIndex = 0;
        }

        Player.main.SetCurrentSub(GetComponent<SubRoot>());
    }

    /// <inheritdoc />
    protected override void OnEndHelmControl()
    {
        base.OnEndHelmControl();
        ThetherChecksSuspended = false;
        isAtHelm = false;
        Player.main.SetScubaMaskActive(false);
        Player.main.armsController.ikToggleTime = 0.5f;
        Player.main.armsController.SetWorldIKTarget(null, null);
        if (!IsVehicleDocked && currentHelmIndex >= 0)
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
                {
                    Logger.Error("Error: pilot exit location is null. Tether source is empty.");
                }
            }
            else
            {
                Player.main.transform.position = exit.position;
            }
        }

        if (isScuttled) Character.GrantInvincibility(3f);
        Player.main.SetCurrentSub(GetComponent<SubRoot>());
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    protected override void OnPlayerExit()
    {
        Log.Debug(this, nameof(Submarine) + '.' + nameof(OnPlayerExit));
        isPlayerInside = false;
        ThetherChecksSuspended = false;
        Log.Debug(this, nameof(Submarine) + '.' + nameof(OnPlayerExit) + " done");
    }


    /// <inheritdoc />
    public override void SubConstructionBeginning()
    {
        base.SubConstructionBeginning();
        PaintVehicleDefaultStyle(GetName());
    }

    /// <inheritdoc />
    public override void SubConstructionComplete()
    {
        if (!HudPingInstance.enabled)
        {
            // Setup the color picker with the submarine's name
            var active = transform.Find("ColorPicker/EditScreen/Active");
            if (active)
            {
                active.transform.Find("InputField").GetComponent<uGUI_InputField>().text = GetName();
                active.transform.Find("InputField/Text").GetComponent<TextMeshProUGUI>().text = GetName();
            }

            MainPatcher.Instance.StartCoroutine(TrySpawnFabricator());
        }

        base.SubConstructionComplete();
        PaintNameDefaultStyle(GetName());
    }

    /// <inheritdoc />
    public override void OnKill()
    {
        var isplayerinthissub = IsPlayerInside();
        base.OnKill();
        if (isplayerinthissub) ClosestPlayerExit(false);
    }

    private IEnumerator TrySpawnFabricator()
    {
        if (Com.Fabricator == null) yield break;
        foreach (var fab in GetComponentsInChildren<Fabricator>())
            if (fab.gameObject.transform.localPosition == Com.Fabricator.transform.localPosition)
                // This fabricator blueprint has already been fulfilled.
                yield break;

        yield return SpawnFabricator(Com.Fabricator.transform);
    }

    private IEnumerator SpawnFabricator(Transform location)
    {
        var result = new TaskResult<GameObject>();
        yield return MainPatcher.Instance.StartCoroutine(
            CraftData.InstantiateFromPrefabAsync(TechType.Fabricator, result));
        fabricator = result.Get();
        fabricator.GetComponent<SkyApplier>().enabled = true;
        fabricator.transform.SetParent(transform);
        fabricator.transform.localPosition = location.localPosition;
        fabricator.transform.localRotation = location.localRotation;
        fabricator.transform.localScale = location.localScale;
        if (location.localScale.x == 0 || location.localScale.y == 0 || location.localScale.z == 0)
            fabricator.transform.localScale = Vector3.one;
        yield break;
    }

    /// <summary>
    ///     Paints the vehicle's name using the default style.
    /// </summary>
    /// <param name="name">The name to paint on the vehicle.</param>
    public virtual void PaintNameDefaultStyle(string name)
    {
        OnNameChange(name);
    }

    /// <summary>
    ///     Paints the submarine vehicle's exterior using the default style, including default textures and name.
    /// </summary>
    /// <param name="name">The name of the vehicle to be painted.</param>
    public virtual void PaintVehicleDefaultStyle(string name)
    {
        IsDefaultTexture = true;
        PaintNameDefaultStyle(name);
    }

    /// <summary>
    ///     Paints a specific section of the vehicle with the given material and color.
    /// </summary>
    /// <param name="materialName">The name of the material to be applied to the section of the vehicle.</param>
    /// <param name="col">The color to apply to the specified material.</param>
    public void PaintVehicleSection(string materialName, VehicleColor col)
    {
        PaintVehicleSection(materialName, col.RGB);
    }

    /// <summary>
    ///     Paints a specific section of the vehicle with the specified material and color.
    /// </summary>
    /// <param name="materialName">The name of the material to be painted.</param>
    /// <param name="col">The color to apply to the specified material.</param>
    public virtual void PaintVehicleSection(string materialName, Color col)
    {
    }

    /// <summary>
    ///     Paints the name of the vehicle and applies colors to the name and hull.
    /// </summary>
    /// <param name="name">The name of the vehicle to be painted.</param>
    /// <param name="nameColor">The color to be applied to the vehicle's name.</param>
    /// <param name="hullColor">The color to be applied to the vehicle's hull.</param>
    public void PaintVehicleName(string name, VehicleColor nameColor, VehicleColor hullColor)
    {
        PaintVehicleName(name, nameColor.RGB, hullColor.RGB);
    }

    /// <summary>
    ///     Paints the vehicle's name onto the vehicle using the specified colors.
    /// </summary>
    /// <param name="name">The name to be painted on the vehicle.</param>
    /// <param name="nameColor">The color to be used for the name text.</param>
    /// <param name="hullColor">The color to be used for the hull of the vehicle.</param>
    public virtual void PaintVehicleName(string name, Color nameColor, Color hullColor)
    {
        OnNameChange(name);
    }

    /// <summary>
    /// Sets the base color of the submarine.
    /// </summary>
    /// <param name="color">The color to be applied as the base color for the submarine.</param>
    public override void SetBaseColor(VehicleColor color)
    {
        base.SetBaseColor(color);
        PaintVehicleSection("ExteriorMainColor", color.RGB);
    }

    /// <summary>
    /// Sets the interior color of the submarine.
    /// </summary>
    /// <param name="color">The color to apply to the submarine's interior.</param>
    public override void SetInteriorColor(VehicleColor color)
    {
        base.SetInteriorColor(color);
        PaintVehicleSection("ExteriorPrimaryAccent", color.RGB);
    }

    /// <summary>
    /// Sets the stripe color of the submarine.
    /// </summary>
    /// <param name="color">The color to set as the stripe color.</param>
    public override void SetStripeColor(VehicleColor color)
    {
        base.SetStripeColor(color);
        PaintVehicleSection("ExteriorSecondaryAccent", color.RGB);
    }

    /// <summary>
    /// Sets the UI color of the color picker for a specific section of the vehicle edit screen.
    /// </summary>
    /// <param name="name">The name of the corresponding section whose color needs to be updated.</param>
    /// <param name="col">The new color to apply.</param>
    public virtual void SetColorPickerUIColor(string name, Color col)
    {
        if (ActualEditScreen != null)
            ActualEditScreen.transform.Find("Active/" + name + "/SelectedColor").GetComponent<Image>().color = col;
    }

    /// <summary>
    /// Handles changes in the color selection for the submarine.
    /// </summary>
    /// <param name="eventData">Data representing the change in color, including the selected color information.</param>
    protected virtual void OnColorChange(ColorChangeEventData eventData)
    {
        // determine which tab is selected
        // call the desired function

        if (ActualEditScreen == null)
        {
            Logger.Error("Error: ActualEditScreen is null. Color picker cannot be used.");
            return;
        }

        var tabnames = new List<string> { "MainExterior", "PrimaryAccent", "SecondaryAccent", "NameLabel" };
        var selectedTab = "";
        foreach (var tab in tabnames)
            if (ActualEditScreen.transform.Find("Active/" + tab + "/Background").gameObject.activeSelf)
            {
                selectedTab = tab;
                break;
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
        }

        ActualEditScreen.transform.Find("Active/MainExterior/SelectedColor")
            .GetComponent<Image>().color = BaseColor.RGB;
    }

    /// <summary>
    /// Called when the name of the vehicle is changed.
    /// </summary>
    /// <param name="e">The new name of the vehicle.</param>
    protected virtual void OnNameChange(string e) // why is this independent from OnNameChange?
    {
        if (vehicleName != e) SetName(e);
    }

    /// <summary>
    /// Handles the submission of selected colors from the color picker.
    /// This method applies the selected colors to the submarine's base, interior, stripe, and name.
    /// Additionally, updates the submarine's paint style based on whether the default texture is used.
    /// </summary>
    protected virtual void OnColorSubmit() // called by color picker submit button
    {
        SetBaseColor(BaseColor);
        SetInteriorColor(InteriorColor);
        SetStripeColor(StripeColor);
        SetNameColor(NameColor);
        if (IsDefaultTexture)
            PaintVehicleDefaultStyle(GetName());
        else
            PaintVehicleName(GetName(), NameColor.RGB, BaseColor.RGB);
    }

    private IEnumerator SetupColorPicker(GameObject colorPickerParent)
    {
        UnityAction CreateAction(string name)
        {
            void Action()
            {
                var tabnames = new List<string> { "MainExterior", "PrimaryAccent", "SecondaryAccent", "NameLabel" };
                foreach (var tab in tabnames.FindAll(x => x != name))
                    ActualEditScreen.transform.Find("Active/" + tab + "/Background").gameObject.SetActive(false);
                ActualEditScreen.transform.Find("Active/" + name + "/Background").gameObject.SetActive(true);
            }

            return Action;
        }

        var console = Resources.FindObjectsOfTypeAll<BaseUpgradeConsoleGeometry>()
            ?.ToList().Find(x => x.gameObject.name.Contains("Short")).SafeGetGameObject();

        if (console == null)
        {
            yield return MainPatcher.Instance.StartCoroutine(Builder.BeginAsync(TechType.BaseUpgradeConsole));
            Builder.ghostModel.GetComponentInChildren<BaseGhost>().OnPlace();
            console = Resources.FindObjectsOfTypeAll<BaseUpgradeConsoleGeometry>().ToList()
                .Find(x => x.gameObject.name.Contains("Short")).gameObject;
            Builder.End();
        }

        ActualEditScreen = Instantiate(console.transform.Find("EditScreen").gameObject);
        ActualEditScreen.GetComponentInChildren<SubNameInput>().enabled = false;
        ActualEditScreen.name = "EditScreen";
        ActualEditScreen.SetActive(true);
        ActualEditScreen.transform.Find("Inactive").gameObject.SetActive(false);
        var originalLocalScale = ActualEditScreen.transform.localScale;


        var frame = colorPickerParent;
        ActualEditScreen.transform.SetParent(frame.transform);
        ActualEditScreen.transform.localPosition = new Vector3(.15f, .28f, 0.01f);
        ActualEditScreen.transform.localEulerAngles = new Vector3(0, 180, 0);
        ActualEditScreen.transform.localScale = originalLocalScale;

        var but = ActualEditScreen.transform.Find("Active/BaseTab");
        but.name = "MainExterior";
        but.GetComponentInChildren<TextMeshProUGUI>().text = Translator.Get(TranslationKey.ColorPicker_Tab_Exterior);
        but.gameObject.EnsureComponent<Button>().onClick.AddListener(CreateAction("MainExterior"));

        but = ActualEditScreen.transform.Find("Active/NameTab");
        but.name = "PrimaryAccent";
        but.GetComponentInChildren<TextMeshProUGUI>().text = Translator.Get(TranslationKey.ColorPicker_Tab_Accent);
        but.gameObject.EnsureComponent<Button>().onClick.AddListener(CreateAction("PrimaryAccent"));

        but = ActualEditScreen.transform.Find("Active/InteriorTab");
        but.name = "SecondaryAccent";
        but.GetComponentInChildren<TextMeshProUGUI>().text = Translator.Get(TranslationKey.ColorPicker_Tab_Interior);
        but.gameObject.EnsureComponent<Button>().onClick.AddListener(CreateAction("SecondaryAccent"));

        but = ActualEditScreen.transform.Find("Active/Stripe1Tab");
        but.name = "NameLabel";
        but.GetComponentInChildren<TextMeshProUGUI>().text = Translator.Get(TranslationKey.ColorPicker_Tab_Name);
        but.gameObject.EnsureComponent<Button>().onClick.AddListener(CreateAction("NameLabel"));

        var colorPicker = ActualEditScreen.transform.Find("Active/ColorPicker").gameObject;
        colorPicker.GetComponentInChildren<uGUI_ColorPicker>().onColorChange.RemoveAllListeners();
        colorPicker.GetComponentInChildren<uGUI_ColorPicker>().onColorChange.AddListener(OnColorChange);
        ActualEditScreen.transform.Find("Active/Button").GetComponent<Button>().onClick.RemoveAllListeners();
        ActualEditScreen.transform.Find("Active/Button").GetComponent<Button>().onClick.AddListener(OnColorSubmit);
        ActualEditScreen.transform.Find("Active/InputField").GetComponent<uGUI_InputField>().onEndEdit
            .RemoveAllListeners();
        ActualEditScreen.transform.Find("Active/InputField").GetComponent<uGUI_InputField>().onEndEdit
            .AddListener(OnNameChange);

        EnsureColorPickerEnabled();
        yield break;
    }


    /// <inheritdoc />
    public override float OnStorageOpen(string name, bool open)
    {
        return 0;
    }

    /// <inheritdoc />
    public void EnableFabricator(bool enabled)
    {
        foreach (Transform tran in transform)
            if (tran.gameObject.name == "Fabricator(Clone)")
            {
                fabricator = tran.gameObject;
                fabricator.GetComponentInChildren<Fabricator>().enabled = enabled;
                fabricator.GetComponentInChildren<Collider>().enabled = enabled;
                //fabricator.SetActive(enabled);
            }
    }

    /// <inheritdoc />
    protected override void OnVehicleDocked()
    {
        base.OnVehicleDocked();
        EnableFabricator(false);
    }

    /// <inheritdoc />
    protected override void OnVehicleUndocked()
    {
        base.OnVehicleUndocked();
        EnableFabricator(true);
    }

    /// <inheritdoc />
    protected override void OnPreDockingPlayerExit()
    {
        EndHelmControl(0.5f);
        base.OnPreDockingPlayerExit();
    }

    /// <inheritdoc />
    protected override void OnUndockingPlayerEntry()
    {
        base.OnUndockingPlayerEntry();
        var helm = Com.Helms[0];
        BeginHelmControl(helm);
    }

    /// <inheritdoc />
    public override void ScuttleVehicle()
    {
        base.ScuttleVehicle();
        EnableFabricator(false);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    protected internal override void DoExitRoutines()
    {
        Log.Debug(this, nameof(Submarine) + '.' + nameof(DoExitRoutines));

        // check if we're level by comparing pitch and roll
        var roll = transform.rotation.eulerAngles.z;
        var rollDelta = roll >= 180 ? 360 - roll : roll;
        var pitch = transform.rotation.eulerAngles.x;
        var pitchDelta = pitch >= 180 ? 360 - pitch : pitch;
        if (!PlayerCanExitHelmControl(rollDelta, pitchDelta, useRigidbody.velocity.magnitude))
        {
            Logger.PDANote($"{Translator.Get(TranslationKey.Error_CannotExitHelmControl)}");
            return;
        }


        Com.Engine.KillMomentum();
        if (currentHelmIndex >= Com.Helms.Count)
        {
            Log.Error(
                $"Error: tried to exit a submarine without pilot seats or with an incorrect selection ({currentHelmIndex})");
            return;
        }

        var myPlayer = Player.main;
        var myMode = myPlayer.mode;

        DoCommonExitActions(ref myMode);
        myPlayer.mode = myMode;
        EndHelmControl(0f);

        var seat = Com.Helms[currentHelmIndex];
        var exitLocation = seat.ExitLocation;
        Vector3 exit;
        if (exitLocation != null)
        {
            Log.Debug(this,
                $"Exit location defined. Deriving from seat status {seat.Root.transform.localPosition} / {seat.Root.transform.localRotation}");
            exit = exitLocation.position;
        }
        else
        {
            Log.Debug(this, "Exit location not declared in seat definition. Calculating location");
            // if the exit location is not set, use the calculated exit location
            exit = seat.CalculatedExitLocation;
        }

        Log.Debug(this, $"Exiting submarine at {exit} (local {transform.InverseTransformPoint(exit)})");
        Player.main.transform.position = exit;
    }

    /// <summary>
    ///     Registers that the player was close enough to a tether source to be considered inside the sub.
    /// </summary>
    /// <param name="tetherSource">Tether source that triggered the event</param>
    internal void RegisterTetherEntry(TetherSource tetherSource)
    {
        if (ThetherChecksSuspended)
            return;
        RegisterPlayerEntry();
    }

    /// <summary>
    ///     Suspends tether checks until the character next enters or exits helm/the vehicle
    /// </summary>
    public void SuspendTetherChecks()
    {
        ThetherChecksSuspended = true;
    }
}