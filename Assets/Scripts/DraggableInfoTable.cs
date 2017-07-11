using UnityEngine;
using System;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;

/// <summary>
/// Component that allows dragging an object with your hand on HoloLens.
/// Dragging is done by calculating the angular delta and z-delta between the current and previous hand positions,
/// and then repositioning the object based on that.
/// </summary>

[RequireComponent(typeof(HandDraggable))]
public class DraggableInfoTable : MonoBehaviour, IInputClickHandler {
    [SerializeField]
    private HandDraggable handDraggable;
    [SerializeField]
    private LineRenderer line;
    private bool shouldUpdateLinePositions;

    private void Start() {
        // Set the number of vertex fo the Line Renderer
        line.positionCount = 2;
        UpdateLinePositions();

        shouldUpdateLinePositions = false;
        handDraggable.StartedDragging += HandDraggable_StartedDragging;
        handDraggable.StoppedDragging += HandDraggable_StoppedDragging;
    }

    private void HandDraggable_StartedDragging() {
        shouldUpdateLinePositions = true;
    }

    private void HandDraggable_StoppedDragging() {
        shouldUpdateLinePositions = false;
    }

    private void OnDestroy() {
        handDraggable.StartedDragging -= HandDraggable_StartedDragging;
        handDraggable.StoppedDragging -= HandDraggable_StoppedDragging;
    }

    private void Update() {
        if (!shouldUpdateLinePositions)
            return;
        UpdateLinePositions();
    }

    public void OnFocusEnter() {
        SendMessageUpwards("EnableEmission");
    }

    public void OnFocusExit() {
        SendMessageUpwards("DisableEmission");
    }
    

    public void OnInputClicked(InputClickedEventData eventData) {
        SendMessageUpwards("HideDetails");
        SendMessageUpwards("DisableEmission");
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

    public void FillTableData(string buildingName) {
        TextMesh textMesh = GetComponent<TextMesh>();
        TableDataHolder.TableData data;
        if (TableDataHolder.Instance.dataDict.TryGetValue(buildingName, out data)) {
            string name = "<size=60><b>" + data.building_name + "</b></size>";
            string _class = "<b>Class</b> : " + data.building_class;
            string GPR = "<b>Gross Plot Ratio</b> : " + data.GPR;
            if (data.building_name == "Chinese Culture Centre") {
                string type = "(Prefab Type " + data.storeys_above_ground + ")";
                textMesh.text = name + "\n" + type + "\n\n" + _class + "\n" + GPR;
                return;
            }
            string measured_height = "<b>Measured Height</b> : " + data.measured_height + "m";
            string numStoreys = "<b>Number of Storeys</b> : " + data.storeys_above_ground;
            textMesh.text = name + "\n\n" + _class + "\n" + GPR + "\n" + measured_height + "\n" + numStoreys;
        }
        else {
            textMesh.text = "status unknown";
        }

        // add box collider at run time so that it fits the dynamically-set text sizes
        gameObject.AddComponent<BoxCollider>();
    }
}

