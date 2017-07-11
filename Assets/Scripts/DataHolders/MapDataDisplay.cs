using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;

/// <summary>
/// holds the map data and is responsible for updating the map data display
/// in toolbar. 
/// Each field value should be updated in the setter in the corresponding value
/// stored in the relavant classes e.g. CustomMap
/// </summary>
public class MapDataDisplay : Singleton<MapDataDisplay> {
    [SerializeField]
    private TextMesh TextMesh_MapTheme;
    [SerializeField]
    private TextMesh TextMesh_MapCenterCoordinates;
    [SerializeField]
    private TextMesh TextMesh_MapWorldRelativeScale;
    [SerializeField]
    private TextMesh TextMesh_MapZoom;

    private bool isAtstart = true;
    
    // data to display
    public string MapCentreCoordinates { get; set; }
    public string MapStyle { get; set; }
    public int MapZoom { get; set; }
    public float MapWorldRelativeScale { get; set; }

    private void OnEnable() {
        // nullPointerException will be thrown if we call this at the start
        if (!isAtstart)
            UpdateMapInfo();
        isAtstart = false;
    }

    /// <summary>
    /// updates ALL the map data field on the display
    /// </summary>
    public void UpdateMapInfo() {
        UpdateZoomInfo(MapZoom);
        UpdateWorldRelativeScaleInfo(MapWorldRelativeScale);
        UpdateCenterCoordinatesInfo(MapCentreCoordinates);
    }

    public void UpdateZoomInfo(int newZoom) {
        MapZoom = newZoom;
        TextMesh_MapZoom.text = RenderBold("Zoom Level: ") + MapZoom;
    }

    public void UpdateWorldRelativeScaleInfo(float newScale) {
        MapWorldRelativeScale = newScale;
        TextMesh_MapWorldRelativeScale.text = RenderBold("World-relative Scale: ") + string.Format("{0:0.0000}", MapWorldRelativeScale);
    }

    public void UpdateCenterCoordinatesInfo(string newCoordinates) {
        var coordinatesSplit = newCoordinates.Split(',');
        MapCentreCoordinates = string.Format("{0:0.00000}, {1:0.0000}", double.Parse(coordinatesSplit[0]), double.Parse(coordinatesSplit[1]));
        TextMesh_MapCenterCoordinates.text = RenderBold("Center Coordinates: ") + "\n" + MapCentreCoordinates;
    }

    public void UpdateThemeInfo() {
        TextMesh_MapTheme.text = RenderBold("Theme: ") + "Street";
    }

    private string RenderBold(string str) {
        return "<b>" + str + "</b>";
    }

    
}
