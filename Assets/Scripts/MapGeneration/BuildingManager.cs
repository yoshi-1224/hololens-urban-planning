using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity;
using Mapbox.Utils;
using HoloToolkit.Unity;
using Mapbox.Map;
using Mapbox.Unity.Utilities;
using System.Linq;
using System;

public class BuildingManager : HoloToolkit.Unity.Singleton<BuildingManager> {
    [Serializable]
    public struct CoordinateBoundObject {
        public float latitude;
        public float longitude;
        public Vector2d coordinates {
            get {
                return new Vector2d(latitude, longitude); // note that x = lat, y = long
            }
        }
        public GameObject gameObject;
    }

    /// <summary>
    /// This is for the prefabs, not the gameObjects in scene
    /// </summary>
    [SerializeField]
    private List<CoordinateBoundObject> BuildingPrefabList;

    /// <summary>
    /// use this to process buildings one by one in order to minimize the computing cost
    /// on each frame
    /// </summary>
    private Queue<CoordinateBoundObject> buildingsToLoad;

    /// <summary>
    /// dictionary that keeps track of which buildings are brought into the scene.
    /// Use this to check for duplicates etc. Note how it doesn't make sense to store
    /// reference to parent tile because the tiles can get destroyed upon zoom
    /// </summary>
    public Dictionary<string, CoordinateBoundObject> BuildingsInScene { get; set; }

    private bool shouldStartLoadingBuildings;

    protected override void Awake() {
        base.Awake();
        shouldStartLoadingBuildings = false;
        CustomRangeTileProvider.OnTileObjectAdded += TileProvider_OnTileAdded;
        CustomRangeTileProvider.OnAllTilesLoaded += CustomRangeTileProvider_OnAllTilesLoaded;
        BuildingsInScene = new Dictionary<string, CoordinateBoundObject>();
        buildingsToLoad = new Queue<CoordinateBoundObject>();
    }

    protected override void OnDestroy() {
        CustomRangeTileProvider.OnTileObjectAdded -= TileProvider_OnTileAdded;
        CustomRangeTileProvider.OnAllTilesLoaded -= CustomRangeTileProvider_OnAllTilesLoaded;
        BuildingsInScene = null;
        buildingsToLoad = null;
        base.OnDestroy();
    }

    private void CustomRangeTileProvider_OnAllTilesLoaded() {
        shouldStartLoadingBuildings = true;
    }

    private void TileProvider_OnTileAdded(UnwrappedTileId tileId) {
        queryBuildingsWithinTileBounds(tileId);
    }

    private void Update() {
        if (shouldStartLoadingBuildings && buildingsToLoad.Count > 0) {
            LoadBuilding(buildingsToLoad.Dequeue());
            if (buildingsToLoad.Count == 0)
                shouldStartLoadingBuildings = false;
        }
    }

    private GameObject LoadBuilding(CoordinateBoundObject buildingModel) {
        string buildingName = buildingModel.gameObject.name;

        GameObject parentTile = LocationHelper.FindParentTile(buildingModel.coordinates);
        if (parentTile == null) {
            Debug.Log("Not instantiating since no parent tile found"); // this should not happen
            return null;
        }

        Vector3 position = LocationHelper.geoCoordinateToWorldPosition(buildingModel.coordinates);
        CoordinateBoundObject building;
        if (BuildingsInScene.TryGetValue(buildingName, out building)) {
            // if it already has been instantiated but simply hidden

            building.gameObject.SetActive(true);
            building.gameObject.transform.SetParent(parentTile.transform, false);
        } else { //instantiate the prefab for the first time
            building.gameObject = Instantiate(buildingModel.gameObject, parentTile.transform);

            //copy the coordinate info
            building.latitude = buildingModel.latitude;
            building.longitude = buildingModel.longitude;

            building.gameObject.name = buildingName; // get the (Clone) substring out of it
            BuildingsInScene[buildingName] = building;
        }
        
        // adjust its position since the pivot position of the building models
        // are at their center.
        float halfHeight = building.gameObject.GetComponent<BoxCollider>().bounds.extents.y;
        position.y += halfHeight;
        building.gameObject.transform.position = position;

        building.gameObject.layer = parentTile.layer;

        return building.gameObject;
    }

    /// <summary>
    /// set the transform parent of all the building models to null as all tiles are going to be     destroyed and hide the buildings models
    /// </summary>
    internal void OnZoomChanged() {
        foreach(CoordinateBoundObject building in BuildingsInScene.Values) {
            // set parent to null in order to avoid getting destroyed with the parent tile
            building.gameObject.transform.SetParent(null, false);
            building.gameObject.SetActive(false); // simply hide it rather than destroy
        }
    }

    /// <summary>
    /// used every time a new tile is loaded (per-tile basis)
    /// </summary>
    private void queryBuildingsWithinTileBounds(UnwrappedTileId tileId) {
        Vector2dBounds bounds = Conversions.TileIdToBounds(tileId);
        var buildings = BuildingPrefabList.Where(building => {
            if (building.coordinates.x.InRange(bounds.North, bounds.South) 
            && building.coordinates.y.InRange(bounds.West, bounds.East)) {
                return true;
            }
            return false;
        });

        List<string> buildingNames = new List<string>();
        foreach (CoordinateBoundObject building in buildings) {
            buildingsToLoad.Enqueue(building);
            buildingNames.Add(building.gameObject.name);
        }

        /// lets add the building name to dropdown here
        DropDownBuildings.Instance.AddItemsToDropdown(buildingNames);
    }
}
