using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;

public class GlobalVoiceCommands : MonoBehaviour, ISpeechHandler {
    [Tooltip("Adjust the scaling sensitivity applied on voice commands")]
    public float ScalingFactor;

    private GameObject map;
    public const string COMMAND_MOVE_MAP = "move map";
    public const string COMMAND_MAP_BIGGER = "map bigger";
    public const string COMMAND_MAP_SMALLER = "map smaller";

    void Start () {
        if (InputManager.Instance == null) {
            return;
        }
        InputManager.Instance.AddGlobalListener(gameObject);
	}
	
	void Update () {
		
	}

    private void OnDestroy() {
        if (InputManager.Instance == null) {
            return;
        }
        InputManager.Instance.RemoveGlobalListener(gameObject);
    }

    public void changeMapLayout(string layoutType) {
        switch (layoutType) {
            case "":

            default:
                break;
        }
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
        switch (eventData.RecognizedText.ToLower()) {
            case COMMAND_MOVE_MAP:
                moveMap();
                break;
            case COMMAND_MAP_BIGGER:
                enlargeMap();
                break;
            case COMMAND_MAP_SMALLER:
                shrinkMap();
                break;
            default:
                // just ignore
                break;
        }
    }

    private void enlargeMap() {
        if (map == null)
            map = GameObject.Find("CustomizedMap");
        bool isPlacing = map.GetComponent<InteractibleMap>().IsPlacing;
        
        if (!isPlacing)
            // make the buildings follow the same scaling as the parent map
            map.SendMessage("MakeSiblingsChildren");
        map.transform.localScale += new Vector3(ScalingFactor, ScalingFactor, ScalingFactor);
        if (!isPlacing)
            map.SendMessage("MakeChildrenSiblings");
    }

    private void shrinkMap() {
        if (map == null)
            map = GameObject.Find("CustomizedMap");

        bool isPlacing = map.GetComponent<InteractibleMap>().IsPlacing;
        
        if (!isPlacing)
            // make the buildings follow the same scaling as the parent map
            map.SendMessage("MakeSiblingsChildren");
        map.transform.localScale -= new Vector3(ScalingFactor, ScalingFactor, ScalingFactor);
        if (!isPlacing)
            map.SendMessage("MakeChildrenSiblings");
    }
}
