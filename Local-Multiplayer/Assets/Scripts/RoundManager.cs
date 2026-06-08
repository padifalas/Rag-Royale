using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class RoundManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private KillerShotManager killerShotManager;

    [SerializeField]
    public KnockdownManager knockdownManager;

    [SerializeField]
    private CountdownManager countdownManager;

    [SerializeField]
    private SceneTransitionManager sceneTransition;

    [Header("Round Settings")]
    [SerializeField]
    private int roundsToWin = 2;

    [SerializeField]
    private float roundEndDelay = 2f;

    public int CurrentRound { get; private set; }
    public int P1RoundWins { get; private set; }
    public int P2RoundWins { get; private set; }

    private bool roundOver = false;
    private bool playersResolved = false;
    private bool roundStarted = false;

    private RoundTimer roundTimerInstance;
    private PlayerHealth p1Health;
    private PlayerHealth p2Health;
    private MultiplayerPlayerController p1Controller;
    private MultiplayerPlayerController p2Controller;

    public UnityEvent<int> OnRoundStarted;
    public UnityEvent<int> OnRoundWon;
    public UnityEvent<int> OnMatchWon;
    public UnityEvent<int, int> OnScoreUpdated;
    public static RoundManager Instance { get; private set; }

    private void OnDestroy()
    {
        Debug.Log($"ROUND MANAGER DESTROYED: {gameObject.name}");
    }
    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        LoadFromMatchData();

        AudioManager.Instance?.CrossfadeToRound(CurrentRound);

    }



    private void Update()
    {
        if (!playersResolved)
        {
            TryResolvePlayerReferences();
            return;
        }

        if (!roundStarted)
        {
            roundStarted = true;
            StartCoroutine(BeginRound());
        }
    }

    private void LoadFromMatchData()
    {
        if (MatchData.Instance == null)
        {
            Debug.LogWarning("[RoundManager] No MatchData — using defaults.");
            CurrentRound = 1;
            P1RoundWins = 0;
            P2RoundWins = 0;
            roundsToWin = 2;
            return;
        }

        CurrentRound = MatchData.Instance.CurrentRound;
        P1RoundWins = MatchData.Instance.P1RoundWins;
        P2RoundWins = MatchData.Instance.P2RoundWins;
        roundsToWin = MatchData.Instance.RoundsToWin;
    }

    private void SaveToMatchData()
    {
        if (MatchData.Instance == null)
            return;
        MatchData.Instance.P1RoundWins = P1RoundWins;
        MatchData.Instance.P2RoundWins = P2RoundWins;
        MatchData.Instance.CurrentRound = CurrentRound;
    }

    private void TryResolvePlayerReferences()
    {
        var controllers = FindObjectsByType<MultiplayerPlayerController>(FindObjectsSortMode.None);
        Debug.Log(
            $"[RoundManager] TryResolvePlayerReferences: Found {controllers.Length} controllers in scene."
        );

        PlayerHealth found1 = null,
            found2 = null;

        foreach (var c in controllers)
        {
            if (c.PlayerID == 1)
            {
                found1 = c.GetComponent<PlayerHealth>();
                p1Controller = c;
            }
            else if (c.PlayerID == 2)
            {
                found2 = c.GetComponent<PlayerHealth>();
                p2Controller = c;
            }
        }

        if (found1 == null || found2 == null)
        {
            Debug.Log(
                $"[RoundManager] Still waiting for players... P1:{(found1 != null ? "found" : "missing")} P2:{(found2 != null ? "found" : "missing")}"
            );
            return;
        }

        p1Health = found1;
        p2Health = found2;

        p1Health.OnPlayerDefeated.AddListener(() => OnPlayerDefeated(1));
        p2Health.OnPlayerDefeated.AddListener(() => OnPlayerDefeated(2));

        playersResolved = true;
        Debug.Log("[RoundManager] Players resolved! Starting BeginRound.");
    }

    private IEnumerator BeginRound()
    {
        OnScoreUpdated?.Invoke(P1RoundWins, P2RoundWins);
        OnRoundStarted?.Invoke(CurrentRound);

        // ensure we have a scene-local CountdownManager (some scenes don't wire the serialized reference)
        if (countdownManager == null)
        {
            countdownManager = FindFirstObjectByType<CountdownManager>();
            if (countdownManager == null)
                Debug.LogWarning("[RoundManager] No CountdownManager found in scene.");
            else
                Debug.Log("[RoundManager] Found CountdownManager at runtime.");
        }

        if (countdownManager != null)
        {
            if (!countdownManager.gameObject.activeInHierarchy)
            {
                Debug.LogWarning("[RoundManager] CountdownManager is inactive, activating it.");
                countdownManager.gameObject.SetActive(true);
            }
            if (!countdownManager.enabled)
            {
                Debug.LogWarning(
                    "[RoundManager] CountdownManager script is disabled, enabling it."
                );
                countdownManager.enabled = true;
            }

            countdownManager.ResetCountdown(); // Reset the countdown flag for this round
            countdownManager.StartCountdown();
            bool done = false;
            countdownManager.OnCountdownFinished.AddListener(() => done = true);
            yield return new WaitUntil(() => done);

            if (CurrentRound >= 2)
            {
                var roundTimer = FindFirstObjectByType<RoundTimer>();
                if (roundTimer != null)
                {
                    if (roundTimerInstance != null)
                    {
                        roundTimerInstance.OnTimerExpired.RemoveListener(OnRoundTimerExpired);
                    }

                    roundTimerInstance = roundTimer;
                    roundTimer.OnTimerExpired.AddListener(OnRoundTimerExpired);

                    if (!roundTimer.IsRunning)
                    {
                        Debug.Log($"[RoundManager] Starting RoundTimer for Round {CurrentRound}.");
                        roundTimer.StartTimer();
                    }
                }
                else
                {
                    Debug.LogWarning("[RoundManager] No RoundTimer found for timer-driven round end.");
                }
            }
        }

        roundOver = false;

        foreach (var p in FindObjectsByType<MultiplayerPlayerController>(FindObjectsSortMode.None))
        {
            p.SetMovementEnabled(true);
        }
    }

    private void OnPlayerDefeated(int playerID)
    {
        if (roundOver)
            return;
        EndRound(winnerID: playerID == 1 ? 2 : 1);
    }

    private void EndRound(int winnerID)
    {
        Debug.Log($"EndRound called on {gameObject.name}");

        if (roundOver)
            return;
        roundOver = true;

        if (winnerID == 1)
            P1RoundWins++;
        else
            P2RoundWins++;

        SaveToMatchData();
        OnRoundWon?.Invoke(winnerID);
        OnScoreUpdated?.Invoke(P1RoundWins, P2RoundWins);

        bool matchOver = P1RoundWins >= roundsToWin || P2RoundWins >= roundsToWin;

        if (matchOver)
            StartCoroutine(EndMatch(winnerID));
        else
        {
            CurrentRound++;
            if (MatchData.Instance != null)
                MatchData.Instance.CurrentRound = CurrentRound;
            //StartCoroutine(TransitionToNextRound());
        }
    }

    private IEnumerator TransitionToNextRound()
    {
        yield return new WaitForSeconds(roundEndDelay);

        if (sceneTransition != null)
            sceneTransition.LoadNextRoundScene(CurrentRound);
        else if (CurrentRound < MatchData.RoundScenes.Length)
            UnityEngine.SceneManagement.SceneManager.LoadScene(MatchData.RoundScenes[CurrentRound]);
    }

    private IEnumerator EndMatch(int winnerID)
    {
        yield return new WaitForSeconds(roundEndDelay);
        OnMatchWon?.Invoke(winnerID);
    }

    private void OnRoundTimerExpired()
    {
        if (CurrentRound != 2)
            return;

        var needleManager = FindFirstObjectByType<NeedleManager>();
        if (needleManager != null)
        {
            needleManager.CompareAndDecideWinner();
            return;
        }

        Debug.LogWarning("[RoundManager] Round timer expired but NeedleManager was not found.");
    }

    public void Debug_ForceEndRound(int winnerID) => EndRound(winnerID);
}
