using UnityEngine;


public class ItemDropZone : MonoBehaviour
{
    [Header("Zone Settings")]
    public ItemType acceptedItem;

    [Header("Visuals")]
    public Renderer zoneRenderer;
    public Color idleColor = Color.white;
    public Color highlightColor = Color.yellow;
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;

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

        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab = other.GetComponentInParent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab != null && grab.isSelected) return;

        if (item.itemType == acceptedItem)
        {
            completed = true;

            SetColor(correctColor);

            KitchenTimerManager.Instance.ZoneCompleted(this);

            SnapItem(other.transform.root.gameObject);
        }
        else
        {
            // flash red briefly
            StartCoroutine(FlashWrong());
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (completed) return;

        ItemIdentity item = other.GetComponentInParent<ItemIdentity>();
        if (item == null) return;

        // Highlight if correct item is nearby
        if (item.itemType == acceptedItem)
        {
            SetColor(highlightColor);
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

            // Optional: emission glow
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 2f);
        }
    }

    private System.Collections.IEnumerator FlashWrong()
    {
        SetColor(wrongColor);
        yield return new WaitForSeconds(0.3f);
        SetColor(idleColor);
    }

    private void SnapItem(GameObject obj)
    {
        obj.transform.position = transform.position;

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }

    public void ResetZone()
    {
        completed = false;
        SetColor(idleColor);
    }
}