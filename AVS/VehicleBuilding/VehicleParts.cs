using AVS.Localization;
using AVS.Log;
using AVS.Util;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AVS.VehicleBuilding;

/// <summary>
/// Represents the control interface for a vehicle, typically used for piloting.
/// The helm defines the positions and orientations of the pilot seat,
/// controls, and exit points, as well as whether helm requires a seated position.
/// </summary>
public readonly struct Helm
{
    /// <summary>
    /// True if the player is expected to be seated at this helm.
    /// </summary>
    public bool IsSeated { get; }

    /// <summary>
    /// The helm root object
    /// </summary>
    public GameObject Root { get; }

    /// <summary>
    /// The location at which the player is inserted when entering helm control
    /// </summary>
    public GameObject PlayerControlLocation { get; }

    /// <summary>
    /// The exit location when the player exits the helm. Can be null
    /// </summary>
    public Transform? ExitLocation { get; }


    /// <summary>
    /// This is what the player's left hand will 'grab' while you pilot.
    /// Can be null if the vehicle does not have a steering wheel.
    /// </summary>
    public Transform? LeftHandTarget { get; }

    /// <summary>
    /// This is what the player's right hand will 'grab' while you pilot.
    /// Can be null if the vehicle does not have a steering wheel.
    /// </summary>
    public Transform? RightHandTarget { get; }

    /// <summary>
    /// Constructs a new instance of <see cref="Helm"/>.
    /// </summary>
    /// <param name="root">The helm root object</param>
    /// <param name="playerControlLocation">The location at which the player is inserted when entering helm control</param>
    /// <param name="exitLocation"></param>
    /// <param name="isSeated"></param>
    /// <param name="steeringWheelLeftHandTarget"></param>
    /// <param name="steeringWheelRightHandTarget"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public Helm(
        GameObject root,
        GameObject playerControlLocation,
        Transform? exitLocation,
        bool isSeated,
        Transform? steeringWheelLeftHandTarget = null,
        Transform? steeringWheelRightHandTarget = null
    )
    {
        if (root.IsNull())
            throw new ArgumentNullException(nameof(root), "Vehicle pilot seat root cannot be null.");
        if (playerControlLocation.IsNull())
            throw new ArgumentNullException(nameof(playerControlLocation),
                "Vehicle pilot sit location cannot be null.");
        LeftHandTarget = steeringWheelLeftHandTarget;
        RightHandTarget = steeringWheelRightHandTarget;
        Root = root;
        PlayerControlLocation = playerControlLocation;
        ExitLocation = exitLocation;
        IsSeated = isSeated;
    }

    /// <summary>
    /// Walking position, just behind the chair.
    /// Todo: Should the configured exit location be used instead?
    /// </summary>
    public Vector3 CalculatedExitLocation =>
        Root.transform.position
        - Root.transform.forward * 1
        + Root.transform.up * 1f;

    /// <summary>
    /// Gets the exit location for the player when exiting the vehicle
    /// or calculates one.
    /// </summary>
    public Vector3 AnyExitLocation =>
        ExitLocation.IsNotNull()
            ? ExitLocation.position
            : CalculatedExitLocation;

    internal bool CheckValidity(RootModController rmc, string thisName, bool verbose)
    {
        using var log = SmartLog.ForAVS(rmc);

        if (ExitLocation.IsNull())
            VehicleRegistrar.VerboseLog(log, VehicleRegistrar.LogType.Warn, verbose,
                thisName +
                "A null PilotSeat.ExitLocation was provided. You might need this if you exit from piloting into a weird place.");
        if (LeftHandTarget.IsNull())
            VehicleRegistrar.VerboseLog(log, VehicleRegistrar.LogType.Log, verbose,
                thisName +
                "A null AvsVehicle.SteeringWheelLeftHandTarget was provided. This is what the player's left hand will 'grab' while you pilot.");
        if (RightHandTarget.IsNull())
            VehicleRegistrar.VerboseLog(log, VehicleRegistrar.LogType.Log, verbose,
                thisName +
                "A null AvsVehicle.SteeringWheelRightHandTarget was provided.  This is what the player's right hand will 'grab' while you pilot.");


        if (Root.IsNull())
        {
            log.Error(thisName + "A null PilotSeat.Seat was provided. There would be no way to pilot this vehicle.");
            return false;
        }

        if (PlayerControlLocation.IsNull())
        {
            log.Error(thisName +
                      "A null PilotSeat.SitLocation was provided. There would be no way to pilot this vehicle.");
            return false;
        }

        return true;
    }
}

