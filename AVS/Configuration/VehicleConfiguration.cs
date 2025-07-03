using AVS.Assets;
using UnityEngine;
using static AVS.ModVehicle;

namespace AVS.Configuration
{
    public class VehicleConfiguration
    {
        /// <summary>
        /// Sprite to show when the camera is sufficiently far away.
        /// Also used on the map, if used.
        /// </summary>
        public Atlas.Sprite PingSprite { get; } = Assets.StaticAssets.DefaultPingSprite;
        /// <summary>
        /// Sprite to attach to the save file in the preview.
        /// Should be very abstract, ideally just an outline.
        /// </summary>
        public Sprite SaveFileSprite { get; } = Assets.StaticAssets.DefaultSaveFileSprite;
        /// <summary>
        /// Construction recipe.
        /// </summary>
        public Recipe Recipe { get; } = Recipe.Example;
        /// <summary>
        /// If true, the recipe can be overridden by a JSON file created in the "recipes" folder.
        /// If so, the imported recipe is passed to <see cref="ModVehicle.OnRecipeOverride(Nautilus.Crafting.RecipeData)"/> before being applied.
        /// </summary>
        public bool AllowRecipeOverride { get; } = true;
        public Sprite UnlockedSprite { get; } = null;
        /// <summary>
        /// Localized description of the vehicle.
        /// </summary>
        public string Description { get; } = "A vehicle";
        /// <summary>
        /// Localized encyclopedia entry for this vehicle.
        /// </summary>
        public string EncyclopediaEntry { get; } = "";
        /// <summary>
        /// Image to show in the encyclopedia entry, if any.
        /// </summary>
        public Sprite EncyclopediaImage { get; } = null;
        /// <summary>
        /// The sprite to show in the crafting menu of the mobile vehicle bay.
        /// </summary>
        public Atlas.Sprite CraftingSprite { get; } = StaticAssets.ModVehicleIcon;
        /// <summary>
        /// The image to show in the background of the vehicle's module menu.
        /// </summary>
        public Sprite ModuleBackgroundImage { get; } = SpriteHelper.GetSpriteRaw("Sprites/VFModuleBackground.png");
        /// <summary>
        /// Type that, if unlocked, also automatically unlocks this vehicle for crafting.
        /// </summary>
        public TechType UnlockedWith { get; } = TechType.Constructor;
        /// <summary>
        /// Maximum health of the vehicle.
        /// 100 is very low.
        /// </summary>
        public int MaxHealth { get; } = 100;    //required > 0
        /// <summary>
        /// Absolute damage dealt to the vehicle when it decended below its crush depth.
        /// </summary>
        public int CrushDamage { get; } = 7; //= MaxHealth / 15;
        /// <summary>
        /// Absolute damage dealt to the vehicle when it is bit by a adult ghost leviathan.
        /// </summary>
        public float GhostAdultBiteDamage { get; } = 150f;
        /// <summary>
        /// Absolute damage dealt to the vehicle when it is bit by a juvenile ghost leviathan.
        /// </summary>
        public float GhostJuvenileBiteDamage { get; } = 100f;
        /// <summary>
        /// Absolute damage dealt to the vehicle when it is bit by a reaper leviathan.
        /// </summary>
        public float ReaperBiteDamage { get; } = 120f;
        /// <summary>
        /// Physical mass of the vehicle. Must be greater than 0.
        /// </summary>
        public int Mass { get; } = 1000;
        /// <summary>
        /// Maximum number of modules that can be installed on this vehicle.
        /// </summary>
        public int NumModules { get; } = 4;
        /// <summary>
        /// PDA message shown when the vehicle is unlocked.
        /// </summary>
        public string UnlockedMessage { get; } = "New vehicle blueprint acquired";
        /// <summary>
        /// Gets the base crush depth of the vehicle, measured in meters.
        /// If it decends below this depth and there are up upgrades installed, it will take damage.
        /// Must be greater than 0.
        /// </summary>
        public int BaseCrushDepth { get; } = 300;
        /// <summary>
        /// Crush depth increase if a level 1 depth upgrade is installed.
        /// </summary>
        public int CrushDepthUpgrade1 { get; } = 200;
        /// <summary>
        /// Crush depth increase if a level 2 depth upgrade is installed.
        /// </summary>
        public int CrushDepthUpgrade2 { get; } = 600;
        /// <summary>
        /// Crush depth increase if a level 3 depth upgrade is installed.
        /// </summary>
        public int CrushDepthUpgrade3 { get; } = 600;
        /// <summary>
        /// Number of times per second the vehicle will take damage when below its crush depth.
        /// </summary>
        public float CrushDamageFrequency { get; } = 1;
        /// <summary>
        /// The piloting style of the vehicle. Affects player animations.
        /// </summary>
        public PilotingStyle PilotingStyle { get; } = PilotingStyle.Other;
        /// <summary>
        /// The number of seconds it takes to construct the vehicle in the mobile vehicle bay.
        /// Reference times: Seamoth : 10 seconds, Cyclops : 20, Rocket Base : 25
        /// </summary>
        public float TimeToConstruct { get; } = 15f;
        /// <summary>
        /// Gets the color used for rendering construction ghost objects.
        /// Applied only if not black.
        /// </summary>
        public Color ConstructionGhostColor { get; } = Color.black;
        /// <summary>
        /// Gets the color used for rendering construction wireframes.
        /// Applied only if not black.
        /// </summary>
        public Color ConstructionWireframeColor { get; } = Color.black;
        /// <summary>
        /// True if the vehicle can be grabbed by a leviathan.
        /// </summary>
        public bool CanLeviathanGrab { get; set; } = true;
        /// <summary>
        /// True if the vehicle can be docked in a moonpool.
        /// </summary>
        public bool CanMoonpoolDock { get; set; } = true;
        /// <summary>
        /// Rotation applied when docking the vehicle in a cyclops.
        /// </summary>
        public Quaternion CyclopsDockRotation { get; } = Quaternion.identity;
        /// <summary>
        /// True to automatically correct shaders to the vehicle's materials.
        /// </summary>
        public bool AutoApplyShaders { get; } = true;



