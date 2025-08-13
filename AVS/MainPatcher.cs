using AVS.Assets;
using AVS.Log;
using BepInEx;
using HarmonyLib;
using Nautilus.Handlers;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AVS
{
    /// <summary>
    /// AVS main patcher class. Must be inherited by the main mod class.
    /// </summary>
    public abstract class MainPatcher : BaseUnityPlugin
    {
        private static MainPatcher? _instance;
        private PatcherImages? images;

        /// <summary>
        /// Loaded patcher images. Available after <see cref="LoadImages"/> is called in <see cref="Awake"/>.
        /// </summary>
        public PatcherImages Images
        {
            get
            {
                if (images == null)
                {
                    throw new InvalidOperationException("PatcherImages is not initialized. Ensure that LoadImages() is called before accessing this property.");
                }
                return images;
            }
        }

        /// <summary>
        /// The icon for the Depth Module 1 upgrade.
        /// </summary>
        public Sprite DepthModule1Icon => Images.DepthModule1Icon.Sprite;
        /// <summary>
        /// The icon for the Depth Module 2 upgrade.
        /// </summary>
        public Sprite DepthModule2Icon => Images.DepthModule2Icon.Sprite;
        /// <summary>
        /// The icon for the Depth Module 3 upgrade.
        /// </summary>
        public Sprite DepthModule3Icon => Images.DepthModule3Icon.Sprite;
        /// <summary>
        /// The icon to use for the parent node of all depth modules in the crafting tree.
        /// </summary>
        public Sprite DepthModuleNodeIcon => Images.DepthModuleNodeIcon.Sprite;

        /// <summary>
        /// Queries the main singleton instance of <see cref="MainPatcher"/>.
        /// </summary>
        public static MainPatcher Instance => _instance ?? throw new InvalidOperationException("MainPatcher instance is not set. Ensure that the Awake method is called before accessing Instance.");

        /// <summary>
        /// Loads the images used by AVS.
        /// </summary>
        /// <returns></returns>
        protected abstract PatcherImages LoadImages();

        public abstract string PluginId { get; }
        //internal static VFConfig VFConfig { get; private set; }
        //internal static AVSNautilusConfig NautilusConfig { get; private set; }

        internal Coroutine? GetVoices { get; private set; } = null;
        internal Coroutine? GetEngineSounds { get; private set; } = null;

        /// <summary>
        /// Begins plugin patching and initialization.
        /// Also initializes the logger. Before this method is called, the logger will not work.
        /// </summary>
        public virtual void Awake()
        {
            AVS.Logger.Init(Logger);
            LogWriter.Default.Write("AVS MainPatcher Awake started.");

            Nautilus.Handlers.LanguageHandler.RegisterLocalizationFolder();
            SetupInstance();
            LogWriter.Default.Write("AVS MainPatcher Awake: SetupInstance completed. Loading images...");
            images = LoadImages();


            //VFConfig = new VFConfig();
            //NautilusConfig = Nautilus.Handlers.OptionsPanelHandler.RegisterModOptions<AVSNautilusConfig>();
            PrePatch();
            PrefabLoader.SignalCanLoad();
            PrefabLoader.Request(CoroutineHost.From(this), TechType.Exosuit);
            _ = SeamothHelper.Coroutine;
            PrefabLoader.Request(CoroutineHost.From(this), TechType.Aquarium);
        }
        /// <inheritdoc/>
        public virtual void Start()
        {
            Patch();
            PostPatch();
            CompatChecker.CheckAll();
            StartCoroutine(AVS.Logger.MakeAlerts());

        }

        /// <summary>
        ///  PrePatch is called before any patches are applied.
        /// </summary>
        public virtual void PrePatch()
        {
            LogWriter.Default.Write("PrePatch started.");
            IEnumerator CollectPrefabsForBuilderReference()
            {
                CoroutineTask<GameObject> request = CraftData.GetPrefabForTechTypeAsync(TechType.BaseUpgradeConsole, true);
                yield return request;
                AvsVehicleBuilder.UpgradeConsole = request.GetResult();
                yield break;
            }
            LogWriter.Default.Write("CollectPrefabsForBuilderReference started.");
            StartCoroutine(CollectPrefabsForBuilderReference());
            //LogWriter.Default.Write("Assets.StaticAssets.GetSprites()");
            //Assets.StaticAssets.GetSprites();
            LogWriter.Default.Write("Assets.AVSFabricator.CreateAndRegister()");
            Assets.AvsFabricator.CreateAndRegister(Images.FabricatorIcon.Sprite);
            //LogWriter.Default.Write("Admin.CraftTreeHandler.AddFabricatorMenus()");
            //CraftTreeHandler.AddFabricatorMenus();

            LogWriter.Default.Write("Admin.Utils.RegisterDepthModules()");
            Admin.Utils.RegisterDepthModules();
            //AVS.Logger.Log("MainPatcher.Instance.StartCoroutine(VoiceManager.LoadAllVoices())");
            //GetVoices = MainPatcher.Instance.StartCoroutine(VoiceManager.LoadAllVoices());
            //AVS.Logger.Log("MainPatcher.Instance.StartCoroutine(EngineSoundsManager.LoadAllVoices())");
            //GetEngineSounds = MainPatcher.Instance.StartCoroutine(DynamicClipLoader.LoadAllVoices());
            LogWriter.Default.Write("PrePatch finished.");
        }
        /// <summary>
        /// Applies various patches and event registrations necessary for mod compatibility and game state management.
        /// </summary>
        /// <remarks>This method registers save data events, patches external mods for compatibility, and
        /// manages game state transitions. It uses the Harmony library to apply patches to methods in other mods,
        /// ensuring that they work correctly with this mod. Additionally, it sets up event handlers to manage game
        /// state changes during loading and unloading of scenes.</remarks>
        public virtual void Patch()
        {
            LogWriter.Default.Write("Patch started.");
            LogWriter.Default.Write("Nautilus.Handlers.SaveDataHandler.RegisterSaveDataCache<SaveLoad.SaveData>()");
            var saveData = Nautilus.Handlers.SaveDataHandler.RegisterSaveDataCache<SaveLoad.ZeroSaveData>();

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
                try
                {
                    AvsVehicleManager.CreateSpritesFile(sender, e);
                    LogWriter.Default.Write("Sprites file created successfully.");
                }
                catch (Exception ex)
                {
                    LogWriter.Default.Error("Failed to create sprites file", ex);
                }
            };

            //saveData.OnFinishedSaving += (object sender, Nautilus.Json.JsonFileEventArgs e) =>
            //{
            //    //VehicleComponents.MagnetBoots.AttachAll();
            //};

            //saveData.OnFinishedLoading += (object sender, Nautilus.Json.JsonFileEventArgs e) =>
            //{
            //    //SaveFileData = e.Instance as SaveLoad.SaveData;
            //};

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
            LogWriter.Default.Write("Registering SaveUtils events.");
            Nautilus.Utility.SaveUtils.RegisterOnQuitEvent(SetWorldNotLoaded);
            WaitScreenHandler.RegisterLateLoadTask(nameof(SetWorldLoaded), t => SetWorldLoaded());
            //Nautilus.Utility.SaveUtils.RegisterOnFinishLoadingEvent(SetWorldLoaded);
            Nautilus.Utility.SaveUtils.RegisterOneTimeUseOnLoadEvent(OnLoadOnce);

            LogWriter.Default.Write("Patching...");
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

        /// <summary>
        /// Executes post-patch operations for vehicle data management.
        /// </summary>
        /// <remarks>This method is intended to be called after patching operations to ensure that vehicle
        /// data is correctly updated. It may involve operations such as scattering data boxes for craftable
        /// items.</remarks>
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
