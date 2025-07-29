using AVS.Configuration;
using AVS.Crafting;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AVS.UpgradeModules
{
    /// <summary>
    /// Base class for all mod vehicle upgrades. Provides core properties, recipe handling, and extension points for custom upgrades.
    /// </summary>
    public abstract class AvsVehicleUpgrade : ILogFilter
    {
        /// <summary>
        /// If true, enables debug logging for this upgrade.
        /// </summary>
        public bool LogDebug { get; set; } = false;

        /// <summary>
        /// Holds TechTypes for this upgrade for each supported vehicle type.
        /// </summary>
        public UpgradeTechTypes TechTypes { get; internal set; }

        internal Node? node;

        /// <summary>
        /// The node this upgrade is registered to.
        /// Set during registration.
        /// </summary>
        public Node Node => node ?? throw new InvalidOperationException($"Cannot access {nameof(AvsVehicleUpgrade)}.{nameof(Node)} before it has been registered");

        private TechType _unlockTechType = TechType.Fragment;

        /// <summary>
        /// Gets a value indicating whether the item is specific to a vehicle.
        /// </summary>
        public virtual bool IsVehicleSpecific => false;

        /// <summary>
        /// The TechType used to unlock this upgrade. Can only be set once if the default is <see cref="TechType.Fragment"/>.
        /// </summary>
        internal TechType UnlockTechType
        {
            get
            {
                return _unlockTechType;
            }
            set
            {
                if (_unlockTechType == TechType.Fragment)
                {
                    _unlockTechType = value;
                }
            }
        }

        /// <summary>
        /// The unique class ID for this upgrade.
        /// </summary>
        public abstract string ClassId { get; }

        /// <summary>
        /// The display name for this upgrade.
        /// </summary>
        public abstract string DisplayName { get; }

        /// <summary>
        /// The description for this upgrade.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// The quick slot type for this upgrade.
        /// </summary>
        public virtual QuickSlotType QuickSlotType => QuickSlotType.Passive;

        /// <summary>
        /// If true, this upgrade is unlocked at the start of a new game.
        /// </summary>
        public virtual bool UnlockAtStart => true;

        /// <summary>
        /// The color associated with this upgrade.
        /// </summary>
        public virtual Color Color => Color.red;

        /// <summary>
        /// The time required to craft this upgrade.
        /// </summary>
        public virtual float CraftingTime => 3f;

        /// <summary>
        /// The icon for this upgrade.
        /// </summary>
        public abstract Atlas.Sprite Icon { get; }

        /// <summary>
        /// The TechType that this module unlocks together with.
        /// If this tech type is unlocked, this upgrade is also unlocked.
        /// </summary>
        public virtual TechType UnlockWith => TechType.Constructor;

        /// <summary>
        /// The default unlock message for this upgrade.
        /// </summary>
        public const string DefaultUnlockMessage = "New vehicle upgrade acquired";

        /// <summary>
        /// The message shown when this upgrade is unlocked.
        /// </summary>
        public virtual string UnlockedMessage => DefaultUnlockMessage;

        /// <summary>
        /// The sprite shown when this upgrade is unlocked.
        /// </summary>
        public virtual Sprite? UnlockedSprite => null;

        /// <summary>
        /// The base recipe for this upgrade.
        /// </summary>
        public virtual Recipe Recipe { get; } = NewRecipe.StartWith(TechType.Titanium, 1).Done();

        /// <summary>
        /// Called when this upgrade is added to a vehicle.
        /// </summary>
        /// <param name="param">Parameters for the add action.</param>
        public virtual void OnAdded(AddActionParams param)
        {
            Logger.DebugLog(this, "Adding " + ClassId + " to ModVehicle: " + param.vehicle.subName.name + " in slotID: " + param.slotID.ToString());
        }

        /// <summary>
        /// Called when this upgrade is removed from a vehicle.
        /// </summary>
        /// <param name="param">Parameters for the remove action.</param>
        public virtual void OnRemoved(AddActionParams param)
        {
            Logger.DebugLog(this, "Removing " + ClassId + " to ModVehicle: " + param.vehicle.subName.name + " in slotID: " + param.slotID.ToString());
        }

        /// <summary>
        /// Called when this upgrade is cycled in a Cyclops vehicle.
        /// </summary>
        /// <param name="param">Parameters for the Cyclops action.</param>
        public virtual void OnCyclops(AddActionParams param)
        {
            Logger.DebugLog(this, "Bumping " + ClassId + " In Cyclops: '" + param.cyclops.subName + "' in slotID: " + param.slotID.ToString());
        }

        /// <summary>
        /// Holds additional TechTypes to extend the recipe for different vehicle types.
        /// </summary>
        private List<UpgradeTechTypes> RecipeExtensions { get; } = new List<UpgradeTechTypes>();

        /// <summary>
        /// Holds additional simple ingredients to extend the recipe.
        /// </summary>
        private NewRecipe SimpleRecipeExtensions { get; } = NewRecipe.WithNothing();

        /// <summary>
        /// Gets the full recipe for this upgrade for a specific vehicle type.
        /// </summary>
        /// <param name="type">The vehicle type.</param>
        /// <returns>A list of ingredients for crafting.</returns>
        public Recipe GetRecipe(VehicleType type)
        {
            var r = NewRecipe
                .StartWith(Recipe)
                .Include(SimpleRecipeExtensions);



            switch (type)
            {
                case VehicleType.AvsVehicle:
                    r = r.IncludeOneOfEach(RecipeExtensions.Select(x => x.ForAvsVehicle));
                    break;
                case VehicleType.Seamoth:
                    r = r.IncludeOneOfEach(RecipeExtensions.Select(x => x.ForSeamoth));
                    break;
                case VehicleType.Prawn:
                    r = r.IncludeOneOfEach(RecipeExtensions.Select(x => x.ForExosuit));
                    break;
                case VehicleType.Cyclops:
                    r = r.IncludeOneOfEach(RecipeExtensions.Select(x => x.ForCyclops));
                    break;
                default:
                    break;
            }
            return r.Done();
        }

        /// <summary>
        /// Adds an <see cref="UpgradeTechTypes"/> to the recipe extensions.
        /// </summary>
        /// <param name="techTypes">The tech types to add.</param>
        public void ExtendRecipe(UpgradeTechTypes techTypes)
        {
            RecipeExtensions.Add(techTypes);
        }

        /// <summary>
        /// Adds a simple ingredient to the recipe extensions.
        /// </summary>
        /// <param name="ingredient">The ingredient to add.</param>
        public void ExtendRecipeSimple(RecipeIngredient ingredient)
        {
            SimpleRecipeExtensions.Include(ingredient);
        }

        /// <summary>
        /// Checks if this upgrade has the specified <see cref="TechType"/>.
        /// </summary>
        /// <param name="tt">The tech type to check.</param>
        /// <returns>True if the tech type is present; otherwise, false.</returns>
        public bool HasTechType(TechType tt) => TechTypes.HasTechType(tt);


        /// <summary>
        /// Gets the number of this upgrade currently installed in the specified vehicle.
        /// </summary>
        /// <param name="vehicle">The vehicle to check.</param>
        /// <returns>The number of upgrades installed.</returns>
        public int GetNumberInstalled(Vehicle vehicle)
        {
            if (vehicle == null)
            {
                return 0;
            }
            return vehicle.GetCurrentUpgrades().Where(x => x.Contains(ClassId)).Count();
        }

        internal void SetNode(Node node)
        {
            if (this.node != null && this.node != node)
                throw new InvalidOperationException($"Trying to reset {nameof(AvsVehicleUpgrade)}.{nameof(Node)} from {this.node} to {node}");
            this.node = node;

        }
    }
}
