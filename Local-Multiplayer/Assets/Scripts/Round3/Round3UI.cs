using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Round3UI : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private ThrowSystem throwSystem;

    [SerializeField]
    private KillerShotManager killerShotManager;

    [Header("Input Prompt Data")]
    [SerializeField]
    private InputPromptData promptData;

    [Header("Throw Button Images")]
    [SerializeField]
    private Image p1ThrowBtnImage;

    [SerializeField]
    private Image p2ThrowBtnImage;

    [Header("Killer Shot React Button Images")]
    [SerializeField]
    private Image p1ReactBtnImage;

    [SerializeField]
    private Image p2ReactBtnImage;

    [Header("Round Timer")]
    [SerializeField]
    private TextMeshProUGUI roundTimerText; // wire Timer-Text here

    [SerializeField]
    private Color timerNormalColor = Color.white;

    [SerializeField]
    private Color timerUrgentColor = new Color(1f, 0.2f, 0.1f);

    [SerializeField]
    private float timerUrgentThreshold = 10f;

    [Header("Ammo — P1")]
    [SerializeField]
    private TextMeshProUGUI p1AmmoLabel;

    [SerializeField]
    private Transform p1AmmoPipRoot;

    [SerializeField]
    private GameObject ammoPipPrefab;

    [Header("Ammo — P2")]
    [SerializeField]
    private TextMeshProUGUI p2AmmoLabel;

    [SerializeField]
    private Transform p2AmmoPipRoot;

    [Header("HP Bars")]
    [SerializeField]
    private Slider p1HpSlider;

    [SerializeField]
    private Slider p2HpSlider;

    [SerializeField]
    private Image p1HpFill;

    [SerializeField]
    private Image p2HpFill;

    [SerializeField]
    private Color hpHealthyColor = new Color(0.2f, 0.85f, 0.3f);

    [SerializeField]
    private Color hpDangerColor = new Color(0.95f, 0.2f, 0.15f);

    [SerializeField, Range(0f, 1f)]
    private float hpDangerThreshold = 0.35f;

    [Header("Power Throw Indicators")]
    [SerializeField]
    private GameObject p1PowerThrowPanel;

    [SerializeField]
    private GameObject p2PowerThrowPanel;

    [SerializeField]
    private float powerPulseSpeed = 3f;

    [Header("Hit Flash Overlays")]
    [SerializeField]
    private Image p1HitFlash;

    [SerializeField]
    private Image p2HitFlash;

    [SerializeField]
    private Color normalHitFlashColor = new Color(1f, 0.1f, 0.1f, 0.45f);

    [SerializeField]
    private Color powerHitFlashColor = new Color(1f, 0.5f, 0f, 0.70f);

    [SerializeField]
    private float hitFlashDuration = 0.25f;

    [Header("Killer Shot HUD")]
    [SerializeField]
    private GameObject killerShotPanel;

    [SerializeField]
    private Slider killerShotTimerSlider;

    [SerializeField]
    private TextMeshProUGUI killerShotLabel;

    [SerializeField]
    private Image killerShotFill;

    [SerializeField]
    private Color killerShotReadyColor = new Color(1f, 0.85f, 0f);

    [SerializeField]
    private Color killerShotUrgentColor = new Color(1f, 0.15f, 0.1f);

    [Header("Exhausted Banners")]
    [SerializeField]
    private GameObject p1ExhaustedBanner;

    [SerializeField]
    private GameObject p2ExhaustedBanner;

    // ── Private state ─────────────────────────────────────────────────────────

    private RoundTimer roundTimer;
    private PlayerHealth p1Health;
    private PlayerHealth p2Health;
    private bool playersResolved = false;
    private Coroutine p1FlashCoroutine;
    private Coroutine p2FlashCoroutine;
    private bool killerShotPhaseActive = false;
    private Image[] p1Pips;
    private Image[] p2Pips;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        SetActive(killerShotPanel, false);
        SetActive(p1PowerThrowPanel, false);
        SetActive(p2PowerThrowPanel, false);
        SetActive(p1ExhaustedBanner, false);
        SetActive(p2ExhaustedBanner, false);
        SetAlpha(p1HitFlash, 0f);
        SetAlpha(p2HitFlash, 0f);

        PlayerInputRegistry.OnPlayerRegistered += OnPlayerRegistered;
        RefreshAllPromptImages();

        // ── ThrowSystem ───────────────────────────────────────────────────────
        if (throwSystem != null)
        {
            throwSystem.OnAmmoChanged.AddListener(OnAmmoChanged);
            throwSystem.OnPowerThrowReady.AddListener(OnPowerThrowReady);
            throwSystem.OnPowerThrowUsed.AddListener(OnPowerThrowUsed);
            throwSystem.OnNeedlesExhausted.AddListener(OnPlayerExhausted);
        }

        // ── KillerShotManager ─────────────────────────────────────────────────
        if (killerShotManager != null)
        {
            killerShotManager.OnKillerShotPhaseStarted.AddListener(OnKillerShotStarted);
            killerShotManager.OnKillerShotPhaseEnded.AddListener(OnKillerShotEnded);
            killerShotManager.OnKillerShotWinner.AddListener(OnKillerShotWinner);
            killerShotManager.OnEarlyPress.AddListener(OnEarlyPress);
        }

        // ── RoundTimer — FIX: subscribe here in Start, not in OnDestroy ───────
        roundTimer = FindFirstObjectByType<RoundTimer>();
        if (roundTimer != null)
        {
            roundTimer.OnTimerTick.AddListener(OnTimerTick);
            roundTimer.OnTimerExpired.AddListener(OnTimerExpired);
            Debug.Log("[Round3UI] Subscribed to RoundTimer.");
        }
        else
            Debug.LogWarning("[Round3UI] RoundTimer not found — timer text will not update.");

        NeedleProjectile.OnProjectileHit += OnProjectileHit;
        StartCoroutine(InitAmmoNextFrame());
    }

    private void OnEnable()
    {
        PlayerInputRegistry.OnPlayerRegistered += OnPlayerRegistered;
        RefreshAllPromptImages();
    }

    private void OnDisable()
    {
        PlayerInputRegistry.OnPlayerRegistered -= OnPlayerRegistered;
    }

    private void OnDestroy()
    {
        // ── Unsubscribe everything cleanly ────────────────────────────────────
        NeedleProjectile.OnProjectileHit -= OnProjectileHit;
        PlayerInputRegistry.OnPlayerRegistered -= OnPlayerRegistered;

        if (throwSystem != null)
        {
            throwSystem.OnAmmoChanged.RemoveListener(OnAmmoChanged);
            throwSystem.OnPowerThrowReady.RemoveListener(OnPowerThrowReady);
            throwSystem.OnPowerThrowUsed.RemoveListener(OnPowerThrowUsed);
            throwSystem.OnNeedlesExhausted.RemoveListener(OnPlayerExhausted);
        }

        if (killerShotManager != null)
        {
            killerShotManager.OnKillerShotPhaseStarted.RemoveListener(OnKillerShotStarted);
            killerShotManager.OnKillerShotPhaseEnded.RemoveListener(OnKillerShotEnded);
            killerShotManager.OnKillerShotWinner.RemoveListener(OnKillerShotWinner);
            killerShotManager.OnEarlyPress.RemoveListener(OnEarlyPress);
        }

        // RoundTimer unsubscribe — only if we actually subscribed in Start
        if (roundTimer != null)
        {
            roundTimer.OnTimerTick.RemoveListener(OnTimerTick);
            roundTimer.OnTimerExpired.RemoveListener(OnTimerExpired);
        }
    }

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!playersResolved)
            TryResolveHealth();
        if (playersResolved)
            UpdateHpBars();

        PulsePowerIndicator(p1PowerThrowPanel);
        PulsePowerIndicator(p2PowerThrowPanel);

        if (killerShotPhaseActive && killerShotManager != null && killerShotTimerSlider != null)
        {
            float t = Mathf.Clamp01(
                killerShotManager.GetWindowTimeRemaining() / killerShotManager.GetWindowDuration()
            );
            killerShotTimerSlider.value = t;
            if (killerShotFill != null)
                killerShotFill.color = Color.Lerp(killerShotUrgentColor, killerShotReadyColor, t);
        }
    }

    // ── Timer ─────────────────────────────────────────────────────────────────

    private void OnTimerTick(float remaining)
    {
        if (roundTimerText == null)
            return;
        int minutes = Mathf.FloorToInt(remaining / 60f);
        int seconds = Mathf.FloorToInt(remaining % 60f);
        roundTimerText.text = $"{minutes}:{seconds:00}";
        roundTimerText.color =
            remaining <= timerUrgentThreshold ? timerUrgentColor : timerNormalColor;
    }

    private void OnTimerExpired()
    {
        if (roundTimerText == null)
            return;
        roundTimerText.text = "0:00";
        roundTimerText.color = timerUrgentColor;
    }

    // ── Player health resolution ───────────────────────────────────────────────

    private void TryResolveHealth()
    {
        var controllers = FindObjectsByType<MultiplayerPlayerController>(FindObjectsSortMode.None);
        foreach (var c in controllers)
        {
            if (c.PlayerID == 1 && p1Health == null)
                p1Health = c.GetComponent<PlayerHealth>();
            else if (c.PlayerID == 2 && p2Health == null)
                p2Health = c.GetComponent<PlayerHealth>();
        }
        if (p1Health != null && p2Health != null)
            playersResolved = true;
    }

    private void UpdateHpBars()
    {
        UpdateSingleHpBar(p1HpSlider, p1HpFill, p1Health);
        UpdateSingleHpBar(p2HpSlider, p2HpFill, p2Health);
    }

    private void UpdateSingleHpBar(Slider slider, Image fill, PlayerHealth health)
    {
        if (slider == null || health == null)
            return;
        float ratio = Mathf.Clamp01(health.CurrentHealth / health.MaxHealth);
        slider.value = ratio;
        if (fill != null)
            fill.color = Color.Lerp(
                hpDangerColor,
                hpHealthyColor,
                ratio > hpDangerThreshold ? 1f : ratio / hpDangerThreshold
            );
    }

    // ── Ammo ──────────────────────────────────────────────────────────────────

    private IEnumerator InitAmmoNextFrame()
    {
        yield return null;
        if (throwSystem == null)
            yield break;

        BuildPips(p1AmmoPipRoot, throwSystem.P1Ammo, out p1Pips);
        BuildPips(p2AmmoPipRoot, throwSystem.P2Ammo, out p2Pips);
        RefreshAmmoLabel(1, throwSystem.P1Ammo);
        RefreshAmmoLabel(2, throwSystem.P2Ammo);
    }

    private void BuildPips(Transform root, int count, out Image[] pips)
    {
        pips = new Image[0];
        if (root == null || ammoPipPrefab == null)
            return;
        foreach (Transform child in root)
            Destroy(child.gameObject);
        pips = new Image[count];
        for (int i = 0; i < count; i++)
            pips[i] = Instantiate(ammoPipPrefab, root).GetComponent<Image>();
    }

    private void OnAmmoChanged(int playerID, int newAmmo)
    {
        RefreshAmmoLabel(playerID, newAmmo);
        RefreshPips(playerID, newAmmo);
    }

    private void RefreshAmmoLabel(int playerID, int ammo)
    {
        var label = playerID == 1 ? p1AmmoLabel : p2AmmoLabel;
        if (label != null)
            label.text = ammo.ToString();
    }

    private void RefreshPips(int playerID, int ammo)
    {
        Image[] pips = playerID == 1 ? p1Pips : p2Pips;
        if (pips == null)
            return;
        for (int i = 0; i < pips.Length; i++)
        {
            if (pips[i] == null)
                continue;
            pips[i].color = i < ammo ? Color.white : new Color(0.3f, 0.3f, 0.3f, 0.4f);
        }
    }

    // ── Hit flash ─────────────────────────────────────────────────────────────

    private void OnProjectileHit(int hitPlayerID, bool isPower)
    {
        Image overlay = hitPlayerID == 1 ? p1HitFlash : p2HitFlash;
        Color color = isPower ? powerHitFlashColor : normalHitFlashColor;
        ref Coroutine r = ref (hitPlayerID == 1 ? ref p1FlashCoroutine : ref p2FlashCoroutine);
        if (overlay == null)
            return;
        if (r != null)
            StopCoroutine(r);
        r = StartCoroutine(HitFlashRoutine(overlay, color));
    }

    private IEnumerator HitFlashRoutine(Image overlay, Color color)
    {
        overlay.color = color;
        overlay.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < hitFlashDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            Color c = color;
            c.a = Mathf.Lerp(
                color.a,
                0f,
                (elapsed / hitFlashDuration) * (elapsed / hitFlashDuration)
            );
            overlay.color = c;
            yield return null;
        }
        overlay.gameObject.SetActive(false);
    }

    // ── Power throw ───────────────────────────────────────────────────────────

    private void OnPowerThrowReady(int playerID)
    {
        SetActive(playerID == 1 ? p1PowerThrowPanel : p2PowerThrowPanel, true);
        if (playerID == 1)
            SetPromptImage(p1ThrowBtnImage, promptData?.throwAction, 1);
        else
            SetPromptImage(p2ThrowBtnImage, promptData?.throwAction, 2);
    }

    private void OnPowerThrowUsed(int playerID) =>
        SetActive(playerID == 1 ? p1PowerThrowPanel : p2PowerThrowPanel, false);

    private void PulsePowerIndicator(GameObject panel)
    {
        if (panel == null || !panel.activeSelf)
            return;
        float s = 1f + Mathf.Sin(Time.unscaledTime * powerPulseSpeed) * 0.08f;
        panel.transform.localScale = Vector3.one * s;
    }

    private void OnPlayerExhausted(int playerID) =>
        SetActive(playerID == 1 ? p1ExhaustedBanner : p2ExhaustedBanner, true);

    // ── Killer Shot ───────────────────────────────────────────────────────────

    private void OnKillerShotStarted(int triggeringPlayerID)
    {
        killerShotPhaseActive = true;
        SetActive(killerShotPanel, true);
        if (killerShotTimerSlider != null)
            killerShotTimerSlider.value = 1f;
        if (killerShotLabel != null)
            killerShotLabel.text =
                triggeringPlayerID == 0 ? "KILLER SHOT!" : $"P{triggeringPlayerID} KILLER SHOT!";
        SetPromptImage(p1ReactBtnImage, promptData?.react, 1);
        SetPromptImage(p2ReactBtnImage, promptData?.react, 2);
    }

    private void OnKillerShotEnded()
    {
        killerShotPhaseActive = false;
        SetActive(killerShotPanel, false);
    }

    private void OnKillerShotWinner(int winnerID) =>
        Debug.Log($"[Round3UI] P{winnerID} won killer shot — power throw incoming.");

    private void OnEarlyPress(int playerID) => Debug.Log($"[Round3UI] P{playerID} pressed EARLY.");

    // ── Input prompts ─────────────────────────────────────────────────────────

    private void RefreshAllPromptImages()
    {
        if (promptData == null)
            return;
        SetPromptImage(p1ThrowBtnImage, promptData.throwAction, 1);
        SetPromptImage(p2ThrowBtnImage, promptData.throwAction, 2);
        SetPromptImage(p1ReactBtnImage, promptData.react, 1);
        SetPromptImage(p2ReactBtnImage, promptData.react, 2);
    }

    private void SetPromptImage(Image img, InputPromptData.ActionPrompt prompt, int playerID)
    {
        if (img == null || prompt == null || promptData == null)
            return;
        var registry = PlayerInputRegistry.Instance;
        var device =
            registry != null
                ? registry.GetDeviceType(playerID)
                : PlayerInputRegistry.DeviceType.Keyboard;
        Sprite sprite = promptData.GetSprite(prompt, device);
        if (sprite != null)
        {
            img.sprite = sprite;
            img.enabled = true;
        }
    }

    private void OnPlayerRegistered(int playerID, PlayerInputRegistry.DeviceType device) =>
        RefreshAllPromptImages();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void SetActive(GameObject go, bool active)
    {
        if (go != null)
            go.SetActive(active);
    }

    private static void SetAlpha(Image img, float alpha)
    {
        if (img == null)
            return;
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }
}
