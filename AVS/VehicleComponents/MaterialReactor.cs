using AVS.BaseVehicle;
using AVS.Localization;
using AVS.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AVS.Log;
using UnityEngine;

namespace AVS.VehicleComponents;

/// <summary>
/// Represents a declaration of input parameters for a material reactor, including the input and output types and
/// energy specifications.
/// </summary>
/// <remarks>This structure is used to define the characteristics of a material reactor's input,
/// specifying the type of material used, the total energy it provides, the rate of energy release, and the
/// resulting output type.</remarks>
public readonly struct MaterialReactorConversionDeclaration
{
    /// <summary>
    /// The tech type of the material being consumed by the reactor.
    /// Each input tech type must be unique within the reactor's configuration.
    /// </summary>
    public TechType InputTechType { get; }

    /// <summary>
    /// The total amount of energy that can be generated from the input material.
    /// </summary>
    public float TotalEnergy { get; }

    /// <summary>
    /// Gets the amount of energy produced per second when consuming the input material.
    /// All materials in the reactor can be processed simultaneously,
    /// so the effective energy production rate is at most the sum of all materials'
    /// energy per second.
    /// </summary>
    public float EnergyPerSecond { get; }

    /// <summary>
    /// Waste material produced by the reactor after processing the input material.
    /// <see cref="TechType.None" /> if no waste is produced.
    /// </summary>
    public TechType OutputTechType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaterialReactorConversionDeclaration"/> struct with the specified parameters.
    /// </summary>
    public MaterialReactorConversionDeclaration(TechType inputTechType, float totalEnergy, float energyPerSecond,
        TechType outputTechType)
    {
        InputTechType = inputTechType;
        TotalEnergy = totalEnergy;
        EnergyPerSecond = energyPerSecond;
        OutputTechType = outputTechType;
    }
}

internal class ReactorBattery : MonoBehaviour, IBattery
{
    private float totalCapacity;
    private float currentCharge;

    float IBattery.charge
    {
        get => currentCharge;
        set => currentCharge = value;
    }

    float IBattery.capacity => totalCapacity;

    string IBattery.GetChargeValueText()
    {
        var percent = currentCharge.Percentage(totalCapacity);
        return Translator.GetFormatted(TranslationKey.Reactor_DedicatedBattery_ChargeValueText, percent,
            Mathf.RoundToInt(currentCharge), totalCapacity);
    }

    internal void SetCapacity(float capacity)
    {
        totalCapacity = capacity;
    }

    internal float GetCharge() => currentCharge;

    internal float SetCharge(float iCharge)
    {
        return currentCharge = iCharge;
    }
}

/// <summary>
/// Represents a reactor that processes materials to generate energy within a vehicle.
/// </summary>
/// <remarks>The <see cref="MaterialReactor"/> is designed to be attached to a vehicle and is responsible
/// for converting materials into energy. It manages an internal container for materials, processes them according
/// to specified energy values, and interacts with the vehicle's energy system. The reactor can display a whitelist
/// of processable materials and their energy potentials, and it supports localization for interaction
/// text.</remarks>
public class MaterialReactor : HandTarget, IHandTarget, IProtoTreeEventListener
{
    private AvsVehicle? mv;
    private ReactorBattery? reactorBattery;
    private float capacity = 0;

    private LogWriter Log { get; set; } = LogWriter.Default.Tag(nameof(MaterialReactor));


    /// <summary>
    /// Set to true if the reactor is currently processing materials and generating energy.
    /// If the local batteries are full, this will be false even if the reactor has materials
    /// to process.
    /// </summary>
    public bool isGeneratingEnergy;

    private MaybeTranslate label;


    /// <summary>
    /// True if the PDA should show the whitelist of materials that can be processed by this reactor
    /// when the user right-clicks on it.
    /// </summary>
    public bool canViewWhitelist = true;

    /// <summary>
    /// If true, the reactor will list the potential energy of each material in the whitelist.
    /// Not effective if <see cref="canViewWhitelist"/> is false.
    /// </summary>
    public bool listPotentials = true;

    /// <summary>
    /// Action to execute when the PDA is closed after being opened from this reactor.
    /// </summary>
    public Action<PDA>? onClosePDAAction = null;

