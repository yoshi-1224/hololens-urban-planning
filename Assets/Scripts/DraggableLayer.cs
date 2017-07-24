using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using System;

/// <summary>
/// Component that allows dragging an object with your hand on HoloLens.
/// Dragging is done by calculating the angular delta and z-delta between the current and previous hand positions, and then repositioning the object based on that.
/// </summary>
[RequireComponent(typeof(HandDraggable))]
public class DraggableLayer : MonoBehaviour {

    [Tooltip("The parent of all the contents this toolbar contains")]
    public GameObject ToolbarContents;
    private HandDraggable handDraggable;

    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;

    private void Start() {
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
        handDraggable = GetComponent<HandDraggable>();
        handDraggable.StartedDragging += HandDraggable_StartedDragging;
        handDraggable.StoppedDragging += HandDraggable_StoppedDragging;
    }

    private void HandDraggable_StoppedDragging() {
        MakeChildrenSiblings();
    }

    private void HandDraggable_StartedDragging() {
        MakeSiblingsChildren();
    }

    private void OnDestroy() {
        if (handDraggable != null) {
            handDraggable.StartedDragging -= HandDraggable_StartedDragging;
            handDraggable.StoppedDragging -= HandDraggable_StoppedDragging;
        }
    }

    public void MakeSiblingsChildren() {
        ToolbarContents.transform.parent = transform;
    }

    public void MakeChildrenSiblings() {
        ToolbarContents.transform.parent = transform.parent;
    }

    /// <summary>
    /// called via Unity message. Should not be deleted
    /// </summary>
    public void OnHideToolbar() {
        MakeSiblingsChildren();
        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;
        MakeChildrenSiblings();
    }
}
