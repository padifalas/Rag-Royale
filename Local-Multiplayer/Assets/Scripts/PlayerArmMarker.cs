using UnityEngine;

public class PlayerArmMarker : MonoBehaviour
{
    public int PlayerID;
    public ParticleSystem StringTrail;

    private Renderer[] renderers;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
    }

    public void SetArmVisible(bool visible)
    {
        // Some renderer references may have been destroyed; guard against that to avoid
        // MissingReferenceException during runtime (e.g., editor object destroy).
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null)
                continue;
            r.enabled = visible;
        }
    }
}