    /// <summary>
    /// Action to execute when items were added to the local reactor.
    /// The first argument is the item that was added,
    /// the second argument is the total number of items in the reactor after the addition.
    /// </summary>
    public Action<InventoryItem, int>? onItemsAdded = null;


    private readonly Dictionary<TechType, float> maxEnergies = new();
    private readonly Dictionary<TechType, float> rateEnergies = new();
    private readonly Dictionary<InventoryItem, float> currentEnergies = new();
    private readonly Dictionary<TechType, TechType> spentMaterialIndex = new();
    private ItemsContainer? container;
    private Coroutine? outputReactorDataCoroutine = null;
    private bool isInitialized = false;
    private const string newSaveFileName = "Reactor";


    /// <summary>
    /// Initializes the MaterialReactor with the specified vehicle, container size, label, total energy capacity, and material data.
    /// </summary>
    /// <param name="avsVehicle">The vehicle to which this reactor is attached. Must not be null.</param>
    /// <param name="height">The height of the reactor's internal container grid. Must be positive.</param>
    /// <param name="width">The width of the reactor's internal container grid. Must be positive.</param>
    /// <param name="label">The label for the reactor's container UI.</param>
    /// <param name="totalCapacity">The total energy capacity of the reactor. Must be non-negative.</param>
    /// <param name="iMaterialData">A list of material data specifying which materials can be processed, their energy values, and output types. Must not be empty and all entries must have positive energy values.</param>
    /// <remarks>
    /// This method sets up the internal ItemsContainer, configures allowed materials, and initializes the energy mixin and battery.
    /// It also ensures that only one MaterialReactor is attached to a given vehicle at a time.
    /// An unintialized MaterialReactor will log an error and destroy itself on the first update cycle.
    /// </remarks>
    public void Initialize(AvsVehicle avsVehicle, int height, int width, MaybeTranslate label, float totalCapacity,
        List<MaterialReactorConversionDeclaration> iMaterialData)
    {
        this.label = label;
        if (avsVehicle.GetComponentsInChildren<MaterialReactor>().Any(x => x.mv == avsVehicle))
        {
            ErrorMessage.AddWarning($"A {nameof(AvsVehicle)} may (for now) only have one material reactor!");
            return;
        }

        if (avsVehicle.IsNull())
        {
            ErrorMessage.AddWarning($"Material Reactor {label} must be given a non-null {nameof(AvsVehicle)}.");
            return;
        }

        if (height < 0 || width < 0 || height * width == 0)
        {
            ErrorMessage.AddWarning($"Material Reactor {label} cannot have non-positive size.");
            return;
        }

        if (totalCapacity < 0)
        {
            ErrorMessage.AddWarning($"Material Reactor {label} cannot have negative capacity.");
            return;
        }

        if (!iMaterialData.Any())
        {
            ErrorMessage.AddWarning($"Material Reactor {label} must be able to accept some material.");
            return;
        }

        foreach (var datum in iMaterialData)
        {
            if (datum.TotalEnergy <= 0)
            {
                ErrorMessage.AddWarning(
                    $"Material Reactor {label} cannot process a material that has non-positive potential: {datum.InputTechType.AsString()}");
                return;
            }

            if (datum.EnergyPerSecond <= 0)
            {
                ErrorMessage.AddWarning(
                    $"Material Reactor {label} cannot process a material that provides non-positive energy: {datum.InputTechType.AsString()}");
                return;
            }
        }

        mv = avsVehicle;
        Log = mv.Log.Tag(nameof(MaterialReactor));
        capacity = totalCapacity;
        container = new ItemsContainer(width, height, transform, label.Rendered, null);
        container.onAddItem += OnAddItem;
        container.isAllowedToRemove = IsAllowedToRemove;
        container.SetAllowedTechTypes(iMaterialData
            .SelectMany(x => new List<TechType> { x.InputTechType, x.OutputTechType }).ToArray());
        foreach (var reagent in iMaterialData)
        {
            maxEnergies.Add(reagent.InputTechType, reagent.TotalEnergy);
            rateEnergies.Add(reagent.InputTechType, reagent.EnergyPerSecond);
            spentMaterialIndex.Add(reagent.InputTechType, reagent.OutputTechType);
        }

        gameObject.LoggedSetActive(false, Log);
        var eMix = gameObject.AddComponent<EnergyMixin>();
        eMix.batterySlot = new StorageSlot(transform);
        gameObject.LoggedSetActive(true, Log);
        var batteryObj = new GameObject("ReactorBattery");
        batteryObj.transform.SetParent(transform);
        reactorBattery = batteryObj.AddComponent<ReactorBattery>();
        reactorBattery.SetCapacity(capacity);
        eMix.battery = reactorBattery;
        mv.energyInterface.sources = mv.energyInterface.sources.Append(eMix).ToArray();
        gameObject.AddComponent<ChildObjectIdentifier>();
        isInitialized = true;
        mv.Log.Debug(
            $"MaterialReactor {label} initialized with width {width}, height {height}, and capacity {capacity}.");
    }

