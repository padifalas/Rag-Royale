using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class CountdownManager : MonoBehaviour
{
    [Header("Countdown Text")]
    [SerializeField]
    private string readyText = "Ready?";

    [SerializeField]
    private string fightText = "FIGHT!";

    [SerializeField]
    private string[] countdownSteps = { "3", "2", "1" };

    [Header("Timing")]
    [SerializeField]
    private float readyDuration = 1.2f;

    [SerializeField]
    private float stepDuration = 0.85f;

    [SerializeField]
    private float fightDuration = 0.9f;

    // --- events ---
    public UnityEvent OnCountdownStarted;
    public UnityEvent<string> OnCountdownStep;
    public UnityEvent OnFightStarted;
    public UnityEvent OnCountdownFinished;

    private bool countdownStarted = false;

    public void ResetCountdown()
    {
        countdownStarted = false;
        Debug.Log("[CountdownManager] Reset for new round.");
    }

    public void StartCountdown()
    {
        if (countdownStarted)
        {
            Debug.LogWarning("[CountdownManager] Countdown already started, ignoring.");
            return;
        }
        Debug.Log("[CountdownManager] StartCountdown called, starting coroutine...");
        countdownStarted = true;
        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        Debug.Log("[CountdownManager] Countdown started!");
        SetPlayersEnabled(false);

        OnCountdownStarted?.Invoke();

        OnCountdownStep?.Invoke(readyText);
        yield return new WaitForSeconds(readyDuration);
        Debug.Log("[CountdownManager] Ready phase done");

        foreach (string step in countdownSteps)
        {
            OnCountdownStep?.Invoke(step);
            Debug.Log($"[CountdownManager] Step: {step}");
            yield return new WaitForSeconds(stepDuration);
        }

        Debug.Log("[CountdownManager] Enabling players...");
        SetPlayersEnabled(true);
        OnFightStarted?.Invoke();
        OnCountdownStep?.Invoke(fightText);

        yield return new WaitForSeconds(fightDuration);

        Debug.Log("[CountdownManager] Countdown finished!");
        OnCountdownFinished?.Invoke();
    }

    private void SetPlayersEnabled(bool enabled)
    {
        var controllers = FindObjectsByType<MultiplayerPlayerController>(FindObjectsSortMode.None);
        foreach (var c in controllers)
        {
            c.SetMovementEnabled(enabled);
        }

        var combatSystems = FindObjectsByType<CombatSystem>(FindObjectsSortMode.None);
        foreach (var cs in combatSystems)
        {
            cs.SetCombatEnabled(enabled);
        }
    }
}
