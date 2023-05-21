using UnityEngine;
using UnityEngine.UI;
using System;

public class HexGridChunk : MonoBehaviour
{
    HexCell[] cells;
    public HexGrid hexGrid;
    public Canvas gridCanvas;

    public int chunkColumnIndex {get;set;}
    public int chunkRowIndex {get;set;}

    public HexGridChunk()
    {
        cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
    }

    public void Awake()
    {
        ShowUI(false);
    }

    public void AddCell(int index, HexCell cell)
    {
        cells[index] = cell;

        if (index == 0)
        {
            Vector3 pos = cell.transform.position;
            pos.y = 0;
            transform.position = pos;
        }
        cell.chunk = this;
        cell.transform.SetParent(transform);
    }

    public void ShowUI(bool visible)
    {
        gridCanvas.gameObject.SetActive(visible);
    }
}