    /// <inheritdoc/>
    public void Update()
    {
        if (!isInitialized)
        {
            var errorMsg = $"Material Reactor must be manually initialized. Destroying.";
            Logger.Error(errorMsg);
            ErrorMessage.AddWarning(errorMsg);
            DestroyImmediate(this);
            return;
        }

        var any = false;
        var reactants = currentEnergies.Keys.ToList();
        foreach (var reactant in reactants)
        {
            var rate = rateEnergies[reactant.techType] * Time.deltaTime;
            var totalCanConsume = mv!.energyInterface.TotalCanConsume(out var sources);
            var consumed = mv!.energyInterface.AddEnergy(rate);
            //foreach (var em in mv.energyInterface.sources)
            //{
            //    if (em.IsNotNull())
            //        mv.Log.Debug($"{em.NiceName()}: {em.charge} < {em.capacity}");
            //}
            //mv.Log.Debug($"MaterialReactor {label} processing {reactant.techType.AsString()} at rate {rate}. Added {consumed} to {mv.Id} {mv.energyInterface.NiceName()}. RequiresPower := {GameModeUtils.RequiresPower()}. totalCanConsume:={totalCanConsume}, #sources:={sources} ");
            currentEnergies[reactant] -= consumed;
            any |= consumed > 0;
        }

        isGeneratingEnergy = any;
        var spentMaterials = new List<InventoryItem>();
        foreach (var reactantPair in currentEnergies.ToList().Where(x => x.Value <= 0))
        {
            spentMaterials.Add(reactantPair.Key);
            if (container!._items.TryGetValue(reactantPair.Key.techType, out var itemGroup))
            {
                List<InventoryItem> items = itemGroup.items;
                if (items.Remove(reactantPair.Key))
                {
                    if (items.Count == 0)
                    {
                        container._items.Remove(reactantPair.Key.techType);
                        container.NotifyRemoveItem(reactantPair.Key);
                        var toAdd = spentMaterialIndex[reactantPair.Key.techType];
                        if (toAdd != TechType.None) MainPatcher.Instance.StartCoroutine(AddMaterial(toAdd));
                    }

                    reactantPair.Key.container = null;
                    DestroyImmediate(reactantPair.Key.item.gameObject);
                }
            }
        }

        spentMaterials.ForEach(x => currentEnergies.Remove(x));
    }

    private IEnumerator AddMaterial(TechType toAdd)
    {
        if (mv.IsNull() || reactorBattery.IsNull() || container.IsNull())
        {
            Log.Error(
                $"MaterialReactor: {nameof(AvsVehicle)}, ReactorBattery or Container is null, cannot add material.");
            yield break;
        }

        var result = new InstanceContainer();
        yield return AvsCraftData.InstantiateFromPrefabAsync(Log, toAdd, result);
        var spentMaterial = result.Instance.SafeGetComponent<Pickupable>();
        if (spentMaterial.IsNull())
        {
            Log.Error($"MaterialReactor: Could not instantiate {toAdd.AsString()}");
            yield break;
        }

        spentMaterial.gameObject.LoggedSetActive(false, Log);
        try
        {
            container.AddItem(spentMaterial.GetComponent<Pickupable>());
        }
        catch (Exception e)
        {
            var errorMessage = $"Could not add material {toAdd.AsString()} to the material reactor!";
            Log.Error(errorMessage, e);
            ErrorMessage.AddError(errorMessage);
        }
    }

