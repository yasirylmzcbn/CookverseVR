using UnityEngine;
using System.Collections.Generic;

public class ShelfItemsManager : MonoBehaviour
{
    [Header("Asset Setup")]
    [Tooltip("List of prefabs that can be randomly placed on shelves")]
    public List<GameObject> shelfItemPrefabs = new List<GameObject>();

    [Header("Settings")]
    [Tooltip("Tag to identify the innermost prop objects to replace (optional)")]
    public string propTag = "ShelfProp";

    [Tooltip("If true, uses tag filtering. If false, replaces all leaf objects.")]
    public bool useTagFiltering = false;

    [Tooltip("If true, spawned items will be children of the shelf. If false, they'll be independent.")]
    public bool parentToShelf = false;

    [Tooltip("Y-axis offset for spawned items (in case you need to adjust height)")]
    public float yOffset = 0f;

    private List<GameObject> spawnedItems = new List<GameObject>();

    void Start()
    {
        if (shelfItemPrefabs == null || shelfItemPrefabs.Count == 0)
        {
            Debug.LogWarning("No shelf item prefabs assigned to ShelfItemsManager. Please add items to the list.", this);
            return;
        }

        ReplaceShelfItems();
    }

    void ReplaceShelfItems()
    {
        List<Transform> propsToReplace = FindInnerMostProps(transform);

        Debug.Log($"Found {propsToReplace.Count} props to replace on {gameObject.name}");

        foreach (Transform prop in propsToReplace)
        {
            GameObject randomPrefab = shelfItemPrefabs[Random.Range(0, shelfItemPrefabs.Count)];
            Vector3 spawnPosition = prop.position + new Vector3(0f, yOffset, 0f);
            Quaternion spawnRotation = prop.rotation;

            GameObject newItem = Instantiate(randomPrefab, spawnPosition, spawnRotation);

            if (parentToShelf)
            {
                newItem.transform.SetParent(prop.parent, true);
            }

            DisableGravity(newItem);
            spawnedItems.Add(newItem);
            prop.gameObject.SetActive(false);
        }

        Debug.Log($"Successfully replaced {spawnedItems.Count} shelf items");
    }

    List<Transform> FindInnerMostProps(Transform parent)
    {
        List<Transform> leafTransforms = new List<Transform>();

        foreach (Transform child in parent)
        {
            if (child.childCount == 0)
            {
                if (useTagFiltering)
                {
                    if (child.CompareTag(propTag))
                    {
                        leafTransforms.Add(child);
                    }
                }
                else
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

    void DisableGravity(GameObject obj)
    {
        Rigidbody rb = obj.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        Rigidbody[] childRigidbodies = obj.GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody childRb in childRigidbodies)
        {
            childRb.useGravity = false;
            childRb.isKinematic = true;
        }
    }

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

    public void RefreshItems()
    {
        ClearAllItems();
        ReplaceShelfItems();
    }
}