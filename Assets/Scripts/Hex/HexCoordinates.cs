using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

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
    private int WrapDelta(int delta, int size)
    {
        int halfSize = size / 2;
        if (delta > halfSize)
            return Mathf.Abs(size - delta);
        else
            return delta;
    }

    public int DistanceTo(HexCoordinates other)
    {
        int deltaX = Mathf.Abs(other.X - X);
        int deltaY = Mathf.Abs(other.Y - Y);
        int deltaZ = Mathf.Abs(other.Z - Z);

        int wrappedDeltaX = WrapDelta(deltaX, HexMetrics.cellSizeX);
        int wrappedDeltaY = WrapDelta(deltaY, HexMetrics.cellSizeZ);
        int wrappedDeltaZ = WrapDelta(deltaZ, HexMetrics.cellSizeZ);
        int distance = wrappedDeltaX + wrappedDeltaY + wrappedDeltaZ;

        HexCoordinates auxCoor = new HexCoordinates(other.X - HexMetrics.cellSizeX / 2, other.Z);
        int deltaX2 = Mathf.Abs(auxCoor.X - X);
        int deltaY2 = Mathf.Abs(auxCoor.Y - Y);
        int deltaZ2 = Mathf.Abs(auxCoor.Z - Z);

        int wrappedDeltaX2 = WrapDelta(deltaX2, HexMetrics.cellSizeX);
        int wrappedDeltaY2 = WrapDelta(deltaY2, HexMetrics.cellSizeZ);
        int wrappedDeltaZ2 = WrapDelta(deltaZ2, HexMetrics.cellSizeZ);
        int distance2 = wrappedDeltaX2 + wrappedDeltaY2 + wrappedDeltaZ2;

        auxCoor = new HexCoordinates(other.X + HexMetrics.cellSizeX / 2, other.Z);
        int deltaX3 = Mathf.Abs(auxCoor.X - X);
        int deltaY3 = Mathf.Abs(auxCoor.Y - Y);
        int deltaZ3 = Mathf.Abs(auxCoor.Z - Z);

        int wrappedDeltaX3 = WrapDelta(deltaX3, HexMetrics.cellSizeX);
        int wrappedDeltaY3 = WrapDelta(deltaY3, HexMetrics.cellSizeZ);
        int wrappedDeltaZ3 = WrapDelta(deltaZ3, HexMetrics.cellSizeZ);
        int distance3 = wrappedDeltaX3 + wrappedDeltaY3 + wrappedDeltaZ3;

        return Mathf.Min(distance, distance2, distance3) / 2;
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

    public void Save(BinaryWriter writer)
    {
        writer.Write((short)x);
        writer.Write((short)z);
    }

    public static HexCoordinates Load(BinaryReader reader)
    {
        HexCoordinates c;
        c.x = reader.ReadInt16();
        c.z = reader.ReadInt16();
        return c;
    }
}
