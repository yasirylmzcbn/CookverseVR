using UnityEngine;

public class ItemDropZone : MonoBehaviour
{
    [Header("Zone Settings")]
    public IngredientType acceptedItem;


    [Header("Visual (Assign your Plane here)")]
    public Renderer zoneRenderer;

    [Header("Colors")]
    public Color idleColor = Color.black;
    public Color correctColor = Color.green;

    private bool completed = false;
    private Material mat;

    private void Start()
    {
        if (zoneRenderer != null)
        {
            mat = zoneRenderer.material;
            SetColor(idleColor);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (completed) return;
        Debug.Log("yasir123 Item entered drop zone: " + other.gameObject + " " + other.gameObject.name);
        // get IngredientController script from the gameobjec
        Ingredient ingredient = other.GetComponentInParent<Ingredient>();
        if (ingredient != null)
        {
            Debug.Log($"yasir123 Found Ingredient on {other.gameObject.name}");
            if (ingredient.ingredientType == acceptedItem && ingredient.grabInteractable.interactionLayers == ingredient.cookedInteractionLayer)
            {
                completed = true;
                SetColor(correctColor);
                Debug.Log("Correct item placed: " + ingredient.ingredientType); KitchenTimerManager.Instance.ZoneCompleted(this);
                SnapItem(ingredient.gameObject);
            }
        }
        else
        {
            Debug.Log($"yasir123 No Ingredient found on {other.gameObject.name}");
        }

    }

    private void OnTriggerExit(Collider other)
    {
        if (completed) return;

        SetColor(idleColor);
    }

    private void SetColor(Color color)
    {
        if (mat != null)
        {
            mat.color = color;

            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 3f);
        }
    }

    private void SnapItem(GameObject obj)
    {
        obj.transform.SetPositionAndRotation(
            transform.position,
            transform.rotation
        );

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        var grab = obj.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab != null)
        {
            grab.enabled = false;
        }
    }

    public void ResetZone()
    {
        completed = false;
        SetColor(idleColor);
    }
}