namespace AVS
{
    internal enum PowerEvent
    {
        OnBatterySafe,
        OnBatteryLow,
        OnBatteryNearlyEmpty,
        OnBatteryDepleted
    }
    /// <summary>
    /// Notifies the listener that the power state of the vehicle has changed.
    /// </summary>
    public interface IPowerChanged // useful for managing things that need to be powered up or down
    {
        /// <summary>
        /// Handles changes in power state, including battery power and switch status.
        /// </summary>
        /// <param name="hasBatteryPower">A value indicating whether the vehicle has battery power.  <see langword="true"/> if battery power is
        /// available; otherwise, <see langword="false"/>.</param>
        /// <param name="isSwitchedOn">A value indicating whether the vehicle is switched on.  <see langword="true"/> if the device is switched on;
        /// otherwise, <see langword="false"/>.</param>
        void OnPowerChanged(bool hasBatteryPower, bool isSwitchedOn);
    }

    /// <summary>
    /// More extensive power listener interface for vehicles.
    /// </summary>
    public interface IPowerListener // useful for issuing power status notifications (ai voice, ui elements)
    {
        /// <summary>
        /// The vehicle powered up.
        /// </summary>
        void OnPowerUp();
        /// <summary>
        /// The vehicle has powered down.
        /// </summary>
        void OnPowerDown();
        /// <summary>
        /// The vehicle's batteries died.
        /// </summary>
        void OnBatteryDead();
        /// <summary>
        /// The vehicle's batteries were replaced or otherwise revived.
        /// </summary>
        void OnBatteryRevive();

        // the following notifications are only sent when the vehicle has battery and is powered ON
        /// <summary>
        /// The battery level is now safe.
        /// </summary>
        void OnBatterySafe();
        /// <summary>
        /// The battery level is low.
        /// </summary>
        void OnBatteryLow();
        /// <summary>
        /// The battery level is nearly empty.
        /// </summary>
        void OnBatteryNearlyEmpty();
        /// <summary>
        /// The battery is completely depleted.
        /// </summary>
        void OnBatteryDepleted();
    }
}
