using UnityEngine;
using HoloToolkit.Unity.InputModule;

public class ZoomButton : ButtonBase {
    [Tooltip("Zoom direction")]
    public ZoomDirection InOrOut;
    private GameObject MapBox;

    public override void OnInputClicked(InputClickedEventData eventData) {
        base.OnInputClicked(eventData);
        if (MapBox == null)
            MapBox = GameObject.Find(GameObjectNamesHolder.NAME_MAPBOX);
        MapBox.SendMessage("ChangeZoom", InOrOut);
    }
}
