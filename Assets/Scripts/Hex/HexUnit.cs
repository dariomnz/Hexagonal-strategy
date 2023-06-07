using UnityEngine;
using System.IO;

public class HexUnit : MonoBehaviour
{

    HexCell location;
    public HexCell Location
    {
        get { return location; }
        set
        {
            if (location)
                location.Unit = null;
            location = value;
            location.Unit = this;
            transform.SetParent(location.transform);
            transform.position = value.transform.position;
        }
    }

    float orientation;
    public float Orientation
    {
        get { return orientation; }
        set
        {
            orientation = value;
            transform.localRotation = Quaternion.Euler(0f, value, 0f);
        }
    }

    public HexUnits.UnitType unitType = HexUnits.UnitType.Base;

    public bool IsValidDestination(HexCell cell)
    {
        return !cell.IsUnderwater && !cell.Unit;
    }

    public void Die()
    {
        location.Unit = null;
        Destroy(gameObject);
    }

    public void Save(BinaryWriter writer)
    {
        location.coordinates.Save(writer);
        writer.Write(orientation);
        writer.Write((byte)unitType);
    }

    public static void Load(BinaryReader reader, HexGrid grid)
    {
        HexCoordinates coordinates = HexCoordinates.Load(reader);
        float orientation = reader.ReadSingle();
        HexUnits.UnitType _unitType = (HexUnits.UnitType)reader.ReadByte();
        HexUnit hexUnit = Instantiate(HexMetrics.Instance.hexUnits.unitsPrefabs[_unitType]);
        grid.AddUnit(hexUnit, grid.GetCell(coordinates), orientation);
    }
}