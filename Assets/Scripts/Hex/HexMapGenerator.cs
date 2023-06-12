using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using System.Linq;

public class HexMapGenerator : MonoBehaviour
{
    struct ClimateData
    {
        public float clouds, moisture;
    }

    struct Biome
    {
        public HexTerrains.HexType terrain;
        public HexFeatureManager.Features feature;

        public Biome(HexTerrains.HexType terrain, HexFeatureManager.Features feature)
        {
            this.terrain = terrain;
            this.feature = feature;
        }
    }
    static Biome[] biomes = {
        new Biome(HexTerrains.HexType.Sand,HexFeatureManager.Features.None), new Biome(HexTerrains.HexType.Snow,HexFeatureManager.Features.None), new Biome(HexTerrains.HexType.Snow,HexFeatureManager.Features.None), new Biome(HexTerrains.HexType.Snow,HexFeatureManager.Features.None),
        new Biome(HexTerrains.HexType.Sand,HexFeatureManager.Features.None), new Biome(HexTerrains.HexType.Rock,HexFeatureManager.Features.Rock1), new Biome(HexTerrains.HexType.Rock,HexFeatureManager.Features.Rock3), new Biome(HexTerrains.HexType.Rock,HexFeatureManager.Features.Forest2),
        new Biome(HexTerrains.HexType.Sand,HexFeatureManager.Features.None), new Biome(HexTerrains.HexType.Grass,HexFeatureManager.Features.Rock2), new Biome(HexTerrains.HexType.Grass,HexFeatureManager.Features.Forest1), new Biome(HexTerrains.HexType.Grass,HexFeatureManager.Features.Forest2),
        new Biome(HexTerrains.HexType.Sand,HexFeatureManager.Features.None), new Biome(HexTerrains.HexType.Grass,HexFeatureManager.Features.Forest1), new Biome(HexTerrains.HexType.Grass,HexFeatureManager.Features.Forest2), new Biome(HexTerrains.HexType.Grass,HexFeatureManager.Features.Forest3)
    };

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
    [Range(0, 20)]
    public int riverPercentage = 10;
    [Range(0f, 1f)]
    public float lowTemperature = 0f;
    [Range(0f, 1f)]
    public float highTemperature = 1f;
    public enum HemisphereMode
    {
        Both, North, South
    }

    public HemisphereMode hemisphere;
    [Range(0f, 1f)]
    public float temperatureJitter = 0.1f;

    [Range(0f, 1f)]
    public float forestPercentage = 0.6f;
    [Range(0f, 1f)]
    public float rockPercentage = 0.6f;

    static float[] temperatureBands = { 0.1f, 0.3f, 0.6f };

    static float[] moistureBands = { 0.06f, 0.28f, 0.85f };

    HexCellPriorityQueue searchFrontier;
    int searchFrontierPhase;
    int landCells;
    List<HexDirection> flowDirections = new List<HexDirection>();

    List<ClimateData> climate = new List<ClimateData>();
    List<ClimateData> nextClimate = new List<ClimateData>();

    public IEnumerator GenerateMap(int x, int z)
    {
        Random.State originalRandomState = Random.state;
        yield return StartCoroutine(grid.CreateMap(x, z));
        if (searchFrontier == null)
        {
            searchFrontier = new HexCellPriorityQueue();
        }
        for (int i = 0; i < grid.cellCount; i++)
        {
            grid.GetCell(i).waterLevel = waterLevel;
        }

        CreateLand();
        ErodeLand();
        CreateClimate();
        CreateRivers();
        SetTerrainType();
        for (int i = 0; i < grid.cellCount; i++)
        {
            grid.GetCell(i).SearchPhase = 0;
        }
        Random.state = originalRandomState;
        yield return null;
    }

    void CreateRivers()
    {
        List<HexCell> riverOrigins = ListPool<HexCell>.Get();
        for (int i = 0; i < grid.cellCount; i++)
        {
            HexCell cell = grid.GetCell(i);
            if (cell.IsUnderwater)
                continue;
            ClimateData data = climate[i];
            float weight =
                data.moisture * (cell.Elevation - waterLevel) /
                (elevationMaximum - waterLevel);
            if (weight > 0.75f)
            {
                riverOrigins.Add(cell);
                riverOrigins.Add(cell);
            }
            if (weight > 0.5f)
                riverOrigins.Add(cell);
            if (weight > 0.25f)
                riverOrigins.Add(cell);
        }
        int riverBudget = Mathf.RoundToInt(landCells * riverPercentage * 0.01f);
        while (riverBudget > 0 && riverOrigins.Count > 0)
        {
            int index = Random.Range(0, riverOrigins.Count);
            int lastIndex = riverOrigins.Count - 1;
            HexCell origin = riverOrigins[index];
            riverOrigins[index] = riverOrigins[lastIndex];
            riverOrigins.RemoveAt(lastIndex);
            if (!origin.HasRiver())
            {
                bool isValidOrigin = true;
                for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    HexCell neighbor = origin.GetNeighbor(d);
                    if (neighbor && (neighbor.HasRiver() || neighbor.IsUnderwater))
                    {
                        isValidOrigin = false;
                        break;
                    }
                }
                if (isValidOrigin)
                    riverBudget -= CreateRiver(origin);
            }
        }

