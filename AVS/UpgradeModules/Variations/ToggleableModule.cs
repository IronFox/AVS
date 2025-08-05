using AVS.Log;
using AVS.Util;

namespace AVS.UpgradeModules.Variations
{
    /// <summary>
    /// Abstract base class for toggleable upgrade modules on AVS vehicles.
    /// A module of this type will consume <see cref="EnergyCostPerActivation"/> energy
    /// every <see cref="RepeatDelay"/> seconds and call <see cref="OnRepeat(Params)"/>
    /// while active.
    /// </summary>
    /// <remarks>Use the static <see cref="Deactivate" /> method to deactive a toggleable module</remarks>
    public abstract class ToggleableModule : AvsVehicleModule
    {

        /// <summary>
        /// Parameters for toggleable upgrade actions.
        /// </summary>
        public readonly struct Params
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
            /// The current time in seconds since the upgrade was activated.
            /// </summary>
            public float RepeatTime { get; }
            /// <summary>
            /// The last time (<see cref="RepeatTime"/>) in seconds the upgrade was repeated.
            /// </summary>
            public float LastRepeatTime { get; }

            internal Params(Vehicle vehicle, int slotID, TechType techType, bool isActive, float repeatTime = 0, float lastRepeatTime = 0)
            {
                Vehicle = vehicle;
                SlotID = slotID;
                TechType = techType;
                IsActive = isActive;
                RepeatTime = repeatTime;
                LastRepeatTime = lastRepeatTime;
            }

            internal Params AdvanceRepeatTime(float secondsElapsed)
                => new Params(Vehicle, SlotID, TechType, IsActive, RepeatTime + secondsElapsed, RepeatTime);

            /// <summary>
            /// Creates a new instance of <see cref="Params"/> with the upgrade set to inactive.
            /// </summary>
            public Params SetInactive()
                => new Params(Vehicle, SlotID, TechType, false, RepeatTime, LastRepeatTime);
        }



        /// <inheritdoc />
        public sealed override QuickSlotType QuickSlotType => QuickSlotType.Toggleable;
        /// <summary>
        /// Time in seconds between calls to <see cref="OnRepeat(Params)"/>
        /// while the module is active.
        /// </summary>
        public virtual float RepeatDelay => 0;
        /// <summary>
        /// The time in seconds before the first call to <see cref="OnRepeat(Params)"/>
        /// after the module is activated.
        /// </summary>
        public virtual float DelayUntilFirstOnRepeat => 0;
        /// <summary>
        /// The energy cost per <see cref="OnRepeat(Params)"/> call.
        /// If the vehicle does not have enough energy, the module will auto-toggle off.
        /// </summary>
        public virtual float EnergyCostPerActivation => 0;
        /// <summary>
        /// Executed once every <see cref="RepeatDelay"/> seconds while the module is active.
        /// </summary>
        /// <param name="param"></param>
        protected virtual void OnRepeat(Params param)
        {
            LogWriter.Default.Debug(this, $"OnRepeat {ClassId} on Vehicle: {param.Vehicle.NiceName()} in slotID: {param.SlotID} active: {param.IsActive} elapsed: {param.RepeatTime}");
        }
        internal void OnRepeatInternal(Params param)
            => OnRepeat(param);

        /// <summary>
        /// Executed when the module is toggled on or off, before waiting <see cref="DelayUntilFirstOnRepeat"/>.
        /// </summary>
        /// <param name="param"></param>
        protected virtual void OnToggle(Params param)
        {
            LogWriter.Default.Write($"Toggle {ClassId} on Vehicle: {param.Vehicle.NiceName()} in slotID: {param.SlotID} active: {param.IsActive} elapsed: {param.RepeatTime}");
        }

        internal void OnToggleInternal(Params param)
            => OnToggle(param);

        /// <summary>
        /// Helper method to deactivate the module as specified by <paramref name="param"/>.
        /// </summary>
        /// <param name="param">Definition of what to deactivate</param>
        public static void Deactivate(Params param)
        {
            param.Vehicle.ToggleSlot(param.SlotID, false);
        }
    }
}
