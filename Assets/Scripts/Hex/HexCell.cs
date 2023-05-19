using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class HexCell : MonoBehaviour
{
    [SerializeField]
    HexTerrains.HexType terrainType;
    public HexTerrains.HexType TerrainType
    {
        get { return terrainType; }
        set
        {
            if (terrainType != value)
            {
                terrainType = value;
                UpdateMesh();
            }
        }
    }
    [SerializeField]
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
            for (int i = 0; i < roads.Length; i++)
                if (roads[i] && GetElevationDifference((HexDirection)i) > 1)
                    SetRoad(i, false);
        }
    }
    public HexCoordinates coordinates;
    public HexFeatureManager featureManager;
    public HexCell[] neighbors;
    [SerializeField]
    bool[] roads;
    MeshCollider meshCollider;
    MeshFilter meshFilter;
    [NonSerialized]
    public HexGridChunk chunk;

    public void Refresh() => enabled = true;

    void Awake()
    {
        meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.convex = true;
        meshFilter = GetComponentInChildren<MeshFilter>();
        meshCollider.sharedMesh = meshFilter.sharedMesh;
    }

    void LateUpdate()
    {
        UpdateMesh();
        enabled = false;
    }

    public void UpdateMesh()
    {
        ChangeMesh(HexMetrics.Instance.hexTerrains.GetMesh(terrainType, roads, out int rotations), rotations);
    }

    void ChangeMesh(GameObject newTilePrefab, int rotations)
    {
        if (newTilePrefab == null)
            return;
        DestroyImmediate(meshFilter.gameObject);
        GameObject newMeshGameObject = Instantiate(newTilePrefab, transform.position, transform.rotation, transform);
        newMeshGameObject.transform.eulerAngles = Vector3.up * -60 * (rotations);
        meshFilter = newMeshGameObject.GetComponent<MeshFilter>();
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

    public int GetElevationDifference(HexDirection direction)
    {
        int difference = elevation - GetNeighbor(direction).elevation;
        return difference >= 0 ? difference : -difference;
    }
    public bool HasRoadThroughEdge(HexDirection direction)
    {
        return roads[(int)direction];
    }

    public void AddRoad(HexDirection direction)
    {
        if (!roads[(int)direction] &&
            GetElevationDifference(direction) <= 1)
            SetRoad((int)direction, true);
    }

    public void RemoveRoads()
    {
        for (int i = 0; i < neighbors.Length; i++)
            if (roads[i])
                SetRoad(i, false);
    }

    void SetRoad(int index, bool state)
    {
        roads[index] = state;
        neighbors[index].roads[(int)((HexDirection)index).Opposite()] = state;
        Refresh();
        neighbors[index].Refresh();
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write((byte)terrainType);
        writer.Write((byte)elevation);
        writer.Write((byte)featureManager.currentFeature);
        int roadFlags = 0;
        for (int i = 0; i < roads.Length; i++)
            if (roads[i])
                roadFlags |= 1 << i;
        writer.Write((byte)roadFlags);
    }

    public void Load(BinaryReader reader)
    {
        terrainType = (HexTerrains.HexType)reader.ReadByte();
        Elevation = reader.ReadByte();
        featureManager.AddFeature(this, (HexFeatureManager.Features)reader.ReadByte());
        int roadFlags = reader.ReadByte();
        for (int i = 0; i < roads.Length; i++)
            roads[i] = (roadFlags & (1 << i)) != 0;

        Refresh();
    }

}
