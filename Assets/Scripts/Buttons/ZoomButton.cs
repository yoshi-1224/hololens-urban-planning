using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;

public class ZoomButton : ButtonBase {
    [Tooltip("Zoom direction")]
    public CustomRangeTileProvider.ZoomDirection InOrOut;
    private GameObject MapBox;

    //protected override void Start() {
    //    base.Start();
    //}

    public override void OnInputClicked(InputClickedEventData eventData) {
        base.OnInputClicked(eventData);
        if (MapBox == null)
            MapBox = GameObject.Find(GameObjectNamesHolder.NAME_MAPBOX);
        MapBox.SendMessage("ChangeZoom", InOrOut);
    }
}
