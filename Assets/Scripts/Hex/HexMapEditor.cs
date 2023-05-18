using System;
using UnityEngine;
using TMPro;
using System.IO;

public class HexMapEditor : MonoBehaviour
{
    public HexGrid hexGrid;

    GameObject activeTilePrefab;
    int activeElevation;
    int brushSize;
    bool applyElevation = false;
    HexMetrics.HexType activeTerrainType;

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

        if (activeTerrainType >= 0)
        {
            cell.TerrainType = activeTerrainType;
        }
        if (applyElevation)
        {
            cell.Elevation = activeElevation;
        }
    }

    public void SetTerrainTypeIndex(int index)
    {
        activeTerrainType = (HexMetrics.HexType)index;
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

    public void Save()
    {
        Debug.Log(Application.persistentDataPath);
        string path = Path.Combine(Application.persistentDataPath, "test.map");
        using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))
        {
            writer.Write(0);
            hexGrid.Save(writer);
        }
    }

    public void Load()
    {
        string path = Path.Combine(Application.persistentDataPath, "test.map");
        using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
        {
            int header = reader.ReadInt32();
            if (header == 0)
            {
                hexGrid.Load(reader);
            }
            else
            {
                Debug.LogWarning("Unknown map format " + header);
            }
        }
    }
}