using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;

/// <summary>
/// to be attached to the dynamically created table. Make sure to set table.transform to
/// building.transform for the messages to be correctly received by the building.
/// </summary>
public class TableInteractible : MonoBehaviour, IInputClickHandler, IFocusable {

    /// <summary>
    /// dismisses the table
    /// </summary>
    /// 
    public void OnInputClicked(InputClickedEventData eventData) {
        SendMessageUpwards("HideDetails");
        SendMessageUpwards("DisableEmission");
    }

    public void OnFocusEnter() {
        SendMessageUpwards("EnableEmission");
    }

    public void OnFocusExit() {
        SendMessageUpwards("DisableEmission");
    }
}
