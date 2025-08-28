using BepInEx.Bootstrap;
using System;

namespace AVS
{
    internal static class CompatChecker
    {
        internal static void CheckAll(MainPatcher mp)
        {
            try
            {
                CheckForNautilusUpdate(mp);
                CheckForBepInExPackUpdate(mp);
                CheckForFlareDurationIndicator(mp);
                CheckForBuildingTweaks(mp);
                CheckForVanillaExpanded(mp);
            }
            catch (Exception e)
            {
                Logger.LogException("Failed to check compatibility notes.", e);
                ShowError(mp, "Failed to check for compatibility notes. Something went wrong!");
            }
        }

        #region private_utilities
        private static void ShowError(MainPatcher mp, string message)
        {
            Logger.LoopMainMenuError(message, mp.ModName);
        }
        private static void ShowWarning(MainPatcher mp, string message)
        {
            Logger.LoopMainMenuWarning(message, mp.ModName);
        }
        #endregion

        #region checks
        private static void CheckForBepInExPackUpdate(MainPatcher mp)
        {
            if (Chainloader.PluginInfos.ContainsKey("Tobey.Subnautica.ConfigHandler"))
            {
                Version target = new Version("1.0.2");
                if (Chainloader.PluginInfos["Tobey.Subnautica.ConfigHandler"].Metadata.Version.CompareTo(target) < 0)
                {
                    ShowWarning(mp, "There is a BepInEx Pack update available!");
                }
            }
            else
            {
                ShowWarning(mp, "There is a BepInEx Pack update available!");
            }
        }
        private static void CheckForNautilusUpdate(MainPatcher mp)
        {
            Version target = new Version(Nautilus.PluginInfo.PLUGIN_VERSION);
            if (Chainloader.PluginInfos[Nautilus.PluginInfo.PLUGIN_GUID].Metadata.Version.CompareTo(target) < 0)
            {
                ShowWarning(mp, "There is a Nautilus update available!");
            }
        }
        private static void CheckForFlareDurationIndicator(MainPatcher mp)
        {
            if (Chainloader.PluginInfos.ContainsKey("com.ramune.FlareDurationIndicator"))
            {
                if (Chainloader.PluginInfos["com.ramune.FlareDurationIndicator"].Metadata.Version.ToString() == "1.0.1")
                {
                    ShowError(mp, "Not compatible with the Flare Duration Indicator mod version 1.0.1\nPlease remove or downgrade the plugin.");
                    Logger.Log("Flare Duration Indicator 1.0.1 has a bad patch that must be fixed.");
                }
            }
        }
        private static void CheckForBuildingTweaks(MainPatcher mp)
        {
            const string buildingTweaksGUID = "BuildingTweaks";
            if (Chainloader.PluginInfos.ContainsKey(buildingTweaksGUID))
            {
                ShowWarning(mp, "Do not use BuildingTweaks to build things inside/on AVS submarines!");
                Logger.Log("Using some BuildingTweaks options to build things inside submarines can prevent those buildables from correctly anchoring to the submarine. Be careful.");
            }
        }
        private static void CheckForVanillaExpanded(MainPatcher mp)
        {
            const string vanillaExpandedGUID = "VanillaExpanded";
            if (Chainloader.PluginInfos.ContainsKey(vanillaExpandedGUID))
            {
                ShowError(mp, "Some vehicles not compatible with Vanilla Expanded!");
                Logger.Log("Vanilla Expanded has a patch on UniqueIdentifier.Awake that throws an error (dereferences null) during many AVS setup methods. If you choose to continue, some vehicles, buildables, and fragments may simply not appear.");
            }
        }
        #endregion
    }
}
