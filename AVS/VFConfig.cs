using BepInEx.Configuration;
using System.Collections.Generic;

namespace AVS
{
    internal class VehicleConfig
    {
        internal static Dictionary<string, VehicleConfig> main = new Dictionary<string, VehicleConfig>();
        //internal static VehicleConfig GetConfig(ModVehicle mv)
        //{
        //    if (!main.ContainsKey(mv.GetType().ToString()))
        //    {
        //        AddNew(mv);
        //    }
        //    return main[mv.GetType().ToString()];
        //}
        private static VehicleConfig AddNew(ModVehicle mv)
        {
            var thisConf = new VehicleConfig();
            main.Add(mv.GetType().ToString(), thisConf);
            return thisConf;
        }
        internal ConfigEntry<bool> IsEnabled { get; set; }
        internal ConfigEntry<float> AutopilotVolume { get; set; }
        internal ConfigEntry<float> EngineVolume { get; set; }
        internal ConfigEntry<int> NumUpgrades { get; set; }
        internal ConfigEntry<bool> UseCustomRecipe { get; set; }
        internal ConfigEntry<uGUI_VehicleHUD.HUDChoice> HUDChoice { get; set; }
        internal List<ConfigEntry<bool>> ExternalToggles = new List<ConfigEntry<bool>>();
        internal List<ConfigEntry<float>> ExternalSliders = new List<ConfigEntry<float>>();
        internal List<ConfigEntry<KeyboardShortcut>> ExternalKeybinds = new List<ConfigEntry<KeyboardShortcut>>();
    }
    internal class VFConfig
    {
        //internal static ConfigFile config;
        internal static void Setup(ModVehicle mv)
        {
            //config = MainPatcher.Instance.Config;
            //var vConf = VehicleConfig.GetConfig(mv);
            //string vehicleName = mv.GetType().ToString();
            //vConf.AutopilotVolume = config.Bind<float>(vehicleName, "Autopilot Volume", 0.5f, new ConfigDescription("How loud is the autopilot voice", new AcceptableValueRange<float>(0, 1)));
            //vConf.EngineVolume = config.Bind<float>(vehicleName, "Engine Volume", 0.5f, new ConfigDescription("How loud are the engine sounds", new AcceptableValueRange<float>(0, 1)));

            //vConf.AutopilotVoice = config.Bind<string>(vehicleName, "Autopilot Voice", VoiceManager.vehicleVoices.Select(x => x.Key).FirstOrDefault(), new ConfigDescription("Choose an autopilot voice for this vehicle", new AcceptableValueList<string>(VoiceManager.vehicleVoices.Select(x => x.Key).ToArray())));
            //vConf.AutopilotVoice.SettingChanged += GrabNewVoiceLines;

            //vConf.EngineSounds = config.Bind<string>(vehicleName, "Engine Sounds", EngineSoundsManager.EngineSoundss.Select(x => x.Key).FirstOrDefault(), new ConfigDescription("Choose engine sounds for this vehicle", new AcceptableValueList<string>(EngineSoundsManager.EngineSoundss.Select(x => x.Key).ToArray())));
            //vConf.EngineSounds.SettingChanged += GrabNewEngineSounds;

            //vConf.HUDChoice = config.Bind<uGUI_VehicleHUD.HUDChoice>(vehicleName, "HUD Choice", uGUI_VehicleHUD.HUDChoice.Storage, "Choose a HUD option for this vehicle");

            //vConf.NumUpgrades = config.Bind<int>(vehicleName, "Number of Upgrade Slots", mv.NumModules, new ConfigDescription("How many upgrades can this vehicle use? (restart required)", new AcceptableValueRange<int>(0, ModuleBuilder.MaxNumModules)));
            //vConf.IsArms = config.Bind<bool>(vehicleName, "Enable Arm Slots", mv.HasArms, new ConfigDescription("Can this vehicle use arm upgrades? (restart required)"));

            //vConf.UseCustomRecipe = config.Bind<bool>(vehicleName, "Use Custom Recipe", false, new ConfigDescription("Should this vehicle use the custom recipe file in the AVS/recipes folder? (restart required)"));
            //Admin.ExternalVehicleConfig<bool>.GetModVehicleConfig(mv.name);
            //Admin.ExternalVehicleConfig<float>.GetModVehicleConfig(mv.name);
            //Admin.ExternalVehicleConfig<KeyboardShortcut>.GetModVehicleConfig(mv.name);
        }
    }
}
