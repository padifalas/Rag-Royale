using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KillerShotPromptUI : MonoBehaviour
{
    [Header("Main Prompt")]
    [SerializeField]
    private GameObject promptRoot;

    [SerializeField]
    private TextMeshProUGUI promptText;

    [SerializeField]
    private Image promptBackground;

    [Header("Per-Player Button Images")]
    [SerializeField]
    private InputPromptImage p1ButtonPrompt;

    [SerializeField]
    private InputPromptImage p2ButtonPrompt;

    [Header("Prompt Text")]
    [SerializeField]
    private string p1PromptLabel = "REACT!";

    [SerializeField]
    private string p2PromptLabel = "REACT!";

    [SerializeField]
    private string bothPromptLabel = "REACT!";

    [Header("Pulse Settings")]
    [SerializeField]
    private float pulseMinScale = 0.92f;

    [SerializeField]
    private float pulseMaxScale = 1.08f;

    [SerializeField]
    private float pulseSpeedBase = 1.5f;

    [SerializeField]
    private float pulseSpeedMax = 5f;

    [SerializeField]
    private Color promptColorNormal = new Color(1f, 0.85f, 0f);

    [SerializeField]
    private Color promptColorUrgent = new Color(1f, 0.2f, 0.1f);

    [Header("Feedback Popups")]
    [SerializeField]
    private TextMeshProUGUI p1FeedbackText;

    [SerializeField]
    private TextMeshProUGUI p2FeedbackText;

    [Header("Feedback Text")]
    [SerializeField]
    private string earlyPrefix = "TOO EARLY!";

    [SerializeField]
    private string latePrefix = "TOO LATE!";

    [SerializeField]
    private string perfectPrefix = "PERFECT!";

    [SerializeField]
    private bool showPlayerName = true;

    [Header("Feedback Timing")]
    [SerializeField]
    private float feedbackDuration = 1.2f;

    [SerializeField]
    private float feedbackFadeTime = 0.3f;

    [Header("Feedback Colours")]
    [SerializeField]
    private Color colourEarly = Color.red;

    [SerializeField]
    private Color colourLate = new Color(1f, 0.5f, 0f);

    [SerializeField]
    private Color colourPerfect = new Color(0.2f, 1f, 0.4f);

    [Header("Input Prompt Data")]
    [SerializeField]
    private InputPromptData promptData;

    [Header("References")]
    [SerializeField]
    private KillerShotManager killerShotManager;

    private bool isPulsing = false;
    private Coroutine pulseCoroutine;
    private Coroutine p1FeedbackCoroutine;
    private Coroutine p2FeedbackCoroutine;

    private void Start()
    {
        SetActive(promptRoot, false);
        SetActive(p1FeedbackText?.gameObject, false);
        SetActive(p2FeedbackText?.gameObject, false);

        if (killerShotManager == null)
        {
            Debug.LogWarning("[KillerShotPromptUI] KillerShotManager not assigned.");
            return;
        }

        killerShotManager.OnKillerShotPhaseStarted.AddListener(ShowPrompt);
        killerShotManager.OnKillerShotPhaseEnded.AddListener(HidePrompt);
        killerShotManager.OnEarlyPress.AddListener(ShowEarlyPress);
        killerShotManager.OnPerfectPress.AddListener(ShowPerfect);
        killerShotManager.OnTooLate.AddListener(ShowTooLate);
    }

    private void ShowPrompt(int triggeringPlayerID)
    {
        SetActive(promptRoot, true);

        if (promptText != null)
            promptText.text =
                triggeringPlayerID == 1 ? p1PromptLabel
                : triggeringPlayerID == 2 ? p2PromptLabel
                : bothPromptLabel;

        RefreshButtonImages();

        isPulsing = true;
        if (pulseCoroutine != null)
            StopCoroutine(pulseCoroutine);
        pulseCoroutine = StartCoroutine(PulseRoutine());
    }

    private void HidePrompt()
    {
        isPulsing = false;
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        if (promptRoot != null)
        {
            promptRoot.transform.localScale = Vector3.one;
            SetActive(promptRoot, false);
        }
    }

    // Button image swap

    private void RefreshButtonImages()
    {
        p1ButtonPrompt?.SetPlayer(1);
        p1ButtonPrompt?.Refresh();

        p2ButtonPrompt?.SetPlayer(2);
        p2ButtonPrompt?.Refresh();
    }

    private IEnumerator PulseRoutine()
    {
        float t = 0f;
        while (isPulsing)
        {
            float timeRatio =
                killerShotManager.GetWindowDuration() > 0f
                    ? Mathf.Clamp01(
                        killerShotManager.GetWindowTimeRemaining()
                            / killerShotManager.GetWindowDuration()
                    )
                    : 1f;

            float speed = Mathf.Lerp(pulseSpeedMax, pulseSpeedBase, timeRatio);

            if (promptBackground != null)
                promptBackground.color = Color.Lerp(
                    promptColorUrgent,
                    promptColorNormal,
                    timeRatio
                );

            t += Time.deltaTime * speed;
            float scale = Mathf.Lerp(
                pulseMinScale,
                pulseMaxScale,
                (Mathf.Sin(t * Mathf.PI * 2f) + 1f) * 0.5f
            );
            if (promptRoot != null)
                promptRoot.transform.localScale = Vector3.one * scale;

            yield return null;
        }
        if (promptRoot != null)
            promptRoot.transform.localScale = Vector3.one;
    }

    private void ShowEarlyPress(int playerID) => ShowFeedback(playerID, earlyPrefix, colourEarly);

    private void ShowPerfect(int playerID) => ShowFeedback(playerID, perfectPrefix, colourPerfect);

    private void ShowTooLate(int playerID) => ShowFeedback(playerID, latePrefix, colourLate);

    private void ShowFeedback(int playerID, string message, Color color)
    {
        TextMeshProUGUI target = playerID == 1 ? p1FeedbackText : p2FeedbackText;
        if (target == null)
            return;

        if (playerID == 1 && p1FeedbackCoroutine != null)
            StopCoroutine(p1FeedbackCoroutine);
        if (playerID == 2 && p2FeedbackCoroutine != null)
            StopCoroutine(p2FeedbackCoroutine);

        string prefix = showPlayerName ? $"P{playerID} " : "";
        target.text = prefix + message;
        target.color = color;
        target.gameObject.SetActive(true);

        var r = StartCoroutine(FadeOutFeedback(target));
        if (playerID == 1)
            p1FeedbackCoroutine = r;
        else
            p2FeedbackCoroutine = r;
    }

    private IEnumerator FadeOutFeedback(TextMeshProUGUI text)
    {
        yield return new WaitForSeconds(feedbackDuration - feedbackFadeTime);
        float elapsed = 0f;
        Color start = text.color;
        while (elapsed < feedbackFadeTime)
        {
            elapsed += Time.deltaTime;
            text.color = new Color(
                start.r,
                start.g,
                start.b,
                Mathf.Lerp(1f, 0f, elapsed / feedbackFadeTime)
            );
            yield return null;
        }
        text.gameObject.SetActive(false);
    }

    private void SetActive(GameObject go, bool state)
    {
        if (go != null)
            go.SetActive(state);
    }
}
