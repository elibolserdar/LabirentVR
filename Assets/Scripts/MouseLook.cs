using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [SerializeField] private float mouseSensitivity = 150f;
    [SerializeField] private Transform playerBody;

    private float xRotation = 0f;
    private bool lookEnabled;

    private void Start()
    {
        #if UNITY_ANDROID
            enabled = false;
            return;
        #endif

        SetLookEnabled(false);
    }

    private void Update()
    {
        if (!lookEnabled)
            return;

        float mouseX =
            Input.GetAxis("Mouse X") *
            mouseSensitivity *
            Time.deltaTime;

        float mouseY =
            Input.GetAxis("Mouse Y") *
            mouseSensitivity *
            Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(
            xRotation,
            -80f,
            80f);

        transform.localRotation =
            Quaternion.Euler(
                xRotation,
                0f,
                0f);

        playerBody.Rotate(
            Vector3.up * mouseX);
    }

    public void SetLookEnabled(bool value)
    {
        lookEnabled = value;

        if (value)
        {
            Cursor.lockState =
                CursorLockMode.Locked;

            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible = true;
        }
    }
}