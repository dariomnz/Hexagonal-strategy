using UnityEngine;
using RotaryHeart.Lib.SerializableDictionary;
using System.IO;
using System.Collections.Generic;

public class HexFeatureManager : MonoBehaviour
{
    public enum Features
    {
        None,
        House,
        Castle,
        Forest,
        Bridge,
        TreeA,
        TreeB,
        TreeC,
        Forest1,
        Forest2,
        Forest3,
        Rock1,
        Rock2,
        Rock3,
    }

    public readonly static Features[] trees = new Features[] { Features.TreeA, Features.TreeB, Features.TreeC };
    public readonly static Features[] forests = new Features[] { Features.Forest1, Features.Forest2, Features.Forest3 };
    public readonly static Features[] rocks = new Features[] { Features.Rock1, Features.Rock2, Features.Rock3 };
    public readonly static Features[] noWalkable = new Features[] { Features.Forest3, Features.Rock3 };

    [SerializeField]
    public SerializableDictionaryBase<Features, GameObject> featurePrefabs;

    public Features currentFeature;
    public HexCell hexCell;
    GameObject currentFeatureGameObject;
    int _currentRotation = 0;
    int currentRotation
    {
        get => _currentRotation;
        set
        {
            _currentRotation = value;
            if (currentFeatureGameObject)
                currentFeatureGameObject.transform.localRotation = Quaternion.AngleAxis(_currentRotation * 60, Vector3.up);
        }
    }

    Transform container;

    public void Clear()
    {
        if (container)
        {
            Destroy(container.gameObject);
        }
        container = new GameObject(string.Format("FeaturesContainer {0}", hexCell.ToString())).transform;
        container.SetParent(hexCell.chunk.transform);
        container.position = hexCell.transform.position;
    }

    public void AddFeature(Features feature, bool randomRotation = false)
    {
        if (currentFeature != feature)
        {
            Clear();
            // if (currentFeatureGameObject != null)
            // {
            //     DestroyImmediate(currentFeatureGameObject);
            //     currentFeatureGameObject = null;
            // }
            currentFeatureGameObject = Instantiate(featurePrefabs[feature], container);
            if (randomRotation)
                currentRotation = Random.Range(0, 6);

            if (feature == Features.Rock1)
                currentFeatureGameObject.transform.localPosition = HexMetrics.hexVertex[Random.Range(0, 6)] * Random.Range(0.25f, 0.75f);
            // currentFeatureGameObject = PrefabUtility.InstantiatePrefab(featurePrefabs[feature], container) as GameObject;
            // currentFeatureGameObject.transform.SetParent(container, false);
            // PoblateRandom();
            currentFeature = feature;
        }
    }
    public void Save(BinaryWriter writer)
    {
        writer.Write((byte)currentFeature);
        if (currentFeature != Features.None)
            writer.Write((byte)currentRotation);
    }

    public void Load(BinaryReader reader)
    {
        AddFeature((HexFeatureManager.Features)reader.ReadByte());
        if (currentFeature != Features.None)
            currentRotation = reader.ReadByte();
    }
}