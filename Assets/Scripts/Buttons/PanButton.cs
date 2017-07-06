using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;

public class PanButton : ButtonBase {
    [Tooltip("Direction in which to pan the map")]
    public CustomRangeTileProvider.Direction direction;

    protected override void Start() {
        base.Start();
    }

    public override void OnInputClicked(InputClickedEventData eventData) {
        base.OnInputClicked(eventData);
        GameObject.Find("MapboxMap").SendMessage("PanTowards", direction);
    }

}
