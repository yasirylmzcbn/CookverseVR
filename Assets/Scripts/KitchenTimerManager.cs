using UnityEngine;
using TMPro;
using Oculus.Haptics;

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

    [Header("Haptics")]
    [SerializeField] public HapticClip hapticClip;
    private HapticClipPlayer clipPlayer;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip startSound;
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip failSound;

    private float timer;
    private bool challengeActive;
    private int zonesCompleted;

    private void Start()
    {
        zones = FindObjectsOfType<ItemDropZone>();
        clipPlayer = new HapticClipPlayer(hapticClip);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
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
    public void StartChallenge()
    {
        if (challengeActive) return;

        challengeActive = true;
        timer = challengeDuration;
        zonesCompleted = 0;

        Debug.Log("Challenge Started");

        // play start sound
        audioSource.PlayOneShot(startSound);

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
        audioSource.PlayOneShot(winSound);

        Debug.Log("SUCCESS");
        clipPlayer.Play(Controller.Both);

        Invoke(nameof(HideUI), 2f);
    }

    // ? LOSE
    private void ChallengeFailed()
    {
        challengeActive = false;

        if (instructionText != null)
            instructionText.text = "FAILED!";
        audioSource.PlayOneShot(failSound);

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

    void OnDestroy()
    {
        clipPlayer?.Dispose();
    }
}