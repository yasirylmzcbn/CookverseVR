using UnityEngine;
using TMPro;

public class KitchenTimerManager : MonoBehaviour
{
    public static KitchenTimerManager Instance;

    [Header("Challenge Settings")]
    public float challengeDuration = 120f;
    public int totalZones = 3;
    private ItemDropZone[] zones;

    [Header("UI")]
    [SerializeField] public TextMeshProUGUI timerText;
    [SerializeField] public TextMeshProUGUI instructionText;

    private float timer;
    private bool challengeActive;
    private int zonesCompleted;

    private void Start()
    {
        zones = FindObjectsOfType<ItemDropZone>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        if (!challengeActive) return;

        timer -= Time.deltaTime;
        if (timer < 0f) timer = 0f;

        if (timerText != null)
            timerText.text = "Time: " + timer.ToString("F1");

        if (timer <= 0f)
        {
            ChallengeFailed();
            return;
        }
    }

    // Start challenge (called by VR button)
    public void StartChallenge()
    {
        if (challengeActive) return;

        challengeActive = true;
        timer = challengeDuration;
        zonesCompleted = 0;

        Debug.Log("Challenge Started");

        // Show UI
        if (timerText != null)
            timerText.gameObject.SetActive(true);

        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(true);
            instructionText.text = "Place items in correct zones!";
        }

        // Reset all zones
        foreach (ItemDropZone zone in zones)
        {
            zone.ResetZone();
        }
    }

    // Called by each drop zone
    public void ZoneCompleted(ItemDropZone zone)
    {
        if (!challengeActive) return;

        zonesCompleted++;

        Debug.Log("Zones Completed: " + zonesCompleted + "/" + totalZones);

        if (instructionText != null)
        {
            instructionText.text =
                "Completed: " + zonesCompleted + "/" + totalZones;
        }

        if (zonesCompleted >= totalZones)
        {
            ChallengeSuccess();
        }
    }

    // WIN
    private void ChallengeSuccess()
    {
        challengeActive = false;

        if (instructionText != null)
            instructionText.text = "SUCCESS!";

        Debug.Log("SUCCESS");

        Invoke(nameof(HideUI), 2f);
    }

    // ? LOSE
    private void ChallengeFailed()
    {
        challengeActive = false;

        if (instructionText != null)
            instructionText.text = "FAILED!";

        Debug.Log("FAILED");

        Invoke(nameof(HideUI), 2f);
    }

    // Hide UI
    private void HideUI()
    {
        if (timerText != null)
            timerText.gameObject.SetActive(false);

        if (instructionText != null)
            instructionText.gameObject.SetActive(false);
    }
}