namespace AVS.UpgradeModules.Variations
{
    /// <summary>
    /// Abstract base class for selectable vehicle upgrade modules.
    /// This modules can be selected in the quickbar and used by the player.
    /// </summary>
    public abstract class SelectableModule : AvsVehicleModule
    {
        /// <summary>
        /// Parameters passed to <see cref="OnSelected(Params)"/>.
        /// </summary>
        public readonly struct Params
        {
            /// <summary>
            /// The vehicle to which the upgrade belongs. May be null if <see cref="Cyclops"/> is not null.
            /// </summary>
            public Vehicle Vehicle { get; }
            /// <summary>
            /// Reference to the owning Cyclops vehicle, if any. May be null.
            /// </summary>
            public SubRoot? Cyclops { get; }
            /// <summary>
            /// The index of the slot in which the upgrade is located.
            /// </summary>
            public int SlotID { get; }
            /// <summary>
            /// The tech type of the upgrade being acted upon.
            /// </summary>
            public TechType TechType { get; }

            internal Params(
                Vehicle vehicle,
                int slotID,
                TechType techType,
                SubRoot? cyclops = null)
            {
                Vehicle = vehicle;
                Cyclops = cyclops;
                SlotID = slotID;
                TechType = techType;
            }
        }

        /// <inheritdoc/>
        public sealed override QuickSlotType QuickSlotType => QuickSlotType.Selectable;
        public virtual float Cooldown => 0;
        public virtual float EnergyCost => 0;
        public virtual void OnSelected(Params param)
        {
            Logger.DebugLog(this, "Selecting " + ClassId + " on AvsVehicle: " + param.Vehicle.subName.name + " in slotID: " + param.SlotID.ToString());
        }

    }
}
