using UnityEngine;
using UnityEngine.UI;
using System;

public class HexGridChunk : MonoBehaviour
{
    HexCell[] cells;
    public HexGrid hexGrid;
    public Canvas gridCanvas;

    public HexGridChunk()
    {
        cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
    }

    public void AddCell(int index, HexCell cell)
    {
        cells[index] = cell;
        cell.chunk = this;
    }

    public void ShowUI(bool visible)
    {
        gridCanvas.gameObject.SetActive(visible);
    }
}