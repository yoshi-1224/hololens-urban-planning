using System.Collections.Generic;
using UnityEngine;
using Mapbox.Utils;
using Mapbox.Map;
using Mapbox.Unity.Utilities;

/// <summary>
/// This class stores the references to the building game objects in the scene and manage them
/// </summary>
public class BuildingManager : CoordinateBoundObjectsManagerBase<BuildingManager> {

    /// <summary>
    /// This is for the building prefabs, not the game objects in scene
    /// </summary>
    [SerializeField]
    private List<CoordinateBoundObject> BuildingPrefabList;

    protected override void loadObject(CoordinateBoundObject buildingModel) {
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

            //copy the coordinate info
            building.latitude = buildingModel.latitude;
            building.longitude = buildingModel.longitude;

            building.gameObject.name = buildingName; // get the (Clone) substring out of it
            GameObjectsInScene[buildingName] = building;
            DropDownBuildings.Instance.AddItemToDropdown(buildingName);
        }

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
        for (int i = 0; i < BuildingPrefabList.Count; i++) {
            CoordinateBoundObject building = BuildingPrefabList[i];
            if (building.coordinates.x.InRange(bounds.North, bounds.South)
            && building.coordinates.y.InRange(bounds.West, bounds.East)) {
                ObjectsToLoad.Enqueue(building);
            }
        }
    }
}
