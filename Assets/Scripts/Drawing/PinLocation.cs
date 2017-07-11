using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using Mapbox.Utils;

public class PinLocator: Singleton<PinLocator> {
    [SerializeField]
    private GameObject pinPrefab;

    /// <summary>
    /// contains all the pins
    /// </summary>

    public static void PinLocation(Vector3 point) {
        Vector2d pointLatLong = LocationHelper.worldPositionToGeoCoordinate(point);
    }

    /// <summary>
    /// this should be called everytime the map is zoomed or panned. The position
    /// of every pins should change anyways
    /// </summary>
    public static void MovePoints() {
        /// check if the point still fits within the map
        /// so this should be called AFTER the map stats have been updated
    }
}