    private void OnAddItem(InventoryItem item)
    {
        if (maxEnergies.Keys.ToList().Contains(item.techType)) currentEnergies.Add(item, maxEnergies[item.techType]);
        onItemsAdded?.Invoke(item, container!.count);
        // check if it can rot, and disable all that
        var eatable = item.item.gameObject.GetComponent<Eatable>();
        if (eatable.IsNotNull()) eatable.decomposes = false;
    }

    private Dictionary<int, float> LastFishTankWarning { get; } = new();

    private bool CanWarnAbout(Pickupable pickupable)
    {
        if (!LastFishTankWarning.TryGetValue(pickupable.GetInstanceID(), out var lastWarning) ||
            Time.time - lastWarning > 30f)
        {
            LastFishTankWarning[pickupable.GetInstanceID()] = Time.time;
            return true;
        }

        return false;
    }

    private bool IsAllowedToRemove(Pickupable pickupable, bool verbose)
    {
        if (spentMaterialIndex.Values.ToList().Contains(pickupable.inventoryItem.techType)) return true;
        if (verbose || CanWarnAbout(pickupable))
            ErrorMessage.AddMessage(Translator.GetFormatted(TranslationKey.Error_CannotRemoveMaterialsFromReactor,
                label.Rendered));
        return false;
    }

    void IHandTarget.OnHandClick(GUIHand hand)
    {
        Logger.DebugLog($"MaterialReactor.OnHandClick: {hand.NiceName()} clicked on {gameObject.NiceName()}");
        if (container.IsNotNull())
        {
            var pda = Player.main.GetPDA();
            Inventory.main.SetUsedStorage(container);
            if (!pda.Open(PDATab.Inventory, transform, OnClosePDA))
                OnClosePDA(pda);
        }

        Logger.DebugLog($"MaterialReactor.OnHandClick: Exit");
    }

    void IHandTarget.OnHandHover(GUIHand hand)
    {
        if (!isInitialized || mv.IsNull() || reactorBattery.IsNull()) return;
        if (container.IsNotNull())
        {
            var main = HandReticle.main;
            var chargeValue = capacity == 0
                ? string.Empty
                : $"({Translator.GetFormatted(TranslationKey.HandHover_Reactor_Charge, (int)reactorBattery.GetCharge(), capacity)})";

            var finalText = $"{label} {chargeValue}";
            main.SetText(HandReticle.TextType.Hand, finalText, true, GameInput.Button.LeftHand);
            if (canViewWhitelist)
            {
                main.SetText(HandReticle.TextType.HandSubscript,
                    Translator.Get(TranslationKey.HandHoverSub_Reactor_ShowWhitelist), false,
                    GameInput.Button.RightHand);
                if (GameInput.GetButtonDown(GameInput.Button.RightHand) && outputReactorDataCoroutine.IsNull())
                    outputReactorDataCoroutine = MainPatcher.Instance.StartCoroutine(OutputReactorData());
            }
            else
            {
                main.SetText(HandReticle.TextType.HandSubscript, string.Empty, false);
            }

            main.SetIcon(HandReticle.IconType.Hand);
        }
    }

    private IEnumerator OutputReactorData()
    {
        Logger.PDANote(
            listPotentials
                ? Translator.Get(TranslationKey.Reactor_WhitelistWithPowerValue)
                : Translator.Get(TranslationKey.Reactor_WhitelistPlain), 4f);
        foreach (var pair in maxEnergies)
        {
            if (5 < Vector3.Distance(Player.main.transform.position, transform.position))
                break;
            yield return new WaitForSeconds(0.75f);
            Logger.PDANote(listPotentials
                    ? $"{pair.Key.AsString()} : {pair.Value}"
                    : pair.Key.AsString(),
                4f);
        }

        outputReactorDataCoroutine = null;
    }

    private void OnClosePDA(PDA pda)
    {
        onClosePDAAction?.Invoke(pda);
    }

    /// <summary>
    /// Computes the total energy potential of all materials in the reactor.
    /// </summary>
    public float GetFuelPotential() => currentEnergies.Values.ToList().Sum();

    /// <summary>
    /// Gets consumption data for bio reactors.
    /// </summary>
    /// <param name="energyPerSecond">The energy produced per second by each added material.</param>
    public static List<MaterialReactorConversionDeclaration> GetBioReactorData(float energyPerSecond = 1)
    {
        return BaseBioReactor.charge.Select(x =>
            new MaterialReactorConversionDeclaration
            (
                x.Key,
                x.Value,
                energyPerSecond,
                TechType.None
            )
        ).ToList();
    }

