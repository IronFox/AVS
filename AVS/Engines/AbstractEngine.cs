using AVS.BaseVehicle;
using AVS.Util;
using UnityEngine;

namespace AVS.Engines
{
    /// <summary>
    /// Base class for vehicle engines in the mod framework.
    /// Handles movement, physics, and input for mod vehicles.
    /// </summary>
    public abstract class AbstractEngine : MonoBehaviour, IScuttleListener
    {
        private AvsVehicle? mv = null;
        private Rigidbody? rb = null;
        //private EngineSounds _sounds = default;

        /// <summary>
        /// Center of mass for the vehicle, applied during Start().
        /// </summary>
        protected Vector3 CenterOfMass { get; set; } = Vector3.zero;

        /// <summary>
        /// Angular drag for the vehicle, applied during Start().
        /// </summary>
        protected float AngularDrag { get; set; } = 5f;

        /// <summary>
        /// Gets the ModVehicle component associated with this engine.
        /// </summary>
        public AvsVehicle MV
        {
            get
            {
                if (mv != null)
                    return mv;
                mv = GetComponentInParent<AvsVehicle>();
                if (mv == null)
                    throw new System.Exception($"ModVehicle component not found on {gameObject.name}. Please ensure this engine is attached to a ModVehicle or its parent.");
                return mv;
            }
        }

        /// <summary>
        /// Gets the Rigidbody component associated with this engine.
        /// </summary>
        protected Rigidbody RB
        {
            get
            {
                if (rb != null)
                    return rb;
                rb = MV.useRigidbody.OrRequired(() => MV.GetComponent<Rigidbody>());
                return rb;
            }
        }

        #region public_fields

