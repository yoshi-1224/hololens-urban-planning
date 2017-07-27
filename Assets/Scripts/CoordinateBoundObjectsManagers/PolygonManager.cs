using System.Collections.Generic;
using UnityEngine;
using Mapbox.Utils;
using Mapbox.Map;

public class PolygonManager : CoordinateBoundObjectsManagerBase<PolygonManager> {
    [SerializeField]
    private Material polygonMaterial;

    protected override void TileProvider_OnTileAdded(UnwrappedTileId tileIdLoaded) {
        if (GameObjectsInScene.Count > 0)
            queryObjectsWithinTileBounds(tileIdLoaded);
    }

    public override void OnZoomChanged() {
        foreach(CoordinateBoundObject polygon in GameObjectsInScene.Values) {
            GameObject polygonObject = polygon.gameObject;
            if (polygonObject == null) // if deleted
                continue;
            polygonObject.transform.parent = null;
            polygonObject.SetActive(false);
        }
    }

    protected override void loadObject(CoordinateBoundObject polygon) {
        GameObject polygonObject = polygon.gameObject;
        if (polygonObject == null)
            return;

        polygonObject.transform.position = LocationHelper.geoCoordinateToWorldPosition(polygon.coordinates);
        GameObject parentTile = LocationHelper.FindParentTile(polygon.coordinates);
        polygonObject.transform.SetParent(parentTile.transform, true);
        polygonObject.SetActive(true);
        polygonObject.layer = parentTile.layer;

    }

    #region polygon-generation

    public void InstantiatePolygonFromVertices(List<Vector3> polygonVertices) {
        Dictionary<int, int> neighbouringVertexMapping;
        GameObject polygon = PolygonGenerator.GeneratePolygonFromVertices(polygonVertices, 0.1f, polygonMaterial, out neighbouringVertexMapping);
        polygon.name = "polygon #" + GameObjectsInScene.Count;
        Vector2d polygonCoordinates = setPolygonParentToMapTile(polygon);
        UserGeneratedPolygon script = polygon.AddComponent<UserGeneratedPolygon>();
        script.neighbouringVertexMapping = neighbouringVertexMapping;

        CoordinateBoundObject polygonWithCoordinates = new CoordinateBoundObject();
        polygonWithCoordinates.latitude = (float)polygonCoordinates.x;
        polygonWithCoordinates.longitude = (float)polygonCoordinates.y;
        polygonWithCoordinates.gameObject = polygon;
        GameObjectsInScene[polygon.name] = polygonWithCoordinates;

        // add to the dropdown
        DropDownPolygons.Instance.AddItemToDropdown(polygon.name);
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
        }
        else {
            Debug.Log("Parent not found!");
        }
        return polygonCoordinates;
    }


    #endregion
}