/// <summary>
/// Defines the attributes and locations associated with a vehicle hatch.
/// A vehicle hatch includes the hatch object itself and specified transform locations
/// for entry, exit, and surface exit points. These attributes are crucial for
/// handling the entry and exit mechanics of a vehicle.
/// </summary>
public readonly struct VehicleHatchDefinition
{
    /// <summary>
    /// The root object of the hatch itself. Should contain a collider so that a hand target can be assigned
    /// </summary>
    public GameObject Hatch { get; init; }

    /// <summary>
    /// The entry location when entering the vehicle through this hatch.
    /// Currently the rotation is ignored.
    /// </summary>
    public Transform EntryLocation { get; init; }

    /// <summary>
    /// The exit transform when exiting the vehicle through this hatch.
    /// Currently the rotation is ignored.
    /// </summary>
    public Transform ExitLocation { get; init; }

    /// <summary>
    /// The exit-transform to use instead of <see cref="ExitLocation"/> when the submarine
    /// is close to the water surface while exiting.
    /// </summary>
    public Transform SurfaceExitLocation { get; init; }

    /// <summary>
    /// Represents the definition of a vehicle hatch, including its transform locations for entry, exit, and surface exit.
    /// </summary>
    public VehicleHatchDefinition(GameObject hatch, Transform entry, Transform exit, Transform surfaceExit)
    {
        if (exit.IsNull())
            throw new ArgumentNullException(nameof(exit), "Exit location cannot be null for a vehicle hatch.");
        if (hatch.IsNull())
            throw new ArgumentNullException(nameof(hatch), "Vehicle hatch cannot be null.");
        if (entry.IsNull())
            throw new ArgumentNullException(nameof(entry), "Entry location cannot be null for a vehicle hatch.");
        if (surfaceExit.IsNull())
            throw new ArgumentNullException(nameof(surfaceExit),
                "Surface exit location cannot be null for a vehicle hatch.");
        Hatch = hatch;
        EntryLocation = entry;
        ExitLocation = exit;
        SurfaceExitLocation = surfaceExit;
    }

    internal bool CheckValidity(RootModController rmc, string thisName, bool verbose)
    {
        using var log = SmartLog.ForAVS(rmc);
        if (SurfaceExitLocation.IsNull())
            VehicleRegistrar.VerboseLog(log, VehicleRegistrar.LogType.Warn, verbose,
                thisName +
                "A null VehicleHatchStruct.SurfaceExitLocation was provided. You might need this if you exit weirdly near the surface.");
        if (Hatch.IsNull())
        {
            log.Error(thisName +
                      "A null VehicleHatchStruct.Hatch was provided. There would be no way to enter/exit this vehicle.");
            return false;
        }

        if (EntryLocation.IsNull())
        {
            log.Error(thisName +
                      "A null VehicleHatchStruct.EntryLocation was provided. There would be no way to enter/exit this vehicle.");
            return false;
        }

        if (ExitLocation.IsNull())
        {
            log.Error(thisName +
                      "A null VehicleHatchStruct.ExitLocation was provided. There would be no way to enter/exit this vehicle.");
            return false;
        }

        return true;
    }
}

