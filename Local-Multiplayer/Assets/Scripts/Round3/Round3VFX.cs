using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Round3VFX : MonoBehaviour
{
    [Header("Power Throw — Readiness Particles")]
    [SerializeField]
    private ParticleSystem p1PowerReadyParticles;

    [SerializeField]
    private ParticleSystem p2PowerReadyParticles;

    [Header("Power Throw — Launch Burst")]
    [SerializeField]
    private ParticleSystem p1PowerLaunchBurst;

    [SerializeField]
    private ParticleSystem p2PowerLaunchBurst;

    [Header("Normal Throw — Launch Puff")]
    [SerializeField]
    private ParticleSystem p1NormalLaunchPuff;

    [SerializeField]
    private ParticleSystem p2NormalLaunchPuff;

    [Header("Killer Shot Warning")]
    [SerializeField]
    private Image killerShotWarningOverlay;

    [SerializeField]
    private float killerShotFlashDuration = 0.6f;

    [SerializeField]
    private Color killerShotFlashColor = new Color(1f, 0.1f, 0.05f, 0.55f);

    [SerializeField]
    private ParticleSystem killerShotBurstParticles;

    [Header("Round End — Per-Player Confetti")]
    [Tooltip("Confetti particle GO pre-placed in scene for P1 win — starts inactive")]
    [SerializeField]
    private GameObject p1ConfettiGO;

    [Tooltip("Confetti particle GO pre-placed in scene for P2 win — starts inactive")]
    [SerializeField]
    private GameObject p2ConfettiGO;

    [Header("Round End — Flash + Results")]
    [SerializeField]
    private Image roundEndFlashOverlay;

    [SerializeField]
    private float roundEndFlashDuration = 0.4f;

    [SerializeField]
    private Color roundEndFlashColor = new Color(1f, 0.85f, 0.1f, 0.45f);

    [Tooltip("Results/game-over panel to show after round ends")]
    [SerializeField]
    private GameObject resultsPanel;

    [SerializeField]
    private float resultsPanelDelay = 1.2f;

    [Header("Audio Clips")]
    [SerializeField]
    private AudioClip powerThrowReadyClip;

    [SerializeField]
    private AudioClip powerThrowLaunchClip;

    [SerializeField]
    private AudioClip killerShotWarningClip;

    [SerializeField]
    private AudioClip roundEndFanfareClip;

    private Coroutine overlayCoroutine;

    private void Start()
    {
        // Confetti GOs are pre-placed in scene, start inactive
        if (p1ConfettiGO != null)
            p1ConfettiGO.SetActive(false);
        if (p2ConfettiGO != null)
            p2ConfettiGO.SetActive(false);
        if (resultsPanel != null)
            resultsPanel.SetActive(false);
    }

    // ── Power throw ───────────────────────────────────────────────────────────

    public void PlayPowerThrowReady(int playerID)
    {
        ParticleSystem ps = playerID == 1 ? p1PowerReadyParticles : p2PowerReadyParticles;
        if (ps != null && !ps.isPlaying)
            ps.Play();
        AudioManager.Instance?.Play(powerThrowReadyClip, 1f);
    }

    public void StopPowerThrowReady(int playerID)
    {
        ParticleSystem ps = playerID == 1 ? p1PowerReadyParticles : p2PowerReadyParticles;
        ps?.Stop();
    }

    public void PlayPowerThrowLaunch(int playerID)
    {
        ParticleSystem ps = playerID == 1 ? p1PowerLaunchBurst : p2PowerLaunchBurst;
        ps?.Play();
        AudioManager.Instance?.Play(powerThrowLaunchClip, 1.1f);
        CameraShake.Instance?.Shake(0.08f, 0.06f);
    }

    public void PlayNormalThrowLaunch(int playerID)
    {
        ParticleSystem ps = playerID == 1 ? p1NormalLaunchPuff : p2NormalLaunchPuff;
        ps?.Play();
    }

    // ── Killer shot ───────────────────────────────────────────────────────────

    public void PlayKillerShotWarning(int triggeringPlayerID)
    {
        killerShotBurstParticles?.Play();
        AudioManager.Instance?.Play(killerShotWarningClip, 1f);
        FlashOverlay(killerShotWarningOverlay, killerShotFlashColor, killerShotFlashDuration);
    }

    // ── Round end ─────────────────────────────────────────────────────────────

    public void PlayRoundEndFanfare(int winnerID)
    {
        // Activate the winner's pre-placed confetti GO
        GameObject confetti = winnerID == 1 ? p1ConfettiGO : p2ConfettiGO;
        if (confetti != null)
        {
            confetti.SetActive(true);
            // Play all particle systems on the GO and its children
            foreach (var ps in confetti.GetComponentsInChildren<ParticleSystem>())
                ps.Play();
        }

        AudioManager.Instance?.Play(roundEndFanfareClip, 1f);
        FlashOverlay(roundEndFlashOverlay, roundEndFlashColor, roundEndFlashDuration);

        // Show results panel after a short delay so the fanfare lands first
        StartCoroutine(ShowResultsAfterDelay());
    }

    private IEnumerator ShowResultsAfterDelay()
    {
        yield return new WaitForSecondsRealtime(resultsPanelDelay);
        if (resultsPanel != null)
            resultsPanel.SetActive(true);
    }

    // ── Overlay flash ─────────────────────────────────────────────────────────

    private void FlashOverlay(Image overlay, Color color, float duration)
    {
        if (overlay == null)
            return;
        if (overlayCoroutine != null)
            StopCoroutine(overlayCoroutine);
        overlayCoroutine = StartCoroutine(FlashRoutine(overlay, color, duration));
    }

    private IEnumerator FlashRoutine(Image overlay, Color targetColor, float duration)
    {
        overlay.color = targetColor;
        overlay.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            Color c = targetColor;
            c.a = Mathf.Lerp(targetColor.a, 0f, elapsed / duration);
            overlay.color = c;
            yield return null;
        }
        overlay.gameObject.SetActive(false);
        overlayCoroutine = null;
    }
}
