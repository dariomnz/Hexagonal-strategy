// using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections;
using UnityEditor;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Linq;
using UnityEngine.Pool;

public class HexGrid : MonoBehaviour
{
    // Chunks must be pair
    [Min(1)]
    public int cellCountX = 24, cellCountZ = 24;
    public int cellCount { get => cellCountX * cellCountZ; }
    int chunkCountX, chunkCountZ;

    // public HexCell cellPrefab;
    public AssetReferenceGameObject cellPrefabReference;
    // public TextMeshProUGUI cellLabelPrefab;
    public AssetReferenceGameObject cellLabelPrefabReference;

    public HexGridChunk chunkPrefab;

    [System.NonSerialized] public GameObject map;
    [System.NonSerialized] public HexCell[] cells;
    [System.NonSerialized] public List<HexUnit> units = new List<HexUnit>();

    HexGridChunk[] chunks;

    Vector3 currentCenterIndex = Vector3.one * 9999;

    public HexMapGenerator hexMapGenerator;

    void Start()
    {
        StartCoroutine(hexMapGenerator.GenerateMap(8 * HexMetrics.chunkSizeX, 8 * HexMetrics.chunkSizeZ));
        // StartCoroutine(GetComponent<HexMapGenerator>().GenerateMap(20 * HexMetrics.chunkSizeX, 20 * HexMetrics.chunkSizeZ));
    }

    public Vector3 GetPosition(HexCoordinates coordinates)
    {
        Vector3 position;
        position.x = (coordinates.X + coordinates.Z * 0.5f) * HexMetrics.xDiameter;
        position.z = coordinates.Z * HexMetrics.zDiameter;
        position.y = 0;
        return position;
    }

    public void DeleteMap()
    {
        ClearUnits();

        for (int i = 0; i < transform.childCount; i++)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        map = new GameObject("Map");
        map.transform.parent = transform;

        chunks = null;
        cells = null;
    }

    public IEnumerator CreateMap(int _cellCountX, int _cellCountZ)
    {
        if (!hexMapGenerator.useFixedSeed)
        {
            hexMapGenerator.seed = Random.Range(0, int.MaxValue);
            hexMapGenerator.seed ^= (int)System.DateTime.Now.Ticks;
            hexMapGenerator.seed ^= (int)Time.unscaledTime;
            hexMapGenerator.seed &= int.MaxValue;
        }
        Random.InitState(hexMapGenerator.seed);
        LoadingScreen.Instance.Open();
        HexSearch.ClearPath();
        if (_cellCountX < HexMetrics.chunkSizeX || _cellCountZ < HexMetrics.chunkSizeZ)
        {
            Debug.LogError("Unsupported map size.");
            yield break;
        }
        cellCountX = _cellCountX;
        cellCountZ = _cellCountZ;
        HexMetrics.cellSizeX = cellCountX;
        HexMetrics.cellSizeZ = cellCountZ;

        chunkCountX = cellCountX / HexMetrics.chunkSizeX;
        chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;

        DeleteMap();

        CreateChunks();

        currentCenterIndex = Vector3.one * 9999;
        CameraController.Instance?.CenterMap();
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        yield return StartCoroutine(CreateCells());
        sw.Stop();
        Debug.Log(string.Format("CreateCells in: {0}ms", sw.ElapsedMilliseconds));
        LoadingScreen.Instance.Close();
    }

    void CreateChunks()
    {
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];

