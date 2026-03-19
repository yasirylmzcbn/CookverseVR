using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class StoveKnob : UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable
{
    [Header("Knob Settings")]
    public float minAngle = -150f;
    public float maxAngle = 150f;
    public float minValue = 0f;
    public float maxValue = 10f;

    [Header("Snapping (optional)")]
    public bool snapToSteps = false;
    public int stepCount = 10;

    public float CurrentValue { get; private set; }

    private float _currentAngle = 0f;
    private float _previousHandAngle;
    private UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor _interactor;

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        _interactor = args.interactorObject;
        // Record the starting hand angle so we don't snap on grab
        _previousHandAngle = GetAngleOnKnobPlane(_interactor.GetAttachTransform(this).position);
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        _interactor = null;
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);

        if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic && isSelected)
            UpdateRotation();
    }

    void UpdateRotation()
    {
        float handAngle = GetAngleOnKnobPlane(_interactor.GetAttachTransform(this).position);

        float delta = Mathf.DeltaAngle(_previousHandAngle, handAngle);
        _previousHandAngle = handAngle;

        float newAngle = Mathf.Clamp(_currentAngle + delta, minAngle, maxAngle);
        float actualDelta = newAngle - _currentAngle;
        _currentAngle = newAngle;

        transform.Rotate(Vector3.up, actualDelta, Space.Self);

        float t = Mathf.InverseLerp(minAngle, maxAngle, _currentAngle);
        CurrentValue = Mathf.Lerp(minValue, maxValue, t);

        if (snapToSteps)
            CurrentValue = Mathf.Round(CurrentValue * stepCount / maxValue) * maxValue / stepCount;

        OnValueChanged(CurrentValue);
    }

    float GetAngleOnKnobPlane(Vector3 worldPos)
    {
        Vector3 local = transform.InverseTransformPoint(worldPos);
        return Mathf.Atan2(local.z, local.x) * Mathf.Rad2Deg;
    }

    protected virtual void OnValueChanged(float value)
    {
        // Override or use UnityEvent to hook into stove, fryer, etc.
        Debug.Log($"yasir123 Knob value: {value}");
    }
}