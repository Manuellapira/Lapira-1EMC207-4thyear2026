using UnityEngine;
using UnityEngine.AI;

// Computes each custom NavMesh area's bitmask once at startup and caches
// it, so the 150+ per-agent AreaSpeedModifier scripts don't each call
// NavMesh.GetAreaFromName() separately. Area masks work like
// LayerMasks - bit shifting is unavoidable here, per the cheat sheet.
//
// Place this on a manager object that already exists in the scene (not
// one you spawn) - Awake() on every object in the scene runs before any
// object's Start(), so as long as this isn't itself instantiated at
// runtime, it's guaranteed ready before AgentSpawner's Start() spawns
// anyone who needs it.
public class TerrainMaskHandler : MonoBehaviour
{
    public static TerrainMaskHandler Instance { get; private set; }

    [SerializeField] private string slowTerrainAreaName = "SlowTerrain";

    public int SlowTerrainMask { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        SlowTerrainMask = 1 << NavMesh.GetAreaFromName(slowTerrainAreaName);
    }
}