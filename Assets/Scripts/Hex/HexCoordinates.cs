using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct HexCoordinates
{
    [SerializeField]
    private int x, z;

    public int X { get { return x; } set { x = value; } }
    public int Y { get { return -X - Z; } set { z = -X - value; } }
    public int Z { get { return z; } set { z = value; } }

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
        HexCoordinates auxCoor = new HexCoordinates(other.X, other.Z);
        
        int xyz = Mathf.Abs(auxCoor.X - X) +
                  Mathf.Abs(auxCoor.Y - Y) +
                  Mathf.Abs(auxCoor.Z - Z);

        auxCoor.X += HexMetrics.cellSizeX;

        int xyzWrapped = Mathf.Abs(auxCoor.X - X) +
                         Mathf.Abs(auxCoor.Y - Y) +
                         Mathf.Abs(auxCoor.Z - Z);
        if (xyzWrapped < xyz)
        {
            xyz = xyzWrapped;
        }
        else
        {
            auxCoor.X -= 2 * HexMetrics.cellSizeX;
            xyzWrapped = Mathf.Abs(auxCoor.X - X) +
                         Mathf.Abs(auxCoor.Y - Y) +
                         Mathf.Abs(auxCoor.Z - Z);
            if (xyzWrapped < xyz)
            {
                xyz = xyzWrapped;
            }
        }

        auxCoor = new HexCoordinates(other.X, other.Z);
        auxCoor.X -= HexMetrics.cellSizeX / 2;
        auxCoor.Z += HexMetrics.cellSizeZ;

        xyzWrapped = Mathf.Abs(auxCoor.X - X) +
                         Mathf.Abs(auxCoor.Y - Y) +
                         Mathf.Abs(auxCoor.Z - Z);
        if (xyzWrapped < xyz)
        {
            xyz = xyzWrapped;
        }
        else
        {
            auxCoor.X += 2 * HexMetrics.cellSizeX / 2;
            auxCoor.Z -= 2 * HexMetrics.cellSizeZ;
            xyzWrapped = Mathf.Abs(auxCoor.X - X) +
                         Mathf.Abs(auxCoor.Y - Y) +
                         Mathf.Abs(auxCoor.Z - Z);
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
