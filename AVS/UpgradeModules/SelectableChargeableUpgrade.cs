namespace AVS.UpgradeModules
{
    /// <summary>
    /// Represents an upgrade module that can be selected and charged within a vehicle's quick slot.
    /// </summary>
    /// <remarks>This abstract class provides a base for creating upgrade modules that require selection and
    /// charging in a vehicle's quick slot. It defines properties for description, quick slot type, maximum charge, and
    /// energy cost, as well as a method to handle selection actions.</remarks>
    public abstract class SelectableChargeableUpgrade : AvsVehicleUpgrade
    {
        /// <inheritdoc/>
        public override string Description => "This is a selectable-chargeable upgrade module.";
        /// <inheritdoc/>
        public override QuickSlotType QuickSlotType => QuickSlotType.SelectableChargeable;
        /// <summary>
        /// Gets the maximum charge level.
        /// </summary>
        public virtual float MaxCharge => 0;
        /// <summary>
        /// Gets the energy cost associated with the operation.
        /// </summary>
        public virtual float EnergyCost => 0;
        /// <summary>
        /// Handles the event when a chargeable action is selected.
        /// </summary>
        /// <remarks>This method logs the selection of a chargeable action for a specific vehicle and
        /// slot. Override this method to implement custom behavior when an action is selected.</remarks>
        /// <param name="param">The parameters associated with the selected chargeable action, including the vehicle and slot information.</param>
        public virtual void OnSelected(SelectableChargeableActionParams param)
        {
            Logger.DebugLog(this, "Selecting-Charging " + ClassId + " on ModVehicle: " + param.Vehicle.subName.name + " in slotID: " + param.SlotID.ToString());
        }
    }
}
