using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity;
using Mapbox.Utils;
using HoloToolkit.Unity;
using Mapbox.Map;
using Mapbox.Unity.Utilities;

public class BuildingsPlacement : HoloToolkit.Unity.Singleton<BuildingsPlacement> {
    private static int mapLayer = LayerMask.NameToLayer(GameObjectNamesHolder.NAME_LAYER_MAP);

    // how about making this a CustomMeshFactory class, each building tied
    // to each map? => hard for the server to tell which building to load?

    [SerializeField]
    private CustomRangeTileProvider tileProvider;

    /// <summary>
    /// dictionary that keeps track of which buildings are brought into the scene.
    /// Use this to check for duplicates etc.
    /// </summary>
    private Dictionary<string, GameObject> buildingsInScene = new Dictionary<string, GameObject>();
    
    /// <summary>
    /// finds the tile object that should parent the building with the latLong
    /// </summary>
    public GameObject FindParentTile(Vector2d ObjectLatLong) {
        //Vector3 ObjectPosition = LocationHelper.geoCoordinateToWorldPosition(ObjectLatLong);
        //GameObject hitTile = null;

        //// shift it slightly above the map so that we can perform raycast
        //ObjectPosition.y += 1f;

        //RaycastHit hitInfo;
        //Vector3 raycastDirection = Vector3.down;
        //int layerMask = 1 << mapLayer; // only hit this layer
        //float maxDistance = 100;
        //if (Physics.Raycast(ObjectPosition, raycastDirection, out hitInfo, maxDistance, layerMask)) {
        //    hitTile = hitInfo.collider.gameObject;
        //} else {
        //    Debug.Log("This object should not be on the map");
        //    return null;
        //}
        //return hitTile;

        UnwrappedTileId parentTile = TileCover.CoordinateToTileId(ObjectLatLong, CustomMap.Instance.Zoom);
        string tileObjectName = parentTile.ToString();
        GameObject tileObject = GameObject.Find(tileObjectName);
        if (tileObject == null) {
            Debug.Log("The tile is not in the scene ");
            return null;
        } else {
            Debug.Log("Tile object with the same id found");
            return tileObject;
        }
    }

    public void FetchAssetsForNewMapBounds() {
        Vector2dBounds mapGeoBounds = tileProvider.GetMapGeoBounds();
        /// request the server or something with this new information
        
    }

    private GameObject InstantiateBuilding(GameObject buildingPrefab, Vector2d latLong) {
        string buildingName = buildingPrefab.name;
        if (buildingsInScene.ContainsKey(buildingName))
            return null;

        GameObject parentTile = FindParentTile(latLong);
        if (parentTile == null) {
            Debug.Log("Not instantiating since no parent tile found");
            return null;
        }

        Vector3 position = LocationHelper.geoCoordinateToWorldPosition(latLong);
        GameObject building = Instantiate(buildingPrefab, position, Quaternion.identity, parentTile.transform);
        // attach stuff here

        buildingsInScene[buildingName] = building;

        return building;
    }
}
