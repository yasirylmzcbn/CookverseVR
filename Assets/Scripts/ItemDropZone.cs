using UnityEngine;

public class ItemDropZone : MonoBehaviour
{
    [Header("Zone Settings")]
    public ItemType acceptedItem;

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

        ItemIdentity item = other.GetComponentInParent<ItemIdentity>();
        if (item == null) return;

        var grab = other.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab != null && grab.isSelected) return;

        if (item.itemType == acceptedItem)
        {
            completed = true;

            SetColor(correctColor);

            Debug.Log("Correct item placed: " + item.itemType);

            KitchenTimerManager.Instance.ZoneCompleted(this);

            SnapItem(item.gameObject);
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