using BepInEx.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AVS.Admin
{
    /// <summary>
    /// Represents a configuration manager for external vehicle types, allowing retrieval and management of
    /// configuration entries for specific vehicles.
    /// </summary>
    /// <remarks>This class provides methods to retrieve configuration values for specific vehicles by name or
    /// type. It also includes static methods to access predefined configurations for common vehicle types such as
    /// Seamoth, Prawn, and Cyclops. Configuration entries are stored in a dictionary and can be accessed by their
    /// unique names.</remarks>
    /// <typeparam name="T">The type of the configuration values managed by this instance.</typeparam>
    public class ExternalVehicleConfig<T>
    {
        internal string MyName = "";
        internal Dictionary<string, ConfigEntry<T>> ExternalConfigs = new Dictionary<string, ConfigEntry<T>>();

        internal static Dictionary<string, ExternalVehicleConfig<T>> main = new Dictionary<string, ExternalVehicleConfig<T>>();
        internal static ExternalVehicleConfig<T> SeamothConfig = null;
        internal static ExternalVehicleConfig<T> PrawnConfig = null;
        internal static ExternalVehicleConfig<T> CyclopsConfig = null;
        private static ExternalVehicleConfig<T> AddNew(ModVehicle mv)
        {
            var thisConf = new ExternalVehicleConfig<T>
            {
                MyName = mv.GetType().ToString()
            };
            main.Add(thisConf.MyName, thisConf);
            return thisConf;
        }
        /// <summary>
        /// Retrieves the value of the external configuration entry with the specified name.
        /// </summary>
        /// <param name="name">The name of the configuration entry to retrieve.</param>
        /// <returns>The value of the configuration entry associated with the specified name.</returns>
        /// <exception cref="ArgumentException">Thrown if the specified <paramref name="name"/> does not exist in the external configuration.</exception>
        public T GetValue(string name)
        {
            if (ExternalConfigs.ContainsKey(name))
            {
                return ExternalConfigs[name].Value;
            }
            throw new ArgumentException($"External config for {MyName} does not have a config entry of name {name}.");
        }
        /// <summary>
        /// Retrieves the external vehicle configuration for a specified mod vehicle by its name.
        /// </summary>
        /// <remarks>This method searches for a mod vehicle by name within the available vehicle types. If
        /// no match is found, or if multiple matches are found, an exception is thrown. If the configuration for the
        /// specified mod vehicle does not already exist, it is created and added to the internal collection.</remarks>
        /// <param name="vehicleName">The name of the mod vehicle to retrieve the configuration for. The comparison is case-insensitive.</param>
        /// <returns>An <see cref="ExternalVehicleConfig{T}"/> object representing the configuration of the specified mod
        /// vehicle.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="vehicleName"/> does not match any mod vehicle, or if it matches multiple mod
        /// vehicles.</exception>
        public static ExternalVehicleConfig<T> GetModVehicleConfig(string vehicleName)
        {
            var MVs = VehicleManager.vehicleTypes.Where(x => x.name.Equals(vehicleName, StringComparison.OrdinalIgnoreCase)).ToList();
            if (MVs.Count == 0)
            {
                StringBuilder sb = new StringBuilder();
                VehicleManager.vehicleTypes.ForEach(x => sb.AppendLine(x.name));
                throw new ArgumentException($"GetModVehicleConfig: vehicle name does not identify a ModVehicle: {vehicleName}. Options are: {sb}");
            }
            if (MVs.Count > 1)
            {
                StringBuilder sb = new StringBuilder();
                VehicleManager.vehicleTypes.ForEach(x => sb.AppendLine(x.name));
                throw new ArgumentException($"GetModVehicleConfig: vehicle name does not uniquely identify a ModVehicle: {vehicleName}. There were {MVs.Count()} matches: {sb}");
            }
            ModVehicle mv = MVs[0].mv;
            if (!main.ContainsKey(mv.GetType().ToString()))
            {
                AddNew(mv);
            }
            return main[mv.GetType().ToString()];
        }
        /// <summary>
        /// Retrieves the configuration for the Seamoth vehicle.
        /// </summary>
        /// <remarks>This method ensures that the Seamoth configuration is initialized before returning
        /// it.  Subsequent calls will return the same configuration instance.</remarks>
        /// <returns>An instance of <see cref="ExternalVehicleConfig{T}"/> representing the Seamoth's configuration.  If the
        /// configuration has not been initialized, it will be created and returned.</returns>
        public static ExternalVehicleConfig<T> GetSeamothConfig()
        {
            if (SeamothConfig == null)
            {
                SeamothConfig = new ExternalVehicleConfig<T>
                {
                    MyName = ConfigRegistrar.SeamothName
                };
            }
            return SeamothConfig;
        }
        /// <summary>
        /// Retrieves the configuration for the Prawn.
        /// </summary>
        /// <remarks>If the configuration has not been initialized, this method creates a new instance of 
        /// <see cref="ExternalVehicleConfig{T}"/> with the name set to the value of  <see
        /// cref="ConfigRegistrar.PrawnName"/>.</remarks>
        /// <returns>An instance of <see cref="ExternalVehicleConfig{T}"/> representing the Prawn configuration.</returns>
        public static ExternalVehicleConfig<T> GetPrawnConfig()
        {
            if (PrawnConfig == null)
            {
                PrawnConfig = new ExternalVehicleConfig<T>
                {
                    MyName = ConfigRegistrar.PrawnName
                };
            }
            return PrawnConfig;
        }
        /// <summary>
        /// Retrieves the Cyclops configuration for the specified config type.
        /// </summary>
        /// <remarks>This method ensures that the Cyclops configuration is initialized before returning
        /// it. Subsequent calls will return the same configuration instance.</remarks>
        /// <returns>An instance of <see cref="ExternalVehicleConfig{T}"/> representing the Cyclops configuration.  If the
        /// configuration has not been initialized, it will be created and returned.</returns>
        public static ExternalVehicleConfig<T> GetCyclopsConfig()
        {
            if (CyclopsConfig == null)
            {
                CyclopsConfig = new ExternalVehicleConfig<T>
                {
                    MyName = ConfigRegistrar.CyclopsName
                };
            }
            return CyclopsConfig;
        }
    }

    /// <summary>
    /// Provides methods for registering configuration options for various vehicles in the game.
    /// </summary>
    /// <remarks>This class includes functionality to register configuration options for all modded vehicles
    /// or specific vehicles, such as the Seamoth, Prawn Suit, and Cyclops. It supports configuration types of <see
    /// langword="bool"/>, <see langword="float"/>,  and <see cref="KeyboardShortcut"/>. Configuration changes can
    /// trigger optional callbacks for custom handling.</remarks>
    public static class ConfigRegistrar
    {
        internal const string SeamothName = "VanillaSeaMoth";
        internal const string PrawnName = "VanillaPrawn";
        internal const string CyclopsName = "VanillaCyclops";
        /// <summary>
        /// Logs the names of all vehicles currently present in the game.
        /// </summary>
        /// <remarks>This method initiates an asynchronous operation to retrieve and log vehicle names. 
        /// It does not block the calling thread and relies on the game's coroutine system to execute.</remarks>
        public static void LogAllVehicleNames()
        {
            UWE.CoroutineHost.StartCoroutine(LogAllVehicleNamesInternal());
        }
        private static IEnumerator LogAllVehicleNamesInternal()
        {
            // wait until the player exists, so that we're sure every vehicle is done with registration
            yield return new UnityEngine.WaitUntil(() => Player.main != null);
            var boolNames = ExternalVehicleConfig<bool>.main.Keys;
            var floatNames = ExternalVehicleConfig<float>.main.Keys;
            var keyNames = ExternalVehicleConfig<KeyboardShortcut>.main.Keys;
            var result = boolNames.Concat(floatNames).Concat(keyNames).Distinct().ToList();
            result.Add(SeamothName);
            result.Add(PrawnName);
            result.Add(CyclopsName);
            Logger.Log("Logging all vehicle type names:");
            result.ForEach(x => Logger.Log(x));
        }
        /// <summary>
        /// Registers a configuration option for all modded vehicles in the game.
        /// </summary>
        /// <typeparam name="T">The type of the configuration value.</typeparam>
        /// <param name="name">The name of the configuration option.</param>
        /// <param name="description">A description of the configuration option, including its purpose and usage.</param>
        /// <param name="defaultValue">The default value for the configuration option.</param>
        /// <param name="OnChange">An optional callback invoked when the configuration value changes. The callback receives the <see
        /// cref="TechType"/> of the vehicle and the new value.</param>
        /// <param name="configFile">An optional configuration file to store the setting. If not provided, a default configuration file is used.</param>
        public static void RegisterForAllModVehicles<T>(string name, ConfigDescription description, T defaultValue, Action<TechType, T> OnChange = null, ConfigFile configFile = null)
        {
            UWE.CoroutineHost.StartCoroutine(RegisterForAllInternal<T>(name, description, defaultValue, OnChange, configFile));
        }
        /// <summary>
        /// Registers a configuration option for a specific modded vehicle.
        /// </summary>
        /// <remarks>This method initiates the registration process asynchronously. The configuration
        /// option will be associated with the specified modded vehicle and can be accessed or modified through the
        /// configuration system.</remarks>
        /// <typeparam name="T">The type of the configuration value. Must be a type supported by the configuration system.</typeparam>
        /// <param name="vehicleName">The name of the modded vehicle for which the configuration option is being registered.</param>
        /// <param name="name">The name of the configuration option.</param>
        /// <param name="description">A description of the configuration option, including details such as its purpose or valid range.</param>
        /// <param name="defaultValue">The default value for the configuration option.</param>
        /// <param name="OnChange">An optional callback that is invoked when the configuration value changes. The callback receives the <see
        /// cref="TechType"/> of the vehicle and the new value of the configuration option.</param>
        /// <param name="configFile">An optional <see cref="ConfigFile"/> instance to store the configuration option. If not provided, the
        /// default configuration file is used.</param>
        public static void RegisterForModVehicle<T>(string vehicleName, string name, ConfigDescription description, T defaultValue, Action<TechType, T> OnChange = null, ConfigFile configFile = null)
        {
            UWE.CoroutineHost.StartCoroutine(RegisterForVehicleInternal<T>(vehicleName, name, description, defaultValue, OnChange, configFile));
        }
        /// <summary>
        /// Registers a configuration option for the Seamoth vehicle.
        /// </summary>
        /// <remarks>This method initiates the registration process asynchronously. The configuration
        /// option will be associated with the Seamoth vehicle and can be used to customize its behavior or
        /// settings.</remarks>
        /// <typeparam name="T">The type of the configuration value.</typeparam>
        /// <param name="name">The unique name of the configuration option.</param>
        /// <param name="description">A description of the configuration option, including its purpose and constraints.</param>
        /// <param name="defaultValue">The default value for the configuration option.</param>
        /// <param name="OnChange">An optional callback that is invoked when the configuration value changes. The new value is passed as a
        /// parameter.</param>
        /// <param name="configFile">An optional configuration file to store the setting. If not provided, a default configuration file is used.</param>
        public static void RegisterForSeamoth<T>(string name, ConfigDescription description, T defaultValue, Action<T> OnChange = null, ConfigFile configFile = null)
        {
            UWE.CoroutineHost.StartCoroutine(RegisterForSeamothInternal<T>(name, description, defaultValue, OnChange, configFile));
        }
        /// <summary>
        /// Registers a configuration option for the Prawn with the specified name, description, and default
        /// value.
        /// </summary>
        /// <remarks>This method starts a coroutine to handle the registration process asynchronously. The
        /// registration ensures that the configuration option is properly integrated with the Prawn and its
        /// associated systems.</remarks>
        /// <typeparam name="T">The type of the configuration value. Must be a type supported by the configuration system.</typeparam>
        /// <param name="name">The unique name of the configuration option. This name is used to identify the option.</param>
        /// <param name="description">A description of the configuration option, including details such as its purpose or valid range of values.</param>
        /// <param name="defaultValue">The default value for the configuration option. This value is used if no other value is provided.</param>
        /// <param name="OnChange">An optional callback that is invoked whenever the configuration value changes. The new value is passed as a
        /// parameter to the callback.</param>
        /// <param name="configFile">An optional configuration file object where the configuration option will be stored. If not provided, a
        /// default configuration file is used.</param>
        public static void RegisterForPrawn<T>(string name, ConfigDescription description, T defaultValue, Action<T> OnChange = null, ConfigFile configFile = null)
        {
            UWE.CoroutineHost.StartCoroutine(RegisterForPrawnInternal<T>(name, description, defaultValue, OnChange, configFile));
        }
        /// <summary>
        /// Registers a configuration option for the Cyclops submarine with the specified name, description, and default
        /// value.
        /// </summary>
        /// <remarks>This method starts a coroutine to handle the registration process asynchronously. The
        /// configuration option will be available for use after the coroutine completes.</remarks>
        /// <typeparam name="T">The type of the configuration value.</typeparam>
        /// <param name="name">The unique name of the configuration option. This name is used to identify the option.</param>
        /// <param name="description">A description of the configuration option, including details such as its purpose or valid range.</param>
        /// <param name="defaultValue">The default value for the configuration option.</param>
        /// <param name="OnChange">An optional callback that is invoked whenever the configuration value changes. The new value is passed as a
        /// parameter to the callback.</param>
        /// <param name="configFile">An optional configuration file where the option will be stored. If not provided, a default configuration
        /// file is used.</param>
        public static void RegisterForCyclops<T>(string name, ConfigDescription description, T defaultValue, Action<T> OnChange = null, ConfigFile configFile = null)
        {
            UWE.CoroutineHost.StartCoroutine(RegisterForCyclopsInternal<T>(name, description, defaultValue, OnChange, configFile));
        }
        private static IEnumerator RegisterForAllInternal<T>(string name, ConfigDescription description, T defaultValue, Action<TechType, T> OnChange = null, ConfigFile configFile = null)
        {
            if (typeof(T) != typeof(bool) && typeof(T) != typeof(float) && typeof(T) != typeof(KeyboardShortcut))
            {
                Logger.Error($"ConfigRegistrar only accepts type parameters: bool, float, KeyboardShortcut, but you supplied the type parameter {typeof(T)}.");
                yield break;
            }
            // wait until the player exists, so that we're sure every vehicle is done with registration
            yield return new UnityEngine.WaitUntil(() => Player.main != null);
            foreach (var pair in ExternalVehicleConfig<T>.main)
            {
                ConfigFile config = configFile;
                if (config == null)
                {
                    config = MainPatcher.Instance.Config;
                }
                var vConf = pair.Value;
                string vehicleName = pair.Key;
                ConfigEntry<T> thisConf;
                try
                {
                    thisConf = config.Bind<T>(vehicleName, name, defaultValue, description);
                }
                catch (Exception e)
                {
                    Logger.LogException("ConfigRegistrar: Could not bind that config option. Probably you chose a non-unique name.", e);
                    yield break;
                }
                if (OnChange != null)
                {
                    void DoThisAction(object sender, EventArgs e)
                    {
                        foreach (ModVehicle innerMV in VehicleManager.vehicleTypes.Select(x => x.mv))
                        {
                            if (innerMV.GetType().ToString() == vehicleName)
                            {
                                OnChange(innerMV.TechType, thisConf.Value);
                                break;
                            }
                        }
                    }
                    thisConf.SettingChanged += DoThisAction;
                }
                vConf.ExternalConfigs.Add(name, thisConf);
            }
        }
        private static IEnumerator RegisterForVehicleInternal<T>(string vehicleName, string name, ConfigDescription description, T defaultValue, Action<TechType, T> OnChange, ConfigFile configFile)
        {
            if (typeof(T) != typeof(bool) && typeof(T) != typeof(float) && typeof(T) != typeof(KeyboardShortcut))
            {
                Logger.Error($"ConfigRegistrar only accepts type parameters: bool, float, KeyboardShortcut, but you supplied the type parameter {typeof(T)}.");
                yield break;
            }
            // wait until the player exists, so that we're sure every vehicle is done with registration
            yield return new UnityEngine.WaitUntil(() => Player.main != null);
            var MVs = VehicleManager.vehicleTypes.Where(x => x.name.ToLower().Contains(vehicleName.ToLower()));
            if (!MVs.Any())
            {
                throw new ArgumentException($"RegisterForModVehicle: vehicle name does not identify a ModVehicle: {vehicleName}");
            }
            if (MVs.Count() > 1)
            {
                throw new ArgumentException($"RegisterForModVehicle: vehicle name does not uniquely identify a ModVehicle: {vehicleName}. There were {MVs.Count()} matches.");
            }
            ModVehicle mv = MVs.First().mv;
            ConfigFile config = configFile;
            if (config == null)
            {
                config = MainPatcher.Instance.Config;
            }
            var vConf = ExternalVehicleConfig<T>.GetModVehicleConfig(vehicleName);
            ConfigEntry<T> thisConf;
            try
            {
                thisConf = config.Bind<T>(vConf.MyName, name, defaultValue, description);
            }
            catch (Exception e)
            {
                Logger.LogException("ConfigRegistrar: Could not bind that config option. Probably you chose a non-unique name.", e);
                yield break;
            }
            if (OnChange != null)
            {
                void DoThisAction(object sender, EventArgs e)
                {
                    OnChange(mv.TechType, thisConf.Value);
                }
                thisConf.SettingChanged += DoThisAction;
            }
            vConf.ExternalConfigs.Add(name, thisConf);
        }
        private static IEnumerator RegisterForSeamothInternal<T>(string name, ConfigDescription description, T defaultValue, Action<T> onChange, ConfigFile configFile)
        {
            if (typeof(T) != typeof(bool) && typeof(T) != typeof(float) && typeof(T) != typeof(KeyboardShortcut))
            {
                Logger.Error($"ConfigRegistrar only accepts type parameters: bool, float, KeyboardShortcut, but you supplied the type parameter {typeof(T)}.");
                yield break;
            }
            // wait until the player exists, so that we're sure every vehicle is done with registration
            yield return new UnityEngine.WaitUntil(() => Player.main != null);
            ConfigFile config = configFile;
            if (config == null)
            {
                config = MainPatcher.Instance.Config;
            }
            var vConf = ExternalVehicleConfig<T>.GetSeamothConfig();
            ConfigEntry<T> thisConf;
            try
            {
                thisConf = config.Bind<T>(SeamothName, name, defaultValue, description);
            }
            catch (Exception e)
            {
                Logger.LogException("ConfigRegistrar: Could not bind that config option. Probably you chose a non-unique name.", e);
                yield break;
            }
            if (onChange != null)
            {
                void DoThisAction(object sender, EventArgs e)
                {
                    onChange(thisConf.Value);
                }
                thisConf.SettingChanged += DoThisAction;
            }
            vConf.ExternalConfigs.Add(name, thisConf);
        }
        private static IEnumerator RegisterForPrawnInternal<T>(string name, ConfigDescription description, T defaultValue, Action<T> onChange, ConfigFile configFile)
        {
            if (typeof(T) != typeof(bool) && typeof(T) != typeof(float) && typeof(T) != typeof(KeyboardShortcut))
            {
                Logger.Error($"ConfigRegistrar only accepts type parameters: bool, float, KeyboardShortcut, but you supplied the type parameter {typeof(T)}.");
                yield break;
            }
            // wait until the player exists, so that we're sure every vehicle is done with registration
            yield return new UnityEngine.WaitUntil(() => Player.main != null);
            ConfigFile config = configFile;
            if (config == null)
            {
                config = MainPatcher.Instance.Config;
            }
            var vConf = ExternalVehicleConfig<T>.GetPrawnConfig();
            ConfigEntry<T> thisConf;
            try
            {
                thisConf = config.Bind<T>(PrawnName, name, defaultValue, description);
            }
            catch (Exception e)
            {
                Logger.LogException("ConfigRegistrar: Could not bind that config option. Probably you chose a non-unique name.", e);
                yield break;
            }
            if (onChange != null)
            {
                void DoThisAction(object sender, EventArgs e)
                {
                    onChange(thisConf.Value);
                }
                thisConf.SettingChanged += DoThisAction;
            }
            vConf.ExternalConfigs.Add(name, thisConf);
        }
        private static IEnumerator RegisterForCyclopsInternal<T>(string name, ConfigDescription description, T defaultValue, Action<T> onChange, ConfigFile configFile)
        {
            if (typeof(T) != typeof(bool) && typeof(T) != typeof(float) && typeof(T) != typeof(KeyboardShortcut))
            {
                Logger.Error($"ConfigRegistrar only accepts type parameters: bool, float, KeyboardShortcut, but you supplied the type parameter {typeof(T)}.");
                yield break;
            }
            // wait until the player exists, so that we're sure every vehicle is done with registration
            yield return new UnityEngine.WaitUntil(() => Player.main != null);
            ConfigFile config = configFile;
            if (config == null)
            {
                config = MainPatcher.Instance.Config;
            }
            var vConf = ExternalVehicleConfig<T>.GetCyclopsConfig();
            ConfigEntry<T> thisConf;
            try
            {
                thisConf = config.Bind<T>(CyclopsName, name, defaultValue, description);
            }
            catch (Exception e)
            {
                Logger.LogException("ConfigRegistrar: Could not bind that config option. Probably you chose a non-unique name.", e);
                yield break;
            }
            if (onChange != null)
            {
                void DoThisAction(object sender, EventArgs e)
                {
                    onChange(thisConf.Value);
                }
                thisConf.SettingChanged += DoThisAction;
            }
            vConf.ExternalConfigs.Add(name, thisConf);
        }
    }
}
