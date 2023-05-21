using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections;

public class HexGrid : MonoBehaviour
{
    [Min(1)]
    public int cellCountX = 30, cellCountZ = 30;
    public int cellCount { get => cellCountX * cellCountZ; }
    int chunkCountX, chunkCountZ;

    // private int cellCountX { get { return chunkCountX * HexMetrics.chunkSizeX; } }
    // private int cellCountZ { get { return chunkCountZ * HexMetrics.chunkSizeZ; } }

    public HexCell cellPrefab;
    public TextMeshProUGUI cellLabelPrefab;

    public HexGridChunk chunkPrefab;

    [NonSerialized] public GameObject map;
    [NonSerialized] public HexCell[] cells;

    HexGridChunk[] chunks;

    HexCellPriorityQueue searchFrontier;
    int searchFrontierPhase;
    HexCell currentPathFrom, currentPathTo;
    bool currentPathExists;

    void Start()
    {
        CreateMap();
    }

    public Vector3 GetPosition(HexCoordinates coordinates)
    {
        Vector3 position;
        position.x = (coordinates.X + coordinates.Z * 0.5f) * HexMetrics.xRadius;
        position.z = coordinates.Z * HexMetrics.zRadius;
        position.y = 0;
        // position.y = Mathf.PerlinNoise(position.x, position.z);
        return position;
    }

    public void CreateMap()
    {
        CreateMap(cellCountX, cellCountZ);
    }

    public void DeleteMap()
    {
        foreach (Transform child in transform)
            DestroyImmediate(child.gameObject);

        map = new GameObject("Map");
        map.transform.parent = transform;

        chunks = null;
        cells = null;
    }

    public void CreateMap(int _cellCountX, int _cellCountZ)
    {
        ClearPath();
        if (_cellCountX < HexMetrics.chunkSizeX || _cellCountZ < HexMetrics.chunkSizeZ)
        {
            Debug.LogError("Unsupported map size.");
            return;
        }
        cellCountX = _cellCountX;
        cellCountZ = _cellCountZ;
        HexMetrics.cellSizeX = cellCountX;
        HexMetrics.cellSizeZ = cellCountZ;

        chunkCountX = cellCountX / HexMetrics.chunkSizeX;
        chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;

        DeleteMap();

        CreateChunks();
        CreateCells();
    }

    void CreateChunks()
    {
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];

