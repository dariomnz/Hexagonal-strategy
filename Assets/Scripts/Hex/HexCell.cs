using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class HexCell : MonoBehaviour
{
    HexMetrics.HexType terrainType;
    public HexMetrics.HexType TerrainType
    {
        get { return terrainType; }
        set
        {
            if (terrainType != value)
            {
                terrainType = value;
                ChangeTerrainType(terrainType);
            }
        }
    }
    int elevation;
    public int Elevation
    {
        get { return elevation; }
        set
        {
            elevation = value;
            Vector3 position = transform.localPosition;
            position.y = value * HexMetrics.elevationStep;
            transform.localPosition = position;
        }
    }
    public HexCoordinates coordinates;
    public HexCell[] neighbors;
    MeshCollider meshCollider;
    [NonSerialized]
    public HexGridChunk chunk;

    void Awake()
    {
        meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.convex = true;
        meshCollider.sharedMesh = GetComponentInChildren<MeshFilter>().sharedMesh;
    }

    void ChangeTerrainType(HexMetrics.HexType newType)
    {
        ChangeMesh(HexMetrics.GetTerrainPrefab(newType));
    }

    void ChangeMesh(GameObject newTilePrefab)
    {
        Destroy(GetComponentInChildren<MeshFilter>().gameObject);
        var tile = Instantiate(newTilePrefab, transform.position, transform.rotation);
        tile.GetComponentInChildren<MeshFilter>().transform.parent = gameObject.transform;
        Destroy(tile);
        // Debug.Log("ChangeTile at " + coordinates.ToString());
    }

    public HexCell GetNeighbor(HexDirection direction)
    {
        return neighbors[(int)direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        neighbors[(int)direction] = cell;
        cell.neighbors[(int)direction.Opposite()] = this;
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write((int)terrainType);
        writer.Write(elevation);
    }

    public void Load(BinaryReader reader)
    {
        TerrainType = (HexMetrics.HexType)reader.ReadInt32();
        Elevation = reader.ReadInt32();
    }

}
