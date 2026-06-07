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
        if (killerShotManager == null)
        {
            killerShotManager = FindFirstObjectByType<KillerShotManager>();
            Debug.Log(
                "[Round2Manager] Found KillerShotManager at runtime: "
                    + (killerShotManager != null ? "success" : "failed")
            );
        }
        if (killerShotManager != null)
            killerShotManager.SetRound(2);
    }

    private void Start()
    {
        DisableCombat();

        // Crossfade to Round 2 music as soon as the scene loads
        AudioManager.Instance?.CrossfadeToRound(2);

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
        {
            needleManager = FindFirstObjectByType<NeedleManager>();
            Debug.Log(
                "[Round2Manager] Found NeedleManager at runtime: "
                    + (needleManager != null ? "success" : "failed")
            );
        }

        if (needleManager != null)
        {
            needleManager.OnRoundWinner.AddListener(OnNeedleRoundWinner);
            needleManager.OnNeedleStolen.AddListener(_ =>
                AudioManager.Instance?.PlayNeedleStolen()
            );
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
    }

    private void OnCountdownFinished() => roundTimer?.StartTimer();

    private void OnTimerExpired() => needleManager?.CompareAndDecideWinner();

    private void OnNeedleRoundWinner(int winnerID)
    {
        if (MatchData.Instance != null)
        {
            MatchData.Instance.P1NeedleCount =
                needleManager != null ? needleManager.P1NeedleCount : 0;
            MatchData.Instance.P2NeedleCount =
                needleManager != null ? needleManager.P2NeedleCount : 0;
        }
        roundManager.Debug_ForceEndRound(winnerID == 0 ? 1 : winnerID);
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
