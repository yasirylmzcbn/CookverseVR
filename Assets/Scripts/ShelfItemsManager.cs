using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ShelfItemsManager : MonoBehaviour
{
    #region Inspector Fields

    [Header("Asset Setup")]
    [Tooltip("List of prefabs that can be randomly placed on shelves")]
    public List<GameObject> shelfItemPrefabs = new List<GameObject>();

    [Header("Placement Settings")]
    [Tooltip("Tag to identify the innermost prop objects to replace (optional)")]
    public string propTag = "ShelfProp";

    [Tooltip("If true, uses tag filtering. If false, replaces all leaf objects.")]
    public bool useTagFiltering = false;

    [Tooltip("If true, spawned items will be children of the shelf. If false, they'll be independent.")]
    public bool parentToShelf = false;

    [Tooltip("Y-axis offset for spawned items (in case you need to adjust height)")]
    public float yOffset = 0f;

    [Header("XR Interaction Settings")]
    [Tooltip("If true, spawned items will be made grabbable by XR player.")]
    public bool makeItemsPickupable = false;

    [Tooltip("Interaction layers for grabbable items")]
    public InteractionLayerMask interactionLayers = 1;

    [Tooltip("Movement type when item is grabbed")]
    public XRBaseInteractable.MovementType movementType = XRBaseInteractable.MovementType.Instantaneous;

    [Tooltip("Enable throwing when item is released")]
    public bool enableThrowing = true;

    #endregion

    #region Private Fields

    private List<GameObject> spawnedItems = new List<GameObject>();

    #endregion

    #region Unity Lifecycle

    void Start()
    {
        if (shelfItemPrefabs == null || shelfItemPrefabs.Count == 0)
        {
            Debug.LogWarning("No shelf item prefabs assigned to ShelfItemsManager. Please add items to the list.", this);
            return;
        }

        ReplaceShelfItems();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Clears all spawned items and re-enables original props.
    /// </summary>
    public void ClearAllItems()
    {
        foreach (GameObject item in spawnedItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }

        spawnedItems.Clear();

        List<Transform> props = FindInnerMostProps(transform);
        foreach (Transform prop in props)
        {
            prop.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Clears existing items and spawns new random items.
    /// </summary>
    public void RefreshItems()
    {
        ClearAllItems();
        ReplaceShelfItems();
    }

    #endregion

    #region Private Methods - Item Spawning

    private void ReplaceShelfItems()
    {
        List<Transform> propsToReplace = FindInnerMostProps(transform);

        Debug.Log($"Found {propsToReplace.Count} props to replace on {gameObject.name}");

        foreach (Transform prop in propsToReplace)
        {
            SpawnRandomItem(prop);
        }

        Debug.Log($"Successfully replaced {spawnedItems.Count} shelf items");
    }

    private void SpawnRandomItem(Transform prop)
    {
        GameObject randomPrefab = shelfItemPrefabs[Random.Range(0, shelfItemPrefabs.Count)];
        Vector3 spawnPosition = prop.position + new Vector3(0f, yOffset, 0f);
        Quaternion spawnRotation = prop.rotation;

        GameObject newItem = Instantiate(randomPrefab, spawnPosition, spawnRotation);

        if (parentToShelf)
        {
            newItem.transform.SetParent(prop.parent, true);
        }

        if (makeItemsPickupable)
        {
            MakeItemPickupable(newItem);
        }
        else
        {
            ConfigureItemPhysics(newItem);
        }

        spawnedItems.Add(newItem);
        prop.gameObject.SetActive(false);
    }

    #endregion

    #region Private Methods - Item Configuration

    private void ConfigureItemPhysics(GameObject item)
    {
        Rigidbody rb = item.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        Rigidbody[] childRigidbodies = item.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody childRb in childRigidbodies)
        {
            childRb.useGravity = false;
            childRb.isKinematic = true;
        }
    }

    private void MakeItemPickupable(GameObject item)
    {
        // Ensure Rigidbody exists and is properly configured for XR Grab
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = item.AddComponent<Rigidbody>();
        }
        
        // For XRGrabInteractable, Rigidbody should NOT be kinematic initially
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Ensure Collider exists
        Collider col = item.GetComponent<Collider>();
        if (col == null)
        {
            col = item.GetComponentInChildren<Collider>();
            if (col == null)
            {
                col = item.AddComponent<BoxCollider>();
                Debug.LogWarning($"No collider found on {item.name}, added BoxCollider. Consider adjusting manually.", item);
            }
        }

        // Add XRGrabInteractable if not present
        XRGrabInteractable grabInteractable = item.GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            grabInteractable = item.AddComponent<XRGrabInteractable>();
            ConfigureGrabInteractable(grabInteractable);
            Debug.Log($"Added XRGrabInteractable to {item.name}", item);
        }
        else
        {
            Debug.Log($"XRGrabInteractable already exists on {item.name}", item);
        }
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
    }

    #endregion

    #region Private Methods - Hierarchy Traversal

    private List<Transform> FindInnerMostProps(Transform parent)
    {
        List<Transform> leafTransforms = new List<Transform>();

        foreach (Transform child in parent)
        {
            if (child.childCount == 0)
            {
                if (ShouldReplaceChild(child))
                {
                    leafTransforms.Add(child);
                }
            }
            else
            {
                leafTransforms.AddRange(FindInnerMostProps(child));
            }
        }

        return leafTransforms;
    }

    private bool ShouldReplaceChild(Transform child)
    {
        if (!useTagFiltering)
        {
            return true;
        }

        return child.CompareTag(propTag);
    }

    #endregion

    #region Obsolete Methods (Kept for Compatibility)

    [System.Obsolete("Use ConfigureItemPhysics instead")]
    private void DisableGravity(GameObject obj)
    {
        ConfigureItemPhysics(obj);
    }

    #endregion
}