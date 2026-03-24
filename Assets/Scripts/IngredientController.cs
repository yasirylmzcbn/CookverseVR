using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public enum IngredientType
{
    Tomato,
    Potato,
    Steak,
}
public class Ingredient : MonoBehaviour
{
    [Header("Ingredient Type")]
    public IngredientType ingredientType;

    [Header("Chop Settings")]
    public int chopsRequired = 3;

    [Header("Ingredient States")]
    public GameObject regularMesh;
    public GameObject choppedMesh;
    public GameObject cookedMesh;
    [SerializeField] private MeshRenderer choppedMeshRenderer;
    [SerializeField] private MeshRenderer cookedMeshRenderer;
    [SerializeField] private Material rawMaterial;
    [SerializeField] private Material cookedMaterial;
    [SerializeField] private Material burntMaterial;

    [Header("XR Interaction (Optional)")]
    [Tooltip("If left empty, will auto-find XRGrabInteractable on this GameObject.")]
    [SerializeField] public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;

    [Tooltip("Interaction Layer Mask to apply when chopped (cook-ready).")]
    [SerializeField] public InteractionLayerMask cookReadyInteractionLayer;
    [SerializeField] public InteractionLayerMask cookedInteractionLayer;
    [SerializeField] public InteractionLayerMask burntInteractionLayer;

    [Header("Cooking")]
    [SerializeField] private float cookLevel = 0f;
    [SerializeField] private float requiredCookLevel = 100f;
    [SerializeField] private float requiredBurnLevel = 150f;

    private bool isBurnt = false;
    private bool isCooked = false;

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
        if (choppedMeshRenderer == null && choppedMesh != null)
            choppedMeshRenderer = choppedMesh.GetComponent<MeshRenderer>();

        if (cookedMeshRenderer == null && cookedMesh != null)
            cookedMeshRenderer = cookedMesh.GetComponent<MeshRenderer>();

        if (choppedMeshRenderer != null)
            choppedMeshRenderer.enabled = false;

        if (cookedMesh != null)
            cookedMesh.SetActive(false);

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
        regularMesh.GetComponent<MeshRenderer>().enabled = false;
        choppedMesh.SetActive(true);
        choppedMesh.GetComponent<MeshRenderer>().enabled = true;

        if (cookedMesh != null)
        {
            cookedMesh.SetActive(false);
            cookedMesh.GetComponent<MeshRenderer>().enabled = false;
        }


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
        Debug.Log($"yasir123 Cook called on {gameObject.name} with rate {rate}. Current cook level: {cookLevel}");
        if (isBurnt) return;

        if (!isCooked)
        {
            cookLevel += rate * Time.deltaTime;
            cookLevel = Mathf.Min(cookLevel, requiredCookLevel);

            if (cookLevel >= requiredCookLevel)
                OnFullyCooked();
        }
        else
        {
            cookLevel += rate * Time.deltaTime;
            Debug.Log($"yasir123 {gameObject.name} is finished cooking. Current cook level: {cookLevel}/{requiredCookLevel}");

            if (cookLevel >= requiredBurnLevel)
                OnBurnt();
        }
    }

    private void OnFullyCooked()
    {
        isCooked = true;

        if (grabInteractable != null)
            grabInteractable.interactionLayers = cookedInteractionLayer;

        if (cookedMesh != null)
        {
            Debug.Log($"yasir123 Swapping to cooked mesh for {gameObject.name}");
            choppedMesh.SetActive(false);
            cookedMesh.SetActive(true);

            if (cookedMeshRenderer != null)
                cookedMeshRenderer.enabled = true;
        }

        SwapMaterial(cookedMaterial);
        Debug.Log($"{gameObject.name} is fully cooked!");
    }

    private void OnBurnt()
    {
        isBurnt = true;

        if (grabInteractable != null)
            grabInteractable.interactionLayers = burntInteractionLayer;

        SwapMaterial(burntMaterial);
        Debug.Log($"{gameObject.name} is burnt!");
    }

    private void SwapMaterial(Material mat)
    {
        if (mat == null) return;

        var targetRenderer = (cookedMesh != null && isCooked) ? cookedMeshRenderer : choppedMeshRenderer;
        if (targetRenderer == null) return;

        // .material creates a per-instance copy, original asset is untouched
        targetRenderer.material = mat;
    }
}
