using UnityEngine;
using Mapbox.Utils;
using Mapbox.Map;

/// <summary>
/// This class is responsible for creating location pins, as well as managing the pinned locations.
/// </summary>
public class PinnedLocationManager : CoordinateBoundObjectsManagerBase<PinnedLocationManager> {
    [SerializeField]
    private GameObject pinPrefab;

    public const string COMMAND_PIN_LOCATION = "save coordinate";

    protected override void TileProvider_OnTileAdded(UnwrappedTileId tileIdAdded) {
        if (GameObjectsInScene.Count == 0) // this is because unlike the buildings, pins are added by user. If no pin is created yet, we don't have to do anything
            return;

        base.TileProvider_OnTileAdded(tileIdAdded);
    }

    protected override void loadObject(CoordinateBoundObject pin) {
        GameObject pinObject = pin.gameObject;
        if (pinObject == null) // if deleted
            return;
        GameObject parentTile = LocationHelper.FindParentTile(pin.coordinates);
        pinObject.transform.position = LocationHelper.geoCoordinateToWorldPosition(pin.coordinates);
        pinObject.transform.SetParent(parentTile.transform);
        pinObject.SetActive(true);
        HoloToolkit.Unity.Utils.SetLayerRecursively(pinObject, parentTile.layer);
    }

    public override void OnZoomChanged() {
        foreach (CoordinateBoundObject pin in GameObjectsInScene.Values) {
            GameObject pinObject = pin.gameObject;
            if (pinObject == null)
                continue;

            pinObject.transform.parent = null;

            /// since the scale of the map changes (*2 or /2) before the tiles get destroyed,
            /// before the parent is set to null the size gets multiplied by the difference
            /// so just set the localscale to pinPrefab's localScale to restore the original scale
            pinObject.transform.localScale = pinPrefab.transform.localScale;
            pinObject.SetActive(false);
        }
    }

#region pinning related

    public void InstantiatePin(Vector3 point) {
        Vector2d pointLatLong = LocationHelper.WorldPositionToGeoCoordinate(point);
        CoordinateBoundObject newPin = new CoordinateBoundObject();
        newPin.latitude = (float)pointLatLong.x;
        newPin.longitude = (float)pointLatLong.y;
        GameObject parentTile = LocationHelper.FindParentTile(pointLatLong);
        GameObject pinObject = Instantiate(pinPrefab, point, Quaternion.identity);
        pinObject.name = "pin " + GameObjectsInScene.Count;
        newPin.gameObject = pinObject;
        pinObject.transform.SetParent(parentTile.transform, true);
        GameObjectsInScene[pinObject.name] = newPin;

        // add to the dropdown
        DropDownPinnedLocations.Instance.AddItemToDropdown(pinObject.name);
    }

    public void pinLocation(Vector3 position) {
        InstantiatePin(position);
    }

#endregion

}

