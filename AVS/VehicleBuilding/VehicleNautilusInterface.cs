using AVS.Configuration;
using AVS.Util;
using Nautilus.Assets.Gadgets;
using System.IO;
using System.Reflection;

namespace AVS;

internal static class VehicleNautilusInterface
{
    internal static TechType RegisterVehicle(VehicleEntry vehicle)
    {
        var vehicleKey = vehicle.AV.name;
        var vehicle_info =
            Nautilus.Assets.PrefabInfo.WithTechType(vehicleKey, vehicleKey, vehicle.AV.Config.Description);
        vehicle_info.WithIcon(vehicle.AV.Config.CraftingSprite);

        var module_CustomPrefab = new Nautilus.Assets.CustomPrefab(vehicle_info);
        Nautilus.Utility.PrefabUtils.AddBasicComponents(vehicle.AV.VehicleRoot, vehicleKey, vehicle_info.TechType,
            LargeWorldEntity.CellLevel.Global);
        module_CustomPrefab.SetGameObject(vehicle.AV.VehicleRoot);
        var jsonRecipeFileName = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "",
            "recipes",
            $"{vehicleKey}_recipe.json");
        var vehicleRecipe = Nautilus.Utility.JsonUtils.Load<Nautilus.Crafting.RecipeData>(jsonRecipeFileName, false,
            new Nautilus.Json.Converters.CustomEnumConverter());
        if (vehicleRecipe.Ingredients.Count == 0)
        {
            // If the custom recipe file doesn't exist, go ahead and make it using the default recipe.
            vehicleRecipe = vehicle.AV.Config.Recipe.ToRecipeData();
            Nautilus.Utility.JsonUtils.Save(vehicleRecipe, jsonRecipeFileName,
                new Nautilus.Json.Converters.CustomEnumConverter());
        }
        else if (vehicle.AV.Config.AllowRecipeOverride)
        {
            vehicleRecipe = vehicle.AV.OnRecipeOverride(
                Recipe.Import(vehicleRecipe, vehicle.AV.Config.Recipe)
            ).ToRecipeData();
            Nautilus.Utility.JsonUtils.Save(vehicleRecipe, jsonRecipeFileName,
                new Nautilus.Json.Converters.CustomEnumConverter());
        }
        else
        {
            vehicleRecipe = vehicle.AV.Config.Recipe.ToRecipeData();
            Nautilus.Utility.JsonUtils.Save(vehicleRecipe, jsonRecipeFileName,
                new Nautilus.Json.Converters.CustomEnumConverter());
        }

        module_CustomPrefab.SetRecipe(vehicleRecipe).WithFabricatorType(CraftTree.Type.Constructor)
            .WithStepsToFabricatorTab("Vehicles");
        var scanningGadget = module_CustomPrefab.SetUnlock(vehicle.AV.Config.UnlockedWith)
            .WithPdaGroupCategory(TechGroup.Constructor, TechCategory.Constructor);

        if (!string.IsNullOrEmpty(vehicle.AV.Config.EncyclopediaEntry))
        {
            Nautilus.Handlers.LanguageHandler.SetLanguageLine($"Ency_{vehicleKey}", vehicleKey);
            Nautilus.Handlers.LanguageHandler.SetLanguageLine($"EncyDesc_{vehicleKey}",
                vehicle.AV.Config.EncyclopediaEntry);
            scanningGadget.WithEncyclopediaEntry("Tech/Vehicles", null,
                vehicle.AV.Config.EncyclopediaImage.SafeGetTexture2D());
            Nautilus.Handlers.StoryGoalHandler.RegisterItemGoal(vehicleKey, Story.GoalType.Encyclopedia,
                vehicle.AV.Config.UnlockedWith);
        }

        if (vehicle.AV.Config.UnlockedSprite.IsNotNull())
            scanningGadget.WithAnalysisTech(vehicle.AV.Config.UnlockedSprite,
                unlockMessage: vehicle.AV.Config.UnlockedMessage);
        module_CustomPrefab.Register();
        return vehicle_info.TechType;
    }

    internal static void PatchCraftable(ref VehicleEntry ve, bool verbose)
    {
        using var log = ve.AV.NewAvsLog();
        try
        {
            var techType = RegisterVehicle(ve);
            VehicleRegistrar.VerboseLog(log, VehicleRegistrar.LogType.Log, verbose, $"Patched the {ve.Name} Craftable");
            var newVE = new VehicleEntry(ve.MainPatcher, ve.AV, ve.UniqueId, ve.PingType, ve.PingSprite, techType);
            AvsVehicleManager.Add(newVE);
        }
        catch (System.Exception e)
        {
            log.Error($"VehicleNautilusInterface Error: Failed to Register Vehicle {ve.Name}. Error follows:", e);
            Logger.LoopMainMenuError($"Failed registration. See log.", ve.Name);
        }
    }
}