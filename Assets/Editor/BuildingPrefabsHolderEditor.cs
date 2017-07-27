using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BuildingPrefabsHolder))]
public class BuildingPrefabsHolderEditor : Editor {

    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        BuildingPrefabsHolder script = (BuildingPrefabsHolder)target;
        if (GUILayout.Button("Save gameObjects")) {
            script.saveBuildingsInScene();
        }
    }
}