/// <summary>
/// Definition of a fish tank within a vehicle.
/// The fish tank can accommodate fish and other aquatic life
/// that may be added to a water park.
/// Eggs will hatch eventually and fish may reproduce.
/// </summary>
public readonly struct MobileWaterPark
{
    /// <summary>
    /// The game object that will contain the fish.
    /// </summary>
    public Transform ContentContainer { get; }

    /// <summary>
    /// The park root object
    /// </summary>
    public GameObject Root { get; }

    /// <summary>
    /// The grid height of the total tank storage capacity.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// The grid width of the total tank storage capacity.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// The display text for the vehicle storage, which can be localized.
    /// </summary>
    public MaybeTranslate? DisplayName { get; }

    /// <summary>
    /// True if fish in the tank can reproduce.
    /// </summary>
    public bool AllowReproduction { get; }

    /// <summary>
    /// True if eggs in the tank can hatch.
    /// </summary>
    public bool HatchEggs { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MobileWaterPark"/> class with the specified container, display name,
    /// dimensions, and behavior settings.
    /// </summary>
    /// <param name="root">The <see cref="GameObject"/> that represents the entire water park. This parameter
    /// cannot be <see langword="null"/>.</param>
    /// <param name="contentContainer">The transform to contain the actual fish. Cannot be null</param>
    /// <param name="displayName">An optional display name for the fish tank, which may be translated.  If <see langword="null"/>, no display
    /// name is assigned.</param>
    /// <param name="height">The height of the fish tank, measured in arbitrary units.  Must be greater than zero. Defaults to 4.</param>
    /// <param name="width">The width of the fish tank, measured in arbitrary units.  Must be greater than zero. Defaults to 4.</param>
    /// <param name="allowReproduction">A value indicating whether fish in the tank are allowed to reproduce.  Defaults to <see langword="true"/>.</param>
    /// <param name="hatchEggs">A value indicating whether fish eggs in the tank are allowed to hatch.  Defaults to <see langword="true"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="root"/> or <paramref name="contentContainer"/> are <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="height"/> or <paramref name="width"/> is less than or equal to zero.</exception>
    public MobileWaterPark(
        GameObject root,
        Transform contentContainer,
        MaybeTranslate? displayName,
        int height = 4,
        int width = 4,
        bool allowReproduction = true,
        bool hatchEggs = true
    )
    {
        if (root.IsNull())
            throw new ArgumentNullException(nameof(root), "Mobile water park root cannot be null.");
        if (contentContainer.IsNull())
            throw new ArgumentNullException(nameof(contentContainer),
                "Mobile water park contentContainer cannot be null.");
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), "Fish tank height must be greater than zero.");
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Fish tank width must be greater than zero.");
        Root = root;
        ContentContainer = contentContainer;
        DisplayName = displayName;
        Height = height;
        Width = width;
        AllowReproduction = allowReproduction;
        HatchEggs = hatchEggs;
    }
}

/// <summary>
/// Represents a storage unit within a vehicle, defined by its container and grid dimensions.
/// </summary>
/// <remarks>The <see cref="VehicleStorage"/> struct encapsulates the storage characteristics of a
/// vehicle, including its physical container, dimensions, and display properties. It is immutable and ensures that
/// the storage dimensions are valid upon initialization.</remarks>
public readonly struct VehicleStorage
{
    /// <summary>
    /// The game object that represents the storage.
    /// </summary>
    public GameObject Container { get; }

    /// <summary>
    /// The grid height of the total storage capacity.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// The grid width of the total storage capacity.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// The display text for the vehicle storage, which can be localized.
    /// </summary>
    public MaybeTranslate? DisplayName { get; }

    /// <summary>
    /// Custom logic to determine if items can be added to this storage.
    /// Only applicable for innate storage containers.
    /// </summary>
    public IsAllowedToAdd? InnateIsAllowedToAdd { get; }

    /// <summary>
    /// Custom logic to determine if items can be removed from this storage.
    /// Only applicable for innate storage containers.
    /// </summary>
    public IsAllowedToRemove? InnateIsAllowedToRemove { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="VehicleStorage"/> class with the specified container, display
    /// name, and dimensions.
    /// </summary>
    /// <param name="container">The <see cref="GameObject"/> that represents the storage container. This parameter cannot be <see
    /// langword="null"/>.</param>
    /// <param name="displayName">The display name for the vehicle storage. This can be <see langword="null"/> if no display name is required.</param>
    /// <param name="height">The height of the vehicle storage. Must be greater than zero.</param>
    /// <param name="width">The width of the vehicle storage. Must be greater than zero.</param>
    /// <param name="innateIsAllowedToAdd">Custom logic to determine if items can be added to this innate storage. Not effective on modular storages. May be null</param>
    /// <param name="innateIsAllowedToRemove">Custom logic to determine if items can be removed from this innate storage. Not effective on modular storages.May be null</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="container"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="height"/> or <paramref name="width"/> is less than or equal to zero.</exception>
    public VehicleStorage(
        GameObject container,
        MaybeTranslate? displayName,
        int height = 4,
        int width = 4,
        IsAllowedToAdd? innateIsAllowedToAdd = null,
        IsAllowedToRemove? innateIsAllowedToRemove = null
    )
    {
        if (container.IsNull())
            throw new ArgumentNullException(nameof(container), "Vehicle storage container cannot be null.");
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height), "Vehicle storage height must be greater than zero.");
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Vehicle storage width must be greater than zero.");
        Container = container;
        DisplayName = displayName;
        Height = height;
        Width = width;
        InnateIsAllowedToAdd = innateIsAllowedToAdd;
        InnateIsAllowedToRemove = innateIsAllowedToRemove;
    }

    internal bool CheckValidity(RootModController rmc, string thisName)
    {
        using var log = SmartLog.ForAVS(rmc);
        if (Container.IsNull())
        {
            log.Error(thisName +
                      "A null VehicleStorage.Container was provided. There would be no way to access this storage.");
            return false;
        }

        if (Height < 0)
        {
            log.Error(thisName + "A negative VehicleStorage.Height was provided. This storage would have no space.");
            return false;
        }

        if (Width < 0)
        {
            log.Error(thisName + "A negative VehicleStorage.Width was provided. This storage would have no space.");
            return false;
        }

        return true;
    }
}

