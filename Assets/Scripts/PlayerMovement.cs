using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float maxForwardSpeed = 1.1f;
    [SerializeField] private float turnSpeed = 50f;
    [SerializeField] private float accelerationTime = 0.4f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundedVelocity = -2f;

    private CharacterController controller;
    private float currentForwardSpeed;
    private float verticalVelocity;

    public float NormalizedSpeed =>
        maxForwardSpeed <= 0f ? 0f : currentForwardSpeed / maxForwardSpeed;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
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

        transform.Rotate(Vector3.up, turnInput * turnSpeed * Time.deltaTime);
    }

    private void HandleForwardMovement()
    {
        float targetSpeed = Input.GetKey(KeyCode.UpArrow)
            ? maxForwardSpeed
            : 0f;

        float acceleration = maxForwardSpeed / Mathf.Max(accelerationTime, 0.01f);

        currentForwardSpeed = Mathf.MoveTowards(
            currentForwardSpeed,
            targetSpeed,
            acceleration * Time.deltaTime);

        Vector3 movement = transform.forward * currentForwardSpeed;
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