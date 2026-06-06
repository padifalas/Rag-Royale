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

    private void Awake() => img = GetComponent<Image>();

    private void OnEnable()
    {
        PlayerInputRegistry.OnPlayerRegistered += OnPlayerRegistered;
        TryRefresh(); // catch cases where players were already registered
    }

    private void OnDisable()
    {
        PlayerInputRegistry.OnPlayerRegistered -= OnPlayerRegistered;
    }

    private void OnPlayerRegistered(int registeredID, PlayerInputRegistry.DeviceType device)
    {
        if (registeredID == playerID)
            TryRefresh();
    }

    public void TryRefresh()
    {
        if (img == null || promptData == null)
            return;
        if (PlayerInputRegistry.Instance == null)
            return;
        if (!PlayerInputRegistry.Instance.IsRegistered(playerID))
            return;

        var device = PlayerInputRegistry.Instance.GetDeviceType(playerID);
        Sprite sprite = promptData.GetSprite(GetPrompt(), device);

        if (sprite != null)
        {
            img.sprite = sprite;
            img.enabled = true;
        }
    }

    public void SetPlayer(int id)
    {
        playerID = id;
        TryRefresh();
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
}
