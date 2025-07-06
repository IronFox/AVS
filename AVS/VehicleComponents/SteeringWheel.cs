﻿using UnityEngine;

namespace AVS.VehicleComponents
{
    // This component can be added to a steering wheel
    // in order to make it animate in a way that corresponds
    // with the movements of the vehicle.
    public class SteeringWheel : MonoBehaviour
    {
        public Rigidbody? useRigidbody
        {
            get
            {
                var vh = GetComponentInParent<Vehicle>();
                if (vh != null)
                {
                    return vh.useRigidbody;
                }
                return null;
            }
        }

        // Store the current Z rotation and the velocity used by SmoothDamp
        private float initialYawRotation = 0f;
        private float currentYawRotation = 0f;
        private float rotationVelocity = 0f;
        public float smoothTime = 0.1f;
        public YawAxis yawAxis = YawAxis.z;
        public float maxExpectedAngularVelocity = 7f;
        public float maxSteeringWheelAngle = 45f;
        public enum YawAxis
        {
            x,
            minusX,
            y,
            minusY,
            z,
            minusZ
        }
        public void Start()
        {
            switch (yawAxis)
            {
                case YawAxis.x:
                    initialYawRotation = transform.localEulerAngles.x;
                    break;
                case YawAxis.y:
                    initialYawRotation = transform.localEulerAngles.y;
                    break;
                case YawAxis.z:
                    initialYawRotation = transform.localEulerAngles.z;
                    break;
                case YawAxis.minusX:
                    initialYawRotation = transform.localEulerAngles.x;
                    break;
                case YawAxis.minusY:
                    initialYawRotation = transform.localEulerAngles.y;
                    break;
                case YawAxis.minusZ:
                    initialYawRotation = transform.localEulerAngles.z;
                    break;
            }
        }
        public void Update()
        {
            if (useRigidbody == null)
            {
                return;
            }
            float percentAng = useRigidbody.angularVelocity.y / maxExpectedAngularVelocity;
            float targetYawRotation = percentAng * maxSteeringWheelAngle;

            // Smoothly update the Z rotation using SmoothDamp
            currentYawRotation = Mathf.SmoothDamp(currentYawRotation, -targetYawRotation, ref rotationVelocity, smoothTime);

            // Apply the smoothed rotation to the transform
            switch (yawAxis)
            {
                case YawAxis.x:
                    transform.localEulerAngles = new Vector3(initialYawRotation + currentYawRotation, transform.localEulerAngles.y, transform.localEulerAngles.z);
                    break;
                case YawAxis.y:
                    transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, initialYawRotation + currentYawRotation, transform.localEulerAngles.z);
                    break;
                case YawAxis.z:
                    transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, initialYawRotation + currentYawRotation);
                    break;
                case YawAxis.minusX:
                    transform.localEulerAngles = new Vector3(initialYawRotation - currentYawRotation, transform.localEulerAngles.y, transform.localEulerAngles.z);
                    break;
                case YawAxis.minusY:
                    transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, initialYawRotation - currentYawRotation, transform.localEulerAngles.z);
                    break;
                case YawAxis.minusZ:
                    transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, initialYawRotation - currentYawRotation);
                    break;
            }
        }
    }
}
