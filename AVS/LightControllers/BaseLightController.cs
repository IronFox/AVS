using AVS.Log;
using UnityEngine;

namespace AVS
{
    public abstract class BaseLightController : MonoBehaviour, IPowerChanged, IScuttleListener, IDockListener
    {
        private bool canLightsBeEnabled = true;
        private bool isDocked = false;
        private bool isScuttled = false;
        private bool _isLightsOn = false;
        public bool IsLightsOn
        {
            get
            {
                return _isLightsOn;
            }
            private set
            {
                bool oldValue = _isLightsOn;
                if (oldValue == value)
                {
                    return; // no change
                }
                LogWriter.Default.Debug($"Setting IsLightsOn to {value} (was {oldValue}) for {gameObject.name}, with canLightsBeEnabled={canLightsBeEnabled}, isScuttled={isScuttled}, isDocked={isDocked}");
                if (canLightsBeEnabled && !isScuttled && !isDocked)
                {
                    _isLightsOn = value;
                }
                else
                {
                    _isLightsOn = false;
                }
                HandleLighting(IsLightsOn);
                if (oldValue != IsLightsOn)
                {
                    HandleSound(IsLightsOn);
                }
            }
        }
        protected abstract void HandleLighting(bool active);
        protected abstract void HandleSound(bool playSound);
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