/// <summary>
/// Definition of vehicle upgrades, including the interface, flap, angles, and module proxies.
/// </summary>
public readonly struct VehicleUpgrades
{
    /// <summary>
    /// The object that serves as the hand target to open the upgrades interface.
    /// </summary>
    public GameObject Interface { get; }

    /// <summary>
    /// A flap geometry that automatically rotates to indicate the upgrades interface is open or closed.
    /// </summary>
    public GameObject Flap { get; }

    /// <summary>
    /// Euler angles of flap when opened
    /// </summary>
    public Vector3 AnglesOpened { get; }

    /// <summary>
    /// Euler angles of flap when closed
    /// </summary>
    public Vector3 AnglesClosed { get; }

    /// <summary>
    /// Parent transforms to put module models into.
    /// If null, no model will be provided for installed modules.
    /// </summary>
    public IReadOnlyList<Transform>? ModuleProxies { get; }

    /// <summary>
    /// Represents the configuration and attributes of vehicle upgrades, including interface objects, flap components,
    /// positional angles, and module proxies used in the upgrade system.
    /// </summary>
    public VehicleUpgrades(GameObject @interface, GameObject flap, Vector3 openAngles, Vector3 closedAngles,
        IReadOnlyList<Transform>? moduleProxies = null)
    {
        if (@interface.IsNull())
            throw new ArgumentNullException(nameof(@interface), "Vehicle upgrades interface cannot be null.");
        if (flap.IsNull())
            throw new ArgumentNullException(nameof(flap), "Vehicle upgrades flap cannot be null.");
        Interface = @interface;
        Flap = flap;
        AnglesOpened = openAngles;
        AnglesClosed = closedAngles;
        ModuleProxies = moduleProxies;
    }

    internal bool CheckValidity(RootModController rmc, string thisName, bool verbose)
    {
        using var log = SmartLog.ForAVS(rmc);

        if (!Interface)
        {
            log.Error(thisName +
                      "A null VehicleUpgrades.Interface was provided. There would be no way to upgrade this vehicle.");
            return false;
        }

        if (!Flap)
        {
            log.Error(thisName +
                      "A null VehicleUpgrades.Flap was provided. The upgrades interface requires this. It will be rotated by the angles in this struct when activated. You can set the rotation angle to zero to take no action.");
            return false;
        }

        if (ModuleProxies is null)
            VehicleRegistrar.VerboseLog(log, VehicleRegistrar.LogType.Log, verbose,
                thisName +
                " A null VehicleUpgrades.ModuleProxies was provided. AVS will not provide a model for this upgrade slot.");
        return true;
    }
}

/// <summary>
/// Represents a vehicle power cell definition, including the battery slot, proxy, and display name.
/// </summary>
public readonly struct VehicleBatteryDefinition
{
    /// <summary>
    /// Primary access point for the battery slot.
    /// </summary>
    public GameObject Root { get; }

    /// <summary>
    /// Parent to place the respective power cell model in.
    /// Any other contained children are purged on prefabrication.
    /// If null, the power cell model will not be placed.
    /// </summary>
    public Transform BatteryProxy { get; }

    /// <summary>
    /// Name to display for the battery slot, which can be localized.
    /// </summary>
    public MaybeTranslate? DisplayName { get; }

    /// <summary>
    /// Constructs a new instance of <see cref="VehicleBatteryDefinition"/>.
    /// </summary>
    /// <param name="root">The game object representing the battery slot. Cannot be null.</param>
    /// <param name="batteryProxy">The transform where the battery model will be placed. Can be null if no model is needed.</param>
    /// <param name="displayName">The display name for the battery slot, which can be localized. Can be null if no display name is needed.</param>
    public VehicleBatteryDefinition(GameObject root, Transform batteryProxy, MaybeTranslate? displayName = null)
    {
        if (root.IsNull())
            throw new ArgumentNullException(nameof(root), "Vehicle battery slot cannot be null.");
        Root = root;
        BatteryProxy = batteryProxy;
        DisplayName = displayName;
    }

    internal bool CheckValidity(RootModController rmc, string thisName, bool verbose)
    {
        using var log = SmartLog.ForAVS(rmc);
        if (!Root)
        {
            log.Error(thisName +
                      "A null VehicleBattery.BatterySlot was provided. There would be no way to access this battery.");
            return false;
        }

        if (!BatteryProxy)
            VehicleRegistrar.VerboseLog(log, VehicleRegistrar.LogType.Log, verbose,
                thisName +
                "A null VehicleBattery.BatteryProxy was provided. AVS will not provide a model for this battery slot.");
        return true;
    }
}

