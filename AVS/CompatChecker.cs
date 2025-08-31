using BepInEx.Bootstrap;
using System;

namespace AVS
{
    internal static class CompatChecker
    {
        internal static void CheckAll(RootModController rmc)
        {
            try
            {
                CheckForNautilusUpdate(rmc);
                CheckForBepInExPackUpdate(rmc);
                CheckForFlareDurationIndicator(rmc);
                CheckForBuildingTweaks(rmc);
                CheckForVanillaExpanded(rmc);
            }
            catch (Exception e)
            {
                Logger.LogException("Failed to check compatibility notes.", e);
                ShowError(rmc, "Failed to check for compatibility notes. Something went wrong!");
            }
        }

        #region private_utilities
        private static void ShowError(RootModController rmc, string message)
        {
            Logger.LoopMainMenuError(message, rmc.ModName);
        }
        private static void ShowWarning(RootModController rmc, string message)
        {
            Logger.LoopMainMenuWarning(message, rmc.ModName);
        }
        #endregion

        #region checks
        private static void CheckForBepInExPackUpdate(RootModController rmc)
        {
            if (Chainloader.PluginInfos.ContainsKey("Tobey.Subnautica.ConfigHandler"))
            {
                Version target = new Version("1.0.2");
                if (Chainloader.PluginInfos["Tobey.Subnautica.ConfigHandler"].Metadata.Version.CompareTo(target) < 0)
                {
                    ShowWarning(rmc, "There is a BepInEx Pack update available!");
                }
            }
            else
            {
                ShowWarning(rmc, "There is a BepInEx Pack update available!");
            }
        }
        private static void CheckForNautilusUpdate(RootModController rmc)
        {
            Version target = new Version(Nautilus.PluginInfo.PLUGIN_VERSION);
            if (Chainloader.PluginInfos[Nautilus.PluginInfo.PLUGIN_GUID].Metadata.Version.CompareTo(target) < 0)
            {
                ShowWarning(rmc, "There is a Nautilus update available!");
            }
        }
        private static void CheckForFlareDurationIndicator(RootModController rmc)
        {
            if (Chainloader.PluginInfos.ContainsKey("com.ramune.FlareDurationIndicator"))
            {
                if (Chainloader.PluginInfos["com.ramune.FlareDurationIndicator"].Metadata.Version.ToString() == "1.0.1")
                {
                    ShowError(rmc, "Not compatible with the Flare Duration Indicator mod version 1.0.1\nPlease remove or downgrade the plugin.");
                    Logger.Log("Flare Duration Indicator 1.0.1 has a bad patch that must be fixed.");
                }
            }
        }
        private static void CheckForBuildingTweaks(RootModController rmc)
        {
            const string buildingTweaksGUID = "BuildingTweaks";
            if (Chainloader.PluginInfos.ContainsKey(buildingTweaksGUID))
            {
                ShowWarning(rmc, "Do not use BuildingTweaks to build things inside/on AVS submarines!");
                Logger.Log("Using some BuildingTweaks options to build things inside submarines can prevent those buildables from correctly anchoring to the submarine. Be careful.");
            }
        }
        private static void CheckForVanillaExpanded(RootModController rmc)
        {
            const string vanillaExpandedGUID = "VanillaExpanded";
            if (Chainloader.PluginInfos.ContainsKey(vanillaExpandedGUID))
            {
                ShowError(rmc, "Some vehicles not compatible with Vanilla Expanded!");
                Logger.Log("Vanilla Expanded has a patch on UniqueIdentifier.Awake that throws an error (dereferences null) during many AVS setup methods. If you choose to continue, some vehicles, buildables, and fragments may simply not appear.");
            }
        }
        #endregion
    }
}
