using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class JoinUIManager : MonoBehaviour
{
    [Header("P1 UI")]
    [SerializeField]
    private GameObject p1PromptRoot;

    [SerializeField]
    private GameObject p1PromptText;

    [SerializeField]
    private GameObject p1JoinedText;

    [Header("P2 UI")]
    [SerializeField]
    private GameObject p2PromptRoot;

    [SerializeField]
    private GameObject p2PromptText;

    [SerializeField]
    private GameObject p2JoinedText;

    [Header("Settings")]
    [SerializeField]
    private float hideDelay = 1.5f;

    [Header("References")]
    [SerializeField]
    private CountdownManager countdownManager;

    private bool p1Joined = false;
    private bool p2Joined = false;

    private void Awake()
    {
        // If we're not in Round 1, this component serves no purpose.
        // Disable it immediately in Awake — before any Start() or event
        // subscription runs — so PlayerInputManager join events never
        // reach this script in Round 2 or Round 3.
        if (MatchData.Instance != null && MatchData.Instance.CurrentRound > 1)
        {
            Debug.Log(
                $"[JoinUIManager] Round {MatchData.Instance.CurrentRound} — disabling self. Join UI only runs in Round 1."
            );
            enabled = false;
            return;
        }

        if (MatchData.Instance == null)
        {
            Debug.LogWarning("[JoinUIManager] No MatchData in Awake — assuming Round 1.");
        }
    }

    private void Start()
    {
        // Awake may have disabled us — double-check before doing anything.
        if (!enabled)
            return;

        // Null-guard every UI reference so missing wires don't crash the GO.
        if (p1JoinedText != null)
            p1JoinedText.SetActive(false);
        if (p2JoinedText != null)
            p2JoinedText.SetActive(false);
        if (p1PromptText != null)
            p1PromptText.SetActive(true);
        if (p2PromptText != null)
            p2PromptText.SetActive(true);

        var registry = PlayerInputRegistry.Instance;
        if (registry != null)
        {
            if (registry.IsRegistered(1))
            {
                p1Joined = true;
                ShowJoined(p1PromptText, p1JoinedText);
                Debug.Log("[JoinUIManager] P1 already registered on Start.");
            }
            if (registry.IsRegistered(2))
            {
                p2Joined = true;
                ShowJoined(p2PromptText, p2JoinedText);
                Debug.Log("[JoinUIManager] P2 already registered on Start.");
            }

            if (p1Joined && p2Joined)
                StartCoroutine(HideAllAfterDelay());
        }
    }

    public void OnPlayerJoined(PlayerInput player)
    {
        // Hard guard — this should never fire outside Round 1 now that
        // Awake disables the component, but belt-and-suspenders.
        if (MatchData.Instance != null && MatchData.Instance.CurrentRound > 1)
        {
            Debug.LogWarning(
                $"[JoinUIManager] OnPlayerJoined fired in Round {MatchData.Instance.CurrentRound} — ignoring. Check PlayerInputManager is disabled in this scene."
            );
            return;
        }

        int id = player.playerIndex + 1;
        Debug.Log($"[JoinUIManager] P{id} joined.");

        // Register after one frame so PlayerInput commits its scheme.
        StartCoroutine(RegisterNextFrame(id, player));

        if (id == 1 && !p1Joined)
        {
            p1Joined = true;
            ShowJoined(p1PromptText, p1JoinedText);
        }
        else if (id == 2 && !p2Joined)
        {
            p2Joined = true;
            ShowJoined(p2PromptText, p2JoinedText);
        }

        if (p1Joined && p2Joined)
            StartCoroutine(HideAllAfterDelay());
    }

    private IEnumerator RegisterNextFrame(int id, PlayerInput player)
    {
        yield return null;
        PlayerInputRegistry.Instance?.RegisterPlayer(id, player);
    }

    private void ShowJoined(GameObject promptText, GameObject joinedText)
    {
        if (promptText != null)
            promptText.SetActive(false);
        if (joinedText != null)
            joinedText.SetActive(true);
    }

    private IEnumerator HideAllAfterDelay()
    {
        yield return new WaitForSeconds(hideDelay);

        if (p1PromptRoot != null)
            p1PromptRoot.SetActive(false);
        if (p2PromptRoot != null)
            p2PromptRoot.SetActive(false);

        countdownManager?.StartCountdown();
    }
}
