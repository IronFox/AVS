namespace AVS.UpgradeModules
{
    public readonly struct SelectableChargeableActionParams
    {
        public Vehicle Vehicle { get; }
        public SubRoot? Cyclops { get; }
        public int SlotID { get; }
        /// <summary>
        /// The tech type of the upgrade being acted upon.
        /// </summary>
        public TechType TechType { get; }
        public float Charge { get; }
        public float SlotCharge { get; }

        public SelectableChargeableActionParams(
            Vehicle vehicle,
            int slotID,
            TechType techType,
            float charge,
            float slotCharge,
            SubRoot? cyclops = null
            )
        {
            Vehicle = vehicle;
            Cyclops = cyclops;
            SlotID = slotID;
            TechType = techType;
            Charge = charge;
            SlotCharge = slotCharge;
        }
    }
}
