namespace AVS.UpgradeModules.Variations
{
    public abstract class SelectableUpgrade : AvsVehicleModule
    {

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
            public Params(
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

        public override string Description => "This is a selectable upgrade module.";
        public override QuickSlotType QuickSlotType => QuickSlotType.Selectable;
        public virtual float Cooldown => 0;
        public virtual float EnergyCost => 0;
        public virtual void OnSelected(Params param)
        {
            Logger.DebugLog(this, "Selecting " + ClassId + " on ModVehicle: " + param.Vehicle.subName.name + " in slotID: " + param.SlotID.ToString());
        }

    }
}
