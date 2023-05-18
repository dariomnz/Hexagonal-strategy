using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexMetrics : MonoBehaviour
{
    public static HexMetrics Instance { get; private set; }

    private void Awake()
    {
        // If there is an instance, and it's not me, delete myself.

        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    // multiplo de 6
    public const int chunkSizeX = 6, chunkSizeZ = 6;

    public const float xRadius = 2f, zRadius = 1.725f;

    public const float elevationStep = 1f;

    public enum HexType
    {
        Grass,
        Rock,
        Sand,
        Water,
    };

    public List<GameObject> TerrainTypesPrefabs;

    public static GameObject GetTerrainPrefab(HexType type)
    {
        if ((int)type >= Instance.TerrainTypesPrefabs.Count)
            return null;
        return Instance.TerrainTypesPrefabs[(int)type];
    }
}
