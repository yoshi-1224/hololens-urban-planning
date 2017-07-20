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
    public struct CoordinateBoundObjects {
        public float latitude;
        public float longitude;
        public Vector2d coordinates {
            get {
                return new Vector2d(latitude, longitude); // note that x = lat, y = long
            }
        }
        public GameObject prefab;
    }

    [SerializeField]
    private List<CoordinateBoundObjects> BuildingPrefabList;

    /// <summary>
    /// use this to process buildings one by one in order to minimize the computing cost
    /// on each frame
    /// </summary>
    private Queue<CoordinateBoundObjects> buildingsToLoad;

    /// <summary>
    /// dictionary that keeps track of which buildings are brought into the scene.
    /// Use this to check for duplicates etc. Note how it doesn't make sense to store
    /// reference to parent tile because the tiles can get destroyed upon zoom
    /// </summary>
    public Dictionary<string, GameObject> BuildingsInScene { get; set; }

    private bool shouldStartLoadingTiles;

    protected override void Awake() {
        base.Awake();
        shouldStartLoadingTiles = false;
        CustomRangeTileProvider.OnTileObjectAdded += TileProvider_OnTileAdded;
        CustomRangeTileProvider.OnAllTilesLoaded += CustomRangeTileProvider_OnAllTilesLoaded;
        BuildingsInScene = new Dictionary<string, GameObject>();
        buildingsToLoad = new Queue<CoordinateBoundObjects>();
    }

    protected override void OnDestroy() {
        CustomRangeTileProvider.OnTileObjectAdded -= TileProvider_OnTileAdded;
        CustomRangeTileProvider.OnAllTilesLoaded -= CustomRangeTileProvider_OnAllTilesLoaded;
        BuildingsInScene = null;
        buildingsToLoad = null;
        base.OnDestroy();
    }

    private void CustomRangeTileProvider_OnAllTilesLoaded() {
        shouldStartLoadingTiles = true;
    }

    private void TileProvider_OnTileAdded(UnwrappedTileId tileId) {
        queryBuildingsWithinTileBounds(tileId);
    }

    private void Update() {
        if (shouldStartLoadingTiles && buildingsToLoad.Count > 0) {
            InstantiateBuilding(buildingsToLoad.Dequeue());
            if (BuildingsInScene.Count == 0)
                shouldStartLoadingTiles = false;
        }
    }

    private GameObject InstantiateBuilding(CoordinateBoundObjects buildingModel) {
        string buildingName = buildingModel.prefab.name;

        GameObject parentTile = LocationHelper.FindParentTile(buildingModel.coordinates);
        if (parentTile == null) {
            Debug.Log("Not instantiating since no parent tile found"); // this should not happen
            return null;
        }

        Vector3 position = LocationHelper.geoCoordinateToWorldPosition(buildingModel.coordinates);
        GameObject building;
        if (BuildingsInScene.TryGetValue(buildingName, out building)) {
            // if it already has been instantiated but simply hidden
            building.SetActive(true);
            building.transform.SetParent(parentTile.transform, false);
        } else { //instantiate the prefab for the first time
            building = Instantiate(buildingModel.prefab, parentTile.transform);
            building.name = buildingName; // get the (Clone) substring out of it
            BuildingsInScene[buildingName] = building;
        }
        
        // adjust its position since the pivot position of the building models
        // are at their center.
        float halfHeight = building.GetComponent<BoxCollider>().bounds.extents.y;
        position.y += halfHeight;
        building.transform.position = position;

        // add the mapping for building name to coordinates
        TableDataHolder.Instance.nameToLocation[buildingName] = buildingModel.coordinates;

        building.layer = parentTile.layer;

        return building;
    }

    /// <summary>
    /// set the transform parent of all the building models to null as all tiles are going to be     destroyed and hide the buildings models
    /// </summary>
    internal void OnZoomChanged() {
        foreach(GameObject building in BuildingsInScene.Values) {
            // set parent to null in order to avoid getting destroyed with the parent tile
            building.transform.SetParent(null, false);
            building.SetActive(false); // simply hide it rather than destroy
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
        foreach (CoordinateBoundObjects building in buildings) {
            buildingsToLoad.Enqueue(building);
            buildingNames.Add(building.prefab.name);
        }

        /// lets add the building name to dropdown here
        DropDownBuildings.Instance.AddBuildingsToDropDown(buildingNames);
    }
}
