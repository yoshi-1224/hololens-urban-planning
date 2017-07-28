using Mapbox.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// This scriptable object is used to create a list of CoordinateBoundObjects by iterating through the game objects in a scene, save them to a prefab holder and linking each prefab and its coordinate info from the given CSV file.
/// </summary>

[CreateAssetMenu(menuName = "BuildingPrefabsHolder")]
public class BuildingPrefabsHolder : ScriptableObject {

    public List<CoordinateBoundObject> BuildingPrefabList;

#region CSV column indices
    public const int INDEX_BUILDING_NAME = 1;
    public const int INDEX_HEIGHT = 2;
    public const int INDEX_CATEGORY = 3;
    public const int INDEX_STOREYS = 4;
    public const int INDEX_GAMEOBJECT_NAME = 5;
    public const int INDEX_LATITUDE = 6;
    public const int INDEX_LONGITUDE = 7;

    public const string unknown = "Unknown";

#endregion

    [Tooltip("CSV file which contains the building information")]
    [SerializeField]
    public TextAsset csvFile;
    private bool csvLoaded = false;
    private string[][] grid;

    [Tooltip("path to folder in which the game objects will be saved as prefabs")]
    public string prefabFolderPath;
    private string prefabExtension = ".prefab";
    
    private void loadCSV() {
        // saves the CSV into a two dimentional string array, with first row at index = 0
        // represents the column names (i.e. data starts from i = 1)
        if (!csvLoaded) {
            grid = CsvParser2.Parse(csvFile.text);
            csvLoaded = true;
        }
    }

    private void loadCSVFromEditor() {
        grid = CsvParser2.Parse(csvFile.text);
    }

    public void saveBuildingsInScene() {
        Debug.Log("saving");
        loadCSVFromEditor();
        object[] gameObjects = FindObjectsOfType(typeof(GameObject));
        Debug.Log(gameObjects.Length);

        // iterates through all the game objects
        foreach (object obj in gameObjects) {
            Debug.Log("in loop");
            GameObject gameObjectInScene = (GameObject) obj;

            if (!gameObjectInScene.name.Contains("CityEngineMaterial"))
                continue;
            try {
                queryCoordinatesAndSavePrefab(gameObjectInScene);
            } catch(Exception e) {
                Debug.Log(e);
                return;
            }
        }
    }

    /// <summary>
    /// finds a row in CSV aboutthis building, save the building game object as prefab and
    /// then add it to BuildingPrefabList
    /// </summary>
    private void queryCoordinatesAndSavePrefab(GameObject building) {
#if UNITY_EDITOR

        string name = building.name;
        CoordinateBoundObject buildingWithCoordinate = new CoordinateBoundObject();
        int rowId;
        
        // parse the building number from its name
        if (! int.TryParse(Regex.Match(name, @"\d+").Value, out rowId))
            return;

        rowId++; // as first row is column names

        float latitude = float.Parse(grid[rowId][INDEX_LATITUDE]);
        float longitude = float.Parse(grid[rowId][INDEX_LONGITUDE]);

        buildingWithCoordinate.latitude = latitude;
        buildingWithCoordinate.longitude = longitude;

        // save as prefab
        GameObject prefab = PrefabUtility.CreatePrefab(prefabFolderPath + "/" + name + prefabExtension, building);
        buildingWithCoordinate.gameObject = prefab;

        // add this to the list
        BuildingPrefabList.Add(buildingWithCoordinate);

#endif

    }

    public string getBuildingInformation(string gameObjectName) {
        loadCSV();
        // parse the building number from its name
        int rowId;
        if (!int.TryParse(Regex.Match(name, @"\d+").Value, out rowId))
            return null;

        string[] row = grid[rowId];
        StringBuilder str = new StringBuilder();

        string category = (string.IsNullOrEmpty(row[INDEX_CATEGORY])) ? unknown : (row[INDEX_CATEGORY]);
        str.AppendLine("<b>Class</b> :\n" + category);

        string height = (string.IsNullOrEmpty(row[INDEX_HEIGHT])) ? unknown : (row[INDEX_HEIGHT]) + "m";
        str.AppendLine("<b>Measured Height</b> :\n" + height);

        string numStoreys = (string.IsNullOrEmpty(row[INDEX_STOREYS])) ? unknown : (row[INDEX_STOREYS]);

        str.AppendLine("<b>Number of Storeys</b> :\n" + numStoreys);
 
        str.Append(Utils.FormatLatLong(double.Parse(row[INDEX_LATITUDE]), double.Parse(row[INDEX_LONGITUDE])));

        return str.ToString();
    }

    public string GetBuildingName(string gameObjectName) {
        loadCSV();
        int rowId;
        if (!int.TryParse(Regex.Match(name, @"\d+").Value, out rowId))
            return null;

        return grid[rowId][INDEX_BUILDING_NAME];
    }

    /// <summary>
    /// sets the pivot of mesh at object center. The changes applied here will only persist within the scene, and will NOT be saved as a prefab for example. 
    /// </summary>
    public void CorrectTransformAtMeshCenter(GameObject obj) {
        var mesh = obj.GetComponent<MeshFilter>().mesh;
        Vector3 pivot = FindObjectPivot(mesh.bounds);

        obj.transform.position -= Vector3.Scale(pivot, obj.transform.localScale);

        // Iterate over all vertices and move them in the opposite direction of the object position movement
        Vector3[] verts = mesh.vertices;
        for (int i = 0; i < verts.Length; i++) {
            verts[i] += pivot;
        }
        mesh.vertices = verts; //Assign the vertex array back to the mesh
        mesh.RecalculateBounds(); //Recalculate bounds of the mesh, for the renderer's sake
    }

    public Vector3 FindObjectPivot(Bounds bounds) {
        Vector3 offset = -1 * bounds.center;
        Vector3 extent = new Vector3(offset.x / bounds.extents.x, offset.y / bounds.extents.y, offset.z / bounds.extents.z);
        return Vector3.Scale(bounds.extents, extent);
    }
}
