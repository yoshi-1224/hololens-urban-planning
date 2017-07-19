using UnityEngine;
using System;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using Mapbox.Utils;

/// <summary>
/// Component that allows dragging an object with your hand on HoloLens.
/// Dragging is done by calculating the angular delta and z-delta between the current and previous hand positions,
/// and then repositioning the object based on that.
/// </summary>

[RequireComponent(typeof(HandDraggable))]
public class DraggableInfoTable : MonoBehaviour, IInputClickHandler {
    private HandDraggable handDraggableComponent;
    [SerializeField]
    private LineRenderer line;
    private bool shouldUpdateLinePositions;

    private void Start() {
        // Set the number of vertex fo the Line Renderer
        line.positionCount = 2;
        UpdateLinePositions();
        shouldUpdateLinePositions = false;
        handDraggableComponent = GetComponent<HandDraggable>();
        handDraggableComponent.StartedDragging += HandDraggable_StartedDragging;
        handDraggableComponent.StoppedDragging += HandDraggable_StoppedDragging;
    }

    private void HandDraggable_StartedDragging() {
        shouldUpdateLinePositions = true;
    }

    private void HandDraggable_StoppedDragging() {
        shouldUpdateLinePositions = false;
    }

    private void OnDestroy() {
        handDraggableComponent.StartedDragging -= HandDraggable_StartedDragging;
        handDraggableComponent.StoppedDragging -= HandDraggable_StoppedDragging;
    }

    private void Update() {
        if (!shouldUpdateLinePositions)
            return;
        UpdateLinePositions();
    }

    public void OnFocusEnter() {
        //SendMessageUpwards("EnableEmission");
    }

    public void OnFocusExit() {
        //SendMessageUpwards("DisableEmission");
    }
    

    public void OnInputClicked(InputClickedEventData eventData) {
        SendMessageUpwards("HideDetails");
        //SendMessageUpwards("DisableEmission");
    }
    
    /// <summary>
    /// called when this table is being dragged, as well as when the original position
    /// of this table is set by Interactible
    /// </summary>
    public void UpdateLinePositions() {
        if (line == null)
            return;
        line.SetPosition(0, transform.parent.position); //building position
        line.SetPosition(1, transform.position);
    }

    public void FillTableData(string textToDisplay) {
        TextMesh textMesh = GetComponent<TextMesh>();
        textMesh.text = textToDisplay;

        // add box collider at run time so that it fits the dynamically-set text sizes
        gameObject.AddComponent<BoxCollider>();
    }
 
 }

