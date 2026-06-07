using System;
using System.Collections.Generic;
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

    public static event Action<int, DeviceType> OnPlayerRegistered;

    private DeviceType p1Device = DeviceType.Gamepad;
    private DeviceType p2Device = DeviceType.Gamepad;
    private bool p1Registered = false;
    private bool p2Registered = false;

    // ── New: store actual device + scheme for re-spawning ──────────────────
    private readonly Dictionary<int, InputDevice> _devices = new();
    private readonly Dictionary<int, string> _controlSchemes = new();

    // ───────────────────────────────────────────────────────────────────────

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

        // ── Store device + scheme ──────────────────────────────────────────
        var inputDevice = input.devices.Count > 0 ? input.devices[0] : null;
        if (inputDevice != null)
            _devices[playerID] = inputDevice;
        _controlSchemes[playerID] = input.currentControlScheme ?? "";
        // ───────────────────────────────────────────────────────────────────

        Debug.Log($"[PlayerInputRegistry] P{playerID} registered as {device}");
        OnPlayerRegistered?.Invoke(playerID, device);
    }

    public DeviceType GetDeviceType(int playerID) => playerID == 1 ? p1Device : p2Device;

    public bool IsRegistered(int playerID) => playerID == 1 ? p1Registered : p2Registered;

    public bool IsKeyboard(int playerID) => GetDeviceType(playerID) == DeviceType.Keyboard;

    public bool IsGamepad(int playerID) => GetDeviceType(playerID) == DeviceType.Gamepad;

    public bool BothPlayersRegistered => p1Registered && p2Registered;

    // ── New getters for RoundSpawner ───────────────────────────────────────
    public InputDevice GetDevice(int playerID) =>
        _devices.TryGetValue(playerID, out var d) ? d : null;

    public string GetControlScheme(int playerID) =>
        _controlSchemes.TryGetValue(playerID, out var s) ? s : "";

    // ───────────────────────────────────────────────────────────────────────

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

        foreach (var d in input.devices)
        {
            if (d is Keyboard)
                return DeviceType.Keyboard;
            if (d is Gamepad)
                return DeviceType.Gamepad;
        }
        return DeviceType.Gamepad;
    }
}
