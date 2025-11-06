using AVS.Configuration;
using AVS.Log;
using AVS.Util;
using Nautilus.Assets.Gadgets;
using Nautilus.Crafting;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace AVS;

internal static class VehicleNautilusInterface
{
    internal static TechType RegisterVehicle(VehicleEntry vehicle)
    {
        using var log = SmartLog.ForAVS(vehicle.RMC, [vehicle.UniqueId.ToString()]);

        log.Debug($"Registering vehicle '{vehicle.Name}' with Nautilus.");

        try
        {
            var vehicleKey = vehicle.AV.name;
            var prefabInfo = Nautilus.Assets.PrefabInfo
                    .WithTechType(vehicleKey, vehicleKey, vehicle.AV.Config.Description)
                    .WithIcon(vehicle.AV.Config.CraftingSprite);

            var customPrefab = new Nautilus.Assets.CustomPrefab(prefabInfo);
            Nautilus.Utility.PrefabUtils.AddBasicComponents(vehicle.AV.VehicleRoot, vehicleKey, prefabInfo.TechType,
                LargeWorldEntity.CellLevel.Global);
            customPrefab.SetGameObject(vehicle.AV.VehicleRoot);
            var jsonRecipeFileName = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "",
                "recipes",
                $"{vehicleKey}_recipe.json");

            var loadedRecipe = vehicle.AV.Config.AllowRecipeOverride
                ? Nautilus.Utility.JsonUtils.Load<List<RecipeIngredient>>(jsonRecipeFileName, false, new Nautilus.Json.Converters.CustomEnumConverter())
                : null;

            RecipeData vehicleRecipe;

            if (loadedRecipe is null || loadedRecipe.Count == 0)
            {
                if (!vehicle.AV.Config.AllowRecipeOverride)
                    log.Debug(
                        $"Recipe override not permitted for vehicle.");
                else
                {
                    if (loadedRecipe is null)
                        log.Debug(
                            $"No custom recipe file found.");
                    else if (loadedRecipe.Count == 0)
                        log.Debug(
                            $"Custom recipe file is empty or does not exist.");
                }
                log.Debug(
                        $"Creating default recipe file at {jsonRecipeFileName}.");
                // If the custom recipe file doesn't exist, go ahead and make it using the default recipe.
                Nautilus.Utility.JsonUtils.Save(vehicle.AV.Config.Recipe, jsonRecipeFileName,
                    new Nautilus.Json.Converters.CustomEnumConverter());
                vehicleRecipe = vehicle.AV.Config.Recipe.ToRecipeData();
            }
            else
            {
                log.Debug(
                    $"Custom recipe file found and permitted. Applying recipe overrides and saving updated recipe to:");
                log.Debug(jsonRecipeFileName);
                vehicleRecipe = vehicle.AV.OnRecipeOverride(new Recipe(loadedRecipe)).ToRecipeData();
                Nautilus.Utility.JsonUtils.Save(vehicleRecipe, jsonRecipeFileName,
                    new Nautilus.Json.Converters.CustomEnumConverter());
            }

            customPrefab
                .SetRecipe(vehicleRecipe)
                .WithFabricatorType(CraftTree.Type.Constructor)
                .WithStepsToFabricatorTab("Vehicles");

            var scanningGadget = customPrefab
                .SetUnlock(vehicle.AV.Config.UnlockedWith)
                .WithPdaGroupCategory(TechGroup.Constructor, TechCategory.Constructor);

            if (!string.IsNullOrEmpty(vehicle.AV.Config.EncyclopediaEntry))
            {
                log.Debug($"Adding encyclopedia entry for vehicle {vehicle.Name}.");

                Nautilus.Handlers.LanguageHandler.SetLanguageLine($"Ency_{vehicleKey}", vehicleKey);
                Nautilus.Handlers.LanguageHandler.SetLanguageLine($"EncyDesc_{vehicleKey}",
                    vehicle.AV.Config.EncyclopediaEntry);
                scanningGadget.WithEncyclopediaEntry("Tech/Vehicles", null,
                    vehicle.AV.Config.EncyclopediaImage.SafeGetTexture2D());
                Nautilus.Handlers.StoryGoalHandler.RegisterItemGoal(vehicleKey, Story.GoalType.Encyclopedia,
                    vehicle.AV.Config.UnlockedWith);
            }
            else
                log.Debug($"No encyclopedia entry defined for vehicle {vehicle.Name}.");

            if (vehicle.AV.Config.UnlockedSprite.IsNotNull())
                scanningGadget.WithAnalysisTech(
                    vehicle.AV.Config.UnlockedSprite,
                    unlockMessage: vehicle.AV.Config.UnlockedMessage);
            log.Debug($".Register()");
            customPrefab.Register();

            log.Write($"Registered vehicle '{vehicle.Name}' with TechType {prefabInfo.TechType} ({(int)prefabInfo.TechType}).");

            return prefabInfo.TechType;
        }
        catch (System.Exception e)
        {
            log.Error($"VehicleNautilusInterface Error: Failed to Register Vehicle {vehicle.Name}", e);
            throw;
        }
    }

    internal static void PatchCraftable(ref VehicleEntry ve, bool verbose)
    {
        using var log = ve.AV.NewAvsLog();
        try
        {
            var techType = RegisterVehicle(ve);
            VehicleRegistrar.VerboseLog(log, VehicleRegistrar.LogType.Log, verbose, $"Patched the {ve.Name} Craftable");
            var newVE = new VehicleEntry(ve.RMC, ve.AV, ve.UniqueId, ve.PingType, ve.PingSprite, techType);
            AvsVehicleManager.Add(newVE);
        }
        catch (System.Exception e)
        {
            log.Error($"VehicleNautilusInterface Error: Failed to Register Vehicle {ve.Name}. Error follows:", e);
            Logger.LoopMainMenuError($"Failed registration. See log.", ve.Name);
        }
    }
}