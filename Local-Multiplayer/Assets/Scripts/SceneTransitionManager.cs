using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField]
    private float matchWinDisplayTime = 4f;

    [Header("Next Round Panel (Round 2 clear winner)")]
    [SerializeField]
    private GameObject resultsPanel;

    [Header("Tiebreaker Panel")]
    [SerializeField]
    private GameObject tiebreakerPanel;

    [SerializeField]
    private TextMeshProUGUI tiebreakerHeaderText;

    [SerializeField]
    private TextMeshProUGUI tiebreakerCountdownText;

    [Header("Tiebreaker Sequence Settings")]
    [SerializeField]
    private string[] countdownLabels = { "3", "2", "1", "NAIL IT!" };

    [SerializeField]
    private float countdownStepDuration = 0.7f;

    [SerializeField]
    private float preCountdownDelay = 1.5f;

    [Header("Tiebreaker Header Pulse")]
    [SerializeField]
    private Color headerColorA = new Color(1f, 0.15f, 0.1f);

    [SerializeField]
    private Color headerColorB = new Color(1f, 0.85f, 0f);

    [SerializeField]
    private float headerPulseSpeed = 3.5f;

    private int pendingRound;
    private bool transitionStarted = false; // guard against double-firing
    private Coroutine headerPulseCoroutine;

    private void Start()
    {
        if (resultsPanel != null)
            resultsPanel.SetActive(false);
        if (tiebreakerPanel != null)
            tiebreakerPanel.SetActive(false);

        // Retry finding RoundManager for one frame in case Start() order
        // puts SceneTransitionManager before RoundManager.
        StartCoroutine(SubscribeToRoundManager());
    }

    // ── Subscribes with a one-frame retry so Start() order doesn't matter ──
    private IEnumerator SubscribeToRoundManager()
    {
        RoundManager roundManager = FindFirstObjectByType<RoundManager>();

        if (roundManager == null)
        {
            yield return null; // wait one frame and try again
            roundManager = FindFirstObjectByType<RoundManager>();
        }

        if (roundManager == null)
        {
            Debug.LogError("[SceneTransitionManager] RoundManager not found after retry.");
            yield break;
        }

        roundManager.OnRoundWon.AddListener(OnRoundWon);
        roundManager.OnMatchWon.AddListener(OnMatchWon);
        Debug.Log("[SceneTransitionManager] Subscribed to RoundManager events.");
    }

    private void OnRoundWon(int winnerID)
    {
        if (transitionStarted)
            return;
        if (MatchData.Instance == null)
        {
            Debug.LogWarning("[SceneTransitionManager] No MatchData.");
            return;
        }

        // winnerID == 0 means a tie was declared — always go to Round 3
        if (winnerID == 0)
        {
            Debug.Log("[SceneTransitionManager] Tie detected — going to tiebreaker.");
            pendingRound = 3;
            transitionStarted = true;
            StartCoroutine(TiebreakerSequence());
            return;
        }

        pendingRound = MatchData.Instance.CurrentRound + 1;
        Debug.Log($"[SceneTransitionManager] P{winnerID} won. Pending round: {pendingRound}");

        if (pendingRound >= MatchData.RoundScenes.Length)
        {
            Debug.Log("[SceneTransitionManager] No more rounds — waiting for OnMatchWon.");
            return;
        }

        // Both players now have 1 win each → tiebreaker
        if (pendingRound == 3)
        {
            transitionStarted = true;
            StartCoroutine(TiebreakerSequence());
            return;
        }

        // Round 2 — clear winner, show Next Round button
        transitionStarted = true;
        if (resultsPanel != null)
            resultsPanel.SetActive(true);
        else
            Debug.LogError("[SceneTransitionManager] resultsPanel is NULL.");
    }

    private IEnumerator TiebreakerSequence()
    {
        gameObject.SetActive(true); // ADD THIS LINE
        yield return new WaitForSecondsRealtime(preCountdownDelay);

        if (tiebreakerPanel != null)
            tiebreakerPanel.SetActive(true);

        if (tiebreakerHeaderText != null)
        {
            tiebreakerHeaderText.text = "THE DOLLS ARE EVEN";
            StartCoroutine(PunchScale(tiebreakerHeaderText.transform, 1.25f, 0.12f));
            if (headerPulseCoroutine != null)
                StopCoroutine(headerPulseCoroutine);
            headerPulseCoroutine = StartCoroutine(PulseHeader());
        }

        foreach (string label in countdownLabels)
        {
            if (tiebreakerCountdownText != null)
            {
                tiebreakerCountdownText.text = label;
                StartCoroutine(PunchScale(tiebreakerCountdownText.transform, 1.4f, 0.1f));
            }
            yield return new WaitForSecondsRealtime(countdownStepDuration);
        }

        if (headerPulseCoroutine != null)
        {
            StopCoroutine(headerPulseCoroutine);
            headerPulseCoroutine = null;
        }
        if (tiebreakerPanel != null)
            tiebreakerPanel.SetActive(false);

        LoadNextRoundScene(pendingRound);
    }

    private IEnumerator PunchScale(Transform t, float targetScale, float halfDuration)
    {
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            t.localScale = Vector3.one * Mathf.Lerp(1f, targetScale, elapsed / halfDuration);
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            t.localScale = Vector3.one * Mathf.Lerp(targetScale, 1f, elapsed / halfDuration);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    private IEnumerator PulseHeader()
    {
        float t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime * headerPulseSpeed;
            if (tiebreakerHeaderText != null)
                tiebreakerHeaderText.color = Color.Lerp(
                    headerColorA,
                    headerColorB,
                    (Mathf.Sin(t * Mathf.PI * 2f) + 1f) * 0.5f
                );
            yield return null;
        }
    }

    public void OnNextRoundPressed()
    {
        if (resultsPanel != null)
            resultsPanel.SetActive(false);
        LoadNextRoundScene(pendingRound);
    }

    private void OnMatchWon(int winnerID)
    {
        if (resultsPanel != null)
            resultsPanel.SetActive(false);
        if (tiebreakerPanel != null)
            tiebreakerPanel.SetActive(false);
        if (headerPulseCoroutine != null)
        {
            StopCoroutine(headerPulseCoroutine);
            headerPulseCoroutine = null;
        }
        StartCoroutine(LoadMatchResult(winnerID));
    }

    public void LoadNextRoundScene(int nextRound)
    {
        StartCoroutine(LoadRoundScene(nextRound));
    }

    private IEnumerator LoadRoundScene(int roundNumber)
    {
        if (roundNumber < 1 || roundNumber >= MatchData.RoundScenes.Length)
        {
            Debug.LogError($"[SceneTransitionManager] Invalid round: {roundNumber}");
            yield break;
        }

        string sceneName = MatchData.RoundScenes[roundNumber];
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"[SceneTransitionManager] RoundScenes[{roundNumber}] is empty.");
            yield break;
        }

        LevelManager.Instance.LoadScene(sceneName);
    }

    private IEnumerator LoadMatchResult(int winnerID)
    {
        yield return new WaitForSeconds(matchWinDisplayTime);

        if (MatchData.Instance != null)
        {
            MatchData.Instance.ResetMatch();
            Destroy(MatchData.Instance.gameObject);
        }

        SceneManager.LoadScene(MatchData.MainMenuScene);
    }
}
