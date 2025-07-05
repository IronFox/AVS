using UnityEngine;

namespace AVS.UpgradeTypes
{
    public struct AddActionParams
    {
        public Vehicle vehicle;
        public SubRoot cyclops;
        public int slotID;
        public TechType techType;
        public bool isAdded;
    }
    public struct ToggleActionParams
    {
        public Vehicle vehicle;
        public SubRoot cyclops;
        public int slotID;
        public TechType techType;
        public bool active;
    }
    public readonly struct SelectableChargeableActionParams
    {
        public Vehicle Vehicle { get; }
        public SubRoot Cyclops { get; }
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
            SubRoot cyclops = null
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
    public readonly struct SelectableActionParams
    {
        /// <summary>
        /// The vehicle to which the upgrade belongs. May be null if <see cref="Cyclops"/> is not null.
        /// </summary>
        public Vehicle Vehicle { get; }
        /// <summary>
        /// Reference to the owning Cyclops vehicle, if any. May be null.
        /// </summary>
        public SubRoot Cyclops { get; }
        /// <summary>
        /// The index of the slot in which the upgrade is located.
        /// </summary>
        public int SlotID { get; }
        /// <summary>
        /// The tech type of the upgrade being acted upon.
        /// </summary>
        public TechType TechType { get; }
        public SelectableActionParams(
            Vehicle vehicle,
            int slotID,
            TechType techType,
            SubRoot cyclops = null)
        {
            Vehicle = vehicle;
            Cyclops = cyclops;
            SlotID = slotID;
            TechType = techType;
        }
    }
    public struct ArmActionParams
    {
        public Vehicle vehicle;
        public SubRoot cyclops;
        public int slotID;
        public TechType techType;
        public GameObject arm;
    }
}
