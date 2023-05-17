using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexCell : MonoBehaviour
{
    public int Elevation
    {
        get { return elevation; }
        set
        {
            elevation = value;
            Vector3 position = transform.localPosition;
            position.y = value * HexMetrics.elevationStep;
            transform.localPosition = position;
        }
    }
    int elevation;
    public HexCoordinates coordinates;
    public HexCell[] neighbors;
    MeshCollider meshCollider;
    public HexGridChunk chunk;

    void Awake()
    {
        meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.convex = true;
        meshCollider.sharedMesh = GetComponentInChildren<MeshFilter>().sharedMesh;
    }

    public void ChangeTile(GameObject newTile)
    {
        Destroy(GetComponentInChildren<MeshFilter>().gameObject);
        var tile = Instantiate(newTile, transform.position, transform.rotation);
        tile.GetComponentInChildren<MeshFilter>().transform.parent = gameObject.transform;
        Destroy(tile);
        // Debug.Log("ChangeTile at " + coordinates.ToString());
    }

    public HexCell GetNeighbor(HexDirection direction)
    {
        return neighbors[(int)direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        neighbors[(int)direction] = cell;
        cell.neighbors[(int)direction.Opposite()] = this;
    }

}
