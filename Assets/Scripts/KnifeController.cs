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
            if (ingredient != null)
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
        foreach (var interactor in grabbable.interactorsSelecting)
        {
            if (interactor is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor controllerInteractor)
            {
                Debug.Log($"yasir123 Sending haptic to interactor {interactor}");
                controllerInteractor.xrController.SendHapticImpulse(amplitude, duration);
            }
        }
    }

    // void OnDrawGizmos()
    // {
    //     if (bladeTip == null || bladeBase == null) return;
    //     Gizmos.color = Color.red;
    //     Vector3 bladeCenter = (bladeTip.position + bladeBase.position) / 2f;
    //     Gizmos.matrix = Matrix4x4.TRS(bladeCenter, transform.rotation, Vector3.one);
    //     Gizmos.DrawWireCube(Vector3.zero, bladeHalfExtents * 2f);
    // }
}