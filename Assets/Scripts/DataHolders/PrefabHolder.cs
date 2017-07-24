using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using Mapbox.Utils;

public class PrefabHolder : Singleton<PrefabHolder> {
    public GameObject tablePrefab;
    public GameObject guidePrefab;

    // some util functions
    public static string renderBold(string str) {
        return "<b>" + str + "</b>";
    }

    public static string formatLatLong(Vector2d coordinates) {
        string lat = renderBold("Lat: ") + string.Format(" {0:0.000}", coordinates.x);
        string lng = renderBold("Long: ") + string.Format(" {0:0.000}", coordinates.y);
        return lat + " " + lng;
    }

    public static string changeTextSize(string str, int size) {
        return "<size=" + size + ">" + str + "</size>";
    }
}
