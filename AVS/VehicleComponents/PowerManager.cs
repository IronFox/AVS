using AVS.BaseVehicle;
using UnityEngine;

namespace AVS
{
    /*
     * The PowerManager handles all power drains for the AvsVehicle.
     * It also monitors the two-bit power status of the AvsVehicle.
     * It also handles the broadcasting of all power-related notifications.
     * We would like very much do consolidate all our power drains here
     * This is trivial for lights,
     * but a bit more difficult for driving
     */
    /// <summary>
    /// The PowerManager class is responsible for managing all power-related functionalities for the AvsVehicle.
    /// </summary>
    /// <remarks>
    /// This class handles power consumption and evaluates the power status of the vehicle. It monitors various power drains,
    /// including lights and driving mechanics, and consolidates these drains into a unified management system.
    /// Additionally, it broadcasts notifications related to power changes and offers methods to check and spend energy.
    /// </remarks>
    public class PowerManager : MonoBehaviour, ILightsStatusListener
    {
        /// <summary>
        /// The PowerStatus structure represents the binary power state of an entity,
        /// encapsulating whether it has sufficient charge and is operationally powered.
        /// </summary>
        /// <remarks>
        /// This structure is used to define and evaluate power conditions within the AvsVehicle's power management system.
        /// It provides predefined status states, including ChargedAndPowered, and allows for quick evaluation of combined charge and power states.
        /// </remarks>
        /// <param name="HasCharge">The vehicle has charge in its batteries</param>
        /// <param name="IsPowered">The vehicle is powered on</param>
        public readonly record struct PowerStatus(bool HasCharge, bool IsPowered)
        {
            /// <summary>
            /// Represents a static predefined power status indicating that the vehicle
            /// is both fully charged and has active power.
            /// </summary>
            public static PowerStatus ChargedAndPowered { get; } = new PowerStatus(true, true);

            /// <summary>
            /// Indicates whether the entity has sufficient charge and is operationally powered.
            /// </summary>
            /// <remarks>
            /// Combines the charge and power state to provide a single true/false evaluation
            /// of whether the entity is both charged and powered. This is typically used
            /// within the power management system for decision-making processes.
            /// </remarks>
            public bool IsChargedAndPowered => HasCharge && IsPowered;
        }

        private PowerStatus lastStatus = new PowerStatus (false,false);
        private PowerEvent latestPowerEvent = PowerEvent.OnBatterySafe;
        private bool isHeadlightsOn = false;
        private bool isFloodlightsOn = false;
        private bool isNavLightsOn = false;
        private bool isInteriorLightsOn = false;
        private AvsVehicle av => GetComponent<AvsVehicle>();
        private EnergyInterface ei => GetComponent<EnergyInterface>();

        private PowerEvent EvaluatePowerEvent()
        {
            av.energyInterface.GetValues(out float charge, out _);
            if (charge < 5)
            {
                return PowerEvent.OnBatteryDepleted;
            }
            else if (charge < 100)
            {
                return PowerEvent.OnBatteryNearlyEmpty;
            }
            else if (charge < 320)
            {
                return PowerEvent.OnBatteryLow;
            }
            else
            {
                return PowerEvent.OnBatterySafe;
            }
        }

        /// <summary>
        /// Evaluates and determines the current power status of the AvsVehicle.
        /// It checks whether the vehicle has remaining charge and whether it is powered on.
        /// </summary>
        /// <returns>A <see cref="PowerManager.PowerStatus"/> indicating the vehicle's power state,
        /// including whether it has charge and whether it is powered on.</returns>
        public PowerStatus EvaluatePowerStatus()
        {
            av.energyInterface.GetValues(out float charge, out _);
            return new PowerStatus
            (
                IsPowered: av.IsPoweredOn,
                HasCharge: charge > 0
            );
        }

        /// <summary>
        /// Attempts to spend the specified amount of energy from the energy source.
        /// The method will consume as much energy as possible up to the requested amount,
        /// depending on the available energy.
        /// </summary>
        /// <param name="val">The amount of energy requested to be spent.</param>
        /// <returns>The actual amount of energy that was successfully consumed.</returns>
        public float TrySpendEnergy(float val)
        {
            float desired = val;
            float available = ei.TotalCanProvide(out _);
            if (available < desired)
            {
                desired = available;
            }
            return ei.ConsumeEnergy(desired);
        }
        
