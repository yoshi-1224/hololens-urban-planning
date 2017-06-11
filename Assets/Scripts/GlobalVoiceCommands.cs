using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;

public class GlobalVoiceCommands : MonoBehaviour {
    private GameObject map;

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

        map.SendMessage("OnInputClicked", null);
    }
}
