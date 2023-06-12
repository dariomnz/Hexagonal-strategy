using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Pool;

static class HexSearch
{

    static HexCellPriorityQueue searchFrontier;
    static int searchFrontierPhase;
    static HexCell currentPathFrom, currentPathTo;
    static bool currentPathExists;
    public static bool HasPath { get { return currentPathExists; } }
    
    public static void FindPath(HexCell fromCell, HexCell toCell, int speed)
    {

        // System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        // sw.Start();
        ClearPath();
        currentPathFrom = fromCell;
        currentPathTo = toCell;
        currentPathExists = HexSearch.Search(fromCell, toCell);
        ShowPath(speed);
        // sw.Stop();
        // Debug.Log(sw.ElapsedMilliseconds);
    }

    public static List<HexCell> GetPath()
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

    static void ShowPath(int speed)
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

    public static void ClearPath()
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

    public static bool Search(HexCell fromCell, HexCell toCell)
    {
        searchFrontierPhase += 2;
        if (searchFrontier == null)
            searchFrontier = new HexCellPriorityQueue();
        else
            searchFrontier.Clear();

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

    static bool CanPassPathFind(HexCell current, HexDirection d, out int moveCost)
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

    public static List<HexCell> CellsInCircle(HexCell center, int radius)
    {
        if (searchFrontier == null)
            searchFrontier = new HexCellPriorityQueue();
        else
            searchFrontier.Clear();

        List<HexCell> outCells = ListPool<HexCell>.Get();
        if (center == null)
            return null;

        searchFrontierPhase += 2;
        center.SearchPhase = searchFrontierPhase;
        center.Distance = 0;
        center.SearchHeuristic = 0;
        searchFrontier.Enqueue(center);
        HexCoordinates centerCoor = center.coordinates;
        int size = 0;
        int totalCells = 1;
        for (int i = 0; i < radius + 1; i++)
        {
            totalCells += 6 * i;
        }
        while (size < totalCells && searchFrontier.Count > 0)
        {
            HexCell current = searchFrontier.Dequeue();
            outCells.Add(current);
            size += 1;
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor && neighbor.SearchPhase < searchFrontierPhase)
                {
                    neighbor.SearchPhase = searchFrontierPhase;
                    neighbor.Distance = neighbor.coordinates.DistanceTo(centerCoor);
                    neighbor.SearchHeuristic = 0;
                    searchFrontier.Enqueue(neighbor);
                }
            }
        }
        searchFrontier.Clear();
        return outCells;
    }

}