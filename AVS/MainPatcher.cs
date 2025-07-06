using BepInEx;
using HarmonyLib;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AVS
{
    public abstract class MainPatcher : BaseUnityPlugin
    {
        private static MainPatcher? _instance;
        public static MainPatcher Instance => _instance ?? throw new InvalidOperationException("MainPatcher instance is not set. Ensure that the Awake method is called before accessing Instance.");


        public abstract string PluginId { get; }
        //internal static VFConfig VFConfig { get; private set; }
        //internal static AVSNautilusConfig NautilusConfig { get; private set; }

        internal Coroutine? GetVoices { get; private set; } = null;
        internal Coroutine? GetEngineSounds { get; private set; } = null;

        public virtual void Awake()
        {
            Nautilus.Handlers.LanguageHandler.RegisterLocalizationFolder();
            SetupInstance();
            //VFConfig = new VFConfig();
            //NautilusConfig = Nautilus.Handlers.OptionsPanelHandler.RegisterModOptions<AVSNautilusConfig>();
            AVS.Logger.Init(Logger);
            PrePatch();
            UWE.CoroutineHost.StartCoroutine(PrawnHelper.EnsurePrawn());
        }
        public virtual void Start()
        {
            Patch();
            PostPatch();
            CompatChecker.CheckAll();
            UWE.CoroutineHost.StartCoroutine(AVS.Logger.MakeAlerts());

        }

        public virtual void PrePatch()
        {
            AVS.Logger.Log("PrePatch started.");
            IEnumerator CollectPrefabsForBuilderReference()
            {
                CoroutineTask<GameObject> request = CraftData.GetPrefabForTechTypeAsync(TechType.BaseUpgradeConsole, true);
                yield return request;
                VehicleBuilder.UpgradeConsole = request.GetResult();
                yield break;
            }
            AVS.Logger.Log("CollectPrefabsForBuilderReference started.");
            UWE.CoroutineHost.StartCoroutine(CollectPrefabsForBuilderReference());
            AVS.Logger.Log("Assets.StaticAssets.GetSprites()");
            Assets.StaticAssets.GetSprites();
            AVS.Logger.Log("Assets.AVSFabricator.CreateAndRegister()");
            Assets.AVSFabricator.CreateAndRegister();
            AVS.Logger.Log("Admin.Utils.RegisterDepthModules()");
            Admin.CraftTreeHandler.AddFabricatorMenus();

            AVS.Logger.Log("Admin.Utils.RegisterDepthModules()");
            Admin.Utils.RegisterDepthModules();
            //AVS.Logger.Log("UWE.CoroutineHost.StartCoroutine(VoiceManager.LoadAllVoices())");
            //GetVoices = UWE.CoroutineHost.StartCoroutine(VoiceManager.LoadAllVoices());
            //AVS.Logger.Log("UWE.CoroutineHost.StartCoroutine(EngineSoundsManager.LoadAllVoices())");
            //GetEngineSounds = UWE.CoroutineHost.StartCoroutine(DynamicClipLoader.LoadAllVoices());
            AVS.Logger.Log("PrePatch finished.");
        }
        public virtual void Patch()
        {
            AVS.Logger.Log("Patch started.");
            AVS.Logger.Log("Nautilus.Handlers.SaveDataHandler.RegisterSaveDataCache<SaveLoad.SaveData>()");
            SaveLoad.SaveData saveData = Nautilus.Handlers.SaveDataHandler.RegisterSaveDataCache<SaveLoad.SaveData>();

            // Update the player position before saving it
            saveData.OnStartedSaving += (object sender, Nautilus.Json.JsonFileEventArgs e) =>
            {
                //try
                //{

                //    VehicleComponents.MagnetBoots.DetachAll();
                //}
                //catch (Exception ex)
                //{
                //    Eve.Logger.LogException("Failed to detach all magnet boots!", ex);
                //}
                //try
                //{
                //    VehicleManager.CreateSaveFileData(sender, e);
                //}
                //catch (Exception ex)
                //{
                //    Eve.Logger.LogException("Failed to Create Save File Data!", ex);
                //}
            };

            saveData.OnFinishedSaving += (object sender, Nautilus.Json.JsonFileEventArgs e) =>
            {
                //VehicleComponents.MagnetBoots.AttachAll();
            };

            saveData.OnFinishedLoading += (object sender, Nautilus.Json.JsonFileEventArgs e) =>
            {
                //SaveFileData = e.Instance as SaveLoad.SaveData;
            };

            void SetWorldNotLoaded()
            {
                Admin.GameStateWatcher.IsWorldLoaded = false;
                ModuleBuilder.haveWeCalledBuildAllSlots = false;
                ModuleBuilder.slotExtenderIsPatched = false;
                ModuleBuilder.slotExtenderHasGreenLight = false;
            }
            void SetWorldLoaded()
            {
                Admin.GameStateWatcher.IsWorldLoaded = true;
            }
            void OnLoadOnce()
            {

            }
            AVS.Logger.Log("Registering SaveUtils events.");
            Nautilus.Utility.SaveUtils.RegisterOnQuitEvent(SetWorldNotLoaded);
            Nautilus.Utility.SaveUtils.RegisterOnFinishLoadingEvent(SetWorldLoaded);
            Nautilus.Utility.SaveUtils.RegisterOneTimeUseOnLoadEvent(OnLoadOnce);

            AVS.Logger.Log("Patching...");
            var harmony = new Harmony(PluginId);
            harmony.PatchAll();

            // Patch SubnauticaMap with appropriate ping sprites, lest it crash.
            var type = Type.GetType("SubnauticaMap.PingMapIcon, SubnauticaMap", false, false);
            if (type != null)
            {
                var pingOriginal = AccessTools.Method(type, "Refresh");
                var pingPrefix = new HarmonyMethod(AccessTools.Method(typeof(Patches.CompatibilityPatches.MapModPatcher), "Prefix"));
                harmony.Patch(pingOriginal, pingPrefix);
            }

            // Patch SlotExtender, lest it break or break us
            var type2 = Type.GetType("SlotExtender.Patches.uGUI_Equipment_Awake_Patch, SlotExtender", false, false);
            if (type2 != null)
            {
                var awakePreOriginal = AccessTools.Method(type2, "Prefix");
                var awakePrefix = new HarmonyMethod(AccessTools.Method(typeof(Patches.CompatibilityPatches.SlotExtenderPatcher), "PrePrefix"));
                harmony.Patch(awakePreOriginal, awakePrefix);

                var awakePostOriginal = AccessTools.Method(type2, "Postfix");
                var awakePostfix = new HarmonyMethod(AccessTools.Method(typeof(Patches.CompatibilityPatches.SlotExtenderPatcher), "PrePostfix"));
                harmony.Patch(awakePostOriginal, awakePostfix);
            }

            // Patch BetterVehicleStorage to add ModVehicle compat
            var type3 = Type.GetType("BetterVehicleStorage.Managers.StorageModuleMgr, BetterVehicleStorage", false, false);
            if (type3 != null)
            {
                var AllowedToAddOriginal = AccessTools.Method(type3, "AllowedToAdd");
                var AllowedToAddPrefix = new HarmonyMethod(AccessTools.Method(typeof(Patches.CompatibilityPatches.BetterVehicleStoragePatcher), "Prefix"));
                harmony.Patch(AllowedToAddOriginal, AllowedToAddPrefix);
            }
            /*
            var type2 = Type.GetType("SlotExtender.Patches.uGUI_Equipment_Awake_Patch, SlotExtender", false, false);
            if (type2 != null)
            {
                // Example of assigning a static field in another mod
                var type3 = Type.GetType("SlotExtender.Main, SlotExtender", false, false);
                var but = AccessTools.StaticFieldRefAccess<bool>(type3, "uGUI_PostfixComplete");
                Logger.Log("but was " + but.ToString());
                but = false;
                // example of calling another mod's function
                var awakeOriginal = AccessTools.Method(type2, "Prefix");
                object dummyInstance = null;
                awakeOriginal.Invoke(dummyInstance, new object[] { equipment });
                //Patches.CompatibilityPatches.SlotExtenderPatcher.hasGreenLight = false;
            }
            */

            // do this here because it happens only once
            SceneManager.sceneUnloaded += Admin.GameStateWatcher.SignalSceneUnloaded;
        }
        public void PostPatch()
        {
            //VehicleBuilder.ScatterDataBoxes(craftables);
        }
        private void SetupInstance()
        {
            if (_instance == null)
            {
                _instance = this;
                return;
            }
            if (_instance != this)
            {
                UnityEngine.Object.Destroy(this);
                return;
            }
        }
    }
}