        public VehicleConfiguration(
            Atlas.Sprite pingSprite = null,
            Sprite saveFileSprite = null,
            Recipe recipe = null,
            bool allowRecipeOverride = true,
            Sprite unlockedSprite = null,
            string description = "A vehicle",
            string encyclopediaEntry = "",
            Sprite encyclopediaImage = null,
            Atlas.Sprite craftingSprite = null,
            Sprite moduleBackgroundImage = null,
            TechType unlockedWith = TechType.Constructor,
            int maxHealth = 100,
            int crushDamage = 7,
            float ghostAdultBiteDamage = 150f,
            float ghostJuvenileBiteDamage = 100f,
            float reaperBiteDamage = 120f,
            int mass = 1000,
            int numModules = 4,
            string unlockedMessage = "New vehicle blueprint acquired",
            int baseCrushDepth = 300,
            int crushDepthUpgrade1 = 200,
            int crushDepthUpgrade2 = 600,
            int crushDepthUpgrade3 = 600,
            float crushDamageFrequency = 1,
            PilotingStyle pilotingStyle = PilotingStyle.Other,
            float timeToConstruct = 15f,
            Color? constructionGhostColor = null,
            Color? constructionWireframeColor = null,
            bool canLeviathanGrab = true,
            bool canMoonpoolDock = true,
            Quaternion? cyclopsDockRotation = null,
            bool autoApplyShaders = true
        )
        {
            if (maxHealth <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(maxHealth), "MaxHealth must be greater than 0.");
            if (mass <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(mass), "Mass must be greater than 0.");
            if (baseCrushDepth <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(baseCrushDepth), "BaseCrushDepth must be greater than 0.");

            PingSprite = pingSprite ?? Assets.StaticAssets.DefaultPingSprite;
            SaveFileSprite = saveFileSprite ?? Assets.StaticAssets.DefaultSaveFileSprite;
            Recipe = recipe ?? Recipe.Example;
            AllowRecipeOverride = allowRecipeOverride;
            UnlockedSprite = unlockedSprite;
            Description = description;
            EncyclopediaEntry = encyclopediaEntry;
            EncyclopediaImage = encyclopediaImage;
            CraftingSprite = craftingSprite ?? StaticAssets.ModVehicleIcon;
            ModuleBackgroundImage = moduleBackgroundImage ?? SpriteHelper.GetSpriteRaw("Sprites/VFModuleBackground.png");
            UnlockedWith = unlockedWith;
            MaxHealth = maxHealth;
            CrushDamage = crushDamage;
            GhostAdultBiteDamage = ghostAdultBiteDamage;
            GhostJuvenileBiteDamage = ghostJuvenileBiteDamage;
            ReaperBiteDamage = reaperBiteDamage;
            Mass = mass;
            NumModules = numModules;
            UnlockedMessage = unlockedMessage;
            BaseCrushDepth = baseCrushDepth;
            CrushDepthUpgrade1 = crushDepthUpgrade1;
            CrushDepthUpgrade2 = crushDepthUpgrade2;
            CrushDepthUpgrade3 = crushDepthUpgrade3;
            CrushDamageFrequency = crushDamageFrequency;
            PilotingStyle = pilotingStyle;
            TimeToConstruct = timeToConstruct;
            ConstructionGhostColor = constructionGhostColor ?? Color.black;
            ConstructionWireframeColor = constructionWireframeColor ?? Color.black;
            CanLeviathanGrab = canLeviathanGrab;
            CanMoonpoolDock = canMoonpoolDock;
            CyclopsDockRotation = cyclopsDockRotation ?? Quaternion.identity;
            AutoApplyShaders = autoApplyShaders;
        }



    }
}
