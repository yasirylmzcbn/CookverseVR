using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Oculus.Haptics;

public class TimedChallengeManager : MonoBehaviour
{
    public static TimedChallengeManager Instance;

    [Header("Challenge Settings")]
    public float challengeDuration = 120f;
    public int roundsRequired = 10;

    [Header("References")]
    [SerializeField] public ShelfItemsManager shelfManager;
    [SerializeField] public TextMeshProUGUI timerText;
    [SerializeField] public TextMeshProUGUI targetText;

    [Header("Haptics & Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip startSound;
    [SerializeField] private AudioClip correctSound;
    [SerializeField] private AudioClip wrongSound;
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip failSound;
    [SerializeField] private HapticClip hapticClip;

    [SerializeField] private Color warningColor = Color.red;
    [SerializeField] private Color normalColor = Color.white;

    private HapticClipPlayer clipPlayer;

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

    // Track items that the challenge has already directed the user to
    private HashSet<GameObject> usedTargetItems = new HashSet<GameObject>();

    private void Start()
    {
        clipPlayer = new HapticClipPlayer(hapticClip);
    }

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

        if (timerText != null)
        {
            timerText.text = "Time: " + timer.ToString("F1");
            if (timer <= 10f)
            {
                timerText.color = warningColor;
            }
            else
            {
                // Reset to normal color if the challenge restarts
                timerText.color = normalColor;
            }
        }

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
        // Force use of the user's actual Headset/Camera position. 
        // If 'playerTransform' was set to the XR Origin root, it might not move when physically walking in room-scale VR!
        Transform actualPlayerTransform = Camera.main != null ? Camera.main.transform : playerTransform;

        if (actualPlayerTransform == null) return;

        float timeSinceLastUpdate = Time.time - lastPathUpdateTime;
        float distanceMoved = Vector3.Distance(actualPlayerTransform.position, lastPlayerPosition);

        // Update path if enough time has passed OR player moved significantly
        if (timeSinceLastUpdate >= pathUpdateInterval || distanceMoved >= pathUpdateDistanceThreshold)
        {
            ShowPathToTarget();
            lastPathUpdateTime = Time.time;
            lastPlayerPosition = actualPlayerTransform.position;
        }
    }

    // Begin the challenge when the player interacts with the button
    public void StartChallenge()
    {
        if (challengeActive) return;

        challengeActive = true;
        timer = challengeDuration;
        roundsWon = 0;
        usedTargetItems.Clear(); // Reset used items whenever a new challenge starts
        audioSource.PlayOneShot(startSound);

        // Show navigation nodes
        NodeScript.SetAllNodesVisible(true);

        // Make UI visible
        Debug.Log("yasir123 making timer and target text visible");
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
        // Build a list of valid targets based ONLY on what is actually assigned to the shelves
        List<ItemType> validTargets = new List<ItemType>();
        if (shelfManager != null && shelfManager.shelfItemPrefabs != null)
        {
            foreach (GameObject prefab in shelfManager.shelfItemPrefabs)
            {
                if (prefab != null)
                {
                    ShelfItemData itemData = prefab.GetComponent<ShelfItemData>();
                    if (itemData != null && !validTargets.Contains(itemData.itemType))
                    {
                        validTargets.Add(itemData.itemType);
                    }
                }
            }
        }

        // Fallback securely just in case there are no prefabs
        if (validTargets.Count == 0)
        {
            Debug.LogWarning("TimedChallengeManager: No valid targets found in ShelfItemsManager. Falling back to random enum.");
            var allTypes = (ItemType[])System.Enum.GetValues(typeof(ItemType));
            validTargets = new List<ItemType>(allTypes);
        }

        // Pick a random target from the currently available valid list
        currentTarget = validTargets[Random.Range(0, validTargets.Count)];

        targetText.text =
            "Round " + (roundsWon + 1) + "/" + roundsRequired +
            "\nFind: " + FormatEnumName(currentTarget.ToString());

        // Reset cached target item so it will be recalculated on next path update
        cachedTargetItem = null;

        // Reset path update tracking
        lastPathUpdateTime = 0f;
        Transform actualPlayerTransform = Camera.main != null ? Camera.main.transform : playerTransform;
        if (actualPlayerTransform != null)
        {
            lastPlayerPosition = actualPlayerTransform.position;
        }

        // Show initial path to the target item
        if (showPathToTarget)
        {
            ShowPathToTarget();
        }
    }

    private void ShowPathToTarget()
    {
        Transform actualPlayerTransform = Camera.main != null ? Camera.main.transform : playerTransform;

        if (actualPlayerTransform == null)
        {
            Debug.LogWarning("TimedChallengeManager: Player transform not assigned!");
            return;
        }

        // If we don't have a cached target item yet, find the closest one
        if (cachedTargetItem == null)
        {
            // Use the explicit list of spawned items dynamically generated by the shelf manager so it NEVER targets permanent kitchen decor
            List<GameObject> validPool = shelfManager != null ? shelfManager.spawnedItems : null;

            var result = NodeScript.FindPathToClosestItemWithTarget(actualPlayerTransform.position, currentTarget, validPool, usedTargetItems);
            cachedTargetItem = result.targetItem;

            if (cachedTargetItem != null)
            {
                // Register this specific physical GameObject as "used" so it doesn't get picked again
                usedTargetItems.Add(cachedTargetItem);

                ShelfItemData targetData = cachedTargetItem.GetComponent<ShelfItemData>();
                Debug.Log($"TimedChallengeManager: Target item cached - Looking for {currentTarget}, found {targetData?.itemType} at {cachedTargetItem.name}");
            }
            else
            {
                Debug.LogWarning($"TimedChallengeManager: No item found for target type {currentTarget}!");
            }
        }

        // Use cached target item for consistent path throughout the round
        List<NodeScript> path = NodeScript.FindPathToSpecificItem(actualPlayerTransform.position, cachedTargetItem);

        // Update the physical nodes so ONLY the active route based on the player's location is visible!
        NodeScript.SetAllNodesVisible(false);
        if (path != null)
        {
            foreach (NodeScript node in path)
            {
                if (node != null)
                {
                    node.SetNodeVisibility(true);
                }
            }
        }

        // Show the path with line to target item
        NavigationPathVisualizer.ShowPath(path, cachedTargetItem);
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
            audioSource.PlayOneShot(correctSound);

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
        else if(collectedItem != currentTarget)
        {
            audioSource.PlayOneShot(wrongSound);
        }
    }

    // Win Condition: complete 3 rounds within the time limit
    private void ChallengeSuccess()
    {
        challengeActive = false;
        targetText.text = "Success!";
        Debug.Log("SUCCESS");

        // Hide path and nodes
        NavigationPathVisualizer.HidePath();
        NodeScript.SetAllNodesVisible(false);

        audioSource.PlayOneShot(winSound);
        clipPlayer.Play(Controller.Both);

        // Hide after 2 seconds
        Invoke("HideUI", 2f);
    }

    // Lose condition: if you don't complete 3 rounds within the time limit
    private void ChallengeFailed()
    {
        challengeActive = false;
        targetText.text = "Failed!";
        Debug.Log("FAILED");
        audioSource.PlayOneShot(failSound);

        // Hide path and nodes
        NavigationPathVisualizer.HidePath();
        NodeScript.SetAllNodesVisible(false);

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