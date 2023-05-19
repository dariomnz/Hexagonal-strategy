using System;
using UnityEngine.EventSystems;
using UnityEngine;
using TMPro;
using System.IO;

public class HexMapEditor : MonoBehaviour
{
    public HexGrid hexGrid;

    // GameObject activeTilePrefab;
    int activeElevation;
    int brushSize;
    bool applyElevation = false;
    HexTerrains.HexType activeTerrainType;
    bool applyFeature = false;
    HexFeatureManager.Features activeFeature;
    enum OptionalToggle
    {
        Ignore, Yes, No
    }

    OptionalToggle roadMode;
    bool isDrag;
    HexDirection dragDirection;
    HexCell previousCell;
    bool editMode;

    void Update()
    {
        if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
            HandleInput();
        else
            previousCell = null;
    }

    void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit))
        {
            HexCell currentCell = hexGrid.GetCell(hit.point);
            if (previousCell && previousCell != currentCell)
                ValidateDrag(currentCell);
            else
                isDrag = false;
            if (editMode)
                EditCells(currentCell);
            else
                hexGrid.FindDistancesTo(currentCell);

            previousCell = currentCell;
        }
        else
            previousCell = null;
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
        if (center == null)
            return;

        int centerX = center.coordinates.X;
        int centerZ = center.coordinates.Z;

        for (int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++)
        {
            for (int x = centerX - r; x <= centerX + brushSize; x++)
            {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
        for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++)
        {
            for (int x = centerX - brushSize; x <= centerX + r; x++)
            {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
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
                cell.featureManager.AddFeature(cell, activeFeature);
        }
        if (roadMode == OptionalToggle.No)
            cell.RemoveRoads();
        if (isDrag)
        {
            HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
            if (otherCell)
                if (roadMode == OptionalToggle.Yes)
                    otherCell.AddRoad(dragDirection);

        }
    }

    public void SetTerrainTypeIndex(int index)
    {
        activeTerrainType = (HexTerrains.HexType)index;
    }

    public void SetBrushSize(float size)
    {
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

    public void SetFeature(int index)
    {
        activeFeature = (HexFeatureManager.Features)index;
    }

    public void ShowUI(bool visible)
    {
        hexGrid.ShowUI(visible);
    }

    public void SetEditMode(bool toggle)
    {
        editMode = toggle;
    }
}