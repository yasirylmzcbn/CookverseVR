using UnityEngine;
using TMPro;

public class TimedChallengeManager : MonoBehaviour
{
    public static TimedChallengeManager Instance;

    [Header("Challenge Settings")]
    public float challengeDuration = 10f;

    [Header("References")]
    public ShelfItemsManager shelfManager;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI targetText;

    private float timer;
    private bool challengeActive;

    private ItemType currentTarget;

    private void Awake()
    {
        Instance = this;
    }

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

    public void StartChallenge()
    {
        if (challengeActive) return;

        challengeActive = true;
        timer = challengeDuration;

        // Make UI visible
        if (timerText != null)
            timerText.gameObject.SetActive(true);
        if (targetText != null)
            targetText.gameObject.SetActive(true);

        // Refresh shelf items each round
        if (shelfManager != null)
            shelfManager.RefreshItems();

        // Pick random target
        currentTarget = (ItemType)Random.Range(0, System.Enum.GetValues(typeof(ItemType)).Length);

        // Update UI
        if (targetText != null)
            targetText.text = "Find: " + FormatEnumName(currentTarget.ToString());
        if (timerText != null)
            timerText.text = "Time: " + timer.ToString("F1");
    }

    public void ItemCollected(ItemType collectedItem)
    {
        if (!challengeActive) return;

        if (collectedItem == currentTarget)
        {
            ChallengeSuccess();
        }
    }

    private void ChallengeSuccess()
    {
        challengeActive = false;
        targetText.text = "Success!";
        Debug.Log("SUCCESS");

        // Hide after 2 seconds
        Invoke("HideUI", 2f);
    }

    private void ChallengeFailed()
    {
        challengeActive = false;
        targetText.text = "Failed!";
        Debug.Log("FAILED");

        // Hide after 2 seconds
        Invoke("HideUI", 2f);
    }

    private void HideUI()
    {
        if (timerText != null)
            timerText.gameObject.SetActive(false);
        if (targetText != null)
            targetText.gameObject.SetActive(false);
    }

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