using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player re-spawning in Round 2+ scenes where PlayerInputManager
/// is not used. Reads stored device/scheme from PlayerInputRegistry and
/// calls PlayerInput.Instantiate to recreate each player's GameObject with
/// full input binding intact.
///
/// Place on a persistent scene object alongside CountdownManager.
/// Wire playerPrefab to the same prefab used by PlayerInputManager in Round 1.
/// </summary>
public class RoundSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject playerPrefab;

    [SerializeField]
    private CountdownManager countdownManager;

    // How many frames to wait for PlayerInputRegistry to be populated
    // before giving up. Covers editor-only "start from Round 2" workflows.
    [SerializeField]
    private int registryWaitFrames = 60;

    private void Start()
    {
        // Only act in Round 2+. Round 1 uses PlayerInputManager directly.
        if (MatchData.Instance == null || MatchData.Instance.CurrentRound <= 1)
            return;

        // Disable the scene's PlayerInputManager so it can't spawn duplicates
        // while RoundSpawner is doing its own managed instantiation.
        var pim = FindFirstObjectByType<PlayerInputManager>();
        if (pim != null)
        {
            pim.enabled = false;
            Debug.Log("[RoundSpawner] Disabled scene PlayerInputManager.");
        }

        StartCoroutine(SpawnThenCountdown());
    }

    private IEnumerator SpawnThenCountdown()
    {
        // Wait until both players are registered, up to registryWaitFrames.
        // This handles the rare case where MatchData/Registry haven't finished
        // DontDestroyOnLoad initialisation by the time Start() fires.
        var registry = PlayerInputRegistry.Instance;
        int waited = 0;
        while ((registry == null || !registry.BothPlayersRegistered) && waited < registryWaitFrames)
        {
            yield return null;
            registry = PlayerInputRegistry.Instance;
            waited++;
        }

        if (registry == null || !registry.BothPlayersRegistered)
        {
            Debug.LogError(
                "[RoundSpawner] PlayerInputRegistry not ready after waiting "
                    + registryWaitFrames
                    + " frames. "
                    + "If running from Round 2/3 directly in the editor, "
                    + "start from Round 1 or the Main Menu instead."
            );
            yield break;
        }

        SpawnPlayer(1, registry);
        SpawnPlayer(2, registry);

        Debug.Log(
            $"[RoundSpawner] Players spawned for Round {MatchData.Instance.CurrentRound}. "
                + "Waiting one frame before starting countdown."
        );

        // Wait one frame so MultiplayerPlayerController.InitialiseAfterInputReady
        // has time to commit playerIndex and call MoveToSpawnPoint().
        yield return null;

        countdownManager?.StartCountdown();
    }

    private void SpawnPlayer(int playerID, PlayerInputRegistry registry)
    {
        InputDevice device = registry.GetDevice(playerID);
        string scheme = registry.GetControlScheme(playerID);

        if (device == null)
        {
            Debug.LogError(
                $"[RoundSpawner] No stored device for P{playerID}. " + "Player will not be spawned."
            );
            return;
        }

        // PlayerInput.Instantiate commits playerIndex synchronously when
        // playerIndex is passed explicitly, so InitialiseAfterInputReady's
        // safety loop should resolve on the very next frame.
        var pi = PlayerInput.Instantiate(
            playerPrefab,
            playerIndex: playerID - 1,
            controlScheme: scheme,
            pairWithDevice: device
        );

        Debug.Log(
            $"[RoundSpawner] P{playerID} spawned "
                + $"| scheme={scheme} "
                + $"| device={device.name}"
        );

        // Do NOT call registry.RegisterPlayer here — device and scheme are
        // already stored from Round 1. Re-registering would fire OnPlayerRegistered
        // again and cause UI components to refresh before their own Start() runs.
    }
}
