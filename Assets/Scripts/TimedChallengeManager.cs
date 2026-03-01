using UnityEngine;
using TMPro;

public class TimedChallengeManager : MonoBehaviour
{
    public static TimedChallengeManager Instance;

    [Header("Challenge Settings")]
    public float challengeDuration = 60f;
    public int roundsRequired = 3;

    [Header("References")]
    public ShelfItemsManager shelfManager;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI targetText;

    private float timer;
    private bool challengeActive;
    private int roundsWon;

    private ItemType currentTarget;

    private void Awake()
    {
        Instance = this;
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

        // Refresh shelf items when a new challenge is activated
        shelfManager.RefreshItems();

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
    }

    // If the item picked up is the correct item, then you win a round
    public void ItemCollected(ItemType collectedItem)
    {
        if (!challengeActive) return;

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

        // Hide after 2 seconds
        Invoke("HideUI", 2f);
    }

    // Lose condition: if you don't complete 3 rounds within the time limit
    private void ChallengeFailed()
    {
        challengeActive = false;
        targetText.text = "Failed!";
        Debug.Log("FAILED");

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