using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(HexGrid))]
public class GeneratePlanetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        HexGrid myScript = (HexGrid)target;
        if (GUILayout.Button("Create new Map"))
        {
            myScript.CreateMap();
        }
        if (GUILayout.Button("Delete Map"))
        {
            myScript.DeleteMap();
        }
    }
}