using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;

sealed class GyroInputTester : MonoBehaviour
{
    #region Static class members
    // Gyro vector data to rotation conversion
    static Quaternion GyroInputToRotation(in InputAction.CallbackContext ctx)
    {
        // Gyro input data
        var gyro = ctx.ReadValue<Vector3>();

        // Coefficient converting a gyro data value into a degree
        // Note: The actual constant is undocumented and unknown.
        //       I just put a plasible value by guessing.
        const double GyroToAngle = 16 * 360 / System.Math.PI;

        // Delta time from the last event
        var dt = ctx.time - ctx.control.device.lastUpdateTime;
        dt = System.Math.Min(dt, 1.0 / 60); // Discarding large deltas

        return Quaternion.Euler(gyro * (float)(GyroToAngle * dt));
    }

    #endregion

    #region Private members

    // Accumulation of gyro input
    Quaternion _accGyro = Quaternion.identity;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        // input layout extension
        switch (Gamepad.current)
        {
            case DualSenseGamepadHID:
                Debug.Log("DualSense detected.");
                InputSystem.RegisterLayoutOverride(ControllerLayouts.LayoutDS5);
                break;
            case DualShock4GamepadHID:
                Debug.Log("DualShock 4 detected.");
                InputSystem.RegisterLayoutOverride(ControllerLayouts.LayoutDS4);
                break;
            default:
                Debug.Log("Unsupported controller.");
                return;
        }

        // Gyroscope input callback
        var action = new InputAction(binding: "<Gamepad>/gyro");
        action.performed += ctx => _accGyro *= GyroInputToRotation(ctx);
        action.Enable();
    }

    void Update()
    {
        // Current status
        var rot = transform.localRotation;

        // Rotation from gyroscope
        rot *= _accGyro;
        _accGyro = Quaternion.identity;

        // Accelerometer input
        var accel = Gamepad.current?.GetChildControl<Vector3Control>("accel");
        var gravity = accel?.ReadValue() ?? -Vector3.up;

        // Drift compensation using gravitational acceleration
        var comp = Quaternion.FromToRotation(rot * gravity, -Vector3.up);

        // Compensation reduction
        comp.w *= 0.2f /  Time.deltaTime;
        comp = comp.normalized;

        // Update
        transform.localRotation = comp * rot;
    }

    #endregion
}
