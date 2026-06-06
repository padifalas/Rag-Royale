using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class InputPromptImage : MonoBehaviour
{
    public enum PromptAction
    {
        React,
        GetUp,
        PullString,
        Collect,
        Throw,
        LightAttack,
        HeavyAttack,
        Jump,
    }

    [Header("Which button to show")]
    [SerializeField]
    private PromptAction action = PromptAction.React;

    [SerializeField]
    private int playerID = 1;

    [Header("Data")]
    [SerializeField]
    private InputPromptData promptData;

    private Image img;
    private Sprite fallbackSprite;

    private void Awake()
    {
        img = GetComponent<Image>();
        fallbackSprite = img.sprite;
    }

    private void Start()
    {
        if (!TryRefresh())
            StartCoroutine(WaitAndRefresh());
    }

    private void OnEnable()
    {
        if (!TryRefresh())
            StartCoroutine(WaitAndRefresh());
    }

    // -------------------------------------------------------

    public bool TryRefresh()
    {
        if (img == null || promptData == null)
            return false;

        if (PlayerInputRegistry.Instance == null)
            return false;

        var deviceType = PlayerInputRegistry.Instance.GetDeviceType(playerID);
        InputPromptData.ActionPrompt prompt = GetPrompt();
        if (prompt == null)
            return false;

        Sprite sprite = promptData.GetSprite(prompt, deviceType);

        if (sprite != null)
        {
            img.sprite = sprite;
            img.enabled = true;
            return true;
        }

        if (fallbackSprite != null)
            img.sprite = fallbackSprite;

        return false;
    }

    public void Refresh() => TryRefresh();

    private IEnumerator WaitAndRefresh()
    {
        float elapsed = 0f;
        while (elapsed < 10f)
        {
            yield return null;
            elapsed += Time.deltaTime;

            if (TryRefresh())
                yield break;
        }

        Debug.LogWarning($"[InputPromptImage] P{playerID} device never registered");
    }



    private InputPromptData.ActionPrompt GetPrompt() =>
        action switch
        {
            PromptAction.React => promptData.react,
            PromptAction.GetUp => promptData.getUp,
            PromptAction.PullString => promptData.pullString,
            PromptAction.Collect => promptData.collect,
            PromptAction.Throw => promptData.throwAction,
            PromptAction.LightAttack => promptData.lightAttack,
            PromptAction.HeavyAttack => promptData.heavyAttack,
            PromptAction.Jump => promptData.jump,
            _ => null,
        };

    public void SetPlayer(int id)
    {
        playerID = id;
        TryRefresh();
    }
}
