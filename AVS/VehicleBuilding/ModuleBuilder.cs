using AVS.BaseVehicle;
using AVS.Log;
using AVS.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AVS.VehicleBuilding;

internal class ModuleBuilder : MonoBehaviour
{
    internal static ModuleBuilder? _main;

    public static ModuleBuilder Main => _main ??
                                        throw new NullReferenceException(
                                            "ModuleBuilder is not initialized. Ensure it is attached to a GameObject in the scene.");

    public static Dictionary<string, uGUI_EquipmentSlot> AllVehicleSlots { get; } = new();
    public const int MaxNumModules = 18;
    public bool isEquipmentInit = false;
    public bool areModulesReady = false;
    public static bool haveWeCalledBuildAllSlots = false;
    public static bool slotExtenderIsPatched = false;
    public static bool SlotExtenderHasGreenLight { get; set; } = false;
    internal static string GetModulePrefix(RootModController rmc) => rmc.ModName + "_Avs_Vehicle_Module";

    public static bool IsModuleName(RootModController rmc, string name)
    {
        var prefix = GetModulePrefix(rmc);
        return name.StartsWith(prefix) && int.TryParse(name.Substring(prefix.Length), out _);
    }

    public static string ModuleName(RootModController rmc, int index) => GetModulePrefix(rmc) + index;

    public void Awake()
    {
        _main = this;
    }

    public uGUI_Equipment? equipment;

    public GameObject? genericModuleObject; // parent object of the regular module slot
    public GameObject? armModuleObject; // parent object of the arm module slot
    public GameObject? genericModuleIconRect;
    public GameObject? genericModuleHint;

    public GameObject? modulesBackground; // background image parent object


    public Sprite? genericModuleSlotSprite;
    public Sprite? leftArmModuleSlotSprite;
    public Sprite? rightArmModuleSlotSprite;

    // These two materials might be the same
    public Material? genericModuleSlotMaterial;

    public Transform? topLeftSlot = null;
    public Transform? bottomRightSlot = null;
    public Transform? leftArmSlot = null;

    private bool haveSlotsBeenInited = false;
    private static LogWriter Log { get; } = LogWriter.Default.Tag(nameof(ModuleBuilder));

    public void BuildAllSlots()
    {
        RootModController.AnyInstance.StartAvsCoroutine(
            nameof(ModuleBuilder) + '.' + nameof(BuildAllSlotsInternal),
            BuildAllSlotsInternal);
    }

    public static void LoadVehicleSlots(Dictionary<string, uGUI_EquipmentSlot> sourceSlots,
        bool clearExisting = true)
    {
        using var log = SmartLog.ForAVS(RootModController.AnyInstance);
        if (clearExisting)
        {
            log.Write($"Clearing existing vehicle slots...");
            AllVehicleSlots.Clear();
        }

        var added = 0;
        foreach (var pair in sourceSlots)
            if (!AllVehicleSlots.ContainsKey(pair.Key))
            {
                AllVehicleSlots.Add(pair.Key, pair.Value);
                //Log.Write($"Loaded slot {pair.Key}: {pair.Value.NiceName()}");
                added++;
            }

        if (added > 0)
            log.Write($"Loaded {added} new vehicle slots.");
    }

