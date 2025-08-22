using AVS.Util;
using UnityEngine;

namespace AVS.VehicleComponents;

// This component can be added to a steering wheel
// in order to make it animate in a way that corresponds
// with the movements of the vehicle.
/// <summary>
/// Represents a component that allows a steering wheel to animate in correspondence
/// with the movements of a vehicle by using angular velocity data.
/// </summary>
public class SteeringWheel : MonoBehaviour
{
    /// <summary>
    /// Gets the Rigidbody associated with the parent Vehicle component, if available.
    /// </summary>
    /// <remarks>
    /// This property retrieves the Rigidbody from the parent Vehicle component, allowing
    /// the steering wheel to access the angular velocity data of the vehicle for animation.
    /// Returns null if no parent Vehicle component with a Rigidbody is present.
    /// </remarks>
    public Rigidbody? UseRigidbody
    {
        get
        {
            var vh = GetComponentInParent<Vehicle>();
            if (vh.IsNotNull())
                return vh.useRigidbody;
            return null;
        }
    }

    // Store the current Z rotation and the velocity used by SmoothDamp
    private float initialYawRotation = 0f;
    private float currentYawRotation = 0f;
    private float rotationVelocity = 0f;

    /// <summary>
    /// Defines the amount of time, in seconds, for the steering wheel's rotation to smooth between its current and target values.
    /// </summary>
    /// <remarks>
    /// Smaller values create faster and sharper adjustments, while larger values create smoother but slower transitions.
    /// This value is used in conjunction with Mathf.SmoothDamp to ensure the steering wheel's rotation responds
    /// fluidly to changes based on the vehicle's angular velocity.
    /// </remarks>
    public float smoothTime = 0.1f;

    /// <summary>
    /// Defines the axis along which the yaw rotation of the steering wheel is applied.
    /// </summary>
    /// <remarks>
    /// This field determines the local axis around which the steering wheel's rotation
    /// is animated to correspond to the movement and angular velocity of the vehicle.
    /// The available options include the x, y, and z axes, as well as their negative counterparts.
    /// </remarks>
    public YawAxis yawAxis = YawAxis.Z;

    /// <summary>
    /// Represents the maximum angular velocity (in radians per second) that can be expected
    /// from the Rigidbody associated with the vehicle. This value is used as a reference
    /// to normalize the angular velocity when calculating steering wheel rotation.
    /// </summary>
    /// <remarks>
    /// Adjusting this value may be necessary to ensure the steering wheel animation behaves
    /// as expected, particularly when dealing with vehicles that operate at significantly
    /// higher or lower angular velocities than the default value.
    /// </remarks>
    public float maxExpectedAngularVelocity = 7f;

    /// <summary>
    /// Sets the maximum angle, in degrees, to which the steering wheel can rotate.
    /// </summary>
    /// <remarks>
    /// This value defines the limits of the steering wheel's rotation to ensure
    /// it corresponds with the vehicle's steering. Adjusting this value changes
    /// how far the steering wheel will turn in response to the vehicle's angular velocity.
    /// </remarks>
    public float maxSteeringWheelAngle = 45f;

    /// <summary>
    /// Defines the possible axes used for determining the rotation direction
    /// of a steering wheel or similar components in relation to angular motion.
    /// </summary>
    public enum YawAxis
    {
        /// <summary>
        /// Represents the positive X-axis as the designated yaw rotation axis.
        /// This value is used to determine the rotation direction for components
        /// such as a steering wheel relative to angular motion.
        /// </summary>
        X,

        /// <summary>
        /// Represents the negative X-axis as the designated yaw rotation axis.
        /// This value is used to determine the opposite rotation direction for components
        /// such as a steering wheel relative to angular motion.
        /// </summary>
        MinusX,

        /// <summary>
        /// Represents the positive Y-axis as the designated yaw rotation axis.
        /// This value is used to determine the rotation direction for components
        /// such as a steering wheel relative to angular motion.
        /// </summary>
        Y,

        /// <summary>
        /// Represents the negative Y-axis as the designated yaw rotation axis.
        /// This value is used to define the rotation direction for components
        /// such as a steering wheel relative to angular motion.
        /// </summary>
        MinusY,

        /// <summary>
        /// Represents the positive Z-axis as the designated yaw rotation axis.
        /// This value is utilized to define angular motion around the Z-axis,
        /// often applied to components like a steering wheel for rotation animations.
        /// </summary>
        Z,

        /// <summary>
        /// Represents the negative Z-axis as the designated yaw rotation axis.
        /// This value is used to determine the rotation direction for components
        /// such as a steering wheel relative to angular motion.
        /// </summary>
        MinusZ
    }

    /// <inheritdoc/>
    public void Start()
    {
        switch (yawAxis)
        {
            case YawAxis.X:
                initialYawRotation = transform.localEulerAngles.x;
                break;
            case YawAxis.Y:
                initialYawRotation = transform.localEulerAngles.y;
                break;
            case YawAxis.Z:
                initialYawRotation = transform.localEulerAngles.z;
                break;
            case YawAxis.MinusX:
                initialYawRotation = transform.localEulerAngles.x;
                break;
            case YawAxis.MinusY:
                initialYawRotation = transform.localEulerAngles.y;
                break;
            case YawAxis.MinusZ:
                initialYawRotation = transform.localEulerAngles.z;
                break;
        }
    }

    /// <inheritdoc/>
    public void Update()
    {
        if (UseRigidbody.IsNull())
            return;
        var percentAng = UseRigidbody.angularVelocity.y / maxExpectedAngularVelocity;
        var targetYawRotation = percentAng * maxSteeringWheelAngle;

        // Smoothly update the Z rotation using SmoothDamp
        currentYawRotation = Mathf.SmoothDamp(currentYawRotation, -targetYawRotation, ref rotationVelocity, smoothTime);

        // Apply the smoothed rotation to the transform
        switch (yawAxis)
        {
            case YawAxis.X:
                transform.localEulerAngles = new Vector3(initialYawRotation + currentYawRotation,
                    transform.localEulerAngles.y, transform.localEulerAngles.z);
                break;
            case YawAxis.Y:
                transform.localEulerAngles = new Vector3(transform.localEulerAngles.x,
                    initialYawRotation + currentYawRotation, transform.localEulerAngles.z);
                break;
            case YawAxis.Z:
                transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y,
                    initialYawRotation + currentYawRotation);
                break;
            case YawAxis.MinusX:
                transform.localEulerAngles = new Vector3(initialYawRotation - currentYawRotation,
                    transform.localEulerAngles.y, transform.localEulerAngles.z);
                break;
            case YawAxis.MinusY:
                transform.localEulerAngles = new Vector3(transform.localEulerAngles.x,
                    initialYawRotation - currentYawRotation, transform.localEulerAngles.z);
                break;
            case YawAxis.MinusZ:
                transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y,
                    initialYawRotation - currentYawRotation);
                break;
        }
    }
}