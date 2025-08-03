using UnityEngine;

namespace AVS.UpgradeModules
{
    public struct AddActionParams
    {
        public Vehicle vehicle;
        public SubRoot cyclops;
        public int slotID;
        public TechType techType;
        public bool isAdded;
    }

    /// <summary>
    /// Parameters for toggleable upgrade actions.
    /// </summary>
    public struct ToggleActionParams
    {
        /// <summary>
        /// The vehicle the action is being performed on.
        /// </summary>
        public Vehicle Vehicle { get; }
        /// <summary>
        /// The index of the slot in which the upgrade is located.
        /// </summary>
        public int SlotID { get; }
        /// <summary>
        /// The tech type of the upgrade being acted upon.
        /// </summary>
        public TechType TechType { get; }
        /// <summary>
        /// True if the upgrade is toggled on, false if off.
        /// </summary>
        public bool IsActive { get; }
        /// <summary>
        /// The total time in seconds since the upgrade was activated.
        /// </summary>
        public float SecondsSinceActivation { get; }

        internal ToggleActionParams(Vehicle vehicle, int slotID, TechType techType, bool isActive, float secondsSinceActivation = 0)
        {
            Vehicle = vehicle;
            SlotID = slotID;
            TechType = techType;
            IsActive = isActive;
            SecondsSinceActivation = secondsSinceActivation;
        }

        internal ToggleActionParams IncreaseSecondsSinceActivation(float secondsElapsed)
            => new ToggleActionParams(Vehicle, SlotID, TechType, IsActive, SecondsSinceActivation + secondsElapsed);

        internal ToggleActionParams SetInactive()
            => new ToggleActionParams(Vehicle, SlotID, TechType, false, SecondsSinceActivation);
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
        public SubRoot? Cyclops { get; }
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
            SubRoot? cyclops = null)
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
