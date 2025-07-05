using AVS.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AVS
{
    public class ModuleBuilder : MonoBehaviour
    {
        public static ModuleBuilder main;
        public static Dictionary<string, uGUI_EquipmentSlot> vehicleAllSlots = new Dictionary<string, uGUI_EquipmentSlot>();
        public const int MaxNumModules = 18;
        public bool isEquipmentInit = false;
        public bool areModulesReady = false;
        public static bool haveWeCalledBuildAllSlots = false;
        public static bool slotExtenderIsPatched = false;
        public static bool slotExtenderHasGreenLight = false;
        internal const string ModulePrefix = "AvsVehicleModule";

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
            main = this;
        }

        public uGUI_Equipment equipment;

        public GameObject genericModuleObject; // parent object of the regular module slot
        public GameObject armModuleObject; // parent object of the arm module slot
        public GameObject genericModuleIconRect;
        public GameObject genericModuleHint;

        public GameObject modulesBackground; // background image parent object
        public Sprite BackgroundSprite
        {
            set
            {
                Sprite setSprite;
                if (value == null)
                {
                    setSprite = Assets.SpriteHelper.GetSpriteRaw("Sprites/VFModuleBackground.png");
                }
                else
                {
                    setSprite = value;
                }
                equipment.transform.Find(ModuleBuilder.ModuleName(0) + "/VehicleModuleBackground(Clone)").GetComponent<UnityEngine.UI.Image>().sprite = setSprite;
            }
        }

        public Sprite genericModuleSlotSprite;
        public Sprite leftArmModuleSlotSprite;
        public Sprite rightArmModuleSlotSprite;

        // These two materials might be the same
        public Material genericModuleSlotMaterial;
        public Material armModuleSlotMaterial;

        public Transform topLeftSlot = null;
        public Transform bottomRightSlot = null;
        public Transform leftArmSlot = null;

        private bool haveSlotsBeenInited = false;

        public void BuildAllSlots()
        {
            UWE.CoroutineHost.StartCoroutine(BuildAllSlotsInternal());
        }
        public IEnumerator BuildAllSlotsInternal()
        {
            yield return new WaitUntil(() => haveSlotsBeenInited);
            if (!vehicleAllSlots.ContainsKey(ModuleName(0)))
            {
                uGUI_Equipment equipment = uGUI_PDA.main.transform.Find("Content/InventoryTab/Equipment")?.GetComponent<uGUI_Equipment>();
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
                uGUI_Equipment equipment = uGUI_PDA.main.transform.Find("Content/InventoryTab/Equipment")?.GetComponent<uGUI_Equipment>();
                for (int i = 0; i < MaxNumModules; i++)
                {
                    vehicleAllSlots[ModuleName(i)] = equipment.transform.Find(ModuleName(i)).GetComponent<uGUI_EquipmentSlot>();
                }
            }

            // Now that we've gotten the data we need,
            // we can let slot extender mangle it
            var type2 = Type.GetType("SlotExtender.Patches.uGUI_Equipment_Awake_Patch, SlotExtender", false, false);
            if (type2 != null)
            {
                uGUI_Equipment equipment = uGUI_PDA.main.transform.Find("Content/InventoryTab/Equipment")?.GetComponent<uGUI_Equipment>();
                ModuleBuilder.slotExtenderHasGreenLight = true;
                equipment.Awake();
            }
        }
        public void grabComponents()
        {
            UWE.CoroutineHost.StartCoroutine(BuildGenericModulesASAP());
        }
        private IEnumerator BuildGenericModulesASAP()
        {
            // this function is invoked by PDA.Awake,
            // so that we can access the same PDA here
            // Unfortunately this means we must wait for the player to open the PDA.
            // Maybe we can grab equipment from prefab?
            equipment = uGUI_PDA.main.transform.Find("Content/InventoryTab").GetComponentInChildren<uGUI_Equipment>(true);
            yield return new WaitUntil(() => main.isEquipmentInit);
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
                            topLeftSlot.Find("Background").GetComponent<RectTransform>().CopyComponentWithFieldsTo(genericModuleBackground);
                            topLeftSlot.Find("Background").GetComponent<CanvasRenderer>().CopyComponentWithFieldsTo(genericModuleBackground);
                            topLeftSlot.Find("Background").GetComponent<UnityEngine.UI.Image>().CopyComponentWithFieldsTo(genericModuleBackground);

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
                                topLeftSlot.Find("IconRect").GetComponent<RectTransform>().CopyComponentWithFieldsTo(genericModuleIconRect);

                            //===============================================================================
                            // get background image components
                            //===============================================================================
                            modulesBackground = new GameObject("VehicleModuleBackground");
                            modulesBackground.SetActive(false);
                            topLeftSlot.Find("Exosuit").GetComponent<RectTransform>().CopyComponentWithFieldsTo(modulesBackground);
                            topLeftSlot.Find("Exosuit").GetComponent<CanvasRenderer>().CopyComponentWithFieldsTo(modulesBackground);
                            topLeftSlot.Find("Exosuit").GetComponent<UnityEngine.UI.Image>().CopyComponentWithFieldsTo(modulesBackground);
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

                            // configure background image
                            topLeftSlot.Find("Background").GetComponent<RectTransform>().CopyComponentWithFieldsTo(genericModuleBackground);
                            topLeftSlot.Find("Background").GetComponent<CanvasRenderer>().CopyComponentWithFieldsTo(genericModuleBackground);
                            topLeftSlot.Find("Background").GetComponent<UnityEngine.UI.Image>().CopyComponentWithFieldsTo(genericModuleBackground);

                            // add iconrect child gameobject
                            var thisModuleIconRect = new GameObject("IconRect");
                            thisModuleIconRect.transform.SetParent(armModuleObject.transform, false);
                            armModuleObject.EnsureComponent<uGUI_EquipmentSlot>().iconRect = topLeftSlot.Find("IconRect").GetComponent<RectTransform>().CopyComponentWithFieldsTo(thisModuleIconRect);

                            // add 'hints' to show which arm is which (left vs right)
                            leftArmModuleSlotSprite = arm.Find("Hint").GetComponent<UnityEngine.UI.Image>().sprite;
                            genericModuleHint = new GameObject("Hint");
                            genericModuleHint.transform.SetParent(armModuleObject.transform, false);
                            genericModuleHint.transform.localScale = new Vector3(.75f, .75f, .75f);
                            genericModuleHint.transform.localEulerAngles = new Vector3(0, 180, 0);
                            arm.Find("Hint").GetComponent<RectTransform>().CopyComponentWithFieldsTo(genericModuleHint);
                            arm.Find("Hint").GetComponent<CanvasRenderer>().CopyComponentWithFieldsTo(genericModuleHint);
                            arm.Find("Hint").GetComponent<UnityEngine.UI.Image>().CopyComponentWithFieldsTo(genericModuleHint);
                            rightArmModuleSlotSprite = arm.Find("Hint").GetComponent<UnityEngine.UI.Image>().sprite;
                            break;
                        }
                    default:
                        break;
                }
            }
            BuildVehicleModuleSlots(MaxNumModules);
            main.areModulesReady = true;
            haveSlotsBeenInited = true;
        }
        public void BuildVehicleModuleSlots(int modules)
        {
            // build, link, and position modules
            for (int i = 0; i < modules; i++)
            {
                GameObject thisModule = GetGenericModuleSlot();
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
            GameObject backgroundTop = thisModule.transform.Find("Background").gameObject;
            genericModuleObject.transform.Find("Background").GetComponent<RectTransform>().CopyComponentWithFieldsTo(backgroundTop);
            genericModuleObject.transform.Find("Background").GetComponent<CanvasRenderer>().CopyComponentWithFieldsTo(backgroundTop);
            thisModule.GetComponent<uGUI_EquipmentSlot>().background = genericModuleObject.transform.Find("Background").GetComponent<UnityEngine.UI.Image>().CopyComponentWithFieldsTo(backgroundTop);
            thisModule.GetComponent<uGUI_EquipmentSlot>().background.sprite = genericModuleSlotSprite;
            thisModule.GetComponent<uGUI_EquipmentSlot>().background.material = genericModuleSlotMaterial;
        }
        public void DistributeModule(ref GameObject thisModule, int position)
        {
            int row_size = 4;
            int arrayX = position % row_size;
            int arrayY = position / row_size;

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
            GameObject thisBackground = GameObject.Instantiate(modulesBackground);
            thisBackground.transform.SetParent(parent.transform);
            thisBackground.transform.localRotation = Quaternion.identity;
            thisBackground.transform.localPosition = new Vector3(250, 250, 0);
            thisBackground.transform.localScale = 5 * Vector3.one;
            thisBackground.EnsureComponent<UnityEngine.UI.Image>().sprite = Assets.SpriteHelper.GetSpriteRaw("Sprites/VFModuleBackground.png");
        }
        public void LinkArm(ref GameObject thisModule)
        {
            // add background
            GameObject backgroundTop = thisModule.transform.Find("Background").gameObject;
            genericModuleObject.transform.Find("Background").GetComponent<RectTransform>().CopyComponentWithFieldsTo(backgroundTop);
            genericModuleObject.transform.Find("Background").GetComponent<CanvasRenderer>().CopyComponentWithFieldsTo(backgroundTop);
            thisModule.GetComponent<uGUI_EquipmentSlot>().background = genericModuleObject.transform.Find("Background").GetComponent<UnityEngine.UI.Image>().CopyComponentWithFieldsTo(backgroundTop);
            thisModule.GetComponent<uGUI_EquipmentSlot>().background.sprite = genericModuleSlotSprite;
            thisModule.GetComponent<uGUI_EquipmentSlot>().background.material = genericModuleSlotMaterial;
        }
        public GameObject GetGenericModuleSlot()
        {
            return GameObject.Instantiate(genericModuleObject);
        }

    }
}
