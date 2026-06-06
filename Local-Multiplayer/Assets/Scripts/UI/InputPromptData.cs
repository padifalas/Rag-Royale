using UnityEngine;

/// <summary>
/// ScriptableObject that maps each game action to a keyboard sprite
/// and a gamepad sprite. Create one asset via
/// Assets → Create → Rag Royale → Input Prompt Data
/// and assign all your button images there.
/// Any UI component that needs to show a button prompt just asks this
/// for the right sprite based on the player's device type.
/// </summary>
[CreateAssetMenu(fileName = "InputPromptData",
                 menuName  = "Rag Royale/Input Prompt Data")]
public class InputPromptData : ScriptableObject
{
    [System.Serializable]
    public class ActionPrompt
    {
        [Tooltip("Keyboard key image — e.g. an image of the 'F' key")]
        public Sprite keyboardSprite;

        [Tooltip("Gamepad button image — e.g. an image of the R2 / RT button")]
        public Sprite gamepadSprite;
    }

    [Header("Killer Shot / React")]
    public ActionPrompt react;          // F key  / R2 (PS) / RT (Xbox)

    [Header("Get Up / Mash")]
    public ActionPrompt getUp;          // Enter  / Triangle (PS) / Y (Xbox)

    [Header("Pull String / Block")]
    public ActionPrompt pullString;     // Ctrl   / L2 (PS) / LT (Xbox)

    [Header("Collect / Release (Round 2)")]
    public ActionPrompt collect;        // E      / RT (PS) / RT (Xbox)

    [Header("Throw (Round 3)")]
    public ActionPrompt throwAction;    // Q      / LT (PS) / LT (Xbox)

    [Header("Light Attack")]
    public ActionPrompt lightAttack;    // X key  / Circle (PS) / B (Xbox)

    [Header("Heavy Attack")]
    public ActionPrompt heavyAttack;    // Z key  / Square (PS) / X (Xbox)

    [Header("Jump")]
    public ActionPrompt jump;           // Space  / X (PS) / A (Xbox)

    // -------------------------------------------------------
    // Convenience getter
    // -------------------------------------------------------

    public Sprite GetSprite(ActionPrompt prompt,
                            PlayerInputRegistry.DeviceType device)
    {
        if (prompt == null) return null;
        return device == PlayerInputRegistry.DeviceType.Keyboard
            ? prompt.keyboardSprite
            : prompt.gamepadSprite;
    }

    public Sprite GetSprite(ActionPrompt prompt, int playerID)
    {
        var device = PlayerInputRegistry.Instance != null
            ? PlayerInputRegistry.Instance.GetDeviceType(playerID)
            : PlayerInputRegistry.DeviceType.Keyboard;
        return GetSprite(prompt, device);
    }
}
