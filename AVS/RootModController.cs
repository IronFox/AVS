using AVS.Admin;
using AVS.Assets;
using AVS.Log;
using AVS.Patches.CompatibilityPatches;
using AVS.Util;
using AVS.VehicleBuilding;
using BepInEx;
using HarmonyLib;
using Nautilus.Handlers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AVS;

/// <summary>
/// AVS root mod controller class. Must be inherited by the main mod class.
/// </summary>
public abstract class RootModController : BaseUnityPlugin
{
    //private static MainPatcher? _instance;
    private PatcherImages? images;

    /// <summary>
    /// Loaded patcher images. Available after <see cref="LoadImages"/> is called in <see cref="Awake"/>.
    /// </summary>
    public PatcherImages Images
    {
        get
        {
            if (images is null)
                throw new InvalidOperationException(
                    "PatcherImages is not initialized. Ensure that LoadImages() is called before accessing this property.");
            return images;
        }
    }



    /// <summary>
    /// The icon for the Depth Module 1 upgrade.
    /// </summary>
    public Sprite DepthModule1Icon => Images.DepthModule1Icon;

    /// <summary>
    /// The icon for the Depth Module 2 upgrade.
    /// </summary>
    public Sprite DepthModule2Icon => Images.DepthModule2Icon;

    /// <summary>
    /// The icon for the Depth Module 3 upgrade.
    /// </summary>
    public Sprite DepthModule3Icon => Images.DepthModule3Icon;

    /// <summary>
    /// The icon to use for the parent node of all depth modules in the crafting tree.
    /// </summary>
    public Sprite DepthModuleNodeIcon => Images.DepthModuleNodeIcon;

    ///// <summary>
    ///// Queries the main singleton instance of <see cref="MainPatcher"/>.
    ///// </summary>
    //public static MainPatcher Instance => _instance.OrThrow(() => new InvalidOperationException(
    //    "MainPatcher instance is not set. Ensure that the Awake method is called before accessing Instance."));

    /// <summary>
    /// Loads the images used by AVS.
    /// </summary>
    /// <returns></returns>
    protected abstract PatcherImages LoadImages();

    /// <summary>
    /// Unique identifier for the plugin.
    /// </summary>
    public abstract string PluginId { get; }

    /// <summary>
    /// Simple name of the plugin, used for logging and identification.
    /// Should be a short, human-readable name without spaces or special characters.
    /// Prefix used for registered identifiers so to not collide with other mods.
    /// </summary>
    public abstract string ModName { get; }


    //internal static VFConfig VFConfig { get; private set; }
    //internal static AVSNautilusConfig NautilusConfig { get; private set; }

    private static Dictionary<int, RootModController> Instances { get; } = [];

    internal static RootModController AnyInstance
    {
        get
        {
            if (Instances.Count > 0)
                return Instances.Values.First();
            throw new InvalidOperationException("No MainPatcher instances are registered.");
        }
    }

    internal static IEnumerable<RootModController> AllInstances => Instances.Values;

    /// <summary>
    /// Logging verbosity level for the mod.
    /// </summary>
    public virtual Verbosity LogVerbosity => Verbosity.Regular;

    /// <summary>
    /// Begins plugin patching and initialization.
    /// Also initializes the logger. Before this method is called, the logger will not work.
    /// </summary>
    public virtual void Awake()
    {
        AVS.Logger.Init(Logger);
        using var log = SmartLog.ForAVS(this);
        var assembly = typeof(RootModController).Assembly;
        var name = assembly.GetName();
        //Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        log.Write($"Booting AVS {name.Version} from {assembly.Location} for {PluginId}.");
        log.Write("AVS MainPatcher Awake started.");

        LanguageHandler.RegisterLocalizationFolder();

        Instances[GetInstanceID()] = this;

        log.Write("AVS MainPatcher Awake: SetupInstance completed. Loading images...");
        images = LoadImages();


        //VFConfig = new VFConfig();
        //NautilusConfig = Nautilus.Handlers.OptionsPanelHandler.RegisterModOptions<AVSNautilusConfig>();
        PrePatch();
        PrefabLoader.SignalCanLoad();
        PrefabLoader.Request(TechType.Exosuit, true);
        SeamothHelper.Request();
        //PrefabLoader.Request(TechType.Aquarium);
    }

