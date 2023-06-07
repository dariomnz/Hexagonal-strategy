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

    HexCellPriorityQueue searchFrontier;
    int searchFrontierPhase;
    HexCell currentPathFrom, currentPathTo;
    bool currentPathExists;
    Vector3 currentCenterIndex = Vector3.one * 9999;

    public bool HasPath { get { return currentPathExists; } }

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
        ClearPath();
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

    void AddCellToChunk(int x, int z, HexCell cell)
    {
        int chunkX = x / HexMetrics.chunkSizeX;
        int chunkZ = z / HexMetrics.chunkSizeZ;
        HexGridChunk chunk = GetChunk(chunkX, chunkZ);

        int localX = x - chunkX * HexMetrics.chunkSizeX;
        int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
    }

    IEnumerator CreateCells()
    {
        cells = new HexCell[cellCountX * cellCountZ];
        AsyncOperationHandle<GameObject>[] asyncOperationHandles = new AsyncOperationHandle<GameObject>[cellCountX * cellCountZ * 2];

        for (int z = 0, i = 0; z < cellCountZ; z++)
        {
            for (int x = 0; x < cellCountX; x++)
            {
                HexCoordinates coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
                int chunkX = x / HexMetrics.chunkSizeX;
                int chunkZ = z / HexMetrics.chunkSizeZ;
                Vector3 newPosition = GetPosition(coordinates);
                HexGridChunk chunk = GetChunk(chunkX, chunkZ);

                asyncOperationHandles[i] = Addressables.InstantiateAsync(cellPrefabReference, chunk.transform);
                int _x = x, _z = z, _i = i;
                asyncOperationHandles[i].Completed += (asyncOperationHandle) =>
                {
                    HexCell cell = cells[_i] = asyncOperationHandle.Result.GetComponent<HexCell>();
                    cell.coordinates = coordinates;
                    newPosition.x -= HexMetrics.chunkSizeX * HexMetrics.xDiameter * chunkX;
                    newPosition.z -= HexMetrics.chunkSizeZ * HexMetrics.zDiameter * chunkZ;
                    cell.transform.localPosition = newPosition;
                    cell.Index = _i;
                    AddCellToChunk(_x, _z, cell);

                    asyncOperationHandles[_i + cellCountX * cellCountZ] = Addressables.InstantiateAsync(cellLabelPrefabReference, cell.chunk.gridCanvas.transform);

                    asyncOperationHandles[_i + cellCountX * cellCountZ].Completed += (asyncOperationHandle) =>
                    {
                        TextMeshProUGUI label = asyncOperationHandle.Result.GetComponent<TextMeshProUGUI>();
                        label.rectTransform.anchoredPosition = new Vector2(cell.transform.localPosition.x, cell.transform.localPosition.z);
                        cell.uiRect = label.rectTransform;
                    };

                    LoadingScreen.Instance.UpdateLoading(i / ((float)cellCount * 2));
                };
                i++;
            }
            if (z % (HexMetrics.chunkSizeZ / 2) == 0)
            {
                LoadingScreen.Instance.UpdateLoading(i / ((float)cellCount * 2));
                yield return null;
            }
        }

        yield return new WaitUntil(() => asyncOperationHandles.All(x => x.IsDone));

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
            if (z % (cellCountZ / 20) == 0)
            {
                LoadingScreen.Instance.UpdateLoading((i + cellCount) / ((float)cellCount * 2));
                yield return null;
            }
        }
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
            return hit.collider.GetComponentInParent<HexUnit>();
        }
        return null;
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

    public void FindPath(HexCell fromCell, HexCell toCell, int speed)
    {

        // System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        // sw.Start();
        ClearPath();
        currentPathFrom = fromCell;
        currentPathTo = toCell;
        currentPathExists = Search(fromCell, toCell);
        ShowPath(speed);
        // sw.Stop();
        // Debug.Log(sw.ElapsedMilliseconds);
    }

    public List<HexCell> GetPath()
    {
        if (!currentPathExists)
        {
            return null;
        }
        List<HexCell> path = ListPool<HexCell>.Get();
        for (HexCell c = currentPathTo; c != currentPathFrom; c = c.PathFrom)
        {
            path.Add(c);
        }
        path.Add(currentPathFrom);
        path.Reverse();
        return path;
    }

    void ShowPath(int speed)
    {
        if (currentPathExists)
        {
            int turn = 0;
            foreach (HexCell current in GetPath())
            {
                int currentTurn = (turn - 1) / speed;
                current.SetLabel(currentTurn.ToString());
                current.EnableHighlight(Color.white);
                turn++;
            }
        }
        currentPathFrom.EnableHighlight(Color.blue);
        currentPathTo.EnableHighlight(Color.red);
    }

    public void ClearPath()
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
            current.SetLabel(null);
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
    {
        searchFrontierPhase += 2;
        if (searchFrontier == null)
            searchFrontier = new HexCellPriorityQueue();
        else
            searchFrontier.Clear();

        fromCell.EnableHighlight(Color.blue);
        toCell.EnableHighlight(Color.red);

        fromCell.SearchPhase = searchFrontierPhase;
        fromCell.Distance = 0;
        searchFrontier.Enqueue(fromCell);
        while (searchFrontier.Count > 0)
        {
            HexCell current = searchFrontier.Dequeue();
            current.SearchPhase += 1;
            if (current == toCell)
                return true;

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                int distance;
                if (!CanPassPathFind(current, d, out distance))
                    continue;
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

    bool CanPassPathFind(HexCell current, HexDirection d, out int moveCost)
    {
        HexCell neighbor = current.GetNeighbor(d);
        moveCost = current.Distance;
        if (neighbor == null)
            return false;
        if (neighbor.SearchPhase > searchFrontierPhase)
            return false;
        if (neighbor.Unit)
            return false;
        if (neighbor.TerrainType == HexTerrains.HexType.Water)
            return false;
        if (HexFeatureManager.noWalkable.Contains(neighbor.featureManager.currentFeature))
            return false;
        if (neighbor.HasRiver())
            moveCost += 10;

        if (current.HasRoadThroughEdge(d))
            moveCost += 1;
        else
            moveCost += 10;
        int elevationDiff = Mathf.Abs(neighbor.Elevation - current.Elevation);
        if (elevationDiff > 1)
            return false;
        if (elevationDiff == 1)
            moveCost += 5;
        return true;
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