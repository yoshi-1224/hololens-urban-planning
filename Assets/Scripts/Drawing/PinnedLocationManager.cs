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

    public const string COMMAND_PIN_LOCATION = "save location";

    /// <summary>
    /// contains all the pins list/dictionary of something
    /// </summary>
    public Dictionary<string, BuildingManager.CoordinateBoundObject> pinsInScene { get; set; }

    private Queue<BuildingManager.CoordinateBoundObject> PinsToLoad;
    private bool shouldStartLoadingPins;

    protected override void Awake() {
        base.Awake();
        PinsToLoad = new Queue<BuildingManager.CoordinateBoundObject>();
        pinsInScene = new Dictionary<string, BuildingManager.CoordinateBoundObject>();
        CustomRangeTileProvider.OnTileObjectAdded += CustomRangeTileProvider_OnTileObjectAdded;
        CustomRangeTileProvider.OnAllTilesLoaded += CustomRangeTileProvider_OnAllTilesLoaded;
        shouldStartLoadingPins = false;
    }

    private void CustomRangeTileProvider_OnAllTilesLoaded() {
        // this is necessary not just for performance but also for correctly setting
        // the layer of the pins (off by one frame or so)
        shouldStartLoadingPins = true;
    }


    private void CustomRangeTileProvider_OnTileObjectAdded(UnwrappedTileId tileIdLoaded) {
        if (pinsInScene.Count > 0)
            queryPinsWithinTileBound(tileIdLoaded);
    }

    private void Update() {
        if (shouldStartLoadingPins && PinsToLoad.Count > 0) {
            LoadPin(PinsToLoad.Dequeue());
            if (PinsToLoad.Count == 0)
                shouldStartLoadingPins = false;
        }
    }

    public void InstantiatePin(Vector3 point) {
        Vector2d pointLatLong = LocationHelper.WorldPositionToGeoCoordinate(point);
        BuildingManager.CoordinateBoundObject newPin = new BuildingManager.CoordinateBoundObject();
        newPin.latitude = (float) pointLatLong.x;
        newPin.longitude = (float) pointLatLong.y;
        GameObject parentTile = LocationHelper.FindParentTile(pointLatLong);
        GameObject pinObject = Instantiate(pinPrefab, point, Quaternion.identity);
        pinObject.name = "pin #" + pinsInScene.Count;
        newPin.gameObject = pinObject;
        pinObject.transform.SetParent(parentTile.transform, true);
        pinsInScene[pinObject.name] = newPin;

        // add to the dropdown
        DropDownPinnedLocations.Instance.AddPinToDropDown(pinObject.name);
    }

    public void LoadPin(BuildingManager.CoordinateBoundObject pin) {
        GameObject pinObject = pin.gameObject;
        if (pinObject == null) // if deleted
            return;
        GameObject parentTile = LocationHelper.FindParentTile(pin.coordinates);
        pinObject.transform.position = LocationHelper.geoCoordinateToWorldPosition(pin.coordinates);
        pinObject.transform.parent = parentTile.transform;
        pinObject.SetActive(true);
        pinObject.layer = parentTile.layer; // this is set too early for some reason
    }

    public void pinGazedLocation() {
        Vector3 gazedLocation = GazeManager.Instance.HitPosition;
        InstantiatePin(gazedLocation);
    }

    public void OnZoomChanged() {
        foreach (BuildingManager.CoordinateBoundObject pin in pinsInScene.Values) {
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

    // this should be merged/generalized with BuildingsPlacement code (inheritance?)
    private void queryPinsWithinTileBound(UnwrappedTileId tileId) {
        Vector2dBounds bounds = Conversions.TileIdToBounds(tileId);
        var pinsWithinBound = pinsInScene.Where((pin) => {
            if (pin.Value.coordinates.x.InRange(bounds.North, bounds.South) && 
            pin.Value.coordinates.y.InRange(bounds.West, bounds.East)) 
                return true;

            return false;
        });

        foreach(var pin in pinsWithinBound) {
            PinsToLoad.Enqueue(pin.Value);
        }
    }

}
