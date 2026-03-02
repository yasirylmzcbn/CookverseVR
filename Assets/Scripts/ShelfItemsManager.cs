using UnityEngine;
using System.Collections.Generic;
using System.Linq;
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

    [Header("Ingredient Distribution")]
    [Tooltip("If true, each shelf will contain only one type of ingredient")]
    public bool oneIngredientPerShelf = true;

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
    private Dictionary<Transform, ItemType> shelfToIngredientMap = new Dictionary<Transform, ItemType>();

    #endregion

    #region Unity Lifecycle

    void Start()
    {
        if (shelfItemPrefabs == null || shelfItemPrefabs.Count == 0)
        {
            Debug.LogWarning("No shelf item prefabs assigned to ShelfItemsManager. Please add items to the list.", this);
            return;
        }

        // Only auto-replace on start if NOT using ingredient distribution
        // (IngredientDistributionManager will call RefreshItems after setting up the mapping)
        if (!oneIngredientPerShelf)
        {
            Debug.Log("ShelfItemsManager: Auto-spawning items (random mode)");
            ReplaceShelfItems();
        }
        else
        {
            Debug.Log("ShelfItemsManager: Waiting for IngredientDistributionManager to set mapping");
        }
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

    /// <summary>
    /// Sets ingredient types for individual shelves based on hierarchy
    /// </summary>
    public void SetIngredientMapping(Dictionary<Transform, ItemType> ingredientMap)
    {
        shelfToIngredientMap = ingredientMap;
        Debug.Log($"ShelfItemsManager: Set ingredient mapping for {ingredientMap.Count} shelves");
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

        Debug.Log($"Successfully replaced {spawnedItems.Count} shelf items on {gameObject.name}");
    }

    private void SpawnRandomItem(Transform prop)
    {
        GameObject prefabToSpawn;

        if (oneIngredientPerShelf && shelfToIngredientMap.Count > 0)
        {
            // Find which shelf this prop belongs to
            Transform parentShelf = GetShelfForProp(prop);
            
            if (parentShelf != null && shelfToIngredientMap.ContainsKey(parentShelf))
            {
                // Get the assigned ingredient type for this shelf
                ItemType ingredientType = shelfToIngredientMap[parentShelf];
                prefabToSpawn = GetPrefabByIngredientType(ingredientType);
                
                if (prefabToSpawn == null)
                {
                    Debug.LogWarning($"No prefab found for ingredient type {ingredientType} on shelf {parentShelf.name}, using random prefab");
                    prefabToSpawn = shelfItemPrefabs[Random.Range(0, shelfItemPrefabs.Count)];
                }
                else
                {
                    Debug.Log($"ShelfItemsManager: Spawning {ingredientType} on shelf {parentShelf.name} at prop {prop.name}");
                }
            }
            else
            {
                // Error case - couldn't find shelf
                if (parentShelf == null)
                {
                    Debug.LogError($"ShelfItemsManager: Could not find parent shelf for prop {prop.name}! Using random ingredient. Prop hierarchy: {GetHierarchyPath(prop)}");
                }
                else
                {
                    Debug.LogError($"ShelfItemsManager: Shelf {parentShelf.name} not in ingredient map! Using random ingredient.");
                }
                prefabToSpawn = shelfItemPrefabs[Random.Range(0, shelfItemPrefabs.Count)];
            }
        }
        else
        {
            // Random selection (original behavior)
            prefabToSpawn = shelfItemPrefabs[Random.Range(0, shelfItemPrefabs.Count)];
        }

        Vector3 spawnPosition = prop.position + new Vector3(0f, yOffset, 0f);
        Quaternion spawnRotation = prop.rotation;

        GameObject newItem = Instantiate(prefabToSpawn, spawnPosition, spawnRotation);

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

    // Helper method to get the full hierarchy path of a transform
    private string GetHierarchyPath(Transform t)
    {
        string path = t.name;
        Transform current = t.parent;
        while (current != null && current != transform)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }

    private GameObject GetPrefabByIngredientType(ItemType targetType)
    {
        // Filter prefabs that have the target ingredient type
        List<GameObject> matchingPrefabs = new List<GameObject>();

        foreach (GameObject prefab in shelfItemPrefabs)
        {
            ShelfItemData itemData = prefab.GetComponent<ShelfItemData>();
            if (itemData != null && itemData.itemType == targetType)
            {
                matchingPrefabs.Add(prefab);
            }
        }

        // Return a random matching prefab, or null if none found
        return matchingPrefabs.Count > 0 ? matchingPrefabs[Random.Range(0, matchingPrefabs.Count)] : null;
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
        
        // Start as kinematic (not affected by physics) until grabbed
        rb.useGravity = false;
        rb.isKinematic = true;  // Changed from false to true
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

        ShelfItemData data = item.GetComponent<ShelfItemData>();

        if (data != null)
        {
            grabInteractable.selectEntered.AddListener((args) =>
            {
                if (TimedChallengeManager.Instance != null)
                {
                    TimedChallengeManager.Instance
                        .ItemCollected(data.itemType);
                }
            });
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
        
        // Make rigidbody non-kinematic when grabbed, kinematic when released
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
            if (rb != null)
            {
                // Only make kinematic if not throwing
                if (!enableThrowing)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
            }
        });
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

    /// <summary>
    /// Get the shelf that a specific prop belongs to (finds the parent shelf object)
    /// </summary>
    private Transform GetShelfForProp(Transform prop)
    {
        // Walk up the hierarchy to find the shelf
        Transform current = prop.parent;
        
        int levelsUp = 0;
        while (current != null && current != transform)
        {
            levelsUp++;
            
            // Check if this is a shelf by:
            // 1. Seeing if its parent is a row (contains "row")
            // 2. OR if it's named like a shelf (contains "shelf" or "prop_storage")
            if (current.parent != null)
            {
                string parentName = current.parent.name.ToLower();
                string currentName = current.name.ToLower();
                
                // Is the parent a row? Then this is a shelf
                if (parentName.Contains("row"))
                {
                    Debug.Log($"ShelfItemsManager: Prop {prop.name} belongs to shelf {current.name}");
                    return current;
                }
                
                // Is this named like a shelf/storage prop? Then it's probably the shelf
                if (currentName.Contains("shelf") || currentName.Contains("storage"))
                {
                    // Double check if its parent is a row
                    if (current.parent.name.ToLower().Contains("row"))
                    {
                        Debug.Log($"ShelfItemsManager: Prop {prop.name} belongs to shelf {current.name}");
                        return current;
                    }
                }
            }
            
            current = current.parent;
            
            // Safety check to avoid infinite loops
            if (levelsUp > 10)
            {
                Debug.LogWarning($"ShelfItemsManager: Couldn't find shelf for prop {prop.name} after {levelsUp} levels");
                break;
            }
        }
        
        Debug.LogError($"ShelfItemsManager: Could not determine shelf for prop {prop.name}. Hierarchy: {GetHierarchyPath(prop)}");
        return null;
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