    public IEnumerator BuildAllSlotsInternal(SmartLog log)
    {
        log.Write($"Waiting for PDA to be initialized...");
        yield return new WaitUntil(() => haveSlotsBeenInited);

        var eq = uGUI_PDA.main.transform
            .Find("Content/InventoryTab/Equipment")
            .SafeGetComponent<uGUI_Equipment>();
        if (eq.IsNull())
        {
            log.Error("Failed to find Equipment in PDA. Cannot build vehicle module slots.");
            yield break;
        }

        // Log.Write(
        //     $"Equipment found. Children are: {string.Join(", ", eq.transform.SafeGetChildren().Select(x => x.NiceName()))}");

        foreach (var rmc in RootModController.AllInstances)
        {
            log.Write($"Building vehicle module slots for {rmc.ModName}...");

            if (!AllVehicleSlots.ContainsKey(ModuleName(rmc, 0)))
            {
                log.Write($"Slots have not previously been mapped (currently: {AllVehicleSlots.Count})");

                for (var i = 0; i < MaxNumModules; i++)
                {
                    var slotName = ModuleName(rmc, i);
                    var mod = eq.transform.Find(slotName);
                    if (mod.IsNull())
                    {
                        // If the slot does not exist, create it
                        log.Error("Missing vehicle module slot: " + slotName);
                        continue;
                    }

                    var slot = mod.GetComponent<uGUI_EquipmentSlot>();
                    //Log.Write($"Mapping slot {slotName}: {slot.NiceName()}");

                    AllVehicleSlots.Add(slotName, slot);
                }
            }
            else
            {
                log.Write($"Slots have previously been mapped. Updating...");

                for (var i = 0; i < MaxNumModules; i++)
                {
                    var slotName = ModuleName(rmc, i);
                    var slot = eq.transform
                        .Find(slotName)
                        .SafeGetComponent<uGUI_EquipmentSlot>();
                    ;
                    if (slot.IsNull())
                    {
                        // If the slot does not exist, create it
                        log.Error("Missing vehicle module slot: " + slotName);
                        continue;
                    }

                    //Log.Write($"Mapping slot {slotName}: {slot.NiceName()}");
                    AllVehicleSlots[slotName] = slot;
                }
            }
        }

        // Now that we've gotten the data we need,
        // we can let slot extender mangle it
        var type2 = Type.GetType("SlotExtender.Patches.uGUI_Equipment_Awake_Patch, SlotExtender", false, false);
        if (type2.IsNotNull())
        {
            SlotExtenderHasGreenLight = true;
            eq.Awake();
        }
    }

    public void GrabComponents()
    {
        RootModController.AnyInstance.StartAvsCoroutine(
            nameof(ModuleBuilder) + '.' + nameof(BuildGenericModulesASAP),
            BuildGenericModulesASAP);
    }

