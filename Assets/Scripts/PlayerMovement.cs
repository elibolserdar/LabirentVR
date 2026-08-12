using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

using XRInputDevice = UnityEngine.XR.InputDevice;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private MazeConfig config;

    [Header("Gravity")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundedVelocity = -2f;

    [Header("Path Tracking")]
    [SerializeField, Min(0.001f)]
    private float pathSampleDistance = 0.02f;

    private readonly List<Vector3> pathHistory = new();

    private CharacterController controller;

    private XRInputDevice leftController;
    private XRInputDevice rightController;

    private float currentForwardSpeed;
    private float verticalVelocity;
    private float pathLength;

    private Vector3 previousFramePosition;
    private Vector3 lastSampledPosition;

    public float NormalizedSpeed =>
        config == null || config.MaxForwardSpeed <= 0f
            ? 0f
            : currentForwardSpeed / config.MaxForwardSpeed;

    public IReadOnlyList<Vector3> PathHistory =>
        pathHistory;

    private void Awake()
    {
        controller =
            GetComponent<CharacterController>();

        if (config == null)
        {
            Debug.LogError(
                "MazeConfig is not assigned to PlayerMovement.",
                this);

            enabled = false;
            return;
        }

        ResetPath();
    }

    private void OnEnable()
    {
        TryInitializeXRControllers();
    }

    private void Update()
    {
        EnsureXRControllers();

        HandleRotation();
        HandleForwardMovement();
        HandleGravity();
        TrackPath();
    }

    private void HandleRotation()
    {
        float turnInput =
            GetTurnInput();

        transform.Rotate(
            Vector3.up,
            turnInput *
            config.TurnSpeed *
            Time.deltaTime);
    }

    private void HandleForwardMovement()
    {
        float forwardInput =
            GetForwardInput();

        float targetSpeed =
            forwardInput *
            config.MaxForwardSpeed;

        float acceleration =
            config.MaxForwardSpeed /
            Mathf.Max(
                config.AccelerationTime,
                0.01f);

        currentForwardSpeed =
            Mathf.MoveTowards(
                currentForwardSpeed,
                targetSpeed,
                acceleration *
                Time.deltaTime);

        Vector3 movement =
            transform.forward *
            currentForwardSpeed;

        controller.Move(
            movement *
            Time.deltaTime);
    }

    private float GetForwardInput()
    {
#if UNITY_EDITOR
        if (Keyboard.current == null)
            return 0f;

        return Keyboard.current.upArrowKey.isPressed
            ? 1f
            : 0f;

#elif UNITY_ANDROID

        if (leftController.isValid &&
            leftController.TryGetFeatureValue(
                XRCommonUsages.primary2DAxis,
                out Vector2 axis))
        {
            float forward =
                axis.y;

            if (forward <= config.InputDeadzone)
                return 0f;

            // Sadece ileri hareket.
            // Negatif Y = geri hareket, izin vermiyoruz.
            return Mathf.Clamp01(forward);
        }

        return 0f;

#else

        if (Keyboard.current == null)
            return 0f;

        return Keyboard.current.upArrowKey.isPressed
            ? 1f
            : 0f;

#endif
    }

    private float GetTurnInput()
    {
#if UNITY_EDITOR

        if (Keyboard.current == null)
            return 0f;

        if (Keyboard.current.leftArrowKey.isPressed)
            return -1f;

        if (Keyboard.current.rightArrowKey.isPressed)
            return 1f;

        return 0f;

#elif UNITY_ANDROID

        if (rightController.isValid &&
            rightController.TryGetFeatureValue(
                XRCommonUsages.primary2DAxis,
                out Vector2 axis))
        {
            float turn =
                axis.x;

            if (Mathf.Abs(turn) <
                config.InputDeadzone)
            {
                return 0f;
            }

            return Mathf.Clamp(
                turn,
                -1f,
                1f);
        }

        return 0f;

#else

        if (Keyboard.current == null)
            return 0f;

        if (Keyboard.current.leftArrowKey.isPressed)
            return -1f;

        if (Keyboard.current.rightArrowKey.isPressed)
            return 1f;

        return 0f;

#endif
    }

    private void TryInitializeXRControllers()
    {
#if UNITY_ANDROID && !UNITY_EDITOR

        leftController =
            InputDevices.GetDeviceAtXRNode(
                XRNode.LeftHand);

        rightController =
            InputDevices.GetDeviceAtXRNode(
                XRNode.RightHand);

#endif
    }

    private void EnsureXRControllers()
    {
#if UNITY_ANDROID && !UNITY_EDITOR

        if (!leftController.isValid)
        {
            leftController =
                InputDevices.GetDeviceAtXRNode(
                    XRNode.LeftHand);
        }

        if (!rightController.isValid)
        {
            rightController =
                InputDevices.GetDeviceAtXRNode(
                    XRNode.RightHand);
        }

#endif
    }

    private void HandleGravity()
    {
        if (controller.isGrounded &&
            verticalVelocity < 0f)
        {
            verticalVelocity =
                groundedVelocity;
        }

        verticalVelocity +=
            gravity *
            Time.deltaTime;

        controller.Move(
            Vector3.up *
            verticalVelocity *
            Time.deltaTime);
    }

    private void TrackPath()
    {
        Vector3 currentPosition =
            transform.position;

        Vector3 frameMovement =
            currentPosition -
            previousFramePosition;

        frameMovement.y = 0f;

        pathLength +=
            frameMovement.magnitude;

        previousFramePosition =
            currentPosition;

        Vector3 sampleMovement =
            currentPosition -
            lastSampledPosition;

        sampleMovement.y = 0f;

        if (sampleMovement.magnitude <
            pathSampleDistance)
        {
            return;
        }

        pathHistory.Add(
            currentPosition);

        lastSampledPosition =
            currentPosition;
    }

    public void ResetPath()
    {
        pathLength = 0f;

        pathHistory.Clear();

        previousFramePosition =
            transform.position;

        lastSampledPosition =
            transform.position;

        pathHistory.Add(
            transform.position);
    }

    public float GetPathLength()
    {
        return pathLength;
    }

    public void Teleport(
        Vector3 position,
        Quaternion rotation)
    {
        controller.enabled = false;

        transform.SetPositionAndRotation(
            position,
            rotation);

        controller.enabled = true;

        currentForwardSpeed = 0f;
        verticalVelocity =
            groundedVelocity;

        ResetPath();
    }
}