        private void AccountForTheTypicalDrains()
        {
            /*
             * research suggests engines should be between 10 and 100x more draining than the lights
             * engine takes [0,3], so we're justified for either [0,0.3] or [0,0.03]
             * we chose [0,0.1] for the lights
             */
            if (isHeadlightsOn)
            {
                TrySpendEnergy(0.01f * Time.deltaTime);
            }
            if (isFloodlightsOn)
            {
                TrySpendEnergy(0.1f * Time.deltaTime);
            }
            if (isNavLightsOn)
            {
                TrySpendEnergy(0.001f * Time.deltaTime);
            }
            if (isInteriorLightsOn)
            {
                TrySpendEnergy(0.001f * Time.deltaTime);
            }
            // if (isAutoLeveling)
            // {
            //     float scalarFactor = 1.0f;
            //     float basePowerConsumptionPerSecond = .15f;
            //     float upgradeModifier = Mathf.Pow(0.85f, av.NumEfficiencyModules);
            //     TrySpendEnergy(scalarFactor * basePowerConsumptionPerSecond * upgradeModifier * Time.deltaTime);
            // }
            // if (isAutopiloting)
            // {
            //     float scalarFactor = 1.0f;
            //     float basePowerConsumptionPerSecond = 3f;
            //     float upgradeModifier = Mathf.Pow(0.85f, av.NumEfficiencyModules);
            //     TrySpendEnergy(scalarFactor * basePowerConsumptionPerSecond * upgradeModifier * Time.deltaTime);
            // }
        }
        /// <inheritdoc />
        public void Update()
        {
            AccountForTheTypicalDrains();

            PowerEvent currentPowerEvent = EvaluatePowerEvent();
            PowerStatus currentPowerStatus = EvaluatePowerStatus();

            if (currentPowerStatus != lastStatus)
            {
                NotifyPowerChanged(currentPowerStatus.HasCharge, currentPowerStatus.IsPowered);
                NotifyBatteryChanged(currentPowerStatus, lastStatus);
                lastStatus = currentPowerStatus;
                latestPowerEvent = currentPowerEvent;
                return;
            }

            if (currentPowerStatus.IsChargedAndPowered)
            {
                if (currentPowerEvent != latestPowerEvent)
                {
                    latestPowerEvent = currentPowerEvent;
                    NotifyPowerStatus(currentPowerEvent);
                }
            }
        }
        private void NotifyPowerChanged(bool isBatteryCharged, bool isSwitchedOn)
        {
            foreach (var component in GetComponentsInChildren<IPowerChanged>())
            {
                (component as IPowerChanged).OnPowerChanged(hasBatteryPower: isBatteryCharged, isSwitchedOn:isSwitchedOn);
            }
        }
        private void NotifyPowerStatus(PowerEvent newEvent)
        {
            foreach (var component in GetComponentsInChildren<IPowerListener>())
            {
                switch (newEvent)
                {
                    case PowerEvent.OnBatterySafe:
                        component.OnBatterySafe();
                        break;
                    case PowerEvent.OnBatteryLow:
                        component.OnBatteryLow();
                        break;
                    case PowerEvent.OnBatteryNearlyEmpty:
                        component.OnBatteryNearlyEmpty();
                        break;
                    case PowerEvent.OnBatteryDepleted:
                        component.OnBatteryDepleted();
                        break;
                    default:
                        Logger.Error("Error: tried to notify using an invalid status");
                        break;
                }
            }
        }
        private void NotifyBatteryChanged(PowerStatus newPS, PowerStatus oldPS)
        {
            foreach (var component in GetComponentsInChildren<IPowerListener>())
            {
                if (oldPS.IsPowered != newPS.IsPowered)
                {
                    if (newPS.IsPowered)
                    {
                        component.OnPowerUp();
                    }
                    else
                    {
                        component.OnPowerDown();
                    }
                }
                if (oldPS.HasCharge != newPS.HasCharge)
                {
                    if (newPS.HasCharge)
                    {
                        component.OnBatteryRevive();
                    }
                    else
                    {
                        component.OnBatteryDead();
                    }
                }
            }
        }

        void ILightsStatusListener.OnFloodlightsOff()
        {
            isFloodlightsOn = false;
        }

        void ILightsStatusListener.OnFloodlightsOn()
        {
            isFloodlightsOn = true;
        }

        void ILightsStatusListener.OnHeadlightsOff()
        {
            isHeadlightsOn = false;
        }

        void ILightsStatusListener.OnHeadlightsOn()
        {
            isHeadlightsOn = true;
        }

        void ILightsStatusListener.OnInteriorLightsOff()
        {
            isInteriorLightsOn = false;
        }

        void ILightsStatusListener.OnInteriorLightsOn()
        {
            isInteriorLightsOn = true;
        }

        void ILightsStatusListener.OnNavLightsOff()
        {
            isNavLightsOn = false;
        }

        void ILightsStatusListener.OnNavLightsOn()
        {
            isNavLightsOn = true;
        }
    }
}