        if (riverBudget > 0)
        {
            Debug.LogWarning("Failed to use up river budget.");
        }
        ListPool<HexCell>.Release(riverOrigins);
    }

    int CreateRiver(HexCell origin)
    {
        int length = 1;
        HexCell cell = origin;
        HexDirection direction = HexDirection.NE;
        while (!cell.IsUnderwater)
        {
            int minNeighborElevation = int.MaxValue;
            flowDirections.Clear();
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = cell.GetNeighbor(d);
                if (!neighbor)
                    continue;
                if (neighbor.Elevation < minNeighborElevation)
                    minNeighborElevation = neighbor.Elevation;
                if (neighbor == origin || neighbor.hasIncomingRiver)
                    continue;
                int delta = neighbor.Elevation - cell.Elevation;
                if (delta > 0)
                    continue;
                if (neighbor.hasOutgoingRiver)
                {
                    cell.SetOutgoingRiver(d);
                    return length;
                }
                if (delta < 0)
                {
                    flowDirections.Add(d);
                    flowDirections.Add(d);
                    flowDirections.Add(d);
                }
                if (length == 1 ||
                    (d != direction.Next2() && d != direction.Previous2()))
                {
                    flowDirections.Add(d);
                }
                flowDirections.Add(d);
            }
            if (flowDirections.Count == 0)
            {
                if (length == 1)
                    return 0;

                if (minNeighborElevation >= cell.Elevation)
                {
                    cell.waterLevel = minNeighborElevation;
                    cell.waterDeep = 1;
                    cell.isLake = true;
                    if (minNeighborElevation == cell.Elevation)
                        cell.Elevation = minNeighborElevation;
                }
                break;
            }
            direction = flowDirections[Random.Range(0, flowDirections.Count)];
            cell.SetOutgoingRiver(direction);
            length += 1;
            cell = cell.GetNeighbor(direction);
        }
        return length;
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

    void CreateLand()
    {
        int totalLandBudget = Mathf.RoundToInt(grid.cellCount * landPercentage * 0.01f);
        landCells = totalLandBudget;
        int landBudget = totalLandBudget;
        while (landBudget > 0)
        {
            int chunkSize = Random.Range(chunkSizeMin, chunkSizeMax - 1);
            if (Random.value < sinkProbability)
                landBudget = SinkTerrain(chunkSize, landBudget);
            else
                landBudget = RaiseTerrain(chunkSize, landBudget);
        }
        landCells -= landBudget;
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

        int rockDesertElevation = elevationMaximum - (elevationMaximum - waterLevel) / 2;
        for (int i = 0; i < grid.cellCount; i++)
        {
            HexCell cell = grid.GetCell(i);
            float temperature = DetermineTemperature(cell);
            float moisture = climate[i].moisture;
            cell.temperature = temperature;
            cell.moisture = moisture;
            if (!cell.IsUnderwater)
            {
                int t = 0;
                for (; t < temperatureBands.Length; t++)
                    if (temperature < temperatureBands[t])
                        break;
                int m = 0;
                for (; m < moistureBands.Length; m++)
                    if (moisture < moistureBands[m])
                        break;
                Biome cellBiome = biomes[t * 4 + m];
                if (cellBiome.terrain == 0)
                {
                    if (cell.Elevation >= rockDesertElevation)
                        cellBiome.terrain = HexTerrains.HexType.Rock;
                }
                // else if (cell.Elevation == elevationMaximum)
                // cellBiome.terrain = HexTerrains.HexType.Snow;

                cell.TerrainType = cellBiome.terrain;
                if (!cell.HasRiver())
                {
                    float rand = Random.Range(0f, 1f);
                    if (HexFeatureManager.forests.Contains(cellBiome.feature))
                        if (forestPercentage < rand)
                            continue;

                    if (HexFeatureManager.rocks.Contains(cellBiome.feature))
                        if (rockPercentage < rand)
                            continue;
                    cell.featureManager.AddFeature(cellBiome.feature, randomRotation: true);
                }
            }
            else
            {
                HexTerrains.HexType terrain;
                if (cell.Elevation == waterLevel - 1 || cell.Elevation == waterLevel - 2)
                    terrain = HexTerrains.HexType.Sand;
                else
                    terrain = HexTerrains.HexType.Rock;
                if (!cell.isLake)
                {
                    cell.waterDeep = waterLevel - cell.Elevation;
                    cell.Elevation = waterLevel;
                }
                cell.TerrainType = HexTerrains.HexType.Water;
                cell.terrainWaterFloorType = terrain;
                cell.UpdateMesh();
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

    float DetermineTemperature(HexCell cell)
    {
        float latitude = (float)cell.coordinates.Z / grid.cellCountZ;
        if (hemisphere == HemisphereMode.Both)
        {
            latitude *= 2f;
            if (latitude > 1f)
            {
                latitude = 2f - latitude;
            }
        }
        else if (hemisphere == HemisphereMode.North)
        {
            latitude = 1f - latitude;
        }

        float temperature = Mathf.LerpUnclamped(lowTemperature, highTemperature, latitude);
        temperature *= 1f - (cell.Elevation - waterLevel) / (elevationMaximum - waterLevel + 1f);
        temperature += (Mathf.PerlinNoise(cell.transform.position.x * 0.1f, cell.transform.position.z * 0.1f) * 2f - 1f) * temperatureJitter;

        return temperature;
    }
}