    private IEnumerator BuildGenericModulesASAP(SmartLog log)
    {
        log.Write($"Begin");
        // this function is invoked by PDA.Awake,
        // so that we can access the same PDA here
        // Unfortunately this means we must wait for the player to open the PDA.
        // Maybe we can grab equipment from prefab?
        equipment = uGUI_PDA.main.transform
            .Find("Content/InventoryTab")
            .SafeGetComponentInChildren<uGUI_Equipment>(true);
        if (equipment.IsNull())
        {
            log.Error("Failed to find Equipment in PDA. Cannot build vehicle module slots.");
            yield break;
        }

        log.Write($"Waiting for PDA to be initialized...");
        yield return new WaitUntil(() => Main.isEquipmentInit);
        foreach (var pair in AllVehicleSlots)
            //log.Write($"Processing slot {pair.Key}: {pair.Value.NiceName()}");
            switch (pair.Key)
            {
                case "ExosuitModule1":
                    {
                        // get slot location
                        topLeftSlot = pair.Value.transform;

                        //===============================================================================
                        // get generic module components
                        //===============================================================================
                        genericModuleObject = new GameObject("AvsGenericVehicleModule");
                        genericModuleObject.SetActive(false);
                        genericModuleObject.transform.SetParent(equipment.transform, false);

                        // set module position
                        genericModuleObject.transform.localPosition = topLeftSlot.localPosition;

                        // add background child gameobject and components
                        var genericModuleBackground = new GameObject("Background");
                        genericModuleBackground.transform.SetParent(genericModuleObject.transform, false);
                        topLeftSlot.Find("Background").GetComponent<RectTransform>()
                            .TryCopyComponentWithFieldsTo(genericModuleBackground);
                        topLeftSlot.Find("Background").GetComponent<CanvasRenderer>()
                            .TryCopyComponentWithFieldsTo(genericModuleBackground);
                        topLeftSlot.Find("Background").GetComponent<UnityEngine.UI.Image>()
                            .TryCopyComponentWithFieldsTo(genericModuleBackground);

                        // save these I guess?
                        genericModuleSlotSprite =
                            topLeftSlot.Find("Background").GetComponent<UnityEngine.UI.Image>().sprite;
                        genericModuleSlotMaterial =
                            topLeftSlot.Find("Background").GetComponent<UnityEngine.UI.Image>().material;

                        // configure slot background image
                        genericModuleObject.EnsureComponent<uGUI_EquipmentSlot>().background =
                            topLeftSlot.Find("Background").GetComponent<UnityEngine.UI.Image>();
                        genericModuleObject.GetComponent<uGUI_EquipmentSlot>().background.sprite =
                            topLeftSlot.Find("Background").GetComponent<UnityEngine.UI.Image>().sprite;
                        genericModuleObject.GetComponent<uGUI_EquipmentSlot>().background.material =
                            topLeftSlot.Find("Background").GetComponent<UnityEngine.UI.Image>().material;

                        // add iconrect child gameobject
                        genericModuleIconRect = new GameObject("IconRect");
                        genericModuleIconRect.transform.SetParent(genericModuleObject.transform, false);
                        genericModuleObject.GetComponent<uGUI_EquipmentSlot>().iconRect =
                            topLeftSlot.Find("IconRect").GetComponent<RectTransform>()
                                .TryCopyComponentWithFieldsTo(genericModuleIconRect);

                        //===============================================================================
                        // get background image components
                        //===============================================================================
                        modulesBackground = new GameObject("VehicleModuleBackground");
                        modulesBackground.SetActive(false);
                        topLeftSlot.Find("Exosuit").GetComponent<RectTransform>()
                            .TryCopyComponentWithFieldsTo(modulesBackground);
                        topLeftSlot.Find("Exosuit").GetComponent<CanvasRenderer>()
                            .TryCopyComponentWithFieldsTo(modulesBackground);
                        topLeftSlot.Find("Exosuit").GetComponent<UnityEngine.UI.Image>()
                            .TryCopyComponentWithFieldsTo(modulesBackground);
                        //backgroundSprite = Assets.SpriteHelper.GetSpriteRaw("Sprites/VFModuleBackground.png");
                        //backgroundSprite = topLeftSlot.Find("Exosuit").GetComponent<UnityEngine.UI.Image>().sprite;
                        modulesBackground.EnsureComponent<UnityEngine.UI.Image>().material =
                            topLeftSlot.Find("Exosuit").GetComponent<UnityEngine.UI.Image>().material;
                        // this can remain active, because its parent's Activity is controlled
                        modulesBackground.SetActive(true);
                        break;
                    }
                case "ExosuitModule4":
                    // get slot location
                    bottomRightSlot = pair.Value.transform;
                    break;
                case "ExosuitArmLeft":
                    {
                        // get slot location
                        leftArmSlot = pair.Value.transform;
                        armModuleObject = new GameObject("ArmVehicleModule");
                        armModuleObject.SetActive(false);
                        var arm = pair.Value.transform;

                        // adjust the module transform
                        armModuleObject.transform.localPosition = arm.localPosition;

                        // add background child gameobject and components
                        var genericModuleBackground = new GameObject("Background");
                        genericModuleBackground.transform.SetParent(armModuleObject.transform, false);

                        if (topLeftSlot.IsNull())
                        {
                            Logger.Error("TopLeftSlot is null, cannot copy background components.");
                            yield break;
                        }

                        // configure background image
                        topLeftSlot.Find("Background").GetComponent<RectTransform>()
                            .TryCopyComponentWithFieldsTo(genericModuleBackground);
                        topLeftSlot.Find("Background").GetComponent<CanvasRenderer>()
                            .TryCopyComponentWithFieldsTo(genericModuleBackground);
                        topLeftSlot.Find("Background").GetComponent<UnityEngine.UI.Image>()
                            .TryCopyComponentWithFieldsTo(genericModuleBackground);

                        // add iconrect child gameobject
                        var thisModuleIconRect = new GameObject("IconRect");
                        thisModuleIconRect.transform.SetParent(armModuleObject.transform, false);
                        armModuleObject.EnsureComponent<uGUI_EquipmentSlot>().iconRect = topLeftSlot.Find("IconRect")
                            .GetComponent<RectTransform>().TryCopyComponentWithFieldsTo(thisModuleIconRect);

                        // add 'hints' to show which arm is which (left vs right)
                        leftArmModuleSlotSprite = arm.Find("Hint").GetComponent<UnityEngine.UI.Image>().sprite;
                        genericModuleHint = new GameObject("Hint");
                        genericModuleHint.transform.SetParent(armModuleObject.transform, false);
                        genericModuleHint.transform.localScale = new Vector3(.75f, .75f, .75f);
                        genericModuleHint.transform.localEulerAngles = new Vector3(0, 180, 0);
                        arm.Find("Hint").GetComponent<RectTransform>().TryCopyComponentWithFieldsTo(genericModuleHint);
                        arm.Find("Hint").GetComponent<CanvasRenderer>().TryCopyComponentWithFieldsTo(genericModuleHint);
                        arm.Find("Hint").GetComponent<UnityEngine.UI.Image>()
                            .TryCopyComponentWithFieldsTo(genericModuleHint);
                        rightArmModuleSlotSprite = arm.Find("Hint").GetComponent<UnityEngine.UI.Image>().sprite;
                        break;
                    }
                default:
                    break;
            }

        foreach (var rmc in RootModController.AllInstances)
            BuildVehicleModuleSlots(rmc, MaxNumModules);
        Main.areModulesReady = true;
        haveSlotsBeenInited = true;
    }

