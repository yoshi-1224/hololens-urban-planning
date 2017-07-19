using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using Mapbox.Utils;
using Mapbox.Map;
using Mapbox.Unity.Utilities;
using System.Linq;

public class PinnedLocationManager: HoloToolkit.Unity.Singleton<PinnedLocationManager> {
    [SerializeField]
    private GameObject pinPrefab;

    public const string COMMAND_PIN_LOCATION = "pin location";

    /// <summary>
    /// contains all the pins list/dictionary of something
    /// </summary>
    private List<BuildingManager.CoordinateBoundObjects> pinsInScene;

    protected override void Awake() {
        base.Awake();
        pinsInScene = new List<BuildingManager.CoordinateBoundObjects>();
        CustomRangeTileProvider.OnTileObjectAdded += CustomRangeTileProvider_OnTileObjectAdded;
    }

    private void CustomRangeTileProvider_OnTileObjectAdded(UnwrappedTileId tileIdLoaded) {
        if (pinsInScene.Count > 0)
            queryPinsWithinTileBound(tileIdLoaded);
    }

    public void InstantiatePin(Vector3 point) {
        Vector2d pointLatLong = LocationHelper.worldPositionToGeoCoordinate(point);
        BuildingManager.CoordinateBoundObjects newPin = new BuildingManager.CoordinateBoundObjects();
        newPin.latitude = (float) pointLatLong.x;
        newPin.longitude = (float) pointLatLong.y;
        GameObject parentTile = LocationHelper.FindParentTile(pointLatLong);
        if (parentTile == null)
            Debug.Log("Parent not found for the new pin");
        GameObject pinObject = Instantiate(pinPrefab, point, Quaternion.identity);
        pinObject.name = "pin #" + pinsInScene.Count;
        newPin.prefab = pinObject;
        pinObject.transform.SetParent(parentTile.transform, true);
        pinsInScene.Add(newPin);
    }

    public void pinGazedLocation() {
        Vector3 gazedLocation = GazeManager.Instance.HitPosition;
        InstantiatePin(gazedLocation);
    }

    public void OnZoomChanged() {
        for (int i = 0; i < pinsInScene.Count; i++) {
            pinsInScene[i].prefab.transform.parent = null;
            /// since the scale of the map changes (*2 or /2) before the tiles get destroyed,
            /// before the parent is set to null the size gets multiplied by the difference
            /// so just set the localscale to pinPrefab's localScale to restore the original scale
            pinsInScene[i].prefab.transform.localScale = pinPrefab.transform.localScale;
            pinsInScene[i].prefab.SetActive(false);
        }
    }

    // this should be merged/generalized with BuildingsPlacement code (inheritance?)
    private void queryPinsWithinTileBound(UnwrappedTileId tileId) {
        Vector2dBounds bounds = Conversions.TileIdToBounds(tileId);
        var pinsWithinBound = pinsInScene.Where((pin) => {
            if (pin.coordinates.x.InRange(bounds.North, bounds.South) && 
            pin.coordinates.y.InRange(bounds.West, bounds.East)) 
                return true;

            return false;
        });

        foreach(BuildingManager.CoordinateBoundObjects pin in pinsWithinBound) {
            // set its position properly
            pin.prefab.transform.position = LocationHelper.geoCoordinateToWorldPosition(pin.coordinates);
            pin.prefab.transform.parent = CustomRangeTileProvider.InstantiatedTiles[tileId].transform;
            pin.prefab.SetActive(true);
            pin.prefab.layer = CustomRangeTileProvider.InstantiatedTiles[tileId].layer;
        }
    }

}