/// <summary>
/// Head- or floodlight definition for vehicles.
/// Defined spot lights will be maintained by AVS, toggled on- and off automatically,
/// and configured to standard.
/// The source objects may or may not already contain a light component.
/// All AVS-created light sources will be configured to use the Subnautica volumetric light system,
/// hard shadows, and a spot light type.
/// </summary>
public readonly struct VehicleSpotLightDefinition
{
    /// <summary>
    /// The game object that represents the light source. The light shines along the Z axis of this object.
    /// If this object does not have a <see cref="UnityEngine.Light"/> component, AVS will add one.
    /// </summary>
    public GameObject Light { get; }

    /// <summary>
    /// The intensity of the light, which must be non-negative.
    /// </summary>
    public float Intensity { get; }

    /// <summary>
    /// The range of the light in meters, which must be greater than zero.
    /// </summary>
    public float Range { get; }

    /// <summary>
    /// The color of the light, which can be any valid Unity <see cref="Color"/>.
    /// </summary>
    public Color Color { get; }

    /// <summary>
    /// The angle of the light cone in degrees, which must be between 0 and 179.
    /// Note that the volumetric Subnautica light cone visualization seems to be fixed at around 55 degrees.
    /// While <see cref="Angle" /> will impact the light source's <see cref="Light.spotAngle" /> maintained by AVS,
    /// the volumetric light cone is harded-coded in Subnautica and will not change.
    /// </summary>
    public float Angle { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="VehicleSpotLightDefinition"/> class with the specified light object,
    /// intensity, range, color, and angle.
    /// </summary>
    /// <param name="light">The <see cref="GameObject"/> representing the spotlight. Cannot be <see langword="null"/>.</param>
    /// <param name="intensity">The brightness of the spotlight. Must be non-negative.</param>
    /// <param name="range">The effective range of the spotlight, in meters. Must be greater than zero.</param>
    /// <param name="color">The color of the spotlight.</param>
    /// <param name="angle">The beam angle of the spotlight, in degrees. Must be between 0 and 179 inclusive.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="light"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="intensity"/> is negative, <paramref name="range"/> is less than or equal to zero,
    /// or <paramref name="angle"/> is outside the range 0 to 179 degrees.</exception>
    public VehicleSpotLightDefinition(GameObject light, float intensity, float range, Color color, float angle)
    {
        if (light.IsNull())
            throw new ArgumentNullException(nameof(light), "Vehicle flood light cannot be null.");
        if (intensity < 0)
            throw new ArgumentOutOfRangeException(nameof(intensity),
                "Vehicle flood light intensity must be non-negative.");
        if (range <= 0)
            throw new ArgumentOutOfRangeException(nameof(range),
                "Vehicle flood light range must be greater than zero.");
        if (angle < 0 || angle > 179)
            throw new ArgumentOutOfRangeException(nameof(angle),
                "Vehicle flood light angle must be between 0 and 360 degrees.");
        Light = light;
        Intensity = intensity;
        Range = range;
        Color = color;
        Angle = angle;
    }

    internal bool CheckValidity(RootModController rmc, string thisName)
    {
        using var log = SmartLog.ForAVS(rmc);
        if (Light.IsNull())
        {
            log.Error(thisName +
                      "A null VehicleFloodLight.Light was provided. There would be nothing from which to emit light.");
            return false;
        }

        if (Intensity < 0)
        {
            log.Error(thisName +
                      "A negative VehicleFloodLight.Intensity was provided. The light would be totally dark.");
            return false;
        }

        if (Range < 0)
        {
            log.Error(thisName +
                      "A negative VehicleFloodLight.Range was provided. The light would be totally dark.");
            return false;
        }

        return true;
    }
}