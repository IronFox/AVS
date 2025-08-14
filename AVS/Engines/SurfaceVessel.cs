using UnityEngine;

namespace AVS.Engines
{
    /// <summary>
    /// Engine of a surface vessel, such as a boat or a ship.
    /// </summary>
    public class SurfaceVessel : AbstractEngine
    {
        /// <summary>
        /// The current water line to rise or fall to.
        /// </summary>
        public virtual float WaterLine => 0f;
        /// <summary>
        /// Gets the buoyancy factor of the object that is applied per second.
        /// </summary>
        public virtual float Buoyancy => 5f;


        /// <summary>
        /// Gets the fore-aft stability factor of the vessel, applied per second.
        /// Higher values result in faster stabilization of pitch.
        /// </summary>
        public virtual float ForeAftStability => 10f;
        /// <summary>
        /// Gets the port-starboard stability factor of the vessel, applied per second.
        /// Higher values result in faster stabilization of roll.
        /// </summary>
        public virtual float PortStarboardStability => 10f;

        /// <inheritdoc/>
        public override bool CanMoveAboveWater => true;
        /// <inheritdoc/>
        public override bool CanRotateAboveWater => true;

        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();
            GetComponent<WorldForces>().handleGravity = false;
        }
        /// <inheritdoc/>
        public override void ControlRotation()
        {
            float yawFactor = 1.4f;
            Vector2 mouseDir = GameInput.GetLookDelta();
            float xRot = mouseDir.x;
            RB.AddTorque(MV.transform.up * xRot * yawFactor * Time.deltaTime, ForceMode.VelocityChange);
            // don't accept pitch inputs!
        }
        /// <inheritdoc/>
        protected override void MoveWithInput(Vector3 moveDirection)
        {
            UpdateRightMomentum(moveDirection.x);
            UpdateForwardMomentum(moveDirection.z);
            return;
        }
        /// <inheritdoc/>
        protected override void DoFixedUpdate()
        {
            if (IsTrackingSurface())
            {
                Vector3 targetPosition = new Vector3(transform.position.x, WaterLine, transform.position.z);
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.fixedDeltaTime * Buoyancy);

                Quaternion targetForeAftRotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetForeAftRotation, Time.fixedDeltaTime * ForeAftStability);

                Quaternion targetPortStarboardRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, 0);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetPortStarboardRotation, Time.fixedDeltaTime * PortStarboardStability);
            }
        }
        /// <summary>
        /// Determines whether the vessel is tracking the surface of the water.
        /// </summary>
        /// <returns>True if the vessel is tracking the water surface; otherwise, false.</returns>
        public virtual bool IsTrackingSurface()
        {
            return true;
        }
    }
}
