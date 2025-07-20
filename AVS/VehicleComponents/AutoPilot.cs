using AVS.BaseVehicle;
using AVS.Interfaces;
using AVS.Log;
using AVS.VehicleComponents;
using System;
using System.Collections;
using UnityEngine;

namespace AVS
{
    internal readonly struct ThresholdWindow
    {
        public ThresholdWindow(float from, float to, AutopilotStatus eventIfContained)
        {
            From = from;
            To = to;
            EventIfContained = eventIfContained;
        }

        public float From { get; }
        public float To { get; }
        public AutopilotStatus EventIfContained { get; }

        public bool Contains(float value)
        {
            return value >= From && value < To;
        }
    }

    internal class CommonValueThresholdTracker
    {
        protected readonly AutopilotStatus[] _statuses;
        protected int _previousLevel;
        public AutopilotStatus CurrentStatus => _statuses[_previousLevel];

        protected CommonValueThresholdTracker(AutopilotStatus[] statuses)
        {
            if (statuses == null)
                throw new System.ArgumentNullException();
            _statuses = statuses;
        }

        // Returns the index of the highest threshold <= value, or -1 if below all
        protected int GetLevel(float[] thresholds, float value, float factor)
        {
            try
            {
                int level = 0;
                for (int i = 0; i < thresholds.Length; i++)
                {
                    if (value >= thresholds[i] * factor)
                        level = i + 1;
                    else
                        break;
                }
                return level;
            }
            catch (System.Exception e)
            {
                LogWriter.Default.Error($"Error finding level ({value} in {thresholds} x{factor}): ", e);
                throw;
            }
        }

        internal void SetLevel(int v)
        {
            if (v < 0 || v >= _statuses.Length)
                throw new IndexOutOfRangeException($"Invalid level index {v} for events {_statuses.Length}");
            _previousLevel = v;
        }
        internal void SetLevel(AutopilotStatus v)
        {
            SetLevel(Array.IndexOf(_statuses, v));
        }

    }

    /// <summary>
    /// Tracker for statuses where high values indicate worse status.
    /// </summary>
    internal class PositiveValueThresholdTracker : CommonValueThresholdTracker
    {


        public PositiveValueThresholdTracker(params AutopilotStatus[] statuses)
            : base(statuses)
        { }


        // Call this on update. Returns the event to signal, or null if no event.
        public AutopilotStatusChange? Update(float value, params float[] thresholds)
        {
            if (thresholds == null || thresholds.Length == 0)
            {
                Logger.Error("No thresholds provided for PositiveValueThresholdTracker.Update");
                return null;
            }
            try
            {
                var upLevel = GetLevel(thresholds, value, 1f);
                var downLevel = GetLevel(thresholds, value, 0.95f);

                try
                {

                    if (upLevel > _previousLevel)
                    {
                        // Level increased
                        var was = CurrentStatus;
                        _previousLevel = upLevel;
                        return new AutopilotStatusChange(was, _statuses[upLevel]);
                    }
                    else if (downLevel < _previousLevel)
                    {
                        // Level decreased
                        var was = CurrentStatus;
                        _previousLevel = downLevel;
                        return new AutopilotStatusChange(was, _statuses[downLevel]);
                    }
                    else
                        return null; // No change in level, no event to signal
                }
                catch (System.IndexOutOfRangeException e)
                {
                    LogWriter.Default.Error($"Error in AutopilotEvent Update ({upLevel},{downLevel}): ", e);
                    throw;
                }
            }
            catch (System.Exception e)
            {
                LogWriter.Default.Error("Error finding levels: ", e);
                throw;
            }
        }

    }


    /// <summary>
    /// Tracker for statuses where lower values indicate worse status.
    /// </summary>
    internal class NegativeValueThresholdTracker : CommonValueThresholdTracker
    {

        public NegativeValueThresholdTracker(params AutopilotStatus[] statuses)
            : base(statuses)
        {
            _previousLevel = statuses.Length - 1; // Start at the highest level
        }

        // Call this on update. Returns the event to signal, or null if no event.
        public AutopilotStatusChange? Update(float value, params float[] thresholds)
        {
            var upLevel = GetLevel(thresholds, value, 1.05f);
            var downLevel = GetLevel(thresholds, value, 1f);
            if (upLevel > _previousLevel)
            {
                // Level increased
                var was = CurrentStatus;
                _previousLevel = upLevel;
                return new AutopilotStatusChange(was, _statuses[upLevel]);
            }
            else if (downLevel < _previousLevel)
            {
                // Level decreased
                var was = CurrentStatus;
                _previousLevel = downLevel;
                return new AutopilotStatusChange(was, _statuses[downLevel]);
            }
            else
                return null; // No change in level, no event to signal
        }

    }


