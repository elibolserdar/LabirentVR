using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    [SerializeField] private float mouseSensitivity = 0.08f;
    [SerializeField] private Transform playerBody;

    private float xRotation = 0f;
    private bool lookEnabled;

    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Gerçek Quest/Android cihazýnda mouse ile bakýþ kullanýlmayacak.
        enabled = false;
        return;
#endif

        // Unity Editor'da build target Android olsa bile
        // PC mouse kontrolünü test edebilmek istiyoruz.
        SetLookEnabled(false);
    }

    private void Update()
    {
        if (!lookEnabled || Mouse.current == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float mouseX =
            mouseDelta.x *
            mouseSensitivity;

        float mouseY =
            mouseDelta.y *
            mouseSensitivity;

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