using AVS.BaseVehicle;
using AVS.Log;
using AVS.Util;
using BepInEx.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AVS.Admin;

/// <summary>
/// Represents a manager for additional external configurations to be applied to vehicles.
/// </summary>
/// <typeparam name="T">The type of the configuration values managed by this instance.</typeparam>
public class ExternalVehicleConfig<T>
{
    internal string MyName = "";
    internal Dictionary<string, ConfigEntry<T>> ExternalConfigs = new();

    internal static Dictionary<string, ExternalVehicleConfig<T>> main = new();
    internal static ExternalVehicleConfig<T>? SeamothConfig = null;
    internal static ExternalVehicleConfig<T>? PrawnConfig = null;
    internal static ExternalVehicleConfig<T>? CyclopsConfig = null;

    private static ExternalVehicleConfig<T> AddNew(AvsVehicle av)
    {
        var thisConf = new ExternalVehicleConfig<T>
        {
            MyName = av.GetType().ToString()
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
            return ExternalConfigs[name].Value;
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
    public static ExternalVehicleConfig<T> GetAvsVehicleConfig(string vehicleName)
    {
        var MVs = AvsVehicleManager.VehicleTypes
            .Where(x => x.Name.Equals(vehicleName, StringComparison.OrdinalIgnoreCase)).ToList();
        if (MVs.Count == 0)
        {
            var sb = new StringBuilder();
            AvsVehicleManager.VehicleTypes.ForEach(x => sb.AppendLine(x.Name));
            throw new ArgumentException(
                $"{nameof(GetAvsVehicleConfig)}: vehicle name does not identify a {nameof(AvsVehicle)}: {vehicleName}. Options are: {sb}");
        }

        if (MVs.Count > 1)
        {
            var sb = new StringBuilder();
            AvsVehicleManager.VehicleTypes.ForEach(x => sb.AppendLine(x.Name));
            throw new ArgumentException(
                $"{nameof(GetAvsVehicleConfig)}: vehicle name does not uniquely identify a {nameof(AvsVehicle)}: {vehicleName}. There were {MVs.Count()} matches: {sb}");
        }

        var av = MVs[0].AV;
        if (!main.ContainsKey(av.GetType().ToString()))
            AddNew(av);
        return main[av.GetType().ToString()];
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
        if (SeamothConfig is null)
            SeamothConfig = new ExternalVehicleConfig<T>
            {
                MyName = ConfigRegistrar.SeamothName
            };
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
        if (PrawnConfig is null)
            PrawnConfig = new ExternalVehicleConfig<T>
            {
                MyName = ConfigRegistrar.PrawnName
            };
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
        if (CyclopsConfig is null)
            CyclopsConfig = new ExternalVehicleConfig<T>
            {
                MyName = ConfigRegistrar.CyclopsName
            };
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
    public static void LogAllVehicleNames(RootModController rmc)
    {
        rmc.StartAvsCoroutine(nameof(ConfigRegistrar) + '.' + nameof(LogAllVehicleNamesInternal), LogAllVehicleNamesInternal);
    }

    private static IEnumerator LogAllVehicleNamesInternal(SmartLog log)
    {
        // wait until the player exists, so that we're sure every vehicle is done with registration
        yield return new UnityEngine.WaitUntil(() => Player.main.IsNotNull());
        var boolNames = ExternalVehicleConfig<bool>.main.Keys;
        var floatNames = ExternalVehicleConfig<float>.main.Keys;
        var keyNames = ExternalVehicleConfig<KeyboardShortcut>.main.Keys;
        var result = boolNames.Concat(floatNames).Concat(keyNames).Distinct().ToList();
        result.Add(SeamothName);
        result.Add(PrawnName);
        result.Add(CyclopsName);
        log.Write("Logging all vehicle type names:");
        result.ForEach(x => log.Write(x));
    }

    /// <summary>
    /// Registers a configuration option for all modded vehicles in the game.
    /// </summary>
    /// <typeparam name="T">The type of the configuration value.</typeparam>
    /// <param name="rmc">The owning root mod controller.</param>
    /// <param name="name">The name of the configuration option.</param>
    /// <param name="description">A description of the configuration option, including its purpose and usage.</param>
    /// <param name="defaultValue">The default value for the configuration option.</param>
    /// <param name="OnChange">An optional callback invoked when the configuration value changes. The callback receives the <see
    /// cref="TechType"/> of the vehicle and the new value.</param>
    /// <param name="configFile">An optional configuration file to store the setting. If not provided, a default configuration file is used.</param>
    public static void RegisterForAllAvsVehicles<T>(RootModController rmc, string name, ConfigDescription description, T defaultValue,
        Action<TechType, T>? OnChange = null, ConfigFile? configFile = null)
    {
        rmc.StartAvsCoroutine(nameof(ConfigRegistrar) + '.' + nameof(RegisterForAllInternal), log => RegisterForAllInternal<T>(log, rmc, name, description, defaultValue, OnChange,
            configFile));
    }

    /// <summary>
    /// Registers a configuration option for a specific modded vehicle.
    /// </summary>
    /// <remarks>This method initiates the registration process asynchronously. The configuration
    /// option will be associated with the specified modded vehicle and can be accessed or modified through the
    /// configuration system.</remarks>
    /// <typeparam name="T">The type of the configuration value. Must be a type supported by the configuration system.</typeparam>
    /// <param name="rmc">The owning root mod controller.</param>
    /// <param name="vehicleName">The name of the modded vehicle for which the configuration option is being registered.</param>
    /// <param name="name">The name of the configuration option.</param>
    /// <param name="description">A description of the configuration option, including details such as its purpose or valid range.</param>
    /// <param name="defaultValue">The default value for the configuration option.</param>
    /// <param name="OnChange">An optional callback that is invoked when the configuration value changes. The callback receives the <see
    /// cref="TechType"/> of the vehicle and the new value of the configuration option.</param>
    /// <param name="configFile">An optional <see cref="ConfigFile"/> instance to store the configuration option. If not provided, the
    /// default configuration file is used.</param>
    public static void RegisterForAvsVehicle<T>(RootModController rmc, string vehicleName, string name, ConfigDescription description,
        T defaultValue, Action<TechType, T>? OnChange = null, ConfigFile? configFile = null)
    {
        rmc.StartAvsCoroutine(nameof(ConfigRegistrar) + '.' + nameof(RegisterForVehicleInternal), log => RegisterForVehicleInternal<T>(log, rmc, vehicleName, name, description, defaultValue,
            OnChange, configFile));
    }

    /// <summary>
    /// Registers a configuration option for the Seamoth vehicle.
    /// </summary>
    /// <remarks>This method initiates the registration process asynchronously. The configuration
    /// option will be associated with the Seamoth vehicle and can be used to customize its behavior or
    /// settings.</remarks>
    /// <typeparam name="T">The type of the configuration value.</typeparam>
    /// <param name="rmc">The root mod controller instance used to start the coroutine for registration.</param>
    /// <param name="name">The unique name of the configuration option.</param>
    /// <param name="description">A description of the configuration option, including its purpose and constraints.</param>
    /// <param name="defaultValue">The default value for the configuration option.</param>
    /// <param name="onChange">An optional callback that is invoked when the configuration value changes. The new value is passed as a
    /// parameter.</param>
    /// <param name="configFile">An optional configuration file to store the setting. If not provided, a default configuration file is used.</param>
    public static void RegisterForSeamoth<T>(RootModController rmc, string name, ConfigDescription description, T defaultValue,
        Action<T>? onChange = null, ConfigFile? configFile = null)
    {
        rmc.StartAvsCoroutine(nameof(ConfigRegistrar) + '.' + nameof(RegisterForSeamothInternal), log => RegisterForSeamothInternal<T>(log, rmc, name, description, defaultValue, onChange,
            configFile));
    }

    /// <summary>
    /// Registers a configuration option for the Prawn with the specified name, description, and default
    /// value.
    /// </summary>
    /// <remarks>This method starts a coroutine to handle the registration process asynchronously. The
    /// registration ensures that the configuration option is properly integrated with the Prawn and its
    /// associated systems.</remarks>
    /// <typeparam name="T">The type of the configuration value. Must be a type supported by the configuration system.</typeparam>
    /// <param name="rmc">The owning root mod controller.</param>
    /// <param name="name">The unique name of the configuration option. This name is used to identify the option.</param>
    /// <param name="description">A description of the configuration option, including details such as its purpose or valid range of values.</param>
    /// <param name="defaultValue">The default value for the configuration option. This value is used if no other value is provided.</param>
    /// <param name="onChange">An optional callback that is invoked whenever the configuration value changes. The new value is passed as a
    /// parameter to the callback.</param>
    /// <param name="configFile">An optional configuration file object where the configuration option will be stored. If not provided, a
    /// default configuration file is used.</param>
    public static void RegisterForPrawn<T>(RootModController rmc, string name, ConfigDescription description, T defaultValue,
        Action<T>? onChange = null, ConfigFile? configFile = null)
    {
        rmc.StartAvsCoroutine(nameof(ConfigRegistrar) + '.' + nameof(RegisterForPrawnInternal), log => RegisterForPrawnInternal<T>(log, rmc, name, description, defaultValue, onChange,
            configFile));
    }

    /// <summary>
    /// Registers a configuration option for the Cyclops submarine with the specified name, description, and default
    /// value.
    /// </summary>
    /// <remarks>This method starts a coroutine to handle the registration process asynchronously. The
    /// configuration option will be available for use after the coroutine completes.</remarks>
    /// <typeparam name="T">The type of the configuration value.</typeparam>
    /// <param name="rmc">The owning root mod controller.</param>
    /// <param name="name">The unique name of the configuration option. This name is used to identify the option.</param>
    /// <param name="description">A description of the configuration option, including details such as its purpose or valid range.</param>
    /// <param name="defaultValue">The default value for the configuration option.</param>
    /// <param name="OnChange">An optional callback that is invoked whenever the configuration value changes. The new value is passed as a
    /// parameter to the callback.</param>
    /// <param name="configFile">An optional configuration file where the option will be stored. If not provided, a default configuration
    /// file is used.</param>
    public static void RegisterForCyclops<T>(RootModController rmc, string name, ConfigDescription description, T defaultValue,
        Action<T>? OnChange = null, ConfigFile? configFile = null)
    {
        rmc.StartAvsCoroutine(nameof(ConfigRegistrar) + '.' + nameof(RegisterForCyclopsInternal), log => RegisterForCyclopsInternal<T>(log, rmc, name, description, defaultValue, OnChange,
            configFile));
    }

    private static IEnumerator RegisterForAllInternal<T>(SmartLog log, RootModController rmc, string name, ConfigDescription description, T defaultValue,
        Action<TechType, T>? OnChange = null, ConfigFile? configFile = null)
    {
        if (typeof(T) != typeof(bool) && typeof(T) != typeof(float) && typeof(T) != typeof(KeyboardShortcut))
        {
            log.Error(
                $"ConfigRegistrar only accepts type parameters: bool, float, KeyboardShortcut, but you supplied the type parameter {typeof(T)}.");
            yield break;
        }

        // wait until the player exists, so that we're sure every vehicle is done with registration
        yield return new UnityEngine.WaitUntil(() => Player.main.IsNotNull());
        foreach (var pair in ExternalVehicleConfig<T>.main)
        {
            var config = configFile ?? rmc.Config;
            var vConf = pair.Value;
            var vehicleName = pair.Key;
            ConfigEntry<T> thisConf;
            try
            {
                thisConf = config.Bind<T>(vehicleName, name, defaultValue, description);
            }
            catch (Exception e)
            {
                log.Error(
                    "ConfigRegistrar: Could not bind that config option. Probably you chose a non-unique name.", e);
                yield break;
            }

            if (OnChange.IsNotNull())
            {
                void DoThisAction(object sender, EventArgs e)
                {
                    foreach (var innerMV in AvsVehicleManager.VehicleTypes.Select(x => x.AV))
                        if (innerMV.GetType().ToString() == vehicleName)
                        {
                            OnChange(innerMV.TechType, thisConf.Value);
                            break;
                        }
                }

                thisConf.SettingChanged += DoThisAction;
            }

            vConf.ExternalConfigs.Add(name, thisConf);
        }
    }

    private static IEnumerator RegisterForVehicleInternal<T>(SmartLog log, RootModController rmc, string vehicleName, string name,
        ConfigDescription description, T defaultValue, Action<TechType, T>? onChange, ConfigFile? configFile)
    {
        if (typeof(T) != typeof(bool) && typeof(T) != typeof(float) && typeof(T) != typeof(KeyboardShortcut))
        {
            log.Error(
                $"ConfigRegistrar only accepts type parameters: bool, float, KeyboardShortcut, but you supplied the type parameter {typeof(T)}.");
            yield break;
        }

        // wait until the player exists, so that we're sure every vehicle is done with registration
        yield return new UnityEngine.WaitUntil(() => Player.main.IsNotNull());
        var MVs = AvsVehicleManager.VehicleTypes.Where(x => x.Name.ToLower().Contains(vehicleName.ToLower()));
        if (!MVs.Any())
            throw new ArgumentException(
                $"{nameof(RegisterForVehicleInternal)}: vehicle name does not identify a {nameof(AvsVehicle)}: {vehicleName}");
        if (MVs.Count() > 1)
            throw new ArgumentException(
                $"{nameof(RegisterForVehicleInternal)}: vehicle name does not uniquely identify a {nameof(AvsVehicle)}: {vehicleName}. There were {MVs.Count()} matches.");
        var av = MVs.First().AV;
        var config = configFile;
        if (config is null)
            config = rmc.Config;
        var vConf = ExternalVehicleConfig<T>.GetAvsVehicleConfig(vehicleName);
        ConfigEntry<T> thisConf;
        try
        {
            thisConf = config.Bind<T>(vConf.MyName, name, defaultValue, description);
        }
        catch (Exception e)
        {
            log.Error(
                "ConfigRegistrar: Could not bind that config option. Probably you chose a non-unique name.", e);
            yield break;
        }

        if (onChange.IsNotNull())
        {
            void DoThisAction(object sender, EventArgs e)
            {
                onChange(av.TechType, thisConf.Value);
            }

            thisConf.SettingChanged += DoThisAction;
        }

        vConf.ExternalConfigs.Add(name, thisConf);
    }

    private static IEnumerator RegisterForSeamothInternal<T>(SmartLog log, RootModController rmc, string name, ConfigDescription description, T defaultValue,
        Action<T>? onChange, ConfigFile? configFile)
    {
        if (typeof(T) != typeof(bool) && typeof(T) != typeof(float) && typeof(T) != typeof(KeyboardShortcut))
        {
            log.Error(
                $"ConfigRegistrar only accepts type parameters: bool, float, KeyboardShortcut, but you supplied the type parameter {typeof(T)}.");
            yield break;
        }

        // wait until the player exists, so that we're sure every vehicle is done with registration
        yield return new UnityEngine.WaitUntil(() => Player.main.IsNotNull());
        var config = configFile ?? rmc.Config;
        var vConf = ExternalVehicleConfig<T>.GetSeamothConfig();
        ConfigEntry<T> thisConf;
        try
        {
            thisConf = config.Bind<T>(SeamothName, name, defaultValue, description);
        }
        catch (Exception e)
        {
            log.Error(
                "ConfigRegistrar: Could not bind that config option. Probably you chose a non-unique name.", e);
            yield break;
        }

        if (onChange.IsNotNull())
        {
            void DoThisAction(object sender, EventArgs e)
            {
                onChange(thisConf.Value);
            }

            thisConf.SettingChanged += DoThisAction;
        }

        vConf.ExternalConfigs.Add(name, thisConf);
    }

    private static IEnumerator RegisterForPrawnInternal<T>(SmartLog log, RootModController rmc, string name, ConfigDescription description, T defaultValue,
        Action<T>? onChange, ConfigFile? configFile)
    {
        if (typeof(T) != typeof(bool) && typeof(T) != typeof(float) && typeof(T) != typeof(KeyboardShortcut))
        {
            log.Error(
                $"ConfigRegistrar only accepts type parameters: bool, float, KeyboardShortcut, but you supplied the type parameter {typeof(T)}.");
            yield break;
        }

        // wait until the player exists, so that we're sure every vehicle is done with registration
        yield return new UnityEngine.WaitUntil(() => Player.main.IsNotNull());
        var config = configFile ?? rmc.Config;
        var vConf = ExternalVehicleConfig<T>.GetPrawnConfig();
        ConfigEntry<T> thisConf;
        try
        {
            thisConf = config.Bind<T>(PrawnName, name, defaultValue, description);
        }
        catch (Exception e)
        {
            log.Error(
                "ConfigRegistrar: Could not bind that config option. Probably you chose a non-unique name.", e);
            yield break;
        }

        if (onChange.IsNotNull())
        {
            void DoThisAction(object sender, EventArgs e)
            {
                onChange(thisConf.Value);
            }

            thisConf.SettingChanged += DoThisAction;
        }

        vConf.ExternalConfigs.Add(name, thisConf);
    }

    private static IEnumerator RegisterForCyclopsInternal<T>(SmartLog log, RootModController rmc, string name, ConfigDescription description, T defaultValue,
        Action<T>? onChange, ConfigFile? configFile)
    {
        if (typeof(T) != typeof(bool) && typeof(T) != typeof(float) && typeof(T) != typeof(KeyboardShortcut))
        {
            log.Error(
                $"ConfigRegistrar only accepts type parameters: bool, float, KeyboardShortcut, but you supplied the type parameter {typeof(T)}.");
            yield break;
        }

        // wait until the player exists, so that we're sure every vehicle is done with registration
        yield return new UnityEngine.WaitUntil(() => Player.main.IsNotNull());
        var config = configFile;
        if (config is null)
            config = rmc.Config;
        var vConf = ExternalVehicleConfig<T>.GetCyclopsConfig();
        ConfigEntry<T> thisConf;
        try
        {
            thisConf = config.Bind<T>(CyclopsName, name, defaultValue, description);
        }
        catch (Exception e)
        {
            log.Error(
                "ConfigRegistrar: Could not bind that config option. Probably you chose a non-unique name.", e);
            yield break;
        }

        if (onChange.IsNotNull())
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