    public class Autopilot : MonoBehaviour, IVehicleStatusListener, IPlayerListener, IPowerListener, ILightsStatusListener, IScuttleListener
    {
        internal EnergyInterface? aiEI;
        internal AvsVehicle mv => GetComponent<AvsVehicle>();
        internal LiveMixin liveMixin => mv.liveMixin;
        internal EnergyInterface eInterf => mv.energyInterface;
        internal LogWriter Log => mv.Log.Prefixed("Autopilot");

        private PositiveValueThresholdTracker DepthTracker { get; }
            = new PositiveValueThresholdTracker(
                AutopilotStatus.DepthSafe,
                AutopilotStatus.DepthNearCrush,
                AutopilotStatus.DepthBeyondCrush
            );
        private NegativeValueThresholdTracker HealthTracker { get; }
            = new NegativeValueThresholdTracker(
                AutopilotStatus.HealthCritical,
                AutopilotStatus.HealthLow,
                AutopilotStatus.HealthSafe
            );

        private NegativeValueThresholdTracker PowerTracker { get; }
            = new NegativeValueThresholdTracker(
                AutopilotStatus.PowerDead,
                AutopilotStatus.PowerCritical,
                AutopilotStatus.PowerLow,
                AutopilotStatus.PowerSafe
            );

        public AutopilotStatus HealthStatus => HealthTracker.CurrentStatus;
        public AutopilotStatus PowerStatus => PowerTracker.CurrentStatus;
        public AutopilotStatus DepthStatus => DepthTracker.CurrentStatus;
        public enum DangerState
        {
            Safe,
            LeviathanNearby,
        }
        public AutopilotStatus DangerStatus { get; private set; } = AutopilotStatus.LeviathanSafe;

#pragma warning disable CS0414
        private bool isDead = false;
#pragma warning restore CS0414

        public void Awake()
        {
            //mv.voice = apVoice = mv.gameObject.EnsureComponent<VoiceQueue>();
            //mv.voice = apVoice = mv.gameObject.EnsureComponent<VoiceQueue>();
            //mv.voice.voice = VoiceManager.GetDefaultVoice(mv);
            mv.gameObject.EnsureComponent<AutoPilotNavigator>();
            DangerStatus = AutopilotStatus.LeviathanSafe;
        }
        public void Start()
        {
            if (mv.Com.BackupBatteries.Count > 0)
            {
                aiEI = mv.Com.BackupBatteries[0].BatterySlot.GetComponent<EnergyInterface>();
            }
            else
            {
                aiEI = mv.energyInterface;
            }
        }

