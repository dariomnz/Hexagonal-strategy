using UnityEngine;
using System;

public class NewMapMenu : MonoBehaviour
{
    bool generateMaps = true;
    public HexGrid hexGrid;
    public HexMapGenerator mapGenerator;

    public void Open()
    {
        gameObject.SetActive(true);
        CameraController.Locked = true;
    }

    public void Close()
    {
        gameObject.SetActive(false);
        CameraController.Locked = false;
    }

    void CreateMap(int x, int z)
    {
        if (generateMaps)
            mapGenerator.GenerateMap(x, z);
        else
            hexGrid.CreateMap(x, z);
        Close();
    }

    public void CreateSmallMap()
    {
        CreateMap(4 * HexMetrics.chunkSizeX, 4 * HexMetrics.chunkSizeZ);
    }

    public void CreateMediumMap()
    {
        CreateMap(7 * HexMetrics.chunkSizeX, 7 * HexMetrics.chunkSizeZ);
    }

    public void CreateLargeMap()
    {
        CreateMap(10 * HexMetrics.chunkSizeX, 10 * HexMetrics.chunkSizeZ);
    }

    public void ToggleMapGeneration(bool toggle)
    {
        generateMaps = toggle;
    }
}