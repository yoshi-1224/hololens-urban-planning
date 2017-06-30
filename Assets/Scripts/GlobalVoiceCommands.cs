using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity;
using System;
using UnityEngine.SceneManagement;

public class GlobalVoiceCommands : Singleton<GlobalVoiceCommands>, ISpeechHandler {
    [Tooltip("Adjust the scaling sensitivity applied on voice commands")]
    public float ScalingFactor;

    private GameObject toolMenuObject;

    private GameObject map;
    public const string COMMAND_MOVE_MAP = "move map";
    public const string COMMAND_MAP_BIGGER = "map bigger";
    public const string COMMAND_MAP_SMALLER = "map smaller";
    public const string COMMAND_SCALE_MAP = "scale map";
    public const string COMMAND_SHOW_TOOLS = "show tools";
    public const string COMMAND_HIDE_TOOLS = "hide tools";
    public const string COMMAND_ROTATE_MAP = "rotate map";
    public const string COMMAND_RESET = "reset";
    public const string COMMAND_QUIT_APP = "quit application";
    public const string COMMAND_DRAW_POLYGON = "polygon";
    public const string COMMAND_CANCEL = "cancel";

    private float toolsDistanceFromCamera = 1.3f;
    public bool IsInStreetViewMode = false;
    public bool IsInDrawingMode = false;

    void Start() {
        if (InputManager.Instance == null) {
            return;
        }
        InputManager.Instance.AddGlobalListener(gameObject);
        toolMenuObject = GameObject.Find("Toolbar");
        toolMenuObject.SetActive(false);
    }

    protected override void OnDestroy() {
        if (InputManager.Instance == null)
            return;
        InputManager.Instance.RemoveGlobalListener(gameObject);
    }

    /// <summary>
    /// handler for "move map" voice command. Has the same effect as selecting the map
    /// </summary>
    public void moveMap() {
        map.SendMessage("OnInputClicked", new InputClickedEventData(null));
    }

    public void OnSpeechKeywordRecognized(SpeechKeywordRecognizedEventData eventData) {
        if (map == null)
            map = GameObject.Find("CustomizedMap");
        if (map == null) // still null then it has not yet been instantiated
            return;

        string keyword = eventData.RecognizedText.ToLower();
        switch(keyword) {
            case COMMAND_RESET:
                resetScene();
                return;
            case COMMAND_QUIT_APP:
                quitApplication();
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
                    scaleMap();
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

    private void quitApplication() {
        Application.Quit();
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

    /// <summary>
    /// enable manipulation gesture to scale the map together with the buildings
    /// </summary>
    private void scaleMap() {
        map.SendMessage("RegisterForScaling");
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

    private void registerMapForRotation() {
        Debug.Log("Registering map for rotation");
        map.SendMessage("RegisterForRotation");
    }
}
