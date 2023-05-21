using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class HexMapGenerator : MonoBehaviour
{
    struct ClimateData
    {
        public float clouds, moisture;
    }

    public HexGrid grid;
    public bool useFixedSeed;
    public int seed;
    [Range(0f, 0.5f)]
    public float jitterProbability = 0.25f;
    [Range(20, 200)]
    public int chunkSizeMin = 30;
    [Range(20, 200)]
    public int chunkSizeMax = 100;
    [Range(0f, 1f)]
    public float highRiseProbability = 0.25f;
    [Range(0f, 0.4f)]
    public float sinkProbability = 0.2f;
    [Range(5, 95)]
    public int landPercentage = 50;
    [Range(1, 5)]
    public int waterLevel = 3;
    [Range(-4, 0)]
    public int elevationMinimum = -2;
    [Range(6, 10)]
    public int elevationMaximum = 8;
    [Range(0, 100)]
    public int erosionPercentage = 50;
    [Range(0f, 1f)]
    public float startingMoisture = 0.1f;
    [Range(0f, 1f)]
    public float evaporationFactor = 0.5f;
    [Range(0f, 1f)]
    public float precipitationFactor = 0.25f;
    [Range(0f, 1f)]
    public float runoffFactor = 0.25f;
    [Range(0f, 1f)]
    public float seepageFactor = 0.125f;
    public HexDirection windDirection = HexDirection.NW;
    [Range(1f, 10f)]
    public float windStrength = 4f;

    HexCellPriorityQueue searchFrontier;
    int searchFrontierPhase;

    List<ClimateData> climate = new List<ClimateData>();
    List<ClimateData> nextClimate = new List<ClimateData>();

    public IEnumerator GenerateMap(int x, int z)
    {
        Random.State originalRandomState = Random.state;
        if (!useFixedSeed)
        {
            seed = Random.Range(0, int.MaxValue);
            seed ^= (int)System.DateTime.Now.Ticks;
            seed ^= (int)Time.unscaledTime;
            seed &= int.MaxValue;
        }
        Random.InitState(seed);
        yield return StartCoroutine(grid.CreateMap(x, z));
        if (searchFrontier == null)
        {
            searchFrontier = new HexCellPriorityQueue();
        }
        for (int i = 0; i < grid.cellCount; i++)
        {
            grid.GetCell(i).waterLevel = waterLevel;
        }

        yield return StartCoroutine(CreateLand());
        ErodeLand();
        CreateClimate();
        SetTerrainType();
        for (int i = 0; i < grid.cellCount; i++)
        {
            grid.GetCell(i).SearchPhase = 0;
        }
        Random.state = originalRandomState;
    }
    void CreateClimate()
    {
        climate.Clear();
        nextClimate.Clear();
        ClimateData initialData = new ClimateData();
        initialData.moisture = startingMoisture;
        ClimateData clearData = new ClimateData();
        for (int i = 0; i < grid.cellCount; i++)
        {
            climate.Add(initialData);
            nextClimate.Add(clearData);
        }
        for (int cycle = 0; cycle < 40; cycle++)
        {
            for (int i = 0; i < grid.cellCount; i++)
            {
                EvolveClimate(i);
            }
            List<ClimateData> swap = climate;
            climate = nextClimate;
            nextClimate = swap;
        }
    }

    void EvolveClimate(int cellIndex)
    {
        HexCell cell = grid.GetCell(cellIndex);
        ClimateData cellClimate = climate[cellIndex];

        if (cell.IsUnderwater)
        {
            cellClimate.moisture = 1f;
            cellClimate.clouds += evaporationFactor;
        }
        else
        {
            float evaporation = cellClimate.moisture * evaporationFactor;
            cellClimate.moisture -= evaporation;
            cellClimate.clouds += evaporation;
        }
        float precipitation = cellClimate.clouds * precipitationFactor;
        cellClimate.clouds -= precipitation;
        cellClimate.moisture += precipitation;
        float cloudMaximum = 1f - cell.Elevation / (elevationMaximum + 1f);
        if (cellClimate.clouds > cloudMaximum)
        {
            cellClimate.moisture += cellClimate.clouds - cloudMaximum;
            cellClimate.clouds = cloudMaximum;
        }
        HexDirection mainDispersalDirection = windDirection.Opposite();
        float cloudDispersal = cellClimate.clouds * (1f / (5f + windStrength));
        float runoff = cellClimate.moisture * runoffFactor * (1f / 6f);
        float seepage = cellClimate.moisture * seepageFactor * (1f / 6f);
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            HexCell neighbor = cell.GetNeighbor(d);
            if (!neighbor)
            {
                continue;
            }
            ClimateData neighborClimate = nextClimate[neighbor.Index];
            if (d == mainDispersalDirection)
            {
                neighborClimate.clouds += cloudDispersal * windStrength;
            }
            else
            {
                neighborClimate.clouds += cloudDispersal;
            }
            int elevationDelta = neighbor.Elevation - cell.Elevation;
            if (elevationDelta < 0)
            {
                cellClimate.moisture -= runoff;
                neighborClimate.moisture += runoff;
            }
            else if (elevationDelta == 0)
            {
                cellClimate.moisture -= seepage;
                neighborClimate.moisture += seepage;
            }
            nextClimate[neighbor.Index] = neighborClimate;
        }
        cellClimate.clouds = 0f;

        ClimateData nextCellClimate = nextClimate[cellIndex];
        nextCellClimate.moisture += cellClimate.moisture;
        if (nextCellClimate.moisture > 1f)
        {
            nextCellClimate.moisture = 1f;
        }
        nextClimate[cellIndex] = nextCellClimate;
        climate[cellIndex] = new ClimateData();
    }

    IEnumerator CreateLand()
    {
        LoadingScreen.Instance.Open();
        int totalLandBudget = Mathf.RoundToInt(grid.cellCount * landPercentage * 0.01f);
        int landBudget = totalLandBudget;
        while (landBudget > 0)
        {
            int chunkSize = Random.Range(chunkSizeMin, chunkSizeMax - 1);
            if (Random.value < sinkProbability)
                landBudget = SinkTerrain(chunkSize, landBudget);
            else
                landBudget = RaiseTerrain(chunkSize, landBudget);

            LoadingScreen.Instance.UpdateLoading((totalLandBudget - landBudget) / (float)totalLandBudget);
            yield return null;
        }
        LoadingScreen.Instance.Close();
    }
    void ErodeLand()
    {
        List<HexCell> erodibleCells = ListPool<HexCell>.Get();
        for (int i = 0; i < grid.cellCount; i++)
        {
            HexCell cell = grid.GetCell(i);
            if (IsErodible(cell))
            {
                erodibleCells.Add(cell);
            }
        }
        int targetErodibleCount =
            (int)(erodibleCells.Count * (100 - erosionPercentage) * 0.01f);

        while (erodibleCells.Count > targetErodibleCount)
        {
            int index = Random.Range(0, erodibleCells.Count);
            HexCell cell = erodibleCells[index];
            HexCell targetCell = GetErosionTarget(cell);

            cell.Elevation -= 1;
            targetCell.Elevation += 1;
            if (!IsErodible(cell))
            {
                erodibleCells[index] = erodibleCells[erodibleCells.Count - 1];
                erodibleCells.RemoveAt(erodibleCells.Count - 1);
            }
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = cell.GetNeighbor(d);
                if (
                    neighbor && neighbor.Elevation == cell.Elevation + 2 &&
                    !erodibleCells.Contains(neighbor)
                )
                {
                    erodibleCells.Add(neighbor);
                }
            }
            if (IsErodible(targetCell) && !erodibleCells.Contains(targetCell))
            {
                erodibleCells.Add(targetCell);
            }
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = targetCell.GetNeighbor(d);
                if (
                    neighbor && neighbor != cell &&
                    neighbor.Elevation == targetCell.Elevation + 1 &&
                    !IsErodible(neighbor)
                )
                {
                    erodibleCells.Remove(neighbor);
                }
            }
        }

        ListPool<HexCell>.Release(erodibleCells);
    }

    HexCell GetErosionTarget(HexCell cell)
    {
        List<HexCell> candidates = ListPool<HexCell>.Get();
        int erodibleElevation = cell.Elevation - 2;
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            HexCell neighbor = cell.GetNeighbor(d);
            if (neighbor && neighbor.Elevation <= erodibleElevation)
            {
                candidates.Add(neighbor);
            }
        }

        HexCell target = candidates[Random.Range(0, candidates.Count)];
        ListPool<HexCell>.Release(candidates);
        return target;
    }

    int RaiseTerrain(int chunkSize, int budget)
    {
        searchFrontierPhase += 1;
        HexCell firstCell = GetRandomCell();
        firstCell.SearchPhase = searchFrontierPhase;
        firstCell.Distance = 0;
        firstCell.SearchHeuristic = 0;
        searchFrontier.Enqueue(firstCell);
        HexCoordinates center = firstCell.coordinates;
        int rise = Random.value < highRiseProbability ? 2 : 1;
        int size = 0;
        while (size < chunkSize && searchFrontier.Count > 0)
        {
            HexCell current = searchFrontier.Dequeue();
            int originalElevation = current.Elevation;
            int newElevation = originalElevation + rise;
            if (newElevation > elevationMaximum)
                continue;
            current.Elevation = newElevation;
            if (originalElevation < waterLevel && newElevation >= waterLevel && --budget == 0)
                break;
            size += 1;

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor && neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = neighbor.coordinates.DistanceTo(center);
                    neighbor.SearchHeuristic = Random.value < jitterProbability ? 1 : 0;
                    searchFrontier.Enqueue(neighbor);
                }
            }
        }
        searchFrontier.Clear();
        return budget;
    }


    int SinkTerrain(int chunkSize, int budget)
    {
        searchFrontierPhase += 1;
        HexCell firstCell = GetRandomCell();
        firstCell.SearchPhase = searchFrontierPhase;
        firstCell.Distance = 0;
        firstCell.SearchHeuristic = 0;
        searchFrontier.Enqueue(firstCell);
        HexCoordinates center = firstCell.coordinates;
        int sink = Random.value < highRiseProbability ? 2 : 1;
        int size = 0;
        while (size < chunkSize && searchFrontier.Count > 0)
        {
            HexCell current = searchFrontier.Dequeue();
            int originalElevation = current.Elevation;
            int newElevation = current.Elevation - sink;
            if (newElevation < elevationMinimum)
                continue;
            current.Elevation = newElevation;
            if (originalElevation >= waterLevel && newElevation < waterLevel)
                budget += 1;
            size += 1;

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor && neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = neighbor.coordinates.DistanceTo(center);
                    neighbor.SearchHeuristic = Random.value < jitterProbability ? 1 : 0;
                    searchFrontier.Enqueue(neighbor);
                }
            }
        }
        searchFrontier.Clear();
        return budget;
    }

    void SetTerrainType()
    {
        for (int i = 0; i < grid.cellCount; i++)
        {
            HexCell cell = grid.GetCell(i);
            float moisture = climate[i].moisture;
            if (!cell.IsUnderwater)
                if (moisture < 0.05f)
                {
                    cell.TerrainType = HexTerrains.HexType.Snow;
                }
                else if (moisture < 0.12f)
                {
                    cell.TerrainType = HexTerrains.HexType.Rock;
                }
                else if (moisture < 0.28f)
                {
                    cell.TerrainType = HexTerrains.HexType.Grass;
                }
                else if (moisture < 0.6f)
                {
                    cell.TerrainType = HexTerrains.HexType.Grass;
                }
                else
                {
                    cell.TerrainType = HexTerrains.HexType.Sand;
                }
            else
            {
                cell.TerrainType = HexTerrains.HexType.Water;
                cell.Elevation = waterLevel;
            }

        }
    }

    HexCell GetRandomCell()
    {
        return grid.GetCell(Random.Range(0, grid.cellCount));
    }

    bool IsErodible(HexCell cell)
    {
        int erodibleElevation = cell.Elevation - 2;
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            HexCell neighbor = cell.GetNeighbor(d);
            if (neighbor && neighbor.Elevation <= erodibleElevation)
            {
                return true;
            }
        }
        return false;
    }
}