using UnityEngine;
using System.Collections;

public class NewMapMenu : MonoBehaviour
{
    bool generateMaps = true;
    public HexGrid hexGrid;
    public HexMapGenerator mapGenerator;

    public void Open()
    {
        // gameObject.SetActive(true);
        GetComponent<Canvas>().enabled = true;
        CameraController.Locked = true;
    }

    public void Close()
    {
        // gameObject.SetActive(false);
        GetComponent<Canvas>().enabled = false;
        CameraController.Locked = false;
    }

    IEnumerator CreateMap(int x, int z)
    {
        Close();
        if (generateMaps)
            yield return StartCoroutine(mapGenerator.GenerateMap(x, z));
        else
            yield return StartCoroutine(hexGrid.CreateMap(x, z));
    }

    public void CreateSmallMap()
    {
        StartCoroutine(CreateMap(4 * HexMetrics.chunkSizeX, 4 * HexMetrics.chunkSizeZ));
    }

    public void CreateMediumMap()
    {
        StartCoroutine(CreateMap(8 * HexMetrics.chunkSizeX, 8 * HexMetrics.chunkSizeZ));
    }

    public void CreateLargeMap()
    {
        StartCoroutine(CreateMap(12 * HexMetrics.chunkSizeX, 12 * HexMetrics.chunkSizeZ));
    }

    public void ToggleMapGeneration(bool toggle)
    {
        generateMaps = toggle;
    }
}