    /// <summary>
    /// Gets consumption data for nuclear reactors.
    /// The returned list contains a single entry for the reactor rod.
    /// </summary>
    /// <param name="energyPerSecond">The energy produced per second by each added reactor rod.</param>
    public static List<MaterialReactorConversionDeclaration> GetNuclearReactorData(float energyPerSecond = 5) =>
    [
        new(
            TechType.ReactorRod,
            20000f,
            energyPerSecond,
            TechType.DepletedReactorRod
        )
    ];

    private record SavedReactorStatus(IReadOnlyList<ReactorEntry> Entries, float BatteryCharge) : INullTestableType;

    void IProtoTreeEventListener.OnProtoSerializeObjectTree(ProtobufSerializer serializer)
    {
        if (mv.IsNull())
        {
            Log.Error($"MaterialReactor: {nameof(AvsVehicle)} is null, cannot save data.");
            return;
        }

        Log.Write($"Saving state");
        mv.PrefabID.WriteReflected(newSaveFileName,
            new SavedReactorStatus(GetSaveDict(), reactorBattery.SafeGet(x => x.GetCharge(), 0)), mv.Log);
    }

    void IProtoTreeEventListener.OnProtoDeserializeObjectTree(ProtobufSerializer serializer)
    {
        if (mv.IsNull())
        {
            Log.Error($"MaterialReactor: {nameof(AvsVehicle)} is null, cannot load saved data.");
            return;
        }

        Log.Write($"Loading state");

        mv.PrefabID.ReadReflected(newSaveFileName, out SavedReactorStatus? reactorStatus, mv.Log);
        if (reactorStatus.IsNull())
        {
            Log.Warn("MaterialReactor: No saved data found, skipping load.");
            return;
        }

        if (reactorBattery.IsNotNull())
            reactorBattery.SetCharge(reactorStatus.BatteryCharge);
        MainPatcher.Instance.StartCoroutine(LoadSaveDict(reactorStatus.Entries));
    }

    // ReSharper disable once NotAccessedPositionalProperty.Local
    private record ReactorEntry(string Type, float Energy, float MaxEnergy);

    private IReadOnlyList<ReactorEntry> GetSaveDict()
    {
        if (reactorBattery.IsNull())
        {
            Logger.Error("MaterialReactor: ReactorBattery is null, cannot save data.");
            return [];
        }

        if (container.IsNull())
        {
            Logger.Error("MaterialReactor: Container is null, cannot save data.");
            return [];
        }

        var result = new List<ReactorEntry>();
        // {
        //     Tuple.Create("", reactorBattery.GetCharge())
        // };
        foreach (var reactant in currentEnergies)
            result.Add(new(reactant.Key.techType.AsString(), reactant.Value, maxEnergies[reactant.Key.techType]));

        foreach (var item in container)
        {
            if (currentEnergies.ContainsKey(item))
                continue;
            result.Add(new(item.techType.AsString(), 0f, maxEnergies[item.techType]));
        }

        return result;
    }

    private IEnumerator LoadSaveDict(IReadOnlyList<ReactorEntry> restoredContents)
    {
        if (restoredContents.Count == 0)
        {
            Log.Warn("MaterialReactor: No saved data found, skipping load.");
            yield break;
        }

        Dictionary<TechType, float> energyDict = new();

        foreach (var reactant in restoredContents)
            if (TechTypeExtensions.FromString(reactant.Type, out var techType, true))
            {
                Log.Write($"Adding {techType} to the reactor");
                energyDict[techType] = reactant.Energy;
                yield return MainPatcher.Instance.StartCoroutine(AddMaterial(techType));
            }

        Dictionary<InventoryItem, float> energyDict2 = new();

        foreach (var reactant in currentEnergies)
            if (energyDict.TryGetValue(reactant.Key.techType, out var energy))
                energyDict2[reactant.Key] = energy;

        foreach (var reactant in energyDict2)
        {
            currentEnergies[reactant.Key] = reactant.Value;
            Log.Write($"Restored {reactant.Key.techType} to {reactant.Value}");
        }

        Log.Write($"Energy dict: {energyDict2.Count} restored");
    }
}