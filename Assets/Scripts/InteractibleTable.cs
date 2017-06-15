using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;

/// <summary>
/// to be attached to the dynamically created table. Make sure to set table.transform to
/// building.transform for the messages to be correctly received by the building.
/// </summary>
public class InteractibleTable : MonoBehaviour, IInputClickHandler, IFocusable {

    /// <summary>
    /// dismisses the table
    /// </summary>
    /// 
    public Color lineColour;
    public float lineWidth;
    private LineRenderer line;
    private void Start() {
        line = gameObject.AddComponent<LineRenderer>();
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        
        // Set the number of vertex fo the Line Renderer
        line.positionCount = 2;
        line.SetPosition(0, transform.parent.position); //table position
        line.SetPosition(1, transform.position);
        //set the material here
        line.startColor = lineColour;
        line.endColor = lineColour;
    }

    public void OnInputClicked(InputClickedEventData eventData) {
        SendMessageUpwards("HideDetails");
        SendMessageUpwards("DisableEmission");
    }

    // should probably enhance it with draggable
    private void Update() {
        line.SetPosition(0, transform.parent.position); //table position
        line.SetPosition(1, transform.position);
    }

    public void OnFocusEnter() {
        SendMessageUpwards("EnableEmission");
    }

    public void OnFocusExit() {
        SendMessageUpwards("DisableEmission");
    }

}
