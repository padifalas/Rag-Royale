using System.Collections;
using System.Xml.XPath;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField]
    private float roundWinDisplayTime = 2.5f;

    [SerializeField]
    private float matchWinDisplayTime = 4f;

    [Header("Next Round")]
    [SerializeField]
    private GameObject resultsPanel;

    [SerializeField]
    private GameObject nextRoundBtn;
    private int pendingRound;

    private void Start()
    {
        var managers = FindObjectsByType<RoundManager>(FindObjectsSortMode.None);

        Debug.Log($"RoundManagers found: {managers.Length}");

        foreach (var rm in managers)
        {
            Debug.Log($"Found RoundManager on object: {rm.gameObject.name}");
        }

        RoundManager roundManager = RoundManager.Instance;

        Debug.Log($"Subscribing to: {roundManager.gameObject.name}");

        roundManager.OnRoundWon.AddListener(OnRoundWon);
        roundManager.OnMatchWon.AddListener(OnMatchWon);

    }


    private void OnRoundWon(int winnerID)
    {
        Debug.Log("ON ROUND WON CALLED");

        resultsPanel.SetActive(true);

        pendingRound = MatchData.Instance.CurrentRound + 1;
    }

    public void OnNextRoundPressed()
    {
        LoadNextRoundScene(pendingRound);
    }

    private void OnMatchWon(int winnerID)
    {
        StartCoroutine(LoadMatchResult(winnerID));
    }

    public void LoadNextRoundScene(int nextRound)
    {
        StartCoroutine(LoadRoundScene(nextRound));
    }

    private IEnumerator LoadRoundScene(int roundNumber)
    {
        if (roundNumber < 1 || roundNumber >= MatchData.RoundScenes.Length)
        {
            yield break;
        }

        LevelManager.Instance.LoadScene(MatchData.RoundScenes[roundNumber]);
    }

    private IEnumerator LoadMatchResult(int winnerID)
    {
        yield return new WaitForSeconds(matchWinDisplayTime);

        SceneManager.LoadScene(MatchData.MainMenuScene);

        // SceneManager.LoadScene(target);

        if (MatchData.Instance != null)
        {
            MatchData.Instance.ResetMatch();
            Destroy(MatchData.Instance.gameObject);
        }
    }
}
