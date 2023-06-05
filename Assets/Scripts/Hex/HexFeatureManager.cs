using UnityEngine;
using RotaryHeart.Lib.SerializableDictionary;
using UnityEditor;

public class HexFeatureManager : MonoBehaviour
{
    public enum Features
    {
        None,
        House,
        Castle,
        Forest,
        Bridge,
    }

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

    public void AddFeature(HexCell cell, Features feature)
    {
        if (currentFeature != feature)
        {
            Clear();
            // if (currentFeatureGameObject != null)
            // {
            //     DestroyImmediate(currentFeatureGameObject);
            //     currentFeatureGameObject = null;
            // }
            // currentFeatureGameObject = Instantiate(featurePrefabs[feature], container);
            currentFeatureGameObject = PrefabUtility.InstantiatePrefab(featurePrefabs[feature], container) as GameObject;
            // currentFeatureGameObject.transform.SetParent(container, false);
            currentFeature = feature;
        }
    }
}