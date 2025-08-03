using AVS.Log;
using AVS.UpgradeModules;
using AVS.Util;

namespace AVS.Crafting
{
    /// <summary>
    /// Abstract base class for toggleable upgrade modules on AVS vehicles.
    /// </summary>
    public abstract class ToggleableUpgrade : AvsVehicleModule
    {
        /// <inheritdoc />
        public sealed override QuickSlotType QuickSlotType => QuickSlotType.Toggleable;
        /// <summary>
        /// Time in seconds between calls to <see cref="OnRepeat(ToggleActionParams)"/>
        /// while the module is active.
        /// </summary>
        public virtual float RepeatDelay => 0;
        /// <summary>
        /// The time in seconds before the first call to <see cref="OnRepeat(ToggleActionParams)"/>
        /// after the module is activated.
        /// </summary>
        public virtual float DelayUntilFirstOnRepeat => 0;
        /// <summary>
        /// The energy cost per <see cref="OnRepeat(ToggleActionParams)"/> call.
        /// If the vehicle does not have enough energy, the module will auto-toggle off.
        /// </summary>
        public virtual float EnergyCostPerActivation => 0;
        /// <summary>
        /// Executed once every <see cref="RepeatDelay"/> seconds while the module is active.
        /// </summary>
        /// <param name="param"></param>
        public virtual void OnRepeat(ToggleActionParams param)
        {
            LogWriter.Default.Debug(this, $"OnRepeat {ClassId} on Vehicle: {param.Vehicle.NiceName()} in slotID: {param.SlotID} active: {param.IsActive} elapsed: {param.SecondsSinceActivation}");
        }

        /// <summary>
        /// Executed when the module is toggled on or off, before waiting <see cref="DelayUntilFirstOnRepeat"/>.
        /// </summary>
        /// <param name="param"></param>
        public virtual void OnToggle(ToggleActionParams param)
        {
            LogWriter.Default.Write($"Toggle {ClassId} on Vehicle: {param.Vehicle.NiceName()} in slotID: {param.SlotID} active: {param.IsActive} elapsed: {param.SecondsSinceActivation}");
        }
    }
}
