using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class KnifeController : MonoBehaviour
{
    [Header("Chop Settings")]
    public float minChopVelocity = 0.3f; // minimum downward speed to count as a chop
    [SerializeField] private Collider bladeCollider;
    private Vector3 lastPosition;
    private Vector3 velocity;
    [SerializeField] private LayerMask ingredientLayer;
    private Rigidbody rb;

    [Header("Haptics")]
    [SerializeField] private bool enableHaptics = true;
    [SerializeField] private bool sendHapticOnGrab = true;
    [SerializeField] private bool sendHapticOnRelease = true;
    [SerializeField] private float grabHapticAmplitude = 0.5f;
    [SerializeField] private float grabHapticDuration = 0.08f;
    [SerializeField] private float releaseHapticAmplitude = 0.35f;
    [SerializeField] private float releaseHapticDuration = 0.06f;
    [SerializeField] private float chopHapticAmplitude = 0.7f;
    [SerializeField] private float chopHapticDuration = 0.12f;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _knifeGrab;
    private bool isGrabEventHooked;

    void Start()
    {
        rb = GetComponentInParent<Rigidbody>();
        Debug.Log("yasir123 KnifeController initialized. Rigidbody found: " + (rb != null));
        lastPosition = transform.position;
        if (bladeCollider == null)
        {
            bladeCollider = GetComponent<Collider>();
            Debug.Log("yasir123 Blade collider auto-assigned: " + (bladeCollider != null));
        }
        _knifeGrab = GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (_knifeGrab == null)
        {
            Debug.LogError("yasir123 XRGrabInteractable not found on knife or parent!");
        }

        RegisterGrabEvents();
    }

    private void OnEnable()
    {
        RegisterGrabEvents();
    }

    private void OnDisable()
    {
        UnregisterGrabEvents();
    }

    private void OnDestroy()
    {
        UnregisterGrabEvents();
    }

    private void RegisterGrabEvents()
    {
        if (isGrabEventHooked)
            return;

        if (_knifeGrab == null)
            _knifeGrab = GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        if (_knifeGrab == null)
            return;

        _knifeGrab.selectEntered.AddListener(OnGrabbed);
        _knifeGrab.selectExited.AddListener(OnReleased);
        isGrabEventHooked = true;
    }

    private void UnregisterGrabEvents()
    {
        if (!isGrabEventHooked || _knifeGrab == null)
            return;

        _knifeGrab.selectEntered.RemoveListener(OnGrabbed);
        _knifeGrab.selectExited.RemoveListener(OnReleased);
        isGrabEventHooked = false;
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (!enableHaptics || !sendHapticOnGrab)
            return;

        SendHapticToInteractor(args.interactorObject, grabHapticAmplitude, grabHapticDuration);
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (!enableHaptics || !sendHapticOnRelease)
            return;

        SendHapticToInteractor(args.interactorObject, releaseHapticAmplitude, releaseHapticDuration);
    }

    private void SendHapticToInteractor(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor interactor, float amplitude, float duration)
    {
        if (interactor == null)
            return;

        amplitude = Mathf.Clamp01(amplitude);
        duration = Mathf.Max(0f, duration);

        if (interactor is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor inputInteractor)
        {
            inputInteractor.SendHapticImpulse(amplitude, duration);
            return;
        }

        if (interactor is Component interactorComponent)
        {
            var hapticPlayer = interactorComponent.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics.HapticImpulsePlayer>(true);
            if (hapticPlayer != null)
                hapticPlayer.SendHapticImpulse(amplitude, duration);
        }
    }

    void Update()
    {
        velocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;

        CheckForChop();
    }


    private void CheckForChop()
    {
        float downwardSpeed = -velocity.y;
        if (downwardSpeed < minChopVelocity) return;

        Bounds b = bladeCollider.bounds;

        Collider[] hits = Physics.OverlapBox(
            b.center,
            b.extents,
            Quaternion.identity,
            ingredientLayer
        );

        foreach (Collider hit in hits)
        {
            Ingredient ingredient = hit.GetComponentInParent<Ingredient>();
            if (ingredient != null && !ingredient.isChopped)
            {
                ingredient.RegisterChop();
                SendChopHaptics(ingredient);
            }
        }
    }

    private void SendChopHaptics(Ingredient ingredient)
    {
        if (_knifeGrab != null)
            SendHapticToGrabbable(_knifeGrab, chopHapticAmplitude, chopHapticDuration);

        if (ingredient.grabInteractable != null)
            SendHapticToGrabbable(ingredient.grabInteractable, chopHapticAmplitude * 0.5f, chopHapticDuration);
    }

    public static void SendHapticToGrabbable(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabbable, float amplitude, float duration)
    {
        Debug.Log($"yasir123 Sending haptic to {grabbable.gameObject.name} with amplitude {amplitude} and duration {duration}");
        Debug.Log("yasir123 interactorsSelecting count: " + grabbable.interactorsSelecting.Count);
        amplitude = Mathf.Clamp01(amplitude);
        duration = Mathf.Max(0f, duration);

        foreach (var interactor in grabbable.interactorsSelecting)
        {
            Debug.Log($"yasir123 Interactor {interactor} is selecting {grabbable.gameObject.name}");
            if (interactor is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor inputInteractor)
            {
                bool sent = inputInteractor.SendHapticImpulse(amplitude, duration);
                Debug.Log($"yasir123 Sent haptic via interactor {interactor}: {sent}");
                continue;
            }

            if (interactor is Component interactorComponent)
            {
                var hapticPlayer = interactorComponent.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics.HapticImpulsePlayer>(true);
                if (hapticPlayer != null)
                {
                    bool sent = hapticPlayer.SendHapticImpulse(amplitude, duration);
                    Debug.Log($"yasir123 Sent haptic via HapticImpulsePlayer on {hapticPlayer.gameObject.name}: {sent}");
                }
            }
        }
    }
}