using UnityEngine;
using RotaryHeart.Lib.SerializableDictionary;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "HexTerrains", menuName = "ScriptableObjects/HexTerrains", order = 1)]
public class HexTerrains : ScriptableObject
{
    public enum HexType
    {
        None = -1,
        Water,
        Sand,
        Grass,
        Rock,
        Snow,
    };

    public enum HexMaterial
    {
        Water = HexType.Water,
        Sand = HexType.Sand,
        Grass = HexType.Grass,
        Rock = HexType.Rock,
        Snow = HexType.Snow,
        Road,
        River,
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

    // 0 no road, 1 road
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

    // 0 No river, 1 incoming river, 2 outgoing river
    public static readonly Dictionary<string, HexRoadsConf> RiverConfiguration
    = new Dictionary<string, HexRoadsConf>{

        {"000000", HexRoadsConf.Zero},
        {"000010", HexRoadsConf.One},
        {"000021", HexRoadsConf.Two},
        {"200010", HexRoadsConf.Three},
        {"020010", HexRoadsConf.Four},

        {"000020", HexRoadsConf.Five},
        {"000012", HexRoadsConf.Six},
        {"100020", HexRoadsConf.Seven},
        {"010020", HexRoadsConf.Eight},
    };


    // [System.Serializable]
    // public class TerrainRoads : SerializableDictionaryBase<HexRoadsConf, GameObject> { }

    // [SerializeField]
    // public SerializableDictionaryBase<HexType, TerrainRoads> terrainMeshs;

    public SerializableDictionaryBase<HexMaterial, Material> terrainMaterials;
    public SerializableDictionaryBase<HexRoadsConf, Mesh> terrainRoadsMeshs;
    public SerializableDictionaryBase<HexRoadsConf, Mesh> terrainRiversMeshs;
    public Mesh waterTop;

    // public GameObject GetSimpleMesh(HexType type)
    // {
    //     return terrainMeshs[type][HexRoadsConf.Zero];
    // }

    public Material[] GetMaterials(HexType type, bool hasRoads = false, bool hasRivers = false)
    {
        int outSize = type != HexType.Water && (hasRoads || hasRivers) ? 2 : 1;
        Material[] outMaterials = new Material[outSize];
        outMaterials[0] = terrainMaterials[(HexMaterial)type];
        if (type != HexType.Water)
        {
            if (hasRivers)
                outMaterials[1] = terrainMaterials[HexMaterial.River];
            if (hasRoads)
                outMaterials[1] = terrainMaterials[HexMaterial.Road];
        }
        return outMaterials;
    }

    public Mesh GetSimpleMesh()
    {
        return terrainRoadsMeshs[HexRoadsConf.Zero];
    }

    public Mesh GetRiverMesh(string riversString, out int rotations)
    {
        rotations = -1;
        for (int i = 0; i < 6; i++)
        {
            if (RiverConfiguration.ContainsKey(riversString))
            {
                rotations = i;
                break;
            }
            char ultimoCaracter = riversString[riversString.Length - 1];  // Guarda el último carácter
            riversString = ultimoCaracter + riversString.Substring(0, riversString.Length - 1);
        }

        Debug.Log(riversString);
        if (rotations == -1)
        {
            Debug.LogError(string.Format("Not registed roadConfiguration: {0}", riversString.ToString()));
            return null;
        }

        HexRoadsConf conf = RiverConfiguration[riversString];

        if (!terrainRiversMeshs.ContainsKey(conf))
        {
            Debug.LogError(string.Format("Not registed terrainRoadPrefab: {0}", conf.ToString()));
            return null;
        }
        Debug.Log(terrainRiversMeshs[conf]);
        return terrainRiversMeshs[conf];
    }

    public Mesh GetRoadMesh(bool[] roads, out int rotations)
    {
        rotations = -1;
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

        if (!terrainRoadsMeshs.ContainsKey(conf))
        {
            Debug.LogError(string.Format("Not registed terrainRoadPrefab: {0}", conf.ToString()));
            return null;
        }
        return terrainRoadsMeshs[conf];
    }


    // public GameObject GetMesh(HexType type, bool[] roads, out int rotations)
    // {
    //     rotations = -1;
    //     if (!terrainMeshs.ContainsKey(type))
    //     {
    //         Debug.LogError(string.Format("Not registed type: {0}", type.ToString()));
    //         return null;
    //     }
    //     TerrainRoads terrainRoads = terrainMeshs[type];

    //     string roadsString = "";
    //     foreach (bool road in roads)
    //     {
    //         if (road)
    //             roadsString += "1";
    //         else
    //             roadsString += "0";
    //     }

    //     for (int i = 0; i < 6; i++)
    //     {
    //         if (RoadConfiguration.ContainsKey(roadsString))
    //         {
    //             rotations = i;
    //             break;
    //         }
    //         char ultimoCaracter = roadsString[roadsString.Length - 1];  // Guarda el último carácter
    //         roadsString = ultimoCaracter + roadsString.Substring(0, roadsString.Length - 1);
    //     }

    //     if (rotations == -1)
    //     {
    //         Debug.LogError(string.Format("Not registed roadConfiguration: {0}", roadsString.ToString()));
    //         return null;
    //     }

    //     HexRoadsConf conf = RoadConfiguration[roadsString];

    //     if (!terrainRoads.ContainsKey(conf))
    //     {
    //         Debug.LogError(string.Format("Not registed terrainRoadPrefab: {0}", conf.ToString()));
    //         return null;
    //     }

    //     return terrainRoads[conf];
    // }
}