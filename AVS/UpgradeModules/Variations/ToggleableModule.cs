using AVS.Log;
using AVS.Util;

namespace AVS.UpgradeModules.Variations
{
    /// <summary>
    /// Parameters for toggleable upgrade actions.
    /// </summary>
    public interface IToggleState
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
        /// True if the upgrade is toggled on, false if off.
        /// </summary>
        public bool IsActive { get; }
        /// <summary>
        /// The current time in seconds since the upgrade was activated.
        /// </summary>
        public float EventTime { get; }
        /// <summary>
        /// The last time (<see cref="EventTime"/>) in seconds the upgrade was repeated.
        /// </summary>
        public float LastRepeatTime { get; }
        /// <summary>
        /// The iteration number of this repition.
        /// -1 before the first call to <see cref="ToggleableModule.OnRepeat"/>,
        /// 0 during the first call, incremented by 1 for each subsequent call.
        /// </summary>
        public int RepeatIteration { get; }

        /// <summary>
        /// Deactivates the upgrade module.
        /// </summary>
        public void Deactivate();
    }


    /// <summary>
    /// Abstract base class for toggleable upgrade modules on AVS vehicles.
    /// A module of this type will consume <see cref="EnergyCostPerSecond"/> energy
    /// every second and call <see cref="OnRepeat"/>
    /// while active.
    /// </summary>
    public abstract class ToggleableModule : AvsVehicleModule
    {





        /// <inheritdoc />
        public sealed override QuickSlotType QuickSlotType => QuickSlotType.Toggleable;
        /// <summary>
        /// Time in seconds between calls to <see cref="OnRepeat"/>
        /// while the module is active.
        /// Should not be 0 or the module will repeat once per frame.
        /// </summary>
        public virtual float RepeatDelay => 0.1f;
        /// <summary>
        /// The time in seconds before the first call to <see cref="OnRepeat"/>
        /// after the module is activated.
        /// </summary>
        public virtual float DelayUntilFirstOnRepeat => 0;
        /// <summary>
        /// The energy cost per second while the module is active.
        /// If the vehicle does not have enough energy, the module will auto-toggle off.
        /// </summary>
        public virtual float EnergyCostPerSecond => 0;
        /// <summary>
        /// Executed once every <see cref="RepeatDelay"/> seconds while the module is active.
        /// </summary>
        /// <param name="state"></param>
        protected virtual void OnRepeat(IToggleState state)
        {
            using var log = SmartLog.ForAVS(Owner);
            log.Debug($"OnRepeat {ClassId} on Vehicle: {state.Vehicle.NiceName()} in slotID: {state.SlotID} active: {state.IsActive} elapsed: {state.EventTime}");
        }
        internal void OnRepeatInternal(IToggleState state)
            => OnRepeat(state);

        /// <summary>
        /// Executed when the module is toggled on or off, before waiting <see cref="DelayUntilFirstOnRepeat"/>.
        /// </summary>
        /// <param name="state"></param>
        protected virtual void OnToggle(IToggleState state)
        {
            using var log = SmartLog.ForAVS(Owner);
            log.Write($"Toggle {ClassId} on Vehicle: {state.Vehicle.NiceName()} in slotID: {state.SlotID} active: {state.IsActive} elapsed: {state.EventTime}");
        }

        internal void OnToggleInternal(IToggleState state)
            => OnToggle(state);

    }
}
