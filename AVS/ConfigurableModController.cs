using AVS.Configuration;
using AVS.Localization;
using AVS.Log;
using AVS.Util;
using Nautilus.Handlers;
using Nautilus.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.Remoting.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace AVS
{
    /// <summary>
    /// A variant of RootModController that supports configuration via a ConfigType.
    /// </summary>
    /// <typeparam name="ConfigType">Configuration type to register</typeparam>
    public abstract class ConfigurableModController<ConfigType> : RootModController where ConfigType : ConfigFile, new()
    {
        private static ConfigType? config;

        /// <summary>
        /// Attempts to retrieve the current configuration, if available.
        /// </summary>
        /// <returns>The current configuration if it has been set; otherwise, <see
        /// langword="null"/>.</returns>
        public static ConfigType? TryGetConfig() => config;
        /// <summary>
        /// The configuration instance registered with all controllers of same config type
        /// </summary>
        public static ConfigType PluginConfig => config ?? throw new NullReferenceException(typeof(ConfigType).Name+ " not initialized");

        /// <inheritdoc/>
        public void LoadLanguagesAndConfig(string languageFolderName = "Localization")
        {
            using var log = SmartLog.ForAVS(this);

            var aType = typeof(BindableButtonAttribute);
            var jType = typeof(JsonIgnoreAttribute);

            var languageFolder = Path.Combine(Path.GetDirectoryName( typeof(ConfigType).Assembly.Location), languageFolderName);
            
            log.Write($"Registering localization folder at '{languageFolder}'");

            LanguageHandler.RegisterLocalizationFolder(languageFolder);

            if (!Directory.Exists(languageFolder))
            {
                log.Warn($"Language folder '{languageFolder}' does not exist. Skipping language loading.");
            }

            config = OptionsPanelHandler.RegisterModOptions<ConfigType>();

            var languages = Directory.Exists(languageFolder) ? new DirectoryInfo(languageFolder).GetFiles("*.json")
                .Select(lang =>
                {
                    var l = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                        File.ReadAllText(lang.FullName)
                        );
                    return (Name: Path.GetFileNameWithoutExtension(lang.Name), Dict: l);
                }).ToRoList()
                : [];
            

            foreach (var f in typeof(ConfigType).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
            {
                var b = Attribute.GetCustomAttribute(f, aType) as BindableButtonAttribute;
                if (b is null)
                    continue;
                try
                {

                    if (EnumHandler.TryAddEntry<GameInput.Button>($"AVS_Button_{ModName}_{f.Name}", out var builder))
                    {
                        bool anyTranslation = false;
                        if (!b.LabelLocalizationKey.IsNullOrEmpty() && languages.Count > 0)
                        {
                            foreach (var lang in languages)
                            {
                                lang.Dict.TryGetValue(b.LabelLocalizationKey, out var translated);
                                string? toolTipTranslated = null;
                                if (!b.TooltipLocalizationKey.IsNullOrEmpty())
                                    lang.Dict.TryGetValue(b.TooltipLocalizationKey, out toolTipTranslated);
                                if (!translated.IsNullOrEmpty())
                                {
                                    builder = builder.CreateInput(displayName: translated, tooltip: toolTipTranslated??"", language: lang.Name);
                                    anyTranslation = true;
                                }
                            }
                        }

                        if (!anyTranslation)
                            builder = builder.CreateInput(b.Name);

                        if (!b.KeyboardDefault.IsNullOrEmpty())
                            builder = builder.WithKeyboardBinding(b.KeyboardDefault);
                        else
                            builder = builder.WithKeyboardBinding("None");
                        if (!b.GamepadDefault.IsNullOrEmpty())
                            builder = builder.WithControllerBinding(b.GamepadDefault);
                        else
                            builder = builder.WithControllerBinding("None");

                        builder.WithCategory(ModName);
                        f.SetValue(config, builder.Value);
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"Error registering bindable button for field '{f.Name}': {ex}");
                }
            }

        }

    }
}
