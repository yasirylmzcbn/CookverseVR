using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ShrinkScript : MonoBehaviour
{
    [Header("Buttons")]
    public GameObject greenButton;
    public GameObject yellowButton;
    public GameObject redButton;

    [Header("Shrink Area")]
    public GameObject shrinker;

    private float currentScale = 1f;

    void Start()
    {
        // Setup XR Interactables for the buttons
        SetupButton(greenButton, 2f);
        SetupButton(yellowButton, 1f);
        SetupButton(redButton, 0.5f);

        // Setup the trigger area for the shrinker
        if (shrinker != null)
        {
            var trigger = shrinker.GetComponent<ShrinkTrigger>();
            if (trigger == null)
            {
                trigger = shrinker.AddComponent<ShrinkTrigger>();
            }
            trigger.shrinkScript = this;

            var col = shrinker.GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
            else
            {
                Debug.LogWarning("[ShrinkScript] No Collider found on the shrinker object! Please add one manually in the inspector.");
            }
        }
        else
        {
            Debug.LogError("[ShrinkScript] IMPORTANT: The 'shrinker' field is not assigned in the Inspector! The trigger will not work.");
        }
    }

    private void SetupButton(GameObject button, float scaleValue)
    {
        if (button == null) return;

        var btnInteractable = button.GetComponent<XRSimpleInteractable>();
        if (btnInteractable == null)
        {
            btnInteractable = button.AddComponent<XRSimpleInteractable>();
        }

        var col = button.GetComponent<Collider>();
        if (col == null)
        {
            col = button.AddComponent<BoxCollider>();
        }
        // Colliders for UI/raycast buttons shouldn't be triggers, otherwise raycasts might pass right through them.
        col.isTrigger = false;

        // Add hover debug to help check if the raycast is even hitting the button
        btnInteractable.hoverEntered.AddListener((args) => Debug.Log($"[ShrinkScript] Hovering over {button.name}"));

        // When the button is pressed/selected in VR, set the scale
        btnInteractable.selectEntered.AddListener((args) => 
        {
            Debug.Log($"[ShrinkScript] Button {button.name} pressed. Setting scale to {scaleValue}");
            SetScale(scaleValue);
        });
    }

    public void SetScale(float scale)
    {
        currentScale = scale;
    }

    public void ShrinkObject(GameObject target)
    {
        // Check if the target OR any of its parent objects have the "Pickup" tag
        // or a ShelfItemData component. XR Grab Interactables often have colliders on child objects.
        Transform current = target.transform;
        bool shouldShrink = false;
        Transform rootToShrink = null;

        while (current != null)
        {
            if (current.CompareTag("Pickup") || current.GetComponent<ShelfItemData>() != null)
            {
                shouldShrink = true;
                rootToShrink = current;
                break;
            }
            current = current.parent;
        }

        if (shouldShrink && rootToShrink != null)
        {
            Debug.Log($"[ShrinkScript] Resizing {rootToShrink.name} to scale {currentScale}");
            rootToShrink.localScale = Vector3.one * currentScale;
        }
        else
        {
            Debug.Log($"[ShrinkScript] Ignoring {target.name} because neither it nor its parents have the 'Pickup' tag or a ShelfItemData component.");
        }
    }
}

public class ShrinkTrigger : MonoBehaviour
{
    public ShrinkScript shrinkScript;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[ShrinkTrigger] Collision detected with: {other.name}");
        if (shrinkScript != null)
        {
            shrinkScript.ShrinkObject(other.gameObject);
        }
    }
}
