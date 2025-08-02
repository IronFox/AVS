using AVS.Localization;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AVS.VehicleParts
{
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
            if (root == null)
                throw new ArgumentNullException(nameof(root), "Vehicle pilot seat root cannot be null.");
            if (playerControlLocation == null)
                throw new ArgumentNullException(nameof(playerControlLocation), "Vehicle pilot sit location cannot be null.");
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
            ExitLocation != null
                ? ExitLocation.position
                : CalculatedExitLocation;

        internal bool CheckValidity(string thisName, bool verbose)
        {
            if (ExitLocation == null)
            {
                VehicleRegistrar.VerboseLog(VehicleRegistrar.LogType.Warn, verbose, thisName + "A null PilotSeat.ExitLocation was provided. You might need this if you exit from piloting into a weird place.");
            }
            if (LeftHandTarget == null)
            {
                VehicleRegistrar.VerboseLog(VehicleRegistrar.LogType.Log, verbose, thisName + "A null ModVehicle.SteeringWheelLeftHandTarget was provided. This is what the player's left hand will 'grab' while you pilot.");
            }
            if (RightHandTarget == null)
            {
                VehicleRegistrar.VerboseLog(VehicleRegistrar.LogType.Log, verbose, thisName + "A null ModVehicle.SteeringWheelRightHandTarget was provided.  This is what the player's right hand will 'grab' while you pilot.");
            }


            if (Root == null)
            {
                Logger.Error(thisName + "A null PilotSeat.Seat was provided. There would be no way to pilot this vehicle.");
                return false;
            }
            if (PlayerControlLocation == null)
            {
                Logger.Error(thisName + "A null PilotSeat.SitLocation was provided. There would be no way to pilot this vehicle.");
                return false;
            }
            return true;
        }
    }
    public readonly struct VehicleHatchDefinition
    {
        public GameObject Hatch { get; }
        public Transform EntryLocation { get; }
        /// <summary>
        /// The exit transform when exiting the vehicle through this hatch.
        /// Currently, the rotation is ignored.
        /// </summary>
        public Transform ExitLocation { get; }
        /// <summary>
        /// The exit transform to use instead of <see cref="ExitLocation"/> when the submarine
        /// is close to the water surface while exiting.
        /// </summary>
        public Transform SurfaceExitLocation { get; }
        public VehicleHatchDefinition(GameObject hatch, Transform entry, Transform exit, Transform surfaceExit)
        {
            if (exit == null)
                throw new ArgumentNullException(nameof(exit), "Exit location cannot be null for a vehicle hatch.");
            if (hatch == null)
                throw new ArgumentNullException(nameof(hatch), "Vehicle hatch cannot be null.");
            if (entry == null)
                throw new ArgumentNullException(nameof(entry), "Entry location cannot be null for a vehicle hatch.");
            if (surfaceExit == null)
                throw new ArgumentNullException(nameof(surfaceExit), "Surface exit location cannot be null for a vehicle hatch.");
            Hatch = hatch;
            EntryLocation = entry;
            ExitLocation = exit;
            SurfaceExitLocation = surfaceExit;
        }

        internal bool CheckValidity(string thisName, bool verbose)
        {
            if (SurfaceExitLocation == null)
            {
                VehicleRegistrar.VerboseLog(VehicleRegistrar.LogType.Warn, verbose, thisName + "A null VehicleHatchStruct.SurfaceExitLocation was provided. You might need this if you exit weirdly near the surface.");
            }
            if (Hatch == null)
            {
                Logger.Error(thisName + "A null VehicleHatchStruct.Hatch was provided. There would be no way to enter/exit this vehicle.");
                return false;
            }
            if (EntryLocation == null)
            {
                Logger.Error(thisName + "A null VehicleHatchStruct.EntryLocation was provided. There would be no way to enter/exit this vehicle.");
                return false;
            }
            if (ExitLocation == null)
            {
                Logger.Error(thisName + "A null VehicleHatchStruct.ExitLocation was provided. There would be no way to enter/exit this vehicle.");
                return false;
            }
            return true;
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
        /// Initializes a new instance of the <see cref="VehicleStorage"/> class with the specified container, display
        /// name, and dimensions.
        /// </summary>
        /// <param name="container">The <see cref="GameObject"/> that represents the storage container. This parameter cannot be <see
        /// langword="null"/>.</param>
        /// <param name="displayName">The display name for the vehicle storage. This can be <see langword="null"/> if no display name is required.</param>
        /// <param name="height">The height of the vehicle storage. Must be greater than zero.</param>
        /// <param name="width">The width of the vehicle storage. Must be greater than zero.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="container"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="height"/> or <paramref name="width"/> is less than or equal to zero.</exception>
        public VehicleStorage(GameObject container, MaybeTranslate? displayName, int height = 4, int width = 4)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container), "Vehicle storage container cannot be null.");
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), "Vehicle storage height must be greater than zero.");
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Vehicle storage width must be greater than zero.");
            Container = container;
            DisplayName = displayName;
            Height = height;
            Width = width;
        }
        internal bool CheckValidity(string thisName)
        {
            if (Container == null)
            {
                Logger.Error(thisName + "A null VehicleStorage.Container was provided. There would be no way to access this storage.");
                return false;
            }
            if (Height < 0)
            {
                Logger.Error(thisName + "A negative VehicleStorage.Height was provided. This storage would have no space.");
                return false;
            }
            if (Width < 0)
            {
                Logger.Error(thisName + "A negative VehicleStorage.Width was provided. This storage would have no space.");
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
        public VehicleUpgrades(GameObject @interface, GameObject flap, Vector3 openAngles, Vector3 closedAngles, IReadOnlyList<Transform>? moduleProxies = null)
        {
            if (@interface == null)
                throw new ArgumentNullException(nameof(@interface), "Vehicle upgrades interface cannot be null.");
            if (flap == null)
                throw new ArgumentNullException(nameof(flap), "Vehicle upgrades flap cannot be null.");
            Interface = @interface;
            Flap = flap;
            AnglesOpened = openAngles;
            AnglesClosed = closedAngles;
            ModuleProxies = moduleProxies;
        }

        internal bool CheckValidity(string thisName, bool verbose)
        {
            if (!Interface)
            {
                Logger.Error(thisName + "A null VehicleUpgrades.Interface was provided. There would be no way to upgrade this vehicle.");
                return false;
            }
            if (!Flap)
            {
                Logger.Error(thisName + "A null VehicleUpgrades.Flap was provided. The upgrades interface requires this. It will be rotated by the angles in this struct when activated. You can set the rotation angle to zero to take no action.");
                return false;
            }
            if (ModuleProxies is null)
            {
                VehicleRegistrar.VerboseLog(VehicleRegistrar.LogType.Log, verbose, thisName + " A null VehicleUpgrades.ModuleProxies was provided. AVS will not provide a model for this upgrade slot.");
            }
            return true;
        }
    }

    /// <summary>
    /// Represents a vehicle power cell definition, including the battery slot, proxy, and display name.
    /// </summary>
    public readonly struct VehiclePowerCellDefinition
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
        /// Constructs a new instance of <see cref="VehiclePowerCellDefinition"/>.
        /// </summary>
        /// <param name="root">The game object representing the battery slot. Cannot be null.</param>
        /// <param name="batteryProxy">The transform where the battery model will be placed. Can be null if no model is needed.</param>
        /// <param name="displayName">The display name for the battery slot, which can be localized. Can be null if no display name is needed.</param>
        public VehiclePowerCellDefinition(GameObject root, Transform batteryProxy, MaybeTranslate? displayName = null)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root), "Vehicle battery slot cannot be null.");
            Root = root;
            BatteryProxy = batteryProxy;
            DisplayName = displayName;
        }

        internal bool CheckValidity(string thisName, bool verbose)
        {
            if (!Root)
            {
                Logger.Error(thisName + "A null VehicleBattery.BatterySlot was provided. There would be no way to access this battery.");
                return false;
            }
            if (!BatteryProxy)
            {
                VehicleRegistrar.VerboseLog(VehicleRegistrar.LogType.Log, verbose, thisName + "A null VehicleBattery.BatteryProxy was provided. AVS will not provide a model for this battery slot.");
            }
            return true;
        }
    }
    public readonly struct VehicleFloodLight
    {
        public GameObject Light { get; }
        public float Intensity { get; }
        public float Range { get; }
        public Color Color { get; }
        public float Angle { get; }
        public VehicleFloodLight(GameObject light, float intensity, float range, Color color, float angle)
        {
            if (light == null)
                throw new ArgumentNullException(nameof(light), "Vehicle flood light cannot be null.");
            if (intensity < 0)
                throw new ArgumentOutOfRangeException(nameof(intensity), "Vehicle flood light intensity must be non-negative.");
            if (range <= 0)
                throw new ArgumentOutOfRangeException(nameof(range), "Vehicle flood light range must be greater than zero.");
            if (angle < 0 || angle > 360)
                throw new ArgumentOutOfRangeException(nameof(angle), "Vehicle flood light angle must be between 0 and 360 degrees.");
            Light = light;
            Intensity = intensity;
            Range = range;
            Color = color;
            Angle = angle;
        }

        internal bool CheckValidity(string thisName)
        {
            if (Light == null)
            {
                Logger.Error(thisName + "A null VehicleFloodLight.Light was provided. There would be nothing from which to emit light.");
                return false;
            }
            if (Intensity < 0)
            {
                Logger.Error(thisName + "A negative VehicleFloodLight.Intensity was provided. The light would be totally dark.");
                return false;
            }
            if (Range < 0)
            {
                Logger.Error(thisName + "A negative VehicleFloodLight.Range was provided. The light would be totally dark.");
                return false;
            }
            return true;
        }
    }

}
