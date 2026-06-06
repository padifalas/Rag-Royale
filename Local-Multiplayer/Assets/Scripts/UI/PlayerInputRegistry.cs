using System;
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

    // Fires (playerID, deviceType) the moment a player registers
    public static event Action<int, DeviceType> OnPlayerRegistered;

    private DeviceType p1Device = DeviceType.Gamepad;
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

    public void RegisterPlayer(int playerID, PlayerInput input)
    {
        Debug.Log(
            $"[PlayerInputRegistry] P{playerID} scheme='{input.currentControlScheme}' devices={string.Join(",", input.devices)}"
        );

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

        Debug.Log($"[PlayerInputRegistry] P{playerID} registered ... {device}");

        // Fire immediately so UI components refresh right now
        OnPlayerRegistered?.Invoke(playerID, device);
    }

    public DeviceType GetDeviceType(int playerID) => playerID == 1 ? p1Device : p2Device;

    public bool IsRegistered(int playerID) => playerID == 1 ? p1Registered : p2Registered;

    public bool IsKeyboard(int playerID) => GetDeviceType(playerID) == DeviceType.Keyboard;

    public bool IsGamepad(int playerID) => GetDeviceType(playerID) == DeviceType.Gamepad;

    public bool BothPlayersRegistered => p1Registered && p2Registered;

    private DeviceType DetectDevice(PlayerInput input)
    {
        if (input == null)
            return DeviceType.Gamepad;

        string scheme = input.currentControlScheme ?? "";
        if (scheme.Contains("Keyboard", StringComparison.OrdinalIgnoreCase))
            return DeviceType.Keyboard;
        if (
            scheme.Contains("Gamepad", StringComparison.OrdinalIgnoreCase)
            || scheme.Contains("Controller", StringComparison.OrdinalIgnoreCase)
        )
            return DeviceType.Gamepad;

        foreach (var device in input.devices)
        {
            if (device is Keyboard)
                return DeviceType.Keyboard;
            if (device is Gamepad)
                return DeviceType.Gamepad;
        }

        return DeviceType.Gamepad;
    }
}
