using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Manages ingredient distribution across multiple shelves
// Works with a single ShelfItemsManager that manages all rows and shelves
public class IngredientDistributionManager : MonoBehaviour
{
    [Header("Shelf Manager Reference")]
    [Tooltip("The single ShelfItemsManager that manages all shelves (typically on RowsOfShelves)")]
    public ShelfItemsManager shelfManager;

    [Header("Shelf Structure")]
    [Tooltip("Parent transform containing all rows (typically RowsOfShelves)")]
    public Transform rowsParent;

    [Header("Distribution Mode")]
    [Tooltip("If true, each shelf gets one ingredient type. If false, each item is completely random.")]
    public bool organizedMode = true;

    [Header("Auto Setup")]
    [Tooltip("Automatically find the ShelfItemsManager and rows parent on Start")]
    public bool autoFindComponents = true;

    private List<Transform> allShelves = new List<Transform>();

    private void Start()
    {
        if (autoFindComponents)
        {
            FindComponents();
        }

        if (organizedMode)
        {
            // Organized mode: one ingredient per shelf
            FindAllIndividualShelves();
            DistributeIngredients();
        }
        else
        {
            // Random mode: disable organized mode in shelf manager
            if (shelfManager != null)
            {
                shelfManager.oneIngredientPerShelf = false;
                Debug.Log("IngredientDistributionManager: Random mode enabled - each item will be completely random");
            }
        }
        
        // Initialize the shelves with items after distribution is set
        InitializeShelvesWithDistribution();
    }

    // Find the ShelfItemsManager and rows parent
    private void FindComponents()
    {
        if (shelfManager == null)
        {
            shelfManager = FindObjectOfType<ShelfItemsManager>();
            if (shelfManager != null)
            {
                rowsParent = shelfManager.transform;
                Debug.Log($"IngredientDistributionManager: Found ShelfItemsManager on {shelfManager.gameObject.name}");
            }
        }

        if (rowsParent == null && shelfManager != null)
        {
            rowsParent = shelfManager.transform;
        }
    }

    // Find all individual shelf transforms in the hierarchy
    private void FindAllIndividualShelves()
    {
        allShelves.Clear();

        if (rowsParent == null)
        {
            Debug.LogWarning("IngredientDistributionManager: No rows parent found!");
            return;
        }

        Debug.Log($"IngredientDistributionManager: Searching for shelves in {rowsParent.name}");

        // Find all row objects
        int rowCount = 0;
        foreach (Transform row in rowsParent)
        {
            if (row.name.ToLower().Contains("row"))
            {
                rowCount++;
                Debug.Log($"IngredientDistributionManager: Found row: {row.name}");
                
                // Find all shelves in this row
                int shelvesInRow = 0;
                foreach (Transform shelf in row)
                {
                    // This should be an individual shelf
                    allShelves.Add(shelf);
                    shelvesInRow++;
                    Debug.Log($"  ? Shelf {allShelves.Count}: {shelf.name} (in {row.name})");
                }
                
                Debug.Log($"IngredientDistributionManager: Found {shelvesInRow} shelves in {row.name}");
            }
            else
            {
                Debug.Log($"IngredientDistributionManager: Skipping '{row.name}' (doesn't contain 'row')");
            }
        }

        Debug.Log($"IngredientDistributionManager: Found {rowCount} rows and {allShelves.Count} total shelves");
    }

    // Distribute ingredients across shelves
    // Each shelf gets one unique ingredient type (assumes 16 shelves and 16 ingredient types)
    public void DistributeIngredients()
    {
        if (allShelves.Count == 0)
        {
            Debug.LogWarning("IngredientDistributionManager: No shelves found!");
            return;
        }

        if (shelfManager == null)
        {
            Debug.LogWarning("IngredientDistributionManager: No ShelfItemsManager assigned!");
            return;
        }

        if (organizedMode)
        {
            // Get all ingredient types from the ItemType enum
            ItemType[] allIngredients = (ItemType[])System.Enum.GetValues(typeof(ItemType));
            
            if (allIngredients.Length != allShelves.Count)
            {
                Debug.LogWarning($"IngredientDistributionManager: Ingredient count ({allIngredients.Length}) doesn't match shelf count ({allShelves.Count})!");
            }

            // Create a shuffled list of ingredients
            List<ItemType> ingredientPool = new List<ItemType>(allIngredients);
            ShuffleList(ingredientPool);

            // Create mapping of shelf to ingredient type
            Dictionary<Transform, ItemType> shelfToIngredientMap = new Dictionary<Transform, ItemType>();

            // Assign ingredients to shelves (one-to-one mapping)
            int assignmentCount = Mathf.Min(allShelves.Count, ingredientPool.Count);
            for (int i = 0; i < assignmentCount; i++)
            {
                if (allShelves[i] == null) continue;

                ItemType ingredientToAssign = ingredientPool[i];
                shelfToIngredientMap[allShelves[i]] = ingredientToAssign;
                Debug.Log($"IngredientDistributionManager: Assigned {ingredientToAssign} to shelf {allShelves[i].name}");
            }

            // Apply the mapping to the shelf manager
            shelfManager.SetIngredientMapping(shelfToIngredientMap);

            Debug.Log($"IngredientDistributionManager: Distributed {assignmentCount} ingredient types across {allShelves.Count} shelves");
        }
        else
        {
            // Random mode: clear the mapping so ShelfItemsManager uses random selection
            shelfManager.SetIngredientMapping(new Dictionary<Transform, ItemType>());
            Debug.Log("IngredientDistributionManager: Random mode - no shelf mapping applied");
        }
    }

    // Shuffle a list using Fisher-Yates algorithm
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    // Get a random ingredient type
    private ItemType GetRandomIngredient()
    {
        ItemType[] allIngredients = (ItemType[])System.Enum.GetValues(typeof(ItemType));
        return allIngredients[Random.Range(0, allIngredients.Length)];
    }

    // Manually refresh all shelves with new ingredient distribution
    public void RefreshAllShelves()
    {
        if (organizedMode)
        {
            // Organized mode: redistribute ingredients
            FindAllIndividualShelves();
            DistributeIngredients();
        }
        else
        {
            // Random mode: just clear the mapping
            if (shelfManager != null)
            {
                shelfManager.SetIngredientMapping(new Dictionary<Transform, ItemType>());
            }
        }

        Debug.Log($"IngredientDistributionManager: Refreshed all shelves in {(organizedMode ? "organized" : "random")} mode");
    }

    // Initialize shelves with ingredient distribution for the first time
    public void InitializeShelvesWithDistribution()
    {
        if (shelfManager != null)
        {
            Debug.Log("IngredientDistributionManager: Initializing shelves with ingredients");
            shelfManager.RefreshItems();
        }
    }
}
