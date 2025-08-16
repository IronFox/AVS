using System;
using AVS.BaseVehicle;
using AVS.Composition;
using AVS.Configuration;

namespace AVS.VehicleTypes;

/// <summary>
///     Incomplete surface boat class.
/// </summary>
public abstract class Skimmer : AvsVehicle
{
    private SkimmerComposition? _skimmerConfig;


    /// <summary>
    ///     Indicates whether the player is currently inside the skimmer vehicle.
    /// </summary>
    /// <remarks>
    ///     This variable determines the player's status regarding their presence inside the vehicle. It can be
    ///     used to trigger actions or behaviors based on the player's occupancy state.
    /// </remarks>
    protected bool isPlayerInside = false;

    /// <summary>
    ///     Constructs the vehicle with the given configuration.
    /// </summary>
    /// <param name="config">Vehicle configuration. Must not be null</param>
    /// <exception cref="ArgumentNullException"></exception>
    public Skimmer(VehicleConfiguration config) : base(config)
    {
    }

    /// <summary>
    ///     Gets the composition configuration specific to the skimmer vehicle.
    ///     Provides access to the <see cref="SkimmerComposition" /> for managing
    ///     specialized vehicle behavior and properties.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if the skimmer's composition has not been initialized.
    ///     Ensure that the <c>Skimmer.Awake()</c> method is invoked prior to accessing this property.
    /// </exception>
    public new SkimmerComposition Com =>
        _skimmerConfig
        ?? throw new InvalidOperationException(
            "This vehicle's composition has not yet been initialized. Please wait until Skimmer.Awake() has been called");

    /// <summary>
    ///     Retrieves the composition of the skimmer.
    ///     Executed once either during <see cref="AvsVehicle.Awake()" /> or vehicle registration, whichever comes first.
    /// </summary>
    protected abstract SkimmerComposition GetSkimmerComposition();

    /// <inheritdoc />
    protected sealed override VehicleComposition GetVehicleComposition()
    {
        _skimmerConfig = GetSkimmerComposition();
        return _skimmerConfig;
    }

    /// <summary>
    ///     Determines whether the player is currently inside the skimmer.
    /// </summary>
    /// <returns>
    ///     True if the player is inside the skimmer; otherwise, false.
    /// </returns>
    public bool IsPlayerInside()
    {
        // this one is correct ?
        return isPlayerInside;
    }

    /// <inheritdoc />
    protected internal override void DoExitRoutines()
    {
        var myPlayer = Player.main;
        var myMode = myPlayer.mode;

        DoCommonExitActions(ref myMode);
        myPlayer.mode = myMode;
        EndHelmControl(0.5f);
    }
}