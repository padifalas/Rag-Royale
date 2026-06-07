using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Round2UI : MonoBehaviour
{
    [Header("P1 Needle Panel")]
    [SerializeField]
    private Image p1NeedleIcon;

    [SerializeField]
    private TextMeshProUGUI p1NeedleCount;

    [SerializeField]
    private TextMeshProUGUI p1PlayerLabel;

    [SerializeField]
    private RectTransform p1Panel;

    [Header("P2 Needle Panel")]
    [SerializeField]
    private Image p2NeedleIcon;

    [SerializeField]
    private TextMeshProUGUI p2NeedleCount;

    [SerializeField]
    private TextMeshProUGUI p2PlayerLabel;

    [SerializeField]
    private RectTransform p2Panel;

    [Header("Shared Pile")]
    [SerializeField]
    private Image pileNeedleIcon;

    [SerializeField]
    private TextMeshProUGUI pileNeedleCount;

    [SerializeField]
    private TextMeshProUGUI pileLabel;

    // ── Timer ─────────────────────────────────────────────────────────────────

    [Header("Timer")]
    [SerializeField]
    private TextMeshProUGUI timerText;

    [SerializeField]
    private Image timerFill;

    [SerializeField]
    private float timerDuration = 60f;

    [SerializeField]
    private Color timerNormal = Color.white;

    [SerializeField]
    private Color timerUrgent = new Color(1f, 0.2f, 0.1f);

    [SerializeField]
    private float urgentThreshold = 10f;

    // ── Control Prompts (image-based) ─────────────────────────────────────────

    [Header("P1 Control Prompts")]
    [SerializeField]
    private GameObject p1CollectPrompt; // root — shown when near pile

    [SerializeField]
    private Image p1CollectBtnImage; // InputPromptImage sits here

    [SerializeField]
    private GameObject p1DepositPrompt; // root — shown when near deposit

    [SerializeField]
    private Image p1DepositBtnImage;

    [Header("P2 Control Prompts")]
    [SerializeField]
    private GameObject p2CollectPrompt;

    [SerializeField]
    private Image p2CollectBtnImage;

    [SerializeField]
    private GameObject p2DepositPrompt;

    [SerializeField]
    private Image p2DepositBtnImage;

    [Header("Input Prompt Data")]
    [SerializeField]
    private InputPromptData promptData; // shared ScriptableObject

    // ── Killer Shot Banner ────────────────────────────────────────────────────

    [Header("Killer Shot UI")]
    [SerializeField]
    private GameObject killerShotBanner;

    [SerializeField]
    private TextMeshProUGUI killerShotLabel;

    [SerializeField]
    private string killerShotText = "NEEDLE STEAL!";

    [SerializeField]
    private float bannerPulseSpeed = 2.5f;

    [SerializeField]
    private Color bannerColorA = new Color(1f, 0.85f, 0f);

    [SerializeField]
    private Color bannerColorB = new Color(1f, 0.2f, 0.1f);

    [Header("Killer Shot — Reaction button images")]
    [Tooltip("Shows P1's react button inside the killer shot banner")]
    [SerializeField]
    private Image p1KillerShotBtnImage;

    [Tooltip("Shows P2's react button inside the killer shot banner")]
    [SerializeField]
    private Image p2KillerShotBtnImage;

    [Header("Killer Shot Winner Banner")]
    [SerializeField]
    private GameObject stealResultBanner;

    [SerializeField]
    private TextMeshProUGUI stealResultLabel;

    [SerializeField]
    private float stealResultDuration = 2f;

    [Header("Needle Steal Float Animation")]
    [SerializeField]
    private GameObject floatingNeedlePrefab;

    [SerializeField]
    private RectTransform floatCanvas;

    [SerializeField]
    private int floatNeedleCount = 3;

    [SerializeField]
    private float floatDuration = 0.9f;

    [SerializeField]
    private float floatSpread = 60f;

    [SerializeField]
    private AnimationCurve floatCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [SerializeField]
    private Color floatNeedleColor = new Color(0.4f, 1f, 0.6f, 1f);

    [Header("References")]
    [SerializeField]
    private NeedleManager needleManager;

    [SerializeField]
    private RoundTimer roundTimer;

    [SerializeField]
    private KillerShotManager killerShotManager;

    private Coroutine bannerPulseCoroutine;
    private Coroutine stealResultCoroutine;

    private bool p1NearPile = false;
    private bool p2NearPile = false;
    private bool p1NearDeposit = false;
    private bool p2NearDeposit = false;

    private void Start()
    {
        Debug.Log($"[Round2UI] Registry instance is null: {PlayerInputRegistry.Instance == null}");
        HideAllPrompts();
        HideKillerShotBanner();
        HideStealResult();

        // swap all btnn images to match each player's device
        RefreshAllPromptImages();

        if (needleManager != null)
        {
            needleManager.OnPileCountChanged.AddListener(OnPileChanged);
            needleManager.OnPlayerNeedleCountChanged.AddListener(OnPlayerNeedleChanged);
            needleManager.OnNeedleStolen.AddListener(OnNeedleStolen);
            needleManager.OnKillerShotConditionMet.AddListener(_ => ShowKillerShotBanner());
        }

        if (roundTimer != null)
        {
            roundTimer.OnTimerTick.AddListener(OnTimerTick);
            roundTimer.OnTimerExpired.AddListener(OnTimerExpired);
        }

        if (killerShotManager != null)
        {
            killerShotManager.OnKillerShotPhaseEnded.AddListener(HideKillerShotBanner);
            killerShotManager.OnKillerShotExpired.AddListener(HideKillerShotBanner);
        }

        SubscribeToZones();

        UpdateNeedleDisplay(p1NeedleCount, 0);
        UpdateNeedleDisplay(p2NeedleCount, 0);
        UpdateNeedleDisplay(pileNeedleCount, needleManager != null ? needleManager.PileCount : 0);
        UpdateTimer(timerDuration);

        PlayerInputRegistry.OnPlayerRegistered += OnPlayerRegistered;
    }

    private void OnDestroy()
    {
        if (needleManager != null)
        {
            needleManager.OnPileCountChanged.RemoveListener(OnPileChanged);
            needleManager.OnPlayerNeedleCountChanged.RemoveListener(OnPlayerNeedleChanged);
            needleManager.OnNeedleStolen.RemoveListener(OnNeedleStolen);
        }
    }

    private void RefreshAllPromptImages()
    {
        if (promptData == null)
            return;

        SetPromptImage(p1CollectBtnImage, promptData.collect, 1);
        SetPromptImage(p1DepositBtnImage, promptData.collect, 1); // same key as collect
        SetPromptImage(p2CollectBtnImage, promptData.collect, 2);
        SetPromptImage(p2DepositBtnImage, promptData.collect, 2);
        SetPromptImage(p1KillerShotBtnImage, promptData.react, 1);
        SetPromptImage(p2KillerShotBtnImage, promptData.react, 2);
    }

    private void SetPromptImage(Image img, InputPromptData.ActionPrompt prompt, int playerID)
    {
        if (img == null || prompt == null || promptData == null)
            return;

        var registry = PlayerInputRegistry.Instance;
        if (registry == null)
        {
            Debug.LogWarning($"[Round2UI] Registry is null when setting P{playerID} image");
            return;
        }

        var device = registry.GetDeviceType(playerID);
        Sprite sprite = promptData.GetSprite(prompt, device);

        // Debug.Log($"[Round2UI] P{playerID} device={device} sprite={sprite?.name ?? "NULL"}");

        if (sprite != null)
        {
            img.sprite = sprite;
            img.enabled = true;
        }
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

    private void OnPlayerRegistered(int playerID, PlayerInputRegistry.DeviceType device)
    {
        RefreshAllPromptImages();
    }

    private void SubscribeToZones()
    {
        var collectZones = FindObjectsByType<CollectZone>(FindObjectsSortMode.None);
        foreach (var z in collectZones)
        {
            z.OnPlayerEnter += id => SetNearPile(id, true);
            z.OnPlayerExit += id => SetNearPile(id, false);
        }

        var depositZones = FindObjectsByType<DepositZone>(FindObjectsSortMode.None);
        foreach (var z in depositZones)
        {
            z.OnPlayerEnter += id => SetNearDeposit(id, true);
            z.OnPlayerExit += id => SetNearDeposit(id, false);
        }
    }

    private void HideAllPrompts()
    {
        SetActive(p1CollectPrompt, false);
        SetActive(p1DepositPrompt, false);
        SetActive(p2CollectPrompt, false);
        SetActive(p2DepositPrompt, false);
    }

    private void SetNearPile(int id, bool near)
    {
        if (id == 1)
            p1NearPile = near;
        else
            p2NearPile = near;
        RefreshPrompts();
    }

    private void SetNearDeposit(int id, bool near)
    {
        if (id == 1)
            p1NearDeposit = near;
        else
            p2NearDeposit = near;
        RefreshPrompts();
    }

    private void RefreshPrompts()
    {
        bool showP1Collect = p1NearPile && !p1NearDeposit;
        bool showP1Deposit = p1NearDeposit && !p1NearPile;
        bool showP2Collect = p2NearPile && !p2NearDeposit;
        bool showP2Deposit = p2NearDeposit && !p2NearPile;

        SetActive(p1CollectPrompt, showP1Collect);
        SetActive(p1DepositPrompt, showP1Deposit);
        SetActive(p2CollectPrompt, showP2Collect);
        SetActive(p2DepositPrompt, showP2Deposit);

        // Refresh images every time visibility changes
        if (showP1Collect)
            SetPromptImage(p1CollectBtnImage, promptData?.collect, 1);
        if (showP1Deposit)
            SetPromptImage(p1DepositBtnImage, promptData?.collect, 1);
        if (showP2Collect)
            SetPromptImage(p2CollectBtnImage, promptData?.collect, 2);
        if (showP2Deposit)
            SetPromptImage(p2DepositBtnImage, promptData?.collect, 2);
    }

    private void OnPileChanged(int count) => UpdateNeedleDisplay(pileNeedleCount, count);

    private void OnPlayerNeedleChanged(int playerID, int count)
    {
        UpdateNeedleDisplay(playerID == 1 ? p1NeedleCount : p2NeedleCount, count);
        PulsePanel(playerID == 1 ? p1Panel : p2Panel);
    }

    private void UpdateNeedleDisplay(TextMeshProUGUI text, int count)
    {
        if (text != null)
            text.text = count.ToString();
    }

    private void PulsePanel(RectTransform panel)
    {
        if (panel != null)
            StartCoroutine(PanelPop(panel));
    }

    private IEnumerator PanelPop(RectTransform panel)
    {
        Vector3 orig = panel.localScale;
        Vector3 big = orig * 1.15f;
        float dur = 0.1f;
        float t = 0f;

        while (t < dur)
        {
            t += Time.deltaTime;
            panel.localScale = Vector3.Lerp(orig, big, t / dur);
            yield return null;
        }
        t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            panel.localScale = Vector3.Lerp(big, orig, t / dur);
            yield return null;
        }
        panel.localScale = orig;
    }

    private void OnTimerTick(float remaining) => UpdateTimer(remaining);

    private void OnTimerExpired() => UpdateTimer(0f);

    private void UpdateTimer(float remaining)
    {
        if (timerText == null)
            return;
        int minutes = Mathf.FloorToInt(remaining / 60f);
        int seconds = Mathf.FloorToInt(remaining % 60f);
        timerText.text = $"{minutes:0}:{seconds:00}";
        timerText.color = remaining <= urgentThreshold ? timerUrgent : timerNormal;
        if (timerFill != null)
            timerFill.fillAmount = Mathf.Clamp01(remaining / timerDuration);
    }

    private void ShowKillerShotBanner()
    {
        SetActive(killerShotBanner, true);
        if (killerShotLabel != null)
            killerShotLabel.text = killerShotText;

        SetPromptImage(p1KillerShotBtnImage, promptData?.react, 1);
        SetPromptImage(p2KillerShotBtnImage, promptData?.react, 2);

        if (bannerPulseCoroutine != null)
            StopCoroutine(bannerPulseCoroutine);
        bannerPulseCoroutine = StartCoroutine(PulseBanner());
    }

    private void HideKillerShotBanner()
    {
        if (bannerPulseCoroutine != null)
        {
            StopCoroutine(bannerPulseCoroutine);
            bannerPulseCoroutine = null;
        }
        SetActive(killerShotBanner, false);
    }

    private IEnumerator PulseBanner()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * bannerPulseSpeed;
            if (killerShotLabel != null)
                killerShotLabel.color = Color.Lerp(
                    bannerColorA,
                    bannerColorB,
                    (Mathf.Sin(t * Mathf.PI * 2f) + 1f) * 0.5f
                );
            yield return null;
        }
    }

    private void OnNeedleStolen(int winnerID)
    {
        HideKillerShotBanner();

        RectTransform src = winnerID == 1 ? p2Panel : p1Panel;
        RectTransform dst = winnerID == 1 ? p1Panel : p2Panel;

        int stolen = needleManager != null ? needleManager.GetStealAmount() : 3;
        ShowStealResult($"PLAYER {winnerID} STEALS {stolen} NEEDLES!");

        if (floatingNeedlePrefab != null && floatCanvas != null && src != null && dst != null)
            StartCoroutine(AnimateFloatingNeedles(src, dst, stolen));
    }

    private void ShowStealResult(string message)
    {
        SetActive(stealResultBanner, true);
        if (stealResultLabel != null)
            stealResultLabel.text = message;
        if (stealResultCoroutine != null)
            StopCoroutine(stealResultCoroutine);
        stealResultCoroutine = StartCoroutine(FadeOutStealResult());
    }

    private void HideStealResult() => SetActive(stealResultBanner, false);

    private IEnumerator FadeOutStealResult()
    {
        yield return new WaitForSeconds(stealResultDuration - 0.4f);
        float t = 0f;
        CanvasGroup cg =
            stealResultBanner != null ? stealResultBanner.GetComponent<CanvasGroup>() : null;
        if (cg != null)
        {
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, t / 0.4f);
                yield return null;
            }
            cg.alpha = 1f;
        }
        SetActive(stealResultBanner, false);
    }

    private IEnumerator AnimateFloatingNeedles(RectTransform src, RectTransform dst, int count)
    {
        int spawnCount = Mathf.Min(count, floatNeedleCount);
        for (int i = 0; i < spawnCount; i++)
        {
            StartCoroutine(FloatOnNeedle(src, dst));
            yield return new WaitForSeconds(0.12f);
        }
    }

    private IEnumerator FloatOnNeedle(RectTransform src, RectTransform dst)
    {
        GameObject needle = Instantiate(floatingNeedlePrefab, floatCanvas);
        RectTransform rt = needle.GetComponent<RectTransform>();
        Image img = needle.GetComponent<Image>();

        if (img != null)
            img.color = floatNeedleColor;

        Vector2 startPos = GetCanvasPos(src) + Random.insideUnitCircle * floatSpread;
        Vector2 endPos = GetCanvasPos(dst);
        Vector2 midPoint = (startPos + endPos) * 0.5f + Vector2.up * 120f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / floatDuration;
            float curved = floatCurve.Evaluate(t);

            rt.anchoredPosition =
                Mathf.Pow(1 - curved, 2) * startPos
                + 2f * (1 - curved) * curved * midPoint
                + Mathf.Pow(curved, 2) * endPos;

            if (img != null)
            {
                Color c = img.color;
                c.a = t > 0.8f ? Mathf.Lerp(1f, 0f, (t - 0.8f) / 0.2f) : 1f;
                img.color = c;
            }

            rt.localRotation = Quaternion.Euler(0f, 0f, curved * 360f);
            yield return null;
        }

        Destroy(needle);
    }

    private Vector2 GetCanvasPos(RectTransform panel)
    {
        Camera cam = Camera.main;
        Vector2 screenPos;
        Canvas canvas = floatCanvas.GetComponentInParent<Canvas>();

        screenPos =
            canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? RectTransformUtility.WorldToScreenPoint(null, panel.position)
                : RectTransformUtility.WorldToScreenPoint(cam, panel.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            floatCanvas,
            screenPos,
            cam,
            out Vector2 local
        );
        return local;
    }

    private void SetActive(GameObject go, bool state)
    {
        if (go != null)
            go.SetActive(state);
    }
}
