using AVS.Log;
using System;

namespace AVS.UpgradeModules.Variations
{
    /// <summary>
    /// An upgrade module that can be charged by holding the quickbar button.
    /// </summary>
    public abstract class ChargeableModule : AvsVehicleModule
    {
        /// <summary>
        /// Parameters passed to <see cref="SelectableChargeableModule.OnActivate(Params)"/>.
        /// </summary>
        public readonly struct Params
        {
            /// <summary>
            /// The vehicle that the upgrade is being used on.
            /// </summary>
            public Vehicle Vehicle { get; }
            /// <summary>
            /// The index of the slot in the quickbar where the upgrade is selected.
            /// </summary>
            public int SlotID { get; }
            /// <summary>
            /// The tech type of the upgrade being acted upon.
            /// </summary>
            public TechType TechType { get; }
            /// <summary>
            /// Total charge level reached (>0)
            /// </summary>
            public float Charge { get; }
            /// <summary>
            /// Relative charge level (>0, 1 = fully charged, 0 = empty)
            /// </summary>
            public float ChargeFraction { get; }

            /// <summary>
            /// Represents the parameters for a chargeable action that can be selected, associated with a specific vehicle
            /// and slot.
            /// </summary>
            /// <remarks>This constructor initializes the parameters required for a chargeable action,
            /// including the vehicle, slot, technology type, and charge details. Ensure that the provided values meet the
            /// specified constraints to avoid unexpected behavior.</remarks>
            /// <param name="vehicle">The vehicle associated with the chargeable action. Cannot be null.</param>
            /// <param name="slotID">The ID of the slot within the vehicle where the action is applied. Must be a non-negative integer.</param>
            /// <param name="techType">The type of technology associated with the chargeable action.</param>
            /// <param name="charge">Total charge level reached. Must be a non-negative value.</param>
            /// <param name="chargeFraction">The fraction of the total charge capacity to be applied. Must be a value between 0 and 1, inclusive.</param>
            internal Params(
                Vehicle vehicle,
                int slotID,
                TechType techType,
                float charge,
                float chargeFraction
                )
            {
                if (!vehicle)
                    throw new ArgumentNullException(nameof(vehicle), "Vehicle cannot be null.");
                Vehicle = vehicle;
                SlotID = slotID;
                TechType = techType;
                Charge = charge;
                ChargeFraction = chargeFraction;
            }
        }


        /// <inheritdoc/>
        public override QuickSlotType QuickSlotType => QuickSlotType.SelectableChargeable;
        /// <summary>
        /// Gets the maximum charge level that must be reached in order to activate.
        /// </summary>
        public virtual float ChargeLimit => 1;
        /// <summary>
        /// Gets the energy cost per second when the player holds the quick slot key to charge the upgrade.
        /// This value accumulates over time while the player holds the key until the maximum charge is reached.
        /// </summary>
        public virtual float EnergyCostPerSecond => 1;
        /// <summary>
        /// Triggered when the charge limit was reached or the left mouse button was released.
        /// </summary>
        /// <param name="param">The current charge state.</param>
        public virtual void OnActivate(Params param)
        {
            LogWriter.Default.Debug(this, $"nameof");
        }
    }
}
