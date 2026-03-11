using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class StoveKnob : UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable
{
    [Header("Rotation Axis")]
    [Tooltip("Local-space axis the knob spins around. Y = up (flat knob on stove top).")]
    [SerializeField] private Vector3 rotationAxis = Vector3.up;

    [Header("Angle Limits")]
    [SerializeField] private float minAngle = 0f;
    [SerializeField] private float maxAngle = 270f;
    [SerializeField] private bool clampRotation = true;

    [Header("Rotation Feel")]
    [Tooltip("Multiplies how fast the knob responds to hand movement. Increase if it feels too slow.")]
    [SerializeField] private float sensitivity = 2.5f;
    [Tooltip("Flip this if the knob rotates the wrong way.")]
    [SerializeField] private bool invertDirection = false;

    [Header("Stove On/Off")]
    [SerializeField] private float onThreshold = 15f;

    [Header("Events")]
    public UnityEvent OnBurnerOn;
    public UnityEvent OnBurnerOff;
    public UnityEvent<float> OnValueChanged;

    // ──────────────────────────── State ────────────────────────────────

    private float currentAngle;
    private bool burnerIsOn;

    private UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor activeInteractor;
    private Vector3 previousProjected;
    private Vector3 knobWorldPos;
    private Vector3 cachedLocalScale;  // preserve original scale

    public float CurrentAngle => currentAngle;
    public float NormalizedValue => Mathf.InverseLerp(minAngle, maxAngle, currentAngle);
    public bool IsOn => burnerIsOn;

    protected override void Awake()
    {
        base.Awake();
        cachedLocalScale = transform.localScale;  // cache so we restore correctly
        currentAngle = minAngle;
        ApplyRotation();
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        activeInteractor = args.interactorObject;
        knobWorldPos = transform.position;

        Vector3 handOffset = GetInteractorPosition() - knobWorldPos;
        previousProjected = ProjectOntoPlane(handOffset);
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        activeInteractor = null;
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);

        if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic) return;
        if (activeInteractor == null) return;

        // Lock position and scale
        transform.position = knobWorldPos;
        transform.localScale = cachedLocalScale;

        Vector3 handOffset = GetInteractorPosition() - knobWorldPos;
        Vector3 currentProjected = ProjectOntoPlane(handOffset);

        if (currentProjected.sqrMagnitude < 0.0001f || previousProjected.sqrMagnitude < 0.0001f)
        {
            previousProjected = currentProjected;
            return;
        }

        Vector3 worldAxis = transform.TransformDirection(rotationAxis.normalized);
        float angleDelta = Vector3.SignedAngle(previousProjected, currentProjected, worldAxis);

        // Apply sensitivity and optional invert
        angleDelta *= sensitivity;
        if (invertDirection) angleDelta = -angleDelta;

        float newAngle = currentAngle + angleDelta;
        if (clampRotation)
            newAngle = Mathf.Clamp(newAngle, minAngle, maxAngle);

        currentAngle = newAngle;
        previousProjected = currentProjected;

        ApplyRotation();
        OnValueChanged?.Invoke(NormalizedValue);
        UpdateBurnerState();
    }

    private void ApplyRotation()
    {
        transform.localRotation = Quaternion.AngleAxis(currentAngle, rotationAxis.normalized);
    }

    private Vector3 ProjectOntoPlane(Vector3 v)
    {
        Vector3 worldAxis = transform.TransformDirection(rotationAxis.normalized);
        return v - Vector3.Dot(v, worldAxis) * worldAxis;
    }

    private Vector3 GetInteractorPosition()
    {
        return activeInteractor.GetAttachTransform(this).position;
    }

    private void UpdateBurnerState()
    {
        bool shouldBeOn = currentAngle >= (minAngle + onThreshold);

        if (shouldBeOn && !burnerIsOn)
        {
            burnerIsOn = true;
            OnBurnerOn?.Invoke();
            Debug.Log("[StoveKnob] Burner ON");
        }
        else if (!shouldBeOn && burnerIsOn)
        {
            burnerIsOn = false;
            OnBurnerOff?.Invoke();
            Debug.Log("[StoveKnob] Burner OFF");
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 dir = transform.TransformDirection(rotationAxis.normalized);
        Gizmos.DrawLine(transform.position - dir * 0.1f, transform.position + dir * 0.1f);

        UnityEditor.Handles.color = new Color(0f, 1f, 0.5f, 0.3f);
        UnityEditor.Handles.DrawSolidArc(
            transform.position, dir,
            Quaternion.AngleAxis(minAngle, dir) * Vector3.right,
            maxAngle - minAngle, 0.05f);
    }
#endif
}