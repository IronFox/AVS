using AVS.Configuration;
using AVS.Crafting;
using AVS.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AVS.UpgradeModules
{
    /// <summary>
    /// Base class for all mod vehicle upgrades.
    /// Provides core properties and recipe handling.
    /// </summary>
    public abstract class AvsVehicleModule : ILogFilter
    {
        /// <summary>
        /// If true, enables debug logging for this upgrade.
        /// </summary>
        public bool LogDebug { get; set; } = false;

        /// <summary>
        /// The registered tech types of this upgrade.
        /// Available once the upgrade has been registered.
        /// </summary>
        public UpgradeTechTypes TechTypes { get; internal set; }

        /// <summary>
        /// The last set sepcific tech type for this upgrade.
        /// Used to daisy chain unlock conditions.
        /// </summary>
        internal TechType LastRegisteredTechType { get; set; } = TechType.None;

        internal Node? node;

        /// <summary>
        /// The node this upgrade has been registered to.
        /// Available once the upgrade has been registered.
        /// </summary>
        public Node Node => node ?? throw new InvalidOperationException($"Cannot access {nameof(AvsVehicleModule)}.{nameof(Node)} before it has been registered");

        /// <summary>
        /// Gets a value indicating whether the item is specific to a vehicle.
        /// </summary>
        public virtual bool IsVehicleSpecific => false;


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
        /// Module types that are automatically displaced when this upgrade is added to a vehicle.
        /// </summary>
        public virtual IReadOnlyCollection<TechType>? AutoDisplace => null;

        /// <summary>
        /// Called when this upgrade is added to a vehicle.
        /// </summary>
        /// <param name="param">Parameters for the add action.</param>
        public virtual void OnAdded(AddActionParams param)
        {
            var now = DateTime.Now;

            LogWriter.Default.Debug($"ArchonBaseModule[{ClassId}].OnAdded(vehicle={param.vehicle},isAdded={param.isAdded},slot={param.slotID})");

            if (AutoDisplace != null)
            {
                LogWriter.Default.Write($"Auto-displacing modules for {ClassId} in vehicle {param.vehicle.subName.name} at slot {param.slotID}");
                try
                {
                    foreach (var slot in param.vehicle.slotIDs)
                    {
                        if (slot == param.vehicle.slotIDs[param.slotID])
                            continue;
                        var p = param.vehicle.modules.GetItemInSlot(slot);
                        if (p != null)
                        {
                            var t = p.item.GetComponent<TechTag>();
                            if (t != null)
                            {
                                if (AutoDisplace.Contains(t.type))
                                {
                                    LogWriter.Default.Write($"Evacuating extra {t.type} type from slot {slot}");
                                    if (!param.vehicle.modules.RemoveItem(p.item))
                                    {
                                        LogWriter.Default.Error($"Failed remove");
                                        continue;
                                    }
                                    Inventory.main.AddPending(p.item);
                                    LogWriter.Default.Write($"Inventory moved");
                                    break;
                                }
                                else
                                {
                                    LogWriter.Default.Debug($"Skipping {t.type} in slot {slot} for auto-displacement because it is not in {string.Join(", ", AutoDisplace)}");
                                }
                            }
                            else
                            {
                                LogWriter.Default.Error($"No TechTag on item {p.item.name} in slot {slot} for auto-displacement");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    LogWriter.Default.Error($"Error while removing auto-displaced modules: {e.Message}", e);
                }
            }
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

        internal void RegisterTechTypeFor(VehicleType vType, TechType newType)
        {
            TechTypes = TechTypes.ReplaceVehicleType(vType, newType, false);
            LastRegisteredTechType = newType;
        }


        private List<AvsVehicleModule> RecipeExtensions { get; } = new List<AvsVehicleModule>();

        /// <summary>
        /// Holds additional simple ingredients to extend the recipe.
        /// </summary>
        private NewRecipe SimpleRecipeExtensions { get; } = NewRecipe.WithNothing();

        /// <summary>
        /// The tech type this one effectively unlocks with.
        /// </summary>
        internal TechType UnlockTechType => LastRegisteredTechType == TechType.None ? UnlockWith : LastRegisteredTechType;

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
                    r = r.IncludeOneOfEach(RecipeExtensions.Select(x => x.TechTypes.ForAvsVehicle));
                    break;
                case VehicleType.Seamoth:
                    r = r.IncludeOneOfEach(RecipeExtensions.Select(x => x.TechTypes.ForSeamoth));
                    break;
                case VehicleType.Prawn:
                    r = r.IncludeOneOfEach(RecipeExtensions.Select(x => x.TechTypes.ForExosuit));
                    break;
                case VehicleType.Cyclops:
                    r = r.IncludeOneOfEach(RecipeExtensions.Select(x => x.TechTypes.ForCyclops));
                    break;
                default:
                    break;
            }
            return r.Done();
        }

        /// <summary>
        /// Adds the given module as a single extension to the local recipe
        /// </summary>
        /// <param name="module">The module to add.</param>
        public void ExtendRecipe(AvsVehicleModule module)
        {
            RecipeExtensions.Add(module);
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
                throw new InvalidOperationException($"Trying to reset {nameof(AvsVehicleModule)}.{nameof(Node)} from {this.node} to {node}");
            this.node = node;

        }
    }
}
