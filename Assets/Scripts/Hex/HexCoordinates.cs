using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct HexCoordinates
{
    [SerializeField]
    private int x, z;

    public int X { get { return x; } }
    public int Y { get { return -X - Z; } }
    public int Z { get { return z; } }

    public HexCoordinates(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    public static HexCoordinates FromOffsetCoordinates(int x, int z)
    {
        return new HexCoordinates(x - z / 2, z);
    }

    public static HexCoordinates FromPosition(Vector3 position)
    {
        float x = (position.x - position.z / (HexMetrics.zDiameter)) / (HexMetrics.xDiameter);
        float y = position.z / (HexMetrics.zDiameter);
        int iX = Mathf.RoundToInt(x);
        int iY = Mathf.RoundToInt(y);
        int iZ = Mathf.RoundToInt(-x - y);
        if (iX + iY + iZ != 0)
        {
            float dX = Mathf.Abs(x - iX);
            float dY = Mathf.Abs(y - iY);
            float dZ = Mathf.Abs(-x - y - iZ);
            if (dX > dY && dX > dZ)
            {
                iX = -iY - iZ;
            }
            else if (dZ > dY)
            {
                iZ = -iX - iY;
            }
        }
        return new HexCoordinates(iX, iY);
    }

    public int DistanceTo(HexCoordinates other)
    {
        // TODO: Fix distance when warp to top or button
        int xyz = (x < other.x ? other.x - x : x - other.x) +
                  (Y < other.Y ? other.Y - Y : Y - other.Y) +
                  (z < other.z ? other.z - z : z - other.z);

        other.x += HexMetrics.cellSizeX;
        // other.z += HexMetrics.cellSizeZ;

        int xyzWrapped = (x < other.x ? other.x - x : x - other.x) +
                         (Y < other.Y ? other.Y - Y : Y - other.Y) +
                         (z < other.z ? other.z - z : z - other.z);
        if (xyzWrapped < xyz)
        {
            xyz = xyzWrapped;
        }
        else
        {
            other.x -= 2 * HexMetrics.cellSizeX;
            // other.z -= 2 * HexMetrics.cellSizeZ;
            xyzWrapped = (x < other.x ? other.x - x : x - other.x) +
                         (Y < other.Y ? other.Y - Y : Y - other.Y) +
                         (z < other.z ? other.z - z : z - other.z);
            if (xyzWrapped < xyz)
            {
                xyz = xyzWrapped;
            }
        }

        // other.x += HexMetrics.cellSizeX;
        other.z += HexMetrics.cellSizeZ;

        xyzWrapped = (x < other.x ? other.x - x : x - other.x) +
                     (Y < other.Y ? other.Y - Y : Y - other.Y) +
                     (z < other.z ? other.z - z : z - other.z);
        if (xyzWrapped < xyz)
        {
            xyz = xyzWrapped;
        }
        else
        {
            // other.x -= 2 * HexMetrics.cellSizeX;
            other.z -= 2 * HexMetrics.cellSizeZ;
            xyzWrapped = (x < other.x ? other.x - x : x - other.x) +
                         (Y < other.Y ? other.Y - Y : Y - other.Y) +
                         (z < other.z ? other.z - z : z - other.z);
            if (xyzWrapped < xyz)
            {
                xyz = xyzWrapped;
            }
        }

        other.x += HexMetrics.cellSizeX;
        other.z += HexMetrics.cellSizeZ;

        xyzWrapped = (x < other.x ? other.x - x : x - other.x) +
                     (Y < other.Y ? other.Y - Y : Y - other.Y) +
                     (z < other.z ? other.z - z : z - other.z);
        if (xyzWrapped < xyz)
        {
            xyz = xyzWrapped;
        }
        else
        {
            other.x -= 2 * HexMetrics.cellSizeX;
            other.z -= 2 * HexMetrics.cellSizeZ;
            xyzWrapped = (x < other.x ? other.x - x : x - other.x) +
                         (Y < other.Y ? other.Y - Y : Y - other.Y) +
                         (z < other.z ? other.z - z : z - other.z);
            if (xyzWrapped < xyz)
            {
                xyz = xyzWrapped;
            }
        }

        return xyz / 2;
    }

    public override string ToString()
    {
        return "(" +
            X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")";
    }

    public string ToStringOnSeparateLines()
    {
        return X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
    }
}
