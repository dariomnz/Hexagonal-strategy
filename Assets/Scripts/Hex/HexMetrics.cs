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
    public static int cellSizeX = 0, cellSizeZ = 0;

    public const float xDiameter = 2f, zDiameter = 1.725f;
    public const float xRadius = xDiameter / 2f, zRadius = zDiameter / 2f;

    public const float elevationStep = 0.5f;

    public HexTerrains hexTerrains;
    public HexUnits hexUnits;

    private static Vector3[] _hexVertex = null;

    public static Vector3[] hexVertex
    {
        get
        {
            if (_hexVertex != null)
                return _hexVertex;
            Vector3[] outVector = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                outVector[i] = Quaternion.AngleAxis(60 * i, Vector3.up) * (Vector3.forward * xRadius);
            }
            _hexVertex = outVector;
            return outVector;
        }
    }
}
