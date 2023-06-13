using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Pool;
using UnityEngine.Events;

public class HexUnit : MonoBehaviour, IInteractive
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
            transform.SetParent(location.chunk.transform);
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

    List<HexCell> pathToTravel;
    public int travelSpeed = 2;
    public float rotationSpeed = 360f;
    public Animator animator;

    public bool IsValidDestination(HexCell cell)
    {
        return !cell.IsUnderwater && !cell.Unit;
    }

    public void Travel(List<HexCell> path)
    {
        Location = path[path.Count - 1];
        pathToTravel = path;
        StopAllCoroutines();
        StartCoroutine(TravelPath());
    }

    IEnumerator TravelPath()
    {
        Vector3 a, b, c = pathToTravel[0].transform.position;
        transform.position = c;
        yield return LookAt(pathToTravel[1].transform.position);
        animator.CrossFade("Run", 0.2f);
        float t = Time.deltaTime * travelSpeed;
        for (int i = 1; i < pathToTravel.Count; i++)
        {
            a = c;
            b = pathToTravel[i - 1].transform.position;
            c = (b + pathToTravel[i].transform.position) * 0.5f;
            for (; t < 1f; t += Time.deltaTime * travelSpeed)
            {
                transform.position = Bezier.GetPoint(a, b, c, t);
                Vector3 d = Bezier.GetDerivative(a, b, c, t);
                d.y = 0f;
                transform.localRotation = Quaternion.LookRotation(d);
                yield return null;
            }
            t -= 1f;
        }

        a = c;
        b = pathToTravel[pathToTravel.Count - 1].transform.position;
        c = b;
        for (; t < 1f; t += Time.deltaTime * travelSpeed)
        {
            transform.position = Bezier.GetPoint(a, b, c, t);
            Vector3 d = Bezier.GetDerivative(a, b, c, t);
            d.y = 0f;
            transform.localRotation = Quaternion.LookRotation(d);
            yield return null;
        }
        transform.position = location.transform.position;
        orientation = transform.localRotation.eulerAngles.y;
        ListPool<HexCell>.Release(pathToTravel);
        pathToTravel = null;
        animator.CrossFade("Idle", 0.2f);
    }

    IEnumerator LookAt(Vector3 point)
    {
        point.y = transform.position.y;
        Quaternion fromRotation = transform.localRotation;
        Quaternion toRotation = Quaternion.LookRotation(point - transform.position);
        float angle = Quaternion.Angle(fromRotation, toRotation);
        if (angle > 0f)
        {
            float speed = rotationSpeed / angle;
            for (float t = Time.deltaTime * speed; t < 1f; t += Time.deltaTime * speed)
            {
                transform.localRotation =
                    Quaternion.Slerp(fromRotation, toRotation, t);
                yield return null;
            }
        }

        transform.LookAt(point);
        orientation = transform.localRotation.eulerAngles.y;
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

    public Dictionary<HexInteration, UnityAction> GetInteractions()
    {
        return new Dictionary<HexInteration, UnityAction>() {
            { HexInteration.UnitMove , () => { Debug.Log(ToString()+HexInteration.UnitMove.ToString());
                                            HexGameUI.Instance.selectedUnit = this;
                                            HexGameUI.Instance.CloseInteraction(); }},
            { HexInteration.UnitAttack , () => { Debug.Log(ToString()+HexInteration.UnitAttack.ToString());
                                            HexGameUI.Instance.CloseInteraction(); }},
        };
    }

    public void Interact()
    {
        HexGameUI.Instance.OpenInteraction(transform.position + Vector3.up * 3, this);
    }
}