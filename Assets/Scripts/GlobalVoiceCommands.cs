using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity;
using System;

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

    public const bool IS_ENLARGE = true;
    private float toolsDistanceFromCamera = 1f;
    public bool IsInStreetViewMode = false;

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
        if (map == null)
            map = GameObject.Find("CustomizedMap");
        map.SendMessage("OnInputClicked", new InputClickedEventData(null));
    }

    public void OnSpeechKeywordRecognized(SpeechKeywordRecognizedEventData eventData) {
        string keyword = eventData.RecognizedText.ToLower();
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
                case COMMAND_MAP_BIGGER:
                    enlargeMap(IS_ENLARGE);
                    break;
                case COMMAND_MAP_SMALLER:
                    enlargeMap(!IS_ENLARGE);
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

    /// <summary>
    /// scales the map together with the buildings by ScalingFactor directly via voice commands
    /// </summary>
    private void enlargeMap(bool enlarge) {
        if (map == null)
            map = GameObject.Find("CustomizedMap");
        bool isPlacing = map.GetComponent<InteractibleMap>().IsPlacing;

        // if enlarge == true, make the map bigger. else smaller
        int sign = enlarge ? 1 : -1;
        if (!isPlacing)
            // make the buildings follow the same scaling as the parent map
            map.SendMessage("MakeSiblingsChildren");
        map.transform.localScale += new Vector3(sign * ScalingFactor, sign * ScalingFactor, sign * ScalingFactor);
        if (!isPlacing)
            map.SendMessage("MakeChildrenSiblings");
    }

    /// <summary>
    /// enable manipulation gesture to scale the map together with the buildings
    /// </summary>
    private void scaleMap() {
        if (map == null)
            map = GameObject.Find("CustomizedMap");
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
