using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameObjectNamesHolder {
    public static string NAME_MAP_PARENT = "MapboxParent";
    public static string NAME_MAP = "Mapbox";
    public static string NAME_TOOL_BAR = "Toolbar";
    public static string NAME_MAP_DATA_HOLDER = "DataHolder";

    // within data holder
    public static string NAME_DATA_MAP_ZOOM = "Zoom";
    public static string NAME_DATA_MAP_THEME = "Theme";
    public static string NAME_DATA_MAP_LATITUDE_LONGITUDE = "CenterCoordinates";
    public static string NAME_DATA_MAP_SCALE = "WorldRelativeScale";
    public static string NAME_DATA_CURSOR = "CustomCursorWithFeedback";

    public static string NAME_LAYER_MAP = "MapObjects";
    public static int LAYER_VISIBLE_TILES = LayerMask.NameToLayer("VisibleTiles");
    public static int LAYER_INVISIBLE_TILES = LayerMask.NameToLayer("InvisibleTiles");

}
