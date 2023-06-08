using UnityEngine.EventSystems;
using UnityEngine;
using TMPro;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Pool;

public class HexMapEditor : MonoBehaviour
{
    public HexGrid hexGrid;

    // GameObject activeTilePrefab;
    int activeElevation;
    int brushSize;
    bool applyElevation = false;
    HexTerrains.HexType activeTerrainType = HexTerrains.HexType.None;
    bool applyFeature = false;
    HexFeatureManager.Features activeFeature;
    enum OptionalToggle
    {
        Ignore, Yes, No
    }

    OptionalToggle roadMode;
    OptionalToggle riverMode;
    bool isDrag;
    HexDirection dragDirection;
    HexCell currentCell, previousCell;
    public HexUnit unitPrefab;

    HexCellPriorityQueue searchFrontier;
    int searchFrontierPhase;

    void Awake()
    {
        SetEditMode(false);
    }

    void Update()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            CalculeCurrentPreviousCell();
            if (Input.GetMouseButton(0))
            {
                HandleInput();
                return;
            }
            if (Input.GetKeyDown(KeyCode.U))
            {
                if (Input.GetKey(KeyCode.LeftShift))
                    DestroyUnit();
                else
                    CreateUnit();
                return;
            }
        }
        // previousCell = null;
    }

    void CalculeCurrentPreviousCell()
    {
        currentCell = GetCellUnderCursor();
        if (currentCell)
        {
            if (previousCell && previousCell != currentCell)
            {
                UpdateHighlight();
                ValidateDrag(currentCell);
            }
            else
                isDrag = false;
            previousCell = currentCell;
        }
        else
            previousCell = null;
    }

    void HandleInput()
    {
        if (currentCell)
            EditCells(currentCell);
    }

    void CreateUnit()
    {
        HexCell cell = GetCellUnderCursor();
        if (cell && !cell.Unit)
        {
            hexGrid.AddUnit(Instantiate(HexMetrics.Instance.hexUnits.unitsPrefabs[HexUnits.UnitType.Knight]), cell, Random.Range(0f, 360f));
        }
    }

    void DestroyUnit()
    {
        HexCell cell = GetCellUnderCursor();
        if (cell && cell.Unit)
        {
            hexGrid.RemoveUnit(cell.Unit);
        }
    }

    HexCell GetCellUnderCursor()
    {
        HexCell cell = hexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
        if (!cell)
            cell = GetUnitUnderCursor()?.Location;
        return cell;
    }

    HexUnit GetUnitUnderCursor()
    {
        return hexGrid.GetUnit(Camera.main.ScreenPointToRay(Input.mousePosition));
    }

    void ValidateDrag(HexCell currentCell)
    {
        for (dragDirection = HexDirection.NE; dragDirection <= HexDirection.NW; dragDirection++)
        {
            if (previousCell.GetNeighbor(dragDirection) == currentCell)
            {
                isDrag = true;
                return;
            }
        }
        isDrag = false;
    }

    void EditCells(HexCell center)
    {
        foreach (HexCell hexCell in hexGrid.CellsInCircle(center, brushSize))
        {
            EditCell(hexCell);
        }
    }

    void EditCell(HexCell cell)
    {
        if (cell == null)
            return;

        if (activeTerrainType >= 0)
            cell.TerrainType = activeTerrainType;
        if (applyElevation)
            cell.Elevation = activeElevation;
        if (applyFeature)
        {
            if (activeFeature == HexFeatureManager.Features.None)
                cell.featureManager.Clear();
            else
                cell.featureManager.AddFeature(activeFeature);
        }
        if (riverMode == OptionalToggle.No)
            cell.RemoveRiver();
        if (roadMode == OptionalToggle.No)
            cell.RemoveRoads();
        if (isDrag)
        {
            HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
            if (otherCell)
            {
                if (roadMode == OptionalToggle.Yes)
                    otherCell.AddRoad(dragDirection);
                if (riverMode == OptionalToggle.Yes)
                    otherCell.SetOutgoingRiver(dragDirection);
            }

        }
    }

    void UpdateHighlight()
    {
        ClearHighlight();
        if (currentCell)
            foreach (HexCell hexCell in hexGrid.CellsInCircle(currentCell, brushSize))
            {
                hexCell?.EnableHighlight(Color.white);
            }
    }

    void ClearHighlight()
    {
        if (previousCell)
            foreach (HexCell hexCell in hexGrid.CellsInCircle(previousCell, brushSize))
            {
                hexCell?.DisableHighlight();
            }
    }

    public void SetTerrainTypeIndex(int index)
    {
        activeTerrainType = (HexTerrains.HexType)index;
    }

    public void SetBrushSize(float size)
    {
        ClearHighlight();
        brushSize = (int)size;
    }

    public void SetElevation(float elevation)
    {
        activeElevation = (int)elevation;
    }

    public void SetApplyElevation(bool toggle)
    {
        applyElevation = toggle;
    }

    public void SetApplyFeature(bool toggle)
    {
        applyFeature = toggle;
    }

    public void SetRoadMode(int mode)
    {
        roadMode = (OptionalToggle)mode;
    }

    public void SetRiverMode(int mode)
    {
        riverMode = (OptionalToggle)mode;
    }
    public void SetFeature(int index)
    {
        activeFeature = (HexFeatureManager.Features)index;
    }

    public void SetEditMode(bool toggle)
    {
        // editMode = toggle;
        ClearHighlight();
        enabled = toggle;
    }
}