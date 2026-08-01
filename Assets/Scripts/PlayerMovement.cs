using System.Collections.Generic;
using UnityEngine;

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
    private float currentForwardSpeed;
    private float verticalVelocity;
    private float pathLength;

    private Vector3 previousFramePosition;
    private Vector3 lastSampledPosition;

    public float NormalizedSpeed =>
        config == null || config.MaxForwardSpeed <= 0f
            ? 0f
            : currentForwardSpeed / config.MaxForwardSpeed;

    public IReadOnlyList<Vector3> PathHistory => pathHistory;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

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

    private void Update()
    {
        HandleRotation();
        HandleForwardMovement();
        HandleGravity();
        TrackPath();
    }

    private void HandleRotation()
    {
        float turnInput = 0f;

        if (Input.GetKey(KeyCode.LeftArrow))
            turnInput = -1f;
        else if (Input.GetKey(KeyCode.RightArrow))
            turnInput = 1f;

        transform.Rotate(
            Vector3.up,
            turnInput * config.TurnSpeed * Time.deltaTime);
    }

    private void HandleForwardMovement()
    {
        float targetSpeed = Input.GetKey(KeyCode.UpArrow)
            ? config.MaxForwardSpeed
            : 0f;

        float acceleration =
            config.MaxForwardSpeed /
            Mathf.Max(config.AccelerationTime, 0.01f);

        currentForwardSpeed = Mathf.MoveTowards(
            currentForwardSpeed,
            targetSpeed,
            acceleration * Time.deltaTime);

        Vector3 movement =
            transform.forward * currentForwardSpeed;

        controller.Move(movement * Time.deltaTime);
    }

    private void HandleGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = groundedVelocity;

        verticalVelocity += gravity * Time.deltaTime;

        controller.Move(
            Vector3.up * verticalVelocity * Time.deltaTime);
    }

    private void TrackPath()
    {
        Vector3 currentPosition = transform.position;

        Vector3 frameMovement =
            currentPosition - previousFramePosition;

        frameMovement.y = 0f;
        pathLength += frameMovement.magnitude;
        previousFramePosition = currentPosition;

        Vector3 sampleMovement =
            currentPosition - lastSampledPosition;

        sampleMovement.y = 0f;

        if (sampleMovement.magnitude < pathSampleDistance)
            return;

        pathHistory.Add(currentPosition);
        lastSampledPosition = currentPosition;
    }

    public void ResetPath()
    {
        pathLength = 0f;
        pathHistory.Clear();

        previousFramePosition = transform.position;
        lastSampledPosition = transform.position;

        pathHistory.Add(transform.position);
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

        transform.SetPositionAndRotation(position, rotation);

        controller.enabled = true;

        currentForwardSpeed = 0f;
        verticalVelocity = groundedVelocity;

        ResetPath();
    }
}