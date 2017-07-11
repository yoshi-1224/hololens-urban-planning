using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;

public class DrawButton : ButtonBase {
    public override void OnInputClicked(InputClickedEventData eventData) {
        base.OnInputClicked(eventData);
        //GlobalVoiceCommands.Instance.enterDrawingMode();
        //GameObject.Find(GameObjectNamesHolder.NAME_MAP).SendMessage("PrintPositions");
        GameObject.Find(GameObjectNamesHolder.NAME_MAP).SendMessage("PrintCoordinates");
    }
}
