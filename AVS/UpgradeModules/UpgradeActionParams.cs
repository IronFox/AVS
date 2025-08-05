

namespace AVS.UpgradeModules
{
    /// <summary>
    /// Parameters passed to <see cref="AvsVehicleModule.OnAdded(AddActionParams)"/> and
    /// <see cref="AvsVehicleModule.OnRemoved(AddActionParams)"/>.
    /// </summary>
    public readonly struct AddActionParams
    {
        /// <summary>
        /// The vehicle that a module was added to or removed from.
        /// Null if the module was added to or removed from a Cyclops.
        /// </summary>
        public Vehicle? Vehicle { get; }
        /// <summary>
        /// If the vehicle is a Cyclops, this is the Cyclops subroot.
        /// </summary>
        public SubRoot? Cyclops { get; }
        /// <summary>
        /// The index of the slot in which the module was added or removed.
        /// </summary>
        public int SlotID { get; }
        /// <summary>
        /// Gets the tech type of the module that was added or removed.
        /// </summary>
        public TechType TechType { get; }
        /// <summary>
        /// True if the module has just been added to the vehicle.
        /// False if it has just been removed from the vehicle.
        /// </summary>
        public bool Added { get; }

        internal AddActionParams(Vehicle? vehicle, SubRoot? cyclops, int slotID, TechType techType, bool added)
        {
            Vehicle = vehicle;
            Cyclops = cyclops;
            SlotID = slotID;
            TechType = techType;
            Added = added;
        }

        internal static AddActionParams CreateForVehicle(Vehicle vehicle, int slotID, TechType techType, bool added)
        {
            return new AddActionParams(vehicle, null, slotID, techType, added);
        }

        internal static AddActionParams CreateForCyclops(SubRoot cyclops, int slotID, TechType techType, bool added)
        {
            return new AddActionParams(null, cyclops, slotID, techType, added);
        }
    }
}
