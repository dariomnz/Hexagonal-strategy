using UnityEngine;
using RotaryHeart.Lib.SerializableDictionary;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "HexTerrains", menuName = "ScriptableObjects/HexTerrains", order = 1)]
public class HexTerrains : ScriptableObject
{
    public enum HexType
    {
        Grass,
        Rock,
        Sand,
        Water,
    };

    public enum HexRoadsConf
    {
        Zero = 0,
        One = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Eleven = 11,
        Twelve = 12,
        Thirteen = 13,
    };

    public static readonly Dictionary<string, HexRoadsConf> RoadConfiguration
    = new Dictionary<string, HexRoadsConf>{

        {"000000", HexRoadsConf.Zero},
        {"000010", HexRoadsConf.One},
        {"000011", HexRoadsConf.Two},
        {"100010", HexRoadsConf.Three},
        {"010010", HexRoadsConf.Four},
        {"100011", HexRoadsConf.Five},
        {"010011", HexRoadsConf.Six},
        {"010110", HexRoadsConf.Seven},
        {"101010", HexRoadsConf.Eight},
        {"110011", HexRoadsConf.Nine},
        {"101011", HexRoadsConf.Ten},
        {"011011", HexRoadsConf.Eleven},
        {"111101", HexRoadsConf.Twelve},
        {"111111", HexRoadsConf.Thirteen},
    };


    [System.Serializable]
    public class TerrainRoads : SerializableDictionaryBase<HexRoadsConf, GameObject> { }

    [SerializeField]
    public SerializableDictionaryBase<HexType, TerrainRoads> terrainMeshs;


    public GameObject GetMesh(HexType type, bool[] roads, out int rotations)
    {
        rotations = -1;
        if (!terrainMeshs.ContainsKey(type))
        {
            Debug.LogError(string.Format("Not registed type: {0}", type.ToString()));
            return null;
        }
        TerrainRoads terrainRoads = terrainMeshs[type];

        string roadsString = "";
        foreach (bool road in roads)
        {
            if (road)
                roadsString += "1";
            else
                roadsString += "0";
        }

        for (int i = 0; i < 6; i++)
        {
            if (RoadConfiguration.ContainsKey(roadsString))
            {
                rotations = i;
                break;
            }
            char ultimoCaracter = roadsString[roadsString.Length - 1];  // Guarda el último carácter
            roadsString = ultimoCaracter + roadsString.Substring(0, roadsString.Length - 1);
        }

        if (rotations == -1)
        {
            Debug.LogError(string.Format("Not registed roadConfiguration: {0}", roadsString.ToString()));
            return null;
        }

        HexRoadsConf conf = RoadConfiguration[roadsString];

        if (!terrainRoads.ContainsKey(conf))
        {
            Debug.LogError(string.Format("Not registed terrainRoadPrefab: {0}", conf.ToString()));
            return null;
        }

        return terrainRoads[conf];
    }
}