using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public float sensX = 50f;
    public float sensY = 37f;

    [Header("Input (New Input System)")]
    [SerializeField] private InputAction lookAction;

    private float xRotation = 0f;

    public Transform playerBody;

    private void Awake()
    {
        EnsureActionsConfigured();
    }

    private void OnEnable()
    {
        lookAction?.Enable();
    }

    private void OnDisable()
    {
        lookAction?.Disable();
    }

    private void EnsureActionsConfigured()
    {
        if (lookAction != null && lookAction.bindings.Count > 0)
        {
            return;
        }

        lookAction = new InputAction("Look", InputActionType.Value);
        // Mouse delta (pixels per frame)
        lookAction.AddBinding("<Mouse>/delta");
        // Gamepad right stick (-1..1)
        lookAction.AddBinding("<Gamepad>/rightStick");
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Vector2 lookInput = lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;
        float mouseX = lookInput.x * sensX * Time.deltaTime;
        float mouseY = lookInput.y * sensY * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
