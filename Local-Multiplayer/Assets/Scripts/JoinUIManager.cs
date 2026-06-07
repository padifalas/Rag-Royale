using System.Collections;
using TMPro;
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

    private void Start()
    {
        p1JoinedText.gameObject.SetActive(false);
        p2JoinedText.gameObject.SetActive(false);

        p1PromptText.gameObject.SetActive(true);
        p2PromptText.gameObject.SetActive(true);
        // If players were already registered (persisting across scenes), reflect that state
        var registry = PlayerInputRegistry.Instance;
        if (registry != null)
        {
            if (registry.IsRegistered(1))
            {
                p1Joined = true;
                ShowJoined(p1PromptText, p1JoinedText);
            }
            if (registry.IsRegistered(2))
            {
                p2Joined = true;
                ShowJoined(p2PromptText, p2JoinedText);
            }

            if (p1Joined && p2Joined)
                StartCoroutine(HideAllAfterDelay());
        }
    }

    public void OnPlayerJoined(PlayerInput player)
    {
        // Guard: only handle joins in Round 1
        if (MatchData.Instance != null && MatchData.Instance.CurrentRound > 1)
        {
            Debug.Log("[JoinUIManager] Ignoring join event — not Round 1.");
            return;
        }

        // Debug.Log($"[JoinUIManager] OnPlayerJoined fired — playerIndex={player.playerIndex}");

        int id = player.playerIndex + 1;
        StartCoroutine(RegisterNextFrame(id, player));

        PlayerInputRegistry.Instance?.RegisterPlayer(id, player);

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
        yield return null; // wait one frame for scheme to commit
        PlayerInputRegistry.Instance?.RegisterPlayer(id, player);
    }

    private void ShowJoined(GameObject promptText, GameObject joinedText)
    {
        promptText.gameObject.SetActive(false);
        joinedText.gameObject.SetActive(true);
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
