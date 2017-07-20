using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using Mapbox.Map;
using System;

public static class LocationHelper {

    public static Vector3 geoCoordinateToWorldPosition(Vector2d latLng) {
        var mapCenterInMeters = Conversions.LatLonToMeters(CustomMap.Instance.CenterLatitudeLongitude);
        var latLngInMeters = Conversions.LatLonToMeters(latLng);
        Vector2d displacementFromMapCenter = latLngInMeters - mapCenterInMeters;
        Vector3 position = displacementFromMapCenter.ToVector3xz();

        // treat position as a point in localspace, and use TransformPoint as
        // it takes into account the rotation, position offset and scaling of the map relative 
        // to the worldspace
        position = CustomMap.Instance.transform.TransformPoint(position);

        // we know that the height would be equal to the map height for sure
        position.y = CustomMap.Instance.transform.position.y;
        return position;
    }
    
    public static Vector2d WorldPositionToGeoCoordinate(Vector3 position) {
        // ignore the height diff and convert to local position wrt the map
        Vector2d displacementFromMapCenter = CustomMap.Instance.transform.InverseTransformPoint(position).ToVector2d();
        // note that the displacement in localSpace is much bigger than that in worldspace because of the mapScaling

        Vector2d mapCenterInMeters = Conversions.LatLonToMeters(CustomMap.Instance.CenterLatitudeLongitude);
        Vector2d positionInMeters = displacementFromMapCenter + mapCenterInMeters;
        return Conversions.MetersToLatLon(positionInMeters);
    }
    public static event Action<UnwrappedTileId> onTileJump;
    /// <summary>
    /// finds the tile object that should parent the building with a latitude/longitude
    /// </summary>
    public static GameObject FindParentTile(Vector2d latLong) {
        UnwrappedTileId parentTile = TileCover.CoordinateToTileId(latLong, CustomMap.Instance.Zoom);
        GameObject tileObject;
        if (CustomRangeTileProvider.InstantiatedTiles.TryGetValue(parentTile, out tileObject)) {
            return tileObject;
        }
        else {
            return null;
        }
    }

    public static void MoveMapToWorldPositionAsCenter(Vector3 worldPositionToMoveTowards) {
        Vector2d coordinates = WorldPositionToGeoCoordinate(worldPositionToMoveTowards);
        MoveMapToCoordinateAsCenter(coordinates);
    }

    public static void MoveMapToCoordinateAsCenter(Vector2d CoordinateToMoveTowards) {
        // find the tile id of the parent
        UnwrappedTileId parentTileId = TileCover.CoordinateToTileId(CoordinateToMoveTowards, CustomMap.Instance.Zoom);

        // using event just as a placeholder
        onTileJump.Invoke(parentTileId);

    }
}
