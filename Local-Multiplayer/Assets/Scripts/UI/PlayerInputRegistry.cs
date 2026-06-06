using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputRegistry : MonoBehaviour
{
    public static PlayerInputRegistry Instance { get; private set; }

    public enum DeviceType
    {
        Keyboard,
        Gamepad,
    }

    private DeviceType p1Device = DeviceType.Keyboard;
    private DeviceType p2Device = DeviceType.Gamepad;

    private bool p1Registered = false;
    private bool p2Registered = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    //

    public void RegisterPlayer(int playerID, PlayerInput input)
    {
        DeviceType device = DetectDevice(input);

        if (playerID == 1)
        {
            p1Device = device;
            p1Registered = true;
        }
        else
        {
            p2Device = device;
            p2Registered = true;
        }

        Debug.Log($"[PlayerInputRegistry] P{playerID} registered — {device}");
    }

    public DeviceType GetDeviceType(int playerID) => playerID == 1 ? p1Device : p2Device;

    public bool IsKeyboard(int playerID) => GetDeviceType(playerID) == DeviceType.Keyboard;

    public bool IsGamepad(int playerID) => GetDeviceType(playerID) == DeviceType.Gamepad;

    public bool BothPlayersRegistered => p1Registered && p2Registered;

    private DeviceType DetectDevice(PlayerInput input)
    {
        if (input == null)
            return DeviceType.Keyboard;

        // currentControlScheme is set by PlayerInputManager based on the
        // device the player used to join
        string scheme = input.currentControlScheme ?? "";

        if (scheme.Contains("Keyboard") || scheme.Contains("keyboard"))
            return DeviceType.Keyboard;
        if (
            scheme.Contains("Gamepad")
            || scheme.Contains("gamepad")
            || scheme.Contains("Controller")
            || scheme.Contains("controller")
        )
            return DeviceType.Gamepad;

        // jus in case other font work... fallback: inspect active devices
        foreach (var device in input.devices)
        {
            if (device is Keyboard)
                return DeviceType.Keyboard;
            if (device is Gamepad)
                return DeviceType.Gamepad;
        }

        return DeviceType.Keyboard; // safe default
    }
}
