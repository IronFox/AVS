using AVS.BaseVehicle;
using AVS.Interfaces;
using AVS.Log;
using AVS.Util;
using System;
using System.Collections;
using UnityEngine;

namespace AVS.VehicleComponents;

internal class CommonValueThresholdTracker
{
    protected readonly AutopilotStatus[] _statuses;
    protected int _previousLevel;
    public AutopilotStatus CurrentStatus => _statuses[_previousLevel];

    protected CommonValueThresholdTracker(AutopilotStatus[] statuses)
    {
        if (statuses.IsNull())
            throw new ArgumentNullException();
        _statuses = statuses;
    }

    // Returns the index of the highest threshold <= value, or -1 if below all
    protected int GetLevel(float[] thresholds, float value, float factor)
    {
        try
        {
            var level = 0;
            for (var i = 0; i < thresholds.Length; i++)
                if (value >= thresholds[i] * factor)
                    level = i + 1;
                else
                    break;
            return level;
        }
        catch (Exception e)
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
internal class PositiveValueThresholdTracker(params AutopilotStatus[] statuses) : CommonValueThresholdTracker(statuses)
{
    // Call this on update. Returns the event to signal, or null if no event.
    public AutopilotStatusChange? Update(float value, params float[] thresholds)
    {
        if (thresholds.IsNull() || thresholds.Length == 0)
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
                {
                    return null; // No change in level, no event to signal
                }
            }
            catch (IndexOutOfRangeException e)
            {
                LogWriter.Default.Error($"Error in AutopilotEvent Update ({upLevel},{downLevel}): ", e);
                throw;
            }
        }
        catch (Exception e)
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
        {
            return null; // No change in level, no event to signal
        }
    }
}

