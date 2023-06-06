using UnityEngine;
using RotaryHeart.Lib.SerializableDictionary;
using UnityEditor;
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

    public static Features[] trees = new Features[] { Features.TreeA, Features.TreeB, Features.TreeC };
    public static Features[] forest = new Features[] { Features.Forest1, Features.Forest2, Features.Forest3 };
    public static Features[] rock = new Features[] { Features.Rock1, Features.Rock2, Features.Rock3 };

    [SerializeField]
    public SerializableDictionaryBase<Features, GameObject> featurePrefabs;

    public Features currentFeature;
    GameObject currentFeatureGameObject;

    Transform container;

    public void Clear()
    {
        if (container)
        {
            Destroy(container.gameObject);
        }
        container = new GameObject("Features Container").transform;
        container.SetParent(transform, false);
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
                currentFeatureGameObject.transform.localRotation = Quaternion.AngleAxis(Random.Range(0, 6) * 60, Vector3.up);

            if (feature == Features.Rock1)
                currentFeatureGameObject.transform.localPosition = HexMetrics.hexVertex[Random.Range(0, 6)] * Random.Range(0.25f, 0.75f);
            // currentFeatureGameObject = PrefabUtility.InstantiatePrefab(featurePrefabs[feature], container) as GameObject;
            // currentFeatureGameObject.transform.SetParent(container, false);
            // PoblateRandom();
            currentFeature = feature;
        }
    }
}