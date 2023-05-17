using UnityEngine;
using UnityEngine.UI;

public class HexGridChunk : MonoBehaviour
{
    HexCell[] cells;
    public HexGrid hexGrid;

    public HexGridChunk()
    {
        cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
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
}