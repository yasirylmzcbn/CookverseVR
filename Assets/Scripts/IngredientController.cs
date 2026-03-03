using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Ingredient : MonoBehaviour
{
    [Header("Chop Settings")]
    public int chopsRequired = 3;

    [Header("Mesh References")]
    public GameObject regularMesh;
    public GameObject choppedMesh;

    [Header("XR Interaction (Optional)")]
    [Tooltip("If left empty, will auto-find XRGrabInteractable on this GameObject.")]
    [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;

    [Tooltip("Interaction Layer Mask to apply when chopped (cook-ready).")]
    [SerializeField] private InteractionLayerMask cookReadyInteractionLayer;

    [Header("Cooking")]
    [SerializeField] private float cookLevel = 0f;
    [SerializeField] private float requiredCookLevel = 5f;
    [Tooltip("Rate per second at which cookLevel increases")]
    [SerializeField] private float cookRate = 1f;

    private int chopCount = 0;
    public bool isChopped = false;

    private float chopCooldown = 0.3f;
    private float lastChopTime = -999f;

    private InteractionLayerMask originalInteractionLayers;
    private bool hasOriginalInteractionLayers;

    private void Awake()
    {
        if (grabInteractable == null)
            grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        if (grabInteractable != null)
        {
            originalInteractionLayers = grabInteractable.interactionLayers;
            hasOriginalInteractionLayers = true;
        }
    }

    public void RegisterChop()
    {
        Debug.Log($"yasir123 RegisterChop called on {gameObject.name}. Current chop count: {chopCount}");
        if (isChopped) return;
        if (Time.time - lastChopTime < chopCooldown) return;

        lastChopTime = Time.time;
        chopCount++;
        Debug.Log($"yasir123 Chop {chopCount}/{chopsRequired}");

        if (chopCount >= chopsRequired)
        {
            Chop();
        }
    }

    private void Chop()
    {
        isChopped = true;
        regularMesh.SetActive(false);
        choppedMesh.SetActive(true);

        if (grabInteractable != null)
        {
            var idx = InteractionLayerMask.NameToLayer("CookReadyIngredient");
            Debug.Log($"yasir123 Setting interaction layer to 'CookReadyIngredient' (Layer {idx}) for {gameObject.name}");
            grabInteractable.interactionLayers = (InteractionLayerMask)(1 << idx);
            Debug.Log($"yasir123 New interaction layers: {grabInteractable.interactionLayers}");
        }

        Debug.Log($"yasir123 {gameObject.name} has been chopped! {gameObject.layer}");
    }

    public void Cook(float rate)
    {
        if (cookLevel >= requiredCookLevel) return;
        cookLevel += rate * Time.deltaTime;
        cookLevel = Mathf.Min(cookLevel, requiredCookLevel);

        if (cookLevel >= requiredCookLevel)
            OnFullyCooked();
    }

    private void OnFullyCooked()
    {
        Debug.Log($"{gameObject.name} is fully cooked!");
        // swap mesh, trigger effects, etc.
    }
}
