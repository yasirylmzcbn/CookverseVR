using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class TimedChallengeManager : MonoBehaviour
{
    public static TimedChallengeManager Instance;

    [Header("Challenge Settings")]
    public float challengeDuration = 120f;
    public int roundsRequired = 10;

    [Header("References")]
    public ShelfItemsManager shelfManager;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI targetText;
    
    [Header("Ingredient Distribution")]
    [Tooltip("Optional: Manager that handles ingredient distribution across shelves")]
    public IngredientDistributionManager ingredientDistribution;

    [Header("Pathfinding Settings")]
    [Tooltip("Transform to use as player position (e.g., XR Origin or Main Camera)")]
    public Transform playerTransform;
    
    [Tooltip("Enable path visualization during the challenge")]
    public bool showPathToTarget = true;

    [Tooltip("How often to update the path (in seconds)")]
    public float pathUpdateInterval = 0.5f;

    [Tooltip("Minimum distance player must move before updating path")]
    public float pathUpdateDistanceThreshold = 1f;

    private float timer;
    private bool challengeActive;
    private int roundsWon;
    private ItemType currentTarget;
    private GameObject cachedTargetItem; // Cache the target item to prevent switching targets as player moves
    
    private float lastPathUpdateTime;
    private Vector3 lastPlayerPosition;

    private void Awake()
    {
        Instance = this;
        
        // Try to find player transform if not assigned
        if (playerTransform == null && Camera.main != null)
        {
            playerTransform = Camera.main.transform;
            Debug.Log("TimedChallengeManager: Using Main Camera as player transform");
        }
    }

    // Updates the timer value
    private void Update()
    {
        if (!challengeActive) return;

        timer -= Time.deltaTime;
        if (timer < 0f) timer = 0f;
        timerText.text = "Time: " + timer.ToString("F1");

        if (timer <= 0f)
        {
            ChallengeFailed();
        }

        // Update path dynamically as player moves
        if (showPathToTarget)
        {
            UpdatePathIfNeeded();
        }
    }

    // Check if we should update the path based on time and distance thresholds
    private void UpdatePathIfNeeded()
    {
        if (playerTransform == null) return;

        float timeSinceLastUpdate = Time.time - lastPathUpdateTime;
        float distanceMoved = Vector3.Distance(playerTransform.position, lastPlayerPosition);

        // Update path if enough time has passed OR player moved significantly
        if (timeSinceLastUpdate >= pathUpdateInterval || distanceMoved >= pathUpdateDistanceThreshold)
        {
            ShowPathToTarget();
            lastPathUpdateTime = Time.time;
            lastPlayerPosition = playerTransform.position;
        }
    }

    // Begin the challenge when the player interacts with the button
    public void StartChallenge()
    {
        if (challengeActive) return;

        challengeActive = true;
        timer = challengeDuration;
        roundsWon = 0;

        // Make UI visible
        if (timerText != null)
            timerText.gameObject.SetActive(true);
        if (targetText != null)
            targetText.gameObject.SetActive(true);

        // Refresh shelf items with new ingredient distribution
        if (ingredientDistribution != null)
        {
            ingredientDistribution.RefreshAllShelves();
        }
        else if (shelfManager != null)
        {
            // Fallback to old behavior if no distribution manager
            shelfManager.RefreshItems();
        }

        // Pick random target
        PickNewTarget();
    }

    // For each round in the challenge, if the player grabs the right item, activates win condition/next round
    private void PickNewTarget()
    {
        currentTarget = (ItemType)Random.Range(
            0, System.Enum.GetValues(typeof(ItemType)).Length
        );

        targetText.text =
            "Round " + (roundsWon + 1) + "/" + roundsRequired +
            "\nFind: " + FormatEnumName(currentTarget.ToString());

        // Reset cached target item so it will be recalculated on next path update
        cachedTargetItem = null;
        
        // Reset path update tracking
        lastPathUpdateTime = 0f;
        if (playerTransform != null)
        {
            lastPlayerPosition = playerTransform.position;
        }

        // Show initial path to the target item
        if (showPathToTarget)
        {
            ShowPathToTarget();
        }
    }

    private void ShowPathToTarget()
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("TimedChallengeManager: Player transform not assigned!");
            return;
        }

        // If we don't have a cached target item yet, find the closest one
        if (cachedTargetItem == null)
        {
            var result = NodeScript.FindPathToClosestItemWithTarget(playerTransform.position, currentTarget);
            cachedTargetItem = result.targetItem;
            
            if (cachedTargetItem != null)
            {
                ShelfItemData targetData = cachedTargetItem.GetComponent<ShelfItemData>();
                Debug.Log($"TimedChallengeManager: Target item cached - Looking for {currentTarget}, found {targetData?.itemType} at {cachedTargetItem.name}");
            }
            else
            {
                Debug.LogWarning($"TimedChallengeManager: No item found for target type {currentTarget}!");
            }
        }

        // Use cached target item for consistent path throughout the round
        List<NodeScript> path = NodeScript.FindPathToSpecificItem(playerTransform.position, cachedTargetItem);

        // Show the path with line to target item
        NavigationPathVisualizer.ShowPath(path, cachedTargetItem);

        // Highlight the target item
        if (cachedTargetItem != null)
        {
            TargetItemHighlighter.HighlightItem(cachedTargetItem);
        }
    }

    // If the item picked up is the correct item, then you win a round
    public void ItemCollected(ItemType collectedItem)
    {
        if (!challengeActive)
        {
            Debug.Log($"TimedChallengeManager: Item collected ({collectedItem}) but challenge is not active");
            return;
        }

        Debug.Log($"TimedChallengeManager: Item collected - Expected {currentTarget}, got {collectedItem}. Match: {collectedItem == currentTarget}");

        if (collectedItem == currentTarget)
        {
            roundsWon++;

            if (roundsWon >= roundsRequired)
            {
                ChallengeSuccess();
            }
            else
            {
                // Next round begins
                PickNewTarget();
            }
        }
    }

    // Win Condition: complete 3 rounds within the time limit
    private void ChallengeSuccess()
    {
        challengeActive = false;
        targetText.text = "Success!";
        Debug.Log("SUCCESS");

        // Hide path and clear highlight
        NavigationPathVisualizer.HidePath();
        TargetItemHighlighter.ClearHighlight();

        // Hide after 2 seconds
        Invoke("HideUI", 2f);
    }

    // Lose condition: if you don't complete 3 rounds within the time limit
    private void ChallengeFailed()
    {
        challengeActive = false;
        targetText.text = "Failed!";
        Debug.Log("FAILED");

        // Hide path and clear highlight
        NavigationPathVisualizer.HidePath();
        TargetItemHighlighter.ClearHighlight();

        // Hide after 2 seconds
        Invoke("HideUI", 2f);
    }

    // Hides the UI for the time challenge
    private void HideUI()
    {
        if (timerText != null)
            timerText.gameObject.SetActive(false);
        if (targetText != null)
            targetText.gameObject.SetActive(false);
    }

    // Enumerates the ingredient names for display on the UI
    private string FormatEnumName(string rawName)
    {
        // Remove prefix like "Food_"
        if (rawName.Contains("_"))
            rawName = rawName.Substring(rawName.IndexOf("_") + 1);

        // Add spaces before capital letters
        return System.Text.RegularExpressions.Regex
            .Replace(rawName, "(\\B[A-Z])", " $1");
    }
}