        for (int z = 0, i = 0; z < chunkCountZ; z++)
        {
            for (int x = 0; x < chunkCountX; x++)
            {
                HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
                chunk.hexGrid = this;
                chunk.transform.SetParent(map.transform);
            }
        }
    }

    void AddCellToChunk(int x, int z, HexCell cell)
    {
        int chunkX = x / HexMetrics.chunkSizeX;
        int chunkZ = z / HexMetrics.chunkSizeZ;
        HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];

        int localX = x - chunkX * HexMetrics.chunkSizeX;
        int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
    }

    void CreateCells()
    {
        cells = new HexCell[cellCountX * cellCountZ];

        for (int z = 0, i = 0; z < cellCountZ; z++)
        {
            for (int x = 0; x < cellCountX; x++)
            {
                HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab, map.transform);
                cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
                cell.transform.localPosition = GetPosition(cell.coordinates);
                AddCellToChunk(x, z, cell);
                i++;
            }
        }

        for (int z = 0, i = 0; z < cellCountZ; z++)
        {
            for (int x = 0; x < cellCountX; x++)
            {
                HexCell cell = cells[i];
                if (x > 0)
                {
                    cell.SetNeighbor(HexDirection.W, cells[i - 1]);

                }
                else
                {
                    cell.SetNeighbor(HexDirection.W, cells[i + cellCountX - 1]);
                    if ((z & 1) == 0)
                    {
                        cell.SetNeighbor(HexDirection.NW, cells[i + cellCountX * 2 - 1]);
                        if (z != 0)
                            cell.SetNeighbor(HexDirection.SW, cells[i - 1]);
                    }
                }
                if (z > 0)
                {
                    if ((z & 1) == 0)
                    {
                        cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
                        if (x > 0)
                        {
                            cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
                        }
                    }
                    else
                    {
                        cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
                        if (x < cellCountX - 1)
                        {
                            cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
                        }
                    }
                }
                else
                {
                    cell.SetNeighbor(HexDirection.SE, cells[cells.Length - (cellCountX - i)]);
                    if (x == 0)
                        cell.SetNeighbor(HexDirection.SW, cells[cells.Length - 1]);
                    else
                        cell.SetNeighbor(HexDirection.SW, cells[cells.Length - (cellCountX - i + 1)]);
                }
                i++;
            }
        }

        foreach (var cell in cells)
        {

            TextMeshProUGUI label = Instantiate<TextMeshProUGUI>(cellLabelPrefab);
            label.rectTransform.SetParent(cell.chunk.gridCanvas.transform, false);
            label.rectTransform.anchoredPosition =
                new Vector2(cell.transform.localPosition.x, cell.transform.localPosition.z);

            cell.uiRect = label.rectTransform;
            cell.UpdateLabel();
        }
    }

    public HexCell GetCell(Vector3 worldPosition)
    {
        HexCoordinates coordinates = HexCoordinates.FromPosition(worldPosition);
        return GetCell(coordinates);
    }

    public HexCell GetCell(HexCoordinates coordinates)
    {
        int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
        if (index >= 0 && index < cells.Length)
            return cells[index];
        else
            return null;
    }

    public HexCell GetCell(int xOffset, int zOffset)
    {
        return cells[xOffset + zOffset * cellCountX];
    }

    public HexCell GetCell(int cellIndex)
    {
        return cells[cellIndex];
    }

    public void FindPath(HexCell fromCell, HexCell toCell)
    {   
        
        // System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        // sw.Start();
        ClearPath();
        currentPathFrom = fromCell;
        currentPathTo = toCell;
        currentPathExists = Search(fromCell, toCell);
        ShowPath();
        // sw.Stop();
        // Debug.Log(sw.ElapsedMilliseconds);
    }

    void ShowPath()
    {
        if (currentPathExists)
        {
            HexCell current = currentPathTo;
            while (current != currentPathFrom)
            {
                // int turn = current.Distance / speed;
                // current.SetLabel(turn.ToString());
                current.EnableHighlight(Color.white);
                current.UpdateLabel();
                current = current.PathFrom;
            }
        }
        currentPathFrom.EnableHighlight(Color.blue);
        currentPathTo.EnableHighlight(Color.red);
    }

    void ClearPath()
    {
        if (currentPathExists)
        {
            HexCell current = currentPathTo;
            while (current != currentPathFrom)
            {
                current.SetLabel(null);
                current.DisableHighlight();
                current = current.PathFrom;
            }
            current.DisableHighlight();
            currentPathExists = false;
        }
        else if (currentPathFrom)
        {
            currentPathFrom.DisableHighlight();
            currentPathTo.DisableHighlight();
        }
        currentPathFrom = currentPathTo = null;
    }

    bool Search(HexCell fromCell, HexCell toCell)
    // IEnumerator Search(HexCell fromCell, HexCell toCell)
    {
        searchFrontierPhase += 2;
        if (searchFrontier == null)
            searchFrontier = new HexCellPriorityQueue();
        else
            searchFrontier.Clear();

        // for (int i = 0; i < cells.Length; i++)
        // {
        //     // cells[i].Distance = int.MaxValue;
        //     // cells[i].SearchHeuristic = 0;
        //     cells[i].DisableHighlight();
        // }

        fromCell.EnableHighlight(Color.blue);
        toCell.EnableHighlight(Color.red);

        // WaitForSeconds delay = new WaitForSeconds(1 / 60f);
        fromCell.SearchPhase = searchFrontierPhase;
        fromCell.Distance = 0;
        searchFrontier.Enqueue(fromCell);
        while (searchFrontier.Count > 0)
        {
            // yield return delay;
            HexCell current = searchFrontier.Dequeue();
            current.SearchPhase += 1;
            if (current == toCell)
                // {
                return true;
            //     current = current.PathFrom;
            //     while (current != fromCell)
            //     {
            //         current.EnableHighlight(Color.white);
            //         current = current.PathFrom;
            //     }
            //     break;
            // }

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor == null || neighbor.SearchPhase > searchFrontierPhase)
                    continue;
                if (neighbor.TerrainType == HexTerrains.HexType.Water)
                    continue;
                int distance = current.Distance;
                if (current.HasRoadThroughEdge(d))
                    distance += 1;
                else
                    distance += 10;
                int elevationDiff = Mathf.Abs(neighbor.Elevation - current.Elevation);
                if (elevationDiff > 1)
                    continue;
                if (elevationDiff == 1)
                    distance += 5;
                if (neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    neighbor.SearchHeuristic = neighbor.coordinates.DistanceTo(toCell.coordinates);
                    searchFrontier.Enqueue(neighbor);
                }
                else if (distance < neighbor.Distance)
                {
                    int oldPriority = neighbor.SearchPriority;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    searchFrontier.Change(neighbor, oldPriority);
                }
            }
        }
        return false;
    }

    public void ShowUI(bool visible)
    {
        for (int i = 0; i < chunks.Length; i++)
        {
            chunks[i].ShowUI(visible);
        }
    }

    public void Save(BinaryWriter writer)
    {
        writer.Write(cellCountX);
        writer.Write(cellCountX);
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Save(writer);
        }
    }

    public void Load(BinaryReader reader)
    {
        StopAllCoroutines();
        CreateMap(reader.ReadInt32(), reader.ReadInt32());
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Load(reader);
        }
    }
}