        public void Update()
        {
            MaybeRefillOxygen();

            var listeners = mv.GetComponentsInChildren<IAutopilotEventListener>();
            if (listeners.Length == 0)
                return;

            UpdateHealthState(listeners);
            UpdatePowerState(listeners);
            UpdateDepthState(listeners);

            //if (mv is Submarine s && s.DoesAutolevel && mv.VFEngine is Engines.ModVehicleEngine)
            //{
            //    MaybeAutoLevel(s);
            //    CheckForDoubleTap(s);
            //}
        }
        //public void MaybeAutoLevel(Submarine mv)
        //{
        //    Vector2 lookDir = GameInput.GetLookDelta();
        //    if (autoLeveling && (10f < lookDir.magnitude || !mv.GetIsUnderwater()))
        //    {
        //        autoLeveling = false;
        //        return;
        //    }
        //    if ((!isDead || aiEI.hasCharge) && (autoLeveling || !mv.IsPlayerControlling()) && mv.GetIsUnderwater())
        //    {
        //        if (RollDelta < 0.4f && PitchDelta < 0.4f && mv.useRigidbody.velocity.magnitude < mv.ExitVelocityLimit)
        //        {
        //            autoLeveling = false;
        //            return;
        //        }
        //        if (RollDelta > 0.4f || PitchDelta > 0.4f)
        //        {
        //            Quaternion desiredRotation = Quaternion.Euler(new Vector3(0, transform.rotation.eulerAngles.y, 0));
        //            // Smoothly move towards target rotation using physics
        //            Quaternion smoothedRotation = Quaternion.RotateTowards(
        //                mv.useRigidbody.rotation,
        //                desiredRotation,
        //                smoothTime * Time.deltaTime * autoLevelRate
        //            );
        //            mv.useRigidbody.MoveRotation(smoothedRotation);
        //        }
        //    }
        //}
        //private void CheckForDoubleTap(Submarine mv)
        //{
        //    if ((!isDead || aiEI.hasCharge) && GameInput.GetButtonDown(GameInput.Button.Exit) && mv.IsPlayerControlling())
        //    {
        //        if (Time.time - timeOfLastLevelTap < doubleTapWindow)
        //        {
        //            autoLeveling = true;
        //            var smoothTime1 = 5f * PitchDelta / 90f;
        //            var smoothTime2 = 5f * RollDelta / 90f;
        //            var smoothTime3 = mv.GetComponent<AVS.Engines.ModVehicleEngine>().GetTimeToStop();
        //            smoothTime = Mathf.Max(smoothTime1, smoothTime2, smoothTime3);
        //        }
        //        else
        //        {
        //            timeOfLastLevelTap = Time.time;
        //        }
        //    }
        //}
        private void UpdateHealthState(IAutopilotEventListener[] listeners)
        {
            try
            {
                Emit(listeners, HealthTracker.Update(liveMixin.health, liveMixin.maxHealth * 0.1f, liveMixin.maxHealth * 0.4f));
            }
            catch (System.Exception e)
            {
                Log.Error("Error updating health state: ", e);
            }
        }
        private void UpdatePowerState(IAutopilotEventListener[] listeners)
        {
            try
            {
                mv.GetEnergyValues(out float totalPower, out float totalCapacity);
                Emit(listeners, PowerTracker.Update(totalPower, 0.1f, 0.1f * totalCapacity, 0.3f * totalCapacity));
            }
            catch (System.Exception e)
            {
                Log.Error("Error updating power state: ", e);
            }
        }
        private void UpdateDepthState(IAutopilotEventListener[] listeners)
        {
            try
            {
                float crushDepth = GetComponent<CrushDamage>().crushDepth;
                float perilousDepth = crushDepth * 0.9f;
                float depth = transform.position.y;

                Emit(listeners, DepthTracker.Update(-depth, perilousDepth, crushDepth));
            }
            catch (System.Exception e)
            {
                Log.Error("Error updating depth state: ", e);
            }
        }

        private void Emit(IAutopilotEventListener[] listeners, AutopilotStatusChange? v)
        {
            if (v.HasValue)
            {
                foreach (var listener in listeners)
                {
                    listener.Signal(v.Value);
                }
            }
        }

        private void MaybeRefillOxygen()
        {
            float totalPower = mv.energyInterface.TotalCanProvide(out _);
            float totalAIPower = eInterf.TotalCanProvide(out _);
            if (totalPower < 0.1 && totalAIPower >= 0.1 && mv.IsBoarded)
            {
                // The main batteries are out, so the AI will take over life support.
                OxygenManager oxygenMgr = Player.main.oxygenMgr;
                oxygenMgr.GetTotal(out float num, out float num2);
                float amount = Mathf.Min(num2 - num, mv.oxygenPerSecond * Time.deltaTime) * mv.oxygenEnergyCost;
                if (mv.aiEnergyInterface != null)
                {
                    float secondsToAdd = mv.aiEnergyInterface.ConsumeEnergy(amount) / mv.oxygenEnergyCost;
                    oxygenMgr.AddOxygen(secondsToAdd);
                }
            }
        }

        void ILightsStatusListener.OnHeadLightsOn()
        {
            Log.Debug("OnHeadLightsOn");
        }

        void ILightsStatusListener.OnHeadLightsOff()
        {
            Log.Debug("OnHeadLightsOff");
        }

        void ILightsStatusListener.OnInteriorLightsOn()
        {
            Log.Debug("OnInteriorLightsOn");
        }

        void ILightsStatusListener.OnInteriorLightsOff()
        {
            Log.Debug("OnInteriorLightsOff");
        }

        void ILightsStatusListener.OnNavLightsOn()
        {
            Log.Debug("OnNavLightsOn");
        }

        void ILightsStatusListener.OnNavLightsOff()
        {
            Log.Debug("OnNavLightsOff");
        }

        void ILightsStatusListener.OnFloodLightsOn()
        {
            Log.Debug("OnFloodLightsOn");
        }

        void ILightsStatusListener.OnFloodLightsOff()
        {
            Log.Debug("OnFloodLightsOff");
        }

        void IVehicleStatusListener.OnTakeDamage()
        {
            Log.Debug("OnTakeDamage");
        }

