using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;

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

            Vector3 uiPosition = uiRect.localPosition;
            uiPosition.z = elevation * -HexMetrics.elevationStep - 1.05f;
            uiRect.localPosition = uiPosition;
        }
    }

    int distance = int.MaxValue;
    public int Distance
    {
        get { return distance; }
        set
        {
            distance = value;
            // UpdateLabel();
        }
    }

    int searchHeuristic = 0;
    public int SearchHeuristic
    {
        get { return searchHeuristic; }
        set
        {
            searchHeuristic = value;
            // UpdateLabel();
        }
    }
    public HexCoordinates coordinates;
    public HexFeatureManager featureManager;
    public HexCell[] neighbors;
    [SerializeField]
    bool[] roads;
    MeshCollider meshCollider;
    MeshFilter meshFilter;
    public HexGridChunk chunk { get; set; }
    public int Index { get; set; }
    public RectTransform uiRect { get; set; }
    public HexCell PathFrom { get; set; }
    public HexCell NextWithSamePriority { get; set; }
    public int SearchPhase { get; set; }
    public int waterLevel { get; set; }
    public int waterDeep { get; set; }
    GameObject waterContainer;
    public HexTerrains.HexType terrainWaterFloorType { get; set; }

    public bool IsUnderwater => waterLevel > elevation || waterDeep > 0;

    public void Refresh() => enabled = true;

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
        if (meshFilter)
            DestroyImmediate(meshFilter.gameObject);
        GameObject newMeshGameObject = Instantiate(newTilePrefab, transform.position, transform.rotation, transform);
        // newMeshGameObject.isStatic = true;
        newMeshGameObject.transform.eulerAngles = Vector3.up * -60 * (rotations);
        meshFilter = newMeshGameObject.GetComponent<MeshFilter>();
        if (terrainType == HexTerrains.HexType.Water)
            Debug.Log(string.Format("IsUnderwater {0} waterDeep {1} elevation {2}", IsUnderwater, waterDeep, elevation));
        if (IsUnderwater)
            GenerateWater();
    }

    void GenerateWater()
    {
        Debug.Log("newMeshGameObject");
        if (waterContainer)
            DestroyImmediate(waterContainer);

        waterContainer = new GameObject("waterContainer");
        waterContainer.transform.parent = transform;
        GameObject newMeshGameObject = Instantiate(HexMetrics.Instance.hexTerrains.GetSimpleMesh(terrainWaterFloorType), transform.position, transform.rotation, waterContainer.transform);
        Vector3 pos = newMeshGameObject.transform.position;
        pos.y -= (waterDeep + 1) * HexMetrics.elevationStep;
        newMeshGameObject.transform.position = pos;
        newMeshGameObject.transform.localScale = Vector3.one * 0.999f;
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

    public void UpdateLabel()
    {
        string text = "";
        // TextMeshProUGUI label = uiRect.GetComponent<TextMeshProUGUI>();

        // label.text = coordinates.ToString();
        // int neighborscount = 0;
        // foreach (var item in neighbors)
        //     if (item != null)
        //         neighborscount++;

        // label.text += "\nNeigh: " + neighborscount.ToString();
        // label.text += "\nDist: " + (distance == int.MaxValue ? "" : distance.ToString());


        text = distance == int.MaxValue ? "" : distance.ToString();
        text += "\n" + (SearchHeuristic == 0 ? "" : SearchHeuristic.ToString());

        SetLabel(text);
    }

    public int SearchPriority { get { return distance + SearchHeuristic; } }

    public void DisableHighlight()
    {
        Image highlight = uiRect.GetChild(0).GetComponent<Image>();
        highlight.enabled = false;
    }

    public void EnableHighlight(Color color)
    {
        Image highlight = uiRect.GetChild(0).GetComponent<Image>();
        highlight.color = color;
        highlight.enabled = true;
    }

    public void SetLabel(string text)
    {
        TextMeshProUGUI label = uiRect.GetComponent<TextMeshProUGUI>();
        label.text = text;
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write((byte)terrainType);
        writer.Write((byte)(elevation + 127));
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
        Elevation = reader.ReadByte() - 127;
        featureManager.AddFeature(this, (HexFeatureManager.Features)reader.ReadByte());
        int roadFlags = reader.ReadByte();
        for (int i = 0; i < roads.Length; i++)
            roads[i] = (roadFlags & (1 << i)) != 0;

        Refresh();
    }

}