/// <summary>
/// The Autopilot class manages various autonomous functionalities for a vehicle,
/// including monitoring and responding to power status, health, lights status,
/// and surrounding dangers. It interacts with subsystems and implements
/// multiple listener interfaces for vehicle, player, power, lights, and scuttle events.
/// </summary>
public class Autopilot : MonoBehaviour, IVehicleStatusListener, IPlayerListener, IPowerListener, ILightsStatusListener,
    IScuttleListener
{
    internal EnergyInterface? aiEI;
    internal AvsVehicle av => GetComponent<AvsVehicle>();
    internal LiveMixin liveMixin => av.liveMixin;
    internal EnergyInterface eInterf => av.energyInterface;
    internal LogWriter Log => av.Log.Prefixed("Autopilot");

    private PositiveValueThresholdTracker DepthTracker { get; }
        = new(
            AutopilotStatus.DepthSafe,
            AutopilotStatus.DepthNearCrush,
            AutopilotStatus.DepthBeyondCrush
        );

    private NegativeValueThresholdTracker HealthTracker { get; }
        = new(
            AutopilotStatus.HealthCritical,
            AutopilotStatus.HealthLow,
            AutopilotStatus.HealthSafe
        );

    private NegativeValueThresholdTracker PowerTracker { get; }
        = new(
            AutopilotStatus.PowerDead,
            AutopilotStatus.PowerCritical,
            AutopilotStatus.PowerLow,
            AutopilotStatus.PowerSafe
        );

    /// <summary>
    /// Gets the current health status of the autopilot system.
    /// </summary>
    /// <remarks>
    /// The health status is determined based on predefined thresholds and can represent
    /// various states including safe, low, or critical health conditions. This property
    /// dynamically monitors and updates the health state of the system.
    /// </remarks>
    /// <value>
    /// A value of type <see cref="AutopilotStatus"/> representing the current health status
    /// of the autopilot system.
    /// </value>
    public AutopilotStatus HealthStatus => HealthTracker.CurrentStatus;

    /// <summary>
    /// Gets the current power status of the autopilot system.
    /// </summary>
    /// <remarks>
    /// The power status reflects the energy state of the system, determined by predefined levels.
    /// It provides insight into whether the system is operating within safe, low, critical, or dead power thresholds.
    /// The property dynamically tracks and updates the power condition of the system.
    /// </remarks>
    /// <value>
    /// A value of type <see cref="AutopilotStatus"/> representing the current power status of the autopilot system.
    /// </value>
    public AutopilotStatus PowerStatus => PowerTracker.CurrentStatus;

    /// <summary>
    /// Gets the current depth status of the vehicle in relation to predefined safety thresholds.
    /// </summary>
    /// <remarks>
    /// The depth status is evaluated based on the vehicle's current depth and thresholds defining safe,
    /// near-crush, and beyond-crush levels. This property continuously monitors depth levels to provide
    /// real-time feedback on operational safety related to depth pressure conditions.
    /// </remarks>
    /// <value>
    /// A value of type <see cref="AutopilotStatus"/> indicating the current depth status, such as
    /// <see cref="AutopilotStatus.DepthSafe"/>, <see cref="AutopilotStatus.DepthNearCrush"/>,
    /// or <see cref="AutopilotStatus.DepthBeyondCrush"/>.
    /// </value>
    public AutopilotStatus DepthStatus => DepthTracker.CurrentStatus;

    /// <summary>
    /// Gets the current danger status as determined by the proximity of nearby threats or hazardous environmental conditions.
    /// </summary>
    /// <remarks>
    /// The danger status reflects the level of threat in the current environment, and it is dynamically updated
    /// based on events such as the presence of nearby Leviathans or other critical factors. The status transitions
    /// between predefined categories to indicate the severity of the danger.
    /// </remarks>
    /// <value>
    /// A value of type <see cref="AutopilotStatus"/> representing the current danger status of the autopilot system.
    /// </value>
    public AutopilotStatus DangerStatus { get; private set; } = AutopilotStatus.LeviathanSafe;

#pragma warning disable CS0414
    private bool isDead = false;
#pragma warning restore CS0414

    /// <inheritdoc/>
    public void Awake()
    {
        //av.voice = apVoice = av.gameObject.EnsureComponent<VoiceQueue>();
        //av.voice = apVoice = av.gameObject.EnsureComponent<VoiceQueue>();
        //av.voice.voice = VoiceManager.GetDefaultVoice(av);
        //av.gameObject.EnsureComponent<AutopilotNavigator>();
        DangerStatus = AutopilotStatus.LeviathanSafe;
    }

    /// <inheritdoc/>
    public void Start()
    {
        if (av.Com.BackupBatteries.Count > 0)
            aiEI = av.Com.BackupBatteries[0].Root.GetComponent<EnergyInterface>();
        else
            aiEI = av.energyInterface;
    }

    /// <inheritdoc/>
    public void Update()
    {
        MaybeRefillOxygen();

        if (!av.VehicleIsReady)
            return;
        var listeners = av.GetComponentsInChildren<IAutopilotEventListener>();
        if (listeners.Length == 0)
            return;

        UpdateHealthState(listeners);
        UpdatePowerState(listeners);
        UpdateDepthState(listeners);

        //if (av is Submarine s && s.DoesAutolevel && av.VFEngine is Engines.AvsVehicleEngine)
        //{
        //    MaybeAutoLevel(s);
        //    CheckForDoubleTap(s);
        //}
    }

    //public void MaybeAutoLevel(Submarine av)
    //{
    //    Vector2 lookDir = GameInput.GetLookDelta();
    //    if (autoLeveling && (10f < lookDir.magnitude || !av.GetIsUnderwater()))
    //    {
    //        autoLeveling = false;
    //        return;
    //    }
    //    if ((!isDead || aiEI.hasCharge) && (autoLeveling || !av.IsPlayerControlling()) && av.GetIsUnderwater())
    //    {
    //        if (RollDelta < 0.4f && PitchDelta < 0.4f && av.useRigidbody.velocity.magnitude < av.ExitVelocityLimit)
    //        {
    //            autoLeveling = false;
    //            return;
    //        }
    //        if (RollDelta > 0.4f || PitchDelta > 0.4f)
    //        {
    //            Quaternion desiredRotation = Quaternion.Euler(new Vector3(0, transform.rotation.eulerAngles.y, 0));
    //            // Smoothly move towards target rotation using physics
    //            Quaternion smoothedRotation = Quaternion.RotateTowards(
    //                av.useRigidbody.rotation,
    //                desiredRotation,
    //                smoothTime * Time.deltaTime * autoLevelRate
    //            );
    //            av.useRigidbody.MoveRotation(smoothedRotation);
    //        }
    //    }
    //}
    //private void CheckForDoubleTap(Submarine av)
    //{
    //    if ((!isDead || aiEI.hasCharge) && GameInput.GetButtonDown(GameInput.Button.Exit) && av.IsPlayerControlling())
    //    {
    //        if (Time.time - timeOfLastLevelTap < doubleTapWindow)
    //        {
    //            autoLeveling = true;
    //            var smoothTime1 = 5f * PitchDelta / 90f;
    //            var smoothTime2 = 5f * RollDelta / 90f;
    //            var smoothTime3 = av.GetComponent<AVS.Engines.AvsVehicleEngine>().GetTimeToStop();
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
            if (liveMixin.maxHealth <= 0)
            {
                Log.Error("LiveMixin max health is zero, cannot update health state.");
                return;
            }

            Emit(listeners,
                HealthTracker.Update(liveMixin.health, liveMixin.maxHealth * 0.1f, liveMixin.maxHealth * 0.4f));
        }
        catch (Exception e)
        {
            Log.Error("Error updating health state: ", e);
        }
    }

    private void UpdatePowerState(IAutopilotEventListener[] listeners)
    {
        try
        {
            av.GetEnergyValues(out var totalPower, out var totalCapacity);
            //Log.Debug($"Total power: {totalPower}, Total capacity: {totalCapacity}");
            if (totalCapacity <= 0)
                //Log.Error("Total capacity is zero, cannot update power state.");
                return;
            Emit(listeners, PowerTracker.Update(totalPower, 0.1f, 0.1f * totalCapacity, 0.3f * totalCapacity));
        }
        catch (Exception e)
        {
            Log.Error("Error updating power state: ", e);
        }
    }

    private void UpdateDepthState(IAutopilotEventListener[] listeners)
    {
        try
        {
            var crushDepth = GetComponent<CrushDamage>().crushDepth;
            if (crushDepth <= 0)
            {
                Log.Error("Crush depth is zero or negative, cannot update depth state.");
                return;
            }

            var perilousDepth = crushDepth * 0.9f;
            var depth = transform.position.y;

            Emit(listeners, DepthTracker.Update(-depth, perilousDepth, crushDepth));
        }
        catch (Exception e)
        {
            Log.Error("Error updating depth state: ", e);
        }
    }

    private void Emit(IAutopilotEventListener[] listeners, AutopilotStatusChange? v)
    {
        if (v.HasValue)
            foreach (var listener in listeners)
                listener.Signal(v.Value);
    }

    private void MaybeRefillOxygen()
    {
        var totalPower = av.energyInterface.TotalCanProvide(out _);
        var totalAIPower = eInterf.TotalCanProvide(out _);
        if (totalPower < 0.1 && totalAIPower >= 0.1 && av.IsBoarded)
        {
            // The main batteries are out, so the AI will take over life support.
            var oxygenMgr = Player.main.oxygenMgr;
            oxygenMgr.GetTotal(out var num, out var num2);
            var amount = Mathf.Min(num2 - num, av.oxygenPerSecond * Time.deltaTime) * av.oxygenEnergyCost;
            if (av.aiEnergyInterface.IsNotNull())
            {
                var secondsToAdd = av.aiEnergyInterface.ConsumeEnergy(amount) / av.oxygenEnergyCost;
                oxygenMgr.AddOxygen(secondsToAdd);
            }
        }
    }

    void ILightsStatusListener.OnHeadlightsOn()
    {
        Log.Debug("OnHeadlightsOn");
    }

    void ILightsStatusListener.OnHeadlightsOff()
    {
        Log.Debug("OnHeadlightsOff");
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

    void ILightsStatusListener.OnFloodlightsOn()
    {
        Log.Debug("OnFloodlightsOn");
    }

    void ILightsStatusListener.OnFloodlightsOff()
    {
        Log.Debug("OnFloodlightsOff");
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
        var listeners = av.GetComponentsInChildren<IAutopilotEventListener>();
        listeners.ForEach(l => l.Signal(AutopilotEvent.PowerUp));
        if (av.IsBoarded)
        {
            IEnumerator ShakeCamera()
            {
                yield return new WaitForSeconds(4.6f);
                MainCameraControl.main.ShakeCamera(1f, 0.5f);
            }

            MainPatcher.AnyInstance.StartCoroutine(ShakeCamera());
            MainCameraControl.main.ShakeCamera(0.15f, 4.5f);
        }
    }

    void IPowerListener.OnPowerDown()
    {
        Log.Debug("OnPowerDown");
        isDead = true;
        av.GetComponentsInChildren<IAutopilotEventListener>()
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
        av.GetComponentsInChildren<IAutopilotEventListener>()
            .ForEach(l => l.Signal(AutopilotEvent.PlayerEntry));
    }

    void IPlayerListener.OnPlayerExit()
    {
        Log.Debug("OnPlayerExit");
        av.GetComponentsInChildren<IAutopilotEventListener>()
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
        av.GetComponentsInChildren<IAutopilotEventListener>()
            .ForEach(l => l.Signal(new AutopilotStatusChange(was, AutopilotStatus.PowerDead)));
    }

    void IPowerListener.OnBatteryRevive()
    {
        Log.Debug("OnBatteryRevive");
    }


    private readonly float MAX_TIME_TO_WAIT = 3f;
    private float timeWeStartedWaiting = 0f;

    void IVehicleStatusListener.OnNearbyLeviathan()
    {
        Log.Debug("OnNearbyLeviathan");

        IEnumerator ResetDangerStatusEventually()
        {
            yield return new WaitUntil(() => Mathf.Abs(Time.time - timeWeStartedWaiting) >= MAX_TIME_TO_WAIT);
            var was = DangerStatus;
            DangerStatus = AutopilotStatus.LeviathanSafe;
            if (was != DangerStatus)
                av.GetComponentsInChildren<IAutopilotEventListener>()
                    .ForEach(l => l.Signal(new AutopilotStatusChange(was, DangerStatus)));
        }

        StopAllCoroutines();
        timeWeStartedWaiting = Time.time;
        MainPatcher.AnyInstance.StartCoroutine(ResetDangerStatusEventually());
        if (DangerStatus == AutopilotStatus.LeviathanSafe)
        {
            var was = DangerStatus;
            DangerStatus = AutopilotStatus.LeviathanNearby;
            if (was != DangerStatus)
                av.GetComponentsInChildren<IAutopilotEventListener>()
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