        void IPowerListener.OnPowerUp()
        {
            Log.Debug("OnPowerUp");
            isDead = false;
            //apVoice.EnqueueClip(apVoice.voice.EnginePoweringUp);
            var listeners = mv.GetComponentsInChildren<IAutopilotEventListener>();
            listeners.ForEach(l => l.Signal(AutopilotEvent.PowerUp));
            if (mv.IsBoarded)
            {
                IEnumerator ShakeCamera()
                {
                    yield return new WaitForSeconds(4.6f);
                    MainCameraControl.main.ShakeCamera(1f, 0.5f, MainCameraControl.ShakeMode.Linear, 1f);
                }
                UWE.CoroutineHost.StartCoroutine(ShakeCamera());
                MainCameraControl.main.ShakeCamera(0.15f, 4.5f, MainCameraControl.ShakeMode.Linear, 1f);
            }
        }

        void IPowerListener.OnPowerDown()
        {
            Log.Debug("OnPowerDown");
            isDead = true;
            mv.GetComponentsInChildren<IAutopilotEventListener>()
                .ForEach(l => l.Signal(AutopilotEvent.PowerDown));
        }

        void IPowerListener.OnBatterySafe()
        {
            Log.Debug("OnBatterySafe");
        }

        void IPowerListener.OnBatteryLow()
        {
            Log.Debug("OnBatteryLow");
        }

        void IPowerListener.OnBatteryNearlyEmpty()
        {
            Log.Debug("OnBatteryNearlyEmpty");
        }

        void IPowerListener.OnBatteryDepleted()
        {
            Log.Debug("OnBatteryDepleted");
        }

        void IPlayerListener.OnPlayerEntry()
        {
            Log.Debug("OnPlayerEntry");
            mv.GetComponentsInChildren<IAutopilotEventListener>()
                .ForEach(l => l.Signal(AutopilotEvent.PlayerEntry));
        }

        void IPlayerListener.OnPlayerExit()
        {
            Log.Debug("OnPlayerExit");
            mv.GetComponentsInChildren<IAutopilotEventListener>()
                .ForEach(l => l.Signal(AutopilotEvent.PlayerExit));
        }

        void IPlayerListener.OnPilotBegin()
        {
            Log.Debug("OnPilotBegin");
        }

        void IPlayerListener.OnPilotEnd()
        {
            Log.Debug("OnPilotEnd");
        }

        void IPowerListener.OnBatteryDead()
        {
            Log.Debug("OnBatteryDead");
            var was = PowerTracker.CurrentStatus;
            PowerTracker.SetLevel(AutopilotStatus.PowerDead); // Reset power tracker to dead state
            mv.GetComponentsInChildren<IAutopilotEventListener>()
                .ForEach(l => l.Signal(new AutopilotStatusChange(was, AutopilotStatus.PowerDead)));

        }

        void IPowerListener.OnBatteryRevive()
        {
            Log.Debug("OnBatteryRevive");
        }


        readonly float MAX_TIME_TO_WAIT = 3f;
        float timeWeStartedWaiting = 0f;
        void IVehicleStatusListener.OnNearbyLeviathan()
        {
            Log.Debug("OnNearbyLeviathan");
            IEnumerator ResetDangerStatusEventually()
            {
                yield return new WaitUntil(() => Mathf.Abs(Time.time - timeWeStartedWaiting) >= MAX_TIME_TO_WAIT);
                var was = DangerStatus;
                DangerStatus = AutopilotStatus.LeviathanSafe;
                if (was != DangerStatus)
                    mv.GetComponentsInChildren<IAutopilotEventListener>()
                        .ForEach(l => l.Signal(new AutopilotStatusChange(was, DangerStatus)));
            }
            StopAllCoroutines();
            timeWeStartedWaiting = Time.time;
            UWE.CoroutineHost.StartCoroutine(ResetDangerStatusEventually());
            if (DangerStatus == AutopilotStatus.LeviathanSafe)
            {
                var was = DangerStatus;
                DangerStatus = AutopilotStatus.LeviathanNearby;
                if (was != DangerStatus)
                    mv.GetComponentsInChildren<IAutopilotEventListener>()
                        .ForEach(l => l.Signal(new AutopilotStatusChange(was, DangerStatus)));
            }
        }

        void IScuttleListener.OnScuttle()
        {
            Log.Debug("OnScuttle");
            enabled = false;
        }

        void IScuttleListener.OnUnscuttle()
        {
            Log.Debug(nameof(IScuttleListener.OnUnscuttle));
            enabled = true;
        }
    }
}
