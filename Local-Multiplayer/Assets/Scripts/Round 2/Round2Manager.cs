using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(RoundManager))]
public class Round2Manager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private NeedleManager needleManager;

    [SerializeField]
    private RoundTimer roundTimer;

    [SerializeField]
    private KillerShotManager killerShotManager;

    [SerializeField]
    private CountdownManager countdownManager;

    private RoundManager roundManager;
    private bool timerPausedForKillerShot = false;

    private void Awake()
    {
        roundManager = GetComponent<RoundManager>();
    }

    private void Start()
    {
        ResolveManagers();
        DisableCombat();

        // If countdownManager not assigned in inspector, find it at runtime
        if (countdownManager == null)
        {
            countdownManager = FindFirstObjectByType<CountdownManager>();
            Debug.Log(
                "[Round2Manager] Found CountdownManager at runtime: "
                    + (countdownManager != null ? "success" : "failed")
            );
        }

        if (countdownManager != null)
            countdownManager.OnCountdownFinished.AddListener(OnCountdownFinished);
        else
            Debug.LogWarning("[Round2Manager] No CountdownManager found!");

        if (roundTimer == null)
        {
            roundTimer = FindFirstObjectByType<RoundTimer>();
            Debug.Log(
                "[Round2Manager] Found RoundTimer at runtime: "
                    + (roundTimer != null ? "success" : "failed")
            );
        }

        if (roundTimer != null)
            roundTimer.OnTimerExpired.AddListener(OnTimerExpired);
        else
            Debug.LogWarning("[Round2Manager] No RoundTimer found!");

        if (needleManager == null)
            needleManager = FindFirstObjectByType<NeedleManager>();

        if (needleManager != null)
        {
            needleManager.OnRoundWinner.AddListener(OnNeedleRoundWinner);
            needleManager.OnNeedleStolen.AddListener(_ =>
                AudioManager.Instance?.PlayNeedleStolen()
            );

            // Inject and subscribe the needle manager to the killer shot manager
            if (killerShotManager != null)
                needleManager.SetKillerShotManager(killerShotManager);
        }
        else
        {
            Debug.LogWarning("[Round2Manager] No NeedleManager found!");
        }

        if (killerShotManager != null)
        {
            killerShotManager.OnKillerShotPhaseStarted.AddListener(_ => PauseTimer());
            killerShotManager.OnKillerShotPhaseEnded.AddListener(ResumeTimer);
        }
        else
        {
            Debug.LogWarning("[Round2Manager] No KillerShotManager found!");
        }

        // Crossfade to Round 2 music as soon as the scene loads
        AudioManager.Instance?.CrossfadeToRound(2);
    }

    private void ResolveManagers()
    {
        if (killerShotManager == null)
            killerShotManager = FindFirstObjectByType<KillerShotManager>();

        if (killerShotManager != null)
        {
            killerShotManager.SetRound(2);
            Debug.Log("[Round2Manager] KillerShotManager resolved and set to Round 2.");
        }
        else
            Debug.LogError(
                "[Round2Manager] KillerShotManager NOT FOUND in scene! Killer shots will not fire."
            );
    }

    private void OnCountdownFinished() => roundTimer?.StartTimer();

    private void OnTimerExpired() => needleManager?.CompareAndDecideWinner();

    // In Round2Manager — fix OnNeedleRoundWinner
    private void OnNeedleRoundWinner(int winnerID)
    {
        if (MatchData.Instance != null)
        {
            MatchData.Instance.P1NeedleCount =
                needleManager != null ? needleManager.P1NeedleCount : 0;
            MatchData.Instance.P2NeedleCount =
                needleManager != null ? needleManager.P2NeedleCount : 0;
        }

        if (winnerID == 0)
        {
            // Genuine tie — neither player wins a round point.
            // Force round end with a sentinel value so SceneTransitionManager
            // can detect the tie and show the tiebreaker panel.
            // We pass 0 here; RoundManager.Debug_ForceEndRound must handle 0.
            Debug.Log("[Round2Manager] Needle tie — triggering tiebreaker.");
            roundManager.Debug_ForceEndRound(0);
            return;
        }

        roundManager.Debug_ForceEndRound(winnerID);
    }

    private void PauseTimer()
    {
        if (roundTimer != null && roundTimer.IsRunning)
        {
            roundTimer.PauseTimer();
            timerPausedForKillerShot = true;
        }
    }

    private void ResumeTimer()
    {
        if (timerPausedForKillerShot && roundTimer != null)
        {
            roundTimer.ResumeTimer();
            timerPausedForKillerShot = false;
        }
    }

    private void DisableCombat()
    {
        var combatSystems = FindObjectsByType<CombatSystem>(FindObjectsSortMode.None);
        foreach (var cs in combatSystems)
            cs.SetCombatEnabled(false);
    }
}