    public void BuildVehicleModuleSlots(RootModController rmc, int modules)
    {
        using var log = SmartLog.ForAVS(rmc);
        log.Write(nameof(BuildVehicleModuleSlots) + $" ({modules}, {rmc.ModName}) called.");
        if (equipment.IsNull())
        {
            log.Error("Equipment is null, cannot build vehicle module slots.");
            return;
        }

        // build, link, and position modules
        for (var i = 0; i < modules; i++)
        {
            var thisModule = InstantiateGenericModuleSlot();
            if (thisModule.IsNull())
            {
                log.Error("Failed to get generic module slot for index: " + i);
                continue;
            }

            thisModule.name = ModuleName(rmc, i);
            thisModule.SetActive(false);
            thisModule.transform.SetParent(equipment.transform, false);
            thisModule.transform.localScale = Vector3.one;
            thisModule.GetComponent<uGUI_EquipmentSlot>().slot = ModuleName(rmc, i);
            thisModule.GetComponent<uGUI_EquipmentSlot>().manager = equipment;

            LinkModule(rmc, ref thisModule);

            DistributeModule(rmc, ref thisModule, i);

            if (i == 0)
                AddBackgroundImage(rmc, ref thisModule);
        }
    }

    public void LinkModule(RootModController rmc, ref GameObject thisModule)
    {
        using var log = SmartLog.ForAVS(rmc);
        //log.Write(nameof(LinkModule) + $" ({thisModule.NiceName()}) called.");
        // add background
        var backgroundTop = thisModule.transform.Find("Background").SafeGetGameObject();
        if (backgroundTop.IsNull() || genericModuleObject.IsNull())
        {
            log.Error("Background or genericModuleObject is null, cannot link module.");
            return;
        }

        genericModuleObject.transform.Find("Background").GetComponent<RectTransform>()
            .TryCopyComponentWithFieldsTo(backgroundTop);
        genericModuleObject.transform.Find("Background").GetComponent<CanvasRenderer>()
            .TryCopyComponentWithFieldsTo(backgroundTop);
        thisModule.GetComponent<uGUI_EquipmentSlot>().background = genericModuleObject.transform.Find("Background")
            .GetComponent<UnityEngine.UI.Image>().TryCopyComponentWithFieldsTo(backgroundTop);
        thisModule.GetComponent<uGUI_EquipmentSlot>().background.sprite = genericModuleSlotSprite;
        thisModule.GetComponent<uGUI_EquipmentSlot>().background.material = genericModuleSlotMaterial;
    }

