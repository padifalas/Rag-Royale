using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class RoundSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject playerPrefab;

    [SerializeField]
    private CountdownManager countdownManager;

    private void Start()
    {
        if (MatchData.Instance == null || MatchData.Instance.CurrentRound <= 1)
            return;

        // Disable the PlayerInputManager so it doesn't spawn duplicate players.
        // RoundSpawner will handle spawning using the registered players from PlayerInputRegistry.
        var pim = FindFirstObjectByType<PlayerInputManager>();
        if (pim != null)
        {
            pim.enabled = false;
            Debug.Log("[RoundSpawner] Disabled scene PlayerInputManager.");
        }

        // Spawn immediately for Round 2+ (no need to wait a frame)
        SpawnPlayersForRound();
    }

    private void SpawnPlayersForRound()
    {
        var registry = PlayerInputRegistry.Instance;
        if (registry == null)
        {
            Debug.LogError("[RoundSpawner] No PlayerInputRegistry found.");
            return;
        }

        if (!registry.BothPlayersRegistered)
        {
            Debug.LogError("[RoundSpawner] Registry doesn't have both players.");
            return;
        }

        // Spawn the players with their registered devices and schemes
        SpawnPlayer(1);
        SpawnPlayer(2);

        Debug.Log("[RoundSpawner] Players spawned for round " + MatchData.Instance.CurrentRound);
        // RoundManager.BeginRound() will handle starting the countdown
    }

    private void SpawnPlayer(int playerID)
    {
        var registry = PlayerInputRegistry.Instance;
        InputDevice device = registry.GetDevice(playerID);
        string scheme = registry.GetControlScheme(playerID);

        if (device == null)
        {
            Debug.LogError($"[RoundSpawner] No device stored for P{playerID}.");
            return;
        }

        var pi = PlayerInput.Instantiate(
            playerPrefab,
            playerIndex: playerID - 1,
            controlScheme: scheme,
            pairWithDevice: device
        );

        registry.RegisterPlayer(playerID, pi);

        Debug.Log($"[RoundSpawner] Spawned P{playerID} | scheme={scheme} | device={device.name}");
    }
}
