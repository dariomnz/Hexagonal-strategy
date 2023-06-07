using UnityEngine;
using RotaryHeart.Lib.SerializableDictionary;
using System.IO;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "HexUnits", menuName = "ScriptableObjects/HexUnits", order = 2)]
public class HexUnits : ScriptableObject
{
    public enum UnitType
    {
        None,
        Base,
    }

    [SerializeField]
    public SerializableDictionaryBase<UnitType, HexUnit> unitsPrefabs;
}