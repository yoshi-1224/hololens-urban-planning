using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity;
using System;
using UnityEngine.SceneManagement;

public class GlobalVoiceCommands : Singleton<GlobalVoiceCommands>, ISpeechHandler {
    private GameObject toolMenuObject;

    [SerializeField]
    private InteractibleMap mapParentInteractible;

#region commands strings
    public const string COMMAND_MOVE_MAP = "move map";
    public const string COMMAND_SCALE_MAP = "scale map";
    public const string COMMAND_ROTATE_MAP = "rotate map";

    public const string COMMAND_SHOW_TOOLS = "show tools";
    public const string COMMAND_HIDE_TOOLS = "hide tools";
    public const string COMMAND_RESET = "reset";

    public const string COMMAND_DRAW_POLYGON = "polygon";
    public const string COMMAND_PIN_LOCATION = "pin location";
    public const string COMMAND_CANCEL = "cancel";

    #endregion

    private float toolsDistanceFromCamera = 1.3f;
    public bool IsInStreetViewMode { get; set; }
    public bool IsInDrawingMode { get; set; }

    void Start() {
        if (InputManager.Instance == null) {
            return;
        }
        InputManager.Instance.AddGlobalListener(gameObject);
        toolMenuObject = GameObject.Find(GameObjectNamesHolder.NAME_TOOL_BAR);
        IsInDrawingMode = false;
        IsInStreetViewMode = false;
    }

    protected override void OnDestroy() {
        if (InputManager.Instance == null)
            return;
        InputManager.Instance.RemoveGlobalListener(gameObject);
    }

    public void OnSpeechKeywordRecognized(SpeechKeywordRecognizedEventData eventData) {

        string keyword = eventData.RecognizedText.ToLower();
        switch(keyword) {
            case COMMAND_RESET:
                resetScene();
                return;
            case COMMAND_CANCEL:
                cancelDrawing();
                break;
        }

        if (IsInStreetViewMode) {
            // allowable voice commands in streetviewmode
            switch (keyword) {
                case StreetView.COMMAND_EXIT_STREET_VIEW:
                    StreetView.Instance.ExitStreetView();
                    break;
            } // end switch

        } else {
            // allowable voice commands in normal mode
            switch (keyword) {
                case COMMAND_MOVE_MAP:
                    moveMap();
                    break;
                case COMMAND_SCALE_MAP:
                    registerMapForScaling();
                    break;
                case COMMAND_SHOW_TOOLS:
                    showTools();
                    break;
                case COMMAND_HIDE_TOOLS:
                    HideTools();
                    break;
                case COMMAND_ROTATE_MAP:
                    registerMapForRotation();
                    break;
                case COMMAND_DRAW_POLYGON:
                    enterDrawingMode();
                    break;
                default:
                    // just ignore
                    break;
            } // end switch
        }
    }

    public void EnterStreetViewMode() {
        IsInStreetViewMode = true;
    }

    public void ExitStreetViewMode() {
        IsInStreetViewMode = false;
    }

    private void resetScene() {
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
    }

    private void markLocation() {
        GameObject hitObject = GazeManager.Instance.HitObject;
        if (hitObject == null || hitObject.layer != LayerMask.NameToLayer(GameObjectNamesHolder.NAME_LAYER_MAP))
            return;
        PinLocation.MarkPoint(GazeManager.Instance.HitPosition);
        // the gazed object was indeed a map
    }

    public void enterDrawingMode() {
        if (!IsInDrawingMode)
            DrawingManager.Instance.StartDrawing();
        IsInDrawingMode = true;
    }

    public void cancelDrawing() {
        if (IsInDrawingMode)
            DrawingManager.Instance.StopDrawing();
        IsInDrawingMode = false;
    }

    public void moveMap() {
        mapParentInteractible.PlacementStart();
    }

    /// <summary>
    /// enable manipulation gesture to scale the map together with the buildings
    /// </summary>
    private void registerMapForScaling() {
        mapParentInteractible.GetComponent<Scalable>().RegisterForScaling(Scalable.ScalingMode.Navigation);
    }

    private void registerMapForRotation() {
        mapParentInteractible.GetComponent<Rotatable>().RegisterForRotation();
    }

    /// <summary>
    /// activates the tools menu
    /// </summary>
    private void showTools() {
        if (!toolMenuObject.activeSelf)
            toolMenuObject.SetActive(true);
        positionTools();
    }

    public void HideTools() {
        if (!toolMenuObject.activeSelf)
            return;
        InteractibleButton.onToolbarMoveOrDisable();
        GameObject.Find("BaseLayer").SendMessage("OnHideToolbar");
        toolMenuObject.SetActive(false);
    }

    private void positionTools() {
        Vector3 moveDirection = Camera.main.transform.forward;
        Vector3 destPoint = Camera.main.transform.position + moveDirection * toolsDistanceFromCamera;
        toolMenuObject.transform.position = destPoint;
        Vector3 lookDirection = Camera.main.transform.position;
        toolMenuObject.transform.LookAt(lookDirection);

        // keep it upright
        Quaternion upRotation = Quaternion.FromToRotation(toolMenuObject.transform.up, Vector3.up);
        toolMenuObject.transform.rotation = upRotation * toolMenuObject.transform.rotation;
    }

}
