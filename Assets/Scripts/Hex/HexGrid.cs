using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;


public class HexGrid : MonoBehaviour
{
    [Min(1)]
    public int cellCountX = 30, cellCountZ = 30;
    int chunkCountX, chunkCountZ;

    // private int cellCountX { get { return chunkCountX * HexMetrics.chunkSizeX; } }
    // private int cellCountZ { get { return chunkCountZ * HexMetrics.chunkSizeZ; } }

    public HexCell cellPrefab;
    public HexGridChunk chunkPrefab;

    [NonSerialized] public GameObject map;
    [NonSerialized] public HexCell[] cells;

    HexGridChunk[] chunks;

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

    public void CreateMap(int _cellCountX, int _cellCountZ)
    {
        if (_cellCountX < HexMetrics.chunkSizeX || _cellCountZ < HexMetrics.chunkSizeZ)
        {
            Debug.LogError("Unsupported map size.");
            return;
        }
        cellCountX = _cellCountX;
        cellCountZ = _cellCountZ;

        chunkCountX = cellCountX / HexMetrics.chunkSizeX;
        chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;


        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }
        map = new GameObject("Map");
        map.transform.parent = transform;

        chunks = null;
        cells = null;

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
            var label = cell.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = cell.coordinates.ToString();
                int neighborscount = 0;
                foreach (var item in cell.neighbors)
                    if (item != null)
                        neighborscount++;

                label.text += "\nNeighbors:\n" + neighborscount.ToString();
            }
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
        CreateMap(reader.ReadInt32(), reader.ReadInt32());
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Load(reader);
        }
    }
}