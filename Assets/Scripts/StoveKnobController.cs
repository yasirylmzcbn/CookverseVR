using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class StoveKnob : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float minAngle = 0f;
    [SerializeField] private float maxAngle = 270f;
    [SerializeField] private float onThreshold = 10f; // degrees past min to count as "on"

    [Header("Axis")]
    [SerializeField] private Vector3 rotationAxis = Vector3.forward; // local axis to rotate around

    public bool IsOn { get; private set; }
    public float HeatLevel { get; private set; } // 0-1

    public event System.Action<bool, float> onKnobChanged;

    private XRGrabInteractable grabInteractable;
    private Transform grabbingHand;
    private Vector3 lockedPosition;
    private float currentAngle = 0f;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        // Lock position - don't move, only rotate
        grabInteractable.trackPosition = false;
        grabInteractable.trackRotation = false; // we'll override rotation manually

        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);

        lockedPosition = transform.localPosition;
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        grabbingHand = args.interactorObject.transform;
    }

    void OnReleased(SelectExitEventArgs args)
    {
        grabbingHand = null;
    }

    void Update()
    {
        // Always lock position
        transform.localPosition = lockedPosition;

        if (grabbingHand == null) return;

        // Get the world-space rotation axis
        Vector3 worldAxis = transform.parent != null
            ? transform.parent.TransformDirection(rotationAxis)
            : rotationAxis;

        // Vector from knob to hand
        Vector3 toHand = grabbingHand.position - transform.position;

        // Project onto the plane perpendicular to the rotation axis
        Vector3 projected = Vector3.ProjectOnPlane(toHand, worldAxis);

        if (projected.sqrMagnitude < 0.001f) return; // hand is directly on axis, skip

        // Measure angle from a fixed reference direction (world up projected onto plane)
        Vector3 reference = Vector3.ProjectOnPlane(Vector3.up, worldAxis).normalized;
        float angle = Vector3.SignedAngle(reference, projected.normalized, worldAxis);

        // Remap from [-180,180] to [0,360] then clamp
        angle = (angle + 360f) % 360f;
        angle = Mathf.Clamp(angle, minAngle, maxAngle);

        SetAngle(angle);
    }

    void SetAngle(float angle)
    {
        currentAngle = angle;

        // Apply rotation around the local axis
        Quaternion localRot = Quaternion.AngleAxis(currentAngle, rotationAxis);
        transform.localRotation = localRot;

        // Calculate heat level (0-1)
        float newHeat = Mathf.InverseLerp(minAngle, maxAngle, currentAngle);
        bool newIsOn = currentAngle > (minAngle + onThreshold);

        // Only fire event if something changed
        if (newIsOn != IsOn || Mathf.Abs(newHeat - HeatLevel) > 0.01f)
        {
            IsOn = newIsOn;
            HeatLevel = newHeat;
            onKnobChanged?.Invoke(IsOn, HeatLevel);
        }
    }
}