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
    public UnityEvent<int> OnRoundWon; // winnerID: 1, 2, or 0 (tie)
    public UnityEvent<int> OnMatchWon;
    public UnityEvent<int, int> OnScoreUpdated;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

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

    // ── MatchData sync ────────────────────────────────────────────────────────

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

    // ── Player resolution ─────────────────────────────────────────────────────

    private void TryResolvePlayerReferences()
    {
        var controllers = FindObjectsByType<MultiplayerPlayerController>(FindObjectsSortMode.None);

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
            return;

        p1Health = found1;
        p2Health = found2;
        p1Health.OnPlayerDefeated.AddListener(() => OnPlayerDefeated(1));
        p2Health.OnPlayerDefeated.AddListener(() => OnPlayerDefeated(2));
        playersResolved = true;
    }

    // ── Round begin ───────────────────────────────────────────────────────────

    private IEnumerator BeginRound()
    {
        OnScoreUpdated?.Invoke(P1RoundWins, P2RoundWins);
        OnRoundStarted?.Invoke(CurrentRound);

        if (countdownManager == null)
            countdownManager = FindFirstObjectByType<CountdownManager>();

        if (countdownManager != null)
        {
            countdownManager.ResetCountdown();
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
                        roundTimerInstance.OnTimerExpired.RemoveListener(OnRoundTimerExpired);

                    roundTimerInstance = roundTimer;
                    roundTimerInstance.OnTimerExpired.AddListener(OnRoundTimerExpired);

                    if (!roundTimer.IsRunning)
                        roundTimer.StartTimer();
                }
            }
        }

        roundOver = false;
    }

    // ── Round end ─────────────────────────────────────────────────────────────

    private void OnPlayerDefeated(int defeatedPlayerID)
    {
        if (roundOver)
            return;
        EndRound(winnerID: defeatedPlayerID == 1 ? 2 : 1);
    }

    private void EndRound(int winnerID)
    {
        if (roundOver)
            return;
        roundOver = true;

        // FIX 1: winnerID == 0 means a genuine tie — neither player gets a win point.
        // Previously this fell into the else branch and gave P2 a free win.
        if (winnerID == 1)
            P1RoundWins++;
        else if (winnerID == 2)
            P2RoundWins++;
        // winnerID == 0 → no increment, we go straight to the tiebreaker round

        // FIX 2: Determine match over BEFORE bumping CurrentRound.
        // SceneTransitionManager reads MatchData.CurrentRound inside OnRoundWon,
        // so the round number must still reflect the round that just ended.
        bool matchOver = P1RoundWins >= roundsToWin || P2RoundWins >= roundsToWin;

        // A tie can only happen at the end of Round 2 (1-1).
        // Treat it as "not match over" — force a Round 3.
        bool isTie = winnerID == 0;

        SaveToMatchData(); // saves CurrentRound before we increment
        OnRoundWon?.Invoke(winnerID);
        OnScoreUpdated?.Invoke(P1RoundWins, P2RoundWins);

        if (matchOver && !isTie)
        {
            StartCoroutine(EndMatch(winnerID));
        }
        else
        {
            // FIX 3: Increment CurrentRound AFTER firing OnRoundWon so that
            // SceneTransitionManager sees the correct round number when it
            // reads MatchData.Instance.CurrentRound inside its OnRoundWon handler.
            CurrentRound++;
            if (MatchData.Instance != null)
                MatchData.Instance.CurrentRound = CurrentRound;
        }
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
            needleManager.CompareAndDecideWinner();
        else
            Debug.LogWarning("[RoundManager] Timer expired but NeedleManager not found.");
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Debug_ForceEndRound(int winnerID) => EndRound(winnerID);
}