    public void DistributeModule(RootModController rmc, ref GameObject thisModule, int position)
    {
        using var log = SmartLog.ForAVS(rmc);

        log.Debug(nameof(DistributeModule) + $" ({thisModule.NiceName()}, {position}) called.");
        var row_size = 4;
        var arrayX = position % row_size;
        var arrayY = position / row_size;

        if (topLeftSlot.IsNull() || bottomRightSlot.IsNull())
        {
            log.Error("TopLeftSlot or BottomRightSlot is null, cannot distribute module.");
            return;
        }

        var centerX = (topLeftSlot.localPosition.x + bottomRightSlot.localPosition.x) / 2;
        var centerY = (topLeftSlot.localPosition.y + bottomRightSlot.localPosition.y) / 2;

        var stepX = Mathf.Abs(topLeftSlot.localPosition.x - centerX);
        var stepY = Mathf.Abs(topLeftSlot.localPosition.y - centerY);

        var arrayOrigin = new Vector3(centerX - 2 * stepX, centerY - 2.5f * stepY, 0);

        var thisX = arrayOrigin.x + arrayX * stepX;
        var thisY = arrayOrigin.y + arrayY * stepY;

        thisModule.transform.localPosition = new Vector3(thisX, thisY, 0);
    }

    public void AddBackgroundImage(RootModController rmc, ref GameObject parent)
    {
        using var log = SmartLog.ForAVS(rmc);
        log.Write(nameof(AddBackgroundImage) + $" ({parent.NiceName()}) called.");
        if (modulesBackground.IsNull())
        {
            log.Error("ModulesBackground is null, cannot add background image.");
            return;
        }

        var thisBackground = Instantiate(modulesBackground, parent.transform);
        thisBackground.transform.localRotation = Quaternion.identity;
        thisBackground.transform.localPosition = new Vector3(250, 250, 0);
        thisBackground.transform.localScale = 5 * Vector3.one;
        thisBackground.EnsureComponent<UnityEngine.UI.Image>().sprite = rmc.Images.ModulesBackground;
    }

    public GameObject? InstantiateGenericModuleSlot()
    {
        if (genericModuleObject.IsNull())
        {
            Log.Error("Generic module object is null, cannot get generic module slot.");
            return null;
        }

        return Instantiate(genericModuleObject);
    }

    private bool haveFixed = false;

    internal void SignalOpened(VehicleUpgradeConsoleInput instance, AvsVehicle av)
    {
        using var log = av.NewAvsLog();
        Sprite? setSprite;
        var rmc = av.Owner;
        setSprite = av.Config.ModuleBackgroundImage.OrRequired(rmc.Images.ModulesBackground);
        if (equipment.IsNotNull())
        {
            var img = equipment.transform
                .Find(ModuleName(rmc, 0) + "/VehicleModuleBackground(Clone)")
                .SafeGetComponent<UnityEngine.UI.Image>();
            if (img.IsNotNull())
            {
                img.sprite = setSprite;
                log.Write($"Background set to {setSprite.NiceName()}");
            }
            else
            {
                log.Error("Failed to set background sprite, Image component not found.");
            }
        }
        else
        {
            log.Error("Equipment is null, cannot set background sprite.");
        }


        if (!haveFixed)
            av.Owner.StartAvsCoroutine(
                nameof(ModuleBuilder) + '.' + nameof(FixModules),
                log => FixModules(log, instance, av));
    }

    private IEnumerator FixModules(SmartLog log, VehicleUpgradeConsoleInput instance, AvsVehicle av)
    {
        var pda = Player.main.GetPDA();
        log.Write("Fixing modules in one second...");
        yield return new WaitForSeconds(1);
        {
            log.Write("PDA still open. Closing, reopening");
            pda.Close();
            pda.isInUse = false;
            //yield return new WaitForEndOfFrame();
            instance.OpenPDA();
            haveFixed = true;
        }
        //else
        //    av.Log.Tag("ModuleBuilder").Write("PDA is not open, no need to close and reopen.");
    }
}