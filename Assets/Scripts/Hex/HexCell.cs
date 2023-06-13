using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;

public class HexCell : MonoBehaviour, IInteractive
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
                Refresh();
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

            if (hasOutgoingRiver &&
                elevation < GetNeighbor(outgoingRiver).elevation)
                RemoveOutgoingRiver();
            if (hasIncomingRiver &&
                elevation > GetNeighbor(incomingRiver).elevation)
                RemoveIncomingRiver();
            if (Unit)
                Unit.Location = this;
            if (featureManager.currentFeature != HexFeatureManager.Features.None)
                featureManager.Location = this;

            Refresh();
        }
    }

    public int Distance { get; set; } = int.MaxValue;
    public HexCoordinates coordinates;
    public HexFeatureManager featureManager;
    public HexCell[] neighbors;
    public bool hasIncomingRiver { get; set; }
    public bool hasOutgoingRiver { get; set; }
    public HexDirection incomingRiver { get; set; }
    public HexDirection outgoingRiver { get; set; }
    [SerializeField]
    bool[] roads;
    MeshCollider meshCollider;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public HexGridChunk chunk { get; set; }
    public int Index { get; set; }
    public RectTransform uiRect { get; set; }
    public HexCell PathFrom { get; set; }
    public HexCell NextWithSamePriority { get; set; }
    public int SearchPhase { get; set; }
    public int SearchHeuristic { get; set; } = 0;
    public int waterLevel { get; set; }
    public int waterDeep { get; set; }
    public bool isLake { get; set; } = false;
    public float temperature;
    public float moisture;
    GameObject waterContainer;
    public HexTerrains.HexType terrainWaterFloorType { get; set; }
    public HexUnit Unit { get; set; }

    public bool IsUnderwater => waterLevel > elevation || waterDeep > 0;

    public void Refresh() => enabled = true;

    void OnDestroy()
    {
        if (uiRect)
            Addressables.ReleaseInstance(uiRect.gameObject);
        Addressables.ReleaseInstance(gameObject);
    }

    void LateUpdate()
    {
        UpdateMesh();
        enabled = false;
    }

    public void UpdateMesh()
    {
        int rotations = 0;
        if (terrainType == HexTerrains.HexType.Water)
            meshFilter.mesh = HexMetrics.Instance.hexTerrains.waterTop;
        else if (HasRiver())
            meshFilter.mesh = HexMetrics.Instance.hexTerrains.GetRiverMesh(GetRivers(), out rotations);
        else if (HasRoads())
            meshFilter.mesh = HexMetrics.Instance.hexTerrains.GetRoadMesh(roads, out rotations);
        else
            meshFilter.mesh = HexMetrics.Instance.hexTerrains.GetSimpleMesh();

        meshRenderer.materials = HexMetrics.Instance.hexTerrains.GetMaterials(terrainType, HasRoads(), HasRiver());

        meshFilter.transform.eulerAngles = Vector3.up * -60 * (rotations);
        if (IsUnderwater)
            GenerateWater();
        else
            ClearWaterFloor();
    }

    void ClearWaterFloor()
    {
        if (waterContainer)
            DestroyImmediate(waterContainer);
    }

    void GenerateWater()
    {
        ClearWaterFloor();
        waterContainer = new GameObject("waterContainer");
        waterContainer.transform.SetParent(transform, false);
        GameObject newMeshGameObject = new GameObject("waterFloor", new Type[] { typeof(MeshFilter), typeof(MeshRenderer) });
        newMeshGameObject.transform.SetParent(waterContainer.transform, false);
        MeshFilter floorMeshFilter = newMeshGameObject.GetComponent<MeshFilter>();
        MeshRenderer floorMeshRenderer = newMeshGameObject.GetComponent<MeshRenderer>();

        floorMeshFilter.mesh = HexMetrics.Instance.hexTerrains.GetSimpleMesh();
        floorMeshRenderer.materials = HexMetrics.Instance.hexTerrains.GetMaterials(terrainWaterFloorType);

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

    public bool HasRiver() { return hasIncomingRiver || hasOutgoingRiver; }
    public bool HasRiverBeginOrEnd() { return hasIncomingRiver != hasOutgoingRiver; }
    public bool HasRiverThroughEdge(HexDirection direction)
    {
        return
            hasIncomingRiver && incomingRiver == direction ||
            hasOutgoingRiver && outgoingRiver == direction;
    }

    public string GetRivers()
    {
        string outValue = "";
        foreach (HexDirection i in Enum.GetValues(typeof(HexDirection)))
            if (hasIncomingRiver && incomingRiver == i)
                outValue += "1";
            else if (hasOutgoingRiver && outgoingRiver == i)
                outValue += "2";
            else
                outValue += "0";
        return outValue;
    }

    public void SetOutgoingRiver(HexDirection direction)
    {
        if (hasOutgoingRiver && outgoingRiver == direction)
            return;
        HexCell neighbor = GetNeighbor(direction);
        if (!neighbor || elevation < neighbor.elevation)
            return;
        RemoveOutgoingRiver();
        if (hasIncomingRiver && incomingRiver == direction)
            RemoveIncomingRiver();
        hasOutgoingRiver = true;
        outgoingRiver = direction;
        Refresh();
        neighbor.RemoveIncomingRiver();
        neighbor.hasIncomingRiver = true;
        neighbor.incomingRiver = direction.Opposite();
        neighbor.Refresh();
    }

    public void RemoveOutgoingRiver()
    {
        if (!hasOutgoingRiver)
        {
            return;
        }
        hasOutgoingRiver = false;
        Refresh();

        HexCell neighbor = GetNeighbor(outgoingRiver);
        neighbor.hasIncomingRiver = false;
        neighbor.Refresh();
    }

    public void RemoveIncomingRiver()
    {
        if (!hasIncomingRiver)
        {
            return;
        }
        hasIncomingRiver = false;
        Refresh();

        HexCell neighbor = GetNeighbor(incomingRiver);
        neighbor.hasOutgoingRiver = false;
        neighbor.Refresh();
    }

    public void RemoveRiver()
    {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }

    public bool HasRoadThroughEdge(HexDirection direction)
    {
        return roads[(int)direction];
    }

    public bool HasRoads()
    {
        foreach (var road in roads)
        {
            if (road)
                return true;
        }
        return false;
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


        text = Distance == int.MaxValue ? "" : Distance.ToString();
        text += "\n" + (SearchHeuristic == 0 ? "" : SearchHeuristic.ToString());

        SetLabel(text);
    }

    public int SearchPriority { get { return Distance + SearchHeuristic; } }

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
        if (terrainType == HexTerrains.HexType.Water)
        {
            writer.Write((byte)waterDeep);
            writer.Write((byte)terrainWaterFloorType);
        }

        if (hasIncomingRiver)
            writer.Write((byte)(incomingRiver + 128));
        else
            writer.Write((byte)0);

        if (hasOutgoingRiver)
            writer.Write((byte)(outgoingRiver + 128));
        else
            writer.Write((byte)0);

        featureManager.Save(writer);
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
        if (terrainType == HexTerrains.HexType.Water)
        {
            waterDeep = reader.ReadByte();
            terrainWaterFloorType = (HexTerrains.HexType)reader.ReadByte();
        }

        byte riverData = reader.ReadByte();
        if (riverData >= 128)
        {
            hasIncomingRiver = true;
            incomingRiver = (HexDirection)(riverData - 128);
        }
        else
            hasIncomingRiver = false;

        riverData = reader.ReadByte();
        if (riverData >= 128)
        {
            hasOutgoingRiver = true;
            outgoingRiver = (HexDirection)(riverData - 128);
        }
        else
            hasOutgoingRiver = false;

        featureManager.Load(reader);
        int roadFlags = reader.ReadByte();
        for (int i = 0; i < roads.Length; i++)
            roads[i] = (roadFlags & (1 << i)) != 0;

        Refresh();
    }

    public override string ToString()
    {
        return coordinates.ToString();
    }

    public Dictionary<HexInteration, UnityAction> GetInteractions()
    {
        Dictionary<HexInteration, UnityAction> outDict = new Dictionary<HexInteration, UnityAction>();

        if (!Unit)
            outDict[HexInteration.CellCreateUnit] = () =>
            {
                HexGameUI.Instance.OpenInteraction(transform.position + Vector3.up * 3);
                HexGameUI.Instance.hexRadialInteractiveUI.CreateMenu(new Dictionary<HexInteration, UnityAction>() {
                    {HexInteration.CellCreateUnitKnight,()=>{chunk.hexGrid.CreateUnit(this,HexUnits.UnitType.Knight);
                    HexGameUI.Instance.CloseInteraction(); }},
                    {HexInteration.CellCreateUnitBarbarian,()=>{chunk.hexGrid.CreateUnit(this,HexUnits.UnitType.Barbarian);
                    HexGameUI.Instance.CloseInteraction(); }},
                    {HexInteration.CellCreateUnitMage,()=>{chunk.hexGrid.CreateUnit(this,HexUnits.UnitType.Mage);
                    HexGameUI.Instance.CloseInteraction(); }},
                    {HexInteration.CellCreateUnitRogue,()=>{chunk.hexGrid.CreateUnit(this,HexUnits.UnitType.Rogue);
                    HexGameUI.Instance.CloseInteraction(); }},
                });
            };

        return outDict;
    }

    public void Interact()
    {
        HexGameUI.Instance.OpenInteraction(transform.position + Vector3.up * 3, this);
    }
}
