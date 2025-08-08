using AVS.Log;
using AVS.Util;
using UnityEngine;

namespace AVS
{
    /// <summary>
    /// Base class for light controllers in AVS vehicles.
    /// </summary>
    public abstract class BaseLightController : MonoBehaviour, IPowerChanged, IScuttleListener, IDockListener
    {
        private bool canLightsBeEnabled = true;
        private bool isDocked = false;
        private bool isScuttled = false;
        private bool _isLightsOn = false;
        /// <summary>
        /// The current state of the lights managed by this controller.
        /// </summary>
        public bool IsLightsOn
        {
            get
            {
                return _isLightsOn;
            }
            private set
            {
                bool oldValue = _isLightsOn;
                var newValue = value && canLightsBeEnabled && !isScuttled && !isDocked;
                if (oldValue == newValue)
                {
                    return; // no change
                }
                LogWriter.Default.Debug($"{this.NiceName()} Setting IsLightsOn to {value}=>{newValue} (was {oldValue}) for {gameObject.name}, with canLightsBeEnabled={canLightsBeEnabled}, isScuttled={isScuttled}, isDocked={isDocked}");
                _isLightsOn = newValue;
                HandleLighting(IsLightsOn);
                HandleSound(IsLightsOn);
            }
        }
        /// <summary>
        /// Called when the light enabled status has changed.
        /// Intended to switch lights on or off in the vehicle.
        /// </summary>
        /// <param name="lightsAreNowEnabled">True if the lights have been switched on</param>
        protected abstract void HandleLighting(bool lightsAreNowEnabled);
        /// <summary>
        /// Called when the light enabled status has changed.
        /// Intended to play a sound when the lights are toggled on or off.
        /// </summary>
        /// <param name="lightsAreNowEnabled">True if the lights have been switched on</param>
        protected abstract void HandleSound(bool lightsAreNowEnabled);
        /// <summary>
        /// Toggles the lights on &lt;-&gt; off.
        /// </summary>
        public void Toggle()
        {
            IsLightsOn = !IsLightsOn;
        }
        void IPowerChanged.OnPowerChanged(bool hasBatteryPower, bool isSwitchedOn)
        {

            bool now = hasBatteryPower && isSwitchedOn;
            if (canLightsBeEnabled != now)
            {
                LogWriter.Default.Debug($"Power changed: canLightsBeEnabled was {canLightsBeEnabled}, now {now}");
                canLightsBeEnabled = now;
            }
            IsLightsOn = IsLightsOn;
        }

        void IScuttleListener.OnScuttle()
        {
            isScuttled = true;
            IsLightsOn = IsLightsOn;
        }
        void IScuttleListener.OnUnscuttle()
        {
            isScuttled = false;
            IsLightsOn = IsLightsOn;
        }
        void IDockListener.OnDock()
        {
            isDocked = true;
            IsLightsOn = IsLightsOn;
        }
        void IDockListener.OnUndock()
        {
            isDocked = false;
            IsLightsOn = IsLightsOn;
        }
    }
}
