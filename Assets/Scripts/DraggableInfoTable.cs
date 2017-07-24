using UnityEngine;
using HoloToolkit.Unity.InputModule;
using UnityEngine.UI;
using System;
using System.Collections;

[RequireComponent(typeof(HandDraggable))]
public class DraggableInfoTable : MonoBehaviour {
    private HandDraggable handDraggableComponent;

    [SerializeField]
    private LineRenderer line;
    private FeedbackSound feedbackSoundComponent;

    [SerializeField]
    private Text tableInfoTextComponent;

    public Transform tableHolderTransform { get; set; }
    public bool ParentHasGazeFeedback { get; set; }

    [SerializeField]
    private HideButton hideButtonComponent;
    public event Action OnHideTableButtonClicked = delegate { };

    private void Start() {
        if (line == null)
            line = GetComponent<LineRenderer>();
        line.positionCount = 2;
        StartCoroutine(disableLineBeforeAnimationCompletes());

        handDraggableComponent = GetComponent<HandDraggable>();
        feedbackSoundComponent = GetComponent<FeedbackSound>();
        handDraggableComponent.OnDraggingUpdate += HandDraggableComponent_OnDraggingUpdate;
        hideButtonComponent.OnButtonClicked += DraggableInfoTable_OnButtonClicked;
        ParentHasGazeFeedback = false;
    }

    private IEnumerator disableLineBeforeAnimationCompletes() {
        line.enabled = false;
        yield return new WaitForSeconds(2.5f);
        line.enabled = true;
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
        GetComponentInChildren<Text>().text = PrefabHolder.renderBold(title);
        tableInfoTextComponent.text = "\n" + tableInfo;
    }
 
 }

