using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private MazeConfig config;

    [Header("Gravity")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundedVelocity = -2f;

    private CharacterController controller;
    private float currentForwardSpeed;
    private float verticalVelocity;

    public float NormalizedSpeed
    {
        get
        {
            if (config == null || config.MaxForwardSpeed <= 0f)
                return 0f;

            return currentForwardSpeed / config.MaxForwardSpeed;
        }
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (config == null)
        {
            Debug.LogError(
                "MazeConfig is not assigned to PlayerMovement.",
                this);

            enabled = false;
        }
    }

    private void Update()
    {
        HandleRotation();
        HandleForwardMovement();
        HandleGravity();
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
}