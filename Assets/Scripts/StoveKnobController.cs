using UnityEngine;
using UnityEngine.InputSystem;


public class KnobController : MonoBehaviour
{
    public enum BurnerSide { Left, Right }

    [Header("Camera Reference")]
    [SerializeField] public Camera kitchenCamera;

    [Header("Cookware Reference")]
    [SerializeField] public GameObject cookware;
    private CookwareSlot cookwareSlot;


    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 1f;
    [SerializeField] private float minAngle = 0f;
    [SerializeField] private float maxAngle = 180f;

    [Header("State")]
    [SerializeField] public BurnerSide side;
    [SerializeField] private float onThreshold = 30f; // Angle at which stove turns on

    private bool isDragging = false;
    private float currentAngle = 0f;
    private Vector2 lastMousePos;
    private Mouse mouse;
    private StoveScript stove;

    void Start()
    {
        mouse = Mouse.current;
        stove = GetComponentInParent<StoveScript>();
        cookwareSlot = cookware.GetComponent<CookwareSlot>();
        if (cookwareSlot != null)
        {
            cookwareSlot.IsOn = false;
        }
    }

    void Update()
    {
        if (mouse == null) return;

        // Check for mouse click on this object
        if (mouse.leftButton.wasPressedThisFrame)
        {
            Ray ray = kitchenCamera.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    isDragging = true;
                    lastMousePos = mouse.position.ReadValue();
                }
            }
        }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
        }

        if (isDragging)
        {
            Vector2 currentMousePos = mouse.position.ReadValue();
            Vector2 mouseDelta = currentMousePos - lastMousePos;

            // Calculate rotation based on horizontal mouse movement
            float rotationAmount = mouseDelta.x * rotationSpeed;
            currentAngle += rotationAmount;
            currentAngle = Mathf.Clamp(currentAngle, minAngle, maxAngle);
            transform.localRotation = Quaternion.Euler(0, currentAngle, 0);

            if (stove != null)
            {
                stove.ApplyVisuals(side, GetHeatLevel());

            }

            bool wasOn = cookwareSlot.IsOn;
            cookwareSlot.IsOn = currentAngle >= onThreshold;

            if (wasOn != cookwareSlot.IsOn)
            {
                OnStateChanged(cookwareSlot.IsOn);
            }

            lastMousePos = currentMousePos;
        }
    }

    protected virtual void OnStateChanged(bool newState)
    {
        Debug.Log($"Knob {gameObject.name} is now: {(newState ? "ON" : "OFF")}");
    }

    // Public method to get current angle (useful for heat levels)
    public float GetCurrentAngle()
    {
        return currentAngle;
    }

    // Get normalized heat level (0 to 1)
    public float GetHeatLevel()
    {
        return Mathf.InverseLerp(minAngle, maxAngle, currentAngle);
    }
}