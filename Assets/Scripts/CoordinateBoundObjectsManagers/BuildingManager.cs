using UnityEngine;
using Mapbox.Utils;
using Mapbox.Map;
using Mapbox.Unity.Utilities;

/// <summary>
/// This class stores the references to the building game objects in the scene and manage them
/// </summary>
public class BuildingManager : CoordinateBoundObjectsManagerBase<BuildingManager> {

    [SerializeField]
    private BuildingPrefabsHolder prefabsHolder;

    [Tooltip("Instantiate all the building models at the start to avoid slow down during 3D scene")]
    [SerializeField]
    private bool isPoolBuildingsAtStart = true;

    private void Start() {
        if (isPoolBuildingsAtStart) {
            for (int i = 0; i < prefabsHolder.BuildingPrefabList.Count; i++) {
                CoordinateBoundObject buildingModel = prefabsHolder.BuildingPrefabList[i];
                CoordinateBoundObject building = new CoordinateBoundObject();

                building.gameObject = Instantiate(buildingModel.gameObject);
                prefabsHolder.CorrectPivotAtMeshCenter(building.gameObject);

                if (!string.IsNullOrEmpty(GetBuildingName(buildingModel.gameObject.name))) {
                    building.gameObject.AddComponent<InteractibleBuilding>();
                }

                building.gameObject.AddComponent<BoxCollider>();

                //copy the coordinate info
                building.latitude = buildingModel.latitude;
                building.longitude = buildingModel.longitude;

                building.gameObject.name = buildingModel.gameObject.name;
                GameObjectsInScene[buildingModel.gameObject.name] = building;
                building.gameObject.transform.localScale *= 99;
                building.gameObject.SetActive(false);
            }
        }

    }

    protected override void Awake() {
        base.Awake();
        prefabsHolder.LoadCSV();
    }

    protected override void loadObject(CoordinateBoundObject buildingModel) {
        if (buildingModel.gameObject == null)
            return;

        string buildingName = buildingModel.gameObject.name;
        
        GameObject parentTile = LocationHelper.FindParentTile(buildingModel.coordinates);
        if (parentTile == null) {
            Debug.Log("Not instantiating since no parent tile found"); // this should not happen
            return;
        }

        Vector3 position = LocationHelper.geoCoordinateToWorldPosition(buildingModel.coordinates);
        CoordinateBoundObject building;
        if (GameObjectsInScene.TryGetValue(buildingName, out building)) {
            // if it already has been instantiated but simply active = false

            building.gameObject.SetActive(true);
            building.gameObject.transform.SetParent(parentTile.transform, false);
        }
        else { //instantiate the prefab for the first time
            building.gameObject = Instantiate(buildingModel.gameObject, parentTile.transform);
            prefabsHolder.CorrectPivotAtMeshCenter(building.gameObject);

            if (!string.IsNullOrEmpty(GetBuildingName(buildingModel.gameObject.name))) {
                building.gameObject.AddComponent<InteractibleBuilding>();
            }

            building.gameObject.AddComponent<BoxCollider>();

            //copy the coordinate info
            building.latitude = buildingModel.latitude;
            building.longitude = buildingModel.longitude;

            building.gameObject.name = buildingName;
            GameObjectsInScene[buildingName] = building;
            building.gameObject.transform.localScale *= 99;
        }

        DropDownBuildings.Instance.AddItemToDropdown(buildingName);

        // adjust its vertical position since the pivot position of the building models
        // are at their center.
        float halfHeight = building.gameObject.GetComponent<BoxCollider>().bounds.extents.y;
        position.y += halfHeight;
        building.gameObject.transform.position = position;
        building.gameObject.layer = parentTile.layer;
    }

    /// <summary>
    /// set the transform parent of all the building models to null as all tiles are going to be     destroyed, and hide the buildings models
    /// </summary>
    public override void OnZoomChanged() {
        foreach (CoordinateBoundObject building in GameObjectsInScene.Values) {
            if (building.gameObject == null)
                continue;
            building.gameObject.transform.SetParent(null, false);
            building.gameObject.SetActive(false); // simply hide it rather than destroy
        }
    }

    protected override void queryObjectsWithinTileBounds(UnwrappedTileId tileId) {

        Vector2dBounds bounds = Conversions.TileIdToBounds(tileId);
        for (int i = 0; i < prefabsHolder.BuildingPrefabList.Count; i++) {
            CoordinateBoundObject building = prefabsHolder.BuildingPrefabList[i];
            if (building.coordinates.x.InRange(bounds.North, bounds.South)
            && building.coordinates.y.InRange(bounds.West, bounds.East)) {
                ObjectsToLoad.Enqueue(building);
            }
        }
    }

    public string GetBuildingName(string gameObjectName) {
        return prefabsHolder.GetBuildingName(gameObjectName);
    }

    public string GetBuildingInformation(string gameObjectName) {
        return prefabsHolder.getBuildingInformation(gameObjectName);
    }

}