        for (int z = 0, i = 0; z < chunkCountZ; z++)
        {
            for (int x = 0; x < chunkCountX; x++)
            {
                HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab, map.transform);
                // HexGridChunk chunk = chunks[i++] = PrefabUtility.InstantiatePrefab(chunkPrefab, map.transform) as HexGridChunk;
                chunk.hexGrid = this;

                Vector3 newPosition = Vector3.zero;
                newPosition.x = HexMetrics.chunkSizeX * HexMetrics.xDiameter * x;
                newPosition.z = HexMetrics.chunkSizeZ * HexMetrics.zDiameter * z;
                chunk.transform.position = newPosition;
            }
        }
    }

    IEnumerator CreateCells()
    {
        cells = new HexCell[cellCountX * cellCountZ];
        TextMeshProUGUI[] labels = new TextMeshProUGUI[cellCountX * cellCountZ];
        AsyncOperationHandle<GameObject>[] asyncOperationHandles = new AsyncOperationHandle<GameObject>[cellCountX * cellCountZ * 2];

        for (int z = 0, i = 0; z < cellCountZ; z++)
        {
            for (int x = 0; x < cellCountX; x++)
            {
                HexCoordinates coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
                int chunkX = x / HexMetrics.chunkSizeX;
                int chunkZ = z / HexMetrics.chunkSizeZ;
                Vector3 newPosition = GetPosition(coordinates);
                newPosition.x -= HexMetrics.chunkSizeX * HexMetrics.xDiameter * chunkX;
                newPosition.z -= HexMetrics.chunkSizeZ * HexMetrics.zDiameter * chunkZ;
                HexGridChunk chunk = GetChunk(chunkX, chunkZ);

                asyncOperationHandles[i] = Addressables.InstantiateAsync(cellPrefabReference, chunk.transform);
                int _x = x, _z = z, _i = i;
                asyncOperationHandles[i].Completed += (asyncOperationHandle) =>
                {
                    HexCell cell = cells[_i] = asyncOperationHandle.Result.GetComponent<HexCell>();
                    cell.coordinates = coordinates;
                    cell.transform.localPosition = newPosition;
                    cell.Index = _i;

                    int localX = _x - chunkX * HexMetrics.chunkSizeX;
                    int localZ = _z - chunkZ * HexMetrics.chunkSizeZ;
                    chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
                };

                asyncOperationHandles[i + cellCountX * cellCountZ] = Addressables.InstantiateAsync(cellLabelPrefabReference, chunk.gridCanvas.transform);

                asyncOperationHandles[i + cellCountX * cellCountZ].Completed += (asyncOperationHandle) =>
                {
                    TextMeshProUGUI label = labels[_i] = asyncOperationHandle.Result.GetComponent<TextMeshProUGUI>();
                    label.rectTransform.anchoredPosition = new Vector2(newPosition.x, newPosition.z);
                };
                if (i % 100 == 0)
                {
                    LoadingScreen.Instance.UpdateLoading(i / ((float)cellCount));
                    yield return new WaitForFixedUpdate();
                }
                i++;
            }
        }

        yield return new WaitUntil(() => asyncOperationHandles.All(x => x.IsDone));

        for (int z = 0, i = 0; z < cellCountZ; z++)
        {
            for (int x = 0; x < cellCountX; x++)
            {
                HexCell cell = cells[i];
                cell.uiRect = labels[i].rectTransform;

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
        yield return null;
    }

    public HexCell GetCell(HexCoordinates coordinates)
    {
        int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
        if (index >= 0 && index < cells.Length)
            return cells[index];
        else
            return null;
    }

    public HexCell GetCell(Ray ray)
    {
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            return hit.collider.GetComponent<HexCell>();
        }
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

    public HexGridChunk GetChunk(int xOffset, int zOffset)
    {
        return chunks[xOffset + zOffset * chunkCountX];
    }

    public HexUnit GetUnit(Ray ray)
    {
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            return hit.collider.GetComponent<HexUnit>();
        }
        return null;
    }

    public void CreateUnit(HexCell cell, HexUnits.UnitType type)
    {
        AddUnit(Instantiate(HexMetrics.Instance.hexUnits.unitsPrefabs[type]), cell, Random.Range(0f, 360f));
    }

    public void AddUnit(HexUnit unit, HexCell location, float orientation)
    {
        units.Add(unit);
        unit.Location = location;
        unit.Orientation = orientation;
    }

    public void RemoveUnit(HexUnit unit)
    {
        units.Remove(unit);
        unit.Die();
    }

    void ClearUnits()
    {
        for (int i = 0; i < units.Count; i++)
        {
            units[i].Die();
        }
        units.Clear();
    }

    public void ShowUI(bool visible)
    {
        for (int i = 0; i < chunks.Length; i++)
        {
            chunks[i].ShowUI(visible);
        }
    }

    public void CenterMap(float xPosition, float zPosition)
    {
        int centerColumnIndex = (int)(xPosition / (HexMetrics.xDiameter * HexMetrics.chunkSizeX));
        int centerRowIndex = (int)(zPosition / (HexMetrics.zDiameter * HexMetrics.chunkSizeZ));
        if (centerColumnIndex == currentCenterIndex.x && centerRowIndex == currentCenterIndex.z)
            return;

        currentCenterIndex.x = centerColumnIndex;
        currentCenterIndex.z = centerRowIndex;

        int minColumnIndex = centerColumnIndex - chunkCountX / 2;
        int minRowIndex = centerRowIndex - chunkCountZ / 2;

        int originX = minColumnIndex;
        int originZ = minRowIndex;

        float ChunkXsize = HexMetrics.xDiameter * HexMetrics.chunkSizeX;
        float superChunkXsize = ChunkXsize * chunkCountX;
        float ChunkZsize = HexMetrics.zDiameter * HexMetrics.chunkSizeZ;
        float superChunkZsize = ChunkZsize * chunkCountZ;

        Vector3 position;
        position.y = 0f;

        for (int z = 0, i = 0; z < chunkCountZ; z++)
        {
            for (int x = 0; x < chunkCountX; x++)
            {
                int checkX = originX + x;
                int superChunkX = checkX / chunkCountX;
                int inSuperChunkX = checkX % chunkCountX;
                position.x = superChunkX * superChunkXsize + ChunkXsize * inSuperChunkX;

                int checkZ = originZ + z;
                int superChunkZ = checkZ / chunkCountZ;
                int inSuperChunkZ = checkZ % chunkCountZ;
                position.z = superChunkZ * superChunkZsize + ChunkZsize * inSuperChunkZ;

                inSuperChunkX = inSuperChunkX < 0 ? chunkCountX + inSuperChunkX : inSuperChunkX;
                inSuperChunkZ = inSuperChunkZ < 0 ? chunkCountZ + inSuperChunkZ : inSuperChunkZ;

                HexGridChunk chunk = GetChunk(inSuperChunkX, inSuperChunkZ);

                position.y = chunk.transform.position.y;
                chunk.transform.position = position;
                i++;
            }
        }

    }

    public void Save(BinaryWriter writer)
    {
        CameraController.Instance.Save(writer);
        writer.Write(cellCountX);
        writer.Write(cellCountZ);
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Save(writer);
        }

        writer.Write(units.Count);
        for (int i = 0; i < units.Count; i++)
        {
            units[i].Save(writer);
        }
    }

    public IEnumerator Load(BinaryReader reader)
    {
        StopAllCoroutines();
        ClearUnits();
        CameraController.Instance.Load(reader);
        yield return StartCoroutine(CreateMap(reader.ReadInt32(), reader.ReadInt32()));
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Load(reader);
        }
        int unitCount = reader.ReadInt32();
        for (int i = 0; i < unitCount; i++)
        {
            HexUnit.Load(reader, this);
        }
    }
}