        /// <summary>
        /// Gets or sets a value indicating whether the vehicle can move above water.
        /// </summary>
        public virtual bool CanMoveAboveWater { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the vehicle can rotate above water.
        /// </summary>
        public virtual bool CanRotateAboveWater { get; set; } = false;
        #endregion

        #region protected_fields
        protected virtual float FORWARD_TOP_SPEED => 1000;
        protected virtual float REVERSE_TOP_SPEED => 1000;
        protected virtual float STRAFE_MAX_SPEED => 1000;
        protected virtual float VERT_MAX_SPEED => 1000;
        protected virtual float FORWARD_ACCEL => FORWARD_TOP_SPEED / 10f;
        protected virtual float REVERSE_ACCEL => REVERSE_TOP_SPEED / 10f;
        protected virtual float STRAFE_ACCEL => STRAFE_MAX_SPEED / 10f;
        protected virtual float VERT_ACCEL => VERT_MAX_SPEED / 10f;

        protected virtual float waterDragDecay => 4.5f;
        protected virtual float airDragDecay => 1.5f;

        /// <summary>
        /// Gets the drag decay value depending on whether the vehicle is underwater.
        /// </summary>
        protected virtual float DragDecay
        {
            get
            {
                if (MV.GetIsUnderwater())
                {
                    return waterDragDecay;
                }
                else
                {
                    return airDragDecay;
                }
            }
        }

        private float _forwardMomentum = 0;

        /// <summary>
        /// Gets or sets the forward momentum of the vehicle.
        /// </summary>
        protected virtual float ForwardMomentum
        {
            get
            {
                return _forwardMomentum;
            }
            set
            {
                if (value < -REVERSE_TOP_SPEED)
                {
                    _forwardMomentum = -REVERSE_TOP_SPEED;
                }
                else if (FORWARD_TOP_SPEED < value)
                {
                    _forwardMomentum = FORWARD_TOP_SPEED;
                }
                else
                {
                    _forwardMomentum = value;
                }
            }
        }

        /// <summary>
        /// Updates the forward momentum based on input magnitude.
        /// </summary>
        /// <param name="inputMagnitude">Input value for forward movement.</param>
        protected virtual void UpdateForwardMomentum(float inputMagnitude)
        {
            if (0 < inputMagnitude)
            {
                ForwardMomentum = ForwardMomentum + inputMagnitude * FORWARD_ACCEL * Time.fixedDeltaTime;
            }
            else
            {
                ForwardMomentum = ForwardMomentum + inputMagnitude * REVERSE_ACCEL * Time.fixedDeltaTime;
            }
        }

        protected float _rightMomentum = 0;

        /// <summary>
        /// Gets or sets the right (strafe) momentum of the vehicle.
        /// </summary>
        protected virtual float RightMomentum
        {
            get
            {
                return _rightMomentum;
            }
            set
            {
                if (value < -STRAFE_MAX_SPEED)
                {
                    _rightMomentum = -STRAFE_MAX_SPEED;
                }
                else if (STRAFE_MAX_SPEED < value)
                {
                    _rightMomentum = STRAFE_MAX_SPEED;
                }
                else
                {
                    _rightMomentum = value;
                }
            }
        }

        /// <summary>
        /// Updates the right (strafe) momentum based on input magnitude.
        /// </summary>
        /// <param name="inputMagnitude">Input value for right movement.</param>
        protected virtual void UpdateRightMomentum(float inputMagnitude)
        {
            if (inputMagnitude != 0)
            {
                RightMomentum += inputMagnitude * STRAFE_ACCEL * Time.fixedDeltaTime;
            }
        }

        private float _upMomentum = 0;

        /// <summary>
        /// Gets or sets the upward momentum of the vehicle.
        /// </summary>
        protected virtual float UpMomentum
        {
            get
            {
                return _upMomentum;
            }
            set
            {
                if (value < -VERT_MAX_SPEED)
                {
                    _upMomentum = -VERT_MAX_SPEED;
                }
                else if (VERT_MAX_SPEED < value)
                {
                    _upMomentum = VERT_MAX_SPEED;
                }
                else
                {
                    _upMomentum = value;
                }
            }
        }

        /// <summary>
        /// Updates the upward momentum based on input magnitude.
        /// </summary>
        /// <param name="inputMagnitude">Input value for upward movement.</param>
        protected virtual void UpdateUpMomentum(float inputMagnitude)
        {
            UpMomentum += inputMagnitude * VERT_ACCEL * Time.fixedDeltaTime;
        }


        #endregion

        #region unity_signals
        /// <summary>
        /// Unity Awake callback. Initializes references and registers the engine.
        /// </summary>
        public virtual void Awake()
        {
            mv = GetComponent<AvsVehicle>();
            rb = GetComponent<Rigidbody>();
            // register self with mainpatcher, for on-the-fly voice selection updating
            //DynamicClipLoader.engines.Add(this);
        }

        /// <summary>
        /// Unity Start callback. Applies center of mass and angular drag.
        /// </summary>
        public virtual void Start()
        {
            Logger.Log($"Starting engine on {RB.NiceName()}");
            RB.centerOfMass = CenterOfMass;
            RB.angularDrag = AngularDrag;
        }

        /// <summary>
        /// Unity OnDisable callback. Stops engine sounds if needed.
        /// </summary>
        public void OnDisable()
        {
            //EngineSource1?.Stop();
            //EngineSource2?.Stop();
        }

        /// <summary>
        /// Unity FixedUpdate callback. Handles movement and physics updates.
        /// </summary>
        public virtual void FixedUpdate()
        {
            if (CanMove())
            {
                if (CanTakeInputs())
                {
                    DoMovementInputs();
                }
                DoMovement();
            }
            DoFixedUpdate();
        }
        #endregion

        #region overridden_methods
        /// <summary>
        /// Determines if the vehicle can move.
        /// </summary>
        /// <returns>True if movement is allowed.</returns>
        protected virtual bool CanMove()
        {
            return MV.GetIsUnderwater() || CanMoveAboveWater;
        }

        /// <summary>
        /// Determines if the vehicle can rotate.
        /// </summary>
        /// <returns>True if rotation is allowed.</returns>
        protected virtual bool CanRotate()
        {
            return MV.GetIsUnderwater() || CanRotateAboveWater;
        }

        /// <summary>
        /// Performs the movement logic for the vehicle.
        /// </summary>
        protected virtual void DoMovement()
        {
            ExecutePhysicsMove();
        }

        /// <summary>
        /// Performs additional fixed update logic, such as drag application.
        /// </summary>
        protected virtual void DoFixedUpdate()
        {
            Vector3 moveDirection = GameInput.GetMoveDirection();
            //DoEngineSounds(moveDirection);
            ApplyDrag(moveDirection);
        }

        /// <summary>
        /// Applies movement input to the vehicle.
        /// </summary>
        /// <param name="moveInput">Movement input vector.</param>
        protected virtual void MoveWithInput(Vector3 moveInput)
        {
            UpdateRightMomentum(moveInput.x);
            UpdateUpMomentum(moveInput.y);
            UpdateForwardMomentum(moveInput.z);
            return;
        }

        /// <summary>
        /// Applies player controls to the vehicle, including acceleration modifiers.
        /// </summary>
        /// <param name="moveDirection">Movement direction vector.</param>
        public void ApplyPlayerControls(Vector3 moveDirection)
        {
            var modifiers = GetComponentsInChildren<VehicleAccelerationModifier>();
            foreach (var modifier in modifiers)
            {
                modifier.ModifyAcceleration(ref moveDirection);
            }
            MoveWithInput(moveDirection);
            return;
        }

        /// <summary>
        /// Handles movement input and power drain if the player is controlling the vehicle.
        /// </summary>
        protected virtual void DoMovementInputs()
        {
            Vector3 moveDirection = GameInput.GetMoveDirection();
            if (!Player.main.GetPDA().isOpen)
            {
                ApplyPlayerControls(moveDirection);
                DrainPower(moveDirection);
            }
        }

        /// <summary>
        /// Drains power from the vehicle based on movement input.
        /// </summary>
        /// <param name="moveDirection">Movement direction vector.</param>
        public virtual void DrainPower(Vector3 moveDirection)
        {
            float scalarFactor = 1.0f;
            float basePowerConsumptionPerSecond = moveDirection.x + moveDirection.y + moveDirection.z;
            float upgradeModifier = Mathf.Pow(0.85f, MV.NumEfficiencyModules);
            MV.PowerManager.TrySpendEnergy(scalarFactor * basePowerConsumptionPerSecond * upgradeModifier * Time.fixedDeltaTime);
        }

        /// <summary>
        /// Kills all movement momentum for the vehicle.
        /// </summary>
        public virtual void KillMomentum()
        {
            ForwardMomentum = 0f;
            RightMomentum = 0f;
            UpMomentum = 0f;
        }

        /// <summary>
        /// Controls the rotation of the vehicle based on player input.
        /// </summary>
        public virtual void ControlRotation()
        {
            if (CanRotate())
            {
                float pitchFactor = 1.4f;
                float yawFactor = 1.4f;
                Vector2 mouseDir = GameInput.GetLookDelta();
                float xRot = mouseDir.x;
                float yRot = mouseDir.y;
                RB.AddTorque(MV.transform.up * xRot * yawFactor * Time.deltaTime, ForceMode.VelocityChange);
                RB.AddTorque(MV.transform.right * yRot * -pitchFactor * Time.deltaTime, ForceMode.VelocityChange);
            }
        }
        #endregion

        #region virtual_methods
        /// <summary>
        /// Gets or sets the drag threshold speed below which momentum is killed.
        /// </summary>
        protected virtual float DragThresholdSpeed
        {
            get
            {
                return 0;
            }
            set
            {

            }
        }

        /// <summary>
        /// Applies drag to the vehicle's momentum based on movement input.
        /// </summary>
        /// <param name="move">Movement input vector.</param>
        protected virtual void ApplyDrag(Vector3 move)
        {
            bool isForward = move.z != 0;
            bool isRight = move.x != 0;
            bool isUp = move.y != 0;
            bool activated = isForward || isRight || isUp || MV.worldForces.IsAboveWater();

            if (!isForward)
            {
                if (0 < Mathf.Abs(ForwardMomentum))
                {
                    ForwardMomentum -= DragDecay * ForwardMomentum * Time.deltaTime;
                }
            }
            if (!isRight)
            {
                if (0 < Mathf.Abs(RightMomentum))
                {
                    RightMomentum -= DragDecay * RightMomentum * Time.deltaTime;
                }
            }
            if (!isUp)
            {
                if (0 < Mathf.Abs(UpMomentum))
                {
                    UpMomentum -= DragDecay * UpMomentum * Time.deltaTime;
                }
            }
            if (!activated && RB.velocity.magnitude < DragThresholdSpeed)
            {
                ForwardMomentum = 0;
                RightMomentum = 0;
                UpMomentum = 0;
                RB.velocity = Vector3.zero;
            }
        }

        /// <summary>
        /// Executes the physics-based movement for the vehicle.
        /// </summary>
        public virtual void ExecutePhysicsMove()
        {
            Vector3 tsm = Vector3.one;
            gameObject.GetComponents<VehicleAccelerationModifier>().ForEach(x => x.ModifyAcceleration(ref tsm));
            RB.AddForce(tsm.z * MV.transform.forward * (ForwardMomentum / 100f) * Time.fixedDeltaTime, ForceMode.VelocityChange);
            RB.AddForce(tsm.x * MV.transform.right * (RightMomentum / 100f) * Time.fixedDeltaTime, ForceMode.VelocityChange);
            RB.AddForce(tsm.y * MV.transform.up * (UpMomentum / 100f) * Time.fixedDeltaTime, ForceMode.VelocityChange);
        }

        /// <summary>
        /// Determines if the vehicle can take player inputs.
        /// </summary>
        /// <returns>True if input is allowed.</returns>
        protected virtual bool CanTakeInputs()
        {
            var fcc = MainCameraControl.main.GetComponent<FreecamController>();
            bool isFreecam = false;
            if (fcc.mode || fcc.ghostMode)
            {
                isFreecam = true;
            }
            return MV.CanPilot() && MV.IsPlayerControlling() && !isFreecam;
        }
        #endregion

        #region methods
        /// <summary>
        /// Gets the estimated time (in seconds) for the vehicle to come to a stop.
        /// </summary>
        /// <returns>Maximum time to stop among all axes.</returns>
        public float GetTimeToStop()
        {
            float timeToXStop = Mathf.Log(0.05f * STRAFE_MAX_SPEED / RightMomentum) / (Mathf.Log(.25f));
            float timeToYStop = Mathf.Log(0.05f * VERT_MAX_SPEED / UpMomentum) / (Mathf.Log(.25f));
            float timeToZStop = Mathf.Log(0.05f * FORWARD_TOP_SPEED / ForwardMomentum) / (Mathf.Log(.25f));
            return Mathf.Max(timeToXStop, timeToYStop, timeToZStop);
        }
        #endregion

        /// <summary>
        /// Called when the vehicle is scuttled. Disables the engine.
        /// </summary>
        void IScuttleListener.OnScuttle()
        {
            enabled = false;
        }

        /// <summary>
        /// Called when the vehicle is unscuttled. Enables the engine.
        /// </summary>
        void IScuttleListener.OnUnscuttle()
        {
            enabled = true;
        }
    }
}