    /// <inheritdoc/>
    public virtual void OnDestroy()
    {
        Instances.Remove(GetInstanceID());
    }

    /// <inheritdoc/>
    public virtual void Start()
    {
        try
        {
            Patch();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        PostPatch();
        CompatChecker.CheckAll(this);
        StartAvsCoroutine(nameof(AVS.Logger) + '.' + nameof(AVS.Logger.MakeAlerts), log => AVS.Logger.MakeAlerts());
    }

    /// <summary>
    ///  PrePatch is called before any patches are applied.
    /// </summary>
    public virtual void PrePatch()
    {
        using var log = SmartLog.ForAVS(this);
        log.Write("PrePatch started.");

        IEnumerator CollectPrefabsForBuilderReference()
        {
            CoroutineTask<GameObject> request = CraftData.GetPrefabForTechTypeAsync(TechType.BaseUpgradeConsole, true);
            yield return request;
            AvsVehicleBuilder.UpgradeConsole = request.GetResult();
        }

        StartAvsCoroutine(nameof(RootModController) + '.' + nameof(PrePatch) + '.' + nameof(CollectPrefabsForBuilderReference), _ => CollectPrefabsForBuilderReference());
        try
        {
            AvsFabricator.CreateAndRegister(this, Images.FabricatorIcon);
            Admin.Utils.RegisterDepthModules(this);
            log.Write("PrePatch finished.");
        }
        catch (Exception e)
        {
            log.Error("PrePatch failed.", e);
        }
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
        using var log = SmartLog.ForAVS(this);

        log.Write("Starting");
        log.Write("Nautilus.Handlers.SaveDataHandler.RegisterSaveDataCache<SaveLoad.SaveData>()");
        var saveData = SaveDataHandler.RegisterSaveDataCache<SaveLoad.ZeroSaveData>();
        log.Write($"Registering OnStartedSaving");
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
                AvsVehicleManager.CreateSpritesFile(this, e);
                log.Write("Sprites file created successfully.");
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
            GameStateWatcher.IsWorldLoaded = false;
            ModuleBuilder.haveWeCalledBuildAllSlots = false;
            ModuleBuilder.slotExtenderIsPatched = false;
            ModuleBuilder.SlotExtenderHasGreenLight = false;
        }

        void SetWorldLoaded()
        {
            GameStateWatcher.IsWorldLoaded = true;
        }

        void OnLoadOnce()
        {
        }

        log.Write("Registering SaveUtils events.");
        Nautilus.Utility.SaveUtils.RegisterOnQuitEvent(SetWorldNotLoaded);
        WaitScreenHandler.RegisterLateLoadTask(nameof(SetWorldLoaded), t => SetWorldLoaded());
        //Nautilus.Utility.SaveUtils.RegisterOnFinishLoadingEvent(SetWorldLoaded);
        Nautilus.Utility.SaveUtils.RegisterOneTimeUseOnLoadEvent(OnLoadOnce);

        log.Write("Patching...");
        var harmony = new Harmony(PluginId);
        var assembly = typeof(RootModController).Assembly;

        var patches =
            AccessTools
                .GetTypesFromAssembly(assembly)
                .Select(x => (Type: x, Processor: harmony.CreateClassProcessor(x)))
                .ToList();
        log.Write($"Identified {patches.Count} types to potentially patch. Patching...");
        foreach (var patch in patches)
            //log.Write($"Executing patch {patch.Type}...");
            try
            {
                patch.Processor.Patch();
            }
            catch (Exception e)
            {
                log.Error($"Failed to patch {patch.Type}.", e);
            }

        log.Write($"Patching PingMapIcon.Refresh()...");
        try
        {
            // Patch SubnauticaMap with appropriate ping sprites, lest it crash.
            var type = Type.GetType("SubnauticaMap.PingMapIcon, SubnauticaMap", false, false);
            if (type.IsNotNull())
            {
                var pingOriginal = AccessTools.Method(type, "Refresh");
                if (pingOriginal.IsNull())
                {
                    log.Warn($"Failed to find method {type.FullName}.Refresh() to patch.");
                }
                else
                {
                    var pingPrefix =
                        new HarmonyMethod(AccessTools.Method(
                            typeof(MapModPatcher),
                            nameof(MapModPatcher.Prefix)));
                    harmony.Patch(pingOriginal, pingPrefix);
                }
            }
        }
        catch (Exception e)
        {
            log.Error($"Failed to patch PingMapIcon.Refresh()", e);
        }

        log.Write($"Patching SlotExtender...");
        try
        {
            // Patch SlotExtender, lest it break or break us
            var type2 = Type.GetType("SlotExtender.Patches.uGUI_Equipment_Awake_Patch, SlotExtender", false, false);
            if (type2.IsNotNull())
            {
                var awakePreOriginal = AccessTools.Method(type2, "Prefix");
                var awakePrefix =
                    new HarmonyMethod(AccessTools.Method(typeof(SlotExtenderPatcher),
                        "PrePrefix"));
                harmony.Patch(awakePreOriginal, awakePrefix);

                var awakePostOriginal = AccessTools.Method(type2, "Postfix");
                var awakePostfix =
                    new HarmonyMethod(AccessTools.Method(typeof(SlotExtenderPatcher),
                        "PrePostfix"));
                harmony.Patch(awakePostOriginal, awakePostfix);
            }
        }
        catch (Exception e)
        {
            log.Error($"Failed to patch SlotExtender", e);
        }

        log.Write($"Patching BetterVehicleStorage...");
        try
        {
            // Patch BetterVehicleStorage to add AvsVehicle compat
            var type3 = Type.GetType("BetterVehicleStorage.Managers.StorageModuleMgr, BetterVehicleStorage", false,
                false);
            if (type3.IsNotNull())
            {
                var AllowedToAddOriginal = AccessTools.Method(type3, "AllowedToAdd");
                var AllowedToAddPrefix =
                    new HarmonyMethod(AccessTools.Method(
                        typeof(BetterVehicleStoragePatcher),
                        "Prefix"));
                harmony.Patch(AllowedToAddOriginal, AllowedToAddPrefix);
            }
        }
        catch (Exception e)
        {
            log.Error($"Failed to patch BetterVehicleStorage", e);
        }
        /*
        var type2 = Type.GetType("SlotExtender.Patches.uGUI_Equipment_Awake_Patch, SlotExtender", false, false);
        if (type2.IsNotNull())
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

        log.Write("Registering SceneManager events.");
        // do this here because it happens only once
        SceneManager.sceneUnloaded += GameStateWatcher.SignalSceneUnloaded;
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

    internal static RootModController GetInstance(int mainPatcherInstanceId)
    {
        if (Instances.TryGetValue(mainPatcherInstanceId, out var instance))
            return instance;
        throw new InvalidOperationException(
            $"No MainPatcher instance with ID {mainPatcherInstanceId} is registered. Known IDs: {string.Join(", ", Instances.Keys)}");
    }


    /// <summary>
    /// Starts a new coroutine with enhanced error handling and logging.
    /// </summary>
    /// <param name="routine">The routine being executed</param>
    /// <param name="methodName">Name of the method or context for logging purposes.</param>
    /// <returns>The coroutine executing the enumerator</returns>
    internal Coroutine StartAvsCoroutine(string methodName, Func<SmartLog, IEnumerator> routine)
    {
        return StartCoroutine(Run(methodName, routine, true));
    }

    /// <summary>
    /// Starts a new coroutine with enhanced error handling and logging.
    /// </summary>
    /// <param name="routine">The routine being executed</param>
    /// <param name="methodName">Name of the method or context for logging purposes.</param>
    /// <returns>The coroutine executing the enumerator</returns>
    public Coroutine StartModCoroutine(string methodName, Func<SmartLog, IEnumerator> routine)
    {
        return StartCoroutine(Run(methodName, routine, false));
    }

    private IEnumerator Run(string methodName, Func<SmartLog, IEnumerator> factory, bool isAvs)
    {
        using var log = new SmartLog(this, isAvs ? "AVS" : "Mod", 5, true, nameOverride: methodName);
        var routine = factory(log);
        while (true)
        {
            object? current;
            try
            {
                if (!routine.MoveNext())
                {
                    log.Write("Finished");
                    yield break;
                }
                current = routine.Current;
            }
            catch (Exception e)
            {
                log.Error("Exception in coroutine", e);
                yield break;
            }
            log.Interrupt();
            yield return current;
            log.Resume();
        }
    }
}