using AVS.BaseVehicle;
using AVS.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AVS
{
    public class ModuleBuilder : MonoBehaviour
    {
        internal static ModuleBuilder? _main;
        public static ModuleBuilder Main => _main ?? throw new NullReferenceException("ModuleBuilder is not initialized. Ensure it is attached to a GameObject in the scene.");
        public static Dictionary<string, uGUI_EquipmentSlot> vehicleAllSlots = new Dictionary<string, uGUI_EquipmentSlot>();
        public const int MaxNumModules = 18;
        public bool isEquipmentInit = false;
        public bool areModulesReady = false;
        public static bool haveWeCalledBuildAllSlots = false;
        public static bool slotExtenderIsPatched = false;
        public static bool slotExtenderHasGreenLight = false;
        internal static string ModulePrefix => "AvsVehicleModule";

        public static bool IsModuleName(string name)
        {
            return name.Contains(ModulePrefix);
        }
        public static string ModuleName(int index)
        {
            return ModulePrefix + index;
        }

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
        public Material? armModuleSlotMaterial;

        public Transform? topLeftSlot = null;
        public Transform? bottomRightSlot = null;
        public Transform? leftArmSlot = null;

        private bool haveSlotsBeenInited = false;

        public void BuildAllSlots()
        {
            MainPatcher.Instance.StartCoroutine(BuildAllSlotsInternal());
        }
        public IEnumerator BuildAllSlotsInternal()
        {
            yield return new WaitUntil(() => haveSlotsBeenInited);
            if (!vehicleAllSlots.ContainsKey(ModuleName(0)))
            {
                var equipment = uGUI_PDA.main.transform
                    .Find("Content/InventoryTab/Equipment")
                    .SafeGetComponent<uGUI_Equipment>();
                if (equipment == null)
                {
                    Logger.Error("Failed to find Equipment in PDA. Cannot build vehicle module slots.");
                    yield break;
                }
                for (int i = 0; i < MaxNumModules; i++)
                {
                    var mod = equipment.transform.Find(ModuleName(i));
                    if (mod == null)
                    {
                        // If the slot does not exist, create it
                        Logger.Error("Missing vehicle module slot: " + ModuleName(i));
                        continue;
                    }
                    vehicleAllSlots.Add(ModuleName(i), mod.GetComponent<uGUI_EquipmentSlot>());
                }
            }
            else
            {
                var equipment = uGUI_PDA.main.transform
                    .Find("Content/InventoryTab/Equipment")
                    .SafeGetComponent<uGUI_Equipment>();
                if (equipment == null)
                {
                    Logger.Error("Failed to find Equipment in PDA. Cannot build vehicle module slots.");
                    yield break;
                }
                for (int i = 0; i < MaxNumModules; i++)
                {
                    var slot = equipment.transform
                        .Find(ModuleName(i))
                        .SafeGetComponent<uGUI_EquipmentSlot>(); ;
                    if (slot == null)
                    {
                        // If the slot does not exist, create it
                        Logger.Error("Missing vehicle module slot: " + ModuleName(i));
                        continue;
                    }
                    vehicleAllSlots[ModuleName(i)] = slot;
                }
            }

            // Now that we've gotten the data we need,
            // we can let slot extender mangle it
            var type2 = Type.GetType("SlotExtender.Patches.uGUI_Equipment_Awake_Patch, SlotExtender", false, false);
            if (type2 != null)
            {
                var equipment = uGUI_PDA.main.transform
                    .Find("Content/InventoryTab/Equipment")
                    .SafeGetComponent<uGUI_Equipment>();
                if (equipment == null)
                {
                    Logger.Error("Failed to find Equipment in PDA. Cannot build vehicle module slots.");
                    yield break;
                }
                ModuleBuilder.slotExtenderHasGreenLight = true;
                equipment.Awake();
            }
        }
        public void GrabComponents()
        {
            MainPatcher.Instance.StartCoroutine(BuildGenericModulesASAP());
        }
        private IEnumerator BuildGenericModulesASAP()
        {
            // this function is invoked by PDA.Awake,
            // so that we can access the same PDA here
            // Unfortunately this means we must wait for the player to open the PDA.
            // Maybe we can grab equipment from prefab?
            equipment = uGUI_PDA.main.transform
                .Find("Content/InventoryTab")
                .SafeGetComponentInChildren<uGUI_Equipment>(true);
            if (equipment == null)
            {
                Logger.Error("Failed to find Equipment in PDA. Cannot build vehicle module slots.");
                yield break;
            }

            yield return new WaitUntil(() => Main.isEquipmentInit);
            foreach (KeyValuePair<string, uGUI_EquipmentSlot> pair in vehicleAllSlots)
            {
                switch (pair.Key)
                {
                    case "ExosuitModule1":
                        {
                            // get slot location
                            topLeftSlot = pair.Value.transform;

                            //===============================================================================
                            // get generic module components
                            //===============================================================================
                            genericModuleObject = new GameObject("GenericVehicleModule");
                            genericModuleObject.SetActive(false);
                            genericModuleObject.transform.SetParent(equipment.transform, false);

                            // set module position
                            genericModuleObject.transform.localPosition = topLeftSlot.localPosition;

                            // add background child gameobject and components
                            var genericModuleBackground = new GameObject("Background");
                            genericModuleBackground.transform.SetParent(genericModuleObject.transform, false);
                            topLeftSlot.Find("Background").GetComponent<RectTransform>().TryCopyComponentWithFieldsTo(genericModuleBackground);
                            topLeftSlot.Find("Background").GetComponent<CanvasRenderer>().TryCopyComponentWithFieldsTo(genericModuleBackground);
                            topLeftSlot.Find("Background").GetComponent<UnityEngine.UI.Image>().TryCopyComponentWithFieldsTo(genericModuleBackground);

                            // save these I guess?
                            genericModuleSlotSprite = topLeftSlot.Find("Background").GetComponent<UnityEngine.UI.Image>().sprite;
                            genericModuleSlotMaterial = topLeftSlot.Find("Background").GetComponent<UnityEngine.UI.Image>().material;

                            // configure slot background image
                            genericModuleObject.EnsureComponent<uGUI_EquipmentSlot>().background = topLeftSlot.Find("Background").GetComponent<UnityEngine.UI.Image>();
                            genericModuleObject.GetComponent<uGUI_EquipmentSlot>().background.sprite = topLeftSlot.Find("Background").GetComponent<UnityEngine.UI.Image>().sprite;
                            genericModuleObject.GetComponent<uGUI_EquipmentSlot>().background.material = topLeftSlot.Find("Background").GetComponent<UnityEngine.UI.Image>().material;

                            // add iconrect child gameobject
                            genericModuleIconRect = new GameObject("IconRect");
                            genericModuleIconRect.transform.SetParent(genericModuleObject.transform, false);
                            genericModuleObject.GetComponent<uGUI_EquipmentSlot>().iconRect =
                                topLeftSlot.Find("IconRect").GetComponent<RectTransform>().TryCopyComponentWithFieldsTo(genericModuleIconRect);

                            //===============================================================================
                            // get background image components
                            //===============================================================================
                            modulesBackground = new GameObject("VehicleModuleBackground");
                            modulesBackground.SetActive(false);
                            topLeftSlot.Find("Exosuit").GetComponent<RectTransform>().TryCopyComponentWithFieldsTo(modulesBackground);
                            topLeftSlot.Find("Exosuit").GetComponent<CanvasRenderer>().TryCopyComponentWithFieldsTo(modulesBackground);
                            topLeftSlot.Find("Exosuit").GetComponent<UnityEngine.UI.Image>().TryCopyComponentWithFieldsTo(modulesBackground);
                            //backgroundSprite = Assets.SpriteHelper.GetSpriteRaw("Sprites/VFModuleBackground.png");
                            //backgroundSprite = topLeftSlot.Find("Exosuit").GetComponent<UnityEngine.UI.Image>().sprite;
                            modulesBackground.EnsureComponent<UnityEngine.UI.Image>().material = topLeftSlot.Find("Exosuit").GetComponent<UnityEngine.UI.Image>().material;
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
                            Transform arm = pair.Value.transform;

                            // adjust the module transform
                            armModuleObject.transform.localPosition = arm.localPosition;

                            // add background child gameobject and components
                            var genericModuleBackground = new GameObject("Background");
                            genericModuleBackground.transform.SetParent(armModuleObject.transform, false);

                            if (topLeftSlot == null)
                            {
                                Logger.Error("TopLeftSlot is null, cannot copy background components.");
                                yield break;
                            }
                            // configure background image
                            topLeftSlot.Find("Background").GetComponent<RectTransform>().TryCopyComponentWithFieldsTo(genericModuleBackground);
                            topLeftSlot.Find("Background").GetComponent<CanvasRenderer>().TryCopyComponentWithFieldsTo(genericModuleBackground);
                            topLeftSlot.Find("Background").GetComponent<UnityEngine.UI.Image>().TryCopyComponentWithFieldsTo(genericModuleBackground);

                            // add iconrect child gameobject
                            var thisModuleIconRect = new GameObject("IconRect");
                            thisModuleIconRect.transform.SetParent(armModuleObject.transform, false);
                            armModuleObject.EnsureComponent<uGUI_EquipmentSlot>().iconRect = topLeftSlot.Find("IconRect").GetComponent<RectTransform>().TryCopyComponentWithFieldsTo(thisModuleIconRect);

                            // add 'hints' to show which arm is which (left vs right)
                            leftArmModuleSlotSprite = arm.Find("Hint").GetComponent<UnityEngine.UI.Image>().sprite;
                            genericModuleHint = new GameObject("Hint");
                            genericModuleHint.transform.SetParent(armModuleObject.transform, false);
                            genericModuleHint.transform.localScale = new Vector3(.75f, .75f, .75f);
                            genericModuleHint.transform.localEulerAngles = new Vector3(0, 180, 0);
                            arm.Find("Hint").GetComponent<RectTransform>().TryCopyComponentWithFieldsTo(genericModuleHint);
                            arm.Find("Hint").GetComponent<CanvasRenderer>().TryCopyComponentWithFieldsTo(genericModuleHint);
                            arm.Find("Hint").GetComponent<UnityEngine.UI.Image>().TryCopyComponentWithFieldsTo(genericModuleHint);
                            rightArmModuleSlotSprite = arm.Find("Hint").GetComponent<UnityEngine.UI.Image>().sprite;
                            break;
                        }
                    default:
                        break;
                }
            }
            BuildVehicleModuleSlots(MaxNumModules);
            Main.areModulesReady = true;
            haveSlotsBeenInited = true;
        }
        public void BuildVehicleModuleSlots(int modules)
        {
            if (equipment == null)
            {
                Logger.Error("Equipment is null, cannot build vehicle module slots.");
                return;
            }
            // build, link, and position modules
            for (int i = 0; i < modules; i++)
            {
                var thisModule = GetGenericModuleSlot();
                if (thisModule == null)
                {
                    Logger.Error("Failed to get generic module slot for index: " + i);
                    continue;
                }
                thisModule.name = ModuleName(i);
                thisModule.SetActive(false);
                thisModule.transform.SetParent(equipment.transform, false);
                thisModule.transform.localScale = Vector3.one;
                thisModule.GetComponent<uGUI_EquipmentSlot>().slot = ModuleName(i);
                thisModule.GetComponent<uGUI_EquipmentSlot>().manager = equipment;

                LinkModule(ref thisModule);

                DistributeModule(ref thisModule, i);

                if (i == 0)
                {
                    AddBackgroundImage(ref thisModule);
                }
            }

        }
        public void LinkModule(ref GameObject thisModule)
        {
            // add background
            var backgroundTop = thisModule.transform.Find("Background").SafeGetGameObject();
            if (backgroundTop == null || genericModuleObject == null)
            {
                Logger.Error("Background or genericModuleObject is null, cannot link module.");
                return;
            }
            genericModuleObject.transform.Find("Background").GetComponent<RectTransform>().TryCopyComponentWithFieldsTo(backgroundTop);
            genericModuleObject.transform.Find("Background").GetComponent<CanvasRenderer>().TryCopyComponentWithFieldsTo(backgroundTop);
            thisModule.GetComponent<uGUI_EquipmentSlot>().background = genericModuleObject.transform.Find("Background").GetComponent<UnityEngine.UI.Image>().TryCopyComponentWithFieldsTo(backgroundTop);
            thisModule.GetComponent<uGUI_EquipmentSlot>().background.sprite = genericModuleSlotSprite;
            thisModule.GetComponent<uGUI_EquipmentSlot>().background.material = genericModuleSlotMaterial;
        }
        public void DistributeModule(ref GameObject thisModule, int position)
        {
            int row_size = 4;
            int arrayX = position % row_size;
            int arrayY = position / row_size;

            if (topLeftSlot == null || bottomRightSlot == null)
            {
                Logger.Error("TopLeftSlot or BottomRightSlot is null, cannot distribute module.");
                return;
            }

            float centerX = (topLeftSlot.localPosition.x + bottomRightSlot.localPosition.x) / 2;
            float centerY = (topLeftSlot.localPosition.y + bottomRightSlot.localPosition.y) / 2;

            float stepX = Mathf.Abs(topLeftSlot.localPosition.x - centerX);
            float stepY = Mathf.Abs(topLeftSlot.localPosition.y - centerY);

            Vector3 arrayOrigin = new Vector3(centerX - 2 * stepX, centerY - 2.5f * stepY, 0);

            float thisX = arrayOrigin.x + arrayX * stepX;
            float thisY = arrayOrigin.y + arrayY * stepY;

            thisModule.transform.localPosition = new Vector3(thisX, thisY, 0);
        }
        public void AddBackgroundImage(ref GameObject parent)
        {
            if (modulesBackground == null)
            {
                Logger.Error("ModulesBackground is null, cannot add background image.");
                return;
            }
            GameObject thisBackground = GameObject.Instantiate(modulesBackground);
            thisBackground.transform.SetParent(parent.transform);
            thisBackground.transform.localRotation = Quaternion.identity;
            thisBackground.transform.localPosition = new Vector3(250, 250, 0);
            thisBackground.transform.localScale = 5 * Vector3.one;
            thisBackground.EnsureComponent<UnityEngine.UI.Image>().sprite = MainPatcher.Instance.Images.ModulesBackground;
        }

        public GameObject? GetGenericModuleSlot()
        {
            if (genericModuleObject == null)
            {
                Logger.Error("Generic module object is null, cannot get generic module slot.");
                return null;
            }
            return GameObject.Instantiate(genericModuleObject);
        }

        private bool haveFixed = false;

        internal void SignalOpened(VehicleUpgradeConsoleInput instance, AvsVehicle mv)
        {
            Sprite? setSprite;
            if (mv.Config.ModuleBackgroundImage == null)
            {
                setSprite = MainPatcher.Instance.Images.ModulesBackground;
            }
            else
            {
                setSprite = mv.Config.ModuleBackgroundImage;
            }
            if (equipment != null)
            {
                var img = equipment.transform
                    .Find(ModuleBuilder.ModuleName(0) + "/VehicleModuleBackground(Clone)")
                    .SafeGetComponent<UnityEngine.UI.Image>();
                if (img != null)
                {
                    img.sprite = setSprite;
                    mv.Log.Tag("ModuleBuilder").Write($"Background set to {setSprite.NiceName()}");
                }
                else
                    mv.Log.Tag("ModuleBuilder").Error("Failed to set background sprite, Image component not found.");
            }
            else
                mv.Log.Tag("ModuleBuilder").Error("Equipment is null, cannot set background sprite.");
            if (!haveFixed)
            {
                mv.StartCoroutine(FixModules(instance, mv));
            }
        }

        private IEnumerator FixModules(VehicleUpgradeConsoleInput instance, AvsVehicle mv)
        {
            var pda = Player.main.GetPDA();
            mv.Log.Tag("ModuleBuilder").Write("Fixing modules...");
            yield return new WaitForSeconds(1);
            {
                mv.Log.Tag("ModuleBuilder").Write("PDA still open. Closing, reopening");
                pda.Close();
                pda.isInUse = false;
                //yield return new WaitForEndOfFrame();
                instance.OpenPDA();
                haveFixed = true;
            }
            //else
            //    mv.Log.Tag("ModuleBuilder").Write("PDA is not open, no need to close and reopen.");
        }
    }
}
