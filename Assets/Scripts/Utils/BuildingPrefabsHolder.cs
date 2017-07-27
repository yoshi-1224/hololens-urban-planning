using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "BuildingPrefabsHolder")]
public class BuildingPrefabsHolder : ScriptableObject {
    public List<CoordinateBoundObject> BuildingPrefabList;

    public void saveBuildingsInScene() {
        Debug.Log("Saving building prefabs");
        object[] gameObjects = FindObjectsOfType(typeof(GameObject));

        // iterates through all the game objects
        foreach (object obj in gameObjects) {
            GameObject gameObjectInScene = (GameObject) obj;

            //if (!gameObjectInScene.name.Contains("CityEngineMaterial"))
            //    continue;

            queryCoordinatesAndSave(gameObjectInScene);
            return;
        }
    }

    private IEnumerator queryCoordinatesAndSave(GameObject building) {
        Debug.Log("Making query");
        string name = building.name;

        CoordinateBoundObject buildingWithCoordinate = new CoordinateBoundObject();

        // fetch building info
        WWW con = new WWW("localhost/holo?name=" + name);

        yield return con;

        string[] data = con.text.Split(',');

        if (data.Length == 1)
            yield break; // fail

        float latitude = float.Parse(data[3]);
        float longitude = float.Parse(data[4]);

        buildingWithCoordinate.latitude = latitude;
        buildingWithCoordinate.longitude = longitude;

        Debug.Log(buildingWithCoordinate.coordinates);

        // save as prefab

        //buildingWithCoordinate.gameObject = building;

    }


}
