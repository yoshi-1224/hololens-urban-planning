using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;

public class ZoomButton : ButtonBase {
    [Tooltip("Zoom direction")]
    public CustomRangeTileProvider.ZoomDirection InOrOut;

    protected override void Start() {
        base.Start();
    }

    public override void OnInputClicked(InputClickedEventData eventData) {
        base.OnInputClicked(eventData);
        GameObject.Find("MapboxMap").SendMessage("ChangeZoom", InOrOut);
    }
}
