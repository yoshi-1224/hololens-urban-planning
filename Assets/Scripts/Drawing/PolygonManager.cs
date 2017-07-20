using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using Mapbox.Utils;
using Mapbox.Map;
using Mapbox.Unity.Utilities;
using System.Linq;

/// <summary>
/// handles the creation of polygons and stores them in a list
/// </summary>
public class PolygonManager : HoloToolkit.Unity.Singleton<PolygonManager> {
    [SerializeField]
    private Material polygonMaterial;

    private List<BuildingManager.CoordinateBoundObjects> polygonsInScene;

    protected override void Awake() {
        base.Awake();
        polygonsInScene = new List<BuildingManager.CoordinateBoundObjects>();
        CustomRangeTileProvider.OnTileObjectAdded += CustomRangeTileProvider_OnAllTilesLoaded;
    }

    protected override void OnDestroy() {
        CustomRangeTileProvider.OnTileObjectAdded -= CustomRangeTileProvider_OnAllTilesLoaded;
        base.OnDestroy();
    }

    private void CustomRangeTileProvider_OnAllTilesLoaded(UnwrappedTileId tileIdLoaded) {
        if (polygonsInScene.Count > 0)
            queryPolygonsWithinTileBound(tileIdLoaded);
    }

    public void OnZoomChanged() {
        for (int i = 0; i < polygonsInScene.Count; i++) {
            polygonsInScene[i].prefab.transform.parent = null;
            polygonsInScene[i].prefab.SetActive(false);
        }
    }

    public void GeneratePolygonFromVertices(List<Vector3> polygonVertices) {
        Dictionary<int, int> neighbouringVertexMapping;
        GameObject polygon = PolygonGenerator.GeneratePolygonFromVertices(polygonVertices, 0.1f, polygonMaterial, out neighbouringVertexMapping);
        polygon.name = "polygon #" + polygonsInScene.Count;
        Vector2d polygonCoordinates = setPolygonParentToMapTile(polygon);
        UserGeneratedPolygon script = polygon.AddComponent<UserGeneratedPolygon>();
        script.neighbouringVertexMapping = neighbouringVertexMapping;

        BuildingManager.CoordinateBoundObjects polygonWithCoordinates = new BuildingManager.CoordinateBoundObjects();
        polygonWithCoordinates.latitude = (float) polygonCoordinates.x;
        polygonWithCoordinates.longitude = (float) polygonCoordinates.y;
        polygonWithCoordinates.prefab = polygon;
        polygonsInScene.Add(polygonWithCoordinates);
    }

    /// <summary>
    /// finds the tile gameObject that is directly below the center of the polygon
    /// and set its transform to polygon's parent transform
    /// </summary>
    private Vector2d setPolygonParentToMapTile(GameObject polygon) {
        Vector2d polygonCoordinates = LocationHelper.WorldPositionToGeoCoordinate(polygon.transform.position);
        UnwrappedTileId parentTileId = TileCover.CoordinateToTileId(polygonCoordinates, CustomMap.Instance.Zoom);
        GameObject parentTileObject = null;
        if (CustomRangeTileProvider.InstantiatedTiles.TryGetValue(parentTileId, out parentTileObject)) {
            polygon.transform.SetParent(parentTileObject.transform, true);
        } else {
            Debug.Log("Parent not found!");
        }
        return polygonCoordinates;
    }

    // this should be merged/generalized with BuildingsPlacement code (inheritance?)
    private void queryPolygonsWithinTileBound(UnwrappedTileId tileId) {
        Vector2dBounds bounds = Conversions.TileIdToBounds(tileId);
        var polygonsWithinBound = polygonsInScene.Where((polygon) => {
            if (polygon.coordinates.x.InRange(bounds.North, bounds.South) &&
            polygon.coordinates.y.InRange(bounds.West, bounds.East))
                return true;
            return false;
        });

        foreach (BuildingManager.CoordinateBoundObjects polygon in polygonsWithinBound) {
            // set its position properly
            polygon.prefab.transform.position = LocationHelper.geoCoordinateToWorldPosition(polygon.coordinates);
            polygon.prefab.transform.parent = CustomRangeTileProvider.InstantiatedTiles[tileId].transform;
            polygon.prefab.SetActive(true);
            polygon.prefab.layer = CustomRangeTileProvider.InstantiatedTiles[tileId].layer;
        }
    }

}
