using System;
using UnityEngine;
using TMPro;

public class HexMapEditor : MonoBehaviour
{
    public GameObject[] tilesPrefabs;

    public HexGrid hexGrid;

    GameObject activeTilePrefab;
    int activeElevation;
    int brushSize;
    bool applyTile = true;
    bool applyElevation = false;

    void Awake()
    {
        SelectTile(0);
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            HandleInput();
        }
    }

    void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit))
        {
            EditCells(hexGrid.GetCell(hit.point));
            // Debug.Log(HexCoordinates.FromPosition(hit.point));
            // hexGrid.ChangeTile(hit.collider.gameObject.GetComponent<HexCell>(), activeTilePrefab);
            // hexGrid. ChangeTile(HexCoordinates.FromPosition(hit.point), activeTilePrefab);
        }
    }

    void EditCells(HexCell center)
    {
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
        if (applyTile)
        {
            cell.ChangeTile(activeTilePrefab);
        }
        if (applyElevation)
        {
            cell.Elevation = activeElevation;
        }
    }

    public void SelectTile(int index)
    {
        applyTile = index >= 0;
        if (applyTile)
        {
            activeTilePrefab = tilesPrefabs[index];
        }
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

    public void ShowUI(bool visible)
    {
        foreach (HexCell cell in hexGrid.cells)
        {
            cell.GetComponentInChildren<TextMeshProUGUI>(true)?.gameObject.SetActive(visible);
        }
    }
}