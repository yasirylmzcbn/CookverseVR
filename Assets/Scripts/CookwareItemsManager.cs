using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class CookwareItemsManager : MonoBehaviour
{
    private class CookwareGrabListenerFlag : MonoBehaviour
    {
        public bool listenersAdded;
    }

    [Header("Hierarchy Detection")]
    [Tooltip("Optional tag for props. If enabled, only tagged objects are made pickupable.")]
    public bool useTagFiltering = false;

    [Tooltip("Tag used when useTagFiltering is enabled.")]
    public string propTag = "ShelfProp";

    [Header("XR Interaction Settings")]
    [Tooltip("Interaction layers for grabbable cookware")]
    public InteractionLayerMask interactionLayers = 1;

    [Tooltip("Movement type when cookware is grabbed")]
    public XRBaseInteractable.MovementType movementType = XRBaseInteractable.MovementType.Instantaneous;

    [Tooltip("Enable throwing when cookware is released")]
    public bool enableThrowing = true;

    [Tooltip("Automatically setup cookware on Start")]
    public bool autoSetupOnStart = true;

    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupCookwareInteractables();
        }
    }

    public void SetupCookwareInteractables()
    {
        List<Transform> props = FindPropsUnderRows();

        if (props.Count == 0)
        {
            props = FindLeafProps(transform);
            Debug.LogWarning($"CookwareItemsManager: No row-based props found. Falling back to leaf search, found {props.Count} objects.", this);
        }

        int configuredCount = 0;
        foreach (Transform prop in props)
        {
            if (prop == null || !ShouldUseProp(prop))
            {
                continue;
            }

            if (MakeItemPickupable(prop.gameObject))
            {
                configuredCount++;
            }
        }

        Debug.Log($"CookwareItemsManager: Configured {configuredCount}/{props.Count} cookware props for XR grabbing on {gameObject.name}", this);
    }

    private List<Transform> FindPropsUnderRows()
    {
        List<Transform> props = new List<Transform>();
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);

        foreach (Transform child in allChildren)
        {
            if (child == null || child == transform)
            {
                continue;
            }

            if (child.name.ToLower().Contains("row"))
            {
                foreach (Transform rowChild in child)
                {
                    if (rowChild != null)
                    {
                        props.Add(rowChild);
                    }
                }
            }
        }

        return props;
    }

    private List<Transform> FindLeafProps(Transform parent)
    {
        List<Transform> leafTransforms = new List<Transform>();

        foreach (Transform child in parent)
        {
            if (child.childCount == 0)
            {
                leafTransforms.Add(child);
            }
            else
            {
                leafTransforms.AddRange(FindLeafProps(child));
            }
        }

        return leafTransforms;
    }

    private bool ShouldUseProp(Transform prop)
    {
        if (!useTagFiltering)
        {
            return true;
        }

        return prop.CompareTag(propTag);
    }

    private bool MakeItemPickupable(GameObject item)
    {
        if (item == null)
        {
            return false;
        }

        Renderer[] renderers = item.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return false;
        }

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = item.AddComponent<Rigidbody>();
        }

        rb.useGravity = false;
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        Collider col = item.GetComponent<Collider>();
        if (col == null)
        {
            col = item.GetComponentInChildren<Collider>();
            if (col == null)
            {
                BoxCollider box = item.AddComponent<BoxCollider>();
                Bounds combinedBounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    combinedBounds.Encapsulate(renderers[i].bounds);
                }

                Vector3 localCenter = item.transform.InverseTransformPoint(combinedBounds.center);
                box.center = localCenter;
                box.size = combinedBounds.size;
            }
        }

        XRGrabInteractable grabInteractable = item.GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            grabInteractable = item.AddComponent<XRGrabInteractable>();
        }

        ConfigureGrabInteractable(grabInteractable);
        
        item.tag = "Pickup";
        
        return true;
    }

    private void ConfigureGrabInteractable(XRGrabInteractable grabInteractable)
    {
        grabInteractable.interactionLayers = interactionLayers;
        grabInteractable.movementType = movementType;
        grabInteractable.throwOnDetach = enableThrowing;
        grabInteractable.smoothPosition = true;
        grabInteractable.smoothRotation = true;
        grabInteractable.smoothPositionAmount = 10f;
        grabInteractable.smoothRotationAmount = 10f;
        grabInteractable.retainTransformParent = false;
        grabInteractable.trackPosition = true;
        grabInteractable.trackRotation = true;

        CookwareGrabListenerFlag listenerFlag = grabInteractable.GetComponent<CookwareGrabListenerFlag>();
        if (listenerFlag == null)
        {
            listenerFlag = grabInteractable.gameObject.AddComponent<CookwareGrabListenerFlag>();
        }

        if (listenerFlag.listenersAdded)
        {
            return;
        }

        grabInteractable.selectEntered.AddListener((args) =>
        {
            Rigidbody rb = grabInteractable.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
        });

        grabInteractable.selectExited.AddListener((args) =>
        {
            Rigidbody rb = grabInteractable.GetComponent<Rigidbody>();
            if (rb != null && !enableThrowing)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        });

        listenerFlag.listenersAdded = true;
    }
}
