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
    [SerializeField] private float chopHapticAmplitude = 0.7f;
    [SerializeField] private float chopHapticDuration = 0.12f;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _knifeGrab;

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