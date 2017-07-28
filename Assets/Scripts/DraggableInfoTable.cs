using UnityEngine;
using HoloToolkit.Unity.InputModule;
using UnityEngine.UI;
using System;
using HoloToolkit.Unity;

/// <summary>
/// This class works together with HandDraggable component to make a draggable table that can display text information
/// </summary>
[RequireComponent(typeof(HandDraggable))]
public class DraggableInfoTable : MonoBehaviour {
    private HandDraggable handDraggableComponent;
    private Interpolator interpolatorComponent;

    [SerializeField]
    private LineRenderer line;

    [SerializeField]
    private Text tableInfoTextComponent;
    private Text tableTitleTextComponent;

    public Transform tableHolderTransform { get; set; }
    public bool TableHolderHasGazeFeedback { get; set; }

    [SerializeField]
    private HideButton hideButtonComponent;
    public event Action OnHideTableButtonClicked = delegate { };

    private Transform parentTransform;

    private bool isAtStart { get; set; }

    private void Awake() {
        if (line == null)
            line = GetComponent<LineRenderer>();
        line.positionCount = 2;

        interpolatorComponent = GetComponentInParent<Interpolator>();
        interpolatorComponent.InterpolationDone += InterpolationDone;
        parentTransform = interpolatorComponent.gameObject.transform;

        handDraggableComponent = GetComponent<HandDraggable>();
        tableTitleTextComponent = GetComponentInChildren<Text>();
        handDraggableComponent.OnDraggingUpdate += HandDraggableComponent_OnDraggingUpdate;
        hideButtonComponent.OnButtonClicked += DraggableInfoTable_OnButtonClicked;
        TableHolderHasGazeFeedback = false;
        isAtStart = true;
    }

    private void OnEnable() { // this is basically when the table is to be displayed
        if (!isAtStart) {
            PositionTableObject();
        }
        line.enabled = false;
    }

    private void OnDisable() { // this is when the table is dismissed
        isAtStart = false;
        tableHolderTransform = null;
        TableHolderHasGazeFeedback = false;
    }

    private void DraggableInfoTable_OnButtonClicked() {
        OnHideTableButtonClicked.Invoke();
    }

    private void HandDraggableComponent_OnDraggingUpdate() {
        UpdateLinePositions();
    }

    private void OnDestroy() {
        if (handDraggableComponent != null) {
            handDraggableComponent.OnDraggingUpdate -= HandDraggableComponent_OnDraggingUpdate;
        }

        if (hideButtonComponent != null)
            hideButtonComponent.OnButtonClicked -= DraggableInfoTable_OnButtonClicked;
    }

    /// <summary>
    /// called when this table is being dragged, as well as when the original position
    /// of this table is set by Interactible
    /// </summary>
    public void UpdateLinePositions() {
        if (line == null)
            return;
        line.SetPosition(0, tableHolderTransform.position); //building position
        line.SetPosition(1, transform.position);
    }

    public void FillTableData(string title, string tableInfo) {
        title = title.Replace('_', ' ');
        tableTitleTextComponent.text = Utils.RenderBold(title); 
        tableInfoTextComponent.text = "\n" + tableInfo;
    }

    public void PositionTableObject() {
        if (tableHolderTransform == null) {
            Debug.Log("tableholder transform is null");
            return;
        }

        float distanceRatio = 0.4f;
        Vector3 targetPosition = distanceRatio * Camera.main.transform.position + (1 - distanceRatio) * tableHolderTransform.position;
        parentTransform.rotation = Quaternion.LookRotation(tableHolderTransform.position - Camera.main.transform.position, Vector3.up);

        parentTransform.position = tableHolderTransform.position; // start animation from where this table object holder is

        interpolatorComponent.SetTargetPosition(targetPosition);
    }

    private void InterpolationDone() {
        line.enabled = true;
        UpdateLinePositions();
    }

}

